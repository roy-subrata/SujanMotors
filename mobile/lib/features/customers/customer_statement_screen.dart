import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/state_views.dart';
import 'customers_repository.dart';

enum _DateFilter { thisMonth, last3Months, thisYear, all }

class CustomerStatementScreen extends ConsumerStatefulWidget {
  const CustomerStatementScreen({super.key, required this.customerId});

  final String customerId;

  @override
  ConsumerState<CustomerStatementScreen> createState() =>
      _CustomerStatementScreenState();
}

class _CustomerStatementScreenState
    extends ConsumerState<CustomerStatementScreen> {
  final _scrollCtrl = ScrollController();

  _DateFilter _filter = _DateFilter.thisMonth;
  DateTime? _fromDate;
  DateTime? _toDate;

  List<CustomerPurchaseItem> _items = [];
  int _totalPages = 0;
  int _currentPage = 1;
  CustomerAccountSummary? _summary;

  bool _isLoading = false;
  bool _isLoadingMore = false;
  bool _isPdfLoading = false;
  String? _error;

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
    _applyFilter(_DateFilter.thisMonth, push: false);
    WidgetsBinding.instance.addPostFrameCallback((_) => _loadPage(1));
  }

  @override
  void dispose() {
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels >=
            _scrollCtrl.position.maxScrollExtent - 300 &&
        !_isLoadingMore &&
        _currentPage < _totalPages) {
      _loadMore();
    }
  }

  void _applyFilter(_DateFilter filter, {bool push = true}) {
    final now = DateTime.now();
    final (DateTime? from, DateTime? to) = switch (filter) {
      _DateFilter.thisMonth => (DateTime(now.year, now.month, 1), null),
      _DateFilter.last3Months =>
        (now.subtract(const Duration(days: 90)), null),
      _DateFilter.thisYear => (DateTime(now.year, 1, 1), null),
      _DateFilter.all => (null, null),
    };
    setState(() {
      _filter = filter;
      _fromDate = from;
      _toDate = to;
      _items = [];
      _currentPage = 1;
      _totalPages = 0;
      _summary = null;
      _error = null;
    });
    if (push) _loadPage(1);
  }

  Future<void> _loadPage(int page) async {
    if (page == 1) {
      setState(() {
        _isLoading = true;
        _error = null;
      });
    } else {
      setState(() => _isLoadingMore = true);
    }
    try {
      final result = await ref
          .read(customersRepositoryProvider)
          .accountSummary(
            customerId: widget.customerId,
            fromDate: _fromDate,
            toDate: _toDate,
            pageNumber: page,
            pageSize: 30,
          );
      if (!mounted) return;
      setState(() {
        if (page == 1) {
          _items = result.purchaseItems;
          _summary = result;
        } else {
          _items = [..._items, ...result.purchaseItems];
          _summary = result;
        }
        _totalPages = result.purchaseItemsTotalPages;
        _currentPage = page;
      });
    } on AppException catch (e) {
      if (mounted) setState(() => _error = e.message);
    } finally {
      if (mounted) {
        setState(() {
          _isLoading = false;
          _isLoadingMore = false;
        });
      }
    }
  }

  Future<void> _loadMore() async {
    if (_isLoadingMore || _currentPage >= _totalPages) return;
    await _loadPage(_currentPage + 1);
  }

  Future<void> _downloadPdf() async {
    setState(() => _isPdfLoading = true);
    final messenger = ScaffoldMessenger.of(context);
    try {
      final Uint8List bytes = await ref
          .read(customersRepositoryProvider)
          .accountSummaryPdf(
            widget.customerId,
            fromDate: _fromDate,
            toDate: _toDate,
          );
      final cacheDir = await getTemporaryDirectory();
      final timestamp = DateTime.now().millisecondsSinceEpoch;
      final path = '${cacheDir.path}/statement_$timestamp.pdf';
      await File(path).writeAsBytes(bytes);
      if (!mounted) return;
      await Share.shareXFiles(
        [XFile(path, mimeType: 'application/pdf', name: 'statement_$timestamp.pdf')],
        subject: 'Account Statement',
      );
    } on AppException catch (e) {
      if (mounted) {
        messenger.clearSnackBars();
        messenger.showSnackBar(SnackBar(
          content: Text(e.message),
          backgroundColor: AppColors.red,
          behavior: SnackBarBehavior.floating,
        ));
      }
    } finally {
      if (mounted) setState(() => _isPdfLoading = false);
    }
  }

  String _filterLabel(_DateFilter f) => switch (f) {
        _DateFilter.thisMonth => 'This Month',
        _DateFilter.last3Months => 'Last 3 Months',
        _DateFilter.thisYear => 'This Year',
        _DateFilter.all => 'All Time',
      };

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          'Statement',
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
        actions: [
          // Period picker
          GestureDetector(
            onTap: _showPeriodPicker,
            child: Container(
              margin: const EdgeInsets.only(right: 8),
              padding:
                  const EdgeInsets.symmetric(horizontal: 12, vertical: 7),
              decoration: BoxDecoration(
                color: Theme.of(context).colorScheme.surface,
                borderRadius: BorderRadius.circular(9),
                border: Border.all(color: Theme.of(context).colorScheme.outline),
              ),
              child: Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(
                    _filterLabel(_filter),
                    style: GoogleFonts.instrumentSans(
                      fontSize: 12,
                      fontWeight: FontWeight.w600
                    ),
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
      body: Column(
        children: [
          // Summary sub-header
          if (_summary != null) _SummaryBar(summary: _summary!),

          Expanded(
            child: _isLoading
                ? const LoadingView()
                : _error != null
                    ? ErrorView(
                        message: _error!,
                        onRetry: () => _loadPage(1),
                      )
                    : RefreshIndicator(
                        onRefresh: () async {
                          setState(() {
                            _items = [];
                            _currentPage = 1;
                          });
                          await _loadPage(1);
                        },
                        child: _items.isEmpty
                            ? const EmptyView(
                                message: 'No transactions in this period.',
                                icon: Icons.receipt_long_outlined,
                              )
                            : _buildList(),
                      ),
          ),
        ],
      ),
      bottomNavigationBar: _buildBottomBar(),
    );
  }

  void _showPeriodPicker() {
    showModalBottomSheet(
      context: context,
      builder: (ctx) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: _DateFilter.values
              .map(
                (f) => ListTile(
                  title: Text(_filterLabel(f)),
                  trailing: _filter == f
                      ? const Icon(Icons.check, color: AppColors.ink)
                      : null,
                  onTap: () {
                    Navigator.pop(ctx);
                    _applyFilter(f);
                  },
                ),
              )
              .toList(),
        ),
      ),
    );
  }

  Widget _buildList() {
    final widgets = _buildItems();
    return ListView.builder(
      controller: _scrollCtrl,
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 100),
      itemCount: widgets.length + (_isLoadingMore ? 1 : 0),
      itemBuilder: (context, index) {
        if (index >= widgets.length) {
          return const Padding(
            padding: EdgeInsets.all(16),
            child: Center(child: CircularProgressIndicator()),
          );
        }
        return widgets[index];
      },
    );
  }

  List<Widget> _buildItems() {
    final widgets = <Widget>[];
    String? lastInvoice;
    for (final item in _items) {
      if (item.invoiceNumber != lastInvoice) {
        widgets.add(_InvoiceDivider(
          invoiceNumber: item.invoiceNumber,
          invoiceDate: item.invoiceDate,
          invoiceStatus: item.invoiceStatus,
        ));
        lastInvoice = item.invoiceNumber;
      }
      widgets.add(_PurchaseItemTile(item: item));
    }
    return widgets;
  }

  Widget _buildBottomBar() {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border(top: BorderSide(color: Theme.of(context).colorScheme.outline)),
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          child: Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: () {},
                  icon: const Icon(Icons.print_outlined, size: 16),
                  label: const Text('Print'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: AppColors.ink,
                    side: BorderSide(color: Theme.of(context).colorScheme.outline),
                    shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(11)),
                    padding: const EdgeInsets.symmetric(vertical: 13),
                    textStyle: GoogleFonts.instrumentSans(
                        fontSize: 13.5, fontWeight: FontWeight.w600),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                flex: 2,
                child: FilledButton.icon(
                  onPressed: _isPdfLoading ? null : _downloadPdf,
                  icon: _isPdfLoading
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(
                              strokeWidth: 2, color: Colors.white))
                      : const Icon(Icons.upload_outlined, size: 16),
                  label: const Text('Generate PDF & share'),
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
            ],
          ),
        ),
      ),
    );
  }
}

// ── Summary bar ───────────────────────────────────────────────────────────────

class _SummaryBar extends StatelessWidget {
  const _SummaryBar({required this.summary});

  final CustomerAccountSummary summary;

  @override
  Widget build(BuildContext context) {
    final hasDue = summary.currentDue > 0;
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 8, 16, 10),
      color: Theme.of(context).colorScheme.surface,
      child: Row(
        children: [
          Expanded(
            child: Text(
              '${summary.customerName} · Purchased ${formatCurrency(summary.totalPurchaseAmount)} · Paid ${formatCurrency(summary.totalPaidAmount)}',
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                color: hasDue ? AppColors.red : AppColors.muted,
              ),
            ),
          ),
          if (hasDue) ...[
            Text(
              'Due ${formatCurrency(summary.currentDue)}',
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: AppColors.red,
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Invoice divider ───────────────────────────────────────────────────────────

class _InvoiceDivider extends StatelessWidget {
  const _InvoiceDivider({
    required this.invoiceNumber,
    required this.invoiceDate,
    required this.invoiceStatus,
  });

  final String invoiceNumber;
  final DateTime invoiceDate;
  final String invoiceStatus;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(14, 10, 14, 8),
      margin: const EdgeInsets.only(bottom: 0),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border(
          bottom: BorderSide(color: Theme.of(context).colorScheme.outline.withAlpha(60)),
        ),
      ),
      child: Row(
        children: [
          Text(
            invoiceNumber,
            style: GoogleFonts.instrumentSans(
              fontSize: 12,
              fontWeight: FontWeight.w700
            ),
          ),
          const SizedBox(width: 8),
          Text(
            formatDate(invoiceDate),
            style: GoogleFonts.instrumentSans(
              fontSize: 11
            ),
          ),
          const Spacer(),
          StatusPill(label: invoiceStatus),
        ],
      ),
    );
  }
}

// ── Purchase item tile ────────────────────────────────────────────────────────

class _PurchaseItemTile extends StatelessWidget {
  const _PurchaseItemTile({required this.item});

  final CustomerPurchaseItem item;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        border: Border(bottom: BorderSide(color: Theme.of(context).colorScheme.outline.withAlpha(60))),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.itemName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13,
                    fontWeight: FontWeight.w500
                  ),
                ),
                if (item.itemLocalName != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    item.itemLocalName!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: GoogleFonts.instrumentSans(
                      fontSize: 11
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                style: GoogleFonts.instrumentSans(
                  fontSize: 11
                ),
              ),
              const SizedBox(height: 3),
              Text(
                formatCurrency(item.lineTotal),
                style: GoogleFonts.instrumentSans(
                  fontWeight: FontWeight.w700,
                  fontSize: 13.5
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
