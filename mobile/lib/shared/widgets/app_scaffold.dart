import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';

import '../../core/theme/app_theme.dart';
import 'app_drawer.dart';
import 'notification_bell.dart';

class AppScaffold extends StatelessWidget {
  const AppScaffold({
    super.key,
    required this.title,
    required this.body,
    this.titleWidget,
    this.actions = const [],
    this.floatingActionButton,
    this.showNotificationBell = true,
    this.showBottomNav = false,
  });

  final String title;

  /// When set, replaces the [title] string in the AppBar. Useful for screens
  /// that need to put a search TextField in the AppBar.
  final Widget? titleWidget;

  final Widget body;
  final List<Widget> actions;
  final Widget? floatingActionButton;
  final bool showNotificationBell;
  final bool showBottomNav;

  static const _navRoutes = ['/', '/products', '/quick-sale', '/customers'];

  int _selectedIndex(String location) {
    final idx = _navRoutes.indexOf(location);
    return idx < 0 ? 0 : idx;
  }

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    final canPop = context.canPop(); // go_router-aware; Navigator.of() finds wrong navigator

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: titleWidget ?? Text(title),
        // Show back button automatically when there is a route to pop;
        // only show the drawer hamburger on root screens.
        leading: canPop
            ? IconButton(
                icon: const Icon(Icons.arrow_back),
                onPressed: () => context.pop(),
              )
            : null,
        actions: [
          ...actions,
          if (showNotificationBell) const NotificationBell(),
          const SizedBox(width: 4),
        ],
      ),
      drawer: canPop ? null : const AppDrawer(),
      body: body,
      floatingActionButton: floatingActionButton,
      bottomNavigationBar: showBottomNav
          ? NavigationBar(
              selectedIndex: _selectedIndex(location),
              onDestinationSelected: (i) => context.go(_navRoutes[i]),
              destinations: const [
                NavigationDestination(
                  icon: Icon(Icons.home_outlined),
                  selectedIcon: Icon(Icons.home),
                  label: 'Home',
                ),
                NavigationDestination(
                  icon: Icon(Icons.inventory_2_outlined),
                  selectedIcon: Icon(Icons.inventory_2),
                  label: 'Products',
                ),
                NavigationDestination(
                  icon: Icon(Icons.point_of_sale_outlined),
                  selectedIcon: Icon(Icons.point_of_sale),
                  label: 'Quick Sale',
                ),
                NavigationDestination(
                  icon: Icon(Icons.people_alt_outlined),
                  selectedIcon: Icon(Icons.people_alt),
                  label: 'Customers',
                ),
              ],
            )
          : null,
    );
  }
}
