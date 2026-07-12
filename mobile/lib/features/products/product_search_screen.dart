import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';
import 'package:google_fonts/google_fonts.dart';

import '../../core/theme/app_theme.dart';
import '../../shared/format.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/design_system.dart';
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
          onSelected: (id, name) =>
              controller.selectCategory(id, categoryName: name),
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
      showCartBadge: true,
      cartCount: cartCount,
      onCartTap: () => context.push('/quick-sale'),
      actions: [
        if (priceCode != null && priceCode.isConfigured)
          IconButton(
            tooltip: showActualPrice ? 'Hide cost prices' : 'Reveal cost prices',
            icon: Icon(showActualPrice
                ? Icons.visibility_off_outlined
                : Icons.visibility_outlined),
            onPressed: () =>
                ref.read(showActualPriceProvider.notifier).toggle(),
          ),
      ],
      body: Column(
        children: [
          // ── Search bar ─────────────────────────────────────────────────────
          Padding(
            padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
            child: SearchInput(
              controller: _searchCtrl,
              hintText: 'Search name, SKU, brand...',
              onChanged: controller.search,
              onScan: () => context.push('/scan'),
            ),
          ),
          const SizedBox(height: 10),

          // ── Category chips ─────────────────────────────────────────────────
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

          const SizedBox(height: 8),
          Expanded(
            child: _buildBody(state, controller, priceCode, showActualPrice),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () => context.go('/quick-sale'),
        backgroundColor: AppColors.ink,
        foregroundColor: Colors.white,
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
        child: const Icon(Icons.add),
      ),
    );
  }

  Widget _buildBody(
    ProductSearchState state,
    ProductSearchController controller,
    PriceCode? priceCode,
    bool showActualPrice,
  ) {
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
        padding: const EdgeInsets.fromLTRB(16, 0, 16, 90),
        itemCount: state.items.length + (state.hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= state.items.length) {
            return const Padding(
              padding: EdgeInsets.symmetric(vertical: 16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _ProductCard(
            product: state.items[index],
            priceCode: priceCode,
            showActualPrice: showActualPrice,
          );
        },
      ),
    );
  }
}

// ── Category tab row ─────────────────────────────────────────────────────────

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
    return SizedBox(
      height: 36,
      child: ListView(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(horizontal: 16),
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
        margin: const EdgeInsets.only(right: 8),
        padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
        decoration: BoxDecoration(
          color: isSelected ? AppColors.ink : Theme.of(context).colorScheme.surface,
          borderRadius: BorderRadius.circular(99),
          border: Border.all(
            color: isSelected ? AppColors.ink : Theme.of(context).colorScheme.outline,
          ),
        ),
        child: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              label,
              style: GoogleFonts.instrumentSans(
                fontSize: 12,
                fontWeight: FontWeight.w600,
                color: isSelected ? Colors.white : AppColors.secondary,
              ),
            ),
            if (icon != null) ...[
              const SizedBox(width: 2),
              Icon(icon,
                  size: 14,
                  color: isSelected ? Colors.white : AppColors.secondary),
            ],
          ],
        ),
      ),
    );
  }
}

// ── Category picker sheet ────────────────────────────────────────────────────

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
    return Column(
      children: [
        Padding(
          padding: const EdgeInsets.fromLTRB(16, 14, 8, 8),
          child: Row(
            children: [
              Expanded(
                child: Text(
                  'All Categories',
                  style: GoogleFonts.instrumentSans(
                    fontSize: 16,
                    fontWeight: FontWeight.w700,
                  ),
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
            hintText: 'Search categories...',
            onChanged: (v) => setState(() => _query = v),
          ),
        ),
        const SizedBox(height: 8),
        Expanded(
          child: PagedListView<Category>(
            resetKey: _query,
            padding: const EdgeInsets.fromLTRB(8, 0, 8, 12),
            fetch: (page) async {
              final res =
                  await ref.read(categoriesRepositoryProvider).search(
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
              leading: const Icon(Icons.category_outlined,
                  color: AppColors.secondary),
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

// ── Product card ─────────────────────────────────────────────────────────────

class _ProductCard extends ConsumerWidget {
  const _ProductCard({
    required this.product,
    required this.priceCode,
    required this.showActualPrice,
  });

  final Product product;
  final PriceCode? priceCode;
  final bool showActualPrice;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final price = product.pricing?.sellingPrice;
    final stock = product.totalStock;
    final sku = product.sku;
    final brand = product.brand?.name;

    final subtitle = [
      if ((sku).isNotEmpty) sku,
      if (brand != null && brand.isNotEmpty) brand,
    ].join(' \u00b7 ');

    final inStock = stock != null && stock > 0;
    final stockLabel = stock != null
        ? (inStock ? '$stock in stock' : 'Out of stock')
        : null;

    return Padding(
      padding: const EdgeInsets.only(bottom: 8),
      child: Material(
        color: Theme.of(context).colorScheme.surface,
        borderRadius: BorderRadius.circular(13),
        child: InkWell(
          borderRadius: BorderRadius.circular(13),
          onTap: () => context.push('/product/${product.id}'),
          child: Container(
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(13),
              border: Border.all(color: Theme.of(context).colorScheme.outline),
            ),
            padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
            child: Row(
              children: [
                // Image placeholder
                Container(
                  width: 44,
                  height: 44,
                  decoration: BoxDecoration(
                    color: Theme.of(context).colorScheme.surface,
                    borderRadius: BorderRadius.circular(10),
                    border: Border.all(
                        color: Theme.of(context).colorScheme.outline.withAlpha(60)),
                  ),
                  child: const Icon(Icons.inventory_2_outlined,
                      size: 20, color: AppColors.disabled),
                ),
                const SizedBox(width: 12),

                // Name + subtitle
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        product.name,
                        maxLines: 2,
                        overflow: TextOverflow.ellipsis,
                        style: GoogleFonts.instrumentSans(
                          fontSize: 13.5,
                          fontWeight: FontWeight.w500,
                        ),
                      ),
                      if (subtitle.isNotEmpty) ...[
                        const SizedBox(height: 2),
                        Text(
                          subtitle,
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: GoogleFonts.instrumentSans(
                            fontSize: 11.5,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
                const SizedBox(width: 12),

                // Price + stock pill
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      price != null
                          ? formatCurrency(price,
                              currency: product.pricing?.currency)
                          : '\u2014',
                      style: GoogleFonts.instrumentSans(
                        fontSize: 13.5,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    if (stockLabel != null) ...[
                      const SizedBox(height: 4),
                      StatusPill(
                        label: inStock ? 'In stock' : 'Out of stock',
                      ),
                    ],
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
