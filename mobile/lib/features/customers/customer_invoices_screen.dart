import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/invoice.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

/// Server-paginated, filterable invoice list for a customer. Loads each page
/// from `GET /SalesOrder/invoices` as the user scrolls; filter by invoice
/// status and search by invoice number. Line items load on demand when a row
/// is expanded.
class CustomerInvoicesScreen extends ConsumerStatefulWidget {
  const CustomerInvoicesScreen({super.key, required this.customerId});

  final String customerId;

  @override
  ConsumerState<CustomerInvoicesScreen> createState() =>
      _CustomerInvoicesScreenState();
}

class _CustomerInvoicesScreenState
    extends ConsumerState<CustomerInvoicesScreen> {
  final _searchCtrl = TextEditingController();
  String? _status; // null = All
  String _search = '';
  int? _total;

  // Invoice statuses (matches the backend's values).
  static const _statuses = [
    'DRAFT', 'ISSUED', 'PARTIALLY_PAID', 'PAID', 'OVERDUE', 'CANCELLED',
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
          Divider(height: 1),
          Expanded(
            child: PagedListView<Invoice>(
              resetKey: '${_status ?? 'all'}|$_search',
              padding: const EdgeInsets.all(12),
              onLoaded: (total) {
                if (_total != total) {
                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    if (mounted) setState(() => _total = total);
                  });
                }
              },
              fetch: (page) => repo.invoicesPage(
                customerId: widget.customerId,
                status: _status,
                search: _search.isEmpty ? null : _search,
                page: page,
              ),
              emptyBuilder: (_) => const EmptyView(
                  message: 'No matching invoices.',
                  icon: Icons.receipt_long_outlined),
              itemBuilder: (_, invoice) => _InvoiceTile(invoice: invoice),
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
          PopupMenuItem(value: s, child: Text(_statusLabel(s))),
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
            Text(active ? _statusLabel(value!) : 'Status',
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

/// "PARTIALLY_PAID" -> "Partially paid".
String _statusLabel(String s) {
  if (s.isEmpty) return s;
  final lower = s.replaceAll('_', ' ').toLowerCase();
  return lower[0].toUpperCase() + lower.substring(1);
}

class _InvoiceTile extends ConsumerStatefulWidget {
  const _InvoiceTile({required this.invoice});

  final Invoice invoice;

  @override
  ConsumerState<_InvoiceTile> createState() => _InvoiceTileState();
}

class _InvoiceTileState extends ConsumerState<_InvoiceTile> {
  // Cached so lines are fetched once per expand, not on every rebuild.
  Future<List<InvoiceLine>>? _linesFuture;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final invoice = widget.invoice;
    final hasOutstanding = invoice.outstandingAmount > 0;

    final subtitleParts = <String>[
      formatDate(invoice.invoiceDate),
      if (invoice.dueDate != null) 'Due ${formatDate(invoice.dueDate!)}',
      if (invoice.salesOrderNumber.isNotEmpty) 'SO ${invoice.salesOrderNumber}',
    ];

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ExpansionTile(
        tilePadding: const EdgeInsets.symmetric(horizontal: 16),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
        onExpansionChanged: (expanded) {
          if (expanded && _linesFuture == null) {
            setState(() {
              _linesFuture =
                  ref.read(customersRepositoryProvider).invoiceLines(invoice.id);
            });
          }
        },
        title: Row(
          children: [
            Flexible(
              child: Text(
                invoice.invoiceNumber.isEmpty ? 'â€”' : invoice.invoiceNumber,
                style: const TextStyle(fontWeight: FontWeight.w600),
                overflow: TextOverflow.ellipsis,
              ),
            ),
            if (invoice.isOverdue) ...[
              const SizedBox(width: 8),
              Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                decoration: BoxDecoration(
                  color: scheme.errorContainer,
                  borderRadius: BorderRadius.circular(4),
                ),
                child: Text('Overdue',
                    style: TextStyle(
                        fontSize: 10,
                        fontWeight: FontWeight.w700,
                        color: scheme.onErrorContainer)),
              ),
            ],
          ],
        ),
        subtitle: Text(subtitleParts.join('  â€¢  ')),
        trailing: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(formatCurrency(invoice.grandTotal, currency: invoice.currency),
                style: const TextStyle(fontWeight: FontWeight.w700)),
            if (hasOutstanding)
              Text(
                  'Due ${formatCurrency(invoice.outstandingAmount, currency: invoice.currency)}',
                  style: TextStyle(fontSize: 11, color: scheme.error))
            else
              Text('Paid',
                  style: TextStyle(fontSize: 11, color: Colors.green.shade700)),
          ],
        ),
        children: [
          _LineItems(future: _linesFuture, currency: invoice.currency),
          if (invoice.status != null) ...[
            Divider(height: 16),
            Row(
              children: [
                Text('Status: ', style: theme.textTheme.bodySmall),
                Text(_statusLabel(invoice.status!),
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

/// Renders the lazily-loaded invoice lines: spinner while loading, a friendly
/// message on error/empty, otherwise the product rows.
class _LineItems extends StatelessWidget {
  const _LineItems({required this.future, this.currency});

  final Future<List<InvoiceLine>>? future;
  final String? currency;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    if (future == null) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 8),
        child: Center(
          child: SizedBox(
            height: 18,
            width: 18,
            child: CircularProgressIndicator(strokeWidth: 2),
          ),
        ),
      );
    }
    return FutureBuilder<List<InvoiceLine>>(
      future: future,
      builder: (context, snap) {
        if (snap.connectionState == ConnectionState.waiting) {
          return const Padding(
            padding: EdgeInsets.symmetric(vertical: 12),
            child: Center(
              child: SizedBox(
                height: 18,
                width: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
            ),
          );
        }
        if (snap.hasError) {
          return Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child: Text('Could not load items.',
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: theme.colorScheme.error)),
          );
        }
        final lines = snap.data ?? const <InvoiceLine>[];
        if (lines.isEmpty) {
          return Padding(
            padding: const EdgeInsets.symmetric(vertical: 8),
            child:
                Text('No items on this invoice.', style: theme.textTheme.bodySmall),
          );
        }
        return Column(
          children: [
            for (final line in lines)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 4),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(line.displayName,
                              style:
                                  const TextStyle(fontWeight: FontWeight.w600)),
                          if (line.partSku != null || line.variantName != null)
                            Text(
                              [
                                if (line.partSku != null) line.partSku!,
                                if (line.variantName != null) line.variantName!,
                              ].join('  â€¢  '),
                              style: theme.textTheme.bodySmall?.copyWith(
                                  color: theme.colorScheme.onSurfaceVariant),
                            ),
                          Text(
                            '${line.quantity} ${line.unitSymbol ?? ''} Ã— '
                            '${formatCurrency(line.unitPrice, currency: currency)}',
                            style: theme.textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                    Text(
                      formatCurrency(line.lineTotal, currency: currency),
                      style: const TextStyle(fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
              ),
          ],
        );
      },
    );
  }
}
