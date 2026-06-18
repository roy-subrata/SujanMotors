import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/cashbook.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/state_views.dart';
import 'cashbook_repository.dart';

class CashBookScreen extends ConsumerStatefulWidget {
  const CashBookScreen({super.key});

  @override
  ConsumerState<CashBookScreen> createState() => _CashBookScreenState();
}

class _CashBookScreenState extends ConsumerState<CashBookScreen> {
  DateTime _date = DateUtils.dateOnly(DateTime.now());

  String get _dateKey => DateFormat('yyyy-MM-dd').format(_date);
  bool get _isToday => DateUtils.isSameDay(_date, DateTime.now());

  void _shift(int days) =>
      setState(() => _date = _date.add(Duration(days: days)));

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime(2020),
      lastDate: DateTime.now(),
    );
    if (picked != null) setState(() => _date = DateUtils.dateOnly(picked));
  }

  @override
  Widget build(BuildContext context) {
    final dayAsync = ref.watch(cashBookDayProvider(_dateKey));

    return AppScaffold(
      title: 'Cash Book',
      body: Column(
        children: [
          _DateBar(
            date: _date,
            isToday: _isToday,
            onPrev: () => _shift(-1),
            onNext: _isToday ? null : () => _shift(1),
            onPick: _pickDate,
          ),
          Expanded(
            child: RefreshIndicator(
              onRefresh: () async =>
                  ref.invalidate(cashBookDayProvider(_dateKey)),
              child: dayAsync.when(
                loading: () => const LoadingView(),
                error: (e, _) => ListView(children: [
                  const SizedBox(height: 120),
                  ErrorView(
                    message: e is AppException
                        ? e.message
                        : 'Failed to load cash book.',
                    onRetry: () =>
                        ref.invalidate(cashBookDayProvider(_dateKey)),
                  ),
                ]),
                data: (day) => _CashBookBody(day: day),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _DateBar extends StatelessWidget {
  const _DateBar({
    required this.date,
    required this.isToday,
    required this.onPrev,
    required this.onNext,
    required this.onPick,
  });

  final DateTime date;
  final bool isToday;
  final VoidCallback onPrev;
  final VoidCallback? onNext;
  final VoidCallback onPick;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Material(
      color: scheme.surfaceContainerHighest.withValues(alpha: 0.5),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 6),
        child: Row(
          children: [
            IconButton(
              icon: const Icon(Icons.chevron_left),
              onPressed: onPrev,
              tooltip: 'Previous day',
            ),
            Expanded(
              child: InkWell(
                onTap: onPick,
                borderRadius: BorderRadius.circular(8),
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 8),
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      const Icon(Icons.calendar_today_outlined, size: 16),
                      const SizedBox(width: 8),
                      Text(
                        isToday ? 'Today · ${formatDate(date)}'
                                : formatDayLong(date),
                        style: const TextStyle(fontWeight: FontWeight.w600),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            IconButton(
              icon: const Icon(Icons.chevron_right),
              onPressed: onNext,
              tooltip: 'Next day',
            ),
          ],
        ),
      ),
    );
  }
}

class _CashBookBody extends StatelessWidget {
  const _CashBookBody({required this.day});

  final CashBookDay day;

  @override
  Widget build(BuildContext context) {
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _SummaryCard(day: day),
        const SizedBox(height: 16),
        if (day.breakdown.isNotEmpty) ...[
          _SectionLabel('By payment method'),
          const SizedBox(height: 8),
          _BreakdownCard(rows: day.breakdown),
          const SizedBox(height: 16),
        ],
        _SectionLabel('Transactions (${day.entryCount})'),
        const SizedBox(height: 8),
        if (day.ledger.isEmpty)
          const Padding(
            padding: EdgeInsets.only(top: 24),
            child: EmptyView(
              message: 'No cash movement on this day.',
              icon: Icons.receipt_long_outlined,
            ),
          )
        else
          ...day.ledger.reversed.map((r) => _LedgerTile(row: r)),
        const SizedBox(height: 24),
      ],
    );
  }
}

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.day});

  final CashBookDay day;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Card(
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Column(
        children: [
          // Closing balance banner.
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(18),
            decoration: const BoxDecoration(gradient: AppGradients.brand),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('Closing balance',
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: Colors.white70)),
                const SizedBox(height: 4),
                Text(
                  formatCurrency(day.closingBalance),
                  style: theme.textTheme.headlineMedium?.copyWith(
                      color: Colors.white, fontWeight: FontWeight.bold),
                ),
                Text('Opening ${formatCurrency(day.openingBalance)}',
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: Colors.white70)),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Expanded(
                  child: _FlowMetric(
                    label: 'Cash in',
                    value: day.totalActualCashIn,
                    color: Colors.green.shade700,
                    icon: Icons.south_west,
                    note: day.totalCreditIn > 0
                        ? '+${formatCurrency(day.totalCreditIn)} on credit'
                        : null,
                  ),
                ),
                Container(
                    width: 1, height: 44, color: scheme.outlineVariant),
                Expanded(
                  child: _FlowMetric(
                    label: 'Cash out',
                    value: day.totalCashOut,
                    color: scheme.error,
                    icon: Icons.north_east,
                  ),
                ),
                Container(
                    width: 1, height: 44, color: scheme.outlineVariant),
                Expanded(
                  child: _FlowMetric(
                    label: 'Net',
                    value: day.netCash,
                    color: day.netCash >= 0
                        ? Colors.green.shade700
                        : scheme.error,
                    icon: Icons.swap_vert,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _FlowMetric extends StatelessWidget {
  const _FlowMetric({
    required this.label,
    required this.value,
    required this.color,
    required this.icon,
    this.note,
  });

  final String label;
  final double value;
  final Color color;
  final IconData icon;
  final String? note;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      children: [
        Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 14, color: color),
            const SizedBox(width: 4),
            Text(label, style: theme.textTheme.bodySmall),
          ],
        ),
        const SizedBox(height: 4),
        FittedBox(
          child: Text(
            formatCurrency(value),
            style: theme.textTheme.titleMedium
                ?.copyWith(fontWeight: FontWeight.bold, color: color),
          ),
        ),
        if (note != null)
          Text(note!,
              style: theme.textTheme.labelSmall
                  ?.copyWith(color: theme.colorScheme.onSurfaceVariant)),
      ],
    );
  }
}

class _BreakdownCard extends StatelessWidget {
  const _BreakdownCard({required this.rows});

  final List<CashMethodBreakdown> rows;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Card(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
        child: Column(
          children: [
            for (final r in rows)
              Padding(
                padding: const EdgeInsets.symmetric(vertical: 6),
                child: Row(
                  children: [
                    Expanded(
                      child: Text(_pretty(r.method),
                          style:
                              const TextStyle(fontWeight: FontWeight.w600)),
                    ),
                    if (r.cashIn > 0)
                      Text('+${formatCurrency(r.cashIn)}',
                          style: TextStyle(color: Colors.green.shade700)),
                    if (r.cashIn > 0 && r.cashOut > 0)
                      const SizedBox(width: 10),
                    if (r.cashOut > 0)
                      Text('-${formatCurrency(r.cashOut)}',
                          style: TextStyle(color: scheme.error)),
                  ],
                ),
              ),
          ],
        ),
      ),
    );
  }

  String _pretty(String method) {
    if (method.isEmpty) return 'Other';
    return method
        .split(RegExp(r'[_\s]+'))
        .map((w) => w.isEmpty ? w : w[0].toUpperCase() + w.substring(1).toLowerCase())
        .join(' ');
  }
}

class _LedgerTile extends StatelessWidget {
  const _LedgerTile({required this.row});

  final CashLedgerRow row;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final isIn = row.isIn;
    final color = isIn ? Colors.green.shade700 : scheme.error;
    final icon = _iconFor(row.type, isIn);

    final subtitleParts = <String>[
      formatTime(row.time),
      if (row.paymentMethod != null && row.paymentMethod!.isNotEmpty)
        row.paymentMethod!,
      if (row.reference != null && row.reference!.isNotEmpty) row.reference!,
    ];

    return Card(
      elevation: 0,
      margin: const EdgeInsets.only(bottom: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: scheme.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
        child: Row(
          children: [
            CircleAvatar(
              radius: 18,
              backgroundColor: color.withValues(alpha: 0.12),
              child: Icon(icon, size: 18, color: color),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(row.description,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: const TextStyle(fontWeight: FontWeight.w600)),
                  const SizedBox(height: 2),
                  Text(subtitleParts.join('  •  '),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: theme.textTheme.bodySmall),
                ],
              ),
            ),
            const SizedBox(width: 8),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  '${isIn ? '+' : '−'}${formatCurrency(row.amount, currency: row.currency)}',
                  style: TextStyle(fontWeight: FontWeight.w700, color: color),
                ),
                const SizedBox(height: 2),
                Text('Bal ${formatCurrency(row.balance, currency: row.currency)}',
                    style: theme.textTheme.labelSmall
                        ?.copyWith(color: scheme.onSurfaceVariant)),
              ],
            ),
          ],
        ),
      ),
    );
  }

  IconData _iconFor(String type, bool isIn) => switch (type.toUpperCase()) {
        'CUSTOMER_PAYMENT' => Icons.person_outline,
        'SUPPLIER_PAYMENT' => Icons.local_shipping_outlined,
        'EXPENSE' => Icons.receipt_long_outlined,
        'REFUND' => Icons.undo,
        _ => isIn ? Icons.south_west : Icons.north_east,
      };
}

class _SectionLabel extends StatelessWidget {
  const _SectionLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) {
    return Align(
      alignment: Alignment.centerLeft,
      child: Text(text.toUpperCase(),
          style: Theme.of(context).textTheme.labelSmall?.copyWith(
              letterSpacing: 0.6,
              fontWeight: FontWeight.w700,
              color: Theme.of(context).colorScheme.onSurfaceVariant)),
    );
  }
}
