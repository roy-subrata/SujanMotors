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
            icon: const Icon(Icons.edit_outlined, size: 20),
            onPressed: () {},
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

class _Body extends ConsumerWidget {
  const _Body({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
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
        ref.invalidate(productMediaProvider(product.id));
      },
      child: Stack(
        children: [
          ListView(
            padding: const EdgeInsets.fromLTRB(16, 4, 16, 110),
            children: [
              // ── Hero card ──────────────────────────────────────────────
              _HeroCard(
                product: product,
                costPrice: costPrice,
                costCurrency: costCurrency,
              ),
              const SizedBox(height: 12),

              // ── 3-stat grid ────────────────────────────────────────────
              _StatsGrid(
                totalQty: stockLoading ? null : totalQty,
                reserved: stockLoading ? null : totalReserved,
                reorderAt: stockLoading ? null : reorderAt,
              ),
              const SizedBox(height: 12),

              // ── Stock by warehouse · lots ──────────────────────────────
              if (stockLoading)
                const _LoadingCard(height: 140)
              else if (warehouseIds.isNotEmpty)
                _WarehouseLotCard(
                  warehouseIds: warehouseIds,
                  levelByWarehouse: levelByWarehouse,
                  lotsByWarehouse: lotsByWarehouse,
                ),
              const SizedBox(height: 12),

              // ── Product info ───────────────────────────────────────────
              _ProductInfoCard(product: product),
              const SizedBox(height: 12),

              // ── Images ───────────────────────────────────────────────
              _ImagesCard(productId: product.id, productName: product.name),
              const SizedBox(height: 12),

              // ── Specifications (variant attributes) ────────────────────
              _SpecsCard(productId: product.id),

              // ── Storage locations ──────────────────────────────────────
              _LocationsCard(partId: product.id),

              // ── Vehicle compatibility ──────────────────────────────────
              _CompatibilityCard(partId: product.id),
            ],
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

// ── Hero card ─────────────────────────────────────────────────────────────────

class _HeroCard extends ConsumerWidget {
  const _HeroCard({
    required this.product,
    required this.costPrice,
    required this.costCurrency,
  });

  final Product product;
  final double? costPrice;
  final String? costCurrency;

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

    final skuMeta = [
      'SKU ${product.sku}',
      if (product.brand != null) product.brand!.name,
      if (product.category != null) product.category!.name,
    ].join(' · ');

    final images = (ref.watch(productMediaProvider(product.id)).value ??
            const <ProductMedia>[])
        .where((m) => m.isImage)
        .toList();

    return _SurfaceCard(
      padding: const EdgeInsets.all(14),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          _ProductThumb(images: images, productName: product.name),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  product.name,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 15,
                    fontWeight: FontWeight.w700,
                    height: 1.3
                  ),
                ),
                const SizedBox(height: 3),
                Text(
                  skuMeta,
                  style: GoogleFonts.instrumentSans(
                      fontSize: 12, color: AppColors.muted),
                ),
                const SizedBox(height: 6),
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
                        fontSize: 18,
                        fontWeight: FontWeight.w700
                      ),
                    ),
                    if (marginText != null) ...[
                      const SizedBox(width: 8),
                      Flexible(
                        child: Text(
                          marginText,
                          style: GoogleFonts.instrumentSans(
                              fontSize: 11.5, color: AppColors.muted),
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Product image thumbnail + gallery ─────────────────────────────────────────

/// 72×72 hero thumbnail. Shows the first product image (the API orders
/// primary-first) and opens a swipeable full-screen gallery on tap; falls back
/// to the checkerboard placeholder when the product has no images.
class _ProductThumb extends StatelessWidget {
  const _ProductThumb({required this.images, required this.productName});

  final List<ProductMedia> images;
  final String productName;

  @override
  Widget build(BuildContext context) {
    final thumb = ClipRRect(
      borderRadius: BorderRadius.circular(11),
      child: Container(
        width: 72,
        height: 72,
        decoration: BoxDecoration(
          border: Border.all(color: Theme.of(context).colorScheme.outline),
          borderRadius: BorderRadius.circular(11),
        ),
        child: images.isEmpty
            ? const _CheckerPlaceholder()
            : Image.network(
                images.first.resolvedUrl,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const _CheckerPlaceholder(),
                loadingBuilder: (context, child, progress) =>
                    progress == null ? child : const _CheckerPlaceholder(),
              ),
      ),
    );

    if (images.isEmpty) return thumb;

    return GestureDetector(
      onTap: () => Navigator.of(context).push(MaterialPageRoute<void>(
        builder: (_) =>
            _ImageGalleryScreen(images: images, title: productName),
      )),
      child: Stack(
        children: [
          thumb,
          if (images.length > 1)
            Positioned(
              right: 3,
              bottom: 3,
              child: Container(
                padding:
                    const EdgeInsets.symmetric(horizontal: 5, vertical: 1),
                decoration: BoxDecoration(
                  color: Colors.black.withAlpha(140),
                  borderRadius: BorderRadius.circular(7),
                ),
                child: Text(
                  '${images.length}',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 10,
                    fontWeight: FontWeight.w600,
                    color: Colors.white,
                  ),
                ),
              ),
            ),
        ],
      ),
    );
  }
}

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
                    fontSize: 11, color: AppColors.muted)),
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
                          fontWeight: FontWeight.w600,
                          color: AppColors.ink)),
                ),
                Text('FIFO',
                    style: GoogleFonts.instrumentSans(
                        fontSize: 11.5, color: AppColors.muted)),
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

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          decoration: BoxDecoration(
            color: Theme.of(context).colorScheme.surface,
            border:
                Border(top: BorderSide(color: Theme.of(context).colorScheme.outline.withAlpha(60))),
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
                        fontWeight: FontWeight.w600,
                        color: AppColors.ink)),
              ),
              Text(
                '$qty${unit.isNotEmpty ? ' $unit' : ' pcs'}',
                style: GoogleFonts.instrumentSans(
                    fontSize: 12.5,
                    fontWeight: FontWeight.w700,
                    color: AppColors.ink),
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
    return Container(
      decoration: BoxDecoration(
        border:
            Border(top: BorderSide(color: Theme.of(context).colorScheme.outline.withAlpha(60))),
      ),
      padding:
          const EdgeInsets.fromLTRB(37, 10, 15, 10),
      child: Row(
        children: [
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(lot.lotNumber,
                    style: GoogleFonts.instrumentSans(
                        fontSize: 12.5,
                        fontWeight: FontWeight.w500,
                        color: AppColors.ink)),
                const SizedBox(height: 1),
                Text(
                  'Recv ${formatDate(lot.receivingDate)}'
                  ' · cost $costLabel',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 11, color: AppColors.muted),
                ),
              ],
            ),
          ),
          Text(
            '${lot.quantityAvailable}'
            '${unit.isNotEmpty ? ' $unit' : ' pcs'}',
            style: GoogleFonts.instrumentSans(
                fontSize: 12.5,
                fontWeight: FontWeight.w600,
                color: AppColors.ink),
          ),
        ],
      ),
    );
  }
}

// ── Product info card ─────────────────────────────────────────────────────────

class _ProductInfoCard extends StatelessWidget {
  const _ProductInfoCard({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final rows = <(String, String)>[
      ('Part no.', product.partNumber),
      if ((product.oemNumber ?? '').isNotEmpty)
        ('OEM no.', product.oemNumber!),
      if ((product.barcode ?? '').isNotEmpty)
        ('Barcode', product.barcode!),
      if ((product.productType ?? '').isNotEmpty)
        ('Type', product.productType!),
      if (product.hasVariants) ('Variants', 'Yes'),
      if ((product.description ?? '').isNotEmpty)
        ('Notes', product.description!),
    ];

    if (rows.isEmpty) return const SizedBox.shrink();

    return _SectionCard(
      title: 'Product info',
      child: Column(
        children: rows.indexed.map((entry) {
          final (idx, row) = entry;
          final (label, value) = row;
          return Column(
            children: [
              if (idx > 0)
                Divider(
                    height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
              Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: 15, vertical: 10),
                child: Row(
                  crossAxisAlignment:
                      CrossAxisAlignment.start,
                  children: [
                    SizedBox(
                      width: 90,
                      child: Text(label,
                          style: GoogleFonts.instrumentSans(
                              fontSize: 12,
                              color: AppColors.muted)),
                    ),
                    Expanded(
                      child: Text(value,
                          style: GoogleFonts.instrumentSans(
                            fontSize: 12.5,
                            fontWeight: FontWeight.w500
                          )),
                    ),
                  ],
                ),
              ),
            ],
          );
        }).toList(),
      ),
    );
  }
}

// ── Images card (view + add + manage) ────────────────────────────────────────

/// Horizontal strip of the product's images with an add-photo tile.
/// Tap a thumbnail to open the gallery at that image; long-press for
/// set-primary / delete. Uploading needs the inventory.edit permission —
/// the API's 403 surfaces as a snackbar.
class _ImagesCard extends ConsumerStatefulWidget {
  const _ImagesCard({required this.productId, required this.productName});

  final String productId;
  final String productName;

  @override
  ConsumerState<_ImagesCard> createState() => _ImagesCardState();
}

class _ImagesCardState extends ConsumerState<_ImagesCard> {
  bool _busy = false;

  Future<void> _pickAndUpload(ImageSource source) async {
    final messenger = ScaffoldMessenger.of(context);
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
        backgroundColor: AppColors.red,
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
        backgroundColor: AppColors.red,
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
    setState(() => _busy = true);
    try {
      await action(ref.read(productsRepositoryProvider));
      ref.invalidate(productMediaProvider(widget.productId));
    } on AppException catch (e) {
      messenger.showSnackBar(SnackBar(
        content: Text(e.message),
        backgroundColor: AppColors.red,
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
                  _runMediaAction(
                      (repo) => repo.setPrimaryMedia(widget.productId, media.id));
                },
              ),
            ListTile(
              leading: const Icon(Icons.delete_outline_rounded,
                  color: AppColors.red),
              title: Text('Delete image',
                  style: GoogleFonts.instrumentSans(
                      fontSize: 14, color: AppColors.red)),
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
                        child: const Text('Delete',
                            style: TextStyle(color: AppColors.red)),
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

  @override
  Widget build(BuildContext context) {
    final images = (ref.watch(productMediaProvider(widget.productId)).value ??
            const <ProductMedia>[])
        .where((m) => m.isImage)
        .toList();

    return _SectionCard(
      title: 'Images',
      child: SizedBox(
        height: 96,
        child: ListView(
          scrollDirection: Axis.horizontal,
          padding: const EdgeInsets.fromLTRB(15, 12, 15, 12),
          children: [
            for (final (i, media) in images.indexed) ...[
              _ImageTile(
                media: media,
                onTap: () =>
                    Navigator.of(context).push(MaterialPageRoute<void>(
                  builder: (_) => _ImageGalleryScreen(
                    images: images,
                    title: widget.productName,
                    initialIndex: i,
                  ),
                )),
                onLongPress: _busy ? null : () => _showManageSheet(media),
              ),
              const SizedBox(width: 8),
            ],
            _AddImageTile(
              busy: _busy,
              onTap: _busy ? null : _showAddSheet,
            ),
          ],
        ),
      ),
    );
  }
}

class _ImageTile extends StatelessWidget {
  const _ImageTile({
    required this.media,
    required this.onTap,
    required this.onLongPress,
  });

  final ProductMedia media;
  final VoidCallback onTap;
  final VoidCallback? onLongPress;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      onLongPress: onLongPress,
      child: Stack(
        children: [
          ClipRRect(
            borderRadius: BorderRadius.circular(10),
            child: Container(
              width: 72,
              height: 72,
              decoration: BoxDecoration(
                border:
                    Border.all(color: Theme.of(context).colorScheme.outline),
                borderRadius: BorderRadius.circular(10),
              ),
              child: Image.network(
                media.resolvedUrl,
                fit: BoxFit.cover,
                errorBuilder: (_, _, _) => const _CheckerPlaceholder(),
                loadingBuilder: (context, child, progress) =>
                    progress == null ? child : const _CheckerPlaceholder(),
              ),
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
                    size: 12, color: Colors.amber),
              ),
            ),
        ],
      ),
    );
  }
}

class _AddImageTile extends StatelessWidget {
  const _AddImageTile({required this.busy, required this.onTap});

  final bool busy;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return GestureDetector(
      onTap: onTap,
      child: Container(
        width: 72,
        height: 72,
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: scheme.outline),
        ),
        alignment: Alignment.center,
        child: busy
            ? const SizedBox(
                width: 18,
                height: 18,
                child: CircularProgressIndicator(strokeWidth: 2),
              )
            : Column(
                mainAxisAlignment: MainAxisAlignment.center,
                children: [
                  const Icon(Icons.add_a_photo_outlined,
                      size: 20, color: AppColors.secondary),
                  const SizedBox(height: 3),
                  Text('Add',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 10.5, color: AppColors.muted)),
                ],
              ),
      ),
    );
  }
}

// ── Specs / variant attributes card ──────────────────────────────────────────

class _SpecsCard extends ConsumerWidget {
  const _SpecsCard({required this.productId});

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
              title: 'Specifications',
              child: Column(
                children: attrs.indexed.map((entry) {
                  final (idx, attr) = entry;
                  return Column(
                    children: [
                      if (idx > 0)
                        Divider(
                            height: 1,
                            color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                      Padding(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 15, vertical: 10),
                        child: Row(
                          crossAxisAlignment:
                              CrossAxisAlignment.start,
                          children: [
                            SizedBox(
                              width: 120,
                              child: Text(attr.attributeName,
                                  style:
                                      GoogleFonts.instrumentSans(
                                    fontSize: 12
                                  )),
                            ),
                            Expanded(
                              child: Text(attr.displayValue,
                                  style:
                                      GoogleFonts.instrumentSans(
                                    fontSize: 12.5,
                                    fontWeight: FontWeight.w500
                                  )),
                            ),
                          ],
                        ),
                      ),
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
                        Divider(
                            height: 1,
                            color: Theme.of(context).colorScheme.outline.withAlpha(60)),
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
                  ? AppColors.ink
                  : AppColors.secondary,
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
                      fontSize: 11.5, color: AppColors.muted),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Vehicle compatibility card ────────────────────────────────────────────────

class _CompatibilityCard extends ConsumerWidget {
  const _CompatibilityCard({required this.partId});

  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async =
        ref.watch(compatibleVehiclesProvider(partId));
    return async.when(
      loading: () => const SizedBox.shrink(),
      error: (_, _) => const SizedBox.shrink(),
      data: (items) {
        if (items.isEmpty) return const SizedBox.shrink();
        return Column(
          children: [
            const SizedBox(height: 12),
            _SectionCard(
              title: 'Vehicle compatibility',
              child: Column(
                children: items.indexed.map((entry) {
                  final (idx, item) = entry;
                  return Column(
                    children: [
                      if (idx > 0)
                        Divider(
                            height: 1,
                            color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                      _CompatRow(item: item),
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

class _CompatRow extends StatelessWidget {
  const _CompatRow({required this.item});

  final VehicleCompatibility item;

  @override
  Widget build(BuildContext context) {
    final ok = item.isCompatible;
    return Padding(
      padding:
          const EdgeInsets.fromLTRB(15, 11, 15, 11),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            width: 22,
            height: 22,
            margin: const EdgeInsets.only(top: 1),
            decoration: BoxDecoration(
              color: ok ? AppColors.greenBg : AppColors.redBg,
              borderRadius: BorderRadius.circular(7),
            ),
            alignment: Alignment.center,
            child: Icon(
              ok ? Icons.check_rounded : Icons.close_rounded,
              size: 14,
              color: ok ? AppColors.green : AppColors.red,
            ),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  item.title,
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13,
                    fontWeight: FontWeight.w600
                  ),
                ),
                if ((item.engineType ?? '').isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(item.engineType!,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11.5,
                          color: AppColors.muted)),
                ],
                if ((item.notes ?? '').isNotEmpty) ...[
                  const SizedBox(height: 2),
                  Text(item.notes!,
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11.5,
                          color: AppColors.secondary)),
                ],
                if (!ok) ...[
                  const SizedBox(height: 2),
                  Text('Not compatible',
                      style: GoogleFonts.instrumentSans(
                          fontSize: 11.5,
                          color: AppColors.red)),
                ],
              ],
            ),
          ),
        ],
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

    return Container(
      decoration: BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topCenter,
          end: Alignment.bottomCenter,
          stops: [0.0, 0.3],
          colors: [Colors.transparent, Theme.of(context).scaffoldBackgroundColor],
        ),
      ),
      padding:
          EdgeInsets.fromLTRB(16, 12, 16, 16 + bottomInset),
      child: Row(
        children: [
          Expanded(
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
                  color: Theme.of(context).colorScheme.surface,
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Theme.of(context).colorScheme.outline),
                ),
                alignment: Alignment.center,
                child: Text(
                  'Stock In',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(width: 8),
          Expanded(
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
                    backgroundColor: AppColors.red,
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
                  color: Theme.of(context).colorScheme.primary,
                  borderRadius: BorderRadius.circular(12),
                ),
                alignment: Alignment.center,
                child: Text(
                  'Add to sale',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 13.5,
                    fontWeight: FontWeight.w600,
                    color: Colors.white,
                  ),
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

/// White bordered card — no internal padding (caller controls it per section).
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
          Divider(height: 1, color: Theme.of(context).colorScheme.outline.withAlpha(60)),
          child,
        ],
      ),
    );
  }
}

/// Card with uniform padding — for the hero.
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
