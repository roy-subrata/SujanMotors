import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../core/network/permission_guard.dart';
import '../../shared/models/stock.dart';

class StockRepository {
  StockRepository(this._dio, this._ref);

  final Dio _dio;
  final Ref _ref;

  /// All warehouse stock levels for a part (variant rows included).
  Future<List<StockLevel>> levelsForPart(String partId) async {
    try {
      final res = await _dio.get('/stock/levels/part/$partId');
      final list = res.data as List<dynamic>;
      return list
          .map((e) => StockLevel.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Purchased lots for a part across warehouses (unit cost + buying date).
  Future<List<StockLot>> lotsForPart(String partId) async {
    try {
      final res = await _dio.get('/StockLot/part/$partId');
      final list = res.data as List<dynamic>;
      return list
          .map((e) => StockLot.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Positive quantity = stock in; negative = removal/adjustment.
  Future<void> adjustStock({
    required String partId,
    String? variantId,
    required String warehouseId,
    required int quantity,
    required String reason,
    String reference = '',
    String notes = '',
  }) async {
    await requirePermission(_ref, 'inventory.adjust-stock');
    try {
      await _dio.post('/stock/adjust', data: {
        'partId': partId,
        'variantId': ?variantId,
        'warehouseId': warehouseId,
        'quantity': quantity,
        'quantityInBaseUnit': quantity,
        'reason': reason,
        'reference': reference,
        if (notes.isNotEmpty) 'notes': notes,
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final stockRepositoryProvider = Provider<StockRepository>(
  (ref) => StockRepository(ref.read(dioProvider), ref),
);

/// Stock levels for a part, keyed by partId.
final stockLevelsProvider =
    FutureProvider.family<List<StockLevel>, String>((ref, partId) {
  return ref.read(stockRepositoryProvider).levelsForPart(partId);
});

/// Purchased lots for a part, keyed by partId.
final stockLotsProvider =
    FutureProvider.family<List<StockLot>, String>((ref, partId) {
  return ref.read(stockRepositoryProvider).lotsForPart(partId);
});
