import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/invoice.dart';
import '../../shared/models/sale_return.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import '../customers/customers_repository.dart';
import 'sales_returns_repository.dart';

// ── Providers ─────────────────────────────────────────────────────────────────

final _returnLinesProvider =
    FutureProvider.autoDispose.family<List<InvoiceLine>, String>(
  (ref, invoiceId) =>
      ref.read(customersRepositoryProvider).invoiceLines(invoiceId),
);

// ── Screen ────────────────────────────────────────────────────────────────────

class SaleReturnScreen extends ConsumerStatefulWidget {
  const SaleReturnScreen({
    super.key,
    this.invoiceId,
    this.invoice,
  });

  /// Optional pre-selected invoice passed as route extra.
  final String? invoiceId;
  final Invoice? invoice;

  @override
  ConsumerState<SaleReturnScreen> createState() => _SaleReturnScreenState();
}

class _SaleReturnScreenState extends ConsumerState<SaleReturnScreen> {
  String? _resolvedInvoiceId;
  Invoice? _resolvedInvoice;

  // Line item selection: partId → selected quantity (0 = not selected)
  final Map<String, int> _selectedQty = {};

  // Return options
  int _reasonIndex = 0;
  int _refundTypeIndex = 0;

  bool _submitting = false;

  static const _reasons = [
    'Wrong part',
    'Defective',
    'Customer changed mind',
    'Damaged in transit',
    'Other',
  ];

  static const _refundTypes = ['Cash refund', 'Store credit'];
  static const _refundTypeCodes = ['CASH_REFUND', 'STORE_CREDIT'];

  @override
  void initState() {
    super.initState();
    if (widget.invoice != null) {
      _resolvedInvoice = widget.invoice;
      _resolvedInvoiceId = widget.invoice!.id;
    } else if (widget.invoiceId != null) {
      _resolvedInvoiceId = widget.invoiceId;
    }
  }

  // ── computed ────────────────────────────────────────────────────────────────

  double _computeRefundTotal(List<InvoiceLine> lines) {
    double total = 0;
    for (final line in lines) {
      final qty = _selectedQty[line.partId ?? ''] ?? 0;
      if (qty > 0) total += qty * line.unitPrice;
    }
    return total;
  }

  int get _selectedCount =>
      _selectedQty.values.where((q) => q > 0).length;

  // ── submit ──────────────────────────────────────────────────────────────────

  Future<void> _submit(List<InvoiceLine> lines) async {
    final invoiceNumber = _resolvedInvoice?.invoiceNumber ?? '';

    if (invoiceNumber.isEmpty) {
      _showError('No invoice loaded.');
      return;
    }

    final items = lines
        .where((l) =>
            l.partId != null && (_selectedQty[l.partId!] ?? 0) > 0)
        .map((l) => QuickReturnItem(
              partId: l.partId!,
              quantity: _selectedQty[l.partId!]!,
              reason: _reasons[_reasonIndex],
            ))
        .toList();

    if (items.isEmpty) {
      _showError('Select at least one item to return.');
      return;
    }

    setState(() => _submitting = true);
    final messenger = ScaffoldMessenger.of(context);
    final nav = Navigator.of(context);
    try {
      final result = await ref
          .read(salesReturnsRepositoryProvider)
          .createQuickReturn(
            originalInvoiceNumber: invoiceNumber,
            items: items,
            refundType: _refundTypeCodes[_refundTypeIndex],
          );
      if (!mounted) return;
      nav.pop();
      messenger.showSnackBar(SnackBar(
        content: Text(
            '${result.returnNumber} submitted · ${formatCurrency(result.refundAmount)}'),
        backgroundColor: AppColors.green,
      ));
    } on AppException catch (e) {
      _showError(e.message);
    } catch (_) {
      _showError('Failed to submit return. Please try again.');
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  void _showError(String msg) {
    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
      content: Text(msg),
      backgroundColor: AppColors.red,
    ));
  }

  // ── build ───────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final hasPreloaded = _resolvedInvoiceId != null;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Sale Return',
              style: GoogleFonts.instrumentSans(
                fontSize: 16,
                fontWeight: FontWeight.w700
              ),
            ),
            if (_resolvedInvoice != null)
              Text(
                _resolvedInvoice!.invoiceNumber,
                style: GoogleFonts.instrumentSans(
                  fontSize: 12
                ),
              ),
          ],
        ),
      ),
      body: hasPreloaded
          ? _LoadedBody(
              invoiceId: _resolvedInvoiceId!,
              invoice: _resolvedInvoice,
              selectedQty: _selectedQty,
              reasonIndex: _reasonIndex,
              refundTypeIndex: _refundTypeIndex,
              reasons: _reasons,
              refundTypes: _refundTypes,
              submitting: _submitting,
              onReasonSelect: (i) => setState(() => _reasonIndex = i),
              onRefundTypeSelect: (i) =>
                  setState(() => _refundTypeIndex = i),
              onQtyChange: (partId, qty) =>
                  setState(() => _selectedQty[partId] = qty),
              onSubmit: _submit,
              selectedCount: _selectedCount,
              computeRefundTotal: _computeRefundTotal,
            )
          : const _ManualEntry(),
    );
  }
}

// ── No invoice selected ───────────────────────────────────────────────────────

class _ManualEntry extends StatelessWidget {
  const _ManualEntry();

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 32, 16, 32),
      children: [
        Container(
          padding: const EdgeInsets.all(20),
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          child: Column(
            children: [
              Container(
                width: 56,
                height: 56,
                decoration: BoxDecoration(
                  color: AppColors.amberBg,
                  borderRadius: BorderRadius.circular(16),
                ),
                child: const Icon(Icons.receipt_long_outlined,
                    size: 28, color: AppColors.amber),
              ),
              const SizedBox(height: 16),
              Text(
                'Open an invoice first',
                style: GoogleFonts.instrumentSans(
                  fontSize: 15,
                  fontWeight: FontWeight.w700
                ),
              ),
              const SizedBox(height: 8),
              Text(
                'Go to Customers → select a customer → Invoices tab → tap an invoice → "Initiate return".',
                textAlign: TextAlign.center,
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  height: 1.5,
                ),
              ),
              const SizedBox(height: 20),
              SizedBox(
                width: double.infinity,
                child: OutlinedButton.icon(
                  onPressed: () => Navigator.of(context).pop(),
                  icon: const Icon(Icons.arrow_back, size: 16),
                  label: const Text('Go back'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.ink,
                    side: BorderSide(color: Theme.of(context).colorScheme.outline),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(12)),
                    padding: const EdgeInsets.symmetric(vertical: 13),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, fontWeight: FontWeight.w600),
                  ),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ── Loaded body (invoice id is known) ─────────────────────────────────────────

class _LoadedBody extends ConsumerWidget {
  const _LoadedBody({
    required this.invoiceId,
    required this.invoice,
    required this.selectedQty,
    required this.reasonIndex,
    required this.refundTypeIndex,
    required this.reasons,
    required this.refundTypes,
    required this.submitting,
    required this.onReasonSelect,
    required this.onRefundTypeSelect,
    required this.onQtyChange,
    required this.onSubmit,
    required this.selectedCount,
    required this.computeRefundTotal,
  });

  final String invoiceId;
  final Invoice? invoice;
  final Map<String, int> selectedQty;
  final int reasonIndex;
  final int refundTypeIndex;
  final List<String> reasons;
  final List<String> refundTypes;
  final bool submitting;
  final void Function(int) onReasonSelect;
  final void Function(int) onRefundTypeSelect;
  final void Function(String partId, int qty) onQtyChange;
  final Future<void> Function(List<InvoiceLine>) onSubmit;
  final int selectedCount;
  final double Function(List<InvoiceLine>) computeRefundTotal;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final linesAsync = ref.watch(_returnLinesProvider(invoiceId));

    return linesAsync.when(
      loading: () => const LoadingView(),
      error: (e, _) => ListView(children: [
        const SizedBox(height: 120),
        ErrorView(
          message: e is AppException ? e.message : 'Failed to load invoice.',
          onRetry: () => ref.invalidate(_returnLinesProvider(invoiceId)),
        ),
      ]),
      data: (lines) => Stack(
        children: [
          ListView(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
            children: [
              // ── Invoice header ─────────────────────────────────────
              if (invoice != null) ...[
                CardSection(
                  padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
                  child: Row(
                    children: [
                      Expanded(
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Text(
                              invoice!.invoiceNumber,
                              style: GoogleFonts.instrumentSans(
                                fontSize: 13.5,
                                fontWeight: FontWeight.w600
                              ),
                            ),
                            Text(
                              'SO: ${invoice!.salesOrderNumber}',
                              style: GoogleFonts.instrumentSans(
                                fontSize: 11.5
                              ),
                            ),
                          ],
                        ),
                      ),
                      Text(
                        formatCurrency(invoice!.grandTotal),
                        style: GoogleFonts.instrumentSans(
                          fontSize: 14,
                          fontWeight: FontWeight.w700
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 16),
              ],

              // ── Select items ───────────────────────────────────────
              Text(
                'Select items to return',
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600
                ),
              ),
              const SizedBox(height: 8),
              lines.isEmpty
                  ? const EmptyView(
                      message: 'No items found on this invoice.',
                      icon: Icons.receipt_long_outlined)
                  : CardSection(
                      padding: EdgeInsets.zero,
                      child: Column(
                        children: lines.asMap().entries.map((e) {
                          final isLast = e.key == lines.length - 1;
                          return Column(
                            children: [
                              _ReturnLineRow(
                                line: e.value,
                                qty: selectedQty[e.value.partId ?? ''] ?? 0,
                                onQtyChange: (q) {
                                  if (e.value.partId != null) {
                                    onQtyChange(e.value.partId!, q);
                                  }
                                },
                              ),
                              if (!isLast)
                                Divider(
                                    height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                            ],
                          );
                        }).toList(),
                      ),
                    ),
              const SizedBox(height: 16),

              // ── Reason ─────────────────────────────────────────────
              Text(
                'Reason for return',
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600
                ),
              ),
              const SizedBox(height: 8),
              FilterChipRow(
                selected: reasonIndex,
                onSelect: onReasonSelect,
                chips: reasons
                    .map((r) => FilterChipData(label: r))
                    .toList(),
              ),
              const SizedBox(height: 16),

              // ── Refund type ────────────────────────────────────────
              Text(
                'Refund method',
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600
                ),
              ),
              const SizedBox(height: 8),
              FilterChipRow(
                selected: refundTypeIndex,
                onSelect: onRefundTypeSelect,
                chips: refundTypes
                    .map((r) => FilterChipData(label: r))
                    .toList(),
              ),
              const SizedBox(height: 16),

              // ── Summary ────────────────────────────────────────────
              CardSection(
                child: Column(
                  children: [
                    _SummaryRow(
                      label: 'Items selected',
                      value: '$selectedCount',
                    ),
                    const SizedBox(height: 8),
                    _SummaryRow(
                      label: 'Refund method',
                      value: refundTypes[refundTypeIndex],
                    ),
                    Padding(
                      padding: EdgeInsets.symmetric(vertical: 10),
                      child: Divider(
                          height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                    ),
                    _SummaryRow(
                      label: 'Refund total',
                      value: formatCurrency(computeRefundTotal(lines)),
                      valueStyle: GoogleFonts.instrumentSans(
                        fontSize: 19,
                        fontWeight: FontWeight.w700,
                        color: AppColors.red,
                      ),
                    ),
                  ],
                ),
              ),
            ],
          ),

          // ── Sticky CTA ─────────────────────────────────────────────
          Positioned(
            bottom: 0,
            left: 0,
            right: 0,
            child: PrimaryCtaBar(
              label: '↩  Confirm return',
              onTap: () => onSubmit(lines),
              isLoading: submitting,
              backgroundColor: AppColors.red,
              shadowColor: const Color(0x40D63841),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Return line row ───────────────────────────────────────────────────────────

class _ReturnLineRow extends StatelessWidget {
  const _ReturnLineRow({
    required this.line,
    required this.qty,
    required this.onQtyChange,
  });

  final InvoiceLine line;
  final int qty;
  final void Function(int) onQtyChange;

  @override
  Widget build(BuildContext context) {
    final selected = qty > 0;
    final unit = line.unitSymbol ?? '';
    return Padding(
      padding: const EdgeInsets.fromLTRB(14, 11, 14, 11),
      child: Row(
        children: [
          // Checkbox
          GestureDetector(
            onTap: () => onQtyChange(selected ? 0 : 1),
            child: AnimatedContainer(
              duration: const Duration(milliseconds: 150),
              width: 22,
              height: 22,
              decoration: BoxDecoration(
                color: selected ? AppColors.ink : Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(6),
                border: Border.all(
                  color: selected ? AppColors.ink : Theme.of(context).colorScheme.outline,
                  width: 1.5,
                ),
              ),
              child: selected
                  ? const Icon(Icons.check, size: 14, color: Colors.white)
                  : null,
            ),
          ),
          const SizedBox(width: 12),
          // Name + SKU
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  line.displayName,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13,
                    fontWeight: FontWeight.w500
                  ),
                ),
                Text(
                  '${line.quantity}${unit.isNotEmpty ? ' $unit' : ''} × ${formatCurrency(line.unitPrice)}',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 11, color: AppColors.muted),
                ),
              ],
            ),
          ),
          // Qty stepper (only when selected)
          if (selected) ...[
            const SizedBox(width: 8),
            _QtyStepper(
              qty: qty,
              max: line.quantity,
              onChanged: onQtyChange,
            ),
          ] else ...[
            Text(
              formatCurrency(line.lineTotal),
              style: GoogleFonts.instrumentSans(
                fontSize: 13,
                fontWeight: FontWeight.w600
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Quantity stepper ──────────────────────────────────────────────────────────

class _QtyStepper extends StatelessWidget {
  const _QtyStepper({
    required this.qty,
    required this.max,
    required this.onChanged,
  });

  final int qty;
  final int max;
  final void Function(int) onChanged;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        _StepBtn(
          icon: Icons.remove,
          onTap: qty > 1 ? () => onChanged(qty - 1) : null,
        ),
        Container(
          width: 32,
          alignment: Alignment.center,
          child: Text(
            '$qty',
            style: GoogleFonts.instrumentSans(
              fontSize: 13,
              fontWeight: FontWeight.w600
            ),
          ),
        ),
        _StepBtn(
          icon: Icons.add,
          onTap: qty < max ? () => onChanged(qty + 1) : null,
        ),
      ],
    );
  }
}

class _StepBtn extends StatelessWidget {
  const _StepBtn({required this.icon, this.onTap});

  final IconData icon;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final enabled = onTap != null;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 28,
        height: 28,
        decoration: BoxDecoration(
          color: enabled ? Theme.of(context).scaffoldBackgroundColor : Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(8),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        child: Icon(
          icon,
          size: 14,
          color: enabled ? AppColors.ink : AppColors.disabled,
        ),
      ),
    );
  }
}

// ── Summary row ───────────────────────────────────────────────────────────────

class _SummaryRow extends StatelessWidget {
  const _SummaryRow({
    required this.label,
    required this.value,
    this.valueStyle,
  });

  final String label;
  final String value;
  final TextStyle? valueStyle;

  @override
  Widget build(BuildContext context) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          label,
          style: GoogleFonts.instrumentSans(
            fontSize: 13.5
          ),
        ),
        Text(
          value,
          style: valueStyle ??
              GoogleFonts.instrumentSans(
                fontSize: 13.5,
                fontWeight: FontWeight.w600
              ),
        ),
      ],
    );
  }
}