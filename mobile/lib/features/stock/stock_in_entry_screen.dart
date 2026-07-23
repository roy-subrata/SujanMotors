import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../core/i18n/strings.dart';
import '../../shared/format.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import '../auth/auth_controller.dart';
import '../products/products_repository.dart';
import '../suppliers/suppliers_repository.dart';
import 'purchase_orders_repository.dart';
import 'stock_in_list_screen.dart' show pickWarehouse;

/// E4 · New Stock In entry — supplier + warehouse, reference/date, line items
/// with qty · cost · lot. "Receive stock" posts to inventory and creates lots;
/// "Save as draft" keeps the PO pending.
class StockInEntryScreen extends ConsumerStatefulWidget {
  const StockInEntryScreen({super.key});

  @override
  ConsumerState<StockInEntryScreen> createState() =>
      _StockInEntryScreenState();
}

class _StockInEntryScreenState extends ConsumerState<StockInEntryScreen> {
  Supplier? _supplier;
  Warehouse? _warehouse;
  final _referenceCtrl = TextEditingController();
  DateTime _date = DateTime.now();
  final List<StockInDraftLine> _lines = [];

  bool _saving = false;
  String? _progress;

  @override
  void dispose() {
    _referenceCtrl.dispose();
    super.dispose();
  }

  double get _grandTotal =>
      _lines.fold<double>(0, (sum, l) => sum + l.lineTotal);

  // ── Pickers ─────────────────────────────────────────────────────────────────

  Future<void> _pickSupplier() async {
    final picked = await showModalBottomSheet<Supplier>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => const _SupplierPickerSheet(),
    );
    if (picked != null) setState(() => _supplier = picked);
  }

  Future<void> _pickWarehouse() async {
    final picked = await pickWarehouse(context, ref);
    if (picked != null) setState(() => _warehouse = picked);
  }

  Future<void> _pickDate() async {
    final picked = await showDatePicker(
      context: context,
      initialDate: _date,
      firstDate: DateTime.now().subtract(const Duration(days: 365)),
      lastDate: DateTime.now(),
    );
    if (picked != null) setState(() => _date = picked);
  }

  Future<void> _addItem() async {
    final product = await showModalBottomSheet<Product>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => const _ProductPickerSheet(),
    );
    if (product == null || !mounted) return;
    final line = await showModalBottomSheet<StockInDraftLine>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _LineEditorSheet(
        initial: StockInDraftLine(
          partId: product.id,
          displayName: product.name,
          quantity: 1,
          unitCost: 0,
        ),
      ),
    );
    if (line != null) setState(() => _lines.add(line));
  }

  Future<void> _editLine(int index) async {
    final edited = await showModalBottomSheet<StockInDraftLine>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => _LineEditorSheet(initial: _lines[index]),
    );
    if (edited != null) setState(() => _lines[index] = edited);
  }

  // ── Submit ──────────────────────────────────────────────────────────────────

  String? _validate({required bool forReceive}) {
    final s = S.of(context);
    if (_supplier == null) return s.selectASupplier;
    if (_lines.isEmpty) return s.addAtLeastOneItem;
    if (forReceive && _warehouse == null) {
      return s.selectWarehouseToReceive;
    }
    if (forReceive && _lines.any((l) => l.unitCost <= 0)) {
      return s.everyItemNeedsCost;
    }
    return null;
  }

  Future<void> _save({required bool receive}) async {
    final problem = _validate(forReceive: receive);
    if (problem != null) {
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text(problem)));
      return;
    }

    final s = S.of(context);
    final repo = ref.read(purchaseOrdersRepositoryProvider);
    final username =
        ref.read(authControllerProvider).value?.username ?? 'mobile';
    setState(() {
      _saving = true;
      _progress = s.creatingOrder;
    });
    try {
      final po = await repo.create(
        supplierId: _supplier!.id,
        deliveryDate: _date,
        notes: _referenceCtrl.text.trim(),
        lines: _lines,
      );

      if (receive) {
        setState(() => _progress = s.confirmingOrder);
        await repo.submit(po.id);
        await repo.confirm(po.id);

        setState(() => _progress = s.postingGrn);
        // Match draft lines back to server PO lines to carry lot/expiry over.
        final serverLines = po.lines;
        final grnId = await repo.createGrn(
          purchaseOrderId: po.id,
          warehouseId: _warehouse!.id,
          receivedDate: _date,
          supplierInvoiceNumber: _referenceCtrl.text.trim(),
          lines: [
            for (final draft in _lines)
              GrnDraftLine(
                partId: draft.partId,
                purchaseOrderLineId: serverLines
                    .where((s) =>
                        s.partId == draft.partId &&
                        s.quantity == draft.quantity)
                    .firstOrNull
                    ?.id,
                receivedQuantity: draft.quantity,
                unitCost: draft.unitCost,
                batchNumber: draft.batchNumber,
                expiryDate: draft.expiryDate,
              ),
          ],
        );
        setState(() => _progress = s.verifying);
        await repo.verifyGrn(grnId, verifiedBy: username);
        setState(() => _progress = s.acceptingStock);
        await repo.acceptGrn(grnId);
      }

      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(receive
            ? s.poReceivedStockPosted(po.poNumber)
            : s.poSavedAsDraft(po.poNumber)),
      ));
      Navigator.of(context).pop(true);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _saving = false;
        _progress = null;
      });
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('$e')));
    }
  }

  // ── UI ──────────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    return AppScaffold(
      title: S.of(context).newStockIn,
      body: Stack(
        children: [
          ListView(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 150),
            children: [
              _PickerField(
                label: S.of(context).supplier,
                value: _supplier?.name,
                hint: S.of(context).selectSupplier,
                icon: Icons.store_outlined,
                onTap: _pickSupplier,
              ),
              const SizedBox(height: 10),
              _PickerField(
                label: S.of(context).warehouse,
                value: _warehouse?.name,
                hint: S.of(context).selectWarehouse,
                icon: Icons.warehouse_outlined,
                onTap: _pickWarehouse,
              ),
              const SizedBox(height: 10),
              Row(
                children: [
                  Expanded(
                    child: TextField(
                      controller: _referenceCtrl,
                      decoration: InputDecoration(
                        labelText: S.of(context).referenceBillNo,
                      ),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _PickerField(
                      label: S.of(context).date,
                      value: formatDate(_date),
                      hint: '',
                      icon: Icons.event_outlined,
                      onTap: _pickDate,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 18),
              SectionEyebrow(label: S.of(context).items),
              const SizedBox(height: 8),
              for (var i = 0; i < _lines.length; i++) ...[
                _LineCard(
                  line: _lines[i],
                  onTap: () => _editLine(i),
                  onRemove: () => setState(() => _lines.removeAt(i)),
                ),
                const SizedBox(height: 8),
              ],
              OutlinedButton.icon(
                onPressed: _saving ? null : _addItem,
                icon: const Icon(Icons.add),
                label: Text(S.of(context).addItem),
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(12)),
                ),
              ),
              const SizedBox(height: 18),
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(
                  color: Theme.of(context).colorScheme.surface,
                  borderRadius: BorderRadius.circular(13),
                  border:
                      Border.all(color: Theme.of(context).colorScheme.outline),
                ),
                child: Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Text(S.of(context).grandTotalLabel,
                        style: GoogleFonts.instrumentSans(
                            fontSize: 13, color: context.colors.secondary)),
                    Text(
                      formatCurrency(_grandTotal),
                      style: GoogleFonts.instrumentSans(
                          fontSize: 19, fontWeight: FontWeight.w700),
                    ),
                  ],
                ),
              ),
            ],
          ),

          // Sticky bottom CTA pair
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: Container(
              padding: const EdgeInsets.fromLTRB(16, 24, 16, 16),
              decoration: BoxDecoration(
                gradient: LinearGradient(
                  begin: Alignment.topCenter,
                  end: Alignment.bottomCenter,
                  colors: [
                    Theme.of(context).scaffoldBackgroundColor.withAlpha(0),
                    Theme.of(context).scaffoldBackgroundColor,
                  ],
                  stops: const [0, 0.3],
                ),
              ),
              child: Row(
                children: [
                  Expanded(
                    child: OutlinedButton(
                      onPressed:
                          _saving ? null : () => _save(receive: false),
                      style: OutlinedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 15),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                      ),
                      child: Text(S.of(context).saveAsDraft),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    flex: 2,
                    child: FilledButton.icon(
                      onPressed: _saving ? null : () => _save(receive: true),
                      style: FilledButton.styleFrom(
                        backgroundColor: context.colors.green,
                        padding: const EdgeInsets.symmetric(vertical: 15),
                        shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(14)),
                      ),
                      icon: _saving
                          ? SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                  strokeWidth: 2, color: context.colors.onInk),
                            )
                          : const Icon(Icons.check_rounded),
                      label: Text(_progress ?? S.of(context).receiveStock),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Field that opens a picker ─────────────────────────────────────────────────

class _PickerField extends StatelessWidget {
  const _PickerField({
    required this.label,
    required this.value,
    required this.hint,
    required this.icon,
    required this.onTap,
  });

  final String label;
  final String? value;
  final String hint;
  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return InkWell(
      onTap: onTap,
      borderRadius: BorderRadius.circular(11),
      child: InputDecorator(
        decoration: InputDecoration(
          labelText: label,
          prefixIcon: Icon(icon, size: 20, color: context.colors.secondary),
          suffixIcon: Icon(Icons.expand_more_rounded,
              color: context.colors.secondary),
        ),
        child: Text(
          value ?? hint,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.instrumentSans(
            fontSize: 14,
            color: value == null ? context.colors.muted : null,
          ),
        ),
      ),
    );
  }
}

// ── Line card ─────────────────────────────────────────────────────────────────

class _LineCard extends StatelessWidget {
  const _LineCard({
    required this.line,
    required this.onTap,
    required this.onRemove,
  });

  final StockInDraftLine line;
  final VoidCallback onTap;
  final VoidCallback onRemove;

  @override
  Widget build(BuildContext context) {
    final s = S.of(context);
    final meta = [
      '${s.qtyShort} ${line.quantity}',
      formatCurrency(line.unitCost),
      if ((line.batchNumber ?? '').isNotEmpty)
        '${s.lotShort} ${line.batchNumber}',
      if (line.expiryDate != null)
        '${s.expShort} ${formatDate(line.expiryDate!)}',
    ].join(' · ');

    return Material(
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
          padding: const EdgeInsets.fromLTRB(14, 8, 4, 8),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      line.displayName,
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 13.5, fontWeight: FontWeight.w500),
                    ),
                    const SizedBox(height: 2),
                    Text(
                      meta,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11.5, color: context.colors.muted),
                    ),
                  ],
                ),
              ),
              Text(
                formatCurrency(line.lineTotal),
                style: GoogleFonts.instrumentSans(
                    fontSize: 13.5, fontWeight: FontWeight.w600),
              ),
              IconButton(
                icon: Icon(Icons.close, size: 18,
                    color: context.colors.muted),
                onPressed: onRemove,
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Line editor sheet (qty · cost · lot · expiry) ────────────────────────────

class _LineEditorSheet extends StatefulWidget {
  const _LineEditorSheet({required this.initial});

  final StockInDraftLine initial;

  @override
  State<_LineEditorSheet> createState() => _LineEditorSheetState();
}

class _LineEditorSheetState extends State<_LineEditorSheet> {
  late final _qtyCtrl = TextEditingController(
      text: widget.initial.quantity > 0 ? '${widget.initial.quantity}' : '');
  late final _costCtrl = TextEditingController(
      text: widget.initial.unitCost > 0
          ? widget.initial.unitCost.toStringAsFixed(2)
          : '');
  late final _lotCtrl =
      TextEditingController(text: widget.initial.batchNumber ?? '');
  DateTime? _expiry;

  @override
  void initState() {
    super.initState();
    _expiry = widget.initial.expiryDate;
  }

  @override
  void dispose() {
    _qtyCtrl.dispose();
    _costCtrl.dispose();
    _lotCtrl.dispose();
    super.dispose();
  }

  void _done() {
    final qty = int.tryParse(_qtyCtrl.text.trim()) ?? 0;
    final cost = double.tryParse(_costCtrl.text.trim()) ?? 0;
    if (qty <= 0) {
      ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(S.of(context).quantityAtLeast1)));
      return;
    }
    Navigator.of(context).pop(StockInDraftLine(
      partId: widget.initial.partId,
      displayName: widget.initial.displayName,
      variantId: widget.initial.variantId,
      quantity: qty,
      unitCost: cost,
      batchNumber:
          _lotCtrl.text.trim().isEmpty ? null : _lotCtrl.text.trim(),
      expiryDate: _expiry,
    ));
  }

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: EdgeInsets.only(
          bottom: MediaQuery.of(context).viewInsets.bottom),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 16),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            Text(
              widget.initial.displayName,
              style: GoogleFonts.instrumentSans(
                  fontSize: 15, fontWeight: FontWeight.w700),
            ),
            const SizedBox(height: 14),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _qtyCtrl,
                    autofocus: true,
                    keyboardType: TextInputType.number,
                    decoration: InputDecoration(
                        labelText: S.of(context).quantity),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: TextField(
                    controller: _costCtrl,
                    keyboardType:
                        const TextInputType.numberWithOptions(decimal: true),
                    decoration: InputDecoration(
                        labelText: S.of(context).unitCost),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                Expanded(
                  child: TextField(
                    controller: _lotCtrl,
                    decoration: InputDecoration(
                        labelText: S.of(context).lotBatchOptional),
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: InkWell(
                    onTap: () async {
                      final picked = await showDatePicker(
                        context: context,
                        initialDate:
                            _expiry ?? DateTime.now().add(const Duration(days: 365)),
                        firstDate: DateTime.now(),
                        lastDate: DateTime.now()
                            .add(const Duration(days: 3650)),
                      );
                      if (picked != null) setState(() => _expiry = picked);
                    },
                    borderRadius: BorderRadius.circular(11),
                    child: InputDecorator(
                      decoration: InputDecoration(
                          labelText: S.of(context).expiryOptional),
                      child: Text(
                        _expiry == null ? '—' : formatDate(_expiry!),
                        style: GoogleFonts.instrumentSans(fontSize: 14),
                      ),
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            FilledButton(
              onPressed: _done,
              style: FilledButton.styleFrom(
                backgroundColor: context.colors.ink,
                padding: const EdgeInsets.symmetric(vertical: 15),
                shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(14)),
              ),
              child: Text(S.of(context).done),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Supplier picker sheet ─────────────────────────────────────────────────────

class _SupplierPickerSheet extends ConsumerStatefulWidget {
  const _SupplierPickerSheet();

  @override
  ConsumerState<_SupplierPickerSheet> createState() =>
      _SupplierPickerSheetState();
}

class _SupplierPickerSheetState extends ConsumerState<_SupplierPickerSheet> {
  final _searchCtrl = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FractionallySizedBox(
      heightFactor: 0.85,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 8, 8),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    S.of(context).selectSupplier,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: S.of(context).searchSupplierHint,
              onChanged: (v) => setState(() => _query = v.trim()),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: PagedListView<Supplier>(
              resetKey: _query,
              padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
              fetch: (page) async {
                final res = await ref
                    .read(suppliersRepositoryProvider)
                    .list(search: _query, page: page);
                return PagedChunk<Supplier>(
                  items: res.items,
                  totalCount: res.items.length,
                  hasMore: res.hasMore,
                );
              },
              emptyBuilder: (context) => EmptyView(
                message: S.of(context).noSuppliersFound,
                icon: Icons.store_outlined,
              ),
              itemBuilder: (context, s) => ListTile(
                leading:
                    Icon(Icons.store_outlined, color: context.colors.secondary),
                title: Text(s.name),
                subtitle: s.phone == null ? null : Text(s.phone!),
                onTap: () => Navigator.of(context).pop(s),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Product picker sheet ──────────────────────────────────────────────────────

class _ProductPickerSheet extends ConsumerStatefulWidget {
  const _ProductPickerSheet();

  @override
  ConsumerState<_ProductPickerSheet> createState() =>
      _ProductPickerSheetState();
}

class _ProductPickerSheetState extends ConsumerState<_ProductPickerSheet> {
  final _searchCtrl = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return FractionallySizedBox(
      heightFactor: 0.85,
      child: Column(
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 14, 8, 8),
            child: Row(
              children: [
                Expanded(
                  child: Text(
                    S.of(context).addItem,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 16, fontWeight: FontWeight.w700),
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 16),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: S.of(context).searchProductsHint,
              onChanged: (v) => setState(() => _query = v.trim()),
            ),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: PagedListView<Product>(
              resetKey: _query,
              padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
              fetch: (page) async {
                final res = await ref
                    .read(productsRepositoryProvider)
                    .search(query: _query, page: page);
                return PagedChunk<Product>(
                  items: res.data,
                  totalCount: res.pagination.totalCount,
                  hasMore: res.pagination.hasNextPage,
                );
              },
              emptyBuilder: (context) => EmptyView(
                message: S.of(context).noProductsFound,
                icon: Icons.search_off,
              ),
              itemBuilder: (context, p) => ListTile(
                leading: Icon(Icons.inventory_2_outlined,
                    color: context.colors.secondary),
                title: Text(p.name),
                subtitle: p.sku.isEmpty ? null : Text(p.sku),
                onTap: () => Navigator.of(context).pop(p),
              ),
            ),
          ),
        ],
      ),
    );
  }
}
