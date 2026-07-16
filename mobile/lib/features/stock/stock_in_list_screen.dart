import 'dart:async';

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../core/i18n/strings.dart';
import '../../shared/format.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import '../auth/auth_controller.dart';
import 'purchase_orders_repository.dart';

/// E3 · Stock In list — purchase orders with receipt status. Tapping a row
/// opens its detail sheet; pending orders can be received from there.
class StockInListScreen extends ConsumerStatefulWidget {
  const StockInListScreen({super.key});

  @override
  ConsumerState<StockInListScreen> createState() => _StockInListScreenState();
}

enum _PoFilter { all, pending, received }

class _StockInListScreenState extends ConsumerState<StockInListScreen> {
  final _searchCtrl = TextEditingController();
  Timer? _debounce;
  String _search = '';
  _PoFilter _filter = _PoFilter.all;

  // Bumped to force a list reload after a receive posts stock.
  int _reloadTick = 0;

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

  Future<void> _openDetail(PurchaseOrder po) async {
    final received = await showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _PoDetailSheet(poId: po.id),
    );
    if (received == true && mounted) setState(() => _reloadTick++);
  }

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: S.of(context).stockIn,
      showNotificationBell: true,
      actions: [
        IconButton(
          tooltip: 'Quick stock adjust',
          icon: const Icon(Icons.bolt_outlined),
          onPressed: () => context.push('/stock-in/quick'),
        ),
      ],
      floatingActionButton: FloatingActionButton(
        onPressed: () async {
          final created = await context.push<bool>('/stock-in/new');
          if (created == true && mounted) setState(() => _reloadTick++);
        },
        backgroundColor: context.colors.ink,
        foregroundColor: context.colors.onInk,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add),
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: 'Search PO no, supplier...',
              onChanged: _onSearchChanged,
            ),
          ),
          const SizedBox(height: 10),
          SizedBox(
            width: double.infinity,
            child: FilterChipRow(
              chips: [
                FilterChipData(label: 'All'),
                FilterChipData(
                  label: 'Pending',
                  inactiveColor: context.colors.amber,
                  inactiveBg: context.colors.amberBg,
                  inactiveBorder: context.colors.amberBorder,
                  activeBg: context.colors.amber,
                ),
                FilterChipData(
                  label: 'Received',
                  inactiveColor: context.colors.green,
                  inactiveBg: context.colors.greenBg,
                  activeBg: context.colors.green,
                ),
              ],
              selected: _filter.index,
              onSelect: (i) => setState(() => _filter = _PoFilter.values[i]),
            ),
          ),
          const SizedBox(height: 12),
          Expanded(
            child: PagedListView<PurchaseOrder>(
              resetKey: '${_filter.name}|$_search|$_reloadTick',
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
              fetch: (page) => ref.read(purchaseOrdersRepositoryProvider).list(
                    search: _search,
                    pendingOnly: _filter == _PoFilter.pending,
                    status: _filter == _PoFilter.received ? 'DELIVERED' : null,
                    page: page,
                  ),
              emptyBuilder: (context) => const EmptyView(
                message: 'No stock-in orders found.',
                icon: Icons.move_to_inbox_outlined,
              ),
              itemBuilder: (context, po) =>
                  _PoRow(po: po, onTap: () => _openDetail(po)),
            ),
          ),
        ],
      ),
    );
  }
}

/// Maps backend PO statuses onto the design's pill labels.
String poPillLabel(String status) {
  switch (status) {
    case 'DELIVERED':
      return 'Received';
    case 'DRAFT':
      return 'Draft';
    case 'SUBMITTED':
    case 'CONFIRMED':
      return 'Pending';
    case 'PARTIAL':
      return 'Partial';
    case 'CANCELLED':
      return 'Cancelled';
    default:
      return status;
  }
}

class _PoRow extends StatelessWidget {
  const _PoRow({required this.po, required this.onTap});

  final PurchaseOrder po;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final subtitle = [
      if (po.supplierName.isNotEmpty) po.supplierName,
      formatDate(po.orderDate),
    ].join(' · ');

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(13),
        child: InkWell(
          borderRadius: BorderRadius.circular(13),
          onTap: onTap,
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
                        po.poNumber,
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
                      formatCurrency(po.grandTotal, currency: po.currency),
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    const SizedBox(height: 4),
                    StatusPill(label: poPillLabel(po.status)),
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

// ── PO detail sheet with receive action ──────────────────────────────────────

class _PoDetailSheet extends ConsumerStatefulWidget {
  const _PoDetailSheet({required this.poId});

  final String poId;

  @override
  ConsumerState<_PoDetailSheet> createState() => _PoDetailSheetState();
}

class _PoDetailSheetState extends ConsumerState<_PoDetailSheet> {
  PurchaseOrder? _po;
  String? _error;
  bool _receiving = false;
  String? _progress;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() => _error = null);
    try {
      final po =
          await ref.read(purchaseOrdersRepositoryProvider).getById(widget.poId);
      if (mounted) setState(() => _po = po);
    } catch (e) {
      if (mounted) setState(() => _error = '$e');
    }
  }

  /// Walks the receive chain for the whole remaining quantity at PO cost:
  /// (submit → confirm if needed) → GRN → verify → accept.
  Future<void> _receive() async {
    final po = _po!;
    final warehouse = await pickWarehouse(context, ref);
    if (warehouse == null || !mounted) return;

    final repo = ref.read(purchaseOrdersRepositoryProvider);
    final username =
        ref.read(authControllerProvider).value?.username ?? 'mobile';
    setState(() => _receiving = true);
    try {
      if (po.status == 'DRAFT') {
        setState(() => _progress = 'Submitting order...');
        await repo.submit(po.id);
      }
      if (po.status == 'DRAFT' || po.status == 'SUBMITTED') {
        setState(() => _progress = 'Confirming order...');
        await repo.confirm(po.id);
      }
      setState(() => _progress = 'Posting goods receipt...');
      final grnId = await repo.createGrn(
        purchaseOrderId: po.id,
        warehouseId: warehouse.id,
        receivedDate: DateTime.now(),
        lines: po.lines
            .where((l) => l.remainingQuantity > 0)
            .map((l) => GrnDraftLine(
                  partId: l.partId,
                  purchaseOrderLineId: l.id,
                  receivedQuantity: l.remainingQuantity,
                  unitCost: l.unitPrice,
                ))
            .toList(),
      );
      setState(() => _progress = 'Verifying...');
      await repo.verifyGrn(grnId, verifiedBy: username);
      setState(() => _progress = 'Accepting stock...');
      await repo.acceptGrn(grnId);
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('${po.poNumber} received — stock posted.')),
      );
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _receiving = false;
        _progress = null;
      });
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('$e')));
      // The chain may have advanced some steps (e.g. PO now CONFIRMED);
      // reload so the sheet reflects the real state.
      _load();
    }
  }

  @override
  Widget build(BuildContext context) {
    final po = _po;
    return FractionallySizedBox(
      heightFactor: 0.8,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 8, 4),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    po?.poNumber ?? 'Purchase order',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                ),
                if (po != null) StatusPill(label: poPillLabel(po.status)),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
          ),
          if (_error != null)
            Expanded(child: ErrorView(message: _error!, onRetry: _load))
          else if (po == null)
            const Expanded(child: LoadingView())
          else ...[
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 10),
              child: Text(
                '${po.supplierName} · ${formatDate(po.orderDate)}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12.5, color: context.colors.secondary),
              ),
            ),
            Expanded(
              child: ListView.separated(
                padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
                itemCount: po.lines.length,
                separatorBuilder: (_, _) => const SizedBox(height: 8),
                itemBuilder: (context, i) {
                  final l = po.lines[i];
                  return Container(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 14, vertical: 12),
                    decoration: BoxDecoration(
                      color: Theme.of(context).colorScheme.surface,
                      borderRadius: BorderRadius.circular(13),
                      border: Border.all(
                          color: Theme.of(context).colorScheme.outline),
                    ),
                    child: Row(
                      children: [
                        Expanded(
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                l.displayName,
                                style: GoogleFonts.instrumentSans(
                                    fontSize: 13.5,
                                    fontWeight: FontWeight.w500),
                              ),
                              const SizedBox(height: 2),
                              Text(
                                'Qty ${l.quantity} · ${formatCurrency(l.unitPrice)}'
                                '${l.receivedQuantity > 0 ? ' · ${l.receivedQuantity} received' : ''}',
                                style: GoogleFonts.instrumentSans(
                                    fontSize: 11.5, color: context.colors.muted),
                              ),
                            ],
                          ),
                        ),
                        Text(
                          formatCurrency(l.lineTotal),
                          style: GoogleFonts.instrumentSans(
                              fontSize: 13.5, fontWeight: FontWeight.w600),
                        ),
                      ],
                    ),
                  );
                },
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 4, 16, 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Grand total',
                          style: GoogleFonts.instrumentSans(
                              fontSize: 13, color: context.colors.secondary)),
                      Text(
                        formatCurrency(po.grandTotal, currency: po.currency),
                        style: GoogleFonts.instrumentSans(
                            fontSize: 16, fontWeight: FontWeight.w700),
                      ),
                    ],
                  ),
                  if (po.isReceivable &&
                      po.lines.any((l) => l.remainingQuantity > 0)) ...[
                    const SizedBox(height: 12),
                    FilledButton.icon(
                      style: FilledButton.styleFrom(
                        backgroundColor: context.colors.green,
                        padding: const EdgeInsets.symmetric(vertical: 15),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                      ),
                      onPressed: _receiving ? null : _receive,
                      icon: _receiving
                          ? SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                  strokeWidth: 2, color: context.colors.onInk),
                            )
                          : const Icon(Icons.check_rounded),
                      label: Text(_progress ?? 'Receive stock'),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Warehouse picker (shared with the E4 entry screen) ──────────────────────

Future<Warehouse?> pickWarehouse(BuildContext context, WidgetRef ref) async {
  return showModalBottomSheet<Warehouse>(
    context: context,
    builder: (sheetContext) => Consumer(
      builder: (context, ref, _) {
        final warehouses = ref.watch(warehousesProvider);
        return SafeArea(
          child: warehouses.when(
            loading: () => const SizedBox(height: 200, child: LoadingView()),
            error: (e, _) => SizedBox(
              height: 200,
              child: ErrorView(
                message: '$e',
                onRetry: () => ref.invalidate(warehousesProvider),
              ),
            ),
            data: (list) => ListView(
              shrinkWrap: true,
              padding: const EdgeInsets.symmetric(vertical: 8),
              children: [
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 8, 16, 8),
                  child: Text(
                    'Receive into warehouse',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 15, fontWeight: FontWeight.w700),
                  ),
                ),
                for (final w in list)
                  ListTile(
                    leading: Icon(Icons.warehouse_outlined,
                        color: context.colors.secondary),
                    title: Text(w.name),
                    onTap: () => Navigator.of(sheetContext).pop(w),
                  ),
              ],
            ),
          ),
        );
      },
    ),
  );
}
