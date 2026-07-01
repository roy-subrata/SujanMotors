import 'dart:io';
import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:open_filex/open_filex.dart';
import 'package:path_provider/path_provider.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/customer.dart';
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

  // Accumulated items across pages
  List<CustomerPurchaseItem> _items = [];
  int _totalCount = 0;
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
      _totalCount = 0;
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
        _totalCount = result.purchaseItemsTotalCount;
        _totalPages = result.purchaseItemsTotalPages;
        _currentPage = page;
      });
    } on AppException catch (e) {
      if (mounted) { setState(() => _error = e.message); }
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
    final errorColor = Theme.of(context).colorScheme.error;
    try {
      final Uint8List bytes = await ref
          .read(customersRepositoryProvider)
          .accountSummaryPdf(
            widget.customerId,
            fromDate: _fromDate,
            toDate: _toDate,
          );

      // Write to cache — always writable on every Android version,
      // and open_filex can serve files from here via its own FileProvider.
      final cacheDir = await getTemporaryDirectory();
      final timestamp = DateTime.now().millisecondsSinceEpoch;
      final path = '${cacheDir.path}/statement_$timestamp.pdf';
      await File(path).writeAsBytes(bytes);

      // Also copy to external app storage so the user can find it in Files.
      _saveToExternalAsync(bytes, timestamp);

      if (!mounted) return;

      // Open in native PDF viewer (preview).
      final result = await OpenFilex.open(path, type: 'application/pdf');

      if (!mounted) return;
      messenger.clearSnackBars();
      if (result.type == ResultType.done) {
        messenger.showSnackBar(
          const SnackBar(
            content: Text('Opening PDF — also saved to Files → Statements'),
            duration: Duration(seconds: 4),
            behavior: SnackBarBehavior.floating,
          ),
        );
      } else {
        messenger.showSnackBar(
          SnackBar(
            content: Text(
              result.message.isNotEmpty
                  ? result.message
                  : 'PDF saved to Files → Statements (no PDF viewer found)',
            ),
            duration: const Duration(seconds: 5),
            behavior: SnackBarBehavior.floating,
          ),
        );
      }
    } on AppException catch (e) {
      if (mounted) {
        messenger.clearSnackBars();
        messenger.showSnackBar(SnackBar(
          content: Text(e.message),
          backgroundColor: errorColor,
          behavior: SnackBarBehavior.floating,
        ));
      }
    } finally {
      if (mounted) setState(() => _isPdfLoading = false);
    }
  }

  /// Fire-and-forget: copy bytes to external app storage (Files → Statements).
  /// Errors are swallowed — preview still works even if this fails.
  Future<void> _saveToExternalAsync(Uint8List bytes, int timestamp) async {
    try {
      final extDir = await getExternalStorageDirectory();
      if (extDir == null) return;
      final dir = Directory('${extDir.path}/Statements');
      await dir.create(recursive: true);
      await File('${dir.path}/statement_$timestamp.pdf').writeAsBytes(bytes);
    } catch (_) {}
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Account Statement'),
        actions: [
          _isPdfLoading
              ? const Padding(
                  padding: EdgeInsets.all(14),
                  child: SizedBox(
                    width: 20,
                    height: 20,
                    child: CircularProgressIndicator(
                        strokeWidth: 2, color: Colors.white),
                  ),
                )
              : IconButton(
                  icon: const Icon(Icons.picture_as_pdf_outlined),
                  tooltip: 'Save & Preview PDF',
                  onPressed: _downloadPdf,
                ),
          const SizedBox(width: 4),
        ],
      ),
      body: Column(
        children: [
          // Date filter strip
          Container(
            color: const Color(0xFF4F46E5),
            padding: const EdgeInsets.fromLTRB(8, 6, 8, 8),
            child: SingleChildScrollView(
              scrollDirection: Axis.horizontal,
              child: Row(
                children: [
                  for (final f in _DateFilter.values)
                    Padding(
                      padding: const EdgeInsets.only(right: 8),
                      child: ChoiceChip(
                        label: Text(_filterLabel(f)),
                        selected: _filter == f,
                        onSelected: (_) => _applyFilter(f),
                        selectedColor: Colors.white,
                        labelStyle: TextStyle(
                          color: _filter == f
                              ? const Color(0xFF4F46E5)
                              : Colors.white,
                          fontWeight: _filter == f
                              ? FontWeight.w700
                              : FontWeight.w400,
                        ),
                        backgroundColor: Colors.white.withValues(alpha: 0.15),
                        side: BorderSide.none,
                      ),
                    ),
                ],
              ),
            ),
          ),

          // Body
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
                            : ListView.builder(
                                controller: _scrollCtrl,
                                padding: EdgeInsets.zero,
                                itemCount: _buildItems().length +
                                    (_isLoadingMore ? 1 : 0),
                                itemBuilder: (context, index) {
                                  final widgets = _buildItems();
                                  if (index >= widgets.length) {
                                    return const Padding(
                                      padding: EdgeInsets.all(16),
                                      child: Center(
                                          child:
                                              CircularProgressIndicator()),
                                    );
                                  }
                                  if (index == 0 && _summary != null) {
                                    return Column(
                                      children: [
                                        _SummaryCard(
                                          summary: _summary!,
                                          totalCount: _totalCount,
                                        ),
                                        widgets[index],
                                      ],
                                    );
                                  }
                                  return widgets[index];
                                },
                              ),
                      ),
          ),
        ],
      ),
    );
  }

  String _filterLabel(_DateFilter f) => switch (f) {
        _DateFilter.thisMonth => 'This Month',
        _DateFilter.last3Months => 'Last 3 Months',
        _DateFilter.thisYear => 'This Year',
        _DateFilter.all => 'All Time',
      };

  /// Builds a flat widget list inserting a divider whenever the invoice changes.
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
}

// ── Summary card ──────────────────────────────────────────────────────────────

class _SummaryCard extends StatelessWidget {
  const _SummaryCard({required this.summary, required this.totalCount});

  final CustomerAccountSummary summary;
  final int totalCount;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final hasDue = summary.currentDue > 0;

    return Container(
      margin: const EdgeInsets.all(12),
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: scheme.primaryContainer.withValues(alpha: 0.3),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
            color: scheme.primaryContainer.withValues(alpha: 0.6)),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Expanded(
                child: _SummaryMetric(
                  label: 'Purchased',
                  value: formatCurrency(summary.totalPurchaseAmount),
                  color: scheme.onSurface,
                ),
              ),
              Expanded(
                child: _SummaryMetric(
                  label: 'Paid',
                  value: formatCurrency(summary.totalPaidAmount),
                  color: Colors.green.shade700,
                ),
              ),
              Expanded(
                child: _SummaryMetric(
                  label: 'Due',
                  value: formatCurrency(summary.currentDue),
                  color: hasDue ? scheme.error : Colors.green.shade700,
                ),
              ),
            ],
          ),
          const SizedBox(height: 10),
          Divider(height: 1, color: scheme.outlineVariant),
          const SizedBox(height: 10),
          Row(
            children: [
              Icon(Icons.receipt_long_outlined,
                  size: 14, color: scheme.onSurfaceVariant),
              const SizedBox(width: 4),
              Text(
                '${summary.totalInvoices} invoice(s)  ·  $totalCount item(s)',
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: scheme.onSurfaceVariant),
              ),
            ],
          ),
        ],
      ),
    );
  }
}

class _SummaryMetric extends StatelessWidget {
  const _SummaryMetric(
      {required this.label, required this.value, required this.color});

  final String label;
  final String value;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: theme.textTheme.bodySmall?.copyWith(
                color: Theme.of(context).colorScheme.onSurfaceVariant)),
        const SizedBox(height: 3),
        Text(value,
            style: theme.textTheme.titleSmall?.copyWith(
                fontWeight: FontWeight.w800, color: color, fontSize: 13)),
      ],
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
    final scheme = Theme.of(context).colorScheme;
    final isActive = invoiceStatus.toUpperCase() == 'PAID';

    return Container(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 8),
      color: scheme.surfaceContainerHighest.withValues(alpha: 0.5),
      child: Row(
        children: [
          Icon(Icons.receipt_outlined,
              size: 14, color: scheme.onSurfaceVariant),
          const SizedBox(width: 6),
          Text(
            invoiceNumber,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: scheme.primary,
            ),
          ),
          const SizedBox(width: 8),
          Text(
            formatDate(invoiceDate),
            style: TextStyle(fontSize: 11, color: scheme.onSurfaceVariant),
          ),
          const Spacer(),
          Container(
            padding:
                const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
            decoration: BoxDecoration(
              color: isActive
                  ? Colors.green.shade50
                  : scheme.surfaceContainerHighest,
              borderRadius: BorderRadius.circular(10),
            ),
            child: Text(
              invoiceStatus,
              style: TextStyle(
                fontSize: 10,
                fontWeight: FontWeight.w600,
                color: isActive
                    ? Colors.green.shade700
                    : scheme.onSurfaceVariant,
              ),
            ),
          ),
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
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 10, 16, 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Item info
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.itemName,
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodyMedium
                      ?.copyWith(fontWeight: FontWeight.w600),
                ),
                if (item.itemLocalName != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    item.itemLocalName!,
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis,
                    style: theme.textTheme.bodySmall
                        ?.copyWith(color: scheme.onSurfaceVariant),
                  ),
                ],
                const SizedBox(height: 4),
                Text(
                  'SKU ${item.sku}',
                  style: theme.textTheme.bodySmall?.copyWith(
                      color: scheme.onSurfaceVariant, fontSize: 11),
                ),
                if ((item.vehicleLabel ?? '').isNotEmpty) ...[
                  const SizedBox(height: 4),
                  Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 6, vertical: 2),
                    decoration: BoxDecoration(
                      color: scheme.secondaryContainer.withValues(alpha: 0.5),
                      borderRadius: BorderRadius.circular(6),
                    ),
                    child: Text(
                      item.vehicleLabel!,
                      style: TextStyle(
                          fontSize: 11,
                          color: scheme.onSecondaryContainer),
                    ),
                  ),
                ],
              ],
            ),
          ),
          const SizedBox(width: 12),
          // Price info
          Column(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Text(
                '${item.quantity} × ${formatCurrency(item.unitPrice)}',
                style: theme.textTheme.bodySmall
                    ?.copyWith(color: scheme.onSurfaceVariant),
              ),
              const SizedBox(height: 3),
              Text(
                formatCurrency(item.lineTotal),
                style: TextStyle(
                  fontWeight: FontWeight.w800,
                  fontSize: 14,
                  color: scheme.primary,
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
