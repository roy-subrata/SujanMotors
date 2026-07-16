import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';
import 'stock_repository.dart';

enum StockMode { stockIn, stockOut, transfer, adjustment }


class StockAdjustmentSheet extends ConsumerStatefulWidget {
  const StockAdjustmentSheet({
    super.key,
    required this.product,
    required this.stockLevels,
    this.preselectedVariantId,
    this.initialMode = StockMode.stockIn,
  });

  final Product product;
  final List<StockLevel> stockLevels;
  final String? preselectedVariantId;
  final StockMode initialMode;

  @override
  ConsumerState<StockAdjustmentSheet> createState() =>
      _StockAdjustmentSheetState();
}

class _StockAdjustmentSheetState extends ConsumerState<StockAdjustmentSheet> {
  final _formKey = GlobalKey<FormState>();
  final _qtyCtrl = TextEditingController();
  final _notesCtrl = TextEditingController();

  late StockMode _mode;
  bool _isAdd = true; // direction for Adjustment mode
  String? _selectedVariantId;
  String? _selectedWarehouseId;
  String? _destinationWarehouseId; // transfer mode only
  String? _reason;
  bool _loading = false;
  String? _error;

  // "Purchase received" is intentionally NOT here — real purchases must go
  // through Stock In (GRN) so they create a priced FIFO lot.
  static const _stockInReasons = ['RETURN', 'FOUND'];
  static const _stockOutReasons = [
    'SALE',
    'INTERNAL_USE',
    'DAMAGED',
    'EXPIRED',
    'LOST',
    'SAMPLE',
  ];
  static const _adjustReasons = ['COUNT_CORRECTION'];

  static const _reasonLabels = {
    'RETURN': 'Customer return',
    'FOUND': 'Found in stock',
    'SALE': 'Sale (manual)',
    'INTERNAL_USE': 'Internal use',
    'DAMAGED': 'Damaged goods',
    'EXPIRED': 'Expired',
    'LOST': 'Lost / stolen',
    'SAMPLE': 'Sample / demo',
    'COUNT_CORRECTION': 'Count correction',
  };

  List<String> get _reasons {
    return switch (_mode) {
      StockMode.stockIn => _stockInReasons,
      StockMode.stockOut => _stockOutReasons,
      StockMode.transfer => const [],
      StockMode.adjustment => _adjustReasons,
    };
  }

  /// Available quantity for the currently selected variant + source warehouse.
  int? get _availableAtSelected {
    if (_selectedWarehouseId == null) return null;
    final matches = widget.stockLevels.where((l) =>
        l.warehouseId == _selectedWarehouseId &&
        (l.variantId == _selectedVariantId ||
            (_selectedVariantId == null && l.variantId == null)));
    if (matches.isEmpty) return null;
    return matches.fold<int>(0, (s, l) => s + l.availableQuantity);
  }

  @override
  void initState() {
    super.initState();
    _mode = widget.initialMode;
    _selectedVariantId = widget.preselectedVariantId;
    if (_selectedVariantId == null) {
      final vs = widget.product.variants.where((v) => v.id != null).toList();
      if (vs.length == 1) _selectedVariantId = vs.first.id;
    }
    final warehouses = _uniqueWarehouses();
    if (warehouses.length == 1) {
      _selectedWarehouseId = warehouses.first.warehouseId;
    }
    _reason = _reasons.first;
  }

  @override
  void dispose() {
    _qtyCtrl.dispose();
    _notesCtrl.dispose();
    super.dispose();
  }

  List<StockLevel> _uniqueWarehouses() {
    final seen = <String>{};
    return widget.stockLevels.where((l) => seen.add(l.warehouseId)).toList();
  }

  void _onModeChanged(StockMode mode) {
    setState(() {
      _mode = mode;
      _reason = _reasons.isEmpty ? null : _reasons.first;
    });
  }

  Future<void> _submit() async {
    if (!_formKey.currentState!.validate()) return;
    if (_selectedWarehouseId == null) {
      setState(() => _error = 'Select a warehouse');
      return;
    }
    if (_mode == StockMode.transfer) {
      await _submitTransfer();
      return;
    }

    final rawQty = int.tryParse(_qtyCtrl.text.trim()) ?? 0;
    final int qty;
    if (_mode == StockMode.stockOut) {
      qty = -rawQty;
    } else if (_mode == StockMode.adjustment && !_isAdd) {
      qty = -rawQty;
    } else {
      qty = rawQty;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      await ref.read(stockRepositoryProvider).adjustStock(
            partId: widget.product.id,
            variantId: _selectedVariantId,
            warehouseId: _selectedWarehouseId!,
            quantity: qty,
            reason: _reason ?? 'FOUND',
            notes: _notesCtrl.text.trim(),
          );

      ref.invalidate(stockLevelsProvider(widget.product.id));
      ref.invalidate(stockLotsProvider(widget.product.id));

      if (!mounted) return;
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(_successMessage(rawQty)),
          backgroundColor: _mode == StockMode.stockOut
              ? Colors.red.shade700
              : Colors.green.shade700,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } on AppException catch (e) {
      setState(() {
        _loading = false;
        _error = e.message;
      });
    }
  }

  Future<void> _submitTransfer() async {
    if (_destinationWarehouseId == null) {
      setState(() => _error = 'Select a destination warehouse');
      return;
    }
    if (_destinationWarehouseId == _selectedWarehouseId) {
      setState(() => _error = 'Source and destination must differ');
      return;
    }
    final rawQty = int.tryParse(_qtyCtrl.text.trim()) ?? 0;

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      await ref.read(stockRepositoryProvider).transferStock(
            partId: widget.product.id,
            variantId: _selectedVariantId,
            fromWarehouseId: _selectedWarehouseId!,
            toWarehouseId: _destinationWarehouseId!,
            quantity: rawQty,
            notes: _notesCtrl.text.trim(),
          );

      ref.invalidate(stockLevelsProvider(widget.product.id));
      ref.invalidate(stockLotsProvider(widget.product.id));

      if (!mounted) return;
      Navigator.of(context).pop();
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('$rawQty ${widget.product.unitName ?? 'units'} transferred'),
          backgroundColor: Colors.blue.shade700,
          behavior: SnackBarBehavior.floating,
        ),
      );
    } on AppException catch (e) {
      setState(() {
        _loading = false;
        _error = e.message;
      });
    }
  }

  String _successMessage(int qty) {
    final unit = widget.product.unitName ?? 'units';
    return switch (_mode) {
      StockMode.stockIn => '$qty $unit recorded as received',
      StockMode.stockOut => '$qty $unit recorded as out',
      StockMode.transfer => '$qty $unit transferred',
      StockMode.adjustment => 'Adjustment saved',
    };
  }

  Color get _submitColor => switch (_mode) {
        StockMode.stockIn => Colors.green.shade600,
        StockMode.stockOut => Colors.red.shade600,
        StockMode.transfer => Colors.blue.shade600,
        StockMode.adjustment => const Color(0xFFD97706),
      };

  String get _submitLabel => switch (_mode) {
        StockMode.stockIn => 'Record Stock In',
        StockMode.stockOut => 'Record Stock Out',
        StockMode.transfer => 'Transfer Stock',
        StockMode.adjustment => 'Save Adjustment',
      };

  IconData get _submitIcon => switch (_mode) {
        StockMode.stockIn => Icons.check_circle_outline,
        StockMode.stockOut => Icons.outbox_outlined,
        StockMode.transfer => Icons.swap_horiz,
        StockMode.adjustment => Icons.save_outlined,
      };

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final product = widget.product;
    final warehouses = _uniqueWarehouses();
    final hasVariants = product.hasVariants;
    final showDirectionToggle = _mode == StockMode.adjustment;

    return Material(
      borderRadius: const BorderRadius.vertical(top: Radius.circular(24)),
      clipBehavior: Clip.antiAlias,
      child: DraggableScrollableSheet(
        expand: false,
        initialChildSize: 0.78,
        minChildSize: 0.5,
        maxChildSize: 0.95,
        builder: (_, sheetScroll) => ListView(
          controller: sheetScroll,
          padding: EdgeInsets.only(
            left: 20,
            right: 20,
            top: 16,
            bottom: MediaQuery.of(context).viewInsets.bottom + 24,
          ),
          children: [
            // ── Drag handle ───────────────────────────────────────────────────
            Center(
              child: Container(
                width: 36,
                height: 4,
                decoration: BoxDecoration(
                  color: scheme.outlineVariant,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            const SizedBox(height: 16),

            // ── Header ────────────────────────────────────────────────────────
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        product.name,
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w800),
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                      ),
                      if (product.localName != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          product.localName!,
                          style: theme.textTheme.bodySmall
                              ?.copyWith(color: scheme.onSurfaceVariant),
                        ),
                      ],
                    ],
                  ),
                ),
                IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () => Navigator.of(context).pop(),
                ),
              ],
            ),
            const SizedBox(height: 20),

            Form(
              key: _formKey,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  // ── Mode toggle (chips wrap cleanly for 4 modes) ──────────
                  Wrap(
                    spacing: 8,
                    runSpacing: 8,
                    children: [
                      for (final (mode, icon, label) in const [
                        (StockMode.stockIn, Icons.move_to_inbox_outlined, 'In'),
                        (StockMode.stockOut, Icons.outbox_outlined, 'Out'),
                        (StockMode.transfer, Icons.swap_horiz, 'Transfer'),
                        (StockMode.adjustment, Icons.tune_outlined, 'Adjust'),
                      ])
                        ChoiceChip(
                          avatar: Icon(icon,
                              size: 17,
                              color: _mode == mode
                                  ? scheme.onPrimaryContainer
                                  : scheme.onSurfaceVariant),
                          label: Text(label),
                          selected: _mode == mode,
                          onSelected: (_) => _onModeChanged(mode),
                        ),
                    ],
                  ),
                  const SizedBox(height: 16),

                  // ── Stock In → GRN hint (priced purchases) ────────────────
                  if (_mode == StockMode.stockIn) ...[
                    _InfoBanner(
                      scheme: scheme,
                      icon: Icons.receipt_long_outlined,
                      message:
                          'Recording a supplier purchase? Use Stock In so it '
                          'creates a priced lot.',
                      actionLabel: 'Open Stock In',
                      onAction: () {
                        Navigator.of(context).pop();
                        context.push('/stock-in/new');
                      },
                    ),
                    const SizedBox(height: 16),
                  ],

                  // ── Variant selector ──────────────────────────────────────
                  if (hasVariants) ...[
                    _Label('Variant', scheme),
                    const SizedBox(height: 6),
                    DropdownButtonFormField<String>(
                      // `_selectedVariantId` is only ever set in initState() (before
                      // first build) or by this field's own onChanged below, so a
                      // one-time initialValue is equivalent to the deprecated `value`.
                      initialValue: _selectedVariantId,
                      hint: const Text('Select variant'),
                      decoration: const InputDecoration(isDense: true),
                      items: product.variants
                          .where((v) => v.id != null)
                          .map((v) => DropdownMenuItem(
                                value: v.id,
                                child: Text(v.name),
                              ))
                          .toList(),
                      validator: (v) => v == null ? 'Select a variant' : null,
                      onChanged: (v) =>
                          setState(() => _selectedVariantId = v),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // ── Warehouse ─────────────────────────────────────────────
                  _Label(
                      _mode == StockMode.transfer
                          ? 'From warehouse'
                          : 'Warehouse',
                      scheme),
                  const SizedBox(height: 6),
                  if (warehouses.isEmpty)
                    Container(
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: scheme.errorContainer.withValues(alpha: 0.35),
                        borderRadius: BorderRadius.circular(10),
                      ),
                      child: Row(
                        children: [
                          Icon(Icons.info_outline,
                              color: scheme.error, size: 18),
                          const SizedBox(width: 8),
                          Expanded(
                            child: Text(
                              'No warehouse found — initialize stock from the admin panel first.',
                              style: TextStyle(
                                  color: scheme.error, fontSize: 13),
                            ),
                          ),
                        ],
                      ),
                    )
                  else
                    DropdownButtonFormField<String>(
                      // `_selectedWarehouseId` is only ever set in initState() (before
                      // first build) or by this field's own onChanged below, so a
                      // one-time initialValue is equivalent to the deprecated `value`.
                      initialValue: _selectedWarehouseId,
                      hint: const Text('Select warehouse'),
                      decoration: const InputDecoration(isDense: true),
                      items: warehouses
                          .map((w) => DropdownMenuItem(
                                value: w.warehouseId,
                                child: Text(
                                  w.warehouseName ?? w.warehouseId,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ))
                          .toList(),
                      validator: (v) =>
                          v == null ? 'Select a warehouse' : null,
                      onChanged: (v) =>
                          setState(() => _selectedWarehouseId = v),
                    ),

                  // Available-stock hint for the selected source warehouse.
                  if (_availableAtSelected != null) ...[
                    const SizedBox(height: 6),
                    Text(
                      'Available here: $_availableAtSelected ${product.unitName ?? 'units'}',
                      style: theme.textTheme.bodySmall
                          ?.copyWith(color: scheme.onSurfaceVariant),
                    ),
                  ],
                  const SizedBox(height: 16),

                  // ── Destination warehouse (transfer only) ─────────────────
                  if (_mode == StockMode.transfer) ...[
                    _Label('To warehouse', scheme),
                    const SizedBox(height: 6),
                    DropdownButtonFormField<String>(
                      initialValue: _destinationWarehouseId,
                      hint: const Text('Select destination'),
                      decoration: const InputDecoration(isDense: true),
                      items: warehouses
                          .where((w) => w.warehouseId != _selectedWarehouseId)
                          .map((w) => DropdownMenuItem(
                                value: w.warehouseId,
                                child: Text(
                                  w.warehouseName ?? w.warehouseId,
                                  overflow: TextOverflow.ellipsis,
                                ),
                              ))
                          .toList(),
                      validator: (v) => _mode == StockMode.transfer && v == null
                          ? 'Select a destination'
                          : null,
                      onChanged: (v) =>
                          setState(() => _destinationWarehouseId = v),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // ── Quantity ──────────────────────────────────────────────
                  _Label(_qtyLabel, scheme),
                  const SizedBox(height: 6),
                  if (showDirectionToggle) ...[
                    Row(
                      children: [
                        Expanded(
                          child: _DirectionBtn(
                            label: '+ Add',
                            selected: _isAdd,
                            onTap: () => setState(() => _isAdd = true),
                            scheme: scheme,
                          ),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: _DirectionBtn(
                            label: '− Remove',
                            selected: !_isAdd,
                            onTap: () => setState(() => _isAdd = false),
                            scheme: scheme,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                  ],
                  TextFormField(
                    controller: _qtyCtrl,
                    keyboardType: TextInputType.number,
                    inputFormatters: [
                      FilteringTextInputFormatter.digitsOnly,
                    ],
                    decoration: InputDecoration(
                      hintText: '0',
                      prefixIcon: Icon(
                        _qtyIcon,
                        color: _qtyIconColor(scheme),
                      ),
                      isDense: true,
                    ),
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.w800,
                    ),
                    validator: (v) {
                      final n = int.tryParse(v ?? '');
                      if (n == null || n <= 0) {
                        return 'Enter a quantity greater than zero';
                      }
                      return null;
                    },
                  ),
                  const SizedBox(height: 16),

                  // ── Reason (not applicable to transfers) ──────────────────
                  if (_mode != StockMode.transfer) ...[
                    _Label('Reason', scheme),
                    const SizedBox(height: 6),
                    DropdownButtonFormField<String>(
                      // Unlike the fields above, `_reason` is reset externally by
                      // _onModeChanged() (not just this field's own onChanged), so it
                      // needs live rebuild syncing — `initialValue` (one-time only)
                      // would not reflect that reset in the UI.
                      // ignore: deprecated_member_use
                      value: _reason,
                      decoration: const InputDecoration(isDense: true),
                      items: _reasons
                          .map((r) => DropdownMenuItem(
                                value: r,
                                child: Text(_reasonLabels[r] ?? r),
                              ))
                          .toList(),
                      validator: (v) => _mode != StockMode.transfer && v == null
                          ? 'Select a reason'
                          : null,
                      onChanged: (v) => setState(() => _reason = v),
                    ),
                    const SizedBox(height: 16),
                  ],

                  // ── Notes ─────────────────────────────────────────────────
                  _Label('Notes (optional)', scheme),
                  const SizedBox(height: 6),
                  TextFormField(
                    controller: _notesCtrl,
                    maxLines: 2,
                    decoration: const InputDecoration(
                      hintText:
                          'Supplier invoice no., batch, delivery ref…',
                      isDense: true,
                    ),
                  ),

                  // ── Error ─────────────────────────────────────────────────
                  if (_error != null) ...[
                    const SizedBox(height: 12),
                    Text(
                      _error!,
                      style: TextStyle(color: scheme.error, fontSize: 13),
                      textAlign: TextAlign.center,
                    ),
                  ],
                  const SizedBox(height: 24),

                  // ── Submit ────────────────────────────────────────────────
                  SizedBox(
                    height: 52,
                    child: FilledButton.icon(
                      onPressed:
                          (_loading || warehouses.isEmpty) ? null : _submit,
                      style: FilledButton.styleFrom(
                        backgroundColor: _submitColor,
                        foregroundColor: Colors.white,
                        textStyle: const TextStyle(
                          fontSize: 16,
                          fontWeight: FontWeight.w700,
                        ),
                      ),
                      icon: _loading
                          ? const SizedBox(
                              width: 18,
                              height: 18,
                              child: CircularProgressIndicator(
                                strokeWidth: 2,
                                color: Colors.white,
                              ),
                            )
                          : Icon(_submitIcon),
                      label: Text(_submitLabel),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  String get _qtyLabel => switch (_mode) {
        StockMode.stockIn => 'Qty received',
        StockMode.stockOut => 'Qty going out',
        StockMode.transfer => 'Qty to transfer',
        StockMode.adjustment => 'Count difference',
      };

  IconData get _qtyIcon {
    if (_mode == StockMode.stockOut) return Icons.remove_circle_outline;
    if (_mode == StockMode.transfer) return Icons.swap_horiz;
    if (_mode == StockMode.adjustment && !_isAdd) {
      return Icons.remove_circle_outline;
    }
    return Icons.add_circle_outline;
  }

  Color _qtyIconColor(ColorScheme scheme) {
    if (_mode == StockMode.stockOut) return Colors.red.shade600;
    if (_mode == StockMode.transfer) return Colors.blue.shade600;
    if (_mode == StockMode.adjustment && !_isAdd) return scheme.error;
    return Colors.green.shade600;
  }
}

// ── Shared label widget ───────────────────────────────────────────────────────

class _Label extends StatelessWidget {
  const _Label(this.text, this.scheme);

  final String text;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) => Text(
        text,
        style: Theme.of(context).textTheme.labelMedium?.copyWith(
              color: scheme.onSurfaceVariant,
              fontWeight: FontWeight.w600,
            ),
      );
}

// ── Info banner (with optional action) ────────────────────────────────────────

class _InfoBanner extends StatelessWidget {
  const _InfoBanner({
    required this.scheme,
    required this.icon,
    required this.message,
    this.actionLabel,
    this.onAction,
  });

  final ColorScheme scheme;
  final IconData icon;
  final String message;
  final String? actionLabel;
  final VoidCallback? onAction;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.fromLTRB(12, 10, 8, 10),
      decoration: BoxDecoration(
        color: scheme.secondaryContainer.withValues(alpha: 0.4),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Icon(icon, size: 18, color: scheme.onSecondaryContainer),
          const SizedBox(width: 8),
          Expanded(
            child: Text(
              message,
              style: TextStyle(
                  fontSize: 12.5, color: scheme.onSecondaryContainer),
            ),
          ),
          if (actionLabel != null && onAction != null)
            TextButton(
              onPressed: onAction,
              style: TextButton.styleFrom(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                minimumSize: const Size(0, 32),
              ),
              child: Text(actionLabel!),
            ),
        ],
      ),
    );
  }
}

// ── Adjustment direction toggle ───────────────────────────────────────────────

class _DirectionBtn extends StatelessWidget {
  const _DirectionBtn({
    required this.label,
    required this.selected,
    required this.onTap,
    required this.scheme,
  });

  final String label;
  final bool selected;
  final VoidCallback onTap;
  final ColorScheme scheme;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding: const EdgeInsets.symmetric(vertical: 10),
        decoration: BoxDecoration(
          color: selected
              ? scheme.primaryContainer
              : scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: selected ? scheme.primary : scheme.outlineVariant,
            width: selected ? 1.5 : 0.8,
          ),
        ),
        child: Center(
          child: Text(
            label,
            style: TextStyle(
              fontWeight: FontWeight.w700,
              color: selected ? scheme.primary : scheme.onSurfaceVariant,
            ),
          ),
        ),
      ),
    );
  }
}
