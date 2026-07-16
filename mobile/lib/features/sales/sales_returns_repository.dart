import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/sale_return.dart';

class SalesReturnsRepository {
  SalesReturnsRepository(this._dio);

  final Dio _dio;

  /// Paged list of returns. Pass [searchTerm] to filter by customer name,
  /// return number, or SO number (server-side text contains); [fromDate] /
  /// [toDate] bound the return date (inclusive).
  Future<PagedChunk<SalesReturn>> list({
    String? searchTerm,
    DateTime? fromDate,
    DateTime? toDate,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.get('/SalesReturn/list', queryParameters: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (searchTerm != null && searchTerm.isNotEmpty)
          'searchTerm': searchTerm,
        if (fromDate != null)
          'fromDate': fromDate.toIso8601String().substring(0, 10),
        if (toDate != null)
          'toDate': toDate.toIso8601String().substring(0, 10),
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
  (ref) => SalesReturnsRepository(ref.read(dioProvider)),
);
