import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import 'notification_models.dart';
import 'notifications_controller.dart';

class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsControllerProvider);
    final controller = ref.read(notificationsControllerProvider.notifier);
    final scheme = Theme.of(context).colorScheme;
    final unreadCount = state.items.where((n) => !n.read).length;

    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.canPop() ? context.pop() : context.go('/'),
        ),
        title: Text(
          'Notifications',
          style: GoogleFonts.instrumentSans(
            fontSize: 17,
            fontWeight: FontWeight.w700,
          ),
        ),
        actions: [
          if (unreadCount > 0)
            TextButton(
              onPressed: controller.markAllRead,
              child: Text(
                'Mark all read',
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w600,
                  color: const Color(0xFF4F46E5),
                ),
              ),
            ),
          if (state.items.isNotEmpty)
            IconButton(
              tooltip: 'Clear all',
              icon: const Icon(Icons.delete_sweep_outlined),
              onPressed: controller.clearAll,
            ),
        ],
        bottom: PreferredSize(
          preferredSize: const Size.fromHeight(32),
          child: _StatusBar(status: state.status),
        ),
      ),
      body: state.items.isEmpty
          ? Column(
              children: [
                const SizedBox(height: 60),
                Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    color: const Color(0xFF4F46E5).withAlpha(18),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: const Color(0xFF4F46E5).withAlpha(30)),
                  ),
                  child: const Icon(
                    Icons.notifications_off_outlined,
                    size: 34,
                    color: Color(0xFF4F46E5),
                  ),
                ),
                const SizedBox(height: 16),
                Text(
                  'No notifications yet',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 6),
                Text(
                  'New sales will appear here live.',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13,
                    color: AppColors.muted,
                  ),
                ),
              ],
            )
          : ListView.separated(
              padding: const EdgeInsets.symmetric(vertical: 8),
              itemCount: state.items.length,
              separatorBuilder: (_, _) => Divider(
                height: 1,
                indent: 72,
                color: scheme.outline.withAlpha(60),
              ),
              itemBuilder: (context, index) => _NotificationTile(
                notification: state.items[index],
                onTap: () => controller.markRead(state.items[index]),
              ),
            ),
    );
  }
}

class _StatusBar extends StatelessWidget {
  const _StatusBar({required this.status});

  final HubStatus status;

  @override
  Widget build(BuildContext context) {
    final (color, label, icon) = switch (status) {
      HubStatus.connected => (AppColors.green, 'Live', Icons.bolt),
      HubStatus.connecting => (AppColors.amber, 'Connecting...', Icons.sync),
      HubStatus.disconnected => (AppColors.disabled, 'Offline', Icons.cloud_off),
    };
    return Container(
      height: 32,
      color: Theme.of(context).colorScheme.surface,
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 14, color: color),
          const SizedBox(width: 6),
          Text(
            label,
            style: GoogleFonts.instrumentSans(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: color,
            ),
          ),
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

    return Material(
      color: notification.read ? null : const Color(0xFF4F46E5).withAlpha(12),
      child: ListTile(
        leading: CircleAvatar(
          radius: 20,
          backgroundColor: const Color(0xFF4F46E5).withAlpha(25),
          child: Icon(Icons.receipt_long, color: const Color(0xFF4F46E5), size: 20),
        ),
        title: Text(
          'New sale ${sale.soNumber}',
          style: GoogleFonts.instrumentSans(
            fontSize: 13.5,
            fontWeight: notification.read ? FontWeight.w500 : FontWeight.w700,
          ),
        ),
        subtitle: Text(
          '${sale.customerName.isEmpty ? 'Walk-in' : sale.customerName} \u2022 '
          '${formatCurrency(sale.grandTotal, currency: sale.currency)} \u2022 '
          '${sale.saleChannel}',
          style: GoogleFonts.instrumentSans(
            fontSize: 11.5,
            color: AppColors.muted,
          ),
        ),
        trailing: Text(
          formatRelative(sale.occurredAt),
          style: GoogleFonts.instrumentSans(
            fontSize: 11,
            color: AppColors.disabled,
          ),
        ),
        onTap: onTap,
      ),
    );
  }
}
