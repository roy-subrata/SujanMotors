import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/i18n/strings.dart';
import '../../features/notifications/notifications_controller.dart';

/// AppBar action: a bell with an unread badge and a tiny live-connection dot.
/// Tapping it opens the realtime notifications inbox.
class NotificationBell extends ConsumerWidget {
  const NotificationBell({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(notificationsControllerProvider);
    final unread = state.unreadCount;
    final connected = state.status == HubStatus.connected;

    return IconButton(
      tooltip: S.of(context).notifications,
      onPressed: () => context.push('/notifications'),
      icon: Badge(
        isLabelVisible: unread > 0,
        label: Text(unread > 99 ? '99+' : '$unread'),
        child: Stack(
          clipBehavior: Clip.none,
          children: [
            const Icon(Icons.notifications_outlined),
            Positioned(
              right: -1,
              bottom: -1,
              child: Container(
                width: 8,
                height: 8,
                decoration: BoxDecoration(
                  color: connected ? Colors.greenAccent.shade400 : Colors.grey,
                  shape: BoxShape.circle,
                  border: Border.all(
                    color: Theme.of(context).colorScheme.surface,
                    width: 1.5,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
