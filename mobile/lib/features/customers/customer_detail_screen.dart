import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

class CustomerDetailScreen extends ConsumerWidget {
  const CustomerDetailScreen({super.key, required this.customerId});

  final String customerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final customerAsync = ref.watch(customerDetailProvider(customerId));

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Customer'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(customerDetailProvider(customerId));
          ref.invalidate(customerPaymentSummaryProvider(customerId));
          ref.invalidate(customerOrdersProvider(customerId));
        },
        child: customerAsync.when(
          loading: () => const LoadingView(),
          error: (e, _) => ListView(children: [
            const SizedBox(height: 120),
            ErrorView(
              message: e is AppException ? e.message : 'Failed to load customer.',
              onRetry: () => ref.invalidate(customerDetailProvider(customerId)),
            ),
          ]),
          data: (customer) => _Body(customer: customer),
        ),
      ),
    );
  }
}

class _Body extends ConsumerWidget {
  const _Body({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final summaryAsync =
        ref.watch(customerPaymentSummaryProvider(customer.id));
    final ordersAsync = ref.watch(customerOrdersProvider(customer.id));

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _CustomerCard(customer: customer),
        const SizedBox(height: 20),
        _SectionHeader(icon: Icons.account_balance_wallet_outlined,
            title: 'Payment & due'),
        const SizedBox(height: 8),
        summaryAsync.when(
          loading: () => const Padding(
            padding: EdgeInsets.symmetric(vertical: 24), child: LoadingView()),
          error: (e, _) => ErrorView(
            message: e is AppException ? e.message : 'Failed to load payments.',
            onRetry: () =>
                ref.invalidate(customerPaymentSummaryProvider(customer.id)),
          ),
          data: (summary) => _PaymentSummaryCard(
            customer: customer,
            summary: summary,
          ),
        ),
        const SizedBox(height: 20),
        // Invoices (parts buying history) live on their own filterable screen.
        Card(
          margin: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.shopping_bag_outlined),
            title: const Text('Invoices'),
            subtitle: Text(ordersAsync.maybeWhen(
              data: (orders) => '${orders.length} invoice(s)',
              orElse: () => 'Parts buying history',
            )),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => context.push('/customers/${customer.id}/orders'),
          ),
        ),
        const SizedBox(height: 12),
        // Payment history lives on its own filterable screen to keep this view tidy.
        Card(
          margin: EdgeInsets.zero,
          child: ListTile(
            leading: const Icon(Icons.history),
            title: const Text('Payment history'),
            subtitle: Text(summaryAsync.maybeWhen(
              data: (summary) => '${summary.history.length} payment(s)',
              orElse: () => 'Completed, pending & failed payments',
            )),
            trailing: const Icon(Icons.chevron_right),
            onTap: () => context.push('/customers/${customer.id}/payments'),
          ),
        ),
        const SizedBox(height: 24),
      ],
    );
  }
}

class _CustomerCard extends StatelessWidget {
  const _CustomerCard({required this.customer});

  final Customer customer;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final initial = customer.fullName.trim().isEmpty
        ? '?'
        : customer.fullName.trim().characters.first.toUpperCase();

    return Card(
      elevation: 0,
      color: scheme.surfaceContainerHighest.withValues(alpha: 0.4),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  radius: 26,
                  backgroundColor: scheme.primaryContainer,
                  child: Text(initial,
                      style: theme.textTheme.titleLarge
                          ?.copyWith(color: scheme.onPrimaryContainer)),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(customer.fullName,
                          style: theme.textTheme.titleLarge),
                      if (customer.companyName != null &&
                          customer.companyName!.isNotEmpty)
                        Text(customer.companyName!,
                            style: theme.textTheme.bodyMedium
                                ?.copyWith(color: scheme.primary)),
                      Text('Code ${customer.customerCode}',
                          style: theme.textTheme.bodySmall),
                    ],
                  ),
                ),
                if (customer.status != null)
                  _StatusChip(status: customer.status!),
              ],
            ),
            if ((customer.phone ?? '').isNotEmpty ||
                (customer.email ?? '').isNotEmpty) ...[
              const Divider(height: 24),
              if ((customer.phone ?? '').isNotEmpty)
                _ContactRow(icon: Icons.phone_outlined, value: customer.phone!),
              if ((customer.email ?? '').isNotEmpty)
                _ContactRow(icon: Icons.email_outlined, value: customer.email!),
              if ((customer.city ?? '').isNotEmpty)
                _ContactRow(
                    icon: Icons.location_on_outlined, value: customer.city!),
            ],
          ],
        ),
      ),
    );
  }
}

class _ContactRow extends StatelessWidget {
  const _ContactRow({required this.icon, required this.value});

  final IconData icon;
  final String value;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 3),
      child: Row(
        children: [
          Icon(icon, size: 16, color: scheme.onSurfaceVariant),
          const SizedBox(width: 8),
          Expanded(child: Text(value)),
        ],
      ),
    );
  }
}

class _PaymentSummaryCard extends ConsumerStatefulWidget {
  const _PaymentSummaryCard({required this.customer, required this.summary});

  final Customer customer;
  final CustomerPaymentSummary summary;

  @override
  ConsumerState<_PaymentSummaryCard> createState() =>
      _PaymentSummaryCardState();
}

class _PaymentSummaryCardState extends ConsumerState<_PaymentSummaryCard> {
  bool _sending = false;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final s = widget.summary;
    final due = s.amountDue;
    final hasDue = due > 0;
    final dueColor = hasDue ? scheme.error : Colors.green.shade700;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // Headline due amount.
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: (hasDue ? scheme.errorContainer : Colors.green.shade50)
                    .withValues(alpha: 0.6),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Outstanding due', style: theme.textTheme.bodySmall),
                  const SizedBox(height: 4),
                  Text(
                    formatCurrency(due),
                    style: theme.textTheme.headlineMedium
                        ?.copyWith(color: dueColor, fontWeight: FontWeight.bold),
                  ),
                  if (s.overdueInvoices > 0)
                    Text('${s.overdueInvoices} overdue invoice(s)',
                        style: TextStyle(color: scheme.error, fontSize: 12)),
                ],
              ),
            ),
            const SizedBox(height: 16),
            // Metric grid.
            Wrap(
              spacing: 24,
              runSpacing: 16,
              children: [
                _Metric(label: 'Total paid', value: formatCurrency(s.totalPaid)),
                _Metric(
                    label: 'Total invoiced',
                    value: formatCurrency(s.totalInvoiceAmount)),
                _Metric(
                    label: 'Advance credit',
                    value: formatCurrency(s.availableAdvance)),
                _Metric(
                    label: 'Unpaid invoices', value: '${s.unpaidInvoices}'),
                if (s.lastPaymentDate != null)
                  _Metric(
                      label: 'Last payment',
                      value:
                          '${formatCurrency(s.lastPaymentAmount)}\n${formatDate(s.lastPaymentDate!)}'),
              ],
            ),
            const SizedBox(height: 16),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: _sending ? null : _openReminderSheet,
                icon: _sending
                    ? const SizedBox(
                        width: 16,
                        height: 16,
                        child: CircularProgressIndicator(strokeWidth: 2))
                    : const Icon(Icons.notifications_active_outlined),
                label: Text(hasDue ? 'Send payment reminder' : 'Send a reminder'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _openReminderSheet() async {
    final customer = widget.customer;
    final channel = await showModalBottomSheet<String>(
      context: context,
      showDragHandle: true,
      builder: (sheetCtx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 8),
              child: Text('Send reminder via',
                  style: Theme.of(sheetCtx).textTheme.titleMedium),
            ),
            ListTile(
              leading: const Icon(Icons.sms_outlined),
              title: const Text('SMS'),
              subtitle: Text(customer.phone ?? 'No phone number'),
              enabled: (customer.phone ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'SMS'),
            ),
            ListTile(
              leading: const Icon(Icons.chat_outlined),
              title: const Text('WhatsApp'),
              subtitle: Text(customer.phone ?? 'No phone number'),
              enabled: (customer.phone ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'WHATSAPP'),
            ),
            ListTile(
              leading: const Icon(Icons.email_outlined),
              title: const Text('Email'),
              subtitle: Text(customer.email ?? 'No email address'),
              enabled: (customer.email ?? '').isNotEmpty,
              onTap: () => Navigator.pop(sheetCtx, 'EMAIL'),
            ),
            const SizedBox(height: 8),
          ],
        ),
      ),
    );

    if (channel == null || !mounted) return;
    await _sendReminder(channel);
  }

  Future<void> _sendReminder(String channel) async {
    setState(() => _sending = true);
    final messenger = ScaffoldMessenger.of(context);
    final errorColor = Theme.of(context).colorScheme.error;
    try {
      final msg = await ref.read(customersRepositoryProvider).sendPaymentReminder(
            customerId: widget.customer.id,
            channel: channel,
          );
      messenger.showSnackBar(SnackBar(content: Text(msg)));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
      ));
    } finally {
      if (mounted) setState(() => _sending = false);
    }
  }
}

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.icon, required this.title});

  final IconData icon;
  final String title;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Row(
      children: [
        Icon(icon, size: 20, color: theme.colorScheme.primary),
        const SizedBox(width: 8),
        Text(title, style: theme.textTheme.titleMedium),
      ],
    );
  }
}

class _StatusChip extends StatelessWidget {
  const _StatusChip({required this.status});

  final String status;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final active = status.toUpperCase() == 'ACTIVE';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: active ? Colors.green.shade50 : scheme.surfaceContainerHighest,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Text(status,
          style: TextStyle(
              fontSize: 11,
              color: active ? Colors.green.shade800 : scheme.onSurfaceVariant)),
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      mainAxisSize: MainAxisSize.min,
      children: [
        Text(label, style: theme.textTheme.bodySmall),
        const SizedBox(height: 2),
        Text(value,
            style: theme.textTheme.titleSmall
                ?.copyWith(fontWeight: FontWeight.bold)),
      ],
    );
  }
}
