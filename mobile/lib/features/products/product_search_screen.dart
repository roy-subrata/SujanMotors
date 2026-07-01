import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/state_views.dart';
import '../sales/quick_sale_providers.dart';
import 'products_providers.dart';

class ProductSearchScreen extends ConsumerStatefulWidget {
  const ProductSearchScreen({super.key});

  @override
  ConsumerState<ProductSearchScreen> createState() =>
      _ProductSearchScreenState();
}

class _ProductSearchScreenState extends ConsumerState<ProductSearchScreen> {
  final _searchCtrl = TextEditingController();
  final _scrollCtrl = ScrollController();

  String? _selectedCategory;
  List<String> _categories = [];

  @override
  void initState() {
    super.initState();
    _scrollCtrl.addListener(_onScroll);
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ref.read(productSearchControllerProvider.notifier).search('');
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
        _scrollCtrl.position.maxScrollExtent - 300) {
      ref.read(productSearchControllerProvider.notifier).loadMore();
    }
  }

  List<String> _extractCategories(List<Product> products) {
    final seen = <String>{};
    for (final p in products) {
      final cat = p.category?.name;
      if (cat != null && cat.isNotEmpty) seen.add(cat);
    }
    return seen.toList()..sort();
  }

  List<Product> _visibleProducts(List<Product> items) {
    if (_selectedCategory == null) return items;
    return items.where((p) => p.category?.name == _selectedCategory).toList();
  }

  @override
  Widget build(BuildContext context, ) {
    final state = ref.watch(productSearchControllerProvider);
    final controller = ref.read(productSearchControllerProvider.notifier);
    final cartCount = ref.watch(
      quickSaleControllerProvider.select((s) => s.itemCount),
    );

    if (state.items.isNotEmpty) {
      final cats = _extractCategories(state.items);
      if (cats.length != _categories.length) {
        WidgetsBinding.instance.addPostFrameCallback((_) {
          if (mounted) setState(() => _categories = cats);
        });
      }
    }

    return AppScaffold(
      title: 'Products',
      showBottomNav: true,
      showNotificationBell: true,
      actions: [
        // Cart icon with live badge
        Badge(
          isLabelVisible: cartCount > 0,
          label: Text('$cartCount'),
          child: IconButton(
            icon: const Icon(Icons.shopping_cart_outlined),
            tooltip: 'Quick Sale cart',
            onPressed: () => context.push('/quick-sale'),
          ),
        ),
      ],
      body: Column(
        children: [
          // ── Persistent search bar ────────────────────────────────────
          _SearchBar(
            controller: _searchCtrl,
            onChanged: (q) {
              controller.search(q);
              setState(() => _selectedCategory = null);
            },
            onScan: () => context.push('/scan'),
          ),

          // ── Category tabs ────────────────────────────────────────────
          if (_categories.isNotEmpty)
            _CategoryTabRow(
              categories: _categories,
              selected: _selectedCategory,
              onSelect: (cat) => setState(() => _selectedCategory = cat),
            ),

          // ── List ─────────────────────────────────────────────────────
          Expanded(child: _buildBody(state, controller)),
        ],
      ),
    );
  }

  Widget _buildBody(
      ProductSearchState state, ProductSearchController controller) {
    if (state.isLoading) return const LoadingView();
    if (state.error != null) {
      return ErrorView(
        message: state.error!,
        onRetry: () => controller.search(state.query),
      );
    }

    final products = _visibleProducts(state.items);

    if (products.isEmpty) {
      return const EmptyView(
          message: 'No products found.', icon: Icons.search_off);
    }

    return RefreshIndicator(
      onRefresh: controller.refresh,
      child: ListView.builder(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 14),
        itemCount: products.length +
            (state.hasMore && _selectedCategory == null ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= products.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: 16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _ProductListTile(product: products[index]);
        },
      ),
    );
  }
}

// ── Persistent search bar ─────────────────────────────────────────────────────

class _SearchBar extends StatelessWidget {
  const _SearchBar({
    required this.controller,
    required this.onChanged,
    required this.onScan,
  });

  final TextEditingController controller;
  final ValueChanged<String> onChanged;
  final VoidCallback onScan;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;

    return Container(
      color: const Color(0xFF4F46E5),
      padding: const EdgeInsets.fromLTRB(12, 8, 12, 10),
      child: TextField(
        controller: controller,
        onChanged: onChanged,
        textInputAction: TextInputAction.search,
        style: const TextStyle(fontSize: 15),
        decoration: InputDecoration(
          hintText: 'Search products...',
          prefixIcon: const Icon(Icons.search, size: 20),
          suffixIcon: IconButton(
            icon: const Icon(Icons.qr_code_scanner, size: 22),
            tooltip: 'Scan barcode',
            onPressed: onScan,
          ),
          filled: true,
          fillColor: scheme.surface,
          contentPadding:
              const EdgeInsets.symmetric(horizontal: 16, vertical: 0),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide.none,
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide.none,
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(12),
            borderSide: BorderSide(color: scheme.primary, width: 2),
          ),
        ),
      ),
    );
  }
}

// ── Category tab row ──────────────────────────────────────────────────────────

class _CategoryTabRow extends StatelessWidget {
  const _CategoryTabRow({
    required this.categories,
    required this.selected,
    required this.onSelect,
  });

  final List<String> categories;
  final String? selected;
  final void Function(String?) onSelect;

  @override
  Widget build(BuildContext context) {
    return Container(
      height: 44,
      color: const Color(0xFF4F46E5),
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 8),
        children: [
          _Tab(
            label: 'All',
            isSelected: selected == null,
            onTap: () => onSelect(null),
          ),
          ...categories.map(
            (cat) => _Tab(
              label: cat,
              isSelected: selected == cat,
              onTap: () => onSelect(cat),
            ),
          ),
        ],
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab(
      {required this.label, required this.isSelected, required this.onTap});

  final String label;
  final bool isSelected;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: isSelected ? Colors.white : Colors.transparent,
              width: 2.5,
            ),
          ),
        ),
        child: Text(
          label,
          style: TextStyle(
            fontSize: 13,
            fontWeight: isSelected ? FontWeight.w700 : FontWeight.w400,
            color: isSelected ? Colors.white : Colors.white70,
          ),
        ),
      ),
    );
  }
}

// ── Product list tile ─────────────────────────────────────────────────────────

class _ProductListTile extends ConsumerWidget {
  const _ProductListTile({required this.product});

  final Product product;

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
    final price = product.pricing?.sellingPrice;
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      elevation: 2,
      shadowColor: Colors.black12,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          // ── Content row ──────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(12, 12, 12, 10),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Image block
                ClipRRect(
                  borderRadius: BorderRadius.circular(12),
                  child: Container(
                    width: 72,
                    height: 72,
                    color: _bgColor(),
                    alignment: Alignment.center,
                    child: Text(
                      initial,
                      style: TextStyle(
                        fontSize: 30,
                        fontWeight: FontWeight.w900,
                        color: _textColor(),
                      ),
                    ),
                  ),
                ),
                const SizedBox(width: 12),

                // Info
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        product.name,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: theme.textTheme.bodyMedium
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
                            fontSize: 13,
                            fontStyle: FontStyle.italic,
                          ),
                        ),
                      ],
                      const SizedBox(height: 6),
                      Wrap(
                        spacing: 6,
                        runSpacing: 4,
                        children: [
                          if (product.brand != null)
                            _MiniChip(
                              label: product.brand!.name,
                              icon: Icons.verified_outlined,
                              color: const Color(0xFFD97706),
                              bg: Colors.amber.shade50,
                            ),
                          if (product.category != null)
                            _MiniChip(
                              label: product.category!.name,
                              icon: Icons.category_outlined,
                              color: scheme.primary,
                              bg: scheme.primaryContainer.withAlpha(80),
                            ),
                        ],
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'SKU ${product.sku}  ·  Part ${product.partNumber}',
                        style: theme.textTheme.bodySmall?.copyWith(
                          color: scheme.onSurfaceVariant,
                          fontSize: 11,
                        ),
                      ),
                      if (price != null) ...[
                        const SizedBox(height: 6),
                        Text(
                          formatCurrency(price,
                              currency: product.pricing?.currency),
                          style: TextStyle(
                            fontSize: 15,
                            fontWeight: FontWeight.w800,
                            color: scheme.primary,
                          ),
                        ),
                      ],
                      if (product.totalStock != null) ...[
                        const SizedBox(height: 5),
                        _StockBadge(
                          qty: product.totalStock!,
                          unit: product.unitName,
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),

          // ── Action button row ────────────────────────────────────────
          const Divider(height: 1, color: Color(0xFFEEEEEE)),
          IntrinsicHeight(
            child: Row(
              children: [
                // Add to Sale
                Expanded(
                  child: TextButton.icon(
                    icon: const Icon(Icons.add_shopping_cart_outlined, size: 18),
                    label: const Text('Add to Sale'),
                    style: TextButton.styleFrom(
                      foregroundColor: const Color(0xFFD97706),
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: const RoundedRectangleBorder(),
                    ),
                    onPressed: () {
                      ref
                          .read(quickSaleControllerProvider.notifier)
                          .addFromSearch(product);
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('${product.name} added to sale'),
                          duration: const Duration(seconds: 2),
                          behavior: SnackBarBehavior.floating,
                          action: SnackBarAction(
                            label: 'Go to Sale',
                            onPressed: () => context.push('/quick-sale'),
                          ),
                        ),
                      );
                    },
                  ),
                ),

                const VerticalDivider(width: 1, color: Color(0xFFEEEEEE)),

                // View Details
                Expanded(
                  child: TextButton.icon(
                    icon: Icon(Icons.open_in_new_outlined,
                        size: 18, color: scheme.primary),
                    label: Text('Details',
                        style: TextStyle(color: scheme.primary)),
                    style: TextButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 12),
                      shape: const RoundedRectangleBorder(),
                    ),
                    onPressed: () => context.push('/product/${product.id}'),
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

// ── Mini chip ─────────────────────────────────────────────────────────────────

// ── Stock badge ───────────────────────────────────────────────────────────────

class _StockBadge extends StatelessWidget {
  const _StockBadge({required this.qty, this.unit});

  final int qty;
  final String? unit;

  @override
  Widget build(BuildContext context) {
    final inStock = qty > 0;
    final label = inStock
        ? '$qty${unit != null ? ' $unit' : ''}'
        : 'Out of stock';
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration: BoxDecoration(
        color: inStock ? Colors.green.shade50 : Colors.red.shade50,
        borderRadius: BorderRadius.circular(10),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            inStock ? Icons.inventory_2_outlined : Icons.remove_circle_outline,
            size: 12,
            color: inStock ? Colors.green.shade700 : Colors.red.shade700,
          ),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w700,
              color: inStock ? Colors.green.shade700 : Colors.red.shade700,
            ),
          ),
        ],
      ),
    );
  }
}

// ── Mini chip ─────────────────────────────────────────────────────────────────

class _MiniChip extends StatelessWidget {
  const _MiniChip({
    required this.label,
    required this.icon,
    required this.color,
    required this.bg,
  });

  final String label;
  final IconData icon;
  final Color color;
  final Color bg;

  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.symmetric(horizontal: 7, vertical: 3),
        decoration:
            BoxDecoration(color: bg, borderRadius: BorderRadius.circular(8)),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 11, color: color),
            const SizedBox(width: 4),
            Text(label,
                style: TextStyle(
                    fontSize: 11,
                    color: color,
                    fontWeight: FontWeight.w600)),
          ],
        ),
      );
}

