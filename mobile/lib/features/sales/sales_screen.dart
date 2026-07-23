import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:intl/intl.dart';

import '../../core/theme/app_theme.dart';
import '../../core/i18n/strings.dart';
import '../../shared/format.dart';
import '../../shared/models/invoice.dart';
import '../../shared/models/sale_return.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import 'quick_sale_providers.dart';
import 'sales_repository.dart';
import 'sales_returns_repository.dart';

/// D1 · Sale list — invoices grouped by day with day totals, a status
/// dropdown (All / Paid / Due / Returns), search, and a date-range filter.
class SalesScreen extends ConsumerStatefulWidget {
  const SalesScreen({super.key});

  @override
  ConsumerState<SalesScreen> createState() => _SalesScreenState();
}

/// Which status is selected. Returns swaps the list to sales returns.
enum _SalesFilter { all, paid, due, returns }

class _SalesScreenState extends ConsumerState<SalesScreen> {
  final _searchCtrl = TextEditingController();
  Timer? _debounce;
  String _search = '';
  _SalesFilter _filter = _SalesFilter.all;

  /// Inclusive date range, or null for all time.
  DateTimeRange? _range;

  @override
  void dispose() {
    _searchCtrl.dispose();
    _debounce?.cancel();
    super.dispose();
  }

  void _onSearchChanged(String v) {
    _debounce?.cancel();
    _debounce = Timer(const Duration(milliseconds: 380), () {
      if (mounted) setState(() => _search = v.trim());
    });
  }

  String get _resetKey => '${_filter.name}|$_search|'
      '${_range?.start.toIso8601String() ?? 'all'}|'
      '${_range?.end.toIso8601String() ?? ''}';

  @override
  Widget build(BuildContext context) {
    final cartCount = ref.watch(
      quickSaleControllerProvider.select((s) => s.itemCount),
    );

    return AppScaffold(
      title: S.of(context).sales,
      showBottomNav: true,
      showNotificationBell: true,
      showCartBadge: true,
      cartCount: cartCount,
      onCartTap: () => context.push('/quick-sale'),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.go('/quick-sale'),
        backgroundColor: context.colors.ink,
        foregroundColor: context.colors.onInk,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add),
      ),
      body: Column(
        children: [
          // Search
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: S.of(context).searchInvoiceCustomerHint,
              onChanged: _onSearchChanged,
            ),
          ),
          const SizedBox(height: 8),

          // Date range + status dropdown
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: Row(
              children: [
                Expanded(
                  child: _DateRangeButton(
                    range: _range,
                    onChanged: (r) => setState(() => _range = r),
                  ),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: FilterDropdown<_SalesFilter>(
                    value: _filter,
                    leadingIcon: Icons.filter_list_rounded,
                    options: [
                      (_SalesFilter.all, S.of(context).allStatuses),
                      (_SalesFilter.paid, S.of(context).statusPaid),
                      (_SalesFilter.due, S.of(context).due),
                      (_SalesFilter.returns, S.of(context).returns),
                    ],
                    onChanged: (f) => setState(() => _filter = f),
                  ),
                ),
              ],
            ),
          ),
          const SizedBox(height: 12),

          Expanded(
            child: _filter == _SalesFilter.returns
                ? _ReturnsList(
                    resetKey: _resetKey,
                    search: _search,
                    range: _range,
                  )
                : _InvoiceList(
                    resetKey: _resetKey,
                    search: _search,
                    status: _filter == _SalesFilter.paid ? 'PAID' : null,
                    hasDue: _filter == _SalesFilter.due,
                    range: _range,
                  ),
          ),
        ],
      ),
    );
  }
}

// ── Date range button ─────────────────────────────────────────────────────────

/// Preset choices for the period button; `custom` opens the system range
/// picker.
enum _RangePreset { allTime, today, last7, thisMonth, lastMonth, custom }

class _DateRangeButton extends StatelessWidget {
  const _DateRangeButton({required this.range, required this.onChanged});

  final DateTimeRange? range;
  final ValueChanged<DateTimeRange?> onChanged;

  String _label(BuildContext context) {
    final r = range;
    if (r == null) return S.of(context).allTime;
    final fmt = DateFormat('d MMM');
    if (_sameDay(r.start, r.end)) return fmt.format(r.start);
    return '${fmt.format(r.start)} – ${fmt.format(r.end)}';
  }

  static bool _sameDay(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;

  Future<void> _onPreset(BuildContext context, _RangePreset preset) async {
    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);
    switch (preset) {
      case _RangePreset.allTime:
        onChanged(null);
      case _RangePreset.today:
        onChanged(DateTimeRange(start: today, end: today));
      case _RangePreset.last7:
        onChanged(DateTimeRange(
            start: today.subtract(const Duration(days: 6)), end: today));
      case _RangePreset.thisMonth:
        onChanged(DateTimeRange(
            start: DateTime(now.year, now.month, 1), end: today));
      case _RangePreset.lastMonth:
        onChanged(DateTimeRange(
          start: DateTime(now.year, now.month - 1, 1),
          end: DateTime(now.year, now.month, 0),
        ));
      case _RangePreset.custom:
        final picked = await showDateRangePicker(
          context: context,
          firstDate: DateTime(now.year - 5),
          lastDate: today,
          initialDateRange: range,
        );
        if (picked != null) onChanged(picked);
    }
  }

  @override
  Widget build(BuildContext context) {
    return PopupMenuButton<_RangePreset>(
      tooltip: S.of(context).filterByDateRange,
      onSelected: (p) => _onPreset(context, p),
      itemBuilder: (context) => [
        for (final (preset, label) in [
          (_RangePreset.allTime, S.of(context).allTime),
          (_RangePreset.today, S.of(context).today),
          (_RangePreset.last7, S.of(context).last7Days),
          (_RangePreset.thisMonth, S.of(context).thisMonth),
          (_RangePreset.lastMonth, S.of(context).lastMonth),
          (_RangePreset.custom, S.of(context).customRange),
        ])
          PopupMenuItem(
            value: preset,
            child: Text(label,
                style: GoogleFonts.instrumentSans(fontSize: 13.5)),
          ),
      ],
      child: Container(
        height: 44,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(11),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        child: Row(
          children: [
            Icon(Icons.calendar_today_outlined,
                size: 15, color: context.colors.secondary),
            const SizedBox(width: 6),
            Expanded(
              child: Text(
                _label(context),
                maxLines: 1,
                overflow: TextOverflow.ellipsis,
                style: GoogleFonts.instrumentSans(
                  fontSize: 12.5,
                  fontWeight: FontWeight.w600,
                  color: context.colors.secondary,
                ),
              ),
            ),
            Icon(Icons.expand_more_rounded,
                size: 18, color: context.colors.secondary),
          ],
        ),
      ),
    );
  }
}

// ── Invoice list (All / Paid / Due) ──────────────────────────────────────────

class _InvoiceList extends ConsumerWidget {
  const _InvoiceList({
    required this.resetKey,
    required this.search,
    required this.status,
    required this.hasDue,
    required this.range,
  });

  final String resetKey;
  final String search;
  final String? status;
  final bool hasDue;
  final DateTimeRange? range;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return PagedListView<Invoice>(
      resetKey: resetKey,
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
      fetch: (page) => ref.read(salesRepositoryProvider).invoices(
            search: search,
            status: status,
            hasDue: hasDue,
            fromDate: range?.start,
            toDate: range?.end,
            page: page,
          ),
      emptyBuilder: (context) => EmptyView(
        message: S.of(context).noSalesFound,
        icon: Icons.receipt_long_outlined,
      ),
      indexedItemBuilder: (context, index, items) {
        final invoice = items[index];
        final isNewDay = index == 0 ||
            !_sameDay(items[index - 1].invoiceDate, invoice.invoiceDate);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (isNewDay)
              _DayHeader(
                day: invoice.invoiceDate,
                // Sum of the rows loaded so far for this day; converges to the
                // full day total as pages stream in.
                total: items
                    .where((x) => _sameDay(x.invoiceDate, invoice.invoiceDate))
                    .fold<double>(0, (sum, x) => sum + x.grandTotal),
              ),
            _InvoiceRow(invoice: invoice),
          ],
        );
      },
    );
  }

  static bool _sameDay(DateTime a, DateTime b) =>
      a.year == b.year && a.month == b.month && a.day == b.day;
}

class _DayHeader extends StatelessWidget {
  const _DayHeader({required this.day, required this.total});

  final DateTime day;
  final double total;

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
      child: SectionEyebrow(label: '$label · ${formatCurrency(total)}'),
    );
  }
}

class _InvoiceRow extends StatelessWidget {
  const _InvoiceRow({required this.invoice});

  final Invoice invoice;

  /// Maps backend invoice statuses onto the design's pill labels.
  static String _pillLabel(BuildContext context, Invoice inv) {
    final s = S.of(context);
    switch (inv.status?.toUpperCase()) {
      case 'PAID':
        return s.statusPaid;
      case 'PARTIALLY_PAID':
        return s.statusPartial;
      case 'CANCELLED':
        return s.statusCancelled;
      default:
        return inv.outstandingAmount > 0 ? s.due : (inv.status ?? '');
    }
  }

  @override
  Widget build(BuildContext context) {
    final customer = invoice.customerName;
    final subtitle = [
      if (customer != null && customer.isNotEmpty) customer,
      formatTime(invoice.invoiceDate),
    ].join(' · ');

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(13),
        child: InkWell(
          borderRadius: BorderRadius.circular(13),
          onTap: () => context.push('/invoice/${invoice.id}', extra: invoice),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(13),
              border: Border.all(color: Theme.of(context).colorScheme.outline),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        invoice.invoiceNumber,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w600,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        subtitle,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 11.5,
                          color: context.colors.muted,
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 12),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      formatCurrency(invoice.grandTotal,
                          currency: invoice.currency),
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    StatusPill(label: _pillLabel(context, invoice)),
                  ],
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

// ── Returns list ──────────────────────────────────────────────────────────────

class _ReturnsList extends ConsumerWidget {
  const _ReturnsList({
    required this.resetKey,
    required this.search,
    required this.range,
  });

  final String resetKey;
  final String search;
  final DateTimeRange? range;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return PagedListView<SalesReturn>(
      resetKey: resetKey,
      padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
      fetch: (page) => ref.read(salesReturnsRepositoryProvider).list(
            searchTerm: search,
            fromDate: range?.start,
            toDate: range?.end,
            page: page,
          ),
      emptyBuilder: (context) => EmptyView(
        message: S.of(context).noReturnsFound,
        icon: Icons.assignment_return_outlined,
      ),
      indexedItemBuilder: (context, index, items) {
        final ret = items[index];
        final isNewDay = index == 0 ||
            !_InvoiceList._sameDay(
                items[index - 1].createdAt, ret.createdAt);
        return Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            if (isNewDay)
              _DayHeader(
                day: ret.createdAt,
                total: items
                    .where((x) =>
                        _InvoiceList._sameDay(x.createdAt, ret.createdAt))
                    .fold<double>(0, (sum, x) => sum - x.totalRefundAmount),
              ),
            _ReturnRow(ret: ret),
          ],
        );
      },
    );
  }
}

class _ReturnRow extends StatelessWidget {
  const _ReturnRow({required this.ret});

  final SalesReturn ret;

  @override
  Widget build(BuildContext context) {
    final subtitle = [
      formatTime(ret.createdAt),
      if ((ret.salesOrderNumber ?? '').isNotEmpty)
        S.of(context).fromOrder(ret.salesOrderNumber!),
    ].join(' · ');

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(13),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
        child: Row(
          children: [
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    ret.returnNumber,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 13.5,
                      fontWeight: FontWeight.w600,
                    ),
                  ),
                  const SizedBox(height: 2),
                  Text(
                    subtitle,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11.5,
                      color: context.colors.muted,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(width: 12),
            Column(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                Text(
                  '- ${formatCurrency(ret.totalRefundAmount)}',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                    color: context.colors.red,
                  ),
                ),
                const SizedBox(height: 4),
                StatusPill(label: S.of(context).returnLabel),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
