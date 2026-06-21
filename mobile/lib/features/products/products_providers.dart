import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/product.dart';
import '../../shared/models/vehicle_compatibility.dart';
import 'products_repository.dart';

/// Immutable view-state for the paginated product search list.
class ProductSearchState {
  const ProductSearchState({
    this.items = const [],
    this.query = '',
    this.isLoading = false,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.error,
  });

  final List<Product> items;
  final String query;
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
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      hasMore: hasMore ?? this.hasMore,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class ProductSearchController extends Notifier<ProductSearchState> {
  int _page = 1;

  @override
  ProductSearchState build() => const ProductSearchState();

  Future<void> search(String query) async {
    _page = 1;
    state = ProductSearchState(query: query, isLoading: true);
    try {
      final res =
          await ref.read(productsRepositoryProvider).search(query: query, page: 1);
      state = ProductSearchState(
        query: query,
        items: res.data,
        hasMore: res.pagination.hasNextPage,
      );
    } on AppException catch (e) {
      state = ProductSearchState(query: query, error: e.message);
    }
  }

  Future<void> loadMore() async {
    if (state.isLoadingMore || !state.hasMore || state.isLoading) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final next = _page + 1;
      final res = await ref
          .read(productsRepositoryProvider)
          .search(query: state.query, page: next);
      _page = next;
      state = state.copyWith(
        items: [...state.items, ...res.data],
        hasMore: res.pagination.hasNextPage,
        isLoadingMore: false,
      );
    } on AppException catch (e) {
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
