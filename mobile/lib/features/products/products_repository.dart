import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/models/product_location.dart';
import '../../shared/models/product_media.dart';
import '../../shared/models/product_specification.dart';
import '../../shared/models/stock.dart';
import '../../shared/models/vehicle.dart';
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
    bool lowStockOnly = false,
  }) async {
    try {
      final res = await _dio.get('/products', queryParameters: {
        if (query != null && query.isNotEmpty) 'search': query,
        'page': page,
        'pageSize': pageSize,
        'isActive': ?isActive,
        'categoryId': ?categoryId,
        if (lowStockOnly) 'lowStockOnly': true,
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

  /// Raw detail payload for the edit form. PUT /products/{id} is a full
  /// replace, so the form round-trips fields the mobile model doesn't parse
  /// (warranty, dimensions, tax, tags) from this map to avoid wiping them.
  Future<Map<String, dynamic>> getRawById(String id) async {
    try {
      final res = await _dio.get('/products/$id');
      final data = (res.data as Map<String, dynamic>)['data'];
      return Map<String, dynamic>.from(data as Map);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Active brands for the edit form dropdown (GET /brands).
  Future<List<NamedRef>> brands() async {
    try {
      final res = await _dio.get('/brands', queryParameters: {
        'isActive': true,
        'pageSize': 100,
      });
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((m) => NamedRef.fromJson(Map<String, dynamic>.from(m)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Full product update (PUT /products/{id}). Needs the inventory.edit
  /// permission — the API's 403 surfaces as an [AppException].
  Future<void> updateProduct(String id, Map<String, dynamic> payload) async {
    try {
      await _dio.put('/products/$id', data: payload);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Descriptive specs (Label/Value) for a product,
  /// GET /products/{id}/specifications (display order).
  Future<List<ProductSpecification>> getSpecifications(String id) async {
    try {
      final res = await _dio.get('/products/$id/specifications');
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((m) =>
              ProductSpecification.fromJson(Map<String, dynamic>.from(m)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Replaces a product's specs (PUT /products/{id}/specifications). Needs
  /// inventory.edit.
  Future<void> updateSpecifications(
      String id, List<ProductSpecification> specs) async {
    try {
      await _dio.put('/products/$id/specifications',
          data: {'specifications': specs.map((s) => s.toJson()).toList()});
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Typeahead suggestions for the spec editor. [field] is 'label' or 'value';
  /// [labelKey] scopes value suggestions to one label.
  Future<List<String>> specificationSuggestions({
    required String field,
    String? query,
    String? labelKey,
  }) async {
    try {
      final res = await _dio.get('/products/specifications/suggestions',
          queryParameters: {
            'field': field,
            if (query != null && query.isNotEmpty) 'query': query,
            if (labelKey != null && labelKey.isNotEmpty) 'labelKey': labelKey,
          });
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data.map((e) => e.toString()).toList();
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

  /// Searches vehicles for the compatibility picker (GET /vehicles/list).
  Future<List<Vehicle>> searchVehicles(String query) async {
    try {
      final res = await _dio.get('/vehicles/list', queryParameters: {
        'pageNumber': 1,
        'pageSize': 30,
        if (query.isNotEmpty) 'searchTerm': query,
      });
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((m) => Vehicle.fromJson(Map<String, dynamic>.from(m)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Attaches a vehicle to a product
  /// (POST /vehicles/{vehicleId}/parts/{partId}/compatibility). Needs
  /// inventory.edit. A 409 means the pairing already exists.
  Future<void> addCompatibility({
    required String partId,
    required String vehicleId,
    bool isCompatible = true,
    String? notes,
  }) async {
    try {
      await _dio.post(
        '/vehicles/$vehicleId/parts/$partId/compatibility',
        data: {'isCompatible': isCompatible, 'notes': ?notes},
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Removes a compatibility record
  /// (DELETE /vehicles/compatibilities/{compatibilityId}).
  Future<void> removeCompatibility(String compatibilityId) async {
    try {
      await _dio.delete('/vehicles/compatibilities/$compatibilityId');
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

  /// Uploads an image file (POST /files, multipart) and attaches it to the
  /// product (POST /products/{id}/media). The API auto-marks the first media
  /// of a product as primary. Requires the inventory.edit permission.
  Future<void> uploadProductImage({
    required String productId,
    required String filePath,
    required String fileName,
  }) async {
    try {
      final form = FormData.fromMap({
        'file': await MultipartFile.fromFile(filePath, filename: fileName),
        'ownerType': 'PRODUCT',
        'ownerId': productId,
      });
      final uploadRes = await _dio.post('/files', data: form);
      final stored =
          (uploadRes.data as Map<String, dynamic>)['data'] as Map;
      await _dio.post('/products/$productId/media', data: {
        'url': stored['url'],
        'mediaType': 'image',
        'fileName': stored['fileName'],
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Marks one media item as the product's primary image.
  Future<void> setPrimaryMedia(String productId, String mediaId) async {
    try {
      await _dio.patch('/products/$productId/media/$mediaId/primary');
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<void> deleteMedia(String productId, String mediaId) async {
    try {
      await _dio.delete('/products/$productId/media/$mediaId');
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
