import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../shared/format.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/meta_tag.dart';
import '../../shared/widgets/paged_list_view.dart';
import '../../shared/widgets/state_views.dart';
import '../pricing/price_code.dart';
import '../sales/quick_sale_providers.dart';
import 'categories_repository.dart';
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

  void _openCategoryPicker(ProductSearchController controller) {
    showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      useSafeArea: true,
      builder: (_) => FractionallySizedBox(
        heightFactor: 0.85,
        child: _CategoryPickerSheet(
          onSelected: (id, name) => controller.selectCategory(id, categoryName: name),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(productSearchControllerProvider);
    final controller = ref.read(productSearchControllerProvider.notifier);
    final cartCount = ref.watch(
      quickSaleControllerProvider.select((s) => s.itemCount),
    );
    final quickCategories = ref.watch(quickCategoriesProvider);
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActualPrice = ref.watch(showActualPriceProvider);

    return AppScaffold(
      title: 'Products',
      showBottomNav: true,
      showNotificationBell: true,
      actions: [
        // Cost-price reveal toggle — masked by default, same code as the
        // product detail screen and shares its state, so revealing here
        // reveals it there too.
        if (priceCode != null && priceCode.isConfigured)
          IconButton(
            tooltip: showActualPrice ? 'Hide cost prices' : 'Reveal cost prices',
            icon: Icon(showActualPrice
                ? Icons.visibility_off_outlined
                : Icons.visibility_outlined),
            onPressed: () => ref.read(showActualPriceProvider.notifier).toggle(),
          ),
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
            onChanged: controller.search,
            onScan: () => context.push('/scan'),
          ),

          // ── Category tabs (server-driven — full active category list) ──
          quickCategories.when(
            data: (cats) => cats.isEmpty
                ? const SizedBox.shrink()
                : _CategoryTabRow(
                    categories: cats,
                    selectedId: state.categoryId,
                    onSelect: (id, name) =>
                        controller.selectCategory(id, categoryName: name),
                    onMore: () => _openCategoryPicker(controller),
                  ),
            loading: () => const SizedBox(height: 44),
            error: (_, _) => const SizedBox.shrink(),
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

    if (state.items.isEmpty) {
      return const EmptyView(
          message: 'No products found.', icon: Icons.search_off);
    }

    return RefreshIndicator(
      onRefresh: controller.refresh,
      child: ListView.builder(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(12, 10, 12, 14),
        itemCount: state.items.length + (state.hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= state.items.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: 16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _ProductListTile(product: state.items[index]);
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
    required this.selectedId,
    required this.onSelect,
    required this.onMore,
  });

  final List<Category> categories;
  final String? selectedId;
  final void Function(String? id, String? name) onSelect;
  final VoidCallback onMore;

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
            isSelected: selectedId == null,
            onTap: () => onSelect(null, null),
          ),
          ...categories.map(
            (cat) => _Tab(
              label: cat.name,
              isSelected: selectedId == cat.id,
              onTap: () => onSelect(cat.id, cat.name),
            ),
          ),
          _Tab(
            label: 'More',
            icon: Icons.expand_more_rounded,
            isSelected: false,
            onTap: onMore,
          ),
        ],
      ),
    );
  }
}

class _Tab extends StatelessWidget {
  const _Tab({
    required this.label,
    required this.isSelected,
    required this.onTap,
    this.icon,
  });

  final String label;
  final bool isSelected;
  final VoidCallback onTap;
  final IconData? icon;

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: onTap,
      child: Container(
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          border: Border(
            bottom: BorderSide(
              color: isSelected ? Colors.white : Colors.transparent,
              width: 2.5,
            ),
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: TextStyle(
                fontSize: 13,
                fontWeight: isSelected ? FontWeight.w700 : FontWeight.w400,
                color: isSelected ? Colors.white : Colors.white70,
              ),
            ),
            if (icon != null) ...[
              const SizedBox(width: 2),
              Icon(icon, size: 16, color: Colors.white70),
            ],
          ],
        ),
      ),
    );
  }
}

// ── Category picker (full, searchable list) ─────────────────────────────────

class _CategoryPickerSheet extends ConsumerStatefulWidget {
  const _CategoryPickerSheet({required this.onSelected});

  final void Function(String id, String name) onSelected;

  @override
  ConsumerState<_CategoryPickerSheet> createState() =>
      _CategoryPickerSheetState();
}

class _CategoryPickerSheetState extends ConsumerState<_CategoryPickerSheet> {
  final _searchCtrl = TextEditingController();
  String _query = '';

  @override
  void dispose() {
    _searchCtrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 8, 8),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  'All Categories',
                  style: Theme.of(context)
                      .textTheme
                      .titleMedium
                      ?.copyWith(fontWeight: FontWeight.w700),
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
          child: TextField(
            controller: _searchCtrl,
            autofocus: false,
            onChanged: (v) => setState(() => _query = v),
            decoration: InputDecoration(
              hintText: 'Search categories...',
              prefixIcon: const Icon(Icons.search, size: 20),
              isDense: true,
              filled: true,
              fillColor: scheme.surfaceContainerHighest.withAlpha(120),
              contentPadding:
                  const EdgeInsets.symmetric(vertical: 12, horizontal: 12),
              border: OutlineInputBorder(
                borderRadius: BorderRadius.circular(10),
                borderSide: BorderSide.none,
              ),
            ),
          ),
        ),
        const SizedBox(height: 8),
        Expanded(
          child: PagedListView<Category>(
            resetKey: _query,
            padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
            fetch: (page) async {
              final res = await ref.read(categoriesRepositoryProvider).search(
                    query: _query,
                    page: page,
                    pageSize: 30,
                  );
              return PagedChunk<Category>(
                items: res.data,
                totalCount: res.pagination.totalCount,
                hasMore: res.pagination.hasNextPage,
              );
            },
            emptyBuilder: (context) => const EmptyView(
              message: 'No categories found.',
              icon: Icons.category_outlined,
            ),
            itemBuilder: (context, cat) => ListTile(
              leading: Icon(Icons.category_outlined, color: scheme.primary),
              title: Text(cat.name),
              onTap: () {
                widget.onSelected(cat.id, cat.name);
                Navigator.of(context).pop();
              },
            ),
          ),
        ),
      ],
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
    final costPrice = product.pricing?.costPrice;
    final stock = product.totalStock;
    final priceCode = ref.watch(priceCodeProvider).value;
    final showActualPrice = ref.watch(showActualPriceProvider);
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: scheme.outlineVariant),
      ),
      clipBehavior: Clip.antiAlias,
      child: Column(
        children: [
          // ── Content (tap → product detail) — same composition as the
          // dashboard's Total Revenue card: name block, then a hero figure
          // (price) paired with stock/SKU/Part mini-stats. ────────────────
          InkWell(
            onTap: () => context.push('/product/${product.id}'),
            child: Padding(
              padding: const EdgeInsets.fromLTRB(14, 12, 14, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  // ── Image + name + category/brand tags ────────────────
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      ClipRRect(
                        borderRadius: BorderRadius.circular(12),
                        child: Container(
                          width: 56,
                          height: 56,
                          color: _bgColor(),
                          alignment: Alignment.center,
                          child: Text(
                            initial,
                            style: TextStyle(
                              fontSize: 24,
                              fontWeight: FontWeight.w900,
                              color: _textColor(),
                            ),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
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
                                ),
                              ),
                            ],
                            if (product.category != null ||
                                product.brand != null) ...[
                              const SizedBox(height: 6),
                              Wrap(
                                spacing: 6,
                                runSpacing: 4,
                                children: [
                                  if (product.category != null)
                                    MetaTag.category(product.category!.name),
                                  if (product.brand != null)
                                    MetaTag.brand(product.brand!.name),
                                ],
                              ),
                            ],
                          ],
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),
                  Divider(height: 1, color: scheme.outlineVariant),
                  const SizedBox(height: 10),

                  // ── Price (hero figure) + stock, alongside it ─────────
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text('Price',
                          style: TextStyle(
                              fontSize: 10, color: scheme.onSurfaceVariant)),
                      const SizedBox(height: 1),
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            price != null
                                ? formatCurrency(price,
                                    currency: product.pricing?.currency)
                                : '—',
                            style: TextStyle(
                              fontSize: 20,
                              fontWeight: FontWeight.w800,
                              color: scheme.primary,
                              letterSpacing: -0.3,
                            ),
                          ),
                          if (stock != null) ...[
                            const SizedBox(width: 8),
                            Padding(
                              padding: const EdgeInsets.only(bottom: 3),
                              child: _StockPill(
                                  qty: stock, unit: product.unitName),
                            ),
                          ],
                        ],
                      ),
                    ],
                  ),
                  const SizedBox(height: 10),

                  // ── Cost / SKU / Part mini-stats ──────────────────────
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      if (costPrice != null)
                        _ListMiniStat(
                          label: 'Cost',
                          value: formatCostMasked(
                              priceCode, showActualPrice, costPrice,
                              currency: product.pricing?.currency),
                        ),
                      const Spacer(),
                      _ListMiniStat(label: 'SKU', value: product.sku),
                      const SizedBox(width: 16),
                      _ListMiniStat(label: 'Part', value: product.partNumber),
                    ],
                  ),
                ],
              ),
            ),
          ),

          // ── Add to Sale (icon only, right-aligned) ────────────────────
          Divider(height: 1, color: scheme.outlineVariant),
          Padding(
            padding: const EdgeInsets.fromLTRB(14, 8, 14, 8),
            child: Align(
              alignment: Alignment.centerRight,
              child: Material(
                color: const Color(0xFFF59E0B),
                shape: const CircleBorder(),
                child: InkWell(
                  customBorder: const CircleBorder(),
                  onTap: () {
                    final added = ref
                        .read(quickSaleControllerProvider.notifier)
                        .addFromSearch(product);
                    if (!added) {
                      ScaffoldMessenger.of(context).showSnackBar(
                        SnackBar(
                          content: Text('${product.name} is out of stock'),
                          backgroundColor: Colors.red.shade700,
                          duration: const Duration(seconds: 2),
                          behavior: SnackBarBehavior.floating,
                        ),
                      );
                      return;
                    }
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
                  child: const Padding(
                    padding: EdgeInsets.all(9),
                    child: Icon(Icons.add_shopping_cart_outlined,
                        size: 18, color: Colors.white),
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

// ── Stock status pill ─────────────────────────────────────────────────────────
// Bordered, translucent-fill pill — same treatment as the up/down trend
// indicator on the dashboard's Total Revenue card.

class _StockPill extends StatelessWidget {
  const _StockPill({required this.qty, this.unit});

  final int qty;
  final String? unit;

  @override
  Widget build(BuildContext context) {
    final inStock = qty > 0;
    final label = '$qty${unit != null ? ' $unit' : ''}';
    final color = inStock ? Colors.green.shade700 : Colors.red.shade600;

    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 4),
      decoration: BoxDecoration(
        color: color.withAlpha(20),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withAlpha(90)),
      ),
      child: Row(
        mainAxisSize: MainAxisSize.min,
        children: [
          Icon(
            inStock ? Icons.check_circle_outline : Icons.remove_circle_outline,
            size: 12,
            color: color,
          ),
          const SizedBox(width: 4),
          Text(
            label,
            style: TextStyle(
              fontSize: 11,
              fontWeight: FontWeight.w700,
              color: color,
            ),
          ),
        ],
      ),
    );
  }
}

// ── List mini-stat ────────────────────────────────────────────────────────────
// Small label-above-value pair — same pattern as the Cash/Credit readout on
// the dashboard's Total Revenue card, adapted for a light card background.

class _ListMiniStat extends StatelessWidget {
  const _ListMiniStat({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final scheme = Theme.of(context).colorScheme;
    return Column(
      crossAxisAlignment: CrossAxisAlignment.end,
      children: [
        Text(label,
            style: TextStyle(fontSize: 10, color: scheme.onSurfaceVariant)),
        const SizedBox(height: 1),
        Text(
          value,
          style: TextStyle(
            fontSize: 12,
            fontWeight: FontWeight.w700,
            color: scheme.onSurface,
          ),
        ),
      ],
    );
  }
}

