import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/i18n/strings.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/dashboard.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/state_views.dart';
import 'dashboard_providers.dart';

// Brand accent used throughout the dashboard (works on both light + dark).
const _kAccent = Color(0xFF4F46E5);

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(dashboardControllerProvider);
    final ctrl = ref.read(dashboardControllerProvider.notifier);

    return AppScaffold(
      titleWidget: const _BrandTitle(),
      showBottomNav: true,
      body: Column(
        children: [
          _DateRangeBar(
            start: state.rangeStart,
            end: state.rangeEnd,
            onPick: () async {
              final now = DateTime.now();
              final picked = await showDateRangePicker(
                context: context,
                firstDate: DateTime(now.year - 5),
                lastDate: DateTime(now.year + 1),
                initialDateRange: DateTimeRange(
                  start: state.rangeStart,
                  end: state.rangeEnd,
                ),
              );
              if (picked != null) {
                ctrl.load(start: picked.start, end: picked.end);
              }
            },
          ),
          Expanded(
            child: state.isLoading
                ? const LoadingView()
                : state.error != null
                    ? ErrorView(
                        message: state.error!,
                        onRetry: () => ctrl.load(),
                      )
                    : _DashboardBody(
                        data: state.data!,
                        onRefresh: () async => ctrl.load(),
                      ),
          ),
        ],
      ),
    );
  }
}

// ── Brand title ───────────────────────────────────────────────────────────────

class _BrandTitle extends StatelessWidget {
  const _BrandTitle();

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Container(
          width: 28,
          height: 28,
          decoration: BoxDecoration(
            gradient: AppGradients.brand,
            borderRadius: BorderRadius.circular(7),
          ),
          child: const Icon(Icons.directions_car_outlined,
              color: Colors.white, size: 15),
        ),
        const SizedBox(width: 9),
        Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              'SujanMotors',
              style: GoogleFonts.instrumentSans(
                fontSize: 16,
                fontWeight: FontWeight.w800,
                color: scheme.onSurface,
                height: 1.1,
              ),
            ),
            Text(
              'Auto Parts',
              style: GoogleFonts.instrumentSans(
                fontSize: 9.5,
                fontWeight: FontWeight.w500,
                color: scheme.onSurface.withAlpha(120),
                letterSpacing: 0.2,
              ),
            ),
          ],
        ),
      ],
    );
  }
}

// ── Date range bar ────────────────────────────────────────────────────────────

class _DateRangeBar extends StatelessWidget {
  const _DateRangeBar({
    required this.start,
    required this.end,
    required this.onPick,
  });

  final DateTime start;
  final DateTime end;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final sameDay = start.year == end.year &&
        start.month == end.month &&
        start.day == end.day;
    final label = sameDay
        ? formatDate(start)
        : '${formatDate(start)}  –  ${formatDate(end)}';

    return Container(
      height: 44,
      color: scheme.surface,
      child: InkWell(
        onTap: onPick,
        child: Padding(
          padding: const EdgeInsets.symmetric(horizontal: 16),
          child: Row(
            children: [
              Icon(Icons.date_range_outlined,
                  size: 17, color: scheme.onSurface.withAlpha(160)),
              const SizedBox(width: 8),
              Text(
                label,
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: scheme.onSurface,
                ),
              ),
              const Spacer(),
              Icon(Icons.arrow_drop_down,
                  color: scheme.onSurface.withAlpha(120)),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Dashboard body ────────────────────────────────────────────────────────────

class _DashboardBody extends StatelessWidget {
  const _DashboardBody({
    required this.data,
    required this.onRefresh,
  });

  final DashboardData data;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final s = data.summary;
    final scheme = Theme.of(context).colorScheme;

    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(12, 14, 12, 24),
        children: [
          _RevenueCard(summary: s),
          const SizedBox(height: 12),
          _StatGrid(summary: s),
          const SizedBox(height: 12),
          _CashFlowCard(summary: s),
          const SizedBox(height: 16),
          _SectionLabel(S.of(context).quickActions, scheme: scheme),
          const SizedBox(height: 8),
          _QuickActions(),
          const SizedBox(height: 16),
          if (data.topProducts.isNotEmpty) ...[
            _SectionLabel(S.of(context).topProducts, scheme: scheme),
            const SizedBox(height: 8),
            _TopProductsList(products: data.topProducts),
            const SizedBox(height: 16),
          ],
          if (data.topCustomers.isNotEmpty) ...[
            _SectionLabel(S.of(context).topCustomers, scheme: scheme),
            const SizedBox(height: 8),
            _TopCustomersList(customers: data.topCustomers),
            const SizedBox(height: 16),
          ],
          if (s.lowStockItemsCount > 0)
            _LowStockAlert(count: s.lowStockItemsCount),
        ],
      ),
    );
  }
}

// ── Revenue headline card ─────────────────────────────────────────────────────

class _RevenueCard extends StatelessWidget {
  const _RevenueCard({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final profit = summary.netProfit;
    final isPositive = profit >= 0;

    return Card(
      elevation: 0,
      color: _kAccent,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 18, 20, 18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.point_of_sale,
                    color: Colors.white70, size: 16),
                const SizedBox(width: 6),
                Text(
                  S.of(context).totalRevenue,
                  style: const TextStyle(
                    color: Colors.white70,
                    fontSize: 13,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const Spacer(),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
                  decoration: BoxDecoration(
                    color: Colors.white.withAlpha(30),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    S.of(context).ordersCount(summary.totalSalesCount),
                    style: const TextStyle(
                        color: Colors.white70, fontSize: 11),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              formatCurrency(summary.totalSales),
              style: const TextStyle(
                color: Colors.white,
                fontSize: 30,
                fontWeight: FontWeight.w800,
                letterSpacing: -0.5,
              ),
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                _MiniStat(
                    label: S.of(context).cash,
                    value: formatCurrency(summary.cashSales),
                    color: Colors.white,
                    subtitle: S.of(context).paid),
                const SizedBox(width: 20),
                _MiniStat(
                    label: S.of(context).credit,
                    value: formatCurrency(summary.creditSales),
                    color: Colors.white70,
                    subtitle: S.of(context).due),
                const Spacer(),
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: isPositive
                        ? Colors.green.shade400.withAlpha(50)
                        : Colors.red.shade300.withAlpha(50),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(
                      color: isPositive
                          ? Colors.green.shade300
                          : Colors.red.shade300,
                    ),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        isPositive
                            ? Icons.arrow_upward
                            : Icons.arrow_downward,
                        size: 12,
                        color: isPositive
                            ? Colors.green.shade200
                            : Colors.red.shade200,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        '${summary.profitMargin.toStringAsFixed(1)}%',
                        style: TextStyle(
                          color: isPositive
                              ? Colors.green.shade200
                              : Colors.red.shade200,
                          fontSize: 12,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _MiniStat extends StatelessWidget {
  const _MiniStat(
      {required this.label, required this.value, required this.color, this.subtitle});

  final String label;
  final String value;
  final Color color;
  final String? subtitle;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: const TextStyle(color: Colors.white54, fontSize: 10)),
          Text(value,
              style: TextStyle(
                  color: color, fontSize: 12, fontWeight: FontWeight.w600)),
          if (subtitle != null)
            Text(subtitle!,
                style: const TextStyle(color: Colors.white38, fontSize: 9)),
        ],
      );
}

// ── 2×2 Stat grid ─────────────────────────────────────────────────────────────

class _StatGrid extends StatelessWidget {
  const _StatGrid({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final profit = summary.netProfit;

    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10,
      crossAxisSpacing: 10,
      childAspectRatio: 1.7,
      children: [
        _StatCard(
          label: S.of(context).grossProfit,
          value: formatCurrency(summary.grossProfit),
          icon: Icons.trending_up,
          accent: const Color(0xFF059669),
        ),
        _StatCard(
          label: S.of(context).netProfit,
          value: formatCurrency(profit),
          icon: profit >= 0 ? Icons.show_chart : Icons.trending_down,
          accent: profit >= 0
              ? const Color(0xFF0D9488)
              : const Color(0xFFDC2626),
        ),
        _StatCard(
          label: S.of(context).customerDue,
          value: formatCurrency(summary.customerDueAmount),
          badge: summary.customerDueCount > 0
              ? '${summary.customerDueCount}'
              : null,
          icon: Icons.account_balance_wallet_outlined,
          accent: const Color(0xFFD97706),
        ),
        _StatCard(
          label: S.of(context).overdue,
          value: formatCurrency(summary.customerOverdueAmount),
          badge: summary.customerOverdueCount > 0
              ? '${summary.customerOverdueCount}'
              : null,
          icon: Icons.warning_amber_outlined,
          accent: const Color(0xFFDC2626),
        ),
      ],
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.label,
    required this.value,
    required this.icon,
    required this.accent,
    this.badge,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color accent;
  final String? badge;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    final cardBg = isDark ? accent.withAlpha(25) : accent.withAlpha(18);
    final cardBorder = accent.withAlpha(isDark ? 40 : 30);

    return Container(
      decoration: BoxDecoration(
        color: cardBg,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: cardBorder),
      ),
      padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Row(
            children: [
              Icon(icon, color: accent, size: 18),
              const Spacer(),
              if (badge != null)
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 6, vertical: 2),
                  decoration: BoxDecoration(
                    color: accent.withAlpha(30),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(
                    badge!,
                    style: TextStyle(
                        fontSize: 10,
                        color: accent,
                        fontWeight: FontWeight.w700),
                  ),
                ),
            ],
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                label,
                style: GoogleFonts.instrumentSans(
                    fontSize: 11,
                    color: scheme.onSurface.withAlpha(160),
                    fontWeight: FontWeight.w500),
              ),
              const SizedBox(height: 2),
              Text(
                value,
                style: GoogleFonts.instrumentSans(
                    fontSize: 14,
                    color: accent,
                    fontWeight: FontWeight.w800),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Cash flow card ─────────────────────────────────────────────────────────────

class _CashFlowCard extends StatelessWidget {
  const _CashFlowCard({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Container(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: scheme.outline.withAlpha(80)),
      ),
      padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Icon(Icons.account_balance_outlined,
                  size: 15, color: scheme.onSurface.withAlpha(160)),
              const SizedBox(width: 6),
              Text(
                S.of(context).cashFlow,
                style: GoogleFonts.instrumentSans(
                  fontSize: 13,
                  fontWeight: FontWeight.w700,
                  color: scheme.onSurface,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              _FlowItem(
                  label: S.of(context).opening,
                  value: summary.openingBalance,
                  color: scheme.onSurface.withAlpha(180)),
              _Arrow(scheme: scheme),
              _FlowItem(
                  label: S.of(context).flowIn,
                  value: summary.cashInflow,
                  color: const Color(0xFF059669)),
              _Arrow(scheme: scheme),
              _FlowItem(
                  label: S.of(context).flowOut,
                  value: summary.cashOutflow,
                  color: const Color(0xFFDC2626)),
              _Arrow(scheme: scheme),
              _FlowItem(
                  label: S.of(context).closing,
                  value: summary.closingBalance,
                  color: _kAccent,
                  bold: true),
            ],
          ),
        ],
      ),
    );
  }
}

class _Arrow extends StatelessWidget {
  const _Arrow({required this.scheme});
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) =>
      Icon(Icons.arrow_forward_ios,
          size: 10, color: scheme.onSurface.withAlpha(80));
}

class _FlowItem extends StatelessWidget {
  const _FlowItem(
      {required this.label,
      required this.value,
      required this.color,
      this.bold = false});

  final String label;
  final double value;
  final Color color;
  final bool bold;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Column(
      children: [
        Text(label,
            style: GoogleFonts.instrumentSans(
                fontSize: 10, color: scheme.onSurface.withAlpha(120))),
        const SizedBox(height: 2),
        Text(
          formatCurrency(value),
          style: GoogleFonts.instrumentSans(
            fontSize: 12,
            color: color,
            fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
          ),
        ),
      ],
    );
  }
}

// ── Quick actions ─────────────────────────────────────────────────────────────

class _QuickActions extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return GridView.count(
      crossAxisCount: 3,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10,
      crossAxisSpacing: 10,
      childAspectRatio: 1.5,
      children: [
        _ActionTile(
          label: S.of(context).newSale,
          icon: Icons.point_of_sale,
          accent: const Color(0xFFF59E0B),
          onTap: () => context.push('/quick-sale'),
        ),
        _ActionTile(
          label: S.of(context).customers,
          icon: Icons.people_alt_outlined,
          accent: _kAccent,
          onTap: () => context.push('/customers'),
        ),
        _ActionTile(
          label: S.of(context).products,
          icon: Icons.inventory_2_outlined,
          accent: const Color(0xFF0891B2),
          onTap: () => context.push('/products'),
        ),
        _ActionTile(
          label: S.of(context).suppliers,
          icon: Icons.store_outlined,
          accent: const Color(0xFF7C3AED),
          onTap: () => context.push('/suppliers'),
        ),
        _ActionTile(
          label: S.of(context).cashBook,
          icon: Icons.account_balance_wallet_outlined,
          accent: const Color(0xFF0D9488),
          onTap: () => context.push('/cashbook'),
        ),
        _ActionTile(
          label: S.of(context).stockIn,
          icon: Icons.move_to_inbox_outlined,
          accent: const Color(0xFF059669),
          onTap: () => context.push('/stock-in'),
        ),
        _ActionTile(
          label: S.of(context).tillSession,
          icon: Icons.lock_clock_outlined,
          accent: const Color(0xFF334155),
          onTap: () => context.push('/till-session'),
        ),
      ],
    );
  }
}

class _ActionTile extends StatelessWidget {
  const _ActionTile({
    required this.label,
    required this.icon,
    required this.accent,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final Color accent;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Material(
      color: isDark ? accent.withAlpha(30) : accent.withAlpha(18),
      borderRadius: BorderRadius.circular(14),
      child: InkWell(
        borderRadius: BorderRadius.circular(14),
        onTap: onTap,
        child: Container(
          decoration: BoxDecoration(
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: accent.withAlpha(isDark ? 50 : 35)),
          ),
          padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 12),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Icon(icon, color: accent, size: 22),
              const SizedBox(height: 6),
              Text(
                label,
                style: GoogleFonts.instrumentSans(
                  color: isDark ? scheme.onSurface : accent.withAlpha(220),
                  fontSize: 11.5,
                  fontWeight: FontWeight.w700,
                ),
                textAlign: TextAlign.center,
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Top products list ─────────────────────────────────────────────────────────

class _TopProductsList extends StatelessWidget {
  const _TopProductsList({required this.products});

  final List<TopProduct> products;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: scheme.outline.withAlpha(80)),
      ),
      child: Column(
        children: products.take(5).indexed.map((entry) {
          final (idx, p) = entry;
          final isLast = idx == (products.length > 5 ? 4 : products.length - 1);
          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: 16, vertical: 11),
                child: Row(
                  children: [
                    Container(
                      width: 26,
                      height: 26,
                      decoration: BoxDecoration(
                        color: _rankColor(idx).withAlpha(30),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      alignment: Alignment.center,
                      child: Text(
                        '${idx + 1}',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w800,
                          color: _rankColor(idx),
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            p.partName,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: scheme.onSurface,
                            ),
                          ),
                          if (p.partNumber.isNotEmpty)
                            Text(
                              p.partNumber,
                              style: GoogleFonts.instrumentSans(
                                fontSize: 11,
                                color: scheme.onSurface.withAlpha(130),
                              ),
                            ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          '×${p.quantitySold}',
                          style: GoogleFonts.instrumentSans(
                            fontSize: 11,
                            color: scheme.onSurface.withAlpha(130),
                          ),
                        ),
                        Text(
                          formatCurrency(p.totalRevenue),
                          style: GoogleFonts.instrumentSans(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: _kAccent,
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              if (!isLast)
                Divider(
                    height: 1,
                    indent: 54,
                    endIndent: 16,
                    color: scheme.outline.withAlpha(60)),
            ],
          );
        }).toList(),
      ),
    );
  }

  Color _rankColor(int idx) => switch (idx) {
        0 => const Color(0xFFF59E0B),
        1 => const Color(0xFF94A3B8),
        2 => const Color(0xFF92400E),
        _ => const Color(0xFF9CA3AF),
      };
}

// ── Top customers list ────────────────────────────────────────────────────────

class _TopCustomersList extends StatelessWidget {
  const _TopCustomersList({required this.customers});

  final List<TopCustomer> customers;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      decoration: BoxDecoration(
        color: scheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: scheme.outline.withAlpha(80)),
      ),
      child: Column(
        children: customers.take(3).indexed.map((entry) {
          final (idx, c) = entry;
          final isLast =
              idx == (customers.length > 3 ? 2 : customers.length - 1);
          final initial = c.customerName.trim().isEmpty
              ? '?'
              : c.customerName.trim()[0].toUpperCase();

          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: 16, vertical: 11),
                child: Row(
                  children: [
                    CircleAvatar(
                      radius: 18,
                      backgroundColor: _kAccent.withAlpha(25),
                      child: Text(
                        initial,
                        style: const TextStyle(
                          color: _kAccent,
                          fontWeight: FontWeight.w700,
                          fontSize: 13,
                        ),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            c.customerName,
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: GoogleFonts.instrumentSans(
                              fontSize: 13,
                              fontWeight: FontWeight.w600,
                              color: scheme.onSurface,
                            ),
                          ),
                          Text(
                            S.of(context).ordersCount(c.totalOrders),
                            style: GoogleFonts.instrumentSans(
                              fontSize: 11,
                              color: scheme.onSurface.withAlpha(130),
                            ),
                          ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          formatCurrency(c.totalRevenue),
                          style: GoogleFonts.instrumentSans(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: _kAccent,
                          ),
                        ),
                        if (c.outstandingAmount > 0)
                          Text(
                            '${S.of(context).due}: ${formatCurrency(c.outstandingAmount)}',
                            style: const TextStyle(
                              fontSize: 11,
                              color: Color(0xFFD97706),
                            ),
                          ),
                      ],
                    ),
                  ],
                ),
              ),
              if (!isLast)
                Divider(
                    height: 1,
                    indent: 54,
                    endIndent: 16,
                    color: scheme.outline.withAlpha(60)),
            ],
          );
        }).toList(),
      ),
    );
  }
}

// ── Low stock alert ───────────────────────────────────────────────────────────

class _LowStockAlert extends StatelessWidget {
  const _LowStockAlert({required this.count});

  final int count;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final isDark = Theme.of(context).brightness == Brightness.dark;
    const accent = Color(0xFFD97706);

    return Container(
      decoration: BoxDecoration(
        color: isDark ? accent.withAlpha(25) : const Color(0xFFFFF7ED),
        borderRadius: BorderRadius.circular(14),
        border: Border.all(
            color: isDark ? accent.withAlpha(50) : const Color(0xFFFED7AA)),
      ),
      padding: const EdgeInsets.all(14),
      child: Row(
        children: [
          Icon(Icons.inventory_2_outlined, color: accent, size: 22),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  S.of(context).lowStockAlert,
                  style: GoogleFonts.instrumentSans(
                    fontWeight: FontWeight.w700,
                    color: accent,
                    fontSize: 13,
                  ),
                ),
                Text(
                  S.of(context).itemsBelowReorderLevel(count),
                  style: GoogleFonts.instrumentSans(
                      fontSize: 12,
                      color: scheme.onSurface.withAlpha(160)),
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right, color: accent.withAlpha(180)),
        ],
      ),
    );
  }
}

// ── Section label ─────────────────────────────────────────────────────────────

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text, {required this.scheme});

  final String text;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: GoogleFonts.instrumentSans(
          fontSize: 13,
          fontWeight: FontWeight.w700,
          color: scheme.onSurface,
        ),
      );
}
