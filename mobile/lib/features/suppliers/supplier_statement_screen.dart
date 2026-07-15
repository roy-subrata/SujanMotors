import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:share_plus/share_plus.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/widgets/state_views.dart';
import 'suppliers_repository.dart';

enum _DateFilter { thisMonth, last3Months, thisYear, all }

/// Supplier statement — ledger table Entry | Purchase | Paid | Balance with a
/// period picker, opening/closing header, and share-as-text export.
class SupplierStatementScreen extends ConsumerStatefulWidget {
  const SupplierStatementScreen({super.key, required this.supplierId});

  final String supplierId;

  @override
  ConsumerState<SupplierStatementScreen> createState() =>
      _SupplierStatementScreenState();
}

class _SupplierStatementScreenState
    extends ConsumerState<SupplierStatementScreen> {
  _DateFilter _filter = _DateFilter.thisMonth;

  bool _loading = true;
  String? _error;
  List<SupplierLedgerEntry> _entries = const [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  DateTime? get _fromDate {
    final now = DateTime.now();
    return switch (_filter) {
      _DateFilter.thisMonth => DateTime(now.year, now.month, 1),
      _DateFilter.last3Months => DateTime(now.year, now.month - 3, now.day),
      _DateFilter.thisYear => DateTime(now.year, 1, 1),
      _DateFilter.all => null,
    };
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final entries =
          await ref.read(suppliersRepositoryProvider).ledgerEntries(
                supplierId: widget.supplierId,
                fromDate: _fromDate,
              );
      if (!mounted) return;
      setState(() {
        _entries = entries;
        _loading = false;
      });
    } on AppException catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.message;
        _loading = false;
      });
    }
  }

  String _filterLabel(_DateFilter f) => switch (f) {
        _DateFilter.thisMonth => 'This Month',
        _DateFilter.last3Months => 'Last 3 Months',
        _DateFilter.thisYear => 'This Year',
        _DateFilter.all => 'All Time',
      };

  void _showPeriodPicker() {
    showModalBottomSheet<void>(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: _DateFilter.values
              .map((f) => ListTile(
                    title: Text(_filterLabel(f)),
                    trailing: _filter == f
                        ? const Icon(Icons.check, color: AppColors.ink)
                        : null,
                    onTap: () {
                      Navigator.pop(ctx);
                      setState(() => _filter = f);
                      _load();
                    },
                  ))
              .toList(),
        ),
      ),
    );
  }

  /// Balance before the first entry in the period — derived by reversing the
  /// first entry's effect on its running balance.
  double get _openingBalance {
    if (_entries.isEmpty) return 0;
    final first = _entries.first;
    return first.runningBalance - first.debit + first.credit;
  }

  double get _closingBalance =>
      _entries.isEmpty ? 0 : _entries.last.runningBalance;

  Future<void> _shareStatement(SupplierLedgerSummary summary) async {
    final buffer = StringBuffer()
      ..writeln('Supplier statement — ${summary.supplierName}')
      ..writeln('Period: ${_filterLabel(_filter)}')
      ..writeln('Opening: ${formatCurrency(_openingBalance)}')
      ..writeln('Closing: ${formatCurrency(_closingBalance)} payable')
      ..writeln('');
    for (final e in _entries) {
      final amount = e.debit > 0
          ? 'purchase ${formatCurrency(e.debit)}'
          : 'paid ${formatCurrency(e.credit)}';
      buffer.writeln(
          '${formatDate(e.date)} · ${e.referenceNumber} · $amount · balance ${formatCurrency(e.runningBalance)}');
    }
    await Share.share(buffer.toString(),
        subject: 'Supplier statement — ${summary.supplierName}');
  }

  @override
  Widget build(BuildContext context) {
    final summaryAsync =
        ref.watch(supplierLedgerSummaryProvider(widget.supplierId));

    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Supplier Statement',
          style: GoogleFonts.instrumentSans(
              fontSize: 16, fontWeight: FontWeight.w700),
        ),
        actions: [
          GestureDetector(
            onTap: _showPeriodPicker,
            child: Container(
              margin: const EdgeInsets.only(right: 8),
              padding:
                  const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(9),
                border:
                    Border.all(color: Theme.of(context).colorScheme.outline),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    _filterLabel(_filter),
                    style: GoogleFonts.instrumentSans(
                        fontSize: 12, fontWeight: FontWeight.w600),
                  ),
                  const SizedBox(width: 4),
                  const Icon(Icons.expand_more_rounded,
                      size: 14, color: AppColors.secondary),
                ],
              ),
            ),
          ),
        ],
      ),
      body: summaryAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ErrorView(
          message:
              e is AppException ? e.message : 'Failed to load supplier.',
          onRetry: () => ref
              .invalidate(supplierLedgerSummaryProvider(widget.supplierId)),
        ),
        data: (summary) => Column(
          children: [
            _SummaryBar(
              summary: summary,
              opening: _openingBalance,
              closing: _entries.isEmpty
                  ? summary.currentBalance
                  : _closingBalance,
            ),
            Expanded(
              child: _loading
                  ? const LoadingView()
                  : _error != null
                      ? ErrorView(message: _error!, onRetry: _load)
                      : RefreshIndicator(
                          onRefresh: _load,
                          child: _entries.isEmpty
                              ? ListView(children: const [
                                  SizedBox(height: 100),
                                  EmptyView(
                                    message:
                                        'No transactions in this period.',
                                    icon: Icons.receipt_long_outlined,
                                  ),
                                ])
                              : _StatementTable(entries: _entries),
                        ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: summaryAsync.value == null
          ? null
          : _BottomBar(
              onShare: () => _shareStatement(summaryAsync.value!),
            ),
    );
  }
}

// ── Summary sub-header ────────────────────────────────────────────────────────

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({
    required this.summary,
    required this.opening,
    required this.closing,
  });

  final SupplierLedgerSummary summary;
  final double opening;
  final double closing;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.fromLTRB(16, 6, 16, 10),
      color: Theme.of(context).colorScheme.surface,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            summary.supplierName,
            style: GoogleFonts.instrumentSans(
                fontSize: 14.5, fontWeight: FontWeight.w700),
          ),
          const SizedBox(height: 2),
          Text.rich(
            TextSpan(
              text: 'Opening ${formatCurrency(opening)} · Closing ',
              style: GoogleFonts.instrumentSans(
                  fontSize: 12, color: AppColors.muted),
              children: [
                TextSpan(
                  text: closing > 0
                      ? '${formatCurrency(closing)} payable'
                      : formatCurrency(closing),
                  style: GoogleFonts.instrumentSans(
                    fontSize: 12,
                    fontWeight: FontWeight.w600,
                    color: closing > 0 ? AppColors.amber : AppColors.green,
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

// ── Statement table (design pattern 11) ───────────────────────────────────────

class _StatementTable extends StatelessWidget {
  const _StatementTable({required this.entries});

  final List<SupplierLedgerEntry> entries;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return ListView(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 100),
      children: [
        Container(
          decoration: BoxDecoration(
            color: scheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: scheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            children: [
              // Header row
              Container(
                color: Theme.of(context).scaffoldBackgroundColor,
                padding:
                    const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                child: _Grid(
                  entry: Text('ENTRY', style: _headerStyle(context)),
                  debit: Text('PURCHASE',
                      textAlign: TextAlign.right,
                      style: _headerStyle(context)),
                  credit: Text('PAID',
                      textAlign: TextAlign.right,
                      style: _headerStyle(context)),
                  balance: Text('BALANCE',
                      textAlign: TextAlign.right,
                      style: _headerStyle(context)),
                ),
              ),
              for (final e in entries)
                Container(
                  decoration: BoxDecoration(
                    border: Border(
                        top: BorderSide(
                            color: scheme.outline.withAlpha(60))),
                  ),
                  padding: const EdgeInsets.symmetric(
                      horizontal: 14, vertical: 11),
                  child: _Grid(
                    entry: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          e.referenceNumber.isEmpty
                              ? _typeLabel(e.type)
                              : '${e.referenceNumber} · ${_typeLabel(e.type)}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: GoogleFonts.instrumentSans(
                              fontSize: 12, fontWeight: FontWeight.w500),
                        ),
                        const SizedBox(height: 1),
                        Text(
                          formatDate(e.date),
                          style: GoogleFonts.instrumentSans(
                              fontSize: 10.5, color: AppColors.muted),
                        ),
                      ],
                    ),
                    debit: Text(
                      e.debit > 0 ? _compact(e.debit) : '',
                      textAlign: TextAlign.right,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12, color: AppColors.amber),
                    ),
                    credit: Text(
                      e.credit > 0 ? _compact(e.credit) : '',
                      textAlign: TextAlign.right,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12, color: AppColors.green),
                    ),
                    balance: Text(
                      _compact(e.runningBalance),
                      textAlign: TextAlign.right,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12, fontWeight: FontWeight.w600),
                    ),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }

  static TextStyle _headerStyle(BuildContext context) =>
      GoogleFonts.instrumentSans(
        fontSize: 10.5,
        fontWeight: FontWeight.w700,
        letterSpacing: 0.5,
        color: AppColors.muted,
      );

  static String _typeLabel(String type) => switch (type.toUpperCase()) {
        'PURCHASE' => 'purchase',
        'PAYMENT' => 'payment',
        'REFUND' => 'refund',
        'ADVANCE' => 'advance',
        'CANCELLATION' => 'cancelled',
        _ => type.toLowerCase(),
      };

  /// Amounts without decimals — the 4-column grid is tight on a phone.
  static String _compact(double v) =>
      formatCurrency(v).replaceAll(RegExp(r'\.00$'), '');
}

/// The design's `1fr 76px 76px 84px` statement grid.
class _Grid extends StatelessWidget {
  const _Grid({
    required this.entry,
    required this.debit,
    required this.credit,
    required this.balance,
  });

  final Widget entry;
  final Widget debit;
  final Widget credit;
  final Widget balance;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        Expanded(child: entry),
        SizedBox(width: 68, child: debit),
        SizedBox(width: 68, child: credit),
        SizedBox(width: 78, child: balance),
      ],
    );
  }
}

// ── Bottom bar ────────────────────────────────────────────────────────────────

class _BottomBar extends StatelessWidget {
  const _BottomBar({required this.onShare});

  final VoidCallback onShare;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border(
            top: BorderSide(color: Theme.of(context).colorScheme.outline)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          child: SizedBox(
            width: double.infinity,
            child: FilledButton.icon(
              onPressed: onShare,
              icon: const Icon(Icons.ios_share_rounded, size: 16),
              label: const Text('Share statement'),
              style: FilledButton.styleFrom(
                backgroundColor: AppColors.ink,
                foregroundColor: Colors.white,
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(11)),
                padding: const EdgeInsets.symmetric(vertical: 13),
                textStyle: GoogleFonts.instrumentSans(
                    fontSize: 13.5, fontWeight: FontWeight.w600),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
