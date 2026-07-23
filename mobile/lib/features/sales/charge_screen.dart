import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../features/customers/customers_repository.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/models/customer_vehicle.dart';
import 'quick_sale_providers.dart';

class ChargeScreen extends ConsumerStatefulWidget {
  const ChargeScreen({super.key, required this.cartTotal});

  final double cartTotal;

  @override
  ConsumerState<ChargeScreen> createState() => _ChargeScreenState();
}

class _ChargeScreenState extends ConsumerState<ChargeScreen> {
  final _discountCtrl = TextEditingController(text: '0');
  final _paidNowCtrl = TextEditingController();
  final _referenceCtrl = TextEditingController();

  // Customer list
  List<Customer> _customers = [];
  bool _isLoadingCustomers = false;

  // Selection — null means Walk-in
  Customer? _customer;
  List<CustomerVehicle> _vehicles = [];
  bool _isLoadingVehicles = false;
  CustomerVehicle? _vehicle;

  // Payment methods (label → backend code).
  static const _methods = ['Cash', 'Card', 'bKash', 'Bank', 'Cheque'];
  static const _methodCodes = [
    'CASH',
    'CARD',
    'MOBILE_BANKING',
    'BANK_TRANSFER',
    'CHEQUE',
  ];
  int _methodIndex = 0;
  bool _applyAdvance = false;

  String? _localError;

  double get _discount =>
      (double.tryParse(_discountCtrl.text) ?? 0).clamp(0, widget.cartTotal);
  double get grandTotal =>
      (widget.cartTotal - _discount).clamp(0, double.infinity);

  double get _advanceAvailable => _customer?.advanceAmount ?? 0;
  double get advanceApplied =>
      _applyAdvance ? _advanceAvailable.clamp(0, grandTotal) : 0;

  /// What still has to be covered by cash/card/due after advance credit.
  double get coverable => (grandTotal - advanceApplied).clamp(0, grandTotal);
  double get paidNow => double.tryParse(_paidNowCtrl.text) ?? 0;
  double get due => (coverable - paidNow).clamp(0, coverable);
  double get change => (paidNow - coverable).clamp(0, double.infinity);
  bool get _isCash => _methodIndex == 0;

  @override
  void initState() {
    super.initState();
    _paidNowCtrl.text = widget.cartTotal.toStringAsFixed(2);
    _discountCtrl.addListener(_onDiscountChanged);
    _paidNowCtrl.addListener(() => setState(() {}));
    _loadCustomers('');
  }

  @override
  void dispose() {
    _discountCtrl.dispose();
    _paidNowCtrl.dispose();
    _referenceCtrl.dispose();
    super.dispose();
  }

  /// A discount change moves the grand total; assume full payment of the
  /// remaining coverable amount until the user enters a partial amount.
  void _onDiscountChanged() {
    _paidNowCtrl.text = coverable.toStringAsFixed(2);
    setState(() {});
  }

  void _toggleAdvance(bool on) {
    setState(() {
      _applyAdvance = on;
      _localError = null;
    });
    // Re-default the tendered amount to whatever's left after advance credit.
    _paidNowCtrl.text = coverable.toStringAsFixed(2);
  }

  // ── Customer loading ─────────────────────────────────────────────────────────

  Future<void> _loadCustomers(String query) async {
    setState(() => _isLoadingCustomers = true);
    try {
      final page = await ref.read(customersRepositoryProvider).list(
          search: query.trim().isEmpty ? null : query.trim(), pageSize: 300);
      if (mounted) {
        setState(() {
          _customers = page.items;
          _isLoadingCustomers = false;
        });
      }
    } on AppException {
      if (mounted) setState(() => _isLoadingCustomers = false);
    }
  }

  Future<void> _selectCustomer(Customer? c) async {
    setState(() {
      _customer = c;
      _vehicle = null;
      _vehicles = [];
      _isLoadingVehicles = c != null;
      _applyAdvance = false; // reset — advance belongs to a specific customer
      _localError = null;
    });
    _paidNowCtrl.text = coverable.toStringAsFixed(2);
    if (c == null) return;

    try {
      final vs = await ref.read(customersRepositoryProvider).vehicles(c.id);
      if (mounted) {
        setState(() {
          _vehicles = vs;
          _isLoadingVehicles = false;
        });
      }
    } on AppException {
      if (mounted) setState(() => _isLoadingVehicles = false);
    }
  }

  // ── Submit ───────────────────────────────────────────────────────────────────

  void _submit() {
    final s = S.of(context);
    final gt = grandTotal;
    if (gt <= 0) {
      setState(() => _localError = s.nothingToCharge);
      return;
    }
    final isWalkIn = _customer == null ||
        _customer!.customerCode.toUpperCase() == 'WALKIN';
    // A due balance (partial or unpaid) must belong to a registered customer.
    if (due > 0 && isWalkIn) {
      setState(() => _localError = _customer == null
          ? s.walkInMustPayFull
          : s.walkInNoBalance);
      return;
    }
    // Non-cash methods should note a reference (txn / cheque / card no.).
    if (!_isCash && paidNow > 0 && _referenceCtrl.text.trim().isEmpty) {
      setState(() => _localError =
          s.addReferenceFor(s.paymentMethodName(_methods[_methodIndex])));
      return;
    }
    setState(() => _localError = null);
    ref.read(quickSaleControllerProvider.notifier).submit(
          grandTotal: gt,
          paidNow: paidNow,
          paymentMethod: _methodCodes[_methodIndex],
          discountAmount: _discount,
          advanceApplied: advanceApplied,
          paymentReference: _isCash ? '' : _referenceCtrl.text.trim(),
          customerName: _customer?.fullName ?? 'Walk-in',
          customerId: _customer?.id,
          customerPhone: _customer?.phone,
          vehicleId: _vehicle?.id,
        );
  }

  @override
  Widget build(BuildContext context) {
    final isSubmitting = ref.watch(
      quickSaleControllerProvider.select((s) => s.isSubmitting),
    );
    final submitError = ref.watch(
      quickSaleControllerProvider.select((s) => s.submitError),
    );

    ref.listen(
      quickSaleControllerProvider.select((s) => s.result),
      (_, result) {
        if (result != null && mounted) Navigator.of(context).pop();
      },
    );

    return Scaffold(
      appBar: AppBar(title: Text(S.of(context).checkout)),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 40),
        children: [
          _AmountCard(
            cartTotal: widget.cartTotal,
            discountCtrl: _discountCtrl,
            grandTotal: grandTotal,
          ),
          const SizedBox(height: 14),
          _CustomerCard(
            customers: _customers,
            isLoading: _isLoadingCustomers,
            selectedCustomer: _customer,
            vehicles: _vehicles,
            isLoadingVehicles: _isLoadingVehicles,
            selectedVehicle: _vehicle,
            onSelectCustomer: _selectCustomer,
            onSelectVehicle: (v) => setState(() => _vehicle = v),
          ),
          // Advance credit — only when the selected customer has some.
          if (_advanceAvailable > 0) ...[
            const SizedBox(height: 14),
            _AdvanceCard(
              available: _advanceAvailable,
              applied: advanceApplied,
              apply: _applyAdvance,
              onToggle: _toggleAdvance,
            ),
          ],
          const SizedBox(height: 14),
          _PaymentCard(
            methods: _methods,
            methodIndex: _methodIndex,
            isCash: _isCash,
            coverable: coverable,
            paidNowCtrl: _paidNowCtrl,
            referenceCtrl: _referenceCtrl,
            due: due,
            change: change,
            onMethodChanged: (i) => setState(() {
              _methodIndex = i;
              _localError = null;
            }),
            onPayFull: () {
              _paidNowCtrl.text = coverable.toStringAsFixed(2);
              setState(() {});
            },
          ),
          if ((_localError ?? submitError) != null) ...[
            const SizedBox(height: 14),
            _ErrorBanner(message: _localError ?? submitError!),
          ],
          const SizedBox(height: 20),
          SizedBox(
            height: 56,
            child: FilledButton(
              onPressed: isSubmitting ? null : _submit,
              style: FilledButton.styleFrom(
                textStyle: const TextStyle(
                    fontSize: 18, fontWeight: FontWeight.w700),
              ),
              child: isSubmitting
                  ? SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                          strokeWidth: 2.5, color: context.colors.onInk),
                    )
                  : Text(due > 0
                      ? S.of(context).confirmPaid(
                          formatCurrency(paidNow.clamp(0, grandTotal)))
                      : S.of(context).confirmSale),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Amount card (with explicit discount) ──────────────────────────────────────

class _AmountCard extends StatelessWidget {
  const _AmountCard({
    required this.cartTotal,
    required this.discountCtrl,
    required this.grandTotal,
  });

  final double cartTotal;
  final TextEditingController discountCtrl;
  final double grandTotal;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return _SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(S.of(context).cartTotal,
                  style: theme.textTheme.bodyMedium
                      ?.copyWith(color: scheme.onSurfaceVariant)),
              Text(formatCurrency(cartTotal),
                  style: theme.textTheme.bodyMedium
                      ?.copyWith(fontWeight: FontWeight.w600)),
            ],
          ),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: Text(S.of(context).discount,
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(color: scheme.onSurfaceVariant)),
              ),
              SizedBox(
                width: 130,
                child: TextField(
                  controller: discountCtrl,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))
                  ],
                  textAlign: TextAlign.right,
                  decoration: const InputDecoration(
                    prefixText: kCurrencyPrefix,
                    isDense: true,
                    border: OutlineInputBorder(),
                    contentPadding:
                        EdgeInsets.symmetric(horizontal: 10, vertical: 10),
                  ),
                ),
              ),
            ],
          ),
          const Padding(
            padding: EdgeInsets.symmetric(vertical: 12),
            child: Divider(height: 1),
          ),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(S.of(context).grandTotalLabel,
                  style: theme.textTheme.titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700)),
              Text(
                formatCurrency(grandTotal),
                style: theme.textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.w800, color: scheme.primary),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Customer card ─────────────────────────────────────────────────────────────

class _CustomerCard extends StatelessWidget {
  const _CustomerCard({
    required this.customers,
    required this.isLoading,
    required this.selectedCustomer,
    required this.vehicles,
    required this.isLoadingVehicles,
    required this.selectedVehicle,
    required this.onSelectCustomer,
    required this.onSelectVehicle,
  });

  final List<Customer> customers;
  final bool isLoading;
  final Customer? selectedCustomer;
  final List<CustomerVehicle> vehicles;
  final bool isLoadingVehicles;
  final CustomerVehicle? selectedVehicle;
  final void Function(Customer?) onSelectCustomer;
  final void Function(CustomerVehicle?) onSelectVehicle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return _SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(S.of(context).customer,
              style: theme.textTheme.titleSmall
                  ?.copyWith(color: scheme.onSurfaceVariant)),
          const SizedBox(height: 10),
          if (isLoading)
            const Padding(
              padding: EdgeInsets.symmetric(vertical: 12),
              child: Center(child: CircularProgressIndicator()),
            )
          else
            LayoutBuilder(
              builder: (context, constraints) => DropdownMenu<Customer?>(
                initialSelection: selectedCustomer,
                enableFilter: true,
                requestFocusOnTap: true,
                width: constraints.maxWidth,
                hintText: S.of(context).walkInSelectCustomer,
                inputDecorationTheme:
                    const InputDecorationTheme(isDense: true),
                dropdownMenuEntries: [
                  DropdownMenuEntry(
                    value: null,
                    label: S.of(context).walkInNoAccount,
                  ),
                  ...customers.map(
                    (c) => DropdownMenuEntry(
                      value: c,
                      label: c.dueAmount > 0
                          ? '${c.fullName} (${S.of(context).due}: ${formatCurrency(c.dueAmount)})'
                          : c.fullName,
                    ),
                  ),
                ],
                onSelected: onSelectCustomer,
              ),
            ),
          // Previous due warning — shown when selected customer has a balance
          if (selectedCustomer != null && selectedCustomer!.dueAmount > 0) ...[
            const SizedBox(height: 12),
            Container(
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
              decoration: BoxDecoration(
                color: Colors.amber.shade50,
                border: Border.all(color: Colors.amber.shade200),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Row(
                children: [
                  Icon(Icons.account_balance_wallet_outlined,
                      color: Colors.amber.shade700, size: 22),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          S.of(context).outstandingDue,
                          style: TextStyle(
                            fontSize: 12,
                            color: Colors.amber.shade800,
                            fontWeight: FontWeight.w600,
                          ),
                        ),
                        const SizedBox(height: 2),
                        Text(
                          formatCurrency(selectedCustomer!.dueAmount),
                          style: TextStyle(
                            fontSize: 18,
                            fontWeight: FontWeight.w900,
                            color: Colors.amber.shade800,
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],

          // Vehicle section — only shown when a real customer is selected
          if (selectedCustomer != null) ...[
            const SizedBox(height: 14),
            Text(S.of(context).vehicle,
                style: theme.textTheme.titleSmall
                    ?.copyWith(color: scheme.onSurfaceVariant)),
            const SizedBox(height: 8),
            if (isLoadingVehicles)
              const Center(child: CircularProgressIndicator())
            else if (vehicles.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text(S.of(context).noVehiclesOnFile,
                    style: TextStyle(color: scheme.onSurfaceVariant)),
              )
            else
              Container(
                decoration: BoxDecoration(
                  border: Border.all(
                      color: scheme.outlineVariant.withValues(alpha: 0.6)),
                  borderRadius: BorderRadius.circular(10),
                ),
                child: Column(
                  children: [
                    _VehicleTile(
                      label: S.of(context).noVehicle,
                      isSelected: selectedVehicle == null,
                      onTap: () => onSelectVehicle(null),
                      scheme: scheme,
                    ),
                    Divider(
                        height: 1,
                        color: scheme.outlineVariant.withValues(alpha: 0.5)),
                    ...vehicles.map(
                      (v) => _VehicleTile(
                        label: v.label,
                        isSelected: selectedVehicle?.id == v.id,
                        onTap: () => onSelectVehicle(v),
                        scheme: scheme,
                      ),
                    ),
                  ],
                ),
              ),
          ],
        ],
      ),
    );
  }
}

class _VehicleTile extends StatelessWidget {
  const _VehicleTile({
    required this.label,
    required this.isSelected,
    required this.onTap,
    required this.scheme,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return ListTile(
      dense: true,
      leading: Icon(
        Icons.directions_car_outlined,
        color: isSelected ? scheme.primary : scheme.onSurfaceVariant,
      ),
      title: Text(
        label,
        style: TextStyle(
          fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
          color: isSelected ? scheme.primary : null,
        ),
      ),
      trailing: isSelected
          ? Icon(Icons.check_circle, color: scheme.primary)
          : null,
      onTap: onTap,
    );
  }
}

// ── Advance credit card ───────────────────────────────────────────────────────

class _AdvanceCard extends StatelessWidget {
  const _AdvanceCard({
    required this.available,
    required this.applied,
    required this.apply,
    required this.onToggle,
  });

  final double available;
  final double applied;
  final bool apply;
  final ValueChanged<bool> onToggle;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return _SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.savings_outlined,
                  size: 20, color: Colors.green.shade700),
              const SizedBox(width: 10),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(S.of(context).advanceCredit,
                        style: theme.textTheme.titleSmall
                            ?.copyWith(fontWeight: FontWeight.w600)),
                    Text(
                        S
                            .of(context)
                            .availableAmount(formatCurrency(available)),
                        style: theme.textTheme.bodySmall?.copyWith(
                            color: theme.colorScheme.onSurfaceVariant)),
                  ],
                ),
              ),
              Switch(value: apply, onChanged: onToggle),
            ],
          ),
          if (apply && applied > 0) ...[
            const SizedBox(height: 8),
            _SummaryRow(
              label: S.of(context).appliedToThisSale,
              value: '− ${formatCurrency(applied)}',
              color: Colors.green.shade700,
            ),
          ],
        ],
      ),
    );
  }
}

// ── Payment card (method grid + partial payment) ──────────────────────────────

class _PaymentCard extends StatelessWidget {
  const _PaymentCard({
    required this.methods,
    required this.methodIndex,
    required this.isCash,
    required this.coverable,
    required this.paidNowCtrl,
    required this.referenceCtrl,
    required this.due,
    required this.change,
    required this.onMethodChanged,
    required this.onPayFull,
  });

  final List<String> methods;
  final int methodIndex;
  final bool isCash;
  final double coverable;
  final TextEditingController paidNowCtrl;
  final TextEditingController referenceCtrl;
  final double due;
  final double change;
  final void Function(int) onMethodChanged;
  final VoidCallback onPayFull;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return _SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(S.of(context).payment,
              style: theme.textTheme.titleSmall
                  ?.copyWith(color: scheme.onSurfaceVariant)),
          const SizedBox(height: 12),
          Wrap(
            spacing: 8,
            runSpacing: 8,
            children: [
              for (final (i, m) in methods.indexed)
                ChoiceChip(
                  label: Text(S.of(context).paymentMethodName(m)),
                  selected: i == methodIndex,
                  onSelected: (_) => onMethodChanged(i),
                ),
            ],
          ),
          const SizedBox(height: 16),

          // Amount paid now + Full shortcut
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: TextField(
                  controller: paidNowCtrl,
                  keyboardType:
                      const TextInputType.numberWithOptions(decimal: true),
                  inputFormatters: [
                    FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))
                  ],
                  textAlign: TextAlign.right,
                  style: theme.textTheme.titleLarge
                      ?.copyWith(fontWeight: FontWeight.w700),
                  decoration: InputDecoration(
                    labelText: S.of(context).amountPaidNow,
                    prefixText: kCurrencyPrefix,
                    border: const OutlineInputBorder(),
                    contentPadding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 14),
                  ),
                ),
              ),
              const SizedBox(width: 8),
              OutlinedButton(
                onPressed: onPayFull,
                style: OutlinedButton.styleFrom(
                  minimumSize: const Size(0, 52),
                  padding: const EdgeInsets.symmetric(horizontal: 14),
                ),
                child: Text(S.of(context).full),
              ),
            ],
          ),

          // Reference for non-cash methods
          if (!isCash) ...[
            const SizedBox(height: 12),
            TextField(
              controller: referenceCtrl,
              decoration: InputDecoration(
                labelText: S.of(context).referenceFor(
                    S.of(context).paymentMethodName(methods[methodIndex])),
                hintText: S.of(context).txnRefHint,
                isDense: true,
                border: const OutlineInputBorder(),
                contentPadding:
                    const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
              ),
            ),
          ],

          const SizedBox(height: 14),
          // Change (overpaid cash) or Due (partial) summary
          if (change > 0 && isCash)
            _SummaryRow(
              label: S.of(context).changeLabel,
              value: formatCurrency(change),
              color: Colors.green.shade700,
            )
          else if (due > 0)
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: scheme.errorContainer.withValues(alpha: 0.4),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline,
                      size: 18, color: scheme.onErrorContainer),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      S
                          .of(context)
                          .remainingAddedToBalance(formatCurrency(due)),
                      style: TextStyle(
                          fontSize: 12.5, color: scheme.onErrorContainer),
                    ),
                  ),
                ],
              ),
            )
          else
            _SummaryRow(
              label: S.of(context).paidInFull,
              value: formatCurrency(coverable),
              color: Colors.green.shade700,
            ),
        ],
      ),
    );
  }
}

class _SummaryRow extends StatelessWidget {
  const _SummaryRow(
      {required this.label, required this.value, required this.color});

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(label,
            style: theme.textTheme.titleMedium
                ?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
        Text(value,
            style: theme.textTheme.titleLarge
                ?.copyWith(fontWeight: FontWeight.w800, color: color)),
      ],
    );
  }
}

// ── Shared card wrapper ───────────────────────────────────────────────────────

class _SectionCard extends StatelessWidget {
  const _SectionCard({required this.child});

  final Widget child;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: scheme.outlineVariant.withValues(alpha: 0.6)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: child,
      ),
    );
  }
}

// ── Error banner ──────────────────────────────────────────────────────────────

class _ErrorBanner extends StatelessWidget {
  const _ErrorBanner({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: scheme.errorContainer,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Icon(Icons.error_outline, color: scheme.onErrorContainer),
          const SizedBox(width: 10),
          Expanded(
              child: Text(message,
                  style: TextStyle(color: scheme.onErrorContainer))),
        ],
      ),
    );
  }
}
