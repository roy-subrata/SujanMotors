import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/state_views.dart';
import 'notification_models.dart';
import 'notifications_controller.dart';

/// Realtime notification inbox. New sales pushed over SignalR appear here live.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsControllerProvider);
    final controller = ref.read(notificationsControllerProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Notifications'),
        actions: [
          if (state.items.any((n) => !n.read))
            TextButton(
              onPressed: controller.markAllRead,
              child: const Text('Mark all read'),
            ),
          if (state.items.isNotEmpty)
            IconButton(
              tooltip: 'Clear all',
              icon: const Icon(Icons.delete_sweep_outlined),
              onPressed: controller.clearAll,
            ),
        ],
        bottom: _StatusBar(status: state.status),
      ),
      body: state.items.isEmpty
          ? const EmptyView(
              message: 'No notifications yet.\nNew sales will appear here live.',
              icon: Icons.notifications_off_outlined,
            )
          : ListView.separated(
              itemCount: state.items.length,
              separatorBuilder: (_, _) => const Divider(height: 1),
              itemBuilder: (context, index) => _NotificationTile(
                notification: state.items[index],
                onTap: () => controller.markRead(state.items[index]),
              ),
            ),
    );
  }
}

class _StatusBar extends StatelessWidget implements PreferredSizeWidget {
  const _StatusBar({required this.status});

  final HubStatus status;

  @override
  Size get preferredSize => const Size.fromHeight(24);

  @override
  Widget build(BuildContext context) {
    final (color, label, icon) = switch (status) {
      HubStatus.connected => (Colors.green, 'Live', Icons.bolt),
      HubStatus.connecting => (Colors.orange, 'Connecting…', Icons.sync),
      HubStatus.disconnected => (Colors.grey, 'Offline', Icons.cloud_off),
    };
    return SizedBox(
      height: 24,
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 6),
          Text(label, style: TextStyle(fontSize: 12, color: color)),
        ],
      ),
    );
  }
}

class _NotificationTile extends StatelessWidget {
  const _NotificationTile({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final sale = notification.sale;
    final scheme = Theme.of(context).colorScheme;
    return ListTile(
      tileColor: notification.read ? null : scheme.primaryContainer.withValues(alpha: 0.18),
      leading: CircleAvatar(
        backgroundColor: scheme.primaryContainer,
        child: Icon(Icons.receipt_long, color: scheme.onPrimaryContainer),
      ),
      title: Text(
        'New sale ${sale.soNumber}',
        style: TextStyle(
          fontWeight: notification.read ? FontWeight.w400 : FontWeight.w700,
        ),
      ),
      subtitle: Text(
        '${sale.customerName.isEmpty ? 'Walk-in' : sale.customerName} • '
        '${formatCurrency(sale.grandTotal, currency: sale.currency)} • '
        '${sale.saleChannel}',
      ),
      trailing: Text(
        formatRelative(sale.occurredAt),
        style: Theme.of(context).textTheme.bodySmall,
      ),
      onTap: onTap,
    );
  }
}
