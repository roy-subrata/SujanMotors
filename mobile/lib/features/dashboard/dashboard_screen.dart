import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../shared/format.dart';
import '../../shared/models/dashboard.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/state_views.dart';
import 'dashboard_providers.dart';

class DashboardScreen extends ConsumerWidget {
  const DashboardScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(dashboardControllerProvider);
    final ctrl = ref.read(dashboardControllerProvider.notifier);

    return AppScaffold(
      title: 'SujanMotors',
      showBottomNav: true,
      body: Column(
        children: [
          _PeriodTabRow(
            selected: state.period,
            onSelect: ctrl.load,
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
                        period: state.period,
                        onRefresh: () async => ctrl.load(),
                      ),
          ),
        ],
      ),
    );
  }
}

// ── Period tab row ────────────────────────────────────────────────────────────
// Same indigo strip style as the category tabs — visually extends the AppBar.

class _PeriodTabRow extends StatelessWidget {
  const _PeriodTabRow({required this.selected, required this.onSelect});

  final DashboardPeriod selected;
  final void Function(DashboardPeriod) onSelect;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 44,
      color: const Color(0xFF4F46E5),
      child: Row(
        children: DashboardPeriod.values.map((p) {
          final label = switch (p) {
            DashboardPeriod.today => 'Today',
            DashboardPeriod.month => 'This Month',
            DashboardPeriod.year => 'This Year',
          };
          final isSelected = p == selected;
          return GestureDetector(
            onTap: () => onSelect(p),
            child: Container(
              padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 10),
              decoration: BoxDecoration(
                border: Border(
                  bottom: BorderSide(
                    color: isSelected ? Colors.white : Colors.transparent,
                    width: 2.5,
                  ),
                ),
              ),
              child: Text(
                label,
                style: TextStyle(
                  fontSize: 13,
                  fontWeight:
                      isSelected ? FontWeight.w700 : FontWeight.w400,
                  color: isSelected ? Colors.white : Colors.white70,
                ),
              ),
            ),
          );
        }).toList(),
      ),
    );
  }
}

// ── Dashboard body ────────────────────────────────────────────────────────────

class _DashboardBody extends StatelessWidget {
  const _DashboardBody({
    required this.data,
    required this.period,
    required this.onRefresh,
  });

  final DashboardData data;
  final DashboardPeriod period;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final s = data.summary;

    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.fromLTRB(12, 14, 12, 24),
        children: [
          // ── Revenue headline ────────────────────────────────────────────
          _RevenueCard(summary: s),
          const SizedBox(height: 12),

          // ── 2×2 stat grid ───────────────────────────────────────────────
          _StatGrid(summary: s),
          const SizedBox(height: 12),

          // ── Cash flow card ──────────────────────────────────────────────
          _CashFlowCard(summary: s),
          const SizedBox(height: 16),

          // ── Quick actions ───────────────────────────────────────────────
          _SectionLabel('Quick Actions'),
          const SizedBox(height: 8),
          _QuickActions(),
          const SizedBox(height: 16),

          // ── Top products ────────────────────────────────────────────────
          if (data.topProducts.isNotEmpty) ...[
            _SectionLabel('Top Products'),
            const SizedBox(height: 8),
            _TopProductsList(products: data.topProducts),
            const SizedBox(height: 16),
          ],

          // ── Top customers ───────────────────────────────────────────────
          if (data.topCustomers.isNotEmpty) ...[
            _SectionLabel('Top Customers'),
            const SizedBox(height: 8),
            _TopCustomersList(customers: data.topCustomers),
            const SizedBox(height: 16),
          ],

          // ── Low stock alert ─────────────────────────────────────────────
          if (s.lowStockItemsCount > 0) _LowStockAlert(count: s.lowStockItemsCount),
        ],
      ),
    );
  }
}

// ── Revenue headline card ─────────────────────────────────────────────────────
// The "Total" in the POS reference — big number, primary metric.

class _RevenueCard extends StatelessWidget {
  const _RevenueCard({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final profit = summary.netProfit;
    final isPositive = profit >= 0;

    return Card(
      elevation: 0,
      color: const Color(0xFF4F46E5),
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
                  'Total Revenue',
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
                    '${summary.totalSalesCount} orders',
                    style: const TextStyle(color: Colors.white70, fontSize: 11),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Text(
              formatCurrency(summary.totalRevenue),
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
                    label: 'Cash',
                    value: formatCurrency(summary.cashSales),
                    color: Colors.white),
                const SizedBox(width: 20),
                _MiniStat(
                    label: 'Credit',
                    value: formatCurrency(summary.creditSales),
                    color: Colors.white70),
                const Spacer(),
                Container(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: isPositive
                        ? Colors.green.shade400.withAlpha(50)
                        : Colors.red.shade300.withAlpha(50),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(
                      color: isPositive
                          ? Colors.green.shade300
                          : Colors.red.shade300,
                      width: 1,
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
      {required this.label, required this.value, required this.color});

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label,
              style: TextStyle(
                  color: Colors.white54, fontSize: 10)),
          Text(value,
              style: TextStyle(
                  color: color, fontSize: 12, fontWeight: FontWeight.w600)),
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
          label: 'Gross Profit',
          value: formatCurrency(summary.grossProfit),
          icon: Icons.trending_up,
          color: Colors.green.shade600,
          bg: Colors.green.shade50,
        ),
        _StatCard(
          label: 'Net Profit',
          value: formatCurrency(profit),
          icon: profit >= 0 ? Icons.show_chart : Icons.trending_down,
          color: profit >= 0 ? Colors.teal.shade600 : Colors.red.shade600,
          bg: profit >= 0 ? Colors.teal.shade50 : Colors.red.shade50,
        ),
        _StatCard(
          label: 'Customer Due',
          value: formatCurrency(summary.customerDueAmount),
          badge: summary.customerDueCount > 0
              ? '${summary.customerDueCount}'
              : null,
          icon: Icons.account_balance_wallet_outlined,
          color: Colors.amber.shade700,
          bg: Colors.amber.shade50,
        ),
        _StatCard(
          label: 'Overdue',
          value: formatCurrency(summary.customerOverdueAmount),
          badge: summary.customerOverdueCount > 0
              ? '${summary.customerOverdueCount}'
              : null,
          icon: Icons.warning_amber_outlined,
          color: Colors.red.shade600,
          bg: Colors.red.shade50,
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
    required this.color,
    required this.bg,
    this.badge,
  });

  final String label;
  final String value;
  final IconData icon;
  final Color color;
  final Color bg;
  final String? badge;

  @override
  Widget build(BuildContext context) => Card(
        elevation: 0,
        color: bg,
        shape:
            RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
        child: Padding(
          padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Row(
                children: [
                  Icon(icon, color: color, size: 18),
                  const Spacer(),
                  if (badge != null)
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 6, vertical: 2),
                      decoration: BoxDecoration(
                        color: color.withAlpha(30),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Text(
                        badge!,
                        style: TextStyle(
                            fontSize: 10,
                            color: color,
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
                    style: TextStyle(
                        fontSize: 11,
                        color: color.withAlpha(180),
                        fontWeight: FontWeight.w500),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    value,
                    style: TextStyle(
                        fontSize: 14,
                        color: color,
                        fontWeight: FontWeight.w800),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                  ),
                ],
              ),
            ],
          ),
        ),
      );
}

// ── Cash flow card ─────────────────────────────────────────────────────────────

class _CashFlowCard extends StatelessWidget {
  const _CashFlowCard({required this.summary});

  final DashboardSummary summary;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      elevation: 0,
      color: theme.colorScheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: theme.colorScheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.account_balance_outlined, size: 15),
                const SizedBox(width: 6),
                Text('Cash Flow',
                    style: theme.textTheme.labelMedium
                        ?.copyWith(fontWeight: FontWeight.w700)),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                _FlowItem(
                    label: 'Opening',
                    value: summary.openingBalance,
                    color: theme.colorScheme.onSurfaceVariant),
                _Arrow(),
                _FlowItem(
                    label: 'In',
                    value: summary.cashInflow,
                    color: Colors.green.shade600),
                _Arrow(),
                _FlowItem(
                    label: 'Out',
                    value: summary.cashOutflow,
                    color: Colors.red.shade500),
                _Arrow(),
                _FlowItem(
                    label: 'Closing',
                    value: summary.closingBalance,
                    color: const Color(0xFF4F46E5),
                    bold: true),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _Arrow extends StatelessWidget {
  @override
  Widget build(BuildContext context) =>
      const Icon(Icons.arrow_forward_ios, size: 10, color: Colors.grey);
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
  Widget build(BuildContext context) => Column(
        children: [
          Text(label,
              style: const TextStyle(fontSize: 10, color: Colors.grey)),
          const SizedBox(height: 2),
          Text(
            formatCurrency(value),
            style: TextStyle(
              fontSize: 12,
              color: color,
              fontWeight: bold ? FontWeight.w800 : FontWeight.w600,
            ),
          ),
        ],
      );
}

// ── Quick actions ─────────────────────────────────────────────────────────────
// The PAY button equivalent — amber/orange as the primary action.

class _QuickActions extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 10,
      crossAxisSpacing: 10,
      childAspectRatio: 2.5,
      children: [
        _ActionButton(
          label: 'New Sale',
          icon: Icons.point_of_sale,
          color: Colors.white,
          bg: const Color(0xFFF59E0B), // amber — matches PAY button in reference
          onTap: () => context.push('/quick-sale'),
        ),
        _ActionButton(
          label: 'Customers',
          icon: Icons.people_alt_outlined,
          color: Colors.white,
          bg: const Color(0xFF4F46E5),
          onTap: () => context.push('/customers'),
        ),
        _ActionButton(
          label: 'Cash Book',
          icon: Icons.account_balance_wallet_outlined,
          color: Colors.white,
          bg: Colors.teal.shade600,
          onTap: () => context.push('/cashbook'),
        ),
        _ActionButton(
          label: 'Products',
          icon: Icons.inventory_2_outlined,
          color: Colors.white,
          bg: Colors.blueGrey.shade600,
          onTap: () => context.push('/products'),
        ),
      ],
    );
  }
}

class _ActionButton extends StatelessWidget {
  const _ActionButton({
    required this.label,
    required this.icon,
    required this.color,
    required this.bg,
    required this.onTap,
  });

  final String label;
  final IconData icon;
  final Color color;
  final Color bg;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
        color: bg,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: onTap,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            child: Row(
              children: [
                Icon(icon, color: color, size: 22),
                const SizedBox(width: 10),
                Text(
                  label,
                  style: TextStyle(
                    color: color,
                    fontSize: 14,
                    fontWeight: FontWeight.w700,
                  ),
                ),
              ],
            ),
          ),
        ),
      );
}

// ── Top products list ─────────────────────────────────────────────────────────

class _TopProductsList extends StatelessWidget {
  const _TopProductsList({required this.products});

  final List<TopProduct> products;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      elevation: 0,
      color: scheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      child: Column(
        children: products.take(5).indexed.map((entry) {
          final (idx, p) = entry;
          final isLast = idx == (products.length > 5 ? 4 : products.length - 1);
          return Column(
            children: [
              Padding(
                padding:
                    const EdgeInsets.symmetric(horizontal: 16, vertical: 11),
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
                            style: theme.textTheme.bodyMedium
                                ?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          if (p.partNumber.isNotEmpty)
                            Text(
                              p.partNumber,
                              style: theme.textTheme.bodySmall
                                  ?.copyWith(color: scheme.onSurfaceVariant),
                            ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          '×${p.quantitySold}',
                          style: theme.textTheme.bodySmall?.copyWith(
                              color: scheme.onSurfaceVariant),
                        ),
                        Text(
                          formatCurrency(p.totalRevenue),
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: scheme.primary,
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
                    color: scheme.outlineVariant),
            ],
          );
        }).toList(),
      ),
    );
  }

  Color _rankColor(int idx) => switch (idx) {
        0 => const Color(0xFFF59E0B),
        1 => Colors.blueGrey.shade400,
        2 => Colors.brown.shade400,
        _ => Colors.grey.shade400,
      };
}

// ── Top customers list ────────────────────────────────────────────────────────

class _TopCustomersList extends StatelessWidget {
  const _TopCustomersList({required this.customers});

  final List<TopCustomer> customers;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      elevation: 0,
      color: scheme.surfaceContainerLowest,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: scheme.outlineVariant),
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
                      backgroundColor:
                          scheme.primaryContainer,
                      child: Text(
                        initial,
                        style: TextStyle(
                          color: scheme.onPrimaryContainer,
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
                            style: theme.textTheme.bodyMedium
                                ?.copyWith(fontWeight: FontWeight.w600),
                          ),
                          Text(
                            '${c.totalOrders} orders',
                            style: theme.textTheme.bodySmall
                                ?.copyWith(color: scheme.onSurfaceVariant),
                          ),
                        ],
                      ),
                    ),
                    Column(
                      crossAxisAlignment: CrossAxisAlignment.end,
                      children: [
                        Text(
                          formatCurrency(c.totalRevenue),
                          style: TextStyle(
                            fontSize: 13,
                            fontWeight: FontWeight.w700,
                            color: scheme.primary,
                          ),
                        ),
                        if (c.outstandingAmount > 0)
                          Text(
                            'Due: ${formatCurrency(c.outstandingAmount)}',
                            style: TextStyle(
                              fontSize: 11,
                              color: Colors.amber.shade700,
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
                    color: scheme.outlineVariant),
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
  Widget build(BuildContext context) => Card(
        elevation: 0,
        color: Colors.orange.shade50,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(color: Colors.orange.shade200),
        ),
        child: Padding(
          padding: const EdgeInsets.all(14),
          child: Row(
            children: [
              Icon(Icons.inventory_2_outlined,
                  color: Colors.orange.shade700, size: 22),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Low Stock Alert',
                      style: TextStyle(
                        fontWeight: FontWeight.w700,
                        color: Colors.orange.shade800,
                      ),
                    ),
                    Text(
                      '$count item${count == 1 ? '' : 's'} below reorder level',
                      style: TextStyle(
                          fontSize: 12, color: Colors.orange.shade700),
                    ),
                  ],
                ),
              ),
              Icon(Icons.chevron_right, color: Colors.orange.shade400),
            ],
          ),
        ),
      );
}

// ── Section label ─────────────────────────────────────────────────────────────

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: Theme.of(context)
            .textTheme
            .titleSmall
            ?.copyWith(fontWeight: FontWeight.w700),
      );
}
