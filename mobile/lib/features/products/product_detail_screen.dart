import 'dart:math' as math;

import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../features/pricing/price_code.dart';
import '../../features/sales/quick_sale_providers.dart';
import '../../features/stock/stock_adjustment_sheet.dart';
import '../../features/stock/stock_repository.dart';
import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/models/product_location.dart';
import '../../shared/models/product_media.dart';
import '../../shared/models/stock.dart';
import '../../shared/models/vehicle_compatibility.dart';
import '../../shared/widgets/state_views.dart';
import 'products_providers.dart';
import 'products_repository.dart';

/// A3 · Product detail — media gallery, specs, technical data & warehouse lots.
class ProductDetailScreen extends ConsumerWidget {
  const ProductDetailScreen({super.key, required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final productAsync = ref.watch(productDetailProvider(productId));
    final product = productAsync.value;
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);
    final codeConfigured = priceCode != null && priceCode.isConfigured;

    return Scaffold(
      appBar: AppBar(
        title: Text(
          product?.name ?? 'Product Detail',
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.instrumentSans(
            fontSize: 16,
            fontWeight: FontWeight.w700
          ),
        ),
        actions: [
          if (codeConfigured)
            IconButton(
              tooltip: showActual ? 'Hide cost prices' : 'Reveal cost prices',
              icon: Icon(
                showActual
                    ? Icons.visibility_off_outlined
                    : Icons.visibility_outlined,
                size: 20,
              ),
              onPressed: () =>
                  ref.read(showActualPriceProvider.notifier).toggle(),
            ),
          IconButton(
            tooltip: 'Edit product',
            icon: const Icon(Icons.edit_outlined, size: 20),
            onPressed: () => context.push('/product/$productId/edit'),
          ),
        ],
      ),
      body: productAsync.when(
        loading: () => const LoadingView(),
        error: (e, _) => ListView(children: [
          const SizedBox(height: 120),
          ErrorView(
            message:
                e is AppException ? e.message : 'Failed to load product.',
            onRetry: () =>
                ref.invalidate(productDetailProvider(productId)),
          ),
        ]),
        data: (product) => _Body(product: product),
      ),
    );
  }
}

// ── Body ─────────────────────────────────────────────────────────────────────

class _Body extends ConsumerStatefulWidget {
  const _Body({required this.product});

  final Product product;

  @override
  ConsumerState<_Body> createState() => _BodyState();
}

class _BodyState extends ConsumerState<_Body> {
  final _scrollController = ScrollController();
  int _activeSection = 0;

  @override
  void dispose() {
    _scrollController.dispose();
    super.dispose();
  }

  /// The pills behave as tabs: Overview stacks every section (as in the
  /// design), the other pills show just their own section.
  void _jumpToSection(int index) {
    if (index == _activeSection) return;
    setState(() => _activeSection = index);
  }

  @override
  Widget build(BuildContext context) {
    final product = widget.product;
    final levelsAsync = ref.watch(stockLevelsProvider(product.id));
    final lotsAsync = ref.watch(stockLotsProvider(product.id));

    final levels = levelsAsync.value ?? <StockLevel>[];
    final lots = lotsAsync.value ?? <StockLot>[];

    final totalQty =
        levels.fold<int>(0, (s, l) => s + l.availableQuantity);
    final totalReserved =
        levels.fold<int>(0, (s, l) => s + l.reservedQuantity);
    final reorderAt = levels.isNotEmpty
        ? levels
            .map((l) => l.reorderLevel)
            .reduce((a, b) => a > b ? a : b)
        : 0;

    final costPrice = lots.isNotEmpty ? lots.first.costPrice : null;
    final costCurrency = lots.firstOrNull?.currency;

    final lotsByWarehouse = <String, List<StockLot>>{};
    for (final lot in lots) {
      lotsByWarehouse.putIfAbsent(lot.warehouseId, () => []).add(lot);
    }
    final levelByWarehouse = {for (final l in levels) l.warehouseId: l};
    final warehouseIds = <String>{
      ...levelByWarehouse.keys,
      ...lotsByWarehouse.keys,
    }.toList();

    final stockLoading = levelsAsync.isLoading || lotsAsync.isLoading;

    return RefreshIndicator(
      onRefresh: () async {
        ref.invalidate(productDetailProvider(product.id));
        ref.invalidate(stockLevelsProvider(product.id));
        ref.invalidate(stockLotsProvider(product.id));
        ref.invalidate(compatibleVehiclesProvider(product.id));
        ref.invalidate(productLocationsProvider(product.id));
        ref.invalidate(productVariantAttributesProvider(product.id));
        ref.invalidate(productSpecificationsProvider(product.id));
        ref.invalidate(productMediaProvider(product.id));
      },
      child: Stack(
        children: [
          SingleChildScrollView(
            controller: _scrollController,
            physics: const AlwaysScrollableScrollPhysics(),
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // ── Media gallery ──────────────────────────────────────────
                _MediaGallery(
                  productId: product.id,
                  productName: product.name,
                  totalQty: stockLoading ? null : totalQty,
                ),
                const SizedBox(height: 12),

                // ── Title / price ──────────────────────────────────────────
                _TitleCard(
                  product: product,
                  costPrice: costPrice,
                  costCurrency: costCurrency,
                  hasWarranty: lots.any((l) => l.hasWarranty),
                ),
                const SizedBox(height: 12),

                // ── 3-stat grid ────────────────────────────────────────────
                _StatsGrid(
                  totalQty: stockLoading ? null : totalQty,
                  reserved: stockLoading ? null : totalReserved,
                  reorderAt: stockLoading ? null : reorderAt,
                ),
                const SizedBox(height: 12),

                // ── Section pill nav (tabs) ────────────────────────────────
                _SectionPillNav(
                  active: _activeSection,
                  onSelect: _jumpToSection,
                ),

                // ── Specifications (editable label/value) + details ────────
                if (_activeSection == 0 || _activeSection == 1) ...[
                  _ProductSpecsCard(productId: product.id),
                  _DetailsCard(product: product),
                  _TechSpecCard(productId: product.id),
                ],

                // ── Compatible vehicles ────────────────────────────────────
                if (_activeSection == 0 || _activeSection == 2)
                  _CompatibilityCard(partId: product.id),

                // ── Stock by warehouse · lots ──────────────────────────────
                if (_activeSection == 0 || _activeSection == 3) ...[
                  const SizedBox(height: 12),
                  if (stockLoading)
                    const _LoadingCard(height: 140)
                  else if (warehouseIds.isNotEmpty)
                    _WarehouseLotCard(
                      warehouseIds: warehouseIds,
                      levelByWarehouse: levelByWarehouse,
                      lotsByWarehouse: lotsByWarehouse,
                    )
                  else if (_activeSection == 3)
                    const _EmptyHintCard(
                        message: 'No stock lots for this product yet.'),

                  // ── Storage locations ────────────────────────────────────
                  _LocationsCard(partId: product.id),
                ],
              ],
            ),
          ),

          // ── Bottom gradient action bar ─────────────────────────────────
          Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            child: _BottomBar(product: product, levels: levels),
          ),
        ],
      ),
    );
  }
}

// ── Media gallery ─────────────────────────────────────────────────────────────

/// Design gallery block: 200px swipeable hero with counter + stock badges, and
/// a 52px thumbnail strip (active thumb ink-bordered). The trailing add tile
/// uploads from camera/gallery; long-press a thumb for set-primary / delete
/// (inventory.edit permission — the API's 403 surfaces as a snackbar).
class _MediaGallery extends ConsumerStatefulWidget {
  const _MediaGallery({
    required this.productId,
    required this.productName,
    required this.totalQty,
  });

  final String productId;
  final String productName;
  final int? totalQty;

  @override
  ConsumerState<_MediaGallery> createState() => _MediaGalleryState();
}

class _MediaGalleryState extends ConsumerState<_MediaGallery> {
  final _pageController = PageController();
  int _page = 0;
  bool _busy = false;

  @override
  void dispose() {
    _pageController.dispose();
    super.dispose();
  }

  Future<void> _pickAndUpload(ImageSource source) async {
    final messenger = ScaffoldMessenger.of(context);
    final errorColor = context.colors.red;
    final XFile? picked;
    try {
      picked = await ImagePicker().pickImage(
        source: source,
        maxWidth: 1600,
        maxHeight: 1600,
        imageQuality: 85,
      );
    } catch (_) {
      messenger.showSnackBar(SnackBar(
        content: Text(source == ImageSource.camera
            ? 'Could not open the camera.'
            : 'Could not open the photo gallery.'),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
      return;
    }
    if (picked == null || !mounted) return;

    setState(() => _busy = true);
    try {
      await ref.read(productsRepositoryProvider).uploadProductImage(
            productId: widget.productId,
            filePath: picked.path,
            fileName: picked.name,
          );
      ref.invalidate(productMediaProvider(widget.productId));
      messenger.showSnackBar(const SnackBar(
        content: Text('Image added'),
        duration: Duration(seconds: 2),
        behavior: SnackBarBehavior.floating,
      ));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _showAddSheet() {
    showModalBottomSheet<void>(
      context: context,
      useSafeArea: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: Text('Take photo',
                  style: GoogleFonts.instrumentSans(fontSize: 14)),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.camera);
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: Text('Choose from gallery',
                  style: GoogleFonts.instrumentSans(fontSize: 14)),
              onTap: () {
                Navigator.of(sheetContext).pop();
                _pickAndUpload(ImageSource.gallery);
              },
            ),
          ],
        ),
      ),
    );
  }

  Future<void> _runMediaAction(
      Future<void> Function(ProductsRepository repo) action) async {
    final messenger = ScaffoldMessenger.of(context);
    final errorColor = context.colors.red;
    setState(() => _busy = true);
    try {
      await action(ref.read(productsRepositoryProvider));
      ref.invalidate(productMediaProvider(widget.productId));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: errorColor,
        behavior: SnackBarBehavior.floating,
      ));
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  void _showManageSheet(ProductMedia media) {
    showModalBottomSheet<void>(
      context: context,
      useSafeArea: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (!media.isPrimary)
              ListTile(
                leading: const Icon(Icons.star_outline_rounded),
                title: Text('Set as primary',
                    style: GoogleFonts.instrumentSans(fontSize: 14)),
                onTap: () {
                  Navigator.of(sheetContext).pop();
                  _runMediaAction((repo) =>
                      repo.setPrimaryMedia(widget.productId, media.id));
                },
              ),
            ListTile(
              leading: Icon(Icons.delete_outline_rounded,
                  color: context.colors.red),
              title: Text('Delete image',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 14, color: context.colors.red)),
              onTap: () async {
                Navigator.of(sheetContext).pop();
                final confirmed = await showDialog<bool>(
                  context: context,
                  builder: (dialogContext) => AlertDialog(
                    title: const Text('Delete image?'),
                    content:
                        const Text('This removes the image from the product.'),
                    actions: [
                      TextButton(
                        onPressed: () =>
                            Navigator.of(dialogContext).pop(false),
                        child: const Text('Cancel'),
                      ),
                      TextButton(
                        onPressed: () => Navigator.of(dialogContext).pop(true),
                        child: Text('Delete',
                            style: TextStyle(color: context.colors.red)),
                      ),
                    ],
                  ),
                );
                if (confirmed == true) {
                  _runMediaAction(
                      (repo) => repo.deleteMedia(widget.productId, media.id));
                }
              },
            ),
          ],
        ),
      ),
    );
  }

  void _openGallery(List<ProductMedia> images, int index) {
    Navigator.of(context).push(MaterialPageRoute<void>(
      builder: (_) => _ImageGalleryScreen(
        images: images,
        title: widget.productName,
        initialIndex: index,
      ),
    ));
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final images = (ref.watch(productMediaProvider(widget.productId)).value ??
            const <ProductMedia>[])
        .where((m) => m.isImage)
        .toList();
    final page = math.min(_page, math.max(0, images.length - 1));

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        // Hero image
        ClipRRect(
          borderRadius: BorderRadius.circular(14),
          child: Container(
            height: 200,
            width: double.infinity,
            decoration: BoxDecoration(
              border: Border.all(color: scheme.outline),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Stack(
              fit: StackFit.expand,
              children: [
                if (images.isEmpty)
                  const _CheckerPlaceholder()
                else
                  PageView.builder(
                    controller: _pageController,
                    itemCount: images.length,
                    onPageChanged: (i) => setState(() => _page = i),
                    itemBuilder: (context, i) => GestureDetector(
                      onTap: () => _openGallery(images, i),
                      child: Image.network(
                        images[i].resolvedUrl,
                        fit: BoxFit.cover,
                        errorBuilder: (_, _, _) =>
                            const _CheckerPlaceholder(),
                        loadingBuilder: (context, child, progress) =>
                            progress == null
                                ? child
                                : const _CheckerPlaceholder(),
                      ),
                    ),
                  ),
                if (images.isNotEmpty)
                  Positioned(
                    top: 10,
                    left: 10,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 9, vertical: 3),
                      decoration: BoxDecoration(
                        color: context.colors.ink.withValues(alpha: 0.72),
                        borderRadius: BorderRadius.circular(99),
                      ),
                      child: Text(
                        '${page + 1} / ${images.length}',
                        style: GoogleFonts.instrumentSans(
                          fontSize: 10.5,
                          fontWeight: FontWeight.w700,
                          color: Colors.white,
                        ),
                      ),
                    ),
                  ),
                if (widget.totalQty != null)
                  Positioned(
                    top: 10,
                    right: 10,
                    child: _StockBadge(qty: widget.totalQty!),
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),

        // Thumbnail strip + add tile
        SizedBox(
          height: 52,
          child: ListView(
            scrollDirection: Axis.horizontal,
            children: [
              for (final (i, media) in images.indexed) ...[
                GestureDetector(
                  onTap: () => _pageController.animateToPage(i,
                      duration: const Duration(milliseconds: 250),
                      curve: Curves.easeOut),
                  onLongPress: _busy ? null : () => _showManageSheet(media),
                  child: Stack(
                    children: [
                      Container(
                        width: 52,
                        height: 52,
                        clipBehavior: Clip.antiAlias,
                        decoration: BoxDecoration(
                          borderRadius: BorderRadius.circular(10),
                          border: Border.all(
                            color: i == page ? scheme.onSurface : scheme.outline,
                            width: i == page ? 2 : 1,
                          ),
                        ),
                        child: Image.network(
                          media.resolvedUrl,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) =>
                              const _CheckerPlaceholder(),
                          loadingBuilder: (context, child, progress) =>
                              progress == null
                                  ? child
                                  : const _CheckerPlaceholder(),
                        ),
                      ),
                      if (media.isPrimary)
                        Positioned(
                          left: 3,
                          top: 3,
                          child: Container(
                            padding: const EdgeInsets.all(2),
                            decoration: BoxDecoration(
                              color: Colors.black.withAlpha(140),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: const Icon(Icons.star_rounded,
                                size: 10, color: Colors.amber),
                          ),
                        ),
                    ],
                  ),
                ),
                const SizedBox(width: 7),
              ],
              GestureDetector(
                onTap: _busy ? null : _showAddSheet,
                child: Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(color: scheme.outline),
                  ),
                  alignment: Alignment.center,
                  child: _busy
                      ? const SizedBox(
                          width: 16,
                          height: 16,
                          child: CircularProgressIndicator(strokeWidth: 2),
                        )
                      : Icon(Icons.add_a_photo_outlined,
                          size: 18, color: context.colors.secondary),
                ),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

/// Solid stock-level badge overlaid on the hero image (design: green in stock,
/// amber low, red out) — solid fills for contrast against photos.
class _StockBadge extends StatelessWidget {
  const _StockBadge({required this.qty});

  final int qty;

  @override
  Widget build(BuildContext context) {
    final (bg, label) = qty <= 0
        ? (context.colors.red, 'Out of stock')
        : qty <= 5
            ? (const Color(0xFFCB8600), '$qty left')
            : (context.colors.green, '$qty in stock');
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(99),
      ),
      child: Text(
        label,
        style: GoogleFonts.instrumentSans(
          fontSize: 10.5,
          fontWeight: FontWeight.w600,
          color: Colors.white,
        ),
      ),
    );
  }
}

// ── Title / price card ────────────────────────────────────────────────────────

class _TitleCard extends ConsumerWidget {
  const _TitleCard({
    required this.product,
    required this.costPrice,
    required this.costCurrency,
    required this.hasWarranty,
  });

  final Product product;
  final double? costPrice;
  final String? costCurrency;
  final bool hasWarranty;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);
    final price = product.pricing?.sellingPrice;

    String? marginText;
    if (price != null && price > 0 && costPrice != null && costPrice! > 0) {
      final costFormatted = formatCostMasked(
          priceCode, showActual, costPrice!, currency: costCurrency);
      if (showActual || priceCode == null || !priceCode.isConfigured) {
        final margin = ((price - costPrice!) / price * 100).round();
        marginText = 'cost $costFormatted · margin $margin%';
      } else {
        marginText = 'cost $costFormatted';
      }
    } else if (costPrice != null) {
      marginText = 'cost ${formatCostMasked(
          priceCode, showActual, costPrice!, currency: costCurrency)}';
    }

    final meta = [
      'SKU ${product.sku}',
      if ((product.barcode ?? '').isNotEmpty) 'Barcode ${product.barcode}',
      if (product.brand != null) product.brand!.name,
      if (product.category != null) product.category!.name,
    ].join(' · ');

    // Attribute tag chips (design: position / pack size / warranty).
    final attrs =
        ref.watch(productVariantAttributesProvider(product.id)).value ??
            const [];
    final chipLabels = attrs.map((a) => a.displayValue).take(3).toList();

    return _SurfaceCard(
      padding: const EdgeInsets.fromLTRB(15, 14, 15, 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            product.name,
            style: GoogleFonts.instrumentSans(
              fontSize: 15.5,
              fontWeight: FontWeight.w700,
              height: 1.3
            ),
          ),
          const SizedBox(height: 4),
          Text(
            meta,
            style: GoogleFonts.instrumentSans(
                fontSize: 12, color: context.colors.muted),
          ),
          const SizedBox(height: 8),
          Row(
            crossAxisAlignment: CrossAxisAlignment.baseline,
            textBaseline: TextBaseline.alphabetic,
            children: [
              Text(
                price != null
                    ? formatCurrency(price,
                        currency: product.pricing?.currency)
                    : '—',
                style: GoogleFonts.instrumentSans(
                  fontSize: 19,
                  fontWeight: FontWeight.w700
                ),
              ),
              if (marginText != null) ...[
                const SizedBox(width: 8),
                Flexible(
                  child: Text(
                    marginText,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 11.5, color: context.colors.muted),
                  ),
                ),
              ],
            ],
          ),
          if (chipLabels.isNotEmpty || hasWarranty) ...[
            const SizedBox(height: 8),
            Wrap(
              spacing: 6,
              runSpacing: 6,
              children: [
                for (final label in chipLabels) _TagChip(label: label),
                if (hasWarranty)
                  const _TagChip(label: 'Warranty', green: true),
              ],
            ),
          ],
        ],
      ),
    );
  }
}

class _TagChip extends StatelessWidget {
  const _TagChip({required this.label, this.green = false});

  final String label;
  final bool green;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 11, vertical: 5),
      decoration: BoxDecoration(
        color: green
            ? context.colors.greenBg
            : Theme.of(context).scaffoldBackgroundColor,
        borderRadius: BorderRadius.circular(99),
        border: Border.all(
            color: green ? const Color(0xFFCDEEDD) : scheme.outline),
      ),
      child: Text(
        label,
        style: GoogleFonts.instrumentSans(
          fontSize: 11,
          fontWeight: FontWeight.w600,
          color: green ? context.colors.green : context.colors.secondary,
        ),
      ),
    );
  }
}

// ── 3-stat grid ───────────────────────────────────────────────────────────────

class _StatsGrid extends StatelessWidget {
  const _StatsGrid({
    required this.totalQty,
    required this.reserved,
    required this.reorderAt,
  });

  final int? totalQty;
  final int? reserved;
  final int? reorderAt;

  @override
  Widget build(BuildContext context) {
    return Row(
      children: [
        _StatCell(
            label: 'Total stock',
            value: totalQty != null ? '$totalQty pcs' : '—'),
        const SizedBox(width: 8),
        _StatCell(
            label: 'Reserved',
            value: reserved != null ? '$reserved' : '—'),
        const SizedBox(width: 8),
        _StatCell(
            label: 'Reorder at',
            value: reorderAt != null ? '$reorderAt' : '—'),
      ],
    );
  }
}

class _StatCell extends StatelessWidget {
  const _StatCell({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Expanded(
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: Theme.of(context).colorScheme.outline),
        ),
        padding: const EdgeInsets.fromLTRB(12, 11, 12, 11),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label,
                style: GoogleFonts.instrumentSans(
                    fontSize: 11, color: context.colors.muted)),
            const SizedBox(height: 2),
            Text(value,
                style: GoogleFonts.instrumentSans(
                  fontSize: 16,
                  fontWeight: FontWeight.w700
                )),
          ],
        ),
      ),
    );
  }
}

// ── Section pill nav ──────────────────────────────────────────────────────────

class _SectionPillNav extends StatelessWidget {
  const _SectionPillNav({required this.active, required this.onSelect});

  static const _labels = [
    'Overview',
    'Specifications',
    'Compatibility',
    'Stock & lots',
  ];

  final int active;
  final ValueChanged<int> onSelect;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return SingleChildScrollView(
      scrollDirection: Axis.horizontal,
      child: Row(
        children: [
          for (final (i, label) in _labels.indexed) ...[
            if (i > 0) const SizedBox(width: 6),
            GestureDetector(
              onTap: () => onSelect(i),
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 13, vertical: 6),
                decoration: BoxDecoration(
                  color: i == active ? scheme.onSurface : scheme.surface,
                  borderRadius: BorderRadius.circular(99),
                  border: Border.all(
                    color: i == active ? scheme.onSurface : scheme.outline,
                  ),
                ),
                child: Text(
                  label,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 11.5,
                    fontWeight:
                        i == active ? FontWeight.w700 : FontWeight.w500,
                    color: i == active
                        ? scheme.surface
                        : context.colors.secondary,
                  ),
                ),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

// ── Details card (built-in product fields, 2-col key-value grid) ──────────────

class _DetailsCard extends StatelessWidget {
  const _DetailsCard({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final pairs = <(String, String)>[
      if (product.brand != null) ('Brand', product.brand!.name),
      if (product.category != null) ('Category', product.category!.name),
      ('Part no.', product.partNumber),
      if ((product.oemNumber ?? '').isNotEmpty)
        ('OEM no.', product.oemNumber!),
      if ((product.barcode ?? '').isNotEmpty) ('Barcode', product.barcode!),
      if ((product.productType ?? '').isNotEmpty)
        ('Type', product.productType!),
      if (product.hasVariants) ('Variants', 'Yes'),
    ];
    final notes = product.description ?? '';

    if (pairs.isEmpty && notes.isEmpty) return const SizedBox.shrink();

    return Column(
      children: [
        const SizedBox(height: 12),
        _SurfaceCard(
          padding: const EdgeInsets.fromLTRB(15, 13, 15, 13),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Details',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 14, fontWeight: FontWeight.w600)),
              const SizedBox(height: 9),
              for (var i = 0; i < pairs.length; i += 2) ...[
                if (i > 0) const SizedBox(height: 9),
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(child: _SpecCell(pair: pairs[i])),
                    const SizedBox(width: 14),
                    Expanded(
                      child: i + 1 < pairs.length
                          ? _SpecCell(pair: pairs[i + 1])
                          : const SizedBox.shrink(),
                    ),
                  ],
                ),
              ],
              if (notes.isNotEmpty) ...[
                const SizedBox(height: 9),
                Text('Notes',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 10.5, color: context.colors.muted)),
                const SizedBox(height: 1),
                Text(notes,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 12.5, fontWeight: FontWeight.w500)),
              ],
            ],
          ),
        ),
      ],
    );
  }
}

class _SpecCell extends StatelessWidget {
  const _SpecCell({required this.pair});

  final (String, String) pair;

  @override
  Widget build(BuildContext context) {
    final (label, value) = pair;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label,
            style: GoogleFonts.instrumentSans(
                fontSize: 10.5, color: context.colors.muted)),
        const SizedBox(height: 1),
        Text(value,
            style: GoogleFonts.instrumentSans(
                fontSize: 12.5, fontWeight: FontWeight.w500)),
      ],
    );
  }
}

// ── Editable product specs card (simple Label/Value) ──────────────────────────

class _ProductSpecsCard extends ConsumerWidget {
  const _ProductSpecsCard({required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final specs =
        ref.watch(productSpecificationsProvider(productId)).value ?? const [];

    return Column(
      children: [
        const SizedBox(height: 12),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(15, 12, 8, 8),
                child: Row(
                  children: [
                    Expanded(
                      child: Text('Specifications',
                          style: GoogleFonts.instrumentSans(
                              fontSize: 14, fontWeight: FontWeight.w600)),
                    ),
                    TextButton.icon(
                      onPressed: () =>
                          context.push('/product/$productId/specs'),
                      icon: Icon(specs.isEmpty
                          ? Icons.add
                          : Icons.edit_outlined, size: 16),
                      label: Text(specs.isEmpty ? 'Add' : 'Edit'),
                      style: TextButton.styleFrom(
                        foregroundColor: context.colors.ink,
                        textStyle: GoogleFonts.instrumentSans(
                            fontSize: 12.5, fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
              ),
              if (specs.isEmpty)
                Padding(
                  padding: const EdgeInsets.fromLTRB(15, 0, 15, 15),
                  child: Text('No specifications yet.',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12.5, color: context.colors.muted)),
                )
              else
                for (final spec in specs)
                  Container(
                    decoration: BoxDecoration(
                      border:
                          Border(top: BorderSide(color: _hairline(context))),
                    ),
                    padding: const EdgeInsets.symmetric(
                        horizontal: 15, vertical: 9),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Expanded(
                          child: Text(spec.label,
                              style: GoogleFonts.instrumentSans(
                                  fontSize: 12.5, color: context.colors.muted)),
                        ),
                        const SizedBox(width: 12),
                        Flexible(
                          child: Text(spec.value,
                              textAlign: TextAlign.right,
                              style: GoogleFonts.instrumentSans(
                                  fontSize: 12.5,
                                  fontWeight: FontWeight.w600)),
                        ),
                      ],
                    ),
                  ),
            ],
          ),
        ),
      ],
    );
  }
}

// ── Technical specification card (variant attributes) ─────────────────────────

class _TechSpecCard extends ConsumerWidget {
  const _TechSpecCard({required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async =
        ref.watch(productVariantAttributesProvider(productId));
    return async.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (attrs) {
        if (attrs.isEmpty) return const SizedBox.shrink();
        return Column(
          children: [
            const SizedBox(height: 12),
            _SectionCard(
              title: 'Technical specification',
              child: Column(
                children: [
                  for (final attr in attrs)
                    Container(
                      decoration: BoxDecoration(
                        border: Border(
                            top: BorderSide(color: _hairline(context))),
                      ),
                      padding: const EdgeInsets.symmetric(
                          horizontal: 15, vertical: 9),
                      child: Row(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Expanded(
                            child: Text(attr.attributeName,
                                style: GoogleFonts.instrumentSans(
                                    fontSize: 12.5,
                                    color: context.colors.muted)),
                          ),
                          const SizedBox(width: 12),
                          Flexible(
                            child: Text(attr.displayValue,
                                textAlign: TextAlign.right,
                                style: GoogleFonts.instrumentSans(
                                    fontSize: 12.5,
                                    fontWeight: FontWeight.w600)),
                          ),
                        ],
                      ),
                    ),
                ],
              ),
            ),
          ],
        );
      },
    );
  }
}

// ── Compatible vehicles card ──────────────────────────────────────────────────

class _CompatibilityCard extends ConsumerWidget {
  const _CompatibilityCard({required this.partId});

  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final items =
        ref.watch(compatibleVehiclesProvider(partId)).value ?? const [];

    return Column(
      children: [
        const SizedBox(height: 12),
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            borderRadius: BorderRadius.circular(14),
            border: Border.all(color: Theme.of(context).colorScheme.outline),
          ),
          clipBehavior: Clip.antiAlias,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Padding(
                padding: const EdgeInsets.fromLTRB(15, 12, 8, 8),
                child: Row(
                  children: [
                    Expanded(
                      child: Text('Compatible vehicles',
                          style: GoogleFonts.instrumentSans(
                              fontSize: 14, fontWeight: FontWeight.w600)),
                    ),
                    TextButton.icon(
                      onPressed: () =>
                          context.push('/product/$partId/compatibility'),
                      icon: Icon(
                          items.isEmpty ? Icons.add : Icons.edit_outlined,
                          size: 16),
                      label: Text(items.isEmpty ? 'Add' : 'Edit'),
                      style: TextButton.styleFrom(
                        foregroundColor: context.colors.ink,
                        textStyle: GoogleFonts.instrumentSans(
                            fontSize: 12.5, fontWeight: FontWeight.w600),
                      ),
                    ),
                  ],
                ),
              ),
              if (items.isEmpty)
                Padding(
                  padding: const EdgeInsets.fromLTRB(15, 0, 15, 15),
                  child: Text('No compatible vehicles yet.',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 12.5, color: context.colors.muted)),
                )
              else
                for (final item in items) _CompatRow(item: item),
            ],
          ),
        ),
      ],
    );
  }
}

class _CompatRow extends StatelessWidget {
  const _CompatRow({required this.item});

  final VehicleCompatibility item;

  @override
  Widget build(BuildContext context) {
    final ok = item.isCompatible;
    return Container(
      decoration: BoxDecoration(
        border: Border(top: BorderSide(color: _hairline(context))),
      ),
      padding: const EdgeInsets.fromLTRB(15, 8, 15, 8),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 22,
            height: 22,
            margin: const EdgeInsets.only(top: 1),
            decoration: BoxDecoration(
              color: ok ? context.colors.greenBg : context.colors.redBg,
              borderRadius: BorderRadius.circular(7),
            ),
            alignment: Alignment.center,
            child: Icon(
              ok ? Icons.check_rounded : Icons.close_rounded,
              size: 14,
              color: ok ? context.colors.green : context.colors.red,
            ),
          ),
          const SizedBox(width: 9),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w500
                  ),
                ),
                if ((item.notes ?? '').isNotEmpty)
                  Text(item.notes!,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11, color: context.colors.secondary)),
                if (!ok)
                  Text('Not compatible',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11, color: context.colors.red)),
              ],
            ),
          ),
          if ((item.engineType ?? '').isNotEmpty) ...[
            const SizedBox(width: 8),
            Text(item.engineType!,
                style: GoogleFonts.instrumentSans(
                    fontSize: 11, color: context.colors.muted)),
          ],
        ],
      ),
    );
  }
}

// ── Warehouse + lots card ─────────────────────────────────────────────────────

class _WarehouseLotCard extends StatelessWidget {
  const _WarehouseLotCard({
    required this.warehouseIds,
    required this.levelByWarehouse,
    required this.lotsByWarehouse,
  });

  final List<String> warehouseIds;
  final Map<String, StockLevel> levelByWarehouse;
  final Map<String, List<StockLot>> lotsByWarehouse;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding: const EdgeInsets.fromLTRB(15, 13, 15, 9),
            child: Row(
              children: [
                Expanded(
                  child: Text('Stock by warehouse · lots',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 14,
                          fontWeight: FontWeight.w600)),
                ),
                Text('FIFO',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 11.5, color: context.colors.muted)),
              ],
            ),
          ),
          for (final wid in warehouseIds)
            _WarehouseSection(
              level: levelByWarehouse[wid],
              lots: (lotsByWarehouse[wid] ?? [])
                ..sort((a, b) =>
                    a.receivingDate.compareTo(b.receivingDate)),
            ),
        ],
      ),
    );
  }
}

class _WarehouseSection extends StatelessWidget {
  const _WarehouseSection({required this.level, required this.lots});

  final StockLevel? level;
  final List<StockLot> lots;

  @override
  Widget build(BuildContext context) {
    final name = level?.warehouseName ??
        lots.firstOrNull?.warehouseName ??
        'Warehouse';
    final unit =
        level?.unitSymbol ?? level?.unitName ?? '';
    final qty = level?.availableQuantity ??
        lots.fold<int>(0, (s, l) => s + l.quantityAvailable);
    final isLight = Theme.of(context).brightness == Brightness.light;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          decoration: BoxDecoration(
            // Design: subtle #fafafb band behind the warehouse row.
            color: isLight
                ? context.colors.surfaceSubtle
                : Colors.white.withAlpha(10),
            border: Border(top: BorderSide(color: _hairline(context))),
          ),
          padding:
              const EdgeInsets.fromLTRB(15, 11, 15, 11),
          child: Row(
            children: [
              const Text('🏬',
                  style: TextStyle(fontSize: 13)),
              const SizedBox(width: 9),
              Expanded(
                child: Text(name,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 13,
                        fontWeight: FontWeight.w600)),
              ),
              Text(
                '$qty${unit.isNotEmpty ? ' $unit' : ' pcs'}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700),
              ),
            ],
          ),
        ),
        for (final lot in lots) _LotRow(lot: lot),
      ],
    );
  }
}

class _LotRow extends ConsumerWidget {
  const _LotRow({required this.lot});

  final StockLot lot;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);
    final unit = lot.unitCode ?? lot.unitName ?? '';
    final costLabel = formatCostMasked(
        priceCode, showActual, lot.costPrice,
        currency: lot.currency);
    final meta = [
      'Recv ${formatDate(lot.receivingDate)}',
      'cost $costLabel',
      if (lot.expiryDate != null) 'exp ${formatDate(lot.expiryDate!)}',
    ].join(' · ');

    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 15),
          child: _DashedDivider(color: _hairline(context)),
        ),
        Padding(
          padding: const EdgeInsets.fromLTRB(37, 10, 15, 10),
          child: Row(
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(lot.lotNumber,
                        style: GoogleFonts.instrumentSans(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w500)),
                    const SizedBox(height: 1),
                    Text(
                      meta,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11, color: context.colors.muted),
                    ),
                  ],
                ),
              ),
              Text(
                '${lot.quantityAvailable}'
                '${unit.isNotEmpty ? ' $unit' : ' pcs'}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w600),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

// ── Storage locations card ────────────────────────────────────────────────────

class _LocationsCard extends ConsumerWidget {
  const _LocationsCard({required this.partId});

  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(productLocationsProvider(partId));
    return async.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (locations) {
        if (locations.isEmpty) return const SizedBox.shrink();
        return Column(
          children: [
            const SizedBox(height: 12),
            _SectionCard(
              title: 'Storage locations',
              child: Column(
                children: locations.indexed.map((entry) {
                  final (idx, loc) = entry;
                  return Column(
                    children: [
                      if (idx > 0)
                        Divider(height: 1, color: _hairline(context)),
                      _LocationRow(location: loc),
                    ],
                  );
                }).toList(),
              ),
            ),
          ],
        );
      },
    );
  }
}

class _LocationRow extends StatelessWidget {
  const _LocationRow({required this.location});

  final ProductLocation location;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(15, 11, 15, 11),
      child: Row(
        children: [
          Container(
            width: 34,
            height: 34,
            decoration: BoxDecoration(
              color: location.isPrimary
                  ? const Color(0xFFEEF2FF)
                  : Theme.of(context).scaffoldBackgroundColor,
              borderRadius: BorderRadius.circular(10),
            ),
            alignment: Alignment.center,
            child: Icon(
              Icons.inbox_outlined,
              size: 17,
              color: location.isPrimary
                  ? context.colors.ink
                  : context.colors.secondary,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        location.warehouseName,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13,
                          fontWeight: FontWeight.w600
                        ),
                      ),
                    ),
                    if (location.isPrimary)
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 2),
                        decoration: BoxDecoration(
                          color: const Color(0xFFEEF2FF),
                          borderRadius:
                              BorderRadius.circular(99),
                        ),
                        child: Text('Primary',
                            style: GoogleFonts.instrumentSans(
                              fontSize: 10.5,
                              fontWeight: FontWeight.w600
                            )),
                      ),
                  ],
                ),
                const SizedBox(height: 2),
                Text(
                  'Section ${location.section} · Shelf ${location.shelf}',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 11.5, color: context.colors.muted),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Full-screen gallery ───────────────────────────────────────────────────────

/// Full-screen swipeable, pinch-to-zoom image gallery.
class _ImageGalleryScreen extends StatefulWidget {
  const _ImageGalleryScreen({
    required this.images,
    required this.title,
    this.initialIndex = 0,
  });

  final List<ProductMedia> images;
  final String title;
  final int initialIndex;

  @override
  State<_ImageGalleryScreen> createState() => _ImageGalleryScreenState();
}

class _ImageGalleryScreenState extends State<_ImageGalleryScreen> {
  late int _index = widget.initialIndex;
  late final PageController _controller =
      PageController(initialPage: widget.initialIndex);

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        foregroundColor: Colors.white,
        title: Text(
          widget.images.length > 1
              ? '${widget.title} (${_index + 1}/${widget.images.length})'
              : widget.title,
          maxLines: 1,
          overflow: TextOverflow.ellipsis,
          style: GoogleFonts.instrumentSans(
              fontSize: 15, fontWeight: FontWeight.w600),
        ),
      ),
      body: PageView.builder(
        controller: _controller,
        itemCount: widget.images.length,
        onPageChanged: (i) => setState(() => _index = i),
        itemBuilder: (context, i) => InteractiveViewer(
          maxScale: 5,
          child: Center(
            child: Image.network(
              widget.images[i].resolvedUrl,
              fit: BoxFit.contain,
              errorBuilder: (_, _, _) => const Icon(
                Icons.broken_image_outlined,
                color: Colors.white38,
                size: 48,
              ),
              loadingBuilder: (context, child, progress) => progress == null
                  ? child
                  : const Center(
                      child: SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(
                            strokeWidth: 2, color: Colors.white54),
                      ),
                    ),
            ),
          ),
        ),
      ),
    );
  }
}

// ── Bottom gradient action bar ────────────────────────────────────────────────

class _BottomBar extends ConsumerWidget {
  const _BottomBar({required this.product, required this.levels});

  final Product product;
  final List<StockLevel> levels;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final bottomInset = MediaQuery.paddingOf(context).bottom;
    final scheme = Theme.of(context).colorScheme;

    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          stops: const [0.0, 0.3],
          colors: [
            Colors.transparent,
            Theme.of(context).scaffoldBackgroundColor
          ],
        ),
      ),
      padding:
          EdgeInsets.fromLTRB(16, 12, 16, 16 + bottomInset),
      child: Row(
        children: [
          // Design: outlined Stock In (flex 1) + dark Add to cart (flex 1.4).
          Expanded(
            flex: 10,
            child: GestureDetector(
              onTap: () {
                showModalBottomSheet<void>(
                  context: context,
                  isScrollControlled: true,
                  useSafeArea: true,
                  backgroundColor: Colors.transparent,
                  builder: (_) => StockAdjustmentSheet(
                    product: product,
                    stockLevels: levels,
                  ),
                );
              },
              child: Container(
                padding:
                    const EdgeInsets.symmetric(vertical: 13),
                decoration: BoxDecoration(
                  color: scheme.surface,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: scheme.outline),
                ),
                alignment: Alignment.center,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    const Icon(Icons.file_upload_outlined, size: 16),
                    const SizedBox(width: 6),
                    Text(
                      'Stock In',
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
            flex: 14,
            child: GestureDetector(
              onTap: () {
                final added = ref
                    .read(quickSaleControllerProvider.notifier)
                    .addFromSearch(product);
                final messenger = ScaffoldMessenger.of(context);
                final router = GoRouter.of(context);
                messenger.clearSnackBars();
                if (!added) {
                  messenger.showSnackBar(SnackBar(
                    content:
                        Text('${product.name} is out of stock'),
                    backgroundColor: context.colors.red,
                    duration: const Duration(seconds: 2),
                    behavior: SnackBarBehavior.floating,
                  ));
                  return;
                }
                messenger.showSnackBar(SnackBar(
                  content:
                      Text('${product.name} added to sale'),
                  duration: const Duration(seconds: 2),
                  behavior: SnackBarBehavior.floating,
                  action: SnackBarAction(
                    label: 'Go to Sale',
                    onPressed: () {
                      messenger.hideCurrentSnackBar();
                      router.push('/quick-sale');
                    },
                  ),
                ));
              },
              child: Container(
                padding:
                    const EdgeInsets.symmetric(vertical: 13),
                decoration: BoxDecoration(
                  color: scheme.primary,
                  borderRadius: BorderRadius.circular(12),
                ),
                alignment: Alignment.center,
                child: Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(Icons.shopping_cart_outlined,
                        size: 16, color: scheme.onPrimary),
                    const SizedBox(width: 6),
                    Text(
                      'Add to cart',
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                        color: scheme.onPrimary,
                      ),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Shared layout helpers ─────────────────────────────────────────────────────

Color _hairline(BuildContext context) =>
    Theme.of(context).colorScheme.outline.withAlpha(60);

/// White bordered card with a titled header — no internal padding below the
/// header (caller controls it per row).
class _SectionCard extends StatelessWidget {
  const _SectionCard({required this.title, required this.child});

  final String title;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Padding(
            padding:
                const EdgeInsets.fromLTRB(15, 13, 15, 9),
            child: Text(
              title,
              style: GoogleFonts.instrumentSans(
                fontSize: 14,
                fontWeight: FontWeight.w600
              ),
            ),
          ),
          child,
        ],
      ),
    );
  }
}

/// Card with uniform padding.
class _SurfaceCard extends StatelessWidget {
  const _SurfaceCard(
      {required this.padding, required this.child});

  final EdgeInsets padding;
  final Widget child;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      padding: padding,
      child: child,
    );
  }
}

/// 1px dashed hairline (design: dashed dividers between lot rows).
class _DashedDivider extends StatelessWidget {
  const _DashedDivider({required this.color});

  final Color color;

  @override
  Widget build(BuildContext context) {
    return SizedBox(
      height: 1,
      width: double.infinity,
      child: CustomPaint(painter: _DashPainter(color: color)),
    );
  }
}

class _DashPainter extends CustomPainter {
  const _DashPainter({required this.color});

  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = color
      ..strokeWidth = 1;
    const dash = 4.0, gap = 3.0;
    var x = 0.0;
    while (x < size.width) {
      canvas.drawLine(Offset(x, 0.5),
          Offset(math.min(x + dash, size.width), 0.5), paint);
      x += dash + gap;
    }
  }

  @override
  bool shouldRepaint(covariant _DashPainter oldDelegate) =>
      oldDelegate.color != color;
}

/// Checkerboard image placeholder.
class _CheckerPlaceholder extends StatelessWidget {
  const _CheckerPlaceholder();

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return CustomPaint(
      size: const Size(72, 72),
      painter: _CheckerPainter(
        lightColor: Theme.of(context).scaffoldBackgroundColor,
        darkColor: scheme.outline.withAlpha(60),
      ),
    );
  }
}

class _CheckerPainter extends CustomPainter {
  const _CheckerPainter({required this.lightColor, required this.darkColor});
  final Color lightColor;
  final Color darkColor;

  @override
  void paint(Canvas canvas, Size size) {
    const tileSize = 10.0;
    final light = Paint()..color = lightColor;
    final dark = Paint()..color = darkColor;
    final cols = (size.width / tileSize).ceil();
    final rows = (size.height / tileSize).ceil();
    for (var r = 0; r < rows; r++) {
      for (var c = 0; c < cols; c++) {
        canvas.drawRect(
          Rect.fromLTWH(
              c * tileSize, r * tileSize, tileSize, tileSize),
          (r + c).isEven ? light : dark,
        );
      }
    }
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}

/// Muted placeholder card shown on a dedicated tab when it has no data.
class _EmptyHintCard extends StatelessWidget {
  const _EmptyHintCard({required this.message});

  final String message;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.symmetric(horizontal: 15, vertical: 22),
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      alignment: Alignment.center,
      child: Text(
        message,
        textAlign: TextAlign.center,
        style: GoogleFonts.instrumentSans(
            fontSize: 12.5, color: context.colors.muted),
      ),
    );
  }
}

/// Simple centred spinner card while stock data loads.
class _LoadingCard extends StatelessWidget {
  const _LoadingCard({required this.height});

  final double height;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: height,
      decoration: BoxDecoration(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: Theme.of(context).colorScheme.outline),
      ),
      child: const Center(
        child: SizedBox(
          width: 20,
          height: 20,
          child: CircularProgressIndicator(strokeWidth: 2),
        ),
      ),
    );
  }
}
