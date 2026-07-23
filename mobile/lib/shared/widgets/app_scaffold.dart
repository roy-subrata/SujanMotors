import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/app_language_controller.dart';
import '../../core/i18n/strings.dart';
import '../../core/theme/app_theme.dart';
import '../../core/theme/theme_mode_controller.dart';
import '../../features/auth/auth_controller.dart';
import 'notification_bell.dart';

/// Application shell: white AppBar + optional 5-slot bottom nav + side drawer.
class AppScaffold extends ConsumerWidget {
  const AppScaffold({
    super.key,
    required this.body,
    this.title,
    this.titleWidget,
    this.actions = const [],
    this.floatingActionButton,
    this.showBottomNav = false,
    this.showNotificationBell = true,
    this.showCartBadge = false,
    this.cartCount = 0,
    this.onCartTap,
  });

  final Widget body;
  final String? title;
  final Widget? titleWidget;
  final List<Widget> actions;
  final Widget? floatingActionButton;
  final bool showBottomNav;
  final bool showNotificationBell;
  final bool showCartBadge;
  final int cartCount;
  final VoidCallback? onCartTap;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final location = GoRouterState.of(context).matchedLocation;
    final themeMode = ref.watch(themeModeProvider);
    final platformBrightness = MediaQuery.platformBrightnessOf(context);
    final isDark = themeMode == ThemeMode.dark ||
        (themeMode == ThemeMode.system &&
            platformBrightness == Brightness.dark);

    return Scaffold(
      backgroundColor: Theme.of(context).scaffoldBackgroundColor,
      drawer: const _AppDrawer(),
      appBar: AppBar(
        leading: Builder(
          builder: (ctx) => IconButton(
            icon: const Icon(Icons.menu_rounded),
            onPressed: () => Scaffold.of(ctx).openDrawer(),
          ),
        ),
        automaticallyImplyLeading: false,
        title: titleWidget ?? (title != null ? Text(title!) : null),
        actions: [
          ...actions,
          // Theme toggle — sun toggles to light, moon toggles to dark
          IconButton(
            tooltip: isDark
                ? S.of(context).switchToLight
                : S.of(context).switchToDark,
            icon: Icon(
              isDark ? Icons.light_mode_outlined : Icons.dark_mode_outlined,
              size: 21,
            ),
            onPressed: () => ref
                .read(themeModeProvider.notifier)
                .setMode(isDark ? ThemeMode.light : ThemeMode.dark),
          ),
          if (showNotificationBell) const NotificationBell(),
          if (showCartBadge)
            Stack(
              alignment: Alignment.topRight,
              children: [
                IconButton(
                  icon: const Icon(Icons.shopping_bag_outlined),
                  onPressed: onCartTap,
                ),
                if (cartCount > 0)
                  Positioned(
                    top: 6,
                    right: 6,
                    child: Container(
                      constraints: const BoxConstraints(minWidth: 16),
                      height: 16,
                      padding: const EdgeInsets.symmetric(horizontal: 4),
                      decoration: BoxDecoration(
                        color: Theme.of(context).colorScheme.onSurface,
                        borderRadius: BorderRadius.circular(99),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        '$cartCount',
                        style: GoogleFonts.instrumentSans(
                          color: Theme.of(context).colorScheme.surface,
                          fontSize: 9.5,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                    ),
                  ),
              ],
            ),
          const SizedBox(width: 4),
        ],
      ),
      body: body,
      floatingActionButton: floatingActionButton,
      bottomNavigationBar:
          showBottomNav ? _AppBottomNav(currentLocation: location) : null,
    );
  }
}

// ── Drawer ────────────────────────────────────────────────────────────────────

class _AppDrawer extends ConsumerWidget {
  const _AppDrawer();

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final session = ref.watch(authControllerProvider).asData?.value;
    final scheme = Theme.of(context).colorScheme;
    final loc = GoRouterState.of(context).matchedLocation;
    final s = S.of(context);

    final navItems = [
      (icon: Icons.home_outlined, label: s.dashboard, route: '/'),
      (icon: Icons.inventory_2_outlined, label: s.products, route: '/products'),
      (icon: Icons.people_alt_outlined, label: s.customers, route: '/customers'),
      (icon: Icons.receipt_long_outlined, label: s.sales, route: '/sales'),
      (icon: Icons.store_outlined, label: s.suppliers, route: '/suppliers'),
      (
        icon: Icons.account_balance_wallet_outlined,
        label: s.cashBook,
        route: '/cashbook'
      ),
      (
        icon: Icons.move_to_inbox_outlined,
        label: s.stockIn,
        route: '/stock-in'
      ),
      (
        icon: Icons.notifications_outlined,
        label: s.notifications,
        route: '/notifications'
      ),
    ];

    return Drawer(
      child: Column(
        children: [
          // ── Brand header (no user info) ─────────────────────────────
          Container(
            width: double.infinity,
            decoration: const BoxDecoration(gradient: AppGradients.brand),
            child: SafeArea(
              bottom: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 18, 20, 18),
                child: Row(
                  children: [
                    Container(
                      width: 36,
                      height: 36,
                      decoration: BoxDecoration(
                        color: Colors.white.withAlpha(25),
                        borderRadius: BorderRadius.circular(9),
                        border:
                            Border.all(color: Colors.white.withAlpha(40)),
                      ),
                      child: const Icon(
                        Icons.directions_car_outlined,
                        color: Colors.white,
                        size: 18,
                      ),
                    ),
                    const SizedBox(width: 10),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          'SujanMotors',
                          style: GoogleFonts.instrumentSans(
                            color: Colors.white,
                            fontSize: 16,
                            fontWeight: FontWeight.w800,
                          ),
                        ),
                        Text(
                          'Auto Parts',
                          style: GoogleFonts.instrumentSans(
                            color: Colors.white54,
                            fontSize: 11,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),

          // ── Nav items ───────────────────────────────────────────────
          Expanded(
            child: ListView(
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: [
                ...navItems.map((item) {
                  final active = loc == item.route;
                  return ListTile(
                    dense: true,
                    leading: Icon(
                      item.icon,
                      size: 20,
                      color: active
                          ? scheme.primary
                          : scheme.onSurface.withAlpha(160),
                    ),
                    title: Text(
                      item.label,
                      style: GoogleFonts.instrumentSans(
                        fontSize: 14,
                        fontWeight:
                            active ? FontWeight.w700 : FontWeight.w500,
                        color: active ? scheme.primary : scheme.onSurface,
                      ),
                    ),
                    selected: active,
                    selectedTileColor: scheme.primary.withAlpha(18),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(10)),
                    contentPadding: const EdgeInsets.symmetric(
                        horizontal: 16, vertical: 0),
                    onTap: () {
                      Navigator.of(context).pop();
                      context.go(item.route);
                    },
                  );
                }),
              ],
            ),
          ),

          // ── Language toggle ──────────────────────────────────────────
          const Divider(height: 1),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
            child: Row(
              children: [
                Icon(Icons.translate_rounded,
                    size: 18, color: scheme.onSurface.withAlpha(160)),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(
                    s.language,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w500,
                      color: scheme.onSurface,
                    ),
                  ),
                ),
                _LanguageToggle(scheme: scheme),
              ],
            ),
          ),

          // ── User profile + logout ────────────────────────────────────
          const Divider(height: 1),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
              child: Row(
                children: [
                  CircleAvatar(
                    radius: 18,
                    backgroundColor: scheme.outline.withAlpha(50),
                    child: Text(
                      _initials(
                          session?.displayName ?? session?.username ?? '?'),
                      style: GoogleFonts.instrumentSans(
                        color: scheme.onSurface.withAlpha(180),
                        fontWeight: FontWeight.w700,
                        fontSize: 12,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          session?.displayName ??
                              session?.username ??
                              s.staffLabel,
                          style: GoogleFonts.instrumentSans(
                            fontSize: 13,
                            fontWeight: FontWeight.w600,
                            color: scheme.onSurface,
                          ),
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                        ),
                        if ((session?.email ?? '').isNotEmpty)
                          Text(
                            session!.email!,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 11,
                              color: scheme.onSurface.withAlpha(120),
                            ),
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                          ),
                      ],
                    ),
                  ),
                  IconButton(
                    tooltip: s.logOut,
                    icon: Icon(Icons.logout_rounded,
                        size: 20, color: context.colors.red),
                    onPressed: () async {
                      Navigator.of(context).pop();
                      await ref
                          .read(authControllerProvider.notifier)
                          .logout();
                    },
                  ),
                ],
              ),
            ),
          ),

        ],
      ),
    );
  }

  String _initials(String name) {
    final parts = name.trim().split(RegExp(r'\s+'));
    if (parts.isEmpty || parts.first.isEmpty) return '?';
    if (parts.length == 1) return parts.first[0].toUpperCase();
    return (parts.first[0] + parts.last[0]).toUpperCase();
  }
}

/// EN / বাং segmented switch persisted via [appLanguageProvider].
class _LanguageToggle extends ConsumerWidget {
  const _LanguageToggle({required this.scheme});

  final ColorScheme scheme;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final locale = ref.watch(appLanguageProvider);

    Widget cell(String label, Locale value) {
      final selected = locale.languageCode == value.languageCode;
      return GestureDetector(
        onTap: () =>
            ref.read(appLanguageProvider.notifier).setLocale(value),
        child: Container(
          padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
          decoration: BoxDecoration(
            color: selected ? scheme.primary : Colors.transparent,
            borderRadius: BorderRadius.circular(8),
          ),
          child: Text(
            label,
            style: GoogleFonts.instrumentSans(
              fontSize: 12,
              fontWeight: selected ? FontWeight.w700 : FontWeight.w500,
              color: selected
                  ? scheme.onPrimary
                  : scheme.onSurface.withAlpha(160),
            ),
          ),
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(2),
      decoration: BoxDecoration(
        border: Border.all(color: scheme.outline),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          cell('EN', AppLanguageController.english),
          cell('বাং', AppLanguageController.bengali),
        ],
      ),
    );
  }
}

// ── Bottom navigation bar ─────────────────────────────────────────────────────

class _AppBottomNav extends StatelessWidget {
  const _AppBottomNav({required this.currentLocation});

  final String currentLocation;

  static const _routes = ['/', '/products', '/customers', '/sales'];

  int get _idx {
    final i = _routes.indexOf(currentLocation);
    return i < 0 ? 0 : i;
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final surfaceColor = scheme.surface;

    return Container(
      decoration: BoxDecoration(
        color: surfaceColor,
        border:
            Border(top: BorderSide(color: scheme.outline.withAlpha(80))),
      ),
      child: SafeArea(
        top: false,
        child: SizedBox(
          height: 64,
          child: Stack(
            alignment: Alignment.center,
            children: [
              Row(
                children: [
                  _NavItem(
                    icon: Icons.home_outlined,
                    activeIcon: Icons.home_rounded,
                    label: S.of(context).home,
                    index: 0,
                    current: _idx,
                    onTap: () => context.go('/'),
                  ),
                  _NavItem(
                    icon: Icons.grid_view_outlined,
                    activeIcon: Icons.grid_view_rounded,
                    label: S.of(context).products,
                    index: 1,
                    current: _idx,
                    onTap: () => context.go('/products'),
                  ),
                  const Expanded(child: SizedBox()),
                  _NavItem(
                    icon: Icons.person_outline_rounded,
                    activeIcon: Icons.person_rounded,
                    label: S.of(context).customers,
                    index: 2,
                    current: _idx,
                    onTap: () => context.go('/customers'),
                  ),
                  _NavItem(
                    icon: Icons.receipt_long_outlined,
                    activeIcon: Icons.receipt_long_rounded,
                    label: S.of(context).sales,
                    index: 3,
                    current: _idx,
                    onTap: () => context.go('/sales'),
                  ),
                ],
              ),
              GestureDetector(
                onTap: () => context.go('/quick-sale'),
                child: Container(
                  width: 48,
                  height: 48,
                  decoration: BoxDecoration(
                    color: scheme.primary,
                    borderRadius: BorderRadius.circular(14),
                    border: Border.all(color: surfaceColor, width: 3),
                    boxShadow: [
                      BoxShadow(
                        color: scheme.primary.withAlpha(60),
                        blurRadius: 24,
                        offset: const Offset(0, 8),
                      ),
                    ],
                  ),
                  alignment: Alignment.center,
                  child:
                      Icon(Icons.add, color: scheme.onPrimary, size: 24),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

class _NavItem extends StatelessWidget {
  const _NavItem({
    required this.icon,
    required this.activeIcon,
    required this.label,
    required this.index,
    required this.current,
    required this.onTap,
  });

  final IconData icon;
  final IconData activeIcon;
  final String label;
  final int index;
  final int current;
  final VoidCallback onTap;

  bool get _active => index == current;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Expanded(
      child: GestureDetector(
        onTap: onTap,
        behavior: HitTestBehavior.opaque,
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Icon(
              _active ? activeIcon : icon,
              size: 22,
              color: _active
                  ? scheme.primary
                  : scheme.onSurface.withAlpha(130),
            ),
            const SizedBox(height: 2),
            Text(
              label,
              style: GoogleFonts.instrumentSans(
                fontSize: 10.5,
                fontWeight:
                    _active ? FontWeight.w700 : FontWeight.w500,
                color: _active
                    ? scheme.primary
                    : scheme.onSurface.withAlpha(130),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
