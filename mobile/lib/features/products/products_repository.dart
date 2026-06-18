import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';
import '../../shared/models/stock.dart';

class ProductsRepository {
  ProductsRepository(this._dio);

  final Dio _dio;

  Future<PagedResponse<Product>> search({
    String? query,
    int page = 1,
    int pageSize = 20,
    bool? isActive,
  }) async {
    try {
      final res = await _dio.get('/products', queryParameters: {
        if (query != null && query.isNotEmpty) 'search': query,
        'page': page,
        'pageSize': pageSize,
        'isActive': ?isActive,
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

  Future<ProductByCode> getByCode(String code) async {
    try {
      final res = await _dio.get('/products/by-code',
          queryParameters: {'code': code});
      return ProductByCode.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final productsRepositoryProvider = Provider<ProductsRepository>(
  (ref) => ProductsRepository(ref.read(dioProvider)),
);
