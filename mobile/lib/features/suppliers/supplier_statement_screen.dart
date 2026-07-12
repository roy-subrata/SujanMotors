import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/state_views.dart';
import 'supplier_payment_screen.dart';

enum _DateFilter { thisMonth, last3Months, thisYear, all }

class SupplierStatementScreen extends ConsumerStatefulWidget {
  const SupplierStatementScreen({super.key, required this.supplierId});

  final String supplierId;

  @override
  ConsumerState<SupplierStatementScreen> createState() =>
      _SupplierStatementScreenState();
}

class _SupplierStatementScreenState
    extends ConsumerState<SupplierStatementScreen> {
  _DateFilter _filter = _DateFilter.thisMonth;

  String _filterLabel(_DateFilter f) => switch (f) {
        _DateFilter.thisMonth => 'This Month',
        _DateFilter.last3Months => 'Last 3 Months',
        _DateFilter.thisYear => 'This Year',
        _DateFilter.all => 'All Time',
      };

  void _showPeriodPicker() {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: _DateFilter.values
              .map((f) => ListTile(
                    title: Text(_filterLabel(f)),
                    trailing: _filter == f
                        ? const Icon(Icons.check, color: AppColors.ink)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() => _filter = f);
                    },
                  ))
              .toList(),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final supplierAsync =
        ref.watch(supplierDetailProvider(widget.supplierId));
    final billsAsync = ref.watch(supplierBillsProvider(widget.supplierId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Statement',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
        actions: [
          GestureDetector(
            onTap: _showPeriodPicker,
            child: Container(
              margin: const EdgeInsets.only(right: 8),
              padding:
                  const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(9),
                border: Border.all(color: Theme.of(context).colorScheme.outline),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    _filterLabel(_filter),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 12,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(width: 4),
                  const Icon(Icons.expand_more_rounded,
                      size: 14, color: AppColors.secondary),
                ],
              ),
            ),
          ),
        ],
      ),
      body: supplierAsync.when(
        loading: () => const LoadingView(),
        error: (_, _) =>
            const ErrorView(message: 'Failed to load supplier.'),
        data: (supplier) => Column(
          children: [
            _SummaryBar(supplier: supplier),
            Expanded(
              child: billsAsync.when(
                loading: () => const LoadingView(),
                error: (_, _) =>
                    const ErrorView(message: 'Failed to load transactions.'),
                data: (bills) => bills.isEmpty
                    ? const EmptyView(
                        message: 'No transactions in this period.',
                        icon: Icons.receipt_long_outlined,
                      )
                    : _BillList(bills: bills),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: _buildBottomBar(),
    );
  }

  Widget _buildBottomBar() {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border(top: BorderSide(color: Theme.of(context).colorScheme.outline)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          child: Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.print_outlined, size: 16),
                  label: const Text('Print'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.ink,
                    side: BorderSide(color: Theme.of(context).colorScheme.outline),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(11)),
                    padding: const EdgeInsets.symmetric(vertical: 13),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, fontWeight: FontWeight.w600),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                flex: 2,
                child: FilledButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.upload_outlined, size: 16),
                  label: const Text('Generate PDF & share'),
                  style: FilledButton.styleFrom(
                    backgroundColor: AppColors.ink,
                    foregroundColor: Colors.white,
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(11)),
                    padding: const EdgeInsets.symmetric(vertical: 13),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, fontWeight: FontWeight.w600),
                  ),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// â”€â”€ Summary bar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.supplier});

  final SupplierSummary supplier;

  @override
  Widget build(BuildContext context) {
    final owes = supplier.amountOwed > 0;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 10),
      color: Theme.of(context).colorScheme.surface,
      child: Row(
        children: [
          Expanded(
            child: Text(
              '${supplier.name} Â· ${supplier.phone}',
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                color: owes ? AppColors.amber : AppColors.muted,
              ),
            ),
          ),
          if (owes) ...[
            Text(
              'We owe ${formatCurrency(supplier.amountOwed)}',
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.amber,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// â”€â”€ Bill list â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

class _BillList extends StatelessWidget {
  const _BillList({required this.bills});

  final List<SupplierBill> bills;

  @override
  Widget build(BuildContext context) {
    return ListView.builder(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
      itemCount: bills.length,
      itemBuilder: (ctx, i) => _BillTile(bill: bills[i]),
    );
  }
}

class _BillTile extends StatelessWidget {
  const _BillTile({required this.bill});

  final SupplierBill bill;

  @override
  Widget build(BuildContext context) {
    final isPaid = bill.outstandingAmount <= 0;
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  bill.billNumber,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  formatDate(bill.billDate),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 11.5
                  ),
                ),
              ],
            ),
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                formatCurrency(bill.outstandingAmount),
                style: GoogleFonts.instrumentSans(
                  fontSize: 13.5,
                  fontWeight: FontWeight.w700,
                  color: isPaid ? AppColors.green : AppColors.amber,
                ),
              ),
              const SizedBox(height: 2),
              Text(
                isPaid ? 'Paid' : 'Outstanding',
                style: GoogleFonts.instrumentSans(
                  fontSize: 10.5
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
