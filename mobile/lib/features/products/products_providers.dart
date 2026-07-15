import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/product.dart';
import '../../shared/models/product_location.dart';
import '../../shared/models/product_media.dart';
import '../../shared/models/vehicle_compatibility.dart';
import 'products_repository.dart';

/// Immutable view-state for the paginated product search list.
class ProductSearchState {
  const ProductSearchState({
    this.items = const [],
    this.query = '',
    this.categoryId,
    this.categoryName,
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
  final bool isLoading;
  final bool isLoadingMore;
  final bool hasMore;
  final String? error;

  bool get isEmpty => items.isEmpty && !isLoading && error == null;

  ProductSearchState copyWith({
    List<Product>? items,
    String? query,
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

  Future<void> search(String query) =>
      _run(query: query, categoryId: state.categoryId, categoryName: state.categoryName);

  /// Filters the current query by category server-side. Pass null to clear
  /// back to "All categories".
  Future<void> selectCategory(String? categoryId, {String? categoryName}) =>
      _run(query: state.query, categoryId: categoryId, categoryName: categoryName);

  Future<void> _run({
    required String query,
    required String? categoryId,
    required String? categoryName,
  }) async {
    final gen = ++_generation;
    _page = 1;
    state = ProductSearchState(
      query: query,
      categoryId: categoryId,
      categoryName: categoryName,
      isLoading: true,
    );
    try {
      final res = await ref.read(productsRepositoryProvider).search(
            query: query,
            page: 1,
            categoryId: categoryId,
          );
      if (gen != _generation) return; // superseded by a newer search
      state = ProductSearchState(
        query: query,
        categoryId: categoryId,
        categoryName: categoryName,
        items: res.data,
        hasMore: res.pagination.hasNextPage,
      );
    } on AppException catch (e) {
      if (gen != _generation) return;
      state = ProductSearchState(
        query: query,
        categoryId: categoryId,
        categoryName: categoryName,
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

/// Deduplicated attribute values (specs) for a part, keyed by productId.
/// Sourced from GET /products/{id}/variants → attributeValues.
final productVariantAttributesProvider =
    FutureProvider.family<List<ProductAttributeValue>, String>((ref, id) {
  return ref.read(productsRepositoryProvider).getVariantAttributes(id);
});
