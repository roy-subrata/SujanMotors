import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../core/i18n/strings.dart';
import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import '../sales/sales_repository.dart';
import 'notification_models.dart';
import 'notifications_controller.dart';

/// E5 · Notifications — realtime inbox, grouped by day. New sales pushed over
/// SignalR appear here live; tapping a row marks it read and deep-links to the
/// invoice.
class NotificationsScreen extends ConsumerWidget {
  const NotificationsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsControllerProvider);
    final controller = ref.read(notificationsControllerProvider.notifier);

    return Scaffold(
      appBar: AppBar(
        // Reached both by pushing (bell icon) and by go()-ing (drawer link,
        // which replaces the stack and leaves nothing to pop) — always fall
        // back to Home so a back affordance is guaranteed either way.
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () => context.canPop() ? context.pop() : context.go('/'),
        ),
        title: Text(S.of(context).notifications),
        actions: [
          if (state.items.any((n) => !n.read))
            TextButton(
              onPressed: controller.markAllRead,
              child: Text(
                S.of(context).markAllRead,
                style: GoogleFonts.instrumentSans(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                ),
              ),
            ),
          if (state.items.isNotEmpty)
            IconButton(
              tooltip: S.of(context).clearAll,
              icon: const Icon(Icons.delete_sweep_outlined, size: 20),
              onPressed: controller.clearAll,
            ),
        ],
      ),
      body: Column(
        children: [
          // The feed is live-only, so a broken connection means missed
          // notifications — surface it; stay quiet when all is well.
          if (state.status != HubStatus.connected)
            _ConnectionBanner(status: state.status),
          Expanded(
            child: state.items.isEmpty
                ? EmptyView(
                    message: S.of(context).noNotificationsYet,
                    icon: Icons.notifications_off_outlined,
                  )
                : ListView.builder(
                    padding: const EdgeInsets.fromLTRB(16, 4, 16, 24),
                    itemCount: state.items.length,
                    itemBuilder: (context, index) {
                      final item = state.items[index];
                      final isNewDay = index == 0 ||
                          !_sameDay(state.items[index - 1].sale.occurredAt,
                              item.sale.occurredAt);
                      return Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (isNewDay) _DayHeader(day: item.sale.occurredAt),
                          _NotificationRow(
                            notification: item,
                            onTap: () => _openSale(context, ref, item),
                          ),
                        ],
                      );
                    },
                  ),
          ),
        ],
      ),
    );
  }

  /// Deep-links a sale notification to its invoice. The realtime payload only
  /// carries the sales-order id/number, and invoice endpoints key on the
  /// invoice id — so resolve it by SO-number search first.
  Future<void> _openSale(
      BuildContext context, WidgetRef ref, AppNotification item) async {
    ref.read(notificationsControllerProvider.notifier).markRead(item);
    final messenger = ScaffoldMessenger.of(context);
    final s = S.of(context);
    final errorColor = context.colors.red;
    final router = GoRouter.of(context);
    try {
      final chunk = await ref
          .read(salesRepositoryProvider)
          .invoices(search: item.sale.soNumber, pageSize: 1);
      final invoice = chunk.items.firstOrNull;
      if (invoice == null) {
        messenger.showSnackBar(SnackBar(
          content: Text(s.invoiceNotFoundYet),
          behavior: SnackBarBehavior.floating,
        ));
        return;
      }
      router.push('/invoice/${invoice.id}', extra: invoice);
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
    }
  }

  static bool _sameDay(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;
}

// ── Connection banner ─────────────────────────────────────────────────────────

class _ConnectionBanner extends StatelessWidget {
  const _ConnectionBanner({required this.status});

  final HubStatus status;

  @override
  Widget build(BuildContext context) {
    final (bg, fg, label, icon) = switch (status) {
      HubStatus.connecting => (
          context.colors.amberBg,
          context.colors.amber,
          S.of(context).connecting,
          Icons.sync,
        ),
      _ => (
          context.colors.redBg,
          context.colors.red,
          S.of(context).offlinePaused,
          Icons.cloud_off,
        ),
    };
    return Container(
      width: double.infinity,
      color: bg,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 7),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.center,
        children: [
          Icon(icon, size: 13, color: fg),
          const SizedBox(width: 6),
          Text(
            label,
            style: GoogleFonts.instrumentSans(
              fontSize: 11.5,
              fontWeight: FontWeight.w600,
              color: fg,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Day header ────────────────────────────────────────────────────────────────

class _DayHeader extends StatelessWidget {
  const _DayHeader({required this.day});

  final DateTime day;

  @override
  Widget build(BuildContext context) {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    final thatDay = DateTime(day.year, day.month, day.day);
    final diff = today.difference(thatDay).inDays;
    final label = diff == 0
        ? S.of(context).today
        : diff == 1
            ? S.of(context).yesterday
            : DateFormat('d MMM').format(day);

    return Padding(
      padding: const EdgeInsets.fromLTRB(2, 14, 2, 8),
      child: SectionEyebrow(label: label),
    );
  }
}

// ── Notification row ──────────────────────────────────────────────────────────

class _NotificationRow extends StatelessWidget {
  const _NotificationRow({required this.notification, required this.onTap});

  final AppNotification notification;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final sale = notification.sale;
    final scheme = Theme.of(context).colorScheme;
    final who =
        sale.customerName.isEmpty ? S.of(context).walkIn : sale.customerName;

    return ListCard(
      onTap: onTap,
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Payment/sale = green ৳ square. Other types (low stock, stock-in,
          // due reminders) join this switch when a persisted feed exists.
          Container(
            width: 40,
            height: 40,
            decoration: BoxDecoration(
              color: context.colors.greenBg,
              borderRadius: BorderRadius.circular(11),
            ),
            alignment: Alignment.center,
            child: Text(
              kCurrencySymbol,
              style: GoogleFonts.instrumentSans(
                fontSize: 17,
                fontWeight: FontWeight.w700,
                color: context.colors.green,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  S.of(context).newSaleTitled(sale.soNumber),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                    color: scheme.onSurface,
                  ),
                ),
                const SizedBox(height: 2),
                Text(
                  '$who · ${formatCurrency(sale.grandTotal, currency: sale.currency)}'
                  ' · ${sale.saleChannel}',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 11.5,
                    color: context.colors.muted,
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(width: 8),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    formatRelative(sale.occurredAt, s: S.of(context)),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11,
                      color: context.colors.muted,
                    ),
                  ),
                  if (!notification.read) ...[
                    const SizedBox(width: 6),
                    Container(
                      width: 8,
                      height: 8,
                      decoration: BoxDecoration(
                        color: context.colors.red,
                        shape: BoxShape.circle,
                      ),
                    ),
                  ],
                ],
              ),
            ],
          ),
        ],
      ),
    );
  }
}
