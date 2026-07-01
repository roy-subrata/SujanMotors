import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/sale.dart';

class SalesRepository {
  SalesRepository(this._dio);

  final Dio _dio;

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
}

final salesRepositoryProvider = Provider<SalesRepository>(
  (ref) => SalesRepository(ref.read(dioProvider)),
);
