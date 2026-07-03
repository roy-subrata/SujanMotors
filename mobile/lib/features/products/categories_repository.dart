import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/product.dart';

class CategoriesRepository {
  CategoriesRepository(this._dio);

  final Dio _dio;

  /// Server-side search/paging over active categories. Ordered by
  /// displayOrder then name (server default) — never derived from loaded
  /// products, so it's complete and stable regardless of how many
  /// categories exist.
  Future<PagedResponse<Category>> search({
    String? query,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.get('/categories', queryParameters: {
        if (query != null && query.isNotEmpty) 'search': query,
        'isActive': true,
        'page': page,
        'pageSize': pageSize,
      });
      return PagedResponse.fromJson(
        res.data as Map<String, dynamic>,
        Category.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final categoriesRepositoryProvider = Provider<CategoriesRepository>(
  (ref) => CategoriesRepository(ref.read(dioProvider)),
);

/// First page of active categories, used for the quick-access tab row.
final quickCategoriesProvider = FutureProvider<List<Category>>((ref) async {
  final res = await ref
      .read(categoriesRepositoryProvider)
      .search(page: 1, pageSize: 12);
  return res.data;
});
