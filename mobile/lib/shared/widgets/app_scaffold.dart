import 'package:flutter/material.dart';

import '../../core/theme/app_theme.dart';
import 'app_drawer.dart';
import 'notification_bell.dart';

/// Standard chrome for top-level screens: a consistent header (with the
/// notification bell) and the app sidebar. Page-specific [actions] are inserted
/// before the bell so every screen shares the same realtime entry point.
class AppScaffold extends StatelessWidget {
  const AppScaffold({
    super.key,
    required this.title,
    required this.body,
    this.actions = const [],
    this.floatingActionButton,
    this.showNotificationBell = true,
  });

  final String title;
  final Widget body;
  final List<Widget> actions;
  final Widget? floatingActionButton;
  final bool showNotificationBell;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: Text(title),
        actions: [
          ...actions,
          if (showNotificationBell) const NotificationBell(),
          const SizedBox(width: 4),
        ],
      ),
      drawer: const AppDrawer(),
      body: body,
      floatingActionButton: floatingActionButton,
    );
  }
}
