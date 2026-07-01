import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../features/sales/quick_sale_providers.dart';
import '../../features/stock/stock_repository.dart';
import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';
import '../../shared/models/vehicle_compatibility.dart';
import '../../shared/widgets/state_views.dart';
import '../pricing/price_code.dart';
import 'products_providers.dart';

class ProductDetailScreen extends ConsumerWidget {
  const ProductDetailScreen({super.key, required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final productAsync = ref.watch(productDetailProvider(productId));
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Product Detail'),
        actions: [
          if (priceCode != null && priceCode.isConfigured)
            IconButton(
              tooltip: showActual ? 'Hide cost prices' : 'Reveal cost prices',
              icon: Icon(showActual
                  ? Icons.visibility_off_outlined
                  : Icons.visibility_outlined),
              onPressed: () =>
                  ref.read(showActualPriceProvider.notifier).toggle(),
            ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(productDetailProvider(productId));
          ref.invalidate(stockLevelsProvider(productId));
          ref.invalidate(stockLotsProvider(productId));
          ref.invalidate(compatibleVehiclesProvider(productId));
        },
        child: productAsync.when(
          loading: () => const LoadingView(),
          error: (e, _) => ListView(children: [
            const SizedBox(height: 120),
            ErrorView(
              message: e is AppException ? e.message : 'Failed to load product.',
              onRetry: () => ref.invalidate(productDetailProvider(productId)),
            ),
          ]),
          data: (product) => _ProductDetailBody(product: product),
        ),
      ),
    );
  }
}

// ── Detail body ───────────────────────────────────────────────────────────────

class _ProductDetailBody extends ConsumerWidget {
  const _ProductDetailBody({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final levelsAsync = ref.watch(stockLevelsProvider(product.id));
    final lotsAsync = ref.watch(stockLotsProvider(product.id));

    final levels = levelsAsync.value ?? <StockLevel>[];
    final lots = lotsAsync.value ?? <StockLot>[];

    // Aggregate totals across all warehouses
    final totalQty =
        levels.fold<int>(0, (s, l) => s + l.availableQuantity);
    final unit = levels.firstOrNull?.unitSymbol ??
        levels.firstOrNull?.unitName ??
        lots.firstOrNull?.unitCode ??
        lots.firstOrNull?.unitName ??
        '';
    final costPrice = lots.isNotEmpty ? lots.first.costPrice : null;
    final costCurrency = lots.firstOrNull?.currency;

    return ListView(
      padding: EdgeInsets.zero,
      children: [
        // ── 1. Product hero card ────────────────────────────────────────
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 14, 12, 0),
          child: _ProductHeroCard(
            product: product,
            totalQty: levelsAsync.isLoading ? null : totalQty,
            unit: unit,
            costPrice: costPrice,
            costCurrency: costCurrency,
          ),
        ),

        // ── 2. Quick action buttons ─────────────────────────────────────
        Padding(
          padding: const EdgeInsets.fromLTRB(12, 12, 12, 0),
          child: _ActionButtons(product: product),
        ),

        // ── 3. Product info table ───────────────────────────────────────
        const SizedBox(height: 16),
        _SectionHeader(icon: Icons.info_outline, label: 'Product Information'),
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: _InfoTable(product: product),
        ),

        // ── 4. Stock by warehouse ───────────────────────────────────────
        const SizedBox(height: 16),
        _SectionHeader(icon: Icons.warehouse_outlined, label: 'Stock by Warehouse'),
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: _StockSection(partId: product.id),
        ),

        // ── 5. Compatible vehicles ──────────────────────────────────────
        const SizedBox(height: 16),
        _SectionHeader(
            icon: Icons.directions_car_outlined, label: 'Compatible Vehicles'),
        const SizedBox(height: 8),
        Padding(
          padding: const EdgeInsets.symmetric(horizontal: 12),
          child: _CompatibilitySection(partId: product.id),
        ),

        // ── 6. Variants ─────────────────────────────────────────────────
        if (product.variants.length > 1) ...[
          const SizedBox(height: 16),
          _SectionHeader(icon: Icons.tune_outlined, label: 'Variants'),
          const SizedBox(height: 8),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Column(
              children:
                  product.variants.map((v) => _VariantTile(variant: v)).toList(),
            ),
          ),
        ],

        const SizedBox(height: 32),
      ],
    );
  }
}

// ── Product hero card: image LEFT, info RIGHT ─────────────────────────────────

class _ProductHeroCard extends ConsumerWidget {
  const _ProductHeroCard({
    required this.product,
    required this.totalQty,
    required this.unit,
    required this.costPrice,
    required this.costCurrency,
  });

  final Product product;
  final int? totalQty;
  final String unit;
  final double? costPrice;
  final String? costCurrency;

  static const _palette = [
    Color(0xFFC7D2FE), Color(0xFF99F6E4), Color(0xFFFDE68A),
    Color(0xFFFECDD3), Color(0xFFBBF7D0), Color(0xFFE9D5FF),
    Color(0xFFFED7AA), Color(0xFFA5F3FC),
  ];

  static const _paletteText = [
    Color(0xFF3730A3), Color(0xFF0F766E), Color(0xFF92400E),
    Color(0xFF9F1239), Color(0xFF166534), Color(0xFF6B21A8),
    Color(0xFF9A3412), Color(0xFF155E75),
  ];

  Color _bgColor() {
    final key = product.category?.name ?? product.brand?.name ?? product.name;
    return _palette[key.hashCode.abs() % _palette.length];
  }

  Color _textColor() {
    final key = product.category?.name ?? product.brand?.name ?? product.name;
    return _paletteText[key.hashCode.abs() % _paletteText.length];
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);

    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();
    final price = product.pricing?.sellingPrice;
    final inStock = (totalQty ?? 0) > 0;

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Row(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            // ── Image block ──────────────────────────────────────────────
            Column(
              children: [
                ClipRRect(
                  borderRadius: BorderRadius.circular(14),
                  child: Container(
                    width: 100,
                    height: 100,
                    color: _bgColor(),
                    alignment: Alignment.center,
                    child: Text(
                      initial,
                      style: TextStyle(
                        fontSize: 44,
                        fontWeight: FontWeight.w900,
                        color: _textColor(),
                      ),
                    ),
                  ),
                ),
                const SizedBox(height: 8),
                // Stock availability badge under the image
                Container(
                  padding: const EdgeInsets.symmetric(
                      horizontal: 10, vertical: 4),
                  decoration: BoxDecoration(
                    color: totalQty == null
                        ? scheme.surfaceContainerHighest
                        : inStock
                            ? Colors.green.shade100
                            : scheme.errorContainer,
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Row(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Icon(
                        totalQty == null
                            ? Icons.hourglass_empty
                            : inStock
                                ? Icons.check_circle
                                : Icons.remove_circle,
                        size: 12,
                        color: totalQty == null
                            ? scheme.onSurfaceVariant
                            : inStock
                                ? Colors.green.shade700
                                : scheme.error,
                      ),
                      const SizedBox(width: 4),
                      Text(
                        totalQty == null
                            ? 'Loading…'
                            : inStock
                                ? '$totalQty${unit.isNotEmpty ? ' $unit' : ''}'
                                : 'Out of stock',
                        style: TextStyle(
                          fontSize: 12,
                          fontWeight: FontWeight.w700,
                          color: totalQty == null
                              ? scheme.onSurfaceVariant
                              : inStock
                                  ? Colors.green.shade700
                                  : scheme.error,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),

            const SizedBox(width: 14),

            // ── Product info ─────────────────────────────────────────────
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // Name
                  Text(
                    product.name,
                    style: theme.textTheme.titleMedium
                        ?.copyWith(fontWeight: FontWeight.w800),
                  ),

                  // Local name
                  if (product.localName != null) ...[
                    const SizedBox(height: 3),
                    Text(
                      product.localName!,
                      style: theme.textTheme.bodySmall
                          ?.copyWith(color: scheme.onSurfaceVariant),
                    ),
                  ],

                  // Brand chip
                  if (product.brand != null) ...[
                    const SizedBox(height: 6),
                    Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(
                        color: Colors.amber.shade50,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(Icons.verified_outlined,
                              size: 12, color: Colors.amber.shade800),
                          const SizedBox(width: 4),
                          Text(
                            product.brand!.name,
                            style: TextStyle(
                              fontSize: 11,
                              fontWeight: FontWeight.w700,
                              color: Colors.amber.shade800,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],

                  const SizedBox(height: 10),
                  const Divider(height: 1),
                  const SizedBox(height: 10),

                  // Info rows
                  _InfoRow('Part No', product.partNumber),
                  _InfoRow('SKU', product.sku),
                  if (unit.isNotEmpty)
                    _InfoRow('Unit', unit),
                  // Stock quantity — prominent, coloured
                  if (totalQty != null)
                    _InfoRow(
                      'Stock',
                      totalQty == 0
                          ? 'Out of stock'
                          : '$totalQty${unit.isNotEmpty ? ' $unit' : ''}',
                      valueColor: (totalQty ?? 0) > 0
                          ? Colors.green.shade700
                          : scheme.error,
                      valueBold: true,
                    ),
                  if (price != null)
                    _InfoRow(
                      'Price',
                      formatCurrency(price,
                          currency: product.pricing?.currency),
                      valueColor: scheme.primary,
                      valueBold: true,
                    ),
                  if (costPrice != null)
                    _InfoRow(
                      'Cost',
                      formatCostMasked(priceCode, showActual, costPrice!,
                          currency: costCurrency),
                      valueColor: scheme.onSurfaceVariant,
                    ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _InfoRow extends StatelessWidget {
  const _InfoRow(this.label, this.value,
      {this.valueColor, this.valueBold = false});

  final String label;
  final String value;
  final Color? valueColor;
  final bool valueBold;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    return Padding(
      padding: const EdgeInsets.only(bottom: 5),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 52,
            child: Text(
              label,
              style: theme.textTheme.bodySmall
                  ?.copyWith(color: scheme.onSurfaceVariant),
            ),
          ),
          const SizedBox(width: 6),
          Expanded(
            child: Text(
              value,
              style: theme.textTheme.bodySmall?.copyWith(
                color: valueColor,
                fontWeight:
                    valueBold ? FontWeight.w700 : FontWeight.w600,
              ),
            ),
          ),
        ],
      ),
    );
  }
}

// ── Action buttons ────────────────────────────────────────────────────────────

class _ActionButtons extends ConsumerWidget {
  const _ActionButtons({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return Row(
      children: [
        // Add to Sale — amber (matches reference PAY button)
        Expanded(
          child: _ActionBtn(
            icon: Icons.add_shopping_cart_outlined,
            label: 'Add to Sale',
            color: Colors.white,
            bg: const Color(0xFFF59E0B),
            onTap: () {
              ref
                  .read(quickSaleControllerProvider.notifier)
                  .addFromSearch(product);
              ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                content: Text('${product.name} added to sale'),
                duration: const Duration(seconds: 2),
                behavior: SnackBarBehavior.floating,
                action: SnackBarAction(
                  label: 'Go to Sale',
                  onPressed: () => context.push('/quick-sale'),
                ),
              ));
            },
          ),
        ),
        const SizedBox(width: 10),
        // Stock In — green
        Expanded(
          child: _ActionBtn(
            icon: Icons.move_to_inbox_outlined,
            label: 'Stock In',
            color: Colors.white,
            bg: Colors.green.shade600,
            onTap: () {
              ScaffoldMessenger.of(context).showSnackBar(const SnackBar(
                content: Text('Stock In — coming soon'),
                behavior: SnackBarBehavior.floating,
                duration: Duration(seconds: 2),
              ));
            },
          ),
        ),
      ],
    );
  }
}

class _ActionBtn extends StatelessWidget {
  const _ActionBtn({
    required this.icon,
    required this.label,
    required this.color,
    required this.bg,
    required this.onTap,
  });

  final IconData icon;
  final String label;
  final Color color;
  final Color bg;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
        color: bg,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: onTap,
          child: Padding(
            padding:
                const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Icon(icon, color: color, size: 20),
                const SizedBox(width: 8),
                Text(
                  label,
                  style: TextStyle(
                    color: color,
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                  ),
                ),
              ],
            ),
          ),
        ),
      );
}

// ── Info table: label LEFT, value RIGHT ───────────────────────────────────────

class _InfoTable extends StatelessWidget {
  const _InfoTable({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    final rows = <(String, String)>[
      if (product.oemNumber != null && product.oemNumber!.isNotEmpty)
        ('OEM Number', product.oemNumber!),
      if (product.barcode != null && product.barcode!.isNotEmpty)
        ('Barcode', product.barcode!),
      if (product.category != null)
        ('Category', product.category!.name),
      if (product.brand != null)
        ('Brand', product.brand!.name),
      if (product.productType != null && product.productType!.isNotEmpty)
        ('Type', product.productType!),
      ('Status', product.isActive ? 'Active' : 'Inactive'),
      if (product.hasVariants) ('Variants', 'Yes'),
      if (product.description != null && product.description!.isNotEmpty)
        ('Description', product.description!),
    ];

    if (rows.isEmpty) return const SizedBox.shrink();

    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      child: Column(
        children: rows.indexed.map((entry) {
          final (idx, row) = entry;
          final (label, value) = row;
          final isLast = idx == rows.length - 1;
          final isStatus = label == 'Status';
          final isActive = value == 'Active';

          return Column(
            children: [
              Padding(
                padding: const EdgeInsets.symmetric(
                    horizontal: 16, vertical: 12),
                child: Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    // Label
                    SizedBox(
                      width: 110,
                      child: Text(
                        label,
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: scheme.onSurfaceVariant,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                    ),
                    // Value
                    Expanded(
                      child: isStatus
                          ? _StatusBadge(isActive: isActive)
                          : Text(
                              value,
                              style: theme.textTheme.bodySmall?.copyWith(
                                fontWeight: FontWeight.w600,
                                color: scheme.onSurface,
                              ),
                            ),
                    ),
                  ],
                ),
              ),
              if (!isLast)
                Divider(
                    height: 1,
                    indent: 16,
                    endIndent: 16,
                    color: scheme.outlineVariant),
            ],
          );
        }).toList(),
      ),
    );
  }
}

class _StatusBadge extends StatelessWidget {
  const _StatusBadge({required this.isActive});
  final bool isActive;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 3),
        decoration: BoxDecoration(
          color: isActive ? Colors.green.shade50 : Colors.grey.shade100,
          borderRadius: BorderRadius.circular(20),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(
              isActive ? Icons.check_circle : Icons.cancel,
              size: 12,
              color: isActive ? Colors.green.shade700 : Colors.grey.shade500,
            ),
            const SizedBox(width: 4),
            Text(
              isActive ? 'Active' : 'Inactive',
              style: TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color:
                    isActive ? Colors.green.shade700 : Colors.grey.shade600,
              ),
            ),
          ],
        ),
      );
}

// ── Section header ────────────────────────────────────────────────────────────

class _SectionHeader extends StatelessWidget {
  const _SectionHeader({required this.icon, required this.label});
  final IconData icon;
  final String label;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 12),
      child: Row(
        children: [
          Icon(icon, size: 18, color: scheme.primary),
          const SizedBox(width: 8),
          Text(label,
              style: Theme.of(context)
                  .textTheme
                  .titleSmall
                  ?.copyWith(fontWeight: FontWeight.w700)),
        ],
      ),
    );
  }
}

// ── Stock section ─────────────────────────────────────────────────────────────

class _StockSection extends ConsumerWidget {
  const _StockSection({required this.partId});
  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final levelsAsync = ref.watch(stockLevelsProvider(partId));
    final lotsAsync = ref.watch(stockLotsProvider(partId));

    if (levelsAsync.isLoading || lotsAsync.isLoading) {
      return const Padding(
          padding: EdgeInsets.symmetric(vertical: 24), child: LoadingView());
    }

    final levelsErr = levelsAsync.hasError;
    final lotsErr = lotsAsync.hasError;

    if (levelsErr && lotsErr) {
      final error = levelsAsync.error;
      return ErrorView(
        message:
            error is AppException ? error.message : 'Failed to load stock.',
        onRetry: () {
          ref.invalidate(stockLevelsProvider(partId));
          ref.invalidate(stockLotsProvider(partId));
        },
      );
    }

    final levels = levelsAsync.value ?? <StockLevel>[];
    final lots = lotsAsync.value ?? <StockLot>[];

    final lotsByWarehouse = <String, List<StockLot>>{};
    for (final lot in lots) {
      lotsByWarehouse.putIfAbsent(lot.warehouseId, () => []).add(lot);
    }
    final levelByWarehouse = {for (final l in levels) l.warehouseId: l};
    final warehouseIds = <String>{
      ...levelByWarehouse.keys,
      ...lotsByWarehouse.keys,
    };

    if (warehouseIds.isEmpty) {
      return const EmptyView(
          message: 'No stock records for this part yet.',
          icon: Icons.warehouse_outlined);
    }

    return Column(
      children: [
        if (lotsErr)
          _StockNotice(
            message: 'Purchase costs & buying dates couldn\'t load.',
            onRetry: () => ref.invalidate(stockLotsProvider(partId)),
          ),
        if (levelsErr)
          _StockNotice(
            message: 'Live availability couldn\'t load.',
            onRetry: () => ref.invalidate(stockLevelsProvider(partId)),
          ),
        for (final id in warehouseIds)
          _WarehouseCard(
            level: levelByWarehouse[id],
            lots: (lotsByWarehouse[id] ?? [])
              ..sort((a, b) => b.receivingDate.compareTo(a.receivingDate)),
          ),
      ],
    );
  }
}

class _WarehouseCard extends StatelessWidget {
  const _WarehouseCard({required this.level, required this.lots});
  final StockLevel? level;
  final List<StockLot> lots;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;

    final name = level?.warehouseName ??
        lots.firstOrNull?.warehouseName ??
        'Warehouse';
    final unit =
        level?.unitSymbol ?? level?.unitName ?? lots.firstOrNull?.unitCode ?? '';
    final available = level?.availableQuantity ??
        lots.fold<int>(0, (s, l) => s + l.quantityAvailable);
    final inStock = available > 0;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            color: inStock
                ? Colors.green.shade50
                : scheme.errorContainer.withAlpha(80),
            child: Row(
              children: [
                Icon(Icons.warehouse_outlined,
                    size: 18,
                    color: inStock ? Colors.green.shade700 : scheme.error),
                const SizedBox(width: 10),
                Expanded(
                  child: Text(name,
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w700)),
                ),
                if (level?.needsReorder == true) ...[
                  _Pill(
                      text: 'Reorder',
                      bg: scheme.errorContainer,
                      fg: scheme.onErrorContainer),
                  const SizedBox(width: 6),
                ],
                _Pill(
                  text: '$available $unit'.trim(),
                  bg: inStock ? Colors.green.shade100 : scheme.errorContainer,
                  fg: inStock
                      ? Colors.green.shade800
                      : scheme.onErrorContainer,
                  icon: inStock ? Icons.check_circle : Icons.remove_circle,
                ),
              ],
            ),
          ),
          if (level != null)
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Row(
                children: [
                  _StockMetric(
                      label: 'Available',
                      value: '$available $unit'.trim(),
                      color: inStock ? Colors.green.shade700 : scheme.error,
                      primary: true),
                  const SizedBox(width: 20),
                  _StockMetric(
                      label: 'On hand',
                      value: '${level!.quantity} $unit'.trim()),
                  const SizedBox(width: 20),
                  _StockMetric(
                      label: 'Reserved',
                      value: '${level!.reservedQuantity} $unit'.trim()),
                ],
              ),
            ),
          if (lots.isNotEmpty) ...[
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 12, 16, 4),
              child: Text('PURCHASE LOTS',
                  style: theme.textTheme.labelSmall?.copyWith(
                      color: scheme.onSurfaceVariant,
                      letterSpacing: 0.8,
                      fontWeight: FontWeight.w700)),
            ),
            Padding(
              padding: const EdgeInsets.fromLTRB(16, 0, 16, 12),
              child: Column(
                  children: lots.map((l) => _LotRow(lot: l)).toList()),
            ),
          ] else
            const SizedBox(height: 12),
        ],
      ),
    );
  }
}

class _StockMetric extends StatelessWidget {
  const _StockMetric(
      {required this.label, required this.value, this.color, this.primary = false});
  final String label;
  final String value;
  final Color? color;
  final bool primary;

  @override
  Widget build(BuildContext context) => Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: const TextStyle(fontSize: 10, color: Colors.grey)),
          const SizedBox(height: 2),
          Text(value,
              style: TextStyle(
                  fontSize: primary ? 16 : 14,
                  fontWeight: FontWeight.w700,
                  color: color ?? Theme.of(context).colorScheme.onSurface)),
        ],
      );
}

class _LotRow extends ConsumerWidget {
  const _LotRow({required this.lot});
  final StockLot lot;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActual = ref.watch(showActualPriceProvider);
    final unit = lot.unitCode ?? lot.unitName ?? '';
    final hasSupplier =
        lot.supplierName != null && lot.supplierName!.isNotEmpty;

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerHighest.withAlpha(100),
        borderRadius: BorderRadius.circular(12),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                    color: scheme.primaryContainer,
                    borderRadius: BorderRadius.circular(8)),
                child: Text('${lot.quantityAvailable} $unit'.trim(),
                    style: TextStyle(
                        fontWeight: FontWeight.w700,
                        fontSize: 12,
                        color: scheme.onPrimaryContainer)),
              ),
              const SizedBox(width: 8),
              Text('in stock', style: theme.textTheme.bodySmall),
              const Spacer(),
              Text(
                formatCostMasked(priceCode, showActual, lot.costPrice,
                    currency: lot.currency),
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 13,
                    color: scheme.primary),
              ),
              if (unit.isNotEmpty)
                Text(' / $unit', style: theme.textTheme.bodySmall),
              if (lot.isExpired) ...[
                const SizedBox(width: 8),
                _Pill(
                    text: 'Expired',
                    bg: scheme.errorContainer,
                    fg: scheme.onErrorContainer),
              ],
            ],
          ),
          const SizedBox(height: 8),
          Row(
            children: [
              Icon(Icons.local_shipping_outlined,
                  size: 14, color: scheme.onSurfaceVariant),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  hasSupplier ? lot.supplierName! : 'Unknown supplier',
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodySmall?.copyWith(
                    fontWeight: FontWeight.w600,
                    fontStyle: hasSupplier ? null : FontStyle.italic,
                    color: hasSupplier ? null : scheme.onSurfaceVariant,
                  ),
                ),
              ),
              Icon(Icons.event_outlined, size: 13, color: scheme.onSurfaceVariant),
              const SizedBox(width: 4),
              Text(formatDate(lot.receivingDate), style: theme.textTheme.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}

// ── Compatible vehicles ───────────────────────────────────────────────────────

class _CompatibilitySection extends ConsumerWidget {
  const _CompatibilitySection({required this.partId});
  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final async = ref.watch(compatibleVehiclesProvider(partId));
    return async.when(
      loading: () => const Padding(
          padding: EdgeInsets.symmetric(vertical: 16), child: LoadingView()),
      error: (e, _) => _StockNotice(
        message: e is AppException
            ? e.message
            : 'Vehicle compatibility couldn\'t load.',
        onRetry: () => ref.invalidate(compatibleVehiclesProvider(partId)),
      ),
      data: (items) {
        if (items.isEmpty) {
          return const EmptyView(
              message: 'No vehicle compatibility listed for this part.',
              icon: Icons.directions_car_outlined);
        }
        return Column(
            children: items.map((c) => _CompatibilityTile(item: c)).toList());
      },
    );
  }
}

class _CompatibilityTile extends StatelessWidget {
  const _CompatibilityTile({required this.item});
  final VehicleCompatibility item;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final ok = item.isCompatible;

    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      elevation: 0,
      color: ok ? Colors.green.shade50 : scheme.errorContainer.withAlpha(60),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(
            color: ok ? Colors.green.shade200 : scheme.error.withAlpha(60)),
      ),
      child: Padding(
        padding: const EdgeInsets.all(12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(ok ? Icons.check_circle : Icons.cancel,
                    size: 18,
                    color: ok ? Colors.green.shade700 : scheme.error),
                const SizedBox(width: 8),
                Expanded(
                    child: Text(item.title,
                        style: theme.textTheme.titleSmall
                            ?.copyWith(fontWeight: FontWeight.w700))),
                if (item.engineType != null && item.engineType!.isNotEmpty)
                  _Pill(
                      text: item.engineType!,
                      bg: scheme.surfaceContainerHighest,
                      fg: scheme.onSurfaceVariant),
              ],
            ),
            if (!ok) ...[
              const SizedBox(height: 4),
              Text('Not compatible',
                  style: theme.textTheme.bodySmall
                      ?.copyWith(color: scheme.error)),
            ],
            if (item.notes != null && item.notes!.isNotEmpty) ...[
              const SizedBox(height: 6),
              Text(item.notes!, style: theme.textTheme.bodySmall),
            ],
          ],
        ),
      ),
    );
  }
}

// ── Variant tile ──────────────────────────────────────────────────────────────

class _VariantTile extends StatelessWidget {
  const _VariantTile({required this.variant});
  final ProductVariant variant;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final price = variant.pricing?.sellingPrice;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      child: ListTile(
        contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
        leading: Container(
          width: 36,
          height: 36,
          decoration: BoxDecoration(
              color: scheme.primaryContainer,
              borderRadius: BorderRadius.circular(10)),
          alignment: Alignment.center,
          child: Icon(Icons.tune, size: 18, color: scheme.onPrimaryContainer),
        ),
        title: Text(variant.name,
            style: const TextStyle(fontWeight: FontWeight.w700)),
        subtitle: Text(
          ['Code ${variant.code}', if (variant.sku != null) 'SKU ${variant.sku}']
              .join('  ·  '),
          style: const TextStyle(fontSize: 11),
        ),
        trailing: price == null
            ? null
            : Text(
                formatCurrency(price, currency: variant.pricing?.currency),
                style: TextStyle(
                    fontWeight: FontWeight.w700,
                    fontSize: 14,
                    color: scheme.primary),
              ),
      ),
    );
  }
}

// ── Shared widgets ────────────────────────────────────────────────────────────

class _Pill extends StatelessWidget {
  const _Pill(
      {required this.text, required this.bg, required this.fg, this.icon});
  final String text;
  final Color bg;
  final Color fg;
  final IconData? icon;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
        decoration:
            BoxDecoration(color: bg, borderRadius: BorderRadius.circular(20)),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            if (icon != null) ...[
              Icon(icon, size: 12, color: fg),
              const SizedBox(width: 3),
            ],
            Text(text,
                style: TextStyle(
                    fontSize: 11, color: fg, fontWeight: FontWeight.w600)),
          ],
        ),
      );
}

class _StockNotice extends StatelessWidget {
  const _StockNotice({required this.message, required this.onRetry});
  final String message;
  final VoidCallback onRetry;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.fromLTRB(12, 8, 8, 8),
      decoration: BoxDecoration(
          color: scheme.tertiaryContainer.withAlpha(120),
          borderRadius: BorderRadius.circular(10)),
      child: Row(
        children: [
          Icon(Icons.warning_amber_rounded,
              size: 18, color: scheme.onTertiaryContainer),
          const SizedBox(width: 8),
          Expanded(
              child: Text(message,
                  style: TextStyle(
                      fontSize: 12, color: scheme.onTertiaryContainer))),
          TextButton(
            onPressed: onRetry,
            style: TextButton.styleFrom(
                visualDensity: VisualDensity.compact,
                padding: const EdgeInsets.symmetric(horizontal: 8)),
            child: const Text('Retry'),
          ),
        ],
      ),
    );
  }
}
