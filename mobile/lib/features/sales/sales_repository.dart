import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/invoice.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/sale.dart';

class SalesRepository {
  SalesRepository(this._dio);

  final Dio _dio;

  /// All invoices (newest first), from GET /SalesOrder/invoices.
  /// [hasDue] keeps only invoices with an unpaid balance (the "Due" chip).
  Future<PagedChunk<Invoice>> invoices({
    String? search,
    String? status,
    bool hasDue = false,
    DateTime? fromDate,
    DateTime? toDate,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.get('/SalesOrder/invoices', queryParameters: {
        'pageNumber': page,
        'pageSize': pageSize,
        if (search != null && search.isNotEmpty) 'searchTerm': search,
        'status': ?status,
        if (hasDue) 'hasDue': true,
        if (fromDate != null)
          'fromDate': fromDate.toIso8601String().substring(0, 10),
        if (toDate != null)
          'toDate': toDate.toIso8601String().substring(0, 10),
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        Invoice.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<QuickSaleResult> submitQuickSale({
    required List<QuickSaleItem> items,
    required double subtotal,
    required double grandTotal,
    required double paidAmount,
    required double dueAmount,
    required String paymentMethod, // CASH | DUE
    String customerName = 'Walk-in',
    String? customerId,
    String? customerPhone,
    String? vehicleId,
  }) async {
    try {
      final discountAmount = subtotal - grandTotal;
      final res = await _dio.post('/SalesOrder/quick-sale', data: {
        'customerName': customerName,
        'customerId': ?customerId,
        'customerPhone':
            ?(customerPhone?.isNotEmpty == true ? customerPhone : null),
        'customerVehicleId': ?vehicleId,
        'subtotal': subtotal,
        'discountAmount': discountAmount > 0 ? discountAmount : 0,
        'grandTotal': grandTotal,
        'paidAmount': paidAmount,
        'dueAmount': dueAmount,
        'channel': 'MOBILE',
        'items': items
            .map((i) => {
                  'partId': i.partId,
                  'quantity': i.quantity,
                  'unitPrice': i.unitPrice,
                  if (i.variantId != null) 'productVariantId': i.variantId,
                })
            .toList(),
        'payments': [
          {
            'method': paymentMethod,
            // The API requires payment lines (including a DUE line for the unpaid
            // balance) to account for the full invoice total.
            'amount': paymentMethod == 'DUE' ? dueAmount : paidAmount,
            'reference': '',
            'notes': '',
          }
        ],
      });
      return QuickSaleResult.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final salesRepositoryProvider = Provider<SalesRepository>(
  (ref) => SalesRepository(ref.read(dioProvider)),
);
