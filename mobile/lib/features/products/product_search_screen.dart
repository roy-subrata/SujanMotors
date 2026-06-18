import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../shared/format.dart';
import '../../shared/models/product.dart';
import '../../shared/widgets/app_scaffold.dart';
import '../../shared/widgets/state_views.dart';
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
    _searchCtrl.addListener(() => setState(() {}));
    // Load an initial page (empty query returns the catalogue).
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

  @override
  Widget build(BuildContext context) {
    final state = ref.watch(productSearchControllerProvider);
    final controller = ref.read(productSearchControllerProvider.notifier);

    return AppScaffold(
      title: 'Products',
      actions: [
        IconButton(
          tooltip: 'Scan barcode',
          icon: const Icon(Icons.qr_code_scanner),
          onPressed: () => context.push('/scan'),
        ),
      ],
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(12),
            child: TextField(
              controller: _searchCtrl,
              textInputAction: TextInputAction.search,
              onSubmitted: controller.search,
              decoration: InputDecoration(
                hintText: 'Search name, SKU or part number',
                prefixIcon: const Icon(Icons.search),
                border: const OutlineInputBorder(),
                suffixIcon: _searchCtrl.text.isEmpty
                    ? null
                    : IconButton(
                        icon: const Icon(Icons.clear),
                        onPressed: () {
                          _searchCtrl.clear();
                          controller.search('');
                        },
                      ),
              ),
            ),
          ),
          Expanded(child: _buildBody(state, controller)),
        ],
      ),
    );
  }

  Widget _buildBody(ProductSearchState state, ProductSearchController controller) {
    if (state.isLoading) return const LoadingView();
    if (state.error != null) {
      return ErrorView(
        message: state.error!,
        onRetry: () => controller.search(state.query),
      );
    }
    if (state.isEmpty) {
      return const EmptyView(
          message: 'No products found.', icon: Icons.search_off);
    }

    return RefreshIndicator(
      onRefresh: controller.refresh,
      child: ListView.builder(
        controller: _scrollCtrl,
        padding: const EdgeInsets.fromLTRB(12, 4, 12, 12),
        itemCount: state.items.length + (state.hasMore ? 1 : 0),
        itemBuilder: (context, index) {
          if (index >= state.items.length) {
            return const Padding(
              padding: EdgeInsets.all(16),
              child: Center(child: CircularProgressIndicator()),
            );
          }
          return _ProductTile(product: state.items[index]);
        },
      ),
    );
  }
}

class _ProductTile extends StatelessWidget {
  const _ProductTile({required this.product});

  final Product product;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final scheme = theme.colorScheme;
    final price = product.pricing?.sellingPrice;
    final initial = product.name.trim().isEmpty
        ? '?'
        : product.name.trim().characters.first.toUpperCase();

    return Card(
      elevation: 0,
      margin: const EdgeInsets.only(bottom: 8),
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(12),
        side: BorderSide(color: scheme.outlineVariant.withValues(alpha: 0.5)),
      ),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: () => context.push('/product/${product.id}'),
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              CircleAvatar(
                radius: 22,
                backgroundColor: scheme.primaryContainer,
                child: Text(initial,
                    style: TextStyle(
                        color: scheme.onPrimaryContainer,
                        fontWeight: FontWeight.w700)),
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
                      style: theme.textTheme.titleSmall
                          ?.copyWith(fontWeight: FontWeight.w700),
                    ),
                    const SizedBox(height: 6),
                    Wrap(
                      spacing: 6,
                      runSpacing: 4,
                      crossAxisAlignment: WrapCrossAlignment.center,
                      children: [
                        if (product.brand != null)
                          _Tag(
                            text: product.brand!.name,
                            bg: scheme.primaryContainer.withValues(alpha: 0.6),
                            fg: scheme.onPrimaryContainer,
                          ),
                        _Tag(
                          text: 'SKU ${product.sku}',
                          bg: scheme.surfaceContainerHighest,
                          fg: scheme.onSurfaceVariant,
                        ),
                      ],
                    ),
                  ],
                ),
              ),
              const SizedBox(width: 8),
              Column(
                crossAxisAlignment: CrossAxisAlignment.end,
                children: [
                  if (price != null)
                    Text(
                      formatCurrency(price, currency: product.pricing?.currency),
                      style: theme.textTheme.titleMedium?.copyWith(
                          fontWeight: FontWeight.w800, color: scheme.primary),
                    ),
                  const SizedBox(height: 4),
                  Icon(Icons.chevron_right, size: 20, color: scheme.outline),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}

/// Compact rounded tag used for brand / SKU on the product card.
class _Tag extends StatelessWidget {
  const _Tag({required this.text, required this.bg, required this.fg});

  final String text;
  final Color bg;
  final Color fg;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
      decoration:
          BoxDecoration(color: bg, borderRadius: BorderRadius.circular(6)),
      child: Text(text,
          style: TextStyle(fontSize: 11, color: fg, fontWeight: FontWeight.w600)),
    );
  }
}
