import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../core/network/permission_guard.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/sale_return.dart';

class SalesReturnsRepository {
  SalesReturnsRepository(this._dio, this._ref);

  final Dio _dio;
  final Ref _ref;

  /// Paged list of returns. Pass [searchTerm] to filter by customer name,
  /// return number, or SO number (server-side text contains).
  Future<PagedChunk<SalesReturn>> list({
    String? searchTerm,
    int page = 1,
    int pageSize = 20,
  }) async {
    await requirePermission(_ref, 'sales.view');
    try {
      final res = await _dio.get('/SalesReturn/list', queryParameters: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (searchTerm != null && searchTerm.isNotEmpty)
          'searchTerm': searchTerm,
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        SalesReturn.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Submit a quick return via `POST /SalesOrder/return`.
  Future<QuickReturnResult> createQuickReturn({
    required String originalInvoiceNumber,
    required List<QuickReturnItem> items,
    String refundType = 'CASH_REFUND',
  }) async {
    await requirePermission(_ref, 'sales.create');
    try {
      final res = await _dio.post('/SalesOrder/return', data: {
        'originalInvoiceNumber': originalInvoiceNumber,
        'refundType': refundType,
        'items': items.map((i) => i.toJson()).toList(),
      });
      return QuickReturnResult.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final salesReturnsRepositoryProvider = Provider<SalesReturnsRepository>(
  (ref) => SalesReturnsRepository(ref.read(dioProvider), ref),
);
