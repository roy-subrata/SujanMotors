import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/json.dart';

/// One resolved discount rule, as returned by the `/discounts/resolve/*`
/// endpoints. Mirrors the backend's `DiscountResolutionResult`.
class DiscountResolution {
  const DiscountResolution({
    this.discountId,
    this.discountName,
    required this.appliedLevel,
    this.discountAmount = 0,
  });

  /// NONE | PRODUCT | VARIANT (item scope) or CART.
  final String appliedLevel;

  /// Money amount the rule knocks off [the price it was resolved against].
  final double discountAmount;
  final String? discountId;
  final String? discountName;

  bool get applied =>
      appliedLevel != 'NONE' && discountAmount > 0;

  factory DiscountResolution.fromJson(Map<String, dynamic> json) =>
      DiscountResolution(
        appliedLevel: asString(json['appliedLevel'], fallback: 'NONE'),
        discountAmount: asDouble(json['discountAmount']),
        discountId: asStringOrNull(json['discountId']),
        discountName: asStringOrNull(json['discountName']),
      );
}

/// Read-only access to the server-side discount resolver so POS totals can
/// mirror what `POST /SalesOrder/quick-sale` will actually charge.
///
/// Both endpoints need only `inventory.view`, which every sales role has.
class DiscountsRepository {
  DiscountsRepository(this._dio);

  final Dio _dio;

  /// Best item-level rule for one line
  /// (`GET /discounts/resolve/item`). Variant-level beats product-level when
  /// both apply — same tie-breaking the sale flow performs.
  Future<DiscountResolution> resolveItem({
    required String partId,
    String? variantId,
    required double unitPrice,
  }) async {
    try {
      final res = await _dio.get('/discounts/resolve/item',
          queryParameters: {
            'partId': partId,
            'variantId': ?variantId,
            'unitPrice': unitPrice,
          });
      return DiscountResolution.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Cart-level rule for a subtotal (`GET /discounts/resolve/cart`).
  ///
  /// With a [promoCode] an invalid/expired code returns `NONE` — the server
  /// never falls through to threshold discounts for a typed code. With no
  /// promo code this resolves the best threshold rule whose minimum cart
  /// amount the subtotal clears.
  Future<DiscountResolution> resolveCart(
    double cartSubtotal, {
    String? promoCode,
  }) async {
    try {
      final res = await _dio.get('/discounts/resolve/cart',
          queryParameters: {
            'cartSubtotal': cartSubtotal,
            'promoCode': ?promoCode,
          });
      return DiscountResolution.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final discountsRepositoryProvider = Provider<DiscountsRepository>(
  (ref) => DiscountsRepository(ref.read(dioProvider)),
);
