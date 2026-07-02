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

  // Routes that permanently own the drawer. Checked against matchedLocation so
  // the drawer widget is never torn down mid-transition (which caused a blink).
  static const _drawerRoutes = <String>{
    '/',
    '/products',
    '/quick-sale',
    '/customers',
    '/cashbook',
    '/stock-in',
    '/notifications',
  };

  int _selectedIndex(String location) {
    final idx = _navRoutes.indexOf(location);
    return idx < 0 ? 0 : idx;
  }

  @override
  Widget build(BuildContext context) {
    final location = GoRouterState.of(context).matchedLocation;
    final isDrawerRoute = _drawerRoutes.contains(location);
    final canPop = context.canPop();

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: titleWidget ?? Text(title),
        leading: (canPop && !isDrawerRoute)
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
      drawer: isDrawerRoute ? const AppDrawer() : null,
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
