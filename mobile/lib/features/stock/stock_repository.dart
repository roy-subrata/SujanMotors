import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/stock.dart';

class StockRepository {
  StockRepository(this._dio);

  final Dio _dio;

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

  /// Moves stock from one warehouse to another atomically (POST /stock/transfer)
  /// — the real transfer, unlike a one-sided adjust.
  Future<void> transferStock({
    required String partId,
    String? variantId,
    required String fromWarehouseId,
    required String toWarehouseId,
    required int quantity,
    String notes = '',
  }) async {
    try {
      await _dio.post('/stock/transfer', data: {
        'partId': partId,
        'variantId': ?variantId,
        'fromWarehouseId': fromWarehouseId,
        'toWarehouseId': toWarehouseId,
        'quantity': quantity,
        'quantityInBaseUnit': quantity,
        if (notes.isNotEmpty) 'notes': notes,
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final stockRepositoryProvider = Provider<StockRepository>(
  (ref) => StockRepository(ref.read(dioProvider)),
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
