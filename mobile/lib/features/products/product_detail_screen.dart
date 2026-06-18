import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/theme/app_theme.dart';
import '../../features/stock/stock_repository.dart';
import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';
import '../../shared/widgets/state_views.dart';
import 'products_providers.dart';

class ProductDetailScreen extends ConsumerWidget {
  const ProductDetailScreen({super.key, required this.productId});

  final String productId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final productAsync = ref.watch(productDetailProvider(productId));

    return Scaffold(
      appBar: AppBar(
        flexibleSpace: const AppBarGradient(),
        title: const Text('Product'),
      ),
      body: RefreshIndicator(
        onRefresh: () async {
          ref.invalidate(productDetailProvider(productId));
          ref.invalidate(stockLevelsProvider(productId));
          ref.invalidate(stockLotsProvider(productId));
        },
        child: productAsync.when(
          loading: () => const LoadingView(),
          error: (e, _) => ListView(
            children: [
              const SizedBox(height: 120),
              ErrorView(
                message:
                    e is AppException ? e.message : 'Failed to load product.',
                onRetry: () =>
                    ref.invalidate(productDetailProvider(productId)),
              ),
            ],
          ),
          data: (product) => _ProductDetailBody(product: product),
        ),
      ),
    );
  }
}

class _ProductDetailBody extends StatelessWidget {
  const _ProductDetailBody({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        _ProductCard(product: product),
        const SizedBox(height: 24),
        Row(
          children: [
            Icon(Icons.warehouse_outlined,
                size: 20, color: theme.colorScheme.primary),
            const SizedBox(width: 8),
            Text('Stock by warehouse', style: theme.textTheme.titleMedium),
          ],
        ),
        const SizedBox(height: 8),
        _StockSection(partId: product.id),
        if (product.variants.length > 1) ...[
          const SizedBox(height: 24),
          Text('Variants', style: theme.textTheme.titleMedium),
          const SizedBox(height: 8),
          ...product.variants.map((v) => _VariantTile(variant: v)),
        ],
      ],
    );
  }
}

/// Top hero card: name, brand, identifying chips and the selling price.
class _ProductCard extends StatelessWidget {
  const _ProductCard({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final price = product.pricing?.sellingPrice;

    return Card(
      elevation: 0,
      color: scheme.surfaceContainerHighest.withValues(alpha: 0.4),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                CircleAvatar(
                  radius: 24,
                  backgroundColor: scheme.primaryContainer,
                  child: Text(
                    _initial(product.name),
                    style: theme.textTheme.titleLarge
                        ?.copyWith(color: scheme.onPrimaryContainer),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(product.name, style: theme.textTheme.titleLarge),
                      if (product.brand != null) ...[
                        const SizedBox(height: 2),
                        Text(
                          product.brand!.name,
                          style: theme.textTheme.bodyMedium
                              ?.copyWith(color: scheme.primary),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 12),
            Wrap(
              spacing: 8,
              runSpacing: 4,
              children: [
                _Chip(label: 'SKU ${product.sku}'),
                _Chip(label: 'Part ${product.partNumber}'),
                if (product.category != null)
                  _Chip(label: product.category!.name),
                if (!product.isActive)
                  const _Chip(label: 'Inactive', tone: _ChipTone.warning),
              ],
            ),
            if (price != null) ...[
              const SizedBox(height: 16),
              Row(
                crossAxisAlignment: CrossAxisAlignment.baseline,
                textBaseline: TextBaseline.alphabetic,
                children: [
                  Text('Selling price', style: theme.textTheme.bodySmall),
                  const SizedBox(width: 12),
                  Text(
                    formatCurrency(price, currency: product.pricing!.currency),
                    style: theme.textTheme.headlineSmall
                        ?.copyWith(color: scheme.primary),
                  ),
                ],
              ),
            ],
            if (product.description != null &&
                product.description!.isNotEmpty) ...[
              const SizedBox(height: 12),
              Text(product.description!, style: theme.textTheme.bodyMedium),
            ],
          ],
        ),
      ),
    );
  }

  String _initial(String name) =>
      name.trim().isEmpty ? '?' : name.trim().characters.first.toUpperCase();
}

/// Combines per-warehouse stock levels (headline availability) with purchased
/// lots (unit cost + buying date), grouped by warehouse.
class _StockSection extends ConsumerWidget {
  const _StockSection({required this.partId});

  final String partId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final levelsAsync = ref.watch(stockLevelsProvider(partId));
    final lotsAsync = ref.watch(stockLotsProvider(partId));

    // Wait until both sources have settled (data or error) to avoid flicker.
    if (levelsAsync.isLoading || lotsAsync.isLoading) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 24),
        child: LoadingView(),
      );
    }

    final levelsErr = levelsAsync.hasError;
    final lotsErr = lotsAsync.hasError;

    // Only a hard error when nothing usable loaded at all.
    if (levelsErr && lotsErr) {
      final error = levelsAsync.error;
      return ErrorView(
        message: error is AppException ? error.message : 'Failed to load stock.',
        onRetry: () {
          ref.invalidate(stockLevelsProvider(partId));
          ref.invalidate(stockLotsProvider(partId));
        },
      );
    }

    final levels = levelsAsync.value ?? const <StockLevel>[];
    final lots = lotsAsync.value ?? const <StockLot>[];

    // Build the set of warehouses from whichever source(s) loaded.
    final lotsByWarehouse = <String, List<StockLot>>{};
    for (final lot in lots) {
      lotsByWarehouse.putIfAbsent(lot.warehouseId, () => []).add(lot);
    }
    final levelByWarehouse = {for (final l in levels) l.warehouseId: l};
    final warehouseIds = <String>{
      ...levelByWarehouse.keys,
      ...lotsByWarehouse.keys,
    };

    if (warehouseIds.isEmpty && !levelsErr && !lotsErr) {
      return const EmptyView(
        message: 'No stock records for this part yet.',
        icon: Icons.warehouse_outlined,
      );
    }

    return Column(
      children: [
        // One source failed — show what we have and offer a targeted retry.
        if (lotsErr)
          _StockNotice(
            message: 'Purchase costs & buying dates couldn’t load.',
            onRetry: () => ref.invalidate(stockLotsProvider(partId)),
          ),
        if (levelsErr)
          _StockNotice(
            message: 'Live availability couldn’t load.',
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
    final unit = level?.unitSymbol ??
        level?.unitName ??
        lots.firstOrNull?.unitCode ??
        '';
    // Prefer the authoritative available figure; fall back to lot sum.
    final available = level?.availableQuantity ??
        lots.fold<int>(0, (sum, l) => sum + l.quantityAvailable);

    final inStock = available > 0;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      clipBehavior: Clip.antiAlias,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Header band: warehouse + available pill.
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            color: scheme.surfaceContainerHighest.withValues(alpha: 0.5),
            child: Row(
              children: [
                Icon(Icons.warehouse_outlined, size: 20, color: scheme.primary),
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
                  bg: inStock ? Colors.green.shade50 : scheme.errorContainer,
                  fg: inStock ? Colors.green.shade800 : scheme.onErrorContainer,
                  icon: inStock ? Icons.check_circle : Icons.remove_circle,
                ),
              ],
            ),
          ),
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 14),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                if (level != null)
                  Row(
                    children: [
                      _Metric(
                          label: 'Available',
                          value: '$available $unit'.trim(),
                          emphasize: true),
                      const SizedBox(width: 24),
                      _Metric(
                          label: 'On hand',
                          value: '${level!.quantity} $unit'.trim()),
                      const SizedBox(width: 24),
                      _Metric(
                          label: 'Reserved',
                          value: '${level!.reservedQuantity} $unit'.trim()),
                    ],
                  ),
                if (lots.isNotEmpty) ...[
                  if (level != null) const Divider(height: 24),
                  Text('PURCHASE LOTS',
                      style: theme.textTheme.labelSmall?.copyWith(
                          color: scheme.onSurfaceVariant,
                          letterSpacing: 0.6,
                          fontWeight: FontWeight.w600)),
                  const SizedBox(height: 8),
                  ...lots.map((l) => _LotRow(lot: l)),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _LotRow extends StatelessWidget {
  const _LotRow({required this.lot});

  final StockLot lot;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final unit = lot.unitCode ?? lot.unitName ?? '';
    final hasSupplier =
        lot.supplierName != null && lot.supplierName!.isNotEmpty;

    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: scheme.surfaceContainerHighest.withValues(alpha: 0.35),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Line 1: qty available + unit cost + expiry flag.
          Row(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: scheme.primaryContainer,
                  borderRadius: BorderRadius.circular(8),
                ),
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
                formatCurrency(lot.costPrice, currency: lot.currency),
                style: theme.textTheme.titleSmall?.copyWith(
                    fontWeight: FontWeight.w700, color: scheme.primary),
              ),
              Text(unit.isEmpty ? '' : ' / $unit',
                  style: theme.textTheme.bodySmall),
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
          // Line 2: supplier (prominent) + buying date.
          Row(
            children: [
              Icon(Icons.local_shipping_outlined,
                  size: 15, color: scheme.onSurfaceVariant),
              const SizedBox(width: 6),
              Expanded(
                child: Text(
                  hasSupplier ? lot.supplierName! : 'Unknown supplier',
                  overflow: TextOverflow.ellipsis,
                  style: theme.textTheme.bodyMedium?.copyWith(
                      fontWeight: FontWeight.w600,
                      fontStyle: hasSupplier ? null : FontStyle.italic,
                      color: hasSupplier ? null : scheme.onSurfaceVariant),
                ),
              ),
              const SizedBox(width: 8),
              Icon(Icons.event_outlined,
                  size: 14, color: scheme.onSurfaceVariant),
              const SizedBox(width: 4),
              Text(formatDate(lot.receivingDate),
                  style: theme.textTheme.bodySmall),
            ],
          ),
        ],
      ),
    );
  }
}

/// Inline amber notice shown when one stock source fails but the other loaded.
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
        color: scheme.tertiaryContainer.withValues(alpha: 0.5),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        children: [
          Icon(Icons.warning_amber_rounded,
              size: 18, color: scheme.onTertiaryContainer),
          const SizedBox(width: 8),
          Expanded(
            child: Text(message,
                style: TextStyle(
                    fontSize: 12, color: scheme.onTertiaryContainer)),
          ),
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

/// Small rounded status pill used across the stock cards.
class _Pill extends StatelessWidget {
  const _Pill(
      {required this.text, required this.bg, required this.fg, this.icon});

  final String text;
  final Color bg;
  final Color fg;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 3),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(20),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          if (icon != null) ...[
            Icon(icon, size: 13, color: fg),
            const SizedBox(width: 4),
          ],
          Text(text,
              style: TextStyle(
                  fontSize: 11, color: fg, fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric(
      {required this.label, required this.value, this.emphasize = false});

  final String label;
  final String value;
  final bool emphasize;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: theme.textTheme.bodySmall),
        const SizedBox(height: 2),
        Text(
          value,
          style: theme.textTheme.titleMedium?.copyWith(
            fontWeight: FontWeight.bold,
            color: emphasize ? theme.colorScheme.primary : null,
          ),
        ),
      ],
    );
  }
}

class _VariantTile extends StatelessWidget {
  const _VariantTile({required this.variant});

  final ProductVariant variant;

  @override
  Widget build(BuildContext context) {
    final price = variant.pricing?.sellingPrice;
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      child: ListTile(
        title: Text(variant.name),
        subtitle: Text([
          'Code ${variant.code}',
          if (variant.sku != null) 'SKU ${variant.sku}',
        ].join('  •  ')),
        trailing: price == null
            ? null
            : Text(formatCurrency(price, currency: variant.pricing?.currency),
                style: const TextStyle(fontWeight: FontWeight.w600)),
      ),
    );
  }
}

enum _ChipTone { normal, warning }

class _Chip extends StatelessWidget {
  const _Chip({required this.label, this.tone = _ChipTone.normal});

  final String label;
  final _ChipTone tone;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    final bg = tone == _ChipTone.warning
        ? scheme.errorContainer
        : scheme.surfaceContainerHighest;
    final fg = tone == _ChipTone.warning
        ? scheme.onErrorContainer
        : scheme.onSurfaceVariant;
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: bg,
        borderRadius: BorderRadius.circular(16),
      ),
      child: Text(label, style: TextStyle(fontSize: 12, color: fg)),
    );
  }
}
