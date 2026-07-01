import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/models/sale.dart';
import '../../shared/widgets/app_scaffold.dart';
import 'charge_screen.dart';
import 'quick_sale_providers.dart';

class QuickSaleScreen extends ConsumerStatefulWidget {
  const QuickSaleScreen({super.key});

  @override
  ConsumerState<QuickSaleScreen> createState() => _QuickSaleScreenState();
}

class _QuickSaleScreenState extends ConsumerState<QuickSaleScreen> {
  // ── Scanner ───────────────────────────────────────────────────────────────────
  final MobileScannerController _scanner = MobileScannerController(
    detectionSpeed: DetectionSpeed.noDuplicates,
  );
  bool _handling = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      final s = ref.read(quickSaleControllerProvider);
      if (s.catalogItems.isEmpty && !s.isCatalogLoading) {
        ref.read(quickSaleControllerProvider.notifier).loadCatalog();
      }
    });
  }

  @override
  void dispose() {
    _scanner.dispose();
    super.dispose();
  }

  // ── Scanner ───────────────────────────────────────────────────────────────────

  Future<void> _onDetect(BarcodeCapture capture) async {
    if (_handling) return;
    final code = capture.barcodes
        .map((b) => b.rawValue)
        .firstWhere((v) => v != null && v.isNotEmpty, orElse: () => null);
    if (code == null) return;
    setState(() => _handling = true);
    await _scanner.stop();
    await ref.read(quickSaleControllerProvider.notifier).lookupByCode(code);
    if (!mounted) return;
    setState(() => _handling = false);
    if (ref.read(quickSaleControllerProvider).isScanning) {
      try {
        await _scanner.start();
      } on MobileScannerException {
        // errorBuilder handles error UI
      }
    }
  }

  void _startScan() =>
      ref.read(quickSaleControllerProvider.notifier).startScan();

  Future<void> _stopScan() async {
    await _scanner.stop();
    ref.read(quickSaleControllerProvider.notifier).stopScan();
  }

  void _openCatalog() {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      backgroundColor: Colors.transparent,
      builder: (_) => _CatalogSheet(
        onScanTapped: () {
          Navigator.pop(context);
          _startScan();
        },
      ),
    );
  }

  void _openChargeScreen() {
    final total = ref.read(quickSaleControllerProvider).total;
    Navigator.of(context).push(
      MaterialPageRoute<void>(
        builder: (_) => ChargeScreen(cartTotal: total),
      ),
    );
  }

  // ── Build ─────────────────────────────────────────────────────────────────────

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quickSaleControllerProvider);
    final controller = ref.read(quickSaleControllerProvider.notifier);

    if (state.result != null) {
      return AppScaffold(
        title: 'Quick Sale',
        showNotificationBell: false,
        body: _SuccessView(result: state.result!, onNewSale: controller.reset),
      );
    }

    if (state.isScanning) {
      return AppScaffold(
        title: 'Quick Sale',
        showNotificationBell: false,
        body: _buildScannerOverlay(state),
      );
    }

    return AppScaffold(
      title: 'Quick Sale',
      showBottomNav: true,
      actions: [
        IconButton(
          icon: const Icon(Icons.qr_code_scanner),
          tooltip: 'Scan barcode',
          onPressed: _startScan,
        ),
        IconButton(
          icon: const Icon(Icons.add_shopping_cart_outlined),
          tooltip: 'Browse products',
          onPressed: _openCatalog,
        ),
      ],
      body: _buildBody(state, controller),
    );
  }

  Widget _buildBody(QuickSaleState state, QuickSaleController controller) {
    if (state.isEmpty) {
      return _EmptyCartView(onScan: _startScan, onBrowse: _openCatalog);
    }

    return Column(
      children: [
        Expanded(
          child: ListView(
            padding: const EdgeInsets.fromLTRB(12, 14, 12, 14),
            children: [
              ...state.items.map(
                (item) => _CartItemTile(
                  key: ValueKey('${item.partId}${item.variantId}'),
                  item: item,
                  onIncrement: () =>
                      controller.increment(item.partId, item.variantId),
                  onDecrement: () =>
                      controller.decrement(item.partId, item.variantId),
                  onRemove: () =>
                      controller.remove(item.partId, item.variantId),
                  onPriceChanged: (p) =>
                      controller.updatePrice(item.partId, item.variantId, p),
                ),
              ),
            ],
          ),
        ),
        _ProceedBar(
          total: state.total,
          itemCount: state.itemCount,
          onProceed: _openChargeScreen,
        ),
      ],
    );
  }

  // ── Scanner overlay ───────────────────────────────────────────────────────────

  Widget _buildScannerOverlay(QuickSaleState state) {
    return Stack(
      alignment: Alignment.center,
      children: [
        MobileScanner(
          controller: _scanner,
          onDetect: _onDetect,
          errorBuilder: _buildCameraError,
        ),
        Container(
          width: 260,
          height: 160,
          decoration: BoxDecoration(
            border: Border.all(color: Colors.white, width: 2.5),
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        Positioned(
          top: 12,
          left: 12,
          child: _ScanIconButton(
              icon: Icons.close, tooltip: 'Cancel', onTap: _stopScan),
        ),
        Positioned(
          top: 12,
          right: 12,
          child: _ScanIconButton(
            icon: Icons.flash_on,
            tooltip: 'Torch',
            onTap: () => _scanner.toggleTorch(),
          ),
        ),
        const Positioned(
          bottom: 80,
          child: Text(
            'Point camera at a barcode',
            style: TextStyle(color: Colors.white, fontSize: 15),
          ),
        ),
        if (_handling || state.isLookingUp)
          const Positioned(
            bottom: 36,
            child: CircularProgressIndicator(color: Colors.white),
          ),
        if (state.lookupError != null && !_handling)
          Positioned(
            bottom: 28,
            left: 24,
            right: 24,
            child: Container(
              padding:
                  const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
              decoration: BoxDecoration(
                color: Colors.red.shade700,
                borderRadius: BorderRadius.circular(10),
              ),
              child: Text(
                state.lookupError!,
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.white),
              ),
            ),
          ),
      ],
    );
  }

  Widget _buildCameraError(
      BuildContext context, MobileScannerException error) {
    final isPermission =
        error.errorCode == MobileScannerErrorCode.permissionDenied;
    return Container(
      color: Colors.black,
      alignment: Alignment.center,
      padding: const EdgeInsets.all(24),
      child: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            isPermission ? Icons.no_photography : Icons.error_outline,
            color: Colors.white70,
            size: 64,
          ),
          const SizedBox(height: 16),
          Text(
            isPermission ? 'Camera access needed' : 'Camera error',
            textAlign: TextAlign.center,
            style: const TextStyle(
                color: Colors.white,
                fontSize: 18,
                fontWeight: FontWeight.w600),
          ),
          const SizedBox(height: 8),
          Text(
            isPermission
                ? 'Allow camera access in Settings, then tap Try again.'
                : (error.errorDetails?.message ??
                    'Could not start the camera.'),
            textAlign: TextAlign.center,
            style: const TextStyle(color: Colors.white70),
          ),
          const SizedBox(height: 20),
          FilledButton.icon(
            onPressed: () async {
              try {
                await _scanner.start();
              } on MobileScannerException {
                // errorBuilder will rebuild
              }
            },
            icon: const Icon(Icons.refresh),
            label: const Text('Try again'),
          ),
        ],
      ),
    );
  }
}

// ── Empty cart view ───────────────────────────────────────────────────────────

class _EmptyCartView extends StatelessWidget {
  const _EmptyCartView({required this.onScan, required this.onBrowse});

  final VoidCallback onScan;
  final VoidCallback onBrowse;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(Icons.shopping_cart_outlined,
                size: 80,
                color: scheme.primary.withValues(alpha: 0.15)),
            const SizedBox(height: 16),
            Text('Cart is empty',
                style: theme.textTheme.headlineSmall
                    ?.copyWith(fontWeight: FontWeight.w700)),
            const SizedBox(height: 8),
            Text(
              'Browse products or scan a barcode to add items',
              textAlign: TextAlign.center,
              style: theme.textTheme.bodyMedium
                  ?.copyWith(color: scheme.onSurfaceVariant),
            ),
            const SizedBox(height: 32),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onBrowse,
                icon: const Icon(Icons.add_shopping_cart_outlined),
                label: const Text('Browse Products'),
                style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14)),
              ),
            ),
            const SizedBox(height: 12),
            SizedBox(
              width: double.infinity,
              child: OutlinedButton.icon(
                onPressed: onScan,
                icon: const Icon(Icons.qr_code_scanner),
                label: const Text('Scan Barcode'),
                style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 14)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── Cart item tile ────────────────────────────────────────────────────────────

class _CartItemTile extends StatefulWidget {
  const _CartItemTile({
    super.key,
    required this.item,
    required this.onIncrement,
    required this.onDecrement,
    required this.onRemove,
    required this.onPriceChanged,
  });

  final QuickSaleItem item;
  final VoidCallback onIncrement;
  final VoidCallback onDecrement;
  final VoidCallback onRemove;
  final void Function(double) onPriceChanged;

  @override
  State<_CartItemTile> createState() => _CartItemTileState();
}

class _CartItemTileState extends State<_CartItemTile> {
  late final TextEditingController _priceCtrl;
  final FocusNode _priceFocus = FocusNode();

  @override
  void initState() {
    super.initState();
    _priceCtrl = TextEditingController(
        text: widget.item.unitPrice.toStringAsFixed(2));
    _priceFocus.addListener(() {
      if (!_priceFocus.hasFocus) _commit();
    });
  }

  @override
  void didUpdateWidget(_CartItemTile old) {
    super.didUpdateWidget(old);
    if (!_priceFocus.hasFocus &&
        old.item.unitPrice != widget.item.unitPrice) {
      _priceCtrl.text = widget.item.unitPrice.toStringAsFixed(2);
    }
  }

  @override
  void dispose() {
    _priceCtrl.dispose();
    _priceFocus.dispose();
    super.dispose();
  }

  void _commit() {
    final val = double.tryParse(_priceCtrl.text);
    if (val != null && val > 0) {
      widget.onPriceChanged(val);
    } else {
      _priceCtrl.text = widget.item.unitPrice.toStringAsFixed(2);
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final item = widget.item;

    return Card(
      elevation: 2,
      shadowColor: Colors.black12,
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 8, 16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Product name + delete ───────────────────────────────────
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        item.name,
                        style: theme.textTheme.titleMedium
                            ?.copyWith(fontWeight: FontWeight.w700),
                      ),
                      if (item.localName != null) ...[
                        const SizedBox(height: 3),
                        Text(
                          item.localName!,
                          style: theme.textTheme.bodyMedium?.copyWith(
                              color: scheme.onSurfaceVariant),
                        ),
                      ],
                    ],
                  ),
                ),
                IconButton(
                  icon: Icon(Icons.delete_outline,
                      size: 22,
                      color: scheme.error.withValues(alpha: 0.7)),
                  onPressed: widget.onRemove,
                  tooltip: 'Remove',
                ),
              ],
            ),

            const SizedBox(height: 12),
            const Divider(height: 1, color: Color(0xFFF0F0F0)),
            const SizedBox(height: 14),

            // ── Price + Qty ─────────────────────────────────────────────
            Row(
              crossAxisAlignment: CrossAxisAlignment.end,
              children: [
                // Price input
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Unit Price',
                          style: theme.textTheme.labelSmall?.copyWith(
                              color: scheme.onSurfaceVariant,
                              letterSpacing: 0.5)),
                      const SizedBox(height: 6),
                      Row(
                        children: [
                          Text('৳',
                              style: TextStyle(
                                color: scheme.primary,
                                fontWeight: FontWeight.w700,
                                fontSize: 18,
                              )),
                          const SizedBox(width: 6),
                          Expanded(
                            child: TextField(
                              controller: _priceCtrl,
                              focusNode: _priceFocus,
                              keyboardType:
                                  const TextInputType.numberWithOptions(
                                      decimal: true),
                              inputFormatters: [
                                FilteringTextInputFormatter.allow(
                                    RegExp(r'[0-9.]')),
                              ],
                              onSubmitted: (_) => _commit(),
                              style: TextStyle(
                                color: scheme.primary,
                                fontWeight: FontWeight.w700,
                                fontSize: 22,
                              ),
                              decoration: InputDecoration(
                                isDense: true,
                                contentPadding:
                                    const EdgeInsets.symmetric(vertical: 2),
                                border: const UnderlineInputBorder(),
                                focusedBorder: UnderlineInputBorder(
                                  borderSide: BorderSide(
                                      color: scheme.primary, width: 2),
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const SizedBox(width: 20),

                // Qty stepper
                Column(
                  crossAxisAlignment: CrossAxisAlignment.center,
                  children: [
                    Text('Quantity',
                        style: theme.textTheme.labelSmall?.copyWith(
                            color: scheme.onSurfaceVariant,
                            letterSpacing: 0.5)),
                    const SizedBox(height: 6),
                    Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _QtyButton(
                            icon: Icons.remove,
                            onTap: widget.onDecrement),
                        Padding(
                          padding:
                              const EdgeInsets.symmetric(horizontal: 14),
                          child: Text(
                            '${item.quantity}',
                            style: theme.textTheme.headlineSmall
                                ?.copyWith(fontWeight: FontWeight.w900),
                          ),
                        ),
                        _QtyButton(
                            icon: Icons.add, onTap: widget.onIncrement),
                      ],
                    ),
                  ],
                ),
              ],
            ),

            const SizedBox(height: 10),

            // ── Line total ──────────────────────────────────────────────
            Align(
              alignment: Alignment.centerRight,
              child: Container(
                padding: const EdgeInsets.symmetric(
                    horizontal: 12, vertical: 5),
                decoration: BoxDecoration(
                  color: scheme.primaryContainer.withValues(alpha: 0.5),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  formatCurrency(item.lineTotal),
                  style: TextStyle(
                    color: scheme.primary,
                    fontWeight: FontWeight.w800,
                    fontSize: 16,
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _QtyButton extends StatelessWidget {
  const _QtyButton({required this.icon, required this.onTap});

  final IconData icon;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      width: 42,
      height: 42,
      child: FilledButton.tonal(
        onPressed: onTap,
        style: FilledButton.styleFrom(
          padding: EdgeInsets.zero,
          shape: const CircleBorder(),
        ),
        child: Icon(icon, size: 20),
      ),
    );
  }
}

// ── Proceed bar (sticky bottom) ───────────────────────────────────────────────

class _ProceedBar extends StatelessWidget {
  const _ProceedBar({
    required this.total,
    required this.itemCount,
    required this.onProceed,
  });

  final double total;
  final int itemCount;
  final VoidCallback onProceed;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    return Container(
      decoration: BoxDecoration(
        color: scheme.surface,
        boxShadow: [
          BoxShadow(
            color: Colors.black.withValues(alpha: 0.08),
            blurRadius: 12,
            offset: const Offset(0, -2),
          ),
        ],
      ),
      child: SafeArea(
        top: false,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(16, 12, 16, 12),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    '$itemCount item${itemCount == 1 ? '' : 's'}',
                    style: theme.textTheme.bodyMedium
                        ?.copyWith(color: scheme.onSurfaceVariant),
                  ),
                  Text(
                    formatCurrency(total),
                    style: theme.textTheme.headlineSmall?.copyWith(
                      fontWeight: FontWeight.w900,
                      color: scheme.primary,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 10),
              SizedBox(
                width: double.infinity,
                height: 54,
                child: FilledButton.icon(
                  onPressed: onProceed,
                  style: FilledButton.styleFrom(
                    backgroundColor: const Color(0xFFD97706),
                    foregroundColor: Colors.white,
                    textStyle: const TextStyle(
                        fontSize: 17, fontWeight: FontWeight.w800),
                  ),
                  icon: const Icon(Icons.arrow_forward_rounded),
                  label: const Text('Proceed to Checkout'),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Catalog bottom sheet ──────────────────────────────────────────────────────

class _CatalogSheet extends ConsumerStatefulWidget {
  const _CatalogSheet({required this.onScanTapped});

  final VoidCallback onScanTapped;

  @override
  ConsumerState<_CatalogSheet> createState() => _CatalogSheetState();
}

class _CatalogSheetState extends ConsumerState<_CatalogSheet> {
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
    _searchCtrl.addListener(() => setState(() {}));
    WidgetsBinding.instance.addPostFrameCallback((_) {
      if (!mounted) return;
      ref.read(quickSaleControllerProvider.notifier).searchCatalog('');
      ref.read(quickSaleControllerProvider.notifier).setCategory(null);
    });
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _scrollCtrl.dispose();
    super.dispose();
  }

  void _onScroll() {
    if (_scrollCtrl.position.pixels >=
        _scrollCtrl.position.maxScrollExtent - 400) {
      ref.read(quickSaleControllerProvider.notifier).loadMoreCatalog();
    }
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(quickSaleControllerProvider);
    final controller = ref.read(quickSaleControllerProvider.notifier);
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final products = state.filteredCatalog;

    return DraggableScrollableSheet(
      expand: false,
      initialChildSize: 0.88,
      minChildSize: 0.5,
      maxChildSize: 0.95,
      builder: (_, sheetScroll) => Material(
        borderRadius: const BorderRadius.vertical(top: Radius.circular(20)),
        clipBehavior: Clip.antiAlias,
        child: Column(
          children: [
            Center(
              child: Container(
                margin: const EdgeInsets.symmetric(vertical: 10),
                width: 36,
                height: 4,
                decoration: BoxDecoration(
                  color: scheme.outlineVariant,
                  borderRadius: BorderRadius.circular(2),
                ),
              ),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 8, 8),
              child: Row(
                children: [
                  Text('Add Products',
                      style: theme.textTheme.titleLarge
                          ?.copyWith(fontWeight: FontWeight.w800)),
                  const Spacer(),
                  IconButton(
                    icon: const Icon(Icons.close),
                    onPressed: () => Navigator.pop(context),
                  ),
                ],
              ),
            ),
            _PosSearchBar(
              controller: _searchCtrl,
              onChanged: (q) => controller.searchCatalog(q),
              onClear: () {
                _searchCtrl.clear();
                controller.searchCatalog('');
              },
              onScan: widget.onScanTapped,
            ),
            if (state.allCategories.isNotEmpty)
              _CategoryChips(
                categories: state.allCategories,
                selected: state.selectedCategory,
                onSelect: controller.setCategory,
              ),
            Expanded(
              child: state.isCatalogLoading && products.isEmpty
                  ? const Center(child: CircularProgressIndicator())
                  : products.isEmpty
                      ? Center(
                          child: Text('No products found',
                              style: TextStyle(
                                  color: scheme.onSurfaceVariant)))
                      : GridView.builder(
                          controller: _scrollCtrl,
                          padding:
                              const EdgeInsets.fromLTRB(12, 8, 12, 16),
                          gridDelegate:
                              const SliverGridDelegateWithFixedCrossAxisCount(
                            crossAxisCount: 2,
                            crossAxisSpacing: 10,
                            mainAxisSpacing: 10,
                            childAspectRatio: 0.75,
                          ),
                          itemCount: products.length +
                              (state.catalogHasMore ? 2 : 0),
                          itemBuilder: (_, index) {
                            if (index >= products.length) {
                              return index == products.length
                                  ? const Center(
                                      child: CircularProgressIndicator())
                                  : const SizedBox.shrink();
                            }
                            final p = products[index];
                            return _PosProductCard(
                              product: p,
                              cartQty: state.items
                                  .where((i) => i.partId == p.id)
                                  .fold(0, (s, i) => s + i.quantity),
                              onAdd: () {
                                controller.addFromSearch(p);
                                ScaffoldMessenger.of(context).showSnackBar(
                                  SnackBar(
                                    content: Text('${p.name} added'),
                                    duration: const Duration(seconds: 1),
                                    behavior: SnackBarBehavior.floating,
                                  ),
                                );
                              },
                            );
                          },
                        ),
            ),
          ],
        ),
      ),
    );
  }
}

// ── POS search bar ────────────────────────────────────────────────────────────

class _PosSearchBar extends StatefulWidget {
  const _PosSearchBar({
    required this.controller,
    required this.onChanged,
    required this.onClear,
    required this.onScan,
  });

  final TextEditingController controller;
  final void Function(String) onChanged;
  final VoidCallback onClear;
  final VoidCallback onScan;

  @override
  State<_PosSearchBar> createState() => _PosSearchBarState();
}

class _PosSearchBarState extends State<_PosSearchBar> {
  @override
  void initState() {
    super.initState();
    widget.controller.addListener(() => setState(() {}));
  }

  @override
  Widget build(BuildContext context) {
    final hasText = widget.controller.text.isNotEmpty;
    return Padding(
      padding: const EdgeInsets.fromLTRB(12, 4, 12, 8),
      child: TextField(
        controller: widget.controller,
        textInputAction: TextInputAction.search,
        onChanged: widget.onChanged,
        decoration: InputDecoration(
          hintText: 'Search products...',
          prefixIcon: const Icon(Icons.search),
          border: const OutlineInputBorder(),
          isDense: true,
          suffixIcon: Row(
            mainAxisSize: MainAxisSize.min,
            children: [
              if (hasText)
                IconButton(
                    icon: const Icon(Icons.clear),
                    onPressed: widget.onClear),
              IconButton(
                  icon: const Icon(Icons.qr_code_scanner),
                  tooltip: 'Scan barcode',
                  onPressed: widget.onScan),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Category chips ────────────────────────────────────────────────────────────

class _CategoryChips extends StatelessWidget {
  const _CategoryChips({
    required this.categories,
    required this.selected,
    required this.onSelect,
  });

  final List<String> categories;
  final String? selected;
  final void Function(String?) onSelect;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 40,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 12),
        children: [
          _Chip(
              label: 'All',
              isSelected: selected == null,
              onTap: () => onSelect(null)),
          const SizedBox(width: 6),
          ...categories.map((cat) => Padding(
                padding: const EdgeInsets.only(right: 6),
                child: _Chip(
                  label: cat,
                  isSelected: selected == cat,
                  onTap: () => onSelect(cat),
                ),
              )),
        ],
      ),
    );
  }
}

class _Chip extends StatelessWidget {
  const _Chip(
      {required this.label,
      required this.isSelected,
      required this.onTap});

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onTap,
      child: AnimatedContainer(
        duration: const Duration(milliseconds: 150),
        padding:
            const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
        decoration: BoxDecoration(
          color: isSelected
              ? scheme.primary
              : scheme.surfaceContainerHighest,
          borderRadius: BorderRadius.circular(20),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.w600,
            color: isSelected
                ? scheme.onPrimary
                : scheme.onSurfaceVariant,
          ),
        ),
      ),
    );
  }
}

// ── POS product card ──────────────────────────────────────────────────────────

class _PosProductCard extends StatelessWidget {
  const _PosProductCard({
    required this.product,
    required this.cartQty,
    required this.onAdd,
  });

  final Product product;
  final int cartQty;
  final VoidCallback onAdd;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final price = product.pricing?.sellingPrice;
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();

    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(16),
        onTap: onAdd,
        child: Padding(
          padding: const EdgeInsets.fromLTRB(10, 12, 10, 10),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Center(
                child: Stack(
                  clipBehavior: Clip.none,
                  children: [
                    CircleAvatar(
                      radius: 24,
                      backgroundColor: scheme.primaryContainer,
                      child: Text(
                        initial,
                        style: TextStyle(
                          color: scheme.onPrimaryContainer,
                          fontWeight: FontWeight.w700,
                          fontSize: 16,
                        ),
                      ),
                    ),
                    if (cartQty > 0)
                      Positioned(
                        top: -4,
                        right: -4,
                        child: Container(
                          padding: const EdgeInsets.all(4),
                          decoration: BoxDecoration(
                            color: scheme.primary,
                            shape: BoxShape.circle,
                          ),
                          child: Text(
                            '$cartQty',
                            style: TextStyle(
                              color: scheme.onPrimary,
                              fontSize: 10,
                              fontWeight: FontWeight.w800,
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
              const SizedBox(height: 8),
              Text(
                product.name,
                maxLines: 2,
                overflow: TextOverflow.ellipsis,
                style: theme.textTheme.bodySmall
                    ?.copyWith(fontWeight: FontWeight.w700),
              ),
              if (product.localName != null) ...[
                const SizedBox(height: 2),
                Text(
                  product.localName!,
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodySmall?.copyWith(
                    color: scheme.onSurfaceVariant,
                    fontSize: 10,
                  ),
                ),
              ],
              const Spacer(),
              Row(
                children: [
                  Expanded(
                    child: price != null
                        ? Text(
                            formatCurrency(price,
                                currency: product.pricing?.currency),
                            style: TextStyle(
                              color: scheme.primary,
                              fontWeight: FontWeight.w800,
                              fontSize: 13,
                            ),
                          )
                        : const SizedBox.shrink(),
                  ),
                  SizedBox(
                    width: 32,
                    height: 32,
                    child: FilledButton(
                      onPressed: onAdd,
                      style: FilledButton.styleFrom(
                        padding: EdgeInsets.zero,
                        shape: const CircleBorder(),
                      ),
                      child: const Icon(Icons.add, size: 18),
                    ),
                  ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

// ── Scan helper button ────────────────────────────────────────────────────────

class _ScanIconButton extends StatelessWidget {
  const _ScanIconButton(
      {required this.icon, required this.tooltip, required this.onTap});

  final IconData icon;
  final String tooltip;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Tooltip(
      message: tooltip,
      child: GestureDetector(
        onTap: onTap,
        child: Container(
          width: 44,
          height: 44,
          decoration: BoxDecoration(
            color: Colors.black54,
            borderRadius: BorderRadius.circular(22),
          ),
          alignment: Alignment.center,
          child: Icon(icon, color: Colors.white, size: 22),
        ),
      ),
    );
  }
}

// ── Sale success view ─────────────────────────────────────────────────────────

class _SuccessView extends StatelessWidget {
  const _SuccessView({required this.result, required this.onNewSale});

  final QuickSaleResult result;
  final VoidCallback onNewSale;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 32),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              width: 88,
              height: 88,
              decoration: BoxDecoration(
                  color: Colors.green.shade50, shape: BoxShape.circle),
              child: Icon(Icons.check_circle_rounded,
                  size: 58, color: Colors.green.shade600),
            ),
            const SizedBox(height: 20),
            Text('Sale Complete!',
                style: theme.textTheme.headlineMedium
                    ?.copyWith(fontWeight: FontWeight.w800)),
            const SizedBox(height: 20),
            _SaleInfoRow(label: 'Invoice', value: result.invoiceNumber),
            const SizedBox(height: 6),
            _SaleInfoRow(
                label: 'Total',
                value: formatCurrency(result.grandTotal)),
            if (result.hasDue) ...[
              const SizedBox(height: 6),
              _SaleInfoRow(
                  label: 'Paid', value: formatCurrency(result.paidAmount)),
              const SizedBox(height: 4),
              _SaleInfoRow(
                label: 'Due',
                value: formatCurrency(result.dueAmount),
                valueColor: Colors.red.shade700,
              ),
            ],
            const SizedBox(height: 40),
            SizedBox(
              width: double.infinity,
              child: FilledButton.icon(
                onPressed: onNewSale,
                icon: const Icon(Icons.add_shopping_cart),
                label: const Text('New Sale',
                    style: TextStyle(fontSize: 17)),
                style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16)),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SaleInfoRow extends StatelessWidget {
  const _SaleInfoRow(
      {required this.label, required this.value, this.valueColor});

  final String label;
  final String value;
  final Color? valueColor;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Row(
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Text('$label: ',
            style: theme.textTheme.bodyLarge
                ?.copyWith(color: scheme.onSurfaceVariant)),
        Text(value,
            style: theme.textTheme.bodyLarge?.copyWith(
                fontWeight: FontWeight.w700, color: valueColor)),
      ],
    );
  }
}
