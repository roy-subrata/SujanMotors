import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'suppliers_repository.dart';

/// Pay Supplier — amount + method grid + reference, applied against open
/// purchase bills oldest-first (editable via checkboxes). A payment without a
/// bill posts as an ADVANCE; each checked bill posts one REGULAR payment.
class SupplierPaymentScreen extends ConsumerStatefulWidget {
  const SupplierPaymentScreen({super.key, required this.supplierId});

  final String supplierId;

  @override
  ConsumerState<SupplierPaymentScreen> createState() =>
      _SupplierPaymentScreenState();
}

class _SupplierPaymentScreenState
    extends ConsumerState<SupplierPaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  final _refCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  static const _methods = ['Cash', 'Bank', 'Cheque', 'bKash'];

  /// Method label → backend payment-provider type.
  static const _providerTypeByMethod = {
    'Cash': 'CASH',
    'Bank': 'BANK_TRANSFER',
    'Cheque': 'CHECK',
    'bKash': 'MOBILE_BANKING',
  };

  int _methodIndex = 0;
  bool _submitting = false;
  final Set<String> _selectedBillIds = {};

  @override
  void initState() {
    super.initState();
    _amountCtrl.addListener(() => setState(() {}));
  }

  @override
  void dispose() {
    _amountCtrl.dispose();
    _refCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  double get _amount =>
      double.tryParse(_amountCtrl.text.trim().replaceAll(',', '')) ?? 0;

  /// Distributes the entered amount across the checked bills, oldest first.
  /// Returns billId → applied amount (only allocations > 0).
  Map<String, double> _allocate(List<SupplierBill> bills) {
    var remaining = _amount;
    final allocations = <String, double>{};
    for (final bill in bills) {
      if (!_selectedBillIds.contains(bill.id)) continue;
      if (remaining <= 0) break;
      final applied = remaining >= bill.outstandingAmount
          ? bill.outstandingAmount
          : remaining;
      allocations[bill.id] = applied;
      remaining -= applied;
    }
    return allocations;
  }

  Future<void> _submit(Supplier supplier, List<SupplierBill> bills) async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();

    final messenger = ScaffoldMessenger.of(context);
    final navigator = Navigator.of(context);
    setState(() => _submitting = true);

    try {
      final repo = ref.read(suppliersRepositoryProvider);

      // Resolve the payment provider matching the chosen method.
      final method = _methods[_methodIndex];
      final wantedType = _providerTypeByMethod[method]!;
      final providers = await repo.activeProviders();
      final provider = providers
              .where((p) => p.type.toUpperCase() == wantedType)
              .firstOrNull ??
          providers.firstOrNull;
      if (provider == null) {
        throw AppException(
            'No active payment provider is configured. Add one on the web app first.');
      }

      final allocations = _allocate(bills);
      final allocatedTotal =
          allocations.values.fold<double>(0, (s, v) => s + v);
      final advanceAmount = _amount - allocatedTotal;

      // One REGULAR payment per checked bill (the API links each payment to a
      // single purchase order).
      for (final bill in bills) {
        final applied = allocations[bill.id];
        if (applied == null || applied <= 0) continue;
        await repo.createPayment(
          supplierId: widget.supplierId,
          purchaseOrderId: bill.id,
          paymentProviderId: provider.id,
          amount: applied,
          paymentMethod: method.toUpperCase(),
          referenceNumber: _refCtrl.text.trim(),
          notes: _notesCtrl.text.trim(),
        );
      }

      // Anything left over (or a payment with no bill selected) posts as an
      // advance so the credit stays on the supplier's account.
      if (advanceAmount > 0.009) {
        await repo.createPayment(
          supplierId: widget.supplierId,
          paymentProviderId: provider.id,
          amount: advanceAmount,
          paymentMethod: method.toUpperCase(),
          referenceNumber: _refCtrl.text.trim(),
          notes: _notesCtrl.text.trim(),
        );
      }

      ref.invalidate(supplierDetailProvider(widget.supplierId));
      ref.invalidate(supplierBillsProvider(widget.supplierId));
      ref.invalidate(supplierLedgerSummaryProvider(widget.supplierId));

      if (!mounted) return;
      navigator.pop();
      messenger.showSnackBar(SnackBar(
        content: Text('Payment of ${formatCurrency(_amount)} recorded'),
        behavior: SnackBarBehavior.floating,
      ));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: AppColors.red,
        behavior: SnackBarBehavior.floating,
      ));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final supplierAsync = ref.watch(supplierDetailProvider(widget.supplierId));
    final billsAsync = ref.watch(supplierBillsProvider(widget.supplierId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Pay Supplier',
          style: GoogleFonts.instrumentSans(
              fontSize: 16, fontWeight: FontWeight.w700),
        ),
      ),
      body: supplierAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ErrorView(
          message:
              e is AppException ? e.message : 'Failed to load supplier.',
          onRetry: () =>
              ref.invalidate(supplierDetailProvider(widget.supplierId)),
        ),
        data: (supplier) {
          final bills = billsAsync.value ?? const <SupplierBill>[];
          return Stack(
            children: [
              Form(
                key: _formKey,
                child: ListView(
                  padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
                  children: [
                    // ── Supplier card ──────────────────────────────
                    CardSection(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 14, vertical: 12),
                      child: Row(
                        children: [
                          InitialsAvatar(name: supplier.name, radius: 18),
                          const SizedBox(width: 12),
                          Expanded(
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.start,
                              children: [
                                Text(
                                  supplier.name,
                                  style: GoogleFonts.instrumentSans(
                                      fontSize: 13.5,
                                      fontWeight: FontWeight.w600),
                                ),
                                if (supplier.hasPayable)
                                  Text(
                                    'We owe ${formatCurrency(supplier.currentBalance)}',
                                    style: GoogleFonts.instrumentSans(
                                      fontSize: 11.5,
                                      fontWeight: FontWeight.w600,
                                      color: AppColors.amber,
                                    ),
                                  )
                                else
                                  Text(
                                    supplier.phone ?? supplier.code,
                                    style: GoogleFonts.instrumentSans(
                                        fontSize: 11.5,
                                        color: AppColors.muted),
                                  ),
                              ],
                            ),
                          ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 16),

                    // ── Amount ────────────────────────────────────
                    Text(
                      'Amount to pay',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    TextFormField(
                      controller: _amountCtrl,
                      keyboardType: const TextInputType.numberWithOptions(
                          decimal: true),
                      style: GoogleFonts.instrumentSans(
                          fontSize: 19, fontWeight: FontWeight.w700),
                      decoration: const InputDecoration(prefixText: '৳ '),
                      validator: (v) {
                        if (v == null || v.isEmpty) return 'Enter an amount';
                        final n =
                            double.tryParse(v.trim().replaceAll(',', ''));
                        if (n == null || n <= 0) {
                          return 'Enter a valid amount';
                        }
                        return null;
                      },
                    ),
                    const SizedBox(height: 8),
                    if (supplier.hasPayable)
                      Wrap(
                        spacing: 8,
                        children: [
                          _QuickChip(
                              label: '৳ 5,000',
                              onTap: () => _amountCtrl.text = '5000'),
                          _QuickChip(
                              label: '৳ 10,000',
                              onTap: () => _amountCtrl.text = '10000'),
                          _QuickChip(
                            label:
                                'Full · ${formatCurrency(supplier.currentBalance)}',
                            onTap: () {
                              _amountCtrl.text = supplier.currentBalance
                                  .toStringAsFixed(2);
                              setState(() => _selectedBillIds
                                  .addAll(bills.map((b) => b.id)));
                            },
                          ),
                        ],
                      ),
                    const SizedBox(height: 16),

                    // ── Payment method ────────────────────────────
                    Text(
                      'Method',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    MethodGrid(
                      methods: _methods,
                      selected: _methodIndex,
                      onSelect: (i) => setState(() => _methodIndex = i),
                    ),
                    const SizedBox(height: 16),

                    // ── Reference ─────────────────────────────────
                    Text(
                      'Reference',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    TextField(
                      controller: _refCtrl,
                      decoration: const InputDecoration(
                        hintText: 'Bank / cheque / TRX no. (optional)',
                      ),
                    ),
                    const SizedBox(height: 16),

                    // ── Against purchase bills ────────────────────
                    Text(
                      'Against purchase bills',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13, fontWeight: FontWeight.w600),
                    ),
                    const SizedBox(height: 8),
                    _BillsCard(
                      billsAsync: billsAsync,
                      selectedIds: _selectedBillIds,
                      allocations: _allocate(bills),
                      remainingPayable: (supplier.currentBalance - _amount)
                          .clamp(0, double.infinity)
                          .toDouble(),
                      onToggle: (id) => setState(() {
                        if (!_selectedBillIds.remove(id)) {
                          _selectedBillIds.add(id);
                        }
                      }),
                    ),
                    const SizedBox(height: 16),

                    // ── Notes ─────────────────────────────────────
                    TextField(
                      controller: _notesCtrl,
                      maxLines: 2,
                      decoration:
                          const InputDecoration(hintText: 'Note (optional)'),
                    ),
                  ],
                ),
              ),

              // ── Sticky CTA ─────────────────────────────────────
              Positioned(
                bottom: 0,
                left: 0,
                right: 0,
                child: PrimaryCtaBar(
                  label: _amount > 0
                      ? 'Confirm payment · ${formatCurrency(_amount)}'
                      : 'Confirm payment',
                  onTap: () => _submit(supplier, bills),
                  isLoading: _submitting,
                  shadowColor: const Color(0x400F172A),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}

// ── Bills card ────────────────────────────────────────────────────────────────

class _BillsCard extends StatelessWidget {
  const _BillsCard({
    required this.billsAsync,
    required this.selectedIds,
    required this.allocations,
    required this.remainingPayable,
    required this.onToggle,
  });

  final AsyncValue<List<SupplierBill>> billsAsync;
  final Set<String> selectedIds;
  final Map<String, double> allocations;
  final double remainingPayable;
  final ValueChanged<String> onToggle;

  @override
  Widget build(BuildContext context) {
    return CardSection(
      padding: EdgeInsets.zero,
      child: billsAsync.when(
        loading: () => const Padding(
          padding: EdgeInsets.symmetric(vertical: 16),
          child: Center(child: CircularProgressIndicator()),
        ),
        error: (_, _) => Padding(
          padding: const EdgeInsets.all(14),
          child: Text(
            'Could not load purchase bills',
            style: GoogleFonts.instrumentSans(
                fontSize: 13, color: AppColors.muted),
          ),
        ),
        data: (bills) => bills.isEmpty
            ? Padding(
                padding: const EdgeInsets.all(14),
                child: Text(
                  'No open purchase bills — the payment will be saved as an advance.',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 12.5, color: AppColors.muted),
                ),
              )
            : Column(
                children: [
                  for (final bill in bills)
                    Padding(
                      padding:
                          const EdgeInsets.symmetric(horizontal: 14),
                      child: BillCheckRow(
                        title: bill.billNumber,
                        sub:
                            '${formatDate(bill.billDate)} · due ${formatCurrency(bill.outstandingAmount)}',
                        amount: allocations.containsKey(bill.id)
                            ? formatCurrency(allocations[bill.id]!)
                            : '—',
                        checked: selectedIds.contains(bill.id),
                        onToggle: () => onToggle(bill.id),
                      ),
                    ),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 11),
                    decoration: BoxDecoration(
                      color: Theme.of(context).scaffoldBackgroundColor,
                      borderRadius: const BorderRadius.vertical(
                          bottom: Radius.circular(13)),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Text(
                            'Remaining payable',
                            style: GoogleFonts.instrumentSans(
                                fontSize: 12.5,
                                color: AppColors.secondary),
                          ),
                        ),
                        Text(
                          formatCurrency(remainingPayable),
                          style: GoogleFonts.instrumentSans(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w700,
                            color: remainingPayable > 0
                                ? AppColors.amber
                                : AppColors.green,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}

// ── Helpers ───────────────────────────────────────────────────────────────────

class _QuickChip extends StatelessWidget {
  const _QuickChip({required this.label, required this.onTap});

  final String label;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(99),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        child: Text(
          label,
          style: GoogleFonts.instrumentSans(
              fontSize: 12, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}
