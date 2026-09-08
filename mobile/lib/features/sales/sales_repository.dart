import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/invoice.dart';
import '../../shared/models/paged_response.dart';
import '../../shared/models/sale.dart';
import '../../shared/models/status_enums.dart';

class SalesRepository {
  SalesRepository(this._dio);

  final Dio _dio;

  /// All invoices (newest first), from GET /SalesOrder/invoices.
  /// [hasDue] keeps only invoices with an unpaid balance (the "Due" chip).
  Future<PagedChunk<Invoice>> invoices({
    String? search,
    InvoiceStatus? status,
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
        'status': ?status?.wire,
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
  /// [grandTotal] must be the SERVER-computed total — the API auto-applies
  /// item-level and cart-level discount rules before charging, so callers
  /// pre-resolve via DiscountsRepository (see ChargeScreen) and pass the
  /// mirrored figure here. [promoCode] resolves to a cart-level rule
  /// server-side; an invalid code simply applies nothing.
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
    String? promoCode,
    // Token from requestPriceOverride() — proves a manager approved this
    // specific sale going below the cost floor or above the MRP ceiling.
    // Null for an ordinary sale; the server ignores it unless a
    // PRICE_OVERRIDE_REQUIRED condition actually applies.
    String? priceOverrideApprovalToken,
  }) async {
    try {
      final promo = promoCode?.trim().toUpperCase() ?? '';
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
        'discountType':
            promo.isNotEmpty ? 'PROMO_CODE' : (discountAmount > 0 ? 'FIXED' : 'NONE'),
        'discountReason': ?(promo.isNotEmpty
            ? promo
            : (discountAmount > 0 ? 'Manual discount' : null)),
        'promoCode': promo.isNotEmpty ? promo : null,
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
        if (priceOverrideApprovalToken != null)
          'priceOverrideApprovalToken': priceOverrideApprovalToken,
      });
      return QuickSaleResult.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Manager-credential approval for a sale that would otherwise be rejected
  /// as below cost or above MRP (`PRICE_OVERRIDE_REQUIRED`). Verifies the
  /// given credentials belong to an active user holding
  /// `sales.approve-price-override` and returns a single-use, 5-minute token
  /// to attach as [submitQuickSale]'s `priceOverrideApprovalToken`. The
  /// backend returns one generic message for every failure mode (unknown
  /// user, wrong password, lockout, valid credentials without the
  /// permission) — surfaced via [AppException.message] as-is, matching the
  /// web app's same "don't become an oracle" behavior.
  Future<PriceOverrideApproval> requestPriceOverride({
    required String username,
    required String password,
  }) async {
    try {
      final res = await _dio.post('/pricing/request-override', data: {
        'username': username,
        'password': password,
      });
      final data = res.data as Map<String, dynamic>;
      return PriceOverrideApproval(
        token: '${data['token']}',
        expiresAt: DateTime.parse('${data['expiresAt']}'),
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

class PriceOverrideApproval {
  const PriceOverrideApproval({required this.token, required this.expiresAt});

  final String token;
  final DateTime expiresAt;
}

final salesRepositoryProvider = Provider<SalesRepository>(
  (ref) => SalesRepository(ref.read(dioProvider)),
);
