import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/product.dart';
import '../../shared/models/sale.dart';
import '../products/products_repository.dart';
import 'sales_repository.dart';

class QuickSaleState {
  const QuickSaleState({
    this.items = const [],
    this.isScanning = false,
    this.isLookingUp = false,
    this.isSubmitting = false,
    this.lookupError,
    this.submitError,
    this.result,
    // catalog
    this.catalogItems = const [],
    this.isCatalogLoading = false,
    this.catalogHasMore = false,
    this.catalogPage = 1,
    this.catalogSearch = '',
    this.selectedCategory,
    this.allCategories = const [],
  });

  // ── Cart ──────────────────────────────────────────────────────────────────────
  final List<QuickSaleItem> items;
  final bool isScanning;
  final bool isLookingUp;
  final bool isSubmitting;
  final String? lookupError;
  final String? submitError;
  final QuickSaleResult? result;

  // ── Catalog ───────────────────────────────────────────────────────────────────
  final List<Product> catalogItems;
  final bool isCatalogLoading;
  final bool catalogHasMore;
  final int catalogPage;
  final String catalogSearch;
  final String? selectedCategory;
  final List<String> allCategories;

  bool get isEmpty => items.isEmpty;
  double get total => items.fold(0.0, (s, i) => s + i.lineTotal);
  int get itemCount => items.fold(0, (s, i) => s + i.quantity);

  List<Product> get filteredCatalog {
    if (selectedCategory == null) return catalogItems;
    return catalogItems
        .where((p) => p.category?.name == selectedCategory)
        .toList();
  }

  static const _keep = Object();

  QuickSaleState copyWith({
    List<QuickSaleItem>? items,
    bool? isScanning,
    bool? isLookingUp,
    bool? isSubmitting,
    Object? lookupError = _keep,
    Object? submitError = _keep,
    Object? result = _keep,
    List<Product>? catalogItems,
    bool? isCatalogLoading,
    bool? catalogHasMore,
    int? catalogPage,
    String? catalogSearch,
    Object? selectedCategory = _keep,
    List<String>? allCategories,
  }) {
    return QuickSaleState(
      items: items ?? this.items,
      isScanning: isScanning ?? this.isScanning,
      isLookingUp: isLookingUp ?? this.isLookingUp,
      isSubmitting: isSubmitting ?? this.isSubmitting,
      lookupError:
          lookupError == _keep ? this.lookupError : lookupError as String?,
      submitError:
          submitError == _keep ? this.submitError : submitError as String?,
      result: result == _keep ? this.result : result as QuickSaleResult?,
      catalogItems: catalogItems ?? this.catalogItems,
      isCatalogLoading: isCatalogLoading ?? this.isCatalogLoading,
      catalogHasMore: catalogHasMore ?? this.catalogHasMore,
      catalogPage: catalogPage ?? this.catalogPage,
      catalogSearch: catalogSearch ?? this.catalogSearch,
      selectedCategory: selectedCategory == _keep
          ? this.selectedCategory
          : selectedCategory as String?,
      allCategories: allCategories ?? this.allCategories,
    );
  }
}

class QuickSaleController extends Notifier<QuickSaleState> {
  @override
  QuickSaleState build() => const QuickSaleState();

  void startScan() =>
      state = state.copyWith(isScanning: true, lookupError: null);

  void stopScan() =>
      state = state.copyWith(isScanning: false, lookupError: null);

  // ── Catalog ───────────────────────────────────────────────────────────────────

  Future<void> loadCatalog({bool reset = true}) async {
    if (!reset && !state.catalogHasMore) return;
    if (state.isCatalogLoading) return;

    final page = reset ? 1 : state.catalogPage + 1;
    final queryAtStart = state.catalogSearch;
    state = state.copyWith(isCatalogLoading: true);

    try {
      final res = await ref.read(productsRepositoryProvider).search(
            query:
                state.catalogSearch.isEmpty ? null : state.catalogSearch,
            page: page,
            pageSize: 30,
          );

      // Drop stale result if query changed while the call was in flight.
      if (state.catalogSearch != queryAtStart) return;

      final newItems =
          reset ? res.data : [...state.catalogItems, ...res.data];

      // Accumulate category names seen so far — never shrink the list.
      final seen = <String>{...state.allCategories};
      for (final p in res.data) {
        final cat = p.category?.name;
        if (cat != null && cat.isNotEmpty) seen.add(cat);
      }

      state = state.copyWith(
        catalogItems: newItems,
        isCatalogLoading: false,
        catalogHasMore: res.data.length >= 30,
        catalogPage: page,
        allCategories: (seen.toList()..sort()),
      );
    } on AppException {
      if (state.catalogSearch == queryAtStart) {
        state = state.copyWith(isCatalogLoading: false);
      }
    }
  }

  Future<void> searchCatalog(String query) async {
    state = state.copyWith(catalogSearch: query, selectedCategory: null);
    await loadCatalog(reset: true);
  }

  void loadMoreCatalog() {
    if (!state.catalogHasMore || state.isCatalogLoading) return;
    loadCatalog(reset: false);
  }

  void setCategory(String? name) {
    final next = name == state.selectedCategory ? null : name;
    state = state.copyWith(selectedCategory: next);
  }

  // ── Barcode scan ─────────────────────────────────────────────────────────────

  Future<void> lookupByCode(String code) async {
    state = state.copyWith(isLookingUp: true, lookupError: null);
    try {
      final product =
          await ref.read(productsRepositoryProvider).getByCode(code);

      if (product.stockLevel <= 0) {
        state = state.copyWith(
          isLookingUp: false,
          lookupError: '${product.name} is out of stock',
        );
        return;
      }

      final existing = state.items.indexWhere(
        (i) =>
            i.partId == product.productId && i.variantId == product.variantId,
      );
      final List<QuickSaleItem> newItems;
      if (existing >= 0) {
        final current = state.items[existing];
        if (current.quantity >= product.stockLevel) {
          state = state.copyWith(
            isLookingUp: false,
            lookupError:
                'Only ${product.stockLevel} ${product.name} in stock — already in cart',
          );
          return;
        }
        newItems = List<QuickSaleItem>.from(state.items);
        newItems[existing] = current.copyWith(quantity: current.quantity + 1);
      } else {
        newItems = [
          QuickSaleItem(
            partId: product.productId,
            variantId: product.variantId,
            name: product.name,
            unitPrice: product.sellingPrice,
            quantity: 1,
            availableStock: product.stockLevel,
          ),
          ...state.items,
        ];
      }
      state = state.copyWith(
        items: newItems,
        isLookingUp: false,
        isScanning: false,
      );
    } on AppException catch (e) {
      state = state.copyWith(isLookingUp: false, lookupError: e.message);
    }
  }

  // ── Cart operations ──────────────────────────────────────────────────────────

  /// Returns `false` (and leaves the cart unchanged) when the product is
  /// known to be out of stock, or adding another unit would exceed the
  /// known stock on hand. `totalStock == null` means stock data wasn't
  /// available, in which case the item is added without a cap.
  bool addFromSearch(Product product) {
    final stock = product.totalStock;
    if (stock != null && stock <= 0) return false;

    final price = product.pricing?.sellingPrice ?? 0.0;
    final existing = state.items.indexWhere(
      (i) => i.partId == product.id && i.variantId == null,
    );
    final List<QuickSaleItem> newItems;
    if (existing >= 0) {
      final current = state.items[existing];
      if (stock != null && current.quantity >= stock) return false;
      newItems = List<QuickSaleItem>.from(state.items);
      newItems[existing] = current.copyWith(quantity: current.quantity + 1);
    } else {
      newItems = [
        QuickSaleItem(
          partId: product.id,
          variantId: null,
          name: product.name,
          localName: product.localName,
          unitPrice: price,
          quantity: 1,
          availableStock: stock,
        ),
        ...state.items,
      ];
    }
    state = state.copyWith(items: newItems);
    return true;
  }

  /// Returns `false` (without changing state) when incrementing would
  /// exceed the item's known available stock.
  bool increment(String partId, String? variantId) {
    final idx = _findIndex(partId, variantId);
    if (idx < 0) return false;
    final current = state.items[idx];
    final cap = current.availableStock;
    if (cap != null && current.quantity >= cap) return false;
    final updated = List<QuickSaleItem>.from(state.items);
    updated[idx] = current.copyWith(quantity: current.quantity + 1);
    state = state.copyWith(items: updated);
    return true;
  }

  void decrement(String partId, String? variantId) {
    final idx = _findIndex(partId, variantId);
    if (idx < 0) return;
    if (state.items[idx].quantity <= 1) {
      remove(partId, variantId);
      return;
    }
    final updated = List<QuickSaleItem>.from(state.items);
    updated[idx] = updated[idx].copyWith(quantity: updated[idx].quantity - 1);
    state = state.copyWith(items: updated);
  }

  void remove(String partId, String? variantId) {
    state = state.copyWith(
      items: state.items
          .where((i) => !(i.partId == partId && i.variantId == variantId))
          .toList(),
    );
  }

  void updatePrice(String partId, String? variantId, double price) {
    final idx = _findIndex(partId, variantId);
    if (idx < 0 || price <= 0) return;
    final updated = List<QuickSaleItem>.from(state.items);
    updated[idx] = updated[idx].copyWith(unitPrice: price);
    state = state.copyWith(items: updated);
  }

  // ── Checkout ─────────────────────────────────────────────────────────────────

  Future<void> submit({
    required double grandTotal,
    required String paymentMethod, // CASH | DUE
    String customerName = 'Walk-in',
    String? customerId,
    String? customerPhone,
    String? vehicleId,
  }) async {
    if (state.isSubmitting || state.items.isEmpty) return;
    state = state.copyWith(isSubmitting: true, submitError: null);

    final paidAmount = paymentMethod == 'CASH' ? grandTotal : 0.0;
    final dueAmount = paymentMethod == 'DUE' ? grandTotal : 0.0;

    try {
      final result = await ref.read(salesRepositoryProvider).submitQuickSale(
            items: state.items,
            subtotal: state.total,
            grandTotal: grandTotal,
            paidAmount: paidAmount,
            dueAmount: dueAmount,
            paymentMethod: paymentMethod,
            customerName: customerName.isEmpty ? 'Walk-in' : customerName,
            customerId: customerId,
            customerPhone: customerPhone,
            vehicleId: vehicleId,
          );
      state = state.copyWith(isSubmitting: false, result: result);
    } on AppException catch (e) {
      state = state.copyWith(isSubmitting: false, submitError: e.message);
    }
  }

  void reset() {
    // Keep the catalog when resetting the cart/result so the product grid
    // doesn't disappear after a successful sale.
    state = state.copyWith(
      items: [],
      isScanning: false,
      isSubmitting: false,
      lookupError: null,
      submitError: null,
      result: null,
    );
  }

  int _findIndex(String partId, String? variantId) => state.items.indexWhere(
        (i) => i.partId == partId && i.variantId == variantId,
      );
}

final quickSaleControllerProvider =
    NotifierProvider<QuickSaleController, QuickSaleState>(
        QuickSaleController.new);
