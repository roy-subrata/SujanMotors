import 'json.dart';

/// A cart item held locally in the Quick Sale controller.
class QuickSaleItem {
  const QuickSaleItem({
    required this.partId,
    this.variantId,
    required this.name,
    this.localName,
    required this.unitPrice,
    required this.quantity,
    this.availableStock,
  });

  final String partId;
  final String? variantId;
  final String name;
  final String? localName;
  final double unitPrice;
  final int quantity;

  /// Stock on hand at the time this line was added/looked up. `null` means
  /// unknown (stock data wasn't available) — quantity is then left uncapped.
  final int? availableStock;

  double get lineTotal => unitPrice * quantity;

  QuickSaleItem copyWith({int? quantity, double? unitPrice}) => QuickSaleItem(
        partId: partId,
        variantId: variantId,
        name: name,
        localName: localName,
        unitPrice: unitPrice ?? this.unitPrice,
        quantity: quantity ?? this.quantity,
        availableStock: availableStock,
      );
}

/// Response from `POST /api/v1/SalesOrder/quick-sale` — fields for the success
/// screen.
class QuickSaleResult {
  const QuickSaleResult({
    required this.invoiceNumber,
    required this.salesOrderNumber,
    required this.grandTotal,
    this.paidAmount = 0,
    this.dueAmount = 0,
    this.vehicleLabel,
  });

  final String invoiceNumber;
  final String salesOrderNumber;
  final double grandTotal;
  final double paidAmount;
  final double dueAmount;
  final String? vehicleLabel;

  bool get hasDue => dueAmount > 0;

  factory QuickSaleResult.fromJson(Map<String, dynamic> json) => QuickSaleResult(
        invoiceNumber: asString(json['invoiceNumber']),
        salesOrderNumber: asString(json['salesOrderNumber']),
        grandTotal: asDouble(json['grandTotal']),
        paidAmount: asDouble(json['paidAmount']),
        dueAmount: asDouble(json['dueAmount']),
        vehicleLabel: asStringOrNull(json['vehicleLabel']),
      );
}
