import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/invoice.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

class ReceivePaymentScreen extends ConsumerStatefulWidget {
  const ReceivePaymentScreen({super.key, required this.customerId});

  final String customerId;

  @override
  ConsumerState<ReceivePaymentScreen> createState() =>
      _ReceivePaymentScreenState();
}

class _ReceivePaymentScreenState
    extends ConsumerState<ReceivePaymentScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();
  final _transactionCtrl = TextEditingController();

  static const _methods = ['Cash', 'Card', 'bKash', 'Bank'];
  static const _methodCodes = ['CASH', 'CARD', 'BKASH', 'BANK_TRANSFER'];
  int _methodIndex = 0;
  final bool _isAdvance = false;
  bool _submitting = false;

  Invoice? _selectedInvoice;
  bool _invoiceError = false;
  final List<Invoice> _loadedInvoices = [];
  bool _loadingInvoices = false;
  bool _invoicesLoaded = false;

  @override
  void initState() {
    super.initState();
    _loadInvoices();
  }

  @override
  void dispose() {
    _amountCtrl.dispose();
    _notesCtrl.dispose();
    _transactionCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadInvoices() async {
    if (_invoicesLoaded || _loadingInvoices) return;
    setState(() => _loadingInvoices = true);
    try {
      final page = await ref.read(customersRepositoryProvider).invoicesPage(
            customerId: widget.customerId,
            pageSize: 50,
          );
      if (!mounted) return;
      setState(() {
        _loadedInvoices
            .addAll(page.items.where((i) => i.outstandingAmount > 0));
        _invoicesLoaded = true;
        _loadingInvoices = false;
      });
    } catch (_) {
      if (mounted) {
        setState(() {
          _invoicesLoaded = true;
          _loadingInvoices = false;
        });
      }
    }
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (!_isAdvance && _selectedInvoice == null) {
      setState(() => _invoiceError = true);
      return;
    }
    FocusScope.of(context).unfocus();
    setState(() => _submitting = true);
    final messenger = ScaffoldMessenger.of(context);
    final nav = Navigator.of(context);
    try {
      await ref.read(customersRepositoryProvider).recordPayment(
            customerId: widget.customerId,
            amount: double.parse(
                _amountCtrl.text.trim().replaceAll(',', '')),
            paymentMethod: _methodCodes[_methodIndex],
            transactionNumber: _transactionCtrl.text.trim().isEmpty
                ? null
                : _transactionCtrl.text.trim(),
            notes: _notesCtrl.text.trim().isEmpty
                ? null
                : _notesCtrl.text.trim(),
            invoiceId: _isAdvance ? null : _selectedInvoice?.id,
            isAdvance: _isAdvance,
          );
      ref.invalidate(customerDetailProvider(widget.customerId));
      ref.invalidate(
          customerPaymentSummaryProvider(widget.customerId));
      if (!mounted) return;
      nav.pop();
      messenger.showSnackBar(
        const SnackBar(
            content: Text('Payment recorded successfully')),
      );
    } on AppException catch (e) {
      if (!mounted) return;
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: AppColors.red,
      ));
    } finally {
      if (mounted) setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final customerAsync =
        ref.watch(customerDetailProvider(widget.customerId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Receive Payment',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
      ),
      body: customerAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ErrorView(
          message: e is AppException ? e.message : 'Failed to load.',
          onRetry: () =>
              ref.invalidate(customerDetailProvider(widget.customerId)),
        ),
        data: (customer) => Stack(
          children: [
            Form(
              key: _formKey,
              child: ListView(
                padding:
                    const EdgeInsets.fromLTRB(16, 4, 16, 110),
                children: [
                  // Customer card
                  CardSection(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 12),
                    child: Row(
                      children: [
                        InitialsAvatar(
                            name: customer.fullName, radius: 18),
                        const SizedBox(width: 12),
                        Expanded(
                          child: Column(
                            crossAxisAlignment:
                                CrossAxisAlignment.start,
                            children: [
                              Text(
                                customer.fullName,
                                style: GoogleFonts.instrumentSans(
                                  fontSize: 13.5,
                                  fontWeight: FontWeight.w600
                                ),
                              ),
                              if (customer.hasDue)
                                Text(
                                  'Total due ${formatCurrency(customer.dueAmount)}',
                                  style: GoogleFonts.instrumentSans(
                                    fontSize: 11.5,
                                    fontWeight: FontWeight.w600,
                                    color: AppColors.red,
                                  ),
                                ),
                            ],
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  // Amount
                  Text(
                    'Amount received',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  TextFormField(
                    controller: _amountCtrl,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
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
                      if (customer.hasDue)
                        _QuickChip(
                          label:
                              'Full · ${formatCurrency(customer.dueAmount)}',
                          onTap: () => _amountCtrl.text =
                              customer.dueAmount
                                  .toStringAsFixed(2),
                        ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // Payment method
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

                  // Invoice allocation
                  Text(
                    'Apply to invoices',
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13,
                      fontWeight: FontWeight.w600
                    ),
                  ),
                  const SizedBox(height: 8),
                  CardSection(
                    child: _loadingInvoices
                        ? const Padding(
                            padding: EdgeInsets.symmetric(
                                vertical: 12),
                            child: Center(
                                child:
                                    CircularProgressIndicator()),
                          )
                        : _loadedInvoices.isEmpty
                            ? Padding(
                                padding: const EdgeInsets.symmetric(
                                    vertical: 12),
                                child: Text(
                                  'No open invoices',
                                  style: GoogleFonts.instrumentSans(
                                      fontSize: 13,
                                      color: AppColors.muted),
                                ),
                              )
                            : Column(
                                children: _loadedInvoices
                                    .map(
                                      (inv) => BillCheckRow(
                                        title: inv.invoiceNumber,
                                        sub: formatDate(
                                            inv.invoiceDate),
                                        amount: formatCurrency(
                                            inv.outstandingAmount),
                                        checked:
                                            _selectedInvoice
                                                    ?.id ==
                                                inv.id,
                                        onToggle: () => setState(
                                          () {
                                            _selectedInvoice =
                                                _selectedInvoice
                                                            ?.id ==
                                                        inv.id
                                                    ? null
                                                    : inv;
                                            _invoiceError =
                                                false;
                                          },
                                        ),
                                      ),
                                    )
                                    .toList(),
                              ),
                  ),
                  if (_invoiceError) ...[
                    const SizedBox(height: 8),
                    Text(
                      'Select an invoice',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12, color: AppColors.red),
                    ),
                  ],
                  const SizedBox(height: 16),

                  // Notes
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

            // Sticky CTA
            Positioned(
              bottom: 0,
              left: 0,
              right: 0,
              child: PrimaryCtaBar(
                label: 'Confirm payment',
                onTap: _submit,
                isLoading: _submitting,
                backgroundColor: AppColors.green,
                shadowColor: const Color(0x400D8A53),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

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
