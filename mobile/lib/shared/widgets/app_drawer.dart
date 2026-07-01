import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_theme.dart';
import '../../features/auth/auth_controller.dart';
import '../../features/notifications/notifications_controller.dart';

/// A navigation destination shown in the sidebar. Add a new feature by adding
/// one entry to [_destinations] below.
class _NavItem {
  const _NavItem(this.label, this.icon, this.route);
  final String label;
  final IconData icon;
  final String route;
}

const _destinations = <_NavItem>[
  _NavItem('Products', Icons.inventory_2_outlined, '/products'),
  _NavItem('Cash Book', Icons.account_balance_wallet_outlined, '/cashbook'),
  _NavItem('Notifications', Icons.notifications_outlined, '/notifications'),
];

/// App-wide sidebar. Shows the signed-in staff member, the primary navigation,
/// and a sign-out action. Designed to grow as features are added.
class AppDrawer extends ConsumerWidget {
  const AppDrawer({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).asData?.value;
    final unread = ref.watch(
      notificationsControllerProvider.select((s) => s.unreadCount),
    );
    final current = GoRouterState.of(context).matchedLocation;
    final scheme = Theme.of(context).colorScheme;

    return Drawer(
      child: Column(
        children: [
          UserAccountsDrawerHeader(
            decoration: const BoxDecoration(gradient: AppGradients.brand),
            currentAccountPicture: CircleAvatar(
              backgroundColor: Colors.white,
              child: Text(
                _initials(session?.displayName ?? '?'),
                style: TextStyle(
                  color: scheme.primary,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            accountName: Text(session?.displayName ?? 'Staff'),
            accountEmail: Text(
              session?.roles.isNotEmpty == true
                  ? session!.roles.join(', ')
                  : (session?.email ?? ''),
            ),
          ),
          Expanded(
            child: ListView(
              padding: EdgeInsets.zero,
              children: [
                for (final item in _destinations)
                  ListTile(
                    leading: Icon(item.icon),
                    title: Text(item.label),
                    selected: current == item.route,
                    selectedTileColor: scheme.primaryContainer.withValues(
                      alpha: 0.4,
                    ),
                    trailing: item.route == '/notifications' && unread > 0
                        ? Badge(label: Text('$unread'))
                        : null,
                    onTap: () {
                      Scaffold.of(context).closeDrawer();
                      if (current != item.route) context.go(item.route);
                    },
                  ),
              ],
            ),
          ),
          const Divider(height: 1),
          ListTile(
            leading: Icon(Icons.logout, color: scheme.error),
            title: Text('Sign out', style: TextStyle(color: scheme.error)),
            onTap: () {
              Scaffold.of(context).closeDrawer();
              ref.read(authControllerProvider.notifier).logout();
            },
          ),
          const SizedBox(height: 8),
        ],
      ),
    );
  }

  String _initials(String name) {
    final parts =
        name.trim().split(RegExp(r'\s+')).where((p) => p.isNotEmpty).toList();
    if (parts.isEmpty) return '?';
    if (parts.length == 1) return parts.first.characters.first.toUpperCase();
    return (parts.first.characters.first + parts.last.characters.first)
        .toUpperCase();
  }
}
