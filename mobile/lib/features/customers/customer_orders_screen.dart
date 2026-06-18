import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

/// Server-paginated, filterable invoice (sales order) list for a customer.
/// Loads each page from the API as the user scrolls. Filter by status and
/// search by invoice number.
class CustomerOrdersScreen extends ConsumerStatefulWidget {
  const CustomerOrdersScreen({super.key, required this.customerId});

  final String customerId;

  @override
  ConsumerState<CustomerOrdersScreen> createState() =>
      _CustomerOrdersScreenState();
}

class _CustomerOrdersScreenState extends ConsumerState<CustomerOrdersScreen> {
  final _searchCtrl = TextEditingController();
  String? _status; // null = All
  String _search = '';
  int? _total;

  // Common sales-order statuses (matches the backend's values).
  static const _statuses = [
    'DRAFT', 'PENDING', 'CONFIRMED', 'PROCESSING', 'PACKED',
    'SHIPPED', 'DELIVERED', 'COMPLETED', 'CANCELLED', 'RETURNED',
  ];

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final repo = ref.read(customersRepositoryProvider);

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Invoices'),
      ),
      body: Column(
        children: [
          // Search + status filter.
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 8),
            child: Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _searchCtrl,
                    textInputAction: TextInputAction.search,
                    onSubmitted: (v) => setState(() => _search = v.trim()),
                    decoration: InputDecoration(
                      hintText: 'Search invoice number',
                      prefixIcon: const Icon(Icons.search),
                      isDense: true,
                      border: const OutlineInputBorder(),
                      suffixIcon: _searchCtrl.text.isEmpty
                          ? null
                          : IconButton(
                              icon: const Icon(Icons.clear),
                              onPressed: () {
                                _searchCtrl.clear();
                                setState(() => _search = '');
                              },
                            ),
                    ),
                  ),
                ),
                const SizedBox(width: 8),
                _StatusMenu(
                  value: _status,
                  options: _statuses,
                  onChanged: (s) => setState(() => _status = s),
                ),
              ],
            ),
          ),
          if (_total != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Align(
                alignment: Alignment.centerLeft,
                child: Text('$_total invoice(s)',
                    style: Theme.of(context).textTheme.bodySmall),
              ),
            ),
          const Divider(height: 1),
          Expanded(
            child: PagedListView<CustomerOrder>(
              resetKey: '${_status ?? 'all'}|$_search',
              padding: const EdgeInsets.all(12),
              onLoaded: (total) {
                if (_total != total) {
                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    if (mounted) setState(() => _total = total);
                  });
                }
              },
              fetch: (page) => repo.ordersPage(
                customerId: widget.customerId,
                status: _status,
                search: _search.isEmpty ? null : _search,
                page: page,
              ),
              emptyBuilder: (_) => const EmptyView(
                  message: 'No matching invoices.',
                  icon: Icons.receipt_long_outlined),
              itemBuilder: (_, order) => _OrderTile(order: order),
            ),
          ),
        ],
      ),
    );
  }
}

class _StatusMenu extends StatelessWidget {
  const _StatusMenu(
      {required this.value, required this.options, required this.onChanged});

  final String? value;
  final List<String> options;
  final ValueChanged<String?> onChanged;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final active = value != null;
    return PopupMenuButton<String?>(
      tooltip: 'Filter by status',
      onSelected: onChanged,
      itemBuilder: (_) => [
        const PopupMenuItem(value: null, child: Text('All statuses')),
        for (final s in options)
          PopupMenuItem(value: s, child: Text(_titleCase(s))),
      ],
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 12),
        decoration: BoxDecoration(
          color: active ? scheme.primaryContainer : scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(8),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.filter_list, size: 18,
                color: active ? scheme.onPrimaryContainer : scheme.onSurfaceVariant),
            const SizedBox(width: 4),
            Text(active ? _titleCase(value!) : 'Status',
                style: TextStyle(
                    color: active
                        ? scheme.onPrimaryContainer
                        : scheme.onSurfaceVariant,
                    fontWeight: FontWeight.w600)),
          ],
        ),
      ),
    );
  }
}

String _titleCase(String s) =>
    s.isEmpty ? s : s[0] + s.substring(1).toLowerCase();

class _OrderTile extends StatelessWidget {
  const _OrderTile({required this.order});

  final CustomerOrder order;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final hasOutstanding = order.outstandingAmount > 0;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ExpansionTile(
        tilePadding: const EdgeInsets.symmetric(horizontal: 16),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
        title: Text(order.soNumber,
            style: const TextStyle(fontWeight: FontWeight.w600)),
        subtitle: Text(
            '${formatDate(order.orderDate)}  •  ${order.itemCount} item(s)'),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(formatCurrency(order.grandTotal, currency: order.currency),
                style: const TextStyle(fontWeight: FontWeight.w700)),
            if (hasOutstanding)
              Text('Due ${formatCurrency(order.outstandingAmount, currency: order.currency)}',
                  style: TextStyle(fontSize: 11, color: scheme.error))
            else
              Text('Paid',
                  style:
                      TextStyle(fontSize: 11, color: Colors.green.shade700)),
          ],
        ),
        children: [
          for (final line in order.lines)
            Padding(
              padding: const EdgeInsets.symmetric(vertical: 4),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(line.displayName),
                        Text(
                          '${line.quantity} ${line.unitSymbol ?? ''} × '
                          '${formatCurrency(line.unitPrice, currency: order.currency)}',
                          style: theme.textTheme.bodySmall,
                        ),
                      ],
                    ),
                  ),
                  Text(
                    formatCurrency(line.lineTotal, currency: order.currency),
                    style: const TextStyle(fontWeight: FontWeight.w600),
                  ),
                ],
              ),
            ),
          if (order.status != null) ...[
            const Divider(height: 16),
            Row(
              children: [
                Text('Status: ', style: theme.textTheme.bodySmall),
                Text(order.status!,
                    style: theme.textTheme.bodySmall
                        ?.copyWith(fontWeight: FontWeight.w600)),
              ],
            ),
          ],
        ],
      ),
    );
  }
}
