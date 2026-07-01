import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
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
  // Amount
  late final TextEditingController _grandTotalCtrl;
  late final TextEditingController _cashReceivedCtrl;

  // Customer list
  final TextEditingController _searchCtrl = TextEditingController();
  List<Customer> _customers = [];
  bool _isLoadingCustomers = false;

  // Selection — null means Walk-in
  Customer? _customer;
  List<CustomerVehicle> _vehicles = [];
  bool _isLoadingVehicles = false;
  CustomerVehicle? _vehicle;

  // Payment
  String _paymentMethod = 'CASH';

  double get grandTotal =>
      double.tryParse(_grandTotalCtrl.text) ?? widget.cartTotal;
  double get cashReceived =>
      double.tryParse(_cashReceivedCtrl.text) ?? grandTotal;
  double get change => (cashReceived - grandTotal).clamp(0, double.infinity);

  @override
  void initState() {
    super.initState();
    _grandTotalCtrl = TextEditingController(
        text: widget.cartTotal.toStringAsFixed(2));
    _cashReceivedCtrl = TextEditingController(
        text: widget.cartTotal.toStringAsFixed(2));
    _grandTotalCtrl.addListener(() => setState(() {}));
    _cashReceivedCtrl.addListener(() => setState(() {}));
    // Load all customers immediately so the list is ready.
    _loadCustomers('');
  }

  @override
  void dispose() {
    _grandTotalCtrl.dispose();
    _cashReceivedCtrl.dispose();
    _searchCtrl.dispose();
    super.dispose();
  }

  // ── Customer loading ─────────────────────────────────────────────────────────

  Future<void> _loadCustomers(String query) async {
    setState(() => _isLoadingCustomers = true);
    try {
      final page = await ref
          .read(customersRepositoryProvider)
          .list(search: query.trim().isEmpty ? null : query.trim(), pageSize: 50);
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
    });
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
    final gt = grandTotal;
    if (gt <= 0) return;
    ref.read(quickSaleControllerProvider.notifier).submit(
          grandTotal: gt,
          paymentMethod: _paymentMethod,
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
      appBar: AppBar(title: const Text('Checkout')),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 40),
        children: [
          _AmountCard(
            cartTotal: widget.cartTotal,
            grandTotalCtrl: _grandTotalCtrl,
            onEditingComplete: () {
              if (_paymentMethod == 'CASH') {
                _cashReceivedCtrl.text = grandTotal.toStringAsFixed(2);
              }
              setState(() {});
            },
          ),
          const SizedBox(height: 14),
          _CustomerCard(
            searchCtrl: _searchCtrl,
            customers: _customers,
            isLoading: _isLoadingCustomers,
            selectedCustomer: _customer,
            vehicles: _vehicles,
            isLoadingVehicles: _isLoadingVehicles,
            selectedVehicle: _vehicle,
            onSearch: (q) {
              setState(() {});
              _loadCustomers(q);
            },
            onSelectCustomer: _selectCustomer,
            onSelectVehicle: (v) => setState(() => _vehicle = v),
          ),
          const SizedBox(height: 14),
          _PaymentCard(
            paymentMethod: _paymentMethod,
            grandTotal: grandTotal,
            cashReceivedCtrl: _cashReceivedCtrl,
            change: change,
            onMethodChanged: (m) => setState(() {
              _paymentMethod = m;
              if (m == 'CASH') {
                _cashReceivedCtrl.text = grandTotal.toStringAsFixed(2);
              }
            }),
          ),
          if (submitError != null) ...[
            const SizedBox(height: 14),
            _ErrorBanner(message: submitError),
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
                  ? const SizedBox(
                      width: 22,
                      height: 22,
                      child: CircularProgressIndicator(
                          strokeWidth: 2.5, color: Colors.white),
                    )
                  : const Text('Confirm Sale'),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Amount card ───────────────────────────────────────────────────────────────

class _AmountCard extends StatelessWidget {
  const _AmountCard({
    required this.cartTotal,
    required this.grandTotalCtrl,
    required this.onEditingComplete,
  });

  final double cartTotal;
  final TextEditingController grandTotalCtrl;
  final VoidCallback onEditingComplete;

  double get _entered => double.tryParse(grandTotalCtrl.text) ?? cartTotal;
  double get _discount => (cartTotal - _entered).clamp(0, double.infinity);

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
              Text('Cart Total',
                  style: theme.textTheme.bodyMedium
                      ?.copyWith(color: scheme.onSurfaceVariant)),
              Text(formatCurrency(cartTotal),
                  style: theme.textTheme.bodyMedium
                      ?.copyWith(fontWeight: FontWeight.w600)),
            ],
          ),
          if (_discount > 0) ...[
            const SizedBox(height: 4),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Discount',
                    style: TextStyle(color: Colors.green.shade700)),
                Text('− ${formatCurrency(_discount)}',
                    style: TextStyle(
                        color: Colors.green.shade700,
                        fontWeight: FontWeight.w600)),
              ],
            ),
          ],
          const SizedBox(height: 12),
          TextField(
            controller: grandTotalCtrl,
            keyboardType:
                const TextInputType.numberWithOptions(decimal: true),
            inputFormatters: [
              FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))
            ],
            textAlign: TextAlign.right,
            onEditingComplete: onEditingComplete,
            style: theme.textTheme.headlineMedium?.copyWith(
                fontWeight: FontWeight.w800, color: scheme.primary),
            decoration: const InputDecoration(
              labelText: 'Grand Total',
              prefixText: '৳  ',
              border: OutlineInputBorder(),
              contentPadding:
                  EdgeInsets.symmetric(horizontal: 14, vertical: 14),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Customer card ─────────────────────────────────────────────────────────────

class _CustomerCard extends StatelessWidget {
  const _CustomerCard({
    required this.searchCtrl,
    required this.customers,
    required this.isLoading,
    required this.selectedCustomer,
    required this.vehicles,
    required this.isLoadingVehicles,
    required this.selectedVehicle,
    required this.onSearch,
    required this.onSelectCustomer,
    required this.onSelectVehicle,
  });

  final TextEditingController searchCtrl;
  final List<Customer> customers;
  final bool isLoading;
  final Customer? selectedCustomer;
  final List<CustomerVehicle> vehicles;
  final bool isLoadingVehicles;
  final CustomerVehicle? selectedVehicle;
  final void Function(String) onSearch;
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
          Text('Customer',
              style: theme.textTheme.titleSmall
                  ?.copyWith(color: scheme.onSurfaceVariant)),
          const SizedBox(height: 10),
          // Search field
          TextField(
            controller: searchCtrl,
            onChanged: onSearch,
            decoration: InputDecoration(
              hintText: 'Search customers...',
              prefixIcon: const Icon(Icons.search),
              border: const OutlineInputBorder(),
              isDense: true,
              suffixIcon: searchCtrl.text.isNotEmpty
                  ? IconButton(
                      icon: const Icon(Icons.clear),
                      onPressed: () {
                        searchCtrl.clear();
                        onSearch('');
                      },
                    )
                  : null,
            ),
          ),
          const SizedBox(height: 8),
          // Customer list in a bounded box
          Container(
            height: 260,
            decoration: BoxDecoration(
              border: Border.all(
                  color: scheme.outlineVariant.withValues(alpha: 0.6)),
              borderRadius: BorderRadius.circular(10),
            ),
            child: isLoading
                ? const Center(child: CircularProgressIndicator())
                : ListView(
                    padding: EdgeInsets.zero,
                    children: [
                      _CustomerTile(
                        name: 'Walk-in',
                        subtitle: 'Default — no account',
                        icon: Icons.person_outline,
                        isSelected: selectedCustomer == null,
                        onTap: () => onSelectCustomer(null),
                        scheme: scheme,
                      ),
                      if (customers.isNotEmpty)
                        Divider(
                            height: 1,
                            color:
                                scheme.outlineVariant.withValues(alpha: 0.5)),
                      ...customers.map(
                        (c) => _CustomerTile(
                          name: c.fullName,
                          subtitle: c.phone ?? c.customerCode,
                          isSelected: selectedCustomer?.id == c.id,
                          onTap: () => onSelectCustomer(c),
                          scheme: scheme,
                          dueAmount: c.dueAmount,
                        ),
                      ),
                    ],
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
                          'Outstanding Due',
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
            Text('Vehicle',
                style: theme.textTheme.titleSmall
                    ?.copyWith(color: scheme.onSurfaceVariant)),
            const SizedBox(height: 8),
            if (isLoadingVehicles)
              const Center(child: CircularProgressIndicator())
            else if (vehicles.isEmpty)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 8),
                child: Text('No vehicles on file for this customer.',
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
                      label: 'No vehicle',
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

class _CustomerTile extends StatelessWidget {
  const _CustomerTile({
    required this.name,
    required this.subtitle,
    this.icon,
    required this.isSelected,
    required this.onTap,
    required this.scheme,
    this.dueAmount = 0,
  });

  final String name;
  final String subtitle;
  final IconData? icon;
  final bool isSelected;
  final VoidCallback onTap;
  final ColorScheme scheme;
  final double dueAmount;

  @override
  Widget build(BuildContext context) {
    final hasDue = dueAmount > 0;
    return ListTile(
      dense: true,
      leading: CircleAvatar(
        radius: 18,
        backgroundColor: isSelected
            ? scheme.primary
            : scheme.surfaceContainerHighest,
        child: Icon(
          icon ?? Icons.person,
          size: 18,
          color: isSelected ? scheme.onPrimary : scheme.onSurfaceVariant,
        ),
      ),
      title: Text(
        name,
        style: TextStyle(
          fontWeight: isSelected ? FontWeight.w700 : FontWeight.w500,
          color: isSelected ? scheme.primary : null,
        ),
      ),
      subtitle: hasDue
          ? Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisSize: MainAxisSize.min,
              children: [
                Text(subtitle,
                    style: TextStyle(
                        fontSize: 12, color: scheme.onSurfaceVariant)),
                const SizedBox(height: 2),
                Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.warning_amber_rounded,
                        size: 12, color: Colors.amber.shade700),
                    const SizedBox(width: 3),
                    Text(
                      'Due: ${formatCurrency(dueAmount)}',
                      style: TextStyle(
                        fontSize: 11,
                        color: Colors.amber.shade700,
                        fontWeight: FontWeight.w700,
                      ),
                    ),
                  ],
                ),
              ],
            )
          : Text(subtitle,
              style:
                  TextStyle(fontSize: 12, color: scheme.onSurfaceVariant)),
      trailing: isSelected
          ? Icon(Icons.check_circle, color: scheme.primary)
          : null,
      onTap: onTap,
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

// ── Payment card ──────────────────────────────────────────────────────────────

class _PaymentCard extends StatelessWidget {
  const _PaymentCard({
    required this.paymentMethod,
    required this.grandTotal,
    required this.cashReceivedCtrl,
    required this.change,
    required this.onMethodChanged,
  });

  final String paymentMethod;
  final double grandTotal;
  final TextEditingController cashReceivedCtrl;
  final double change;
  final void Function(String) onMethodChanged;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return _SectionCard(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text('Payment',
              style: theme.textTheme.titleSmall
                  ?.copyWith(color: scheme.onSurfaceVariant)),
          const SizedBox(height: 12),
          SegmentedButton<String>(
            style: SegmentedButton.styleFrom(
              textStyle:
                  const TextStyle(fontSize: 15, fontWeight: FontWeight.w600),
            ),
            showSelectedIcon: false,
            segments: const [
              ButtonSegment(
                value: 'CASH',
                label: Text('Cash'),
                icon: Icon(Icons.payments_outlined),
              ),
              ButtonSegment(
                value: 'DUE',
                label: Text('Due'),
                icon: Icon(Icons.pending_actions_outlined),
              ),
            ],
            selected: {paymentMethod},
            onSelectionChanged: (s) => onMethodChanged(s.first),
          ),
          const SizedBox(height: 16),
          if (paymentMethod == 'CASH') ...[
            TextField(
              controller: cashReceivedCtrl,
              keyboardType:
                  const TextInputType.numberWithOptions(decimal: true),
              inputFormatters: [
                FilteringTextInputFormatter.allow(RegExp(r'[0-9.]'))
              ],
              textAlign: TextAlign.right,
              style: theme.textTheme.titleLarge
                  ?.copyWith(fontWeight: FontWeight.w700),
              decoration: const InputDecoration(
                labelText: 'Cash Received',
                prefixText: '৳  ',
                border: OutlineInputBorder(),
                contentPadding:
                    EdgeInsets.symmetric(horizontal: 14, vertical: 14),
              ),
            ),
            const SizedBox(height: 12),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Change',
                    style: theme.textTheme.titleMedium
                        ?.copyWith(color: scheme.onSurfaceVariant)),
                Text(
                  formatCurrency(change),
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w800,
                    color: change > 0
                        ? Colors.green.shade700
                        : scheme.onSurface,
                  ),
                ),
              ],
            ),
          ] else ...[
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Text('Amount Due', style: theme.textTheme.titleMedium),
                Text(
                  formatCurrency(grandTotal),
                  style: theme.textTheme.titleLarge?.copyWith(
                    fontWeight: FontWeight.w800,
                    color: scheme.error,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: scheme.errorContainer.withValues(alpha: 0.4),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                children: [
                  Icon(Icons.info_outline,
                      size: 16, color: scheme.onErrorContainer),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      'Added to the customer\'s outstanding balance.',
                      style: TextStyle(
                          fontSize: 12, color: scheme.onErrorContainer),
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
