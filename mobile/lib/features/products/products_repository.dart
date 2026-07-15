import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/models/product_location.dart';
import '../../shared/models/product_media.dart';
import '../../shared/models/stock.dart';
import '../../shared/models/vehicle_compatibility.dart';

class ProductsRepository {
  ProductsRepository(this._dio);

  final Dio _dio;

  Future<PagedResponse<Product>> search({
    String? query,
    int page = 1,
    int pageSize = 20,
    bool? isActive,
    String? categoryId,
  }) async {
    try {
      final res = await _dio.get('/products', queryParameters: {
        if (query != null && query.isNotEmpty) 'search': query,
        'page': page,
        'pageSize': pageSize,
        'isActive': ?isActive,
        'categoryId': ?categoryId,
      });
      return PagedResponse.fromJson(
        res.data as Map<String, dynamic>,
        Product.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<Product> getById(String id) async {
    try {
      final res = await _dio.get('/products/$id');
      final data = (res.data as Map<String, dynamic>)['data'];
      return Product.fromJson(Map<String, dynamic>.from(data as Map));
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Vehicles this part fits, from GET /products/{id}/compatible-vehicles.
  Future<List<VehicleCompatibility>> compatibleVehicles(String id) async {
    try {
      final res = await _dio.get('/products/$id/compatible-vehicles');
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) =>
              VehicleCompatibility.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Calls GET /products/{id}/variants and collects deduplicated attribute values
  /// across all variants (first value wins per attribute name). Returns an empty
  /// list when no variants exist or none have attributes.
  Future<List<ProductAttributeValue>> getVariantAttributes(
      String productId) async {
    try {
      final res = await _dio.get('/products/$productId/variants');
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      final seen = <String>{};
      final attrs = <ProductAttributeValue>[];
      for (final v in data.whereType<Map>()) {
        final avs = v['attributeValues'];
        if (avs is! List) continue;
        for (final av in avs.whereType<Map>()) {
          final a = ProductAttributeValue.fromJson(
              Map<String, dynamic>.from(av));
          if (a.displayValue.isNotEmpty && seen.add(a.attributeName)) {
            attrs.add(a);
          }
        }
      }
      return attrs;
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<List<ProductLocation>> getLocations(String productId) async {
    try {
      final res = await _dio.get('/products/$productId/locations');
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) => ProductLocation.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Images/videos of a product, from GET /products/{id}/media (display order).
  Future<List<ProductMedia>> getMedia(String productId) async {
    try {
      final res = await _dio.get('/products/$productId/media');
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) => ProductMedia.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<ProductByCode> getByCode(String code) async {
    try {
      final res = await _dio.get('/products/by-code',
          queryParameters: {'code': code});
      final data = (res.data as Map<String, dynamic>)['data'];
      return ProductByCode.fromJson(Map<String, dynamic>.from(data as Map));
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final productsRepositoryProvider = Provider<ProductsRepository>(
  (ref) => ProductsRepository(ref.read(dioProvider)),
);
