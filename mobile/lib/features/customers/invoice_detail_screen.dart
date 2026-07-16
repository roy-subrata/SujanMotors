import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/invoice.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

final _invoiceLinesProvider =
    FutureProvider.autoDispose.family<List<InvoiceLine>, String>(
  (ref, invoiceId) =>
      ref.read(customersRepositoryProvider).invoiceLines(invoiceId),
);

class InvoiceDetailScreen extends ConsumerWidget {
  const InvoiceDetailScreen({
    super.key,
    required this.invoiceId,
    this.invoice,
  });

  final String invoiceId;
  final Invoice? invoice;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final linesAsync = ref.watch(_invoiceLinesProvider(invoiceId));
    final inv = invoice;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          inv?.invoiceNumber ?? 'Invoice',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
      ),
      body: linesAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ListView(children: [
          const SizedBox(height: 120),
          ErrorView(
            message:
                e is AppException ? e.message : 'Failed to load invoice.',
            onRetry: () =>
                ref.invalidate(_invoiceLinesProvider(invoiceId)),
          ),
        ]),
        data: (lines) => _InvoiceBody(invoice: inv, lines: lines),
      ),
    );
  }
}

// ── Body ──────────────────────────────────────────────────────────────────────

class _InvoiceBody extends StatelessWidget {
  const _InvoiceBody({required this.invoice, required this.lines});

  final Invoice? invoice;
  final List<InvoiceLine> lines;

  @override
  Widget build(BuildContext context) {
    final inv = invoice;
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
      children: [
        // Header card
        if (inv != null) ...[
          CardSection(
            padding: const EdgeInsets.all(15),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            inv.invoiceNumber,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 15,
                              fontWeight: FontWeight.w700
                            ),
                          ),
                          const SizedBox(height: 3),
                          Text(
                            'SO: ${inv.salesOrderNumber}',
                            style: GoogleFonts.instrumentSans(
                              fontSize: 12
                            ),
                          ),
                        ],
                      ),
                    ),
                    StatusPill(label: inv.status ?? 'ISSUED'),
                  ],
                ),
                const SizedBox(height: 10),
                Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                const SizedBox(height: 10),
                Row(
                  children: [
                    _MetaItem(
                      label: 'Invoice date',
                      value: formatDate(inv.invoiceDate),
                    ),
                    const SizedBox(width: 24),
                    if (inv.dueDate != null)
                      _MetaItem(
                        label: 'Due date',
                        value: formatDate(inv.dueDate!),
                        valueColor: inv.isOverdue ? context.colors.red : null,
                      ),
                  ],
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),
        ],

        // Items
        Text(
          'Items',
          style: GoogleFonts.instrumentSans(
            fontSize: 13,
            fontWeight: FontWeight.w600
          ),
        ),
        const SizedBox(height: 8),
        lines.isEmpty
            ? const EmptyView(
                message: 'No items found.',
                icon: Icons.receipt_long_outlined)
            : CardSection(
                padding: EdgeInsets.zero,
                child: Column(
                  children: lines.asMap().entries.map((e) {
                    final isLast = e.key == lines.length - 1;
                    return Column(
                      children: [
                        _LineItemRow(line: e.value),
                        if (!isLast)
                          Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                      ],
                    );
                  }).toList(),
                ),
              ),

        // Totals
        if (inv != null) ...[
          const SizedBox(height: 16),
          CardSection(
            child: Column(
              children: [
                _TotalRow(
                  label: 'Grand Total',
                  value: formatCurrency(inv.grandTotal),
                ),
                const SizedBox(height: 8),
                _TotalRow(
                  label: 'Amount Paid',
                  value: formatCurrency(inv.amountPaid),
                  valueColor: context.colors.green,
                ),
                if (inv.outstandingAmount > 0) ...[
                  Padding(
                    padding: EdgeInsets.symmetric(vertical: 10),
                    child: Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  ),
                  _TotalRow(
                    label: 'Outstanding',
                    value: formatCurrency(inv.outstandingAmount),
                    valueStyle: GoogleFonts.instrumentSans(
                      fontSize: 19,
                      fontWeight: FontWeight.w700,
                      color: context.colors.red,
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(height: 16),
          SizedBox(
            width: double.infinity,
            child: OutlinedButton.icon(
              onPressed: () =>
                  context.push('/sales/return', extra: inv),
              icon: const Icon(Icons.assignment_return_outlined,
                  size: 16),
              label: const Text('Initiate return'),
              style: OutlinedButton.styleFrom(
                foregroundColor: context.colors.red,
                side: BorderSide(color: context.colors.redBorder),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12)),
                padding: const EdgeInsets.symmetric(vertical: 13),
                textStyle: GoogleFonts.instrumentSans(
                    fontSize: 13.5, fontWeight: FontWeight.w600),
              ),
            ),
          ),
        ],
      ],
    );
  }
}

// ── Meta item ─────────────────────────────────────────────────────────────────

class _MetaItem extends StatelessWidget {
  const _MetaItem({
    required this.label,
    required this.value,
    this.valueColor,
  });

  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          label,
          style: GoogleFonts.instrumentSans(
            fontSize: 10.5
          ),
        ),
        const SizedBox(height: 2),
        Text(
          value,
          style: GoogleFonts.instrumentSans(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: valueColor ?? context.colors.ink,
          ),
        ),
      ],
    );
  }
}

// ── Line item row ─────────────────────────────────────────────────────────────

class _LineItemRow extends StatelessWidget {
  const _LineItemRow({required this.line});

  final InvoiceLine line;

  @override
  Widget build(BuildContext context) {
    final unit = line.unitSymbol ?? '';
    return Padding(
      padding: const EdgeInsets.fromLTRB(14, 11, 14, 11),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
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
                if (line.partSku != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    line.partSku!,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${line.quantity}${unit.isNotEmpty ? ' $unit' : ''} × ${formatCurrency(line.unitPrice)}',
                style: GoogleFonts.instrumentSans(
                  fontSize: 11
                ),
              ),
              const SizedBox(height: 3),
              Text(
                formatCurrency(line.lineTotal),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w700
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Total row ─────────────────────────────────────────────────────────────────

class _TotalRow extends StatelessWidget {
  const _TotalRow({
    required this.label,
    required this.value,
    this.valueColor,
    this.valueStyle,
  });

  final String label;
  final String value;
  final Color? valueColor;
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
                fontWeight: FontWeight.w600,
                color: valueColor ?? context.colors.ink,
              ),
        ),
      ],
    );
  }
}