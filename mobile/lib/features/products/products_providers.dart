import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/attribute_group.dart';
import '../../shared/models/product.dart';
import '../../shared/models/product_location.dart';
import '../../shared/models/product_media.dart';
import '../../shared/models/vehicle_compatibility.dart';
import 'categories_repository.dart';
import 'products_repository.dart';

/// Immutable view-state for the paginated product search list.
class ProductSearchState {
  const ProductSearchState({
    this.items = const [],
    this.query = '',
    this.categoryId,
    this.categoryName,
    this.lowStockOnly = false,
    this.attributeOptionIds = const [],
    this.attributeGroups = const [],
    this.isLoading = false,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.error,
  });

  final List<Product> items;
  final String query;

  /// Server-side category filter — null means "All categories".
  final String? categoryId;
  final String? categoryName;

  /// Server-side "at/below reorder point" filter (the red "Low stock" chip).
  final bool lowStockOnly;

  /// Selected `ProductAttributeOption` ids for the category-scoped
  /// attribute filter (AND across attributes, OR within one attribute).
  final List<String> attributeOptionIds;

  /// Filterable attribute groups for the current category — fetched once
  /// per category selection, cached here so opening the filter sheet
  /// doesn't refetch every time.
  final List<FilterableAttributeGroup> attributeGroups;

  final bool isLoading;
  final bool isLoadingMore;
  final bool hasMore;
  final String? error;

  bool get isEmpty => items.isEmpty && !isLoading && error == null;

  ProductSearchState copyWith({
    List<Product>? items,
    String? query,
    List<FilterableAttributeGroup>? attributeGroups,
    bool? isLoading,
    bool? isLoadingMore,
    bool? hasMore,
    String? error,
    bool clearError = false,
  }) {
    return ProductSearchState(
      items: items ?? this.items,
      query: query ?? this.query,
      categoryId: categoryId,
      categoryName: categoryName,
      lowStockOnly: lowStockOnly,
      attributeOptionIds: attributeOptionIds,
      attributeGroups: attributeGroups ?? this.attributeGroups,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      hasMore: hasMore ?? this.hasMore,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class ProductSearchController extends Notifier<ProductSearchState> {
  int _page = 1;

  // Bumped on every new search; in-flight search/loadMore calls from an older
  // generation are discarded when they resolve, so a slow "b" response can't
  // overwrite a faster "ba" response, and a pending loadMore for an old query
  // can't append its page onto a newer query's results.
  int _generation = 0;

  @override
  ProductSearchState build() => const ProductSearchState();

  Future<void> search(String query) => _run(
      query: query,
      categoryId: state.categoryId,
      categoryName: state.categoryName,
      lowStockOnly: state.lowStockOnly,
      attributeOptionIds: state.attributeOptionIds,
      attributeGroups: state.attributeGroups);

  /// Filters the current query by category server-side. Pass null to clear
  /// back to "All categories". The attribute filter set is category-
  /// dependent, so both the selections and the cached attribute groups
  /// reset here; a new category's groups are fetched afterwards.
  Future<void> selectCategory(String? categoryId, {String? categoryName}) async {
    await _run(
        query: state.query,
        categoryId: categoryId,
        categoryName: categoryName,
        lowStockOnly: state.lowStockOnly,
        attributeOptionIds: const [],
        attributeGroups: const []);
    if (categoryId == null) return;
    final gen = _generation;
    try {
      final groups = await ref
          .read(categoriesRepositoryProvider)
          .getAttributeGroups(categoryId);
      if (gen != _generation) return; // category changed again meanwhile
      state = state.copyWith(attributeGroups: groups);
    } on AppException {
      // Attribute groups are a filter affordance, not core data — a failed
      // fetch just means the "Attributes" tab stays hidden, no error banner.
    }
  }

  /// The "All" chip — clears the category, low-stock, and attribute filters.
  Future<void> showAll() => _run(
      query: state.query,
      categoryId: null,
      categoryName: null,
      lowStockOnly: false,
      attributeOptionIds: const [],
      attributeGroups: const []);

  /// Toggles the server-side low-stock filter (at/below reorder point).
  Future<void> toggleLowStock() => _run(
      query: state.query,
      categoryId: state.categoryId,
      categoryName: state.categoryName,
      lowStockOnly: !state.lowStockOnly,
      attributeOptionIds: state.attributeOptionIds,
      attributeGroups: state.attributeGroups);

  /// Applies the attribute filter chosen in the filter sheet.
  Future<void> setAttributeFilters(List<String> optionIds) => _run(
      query: state.query,
      categoryId: state.categoryId,
      categoryName: state.categoryName,
      lowStockOnly: state.lowStockOnly,
      attributeOptionIds: optionIds,
      attributeGroups: state.attributeGroups);

  Future<void> _run({
    required String query,
    required String? categoryId,
    required String? categoryName,
    required bool lowStockOnly,
    required List<String> attributeOptionIds,
    required List<FilterableAttributeGroup> attributeGroups,
  }) async {
    final gen = ++_generation;
    _page = 1;
    state = ProductSearchState(
      query: query,
      categoryId: categoryId,
      categoryName: categoryName,
      lowStockOnly: lowStockOnly,
      attributeOptionIds: attributeOptionIds,
      attributeGroups: attributeGroups,
      isLoading: true,
    );
    try {
      final res = await ref.read(productsRepositoryProvider).search(
            query: query,
            page: 1,
            categoryId: categoryId,
            lowStockOnly: lowStockOnly,
            attributeOptionIds: attributeOptionIds,
          );
      if (gen != _generation) return; // superseded by a newer search
      state = ProductSearchState(
        query: query,
        categoryId: categoryId,
        categoryName: categoryName,
        lowStockOnly: lowStockOnly,
        attributeOptionIds: attributeOptionIds,
        attributeGroups: attributeGroups,
        items: res.data,
        hasMore: res.pagination.hasNextPage,
      );
    } on AppException catch (e) {
      if (gen != _generation) return;
      state = ProductSearchState(
        query: query,
        categoryId: categoryId,
        categoryName: categoryName,
        lowStockOnly: lowStockOnly,
        attributeOptionIds: attributeOptionIds,
        attributeGroups: attributeGroups,
        error: e.message,
      );
    }
  }

  Future<void> loadMore() async {
    if (state.isLoadingMore || !state.hasMore || state.isLoading) return;
    final gen = _generation;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final next = _page + 1;
      final res = await ref.read(productsRepositoryProvider).search(
            query: state.query,
            page: next,
            categoryId: state.categoryId,
            lowStockOnly: state.lowStockOnly,
            attributeOptionIds: state.attributeOptionIds,
          );
      if (gen != _generation) return; // superseded by a newer search
      _page = next;
      state = state.copyWith(
        items: [...state.items, ...res.data],
        hasMore: res.pagination.hasNextPage,
        isLoadingMore: false,
      );
    } on AppException catch (e) {
      if (gen != _generation) return;
      state = state.copyWith(isLoadingMore: false, error: e.message);
    }
  }

  Future<void> refresh() => search(state.query);
}

final productSearchControllerProvider =
    NotifierProvider<ProductSearchController, ProductSearchState>(
        ProductSearchController.new);

/// Count for the "Low stock · N" chip — cheapest possible query (one row,
/// read totalCount). Refreshed whenever the products screen is rebuilt fresh.
final lowStockCountProvider = FutureProvider.autoDispose<int>((ref) async {
  final res = await ref
      .read(productsRepositoryProvider)
      .search(page: 1, pageSize: 1, lowStockOnly: true);
  return res.pagination.totalCount;
});

/// Single product detail by id.
final productDetailProvider =
    FutureProvider.family<Product, String>((ref, id) {
  return ref.read(productsRepositoryProvider).getById(id);
});

/// Vehicles a part is compatible with, keyed by productId.
final compatibleVehiclesProvider =
    FutureProvider.family<List<VehicleCompatibility>, String>((ref, id) {
  return ref.read(productsRepositoryProvider).compatibleVehicles(id);
});

/// Physical bin/shelf locations for a part, keyed by productId.
final productLocationsProvider =
    FutureProvider.family<List<ProductLocation>, String>((ref, id) {
  return ref.read(productsRepositoryProvider).getLocations(id);
});

/// Product images/videos in display order, keyed by productId.
final productMediaProvider =
    FutureProvider.family<List<ProductMedia>, String>((ref, id) {
  return ref.read(productsRepositoryProvider).getMedia(id);
});

/// Merged read-only attribute values for a product's "Attributes" display:
/// the product's own attribute values from [productDetailProvider], plus
/// deduplicated variant-level attribute values collected across its variants (GET
/// /products/{id}/variants → attributeValues). Product-level values come
/// first; a variant-level value is dropped if an attribute of the same name
/// is already present at the product level.
final productAttributeValuesProvider =
    FutureProvider.family<List<ProductAttributeValue>, String>((ref, id) async {
  final product = await ref.watch(productDetailProvider(id).future);
  final variantAttrs =
      await ref.read(productsRepositoryProvider).getVariantAttributes(id);

  final seen = <String>{};
  final merged = <ProductAttributeValue>[];
  for (final a in product.attributeValues) {
    if (a.displayValue.isNotEmpty && seen.add(a.attributeName)) {
      merged.add(a);
    }
  }
  for (final a in variantAttrs) {
    if (a.displayValue.isNotEmpty && seen.add(a.attributeName)) {
      merged.add(a);
    }
  }
  return merged;
});
