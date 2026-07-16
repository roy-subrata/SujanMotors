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

  /// Submits a quick sale. [paidAmount] is what's tendered now via
  /// [paymentMethod] (CASH/CARD/MOBILE_BANKING/BANK_TRANSFER/CHEQUE); any
  /// [dueAmount] remainder goes on the customer's account as a DUE line. The
  /// API requires payment lines to sum to the grand total, so a partial payment
  /// sends both a paid line and a DUE line.
  /// [advanceApplied] draws down the customer's existing advance credit. The
  /// API requires payment lines + advance applied to equal the grand total, so
  /// paidAmount + dueAmount + advanceApplied must equal grandTotal.
  Future<QuickSaleResult> submitQuickSale({
    required List<QuickSaleItem> items,
    required double subtotal,
    required double grandTotal,
    required double paidAmount,
    required double dueAmount,
    required String paymentMethod,
    double discountAmount = 0,
    double advanceApplied = 0,
    String paymentReference = '',
    String customerName = 'Walk-in',
    String? customerId,
    String? customerPhone,
    String? vehicleId,
  }) async {
    try {
      final payments = <Map<String, dynamic>>[
        if (paidAmount > 0)
          {
            'method': paymentMethod,
            'amount': paidAmount,
            'reference': paymentReference,
            'notes': '',
          },
        if (dueAmount > 0)
          {'method': 'DUE', 'amount': dueAmount, 'reference': '', 'notes': ''},
      ];
      final res = await _dio.post('/SalesOrder/quick-sale', data: {
        'customerName': customerName,
        'customerId': ?customerId,
        'customerPhone':
            ?(customerPhone?.isNotEmpty == true ? customerPhone : null),
        'customerVehicleId': ?vehicleId,
        'subtotal': subtotal,
        'discountAmount': discountAmount > 0 ? discountAmount : 0,
        'discountType': discountAmount > 0 ? 'FIXED' : 'NONE',
        'grandTotal': grandTotal,
        'paidAmount': paidAmount,
        'dueAmount': dueAmount,
        if (advanceApplied > 0) 'useAdvanceBalance': true,
        if (advanceApplied > 0) 'advanceAmountToApply': advanceApplied,
        'channel': 'MOBILE',
        'items': items
            .map((i) => {
                  'partId': i.partId,
                  'quantity': i.quantity,
                  'unitPrice': i.unitPrice,
                  if (i.variantId != null) 'productVariantId': i.variantId,
                })
            .toList(),
        'payments': payments,
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
