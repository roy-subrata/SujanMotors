import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/customer.dart';
import 'customers_repository.dart';

/// Immutable view-state for the paginated, searchable customer list.
class CustomerListState {
  const CustomerListState({
    this.items = const [],
    this.query = '',
    this.isLoading = false,
    this.isLoadingMore = false,
    this.hasMore = false,
    this.error,
  });

  final List<Customer> items;
  final String query;
  final bool isLoading;
  final bool isLoadingMore;
  final bool hasMore;
  final String? error;

  bool get isEmpty => items.isEmpty && !isLoading && error == null;

  CustomerListState copyWith({
    List<Customer>? items,
    String? query,
    bool? isLoading,
    bool? isLoadingMore,
    bool? hasMore,
    String? error,
    bool clearError = false,
  }) {
    return CustomerListState(
      items: items ?? this.items,
      query: query ?? this.query,
      isLoading: isLoading ?? this.isLoading,
      isLoadingMore: isLoadingMore ?? this.isLoadingMore,
      hasMore: hasMore ?? this.hasMore,
      error: clearError ? null : (error ?? this.error),
    );
  }
}

class CustomerListController extends Notifier<CustomerListState> {
  int _page = 1;

  @override
  CustomerListState build() => const CustomerListState();

  Future<void> search(String query) async {
    _page = 1;
    state = CustomerListState(query: query, isLoading: true);
    try {
      final res =
          await ref.read(customersRepositoryProvider).list(search: query, page: 1);
      state = CustomerListState(
        query: query,
        items: res.items,
        hasMore: res.hasMore,
      );
    } on AppException catch (e) {
      state = CustomerListState(query: query, error: e.message);
    }
  }

  Future<void> loadMore() async {
    if (state.isLoadingMore || !state.hasMore || state.isLoading) return;
    state = state.copyWith(isLoadingMore: true, clearError: true);
    try {
      final next = _page + 1;
      final res = await ref
          .read(customersRepositoryProvider)
          .list(search: state.query, page: next);
      _page = next;
      state = state.copyWith(
        items: [...state.items, ...res.items],
        hasMore: res.hasMore,
        isLoadingMore: false,
      );
    } on AppException catch (e) {
      state = state.copyWith(isLoadingMore: false, error: e.message);
    }
  }

  Future<void> refresh() => search(state.query);
}

final customerListControllerProvider =
    NotifierProvider<CustomerListController, CustomerListState>(
        CustomerListController.new);
