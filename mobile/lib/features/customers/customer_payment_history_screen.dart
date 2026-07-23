import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/i18n/strings.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

/// Server-paginated, filterable payment list for a customer. Loads each page
/// from the API as the user scrolls; filter by status.
class CustomerPaymentHistoryScreen extends ConsumerStatefulWidget {
  const CustomerPaymentHistoryScreen({super.key, required this.customerId});

  final String customerId;

  @override
  ConsumerState<CustomerPaymentHistoryScreen> createState() =>
      _CustomerPaymentHistoryScreenState();
}

class _CustomerPaymentHistoryScreenState
    extends ConsumerState<CustomerPaymentHistoryScreen> {
  String? _status; // null = All
  int? _total;

  static const _filters = <String?, String>{
    null: 'All',
    'COMPLETED': 'Completed',
    'PENDING': 'Pending',
    'FAILED': 'Failed',
    'REFUNDED': 'Refunded',
  };

  @override
  Widget build(BuildContext context) {
    final repo = ref.read(customersRepositoryProvider);

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: Text(S.of(context).paymentHistory),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 4),
            child: Align(
              alignment: Alignment.centerLeft,
              child: Wrap(
                spacing: 8,
                children: [
                  for (final entry in _filters.entries)
                    ChoiceChip(
                      label: Text(entry.key == null
                          ? S.of(context).all
                          : S.of(context).statusName(entry.key!)),
                      selected: _status == entry.key,
                      onSelected: (_) => setState(() => _status = entry.key),
                    ),
                ],
              ),
            ),
          ),
          if (_total != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 8),
              child: Align(
                alignment: Alignment.centerLeft,
                child: Text(S.of(context).paymentsCount(_total!),
                    style: Theme.of(context).textTheme.bodySmall),
              ),
            ),
          Divider(height: 1),
          Expanded(
            child: PagedListView<PaymentHistoryItem>(
              resetKey: _status ?? 'all',
              onLoaded: (total) {
                if (_total != total) {
                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    if (mounted) setState(() => _total = total);
                  });
                }
              },
              fetch: (page) => repo.paymentsPage(
                customerId: widget.customerId,
                status: _status,
                page: page,
              ),
              separatorBuilder: (_, _) => Divider(height: 1),
              emptyBuilder: (_) => EmptyView(
                message: _status == null
                    ? S.of(context).noPaymentsRecorded
                    : S.of(context).noStatusPayments(
                        S.of(context).statusName(_status!).toLowerCase()),
                icon: Icons.receipt_long_outlined,
              ),
              itemBuilder: (_, item) => _PaymentTile(item: item),
            ),
          ),
        ],
      ),
    );
  }
}

class _PaymentTile extends StatelessWidget {
  const _PaymentTile({required this.item});

  final PaymentHistoryItem item;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final status = (item.status ?? '').toUpperCase();
    final (bg, fg, icon) = switch (status) {
      'COMPLETED' => (Colors.green.shade50, Colors.green.shade700, Icons.check),
      'FAILED' => (scheme.errorContainer, scheme.error, Icons.close),
      _ => (scheme.surfaceContainerHighest, scheme.onSurfaceVariant,
          Icons.schedule),
    };

    return ListTile(
      leading: CircleAvatar(
        backgroundColor: bg,
        child: Icon(icon, size: 18, color: fg),
      ),
      title: Text(formatCurrency(item.amount),
          style: const TextStyle(fontWeight: FontWeight.w700)),
      subtitle: Text([
        formatDate(item.paymentDate),
        if (item.paymentMethod != null && item.paymentMethod!.isNotEmpty)
          item.paymentMethod!,
        if (item.invoiceNumber != null && item.invoiceNumber!.isNotEmpty)
          item.invoiceNumber!,
      ].join('  •  ')),
      trailing: Container(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
        decoration: BoxDecoration(
          color: bg,
          borderRadius: BorderRadius.circular(12),
        ),
        child: Text(
          item.status == null ? '—' : S.of(context).statusName(item.status!),
          style: TextStyle(fontSize: 11, color: fg, fontWeight: FontWeight.w600),
        ),
      ),
    );
  }
}
