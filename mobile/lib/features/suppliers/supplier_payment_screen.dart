import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/design_system.dart';

// ── Stub models (replace with real API types when backend supplier API ships) ─

class SupplierSummary {
  const SupplierSummary({
    required this.id,
    required this.name,
    required this.phone,
    required this.amountOwed,
  });

  final String id;
  final String name;
  final String phone;
  final double amountOwed;
}

class SupplierBill {
  const SupplierBill({
    required this.id,
    required this.billNumber,
    required this.billDate,
    required this.outstandingAmount,
  });

  final String id;
  final String billNumber;
  final DateTime billDate;
  final double outstandingAmount;
}

// ── Stub provider (returns hard-coded data until backend exists) ───────────────

final supplierDetailProvider = FutureProvider.family<SupplierSummary, String>(
  (ref, id) async {
    await Future<void>.delayed(const Duration(milliseconds: 200));
    return SupplierSummary(
      id: id,
      name: 'Supplier $id',
      phone: '+880 1700-000000',
      amountOwed: 0,
    );
  },
);

final supplierBillsProvider =
    FutureProvider.family<List<SupplierBill>, String>(
  (ref, supplierId) async {
    await Future<void>.delayed(const Duration(milliseconds: 200));
    return const [];
  },
);

// ── Screen ────────────────────────────────────────────────────────────────────

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
  int _methodIndex = 0;
  bool _submitting = false;
  final Set<String> _selectedBillIds = {};

  @override
  void dispose() {
    _amountCtrl.dispose();
    _refCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    FocusScope.of(context).unfocus();
    setState(() => _submitting = true);
    await Future<void>.delayed(const Duration(milliseconds: 600));
    if (!mounted) return;
    setState(() => _submitting = false);
    Navigator.of(context).pop();
    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Payment recorded successfully')),
    );
  }

  @override
  Widget build(BuildContext context) {
    final supplierAsync =
        ref.watch(supplierDetailProvider(widget.supplierId));
    final billsAsync =
        ref.watch(supplierBillsProvider(widget.supplierId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Pay Supplier',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
      ),
      body: supplierAsync.when(
        loading: () =>
            const Center(child: CircularProgressIndicator()),
        error: (_, _) => const Center(
          child: Text('Failed to load supplier'),
        ),
        data: (supplier) => Stack(
          children: [
            Form(
              key: _formKey,
              child: ListView(
                padding:
                    const EdgeInsets.fromLTRB(16, 4, 16, 110),
                children: [
                  // ── Supplier card ──────────────────────────────
                  CardSection(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 12),
                    child: Row(
                      children: [
                        InitialsAvatar(
                            name: supplier.name, radius: 18),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment:
                                CrossAxisAlignment.start,
                            children: [
                              Text(
                                supplier.name,
                                style: GoogleFonts.instrumentSans(
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w600
                                ),
                              ),
                              if (supplier.amountOwed > 0)
                                Text(
                                  'We owe ${formatCurrency(supplier.amountOwed)}',
                                  style: GoogleFonts.instrumentSans(
                                    fontSize: 11.5,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.amber,
                                  ),
                                )
                              else
                                Text(
                                  supplier.phone,
                                  style: GoogleFonts.instrumentSans(
                                    fontSize: 11.5
                                  ),
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
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextFormField(
                    controller: _amountCtrl,
                    keyboardType: const TextInputType.numberWithOptions(
                        decimal: true),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 19,
                      fontWeight: FontWeight.w700
                    ),
                    decoration: const InputDecoration(
                      prefixText: '৳ ',
                    ),
                    validator: (v) {
                      if (v == null || v.isEmpty) {
                        return 'Enter an amount';
                      }
                      final n = double.tryParse(
                          v.trim().replaceAll(',', ''));
                      if (n == null || n <= 0) {
                        return 'Enter a valid amount';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 8),
                  // Quick chips
                  if (supplier.amountOwed > 0)
                    Wrap(
                      spacing: 8,
                      children: [
                        _QuickChip(
                            label: '৳ 5,000',
                            onTap: () =>
                                _amountCtrl.text = '5000'),
                        _QuickChip(
                            label: '৳ 10,000',
                            onTap: () =>
                                _amountCtrl.text = '10000'),
                        _QuickChip(
                          label:
                              'Full · ${formatCurrency(supplier.amountOwed)}',
                          onTap: () => _amountCtrl.text =
                              supplier.amountOwed
                                  .toStringAsFixed(2),
                        ),
                      ],
                    ),
                  const SizedBox(height: 16),

                  // ── Payment method ────────────────────────────
                  Text(
                    'Payment method',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  MethodGrid(
                    methods: _methods,
                    selected: _methodIndex,
                    onSelect: (i) =>
                        setState(() => _methodIndex = i),
                  ),
                  const SizedBox(height: 16),

                  // ── Reference / cheque number ─────────────────
                  Text(
                    'Reference / cheque no.',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextField(
                    controller: _refCtrl,
                    decoration: const InputDecoration(
                      hintText: 'Optional',
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Against purchase bills ────────────────────
                  Text(
                    'Against purchase bills',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  CardSection(
                    child: billsAsync.when(
                      loading: () => const Padding(
                        padding: EdgeInsets.symmetric(
                            vertical: 12),
                        child: Center(
                            child: CircularProgressIndicator()),
                      ),
                      error: (_, _) => Padding(
                        padding: const EdgeInsets.symmetric(
                            vertical: 12),
                        child: Text(
                          'Could not load bills',
                          style: GoogleFonts.instrumentSans(
                              fontSize: 13,
                              color: AppColors.muted),
                        ),
                      ),
                      data: (bills) => bills.isEmpty
                          ? Padding(
                              padding:
                                  const EdgeInsets.symmetric(
                                      vertical: 12),
                              child: Text(
                                'No open purchase bills',
                                style:
                                    GoogleFonts.instrumentSans(
                                        fontSize: 13,
                                        color: AppColors.muted),
                              ),
                            )
                          : Column(
                              children: bills.map((bill) {
                                final isChecked =
                                    _selectedBillIds
                                        .contains(bill.id);
                                return BillCheckRow(
                                  title: bill.billNumber,
                                  sub: formatDate(bill.billDate),
                                  amount: formatCurrency(
                                      bill.outstandingAmount),
                                  checked: isChecked,
                                  onToggle: () => setState(() {
                                    if (isChecked) {
                                      _selectedBillIds
                                          .remove(bill.id);
                                    } else {
                                      _selectedBillIds
                                          .add(bill.id);
                                    }
                                  }),
                                );
                              }).toList(),
                            ),
                    ),
                  ),
                  const SizedBox(height: 16),

                  // ── Notes ─────────────────────────────────────
                  TextField(
                    controller: _notesCtrl,
                    maxLines: 2,
                    decoration: const InputDecoration(
                      hintText: 'Notes (optional)',
                    ),
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
                label: 'Confirm payment',
                onTap: _submit,
                isLoading: _submitting,
                shadowColor: const Color(0x400F172A),
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
        padding:
            const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(99),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        child: Text(
          label,
          style: GoogleFonts.instrumentSans(
            fontSize: 12,
            fontWeight: FontWeight.w500
          ),
        ),
      ),
    );
  }
}
