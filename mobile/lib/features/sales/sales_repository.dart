import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../core/network/permission_guard.dart';
import '../../shared/models/sale.dart';

class SalesRepository {
  SalesRepository(this._dio, this._ref);

  final Dio _dio;
  final Ref _ref;

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
    String? technicianId,
  }) async {
    await requirePermission(_ref, 'sales.create');
    try {
      final discountAmount = subtotal - grandTotal;
      final res = await _dio.post('/SalesOrder/quick-sale', data: {
        'customerName': customerName,
        'customerId': ?customerId,
        'customerPhone':
            ?(customerPhone?.isNotEmpty == true ? customerPhone : null),
        'customerVehicleId': ?vehicleId,
        'technicianId': ?technicianId,
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
            'amount': paidAmount,
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

  Future<List<int>> downloadInvoicePdf(String invoiceId) async {
    try {
      final res = await _dio.get(
        '/SalesOrder/invoices/$invoiceId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      return res.data as List<int>;
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final salesRepositoryProvider = Provider<SalesRepository>(
  (ref) => SalesRepository(ref.read(dioProvider), ref),
);
