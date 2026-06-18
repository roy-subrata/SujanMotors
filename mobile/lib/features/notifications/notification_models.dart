import '../../shared/models/json.dart';

/// A realtime sale notification, parsed from the API's `SaleNotificationEvent`
/// SignalR payload (method `ReceiveSaleNotification`).
class SaleNotification {
  const SaleNotification({
    required this.salesOrderId,
    required this.soNumber,
    required this.customerName,
    required this.grandTotal,
    required this.currency,
    required this.saleChannel,
    required this.saleType,
    required this.occurredAt,
    required this.createdBy,
  });

  final String salesOrderId;
  final String soNumber;
  final String customerName;
  final double grandTotal;
  final String currency;
  final String saleChannel; // POS | ECOMMERCE | MOBILE
  final String saleType; // SALE | QUICK_SALE
  final DateTime occurredAt;
  final String createdBy;

  factory SaleNotification.fromJson(Map<String, dynamic> json) {
    return SaleNotification(
      salesOrderId: asString(json['salesOrderId']),
      soNumber: asString(json['soNumber'] ?? json['sONumber']),
      customerName: asString(json['customerName']),
      grandTotal: asDouble(json['grandTotal']),
      currency: asStringOrNull(json['currency']) ?? 'BDT',
      saleChannel: asStringOrNull(json['saleChannel']) ?? 'POS',
      saleType: asStringOrNull(json['saleType']) ?? 'SALE',
      occurredAt:
          DateTime.tryParse(asString(json['occurredAt']))?.toLocal() ??
              DateTime.now(),
      createdBy: asString(json['createdBy']),
    );
  }
}

/// A notification as held in the in-app inbox, wrapping the realtime payload
/// with local read state.
class AppNotification {
  AppNotification({required this.sale, this.read = false});

  final SaleNotification sale;
  final bool read;

  AppNotification copyWith({bool? read}) =>
      AppNotification(sale: sale, read: read ?? this.read);
}
