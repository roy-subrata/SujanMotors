import 'json.dart';

/// `StockLevelResponse` from `GET /api/v1/stock/levels/part/{partId}`.
class StockLevel {
  const StockLevel({
    required this.id,
    required this.partId,
    this.variantId,
    this.variantName,
    required this.displayName,
    required this.warehouseId,
    this.warehouseName,
    required this.quantity,
    required this.reservedQuantity,
    required this.availableQuantity,
    this.damagedQuantity = 0,
    this.quarantineQuantity = 0,
    this.reorderLevel = 0,
    this.needsReorder = false,
    this.unitName,
    this.unitSymbol,
  });

  final String id;
  final String partId;
  final String? variantId;
  final String? variantName;
  final String displayName;
  final String warehouseId;
  final String? warehouseName;
  final int quantity;
  final int reservedQuantity;
  final int availableQuantity;
  final int damagedQuantity;
  final int quarantineQuantity;
  final int reorderLevel;
  final bool needsReorder;
  final String? unitName;
  final String? unitSymbol;

  factory StockLevel.fromJson(Map<String, dynamic> json) => StockLevel(
        id: asString(json['id']),
        partId: asString(json['partId']),
        variantId: asStringOrNull(json['variantId']),
        variantName: asStringOrNull(json['variantName']),
        displayName: asString(json['displayName']),
        warehouseId: asString(json['warehouseId']),
        warehouseName: asStringOrNull(json['warehouseName']),
        quantity: asInt(json['quantity']),
        reservedQuantity: asInt(json['reservedQuantity']),
        availableQuantity: asInt(json['availableQuantity']),
        damagedQuantity: asInt(json['damagedQuantity']),
        quarantineQuantity: asInt(json['quarantineQuantity']),
        reorderLevel: asInt(json['reorderLevel']),
        needsReorder: asBool(json['needsReorder']),
        unitName: asStringOrNull(json['unitName']),
        unitSymbol: asStringOrNull(json['unitSymbol']),
      );
}

/// A purchased inventory lot from `GET /api/v1/StockLot/part/{partId}`
/// (`StockLotResponse`). Carries the unit cost and buying (receiving) date that
/// the per-warehouse stock levels don't expose.
class StockLot {
  const StockLot({
    required this.id,
    required this.lotNumber,
    required this.warehouseId,
    this.warehouseName,
    this.supplierName,
    required this.quantityAvailable,
    required this.quantityReceived,
    this.unitName,
    this.unitCode,
    required this.costPrice,
    this.currency,
    required this.receivingDate,
    this.expiryDate,
    this.isExpired = false,
    this.hasWarranty = false,
  });

  final String id;
  final String lotNumber;
  final String warehouseId;
  final String? warehouseName;
  final String? supplierName;
  final int quantityAvailable;
  final int quantityReceived;
  final String? unitName;
  final String? unitCode;
  final double costPrice;
  final String? currency;
  final DateTime receivingDate;
  final DateTime? expiryDate;
  final bool isExpired;
  final bool hasWarranty;

  factory StockLot.fromJson(Map<String, dynamic> json) => StockLot(
        id: asString(json['id']),
        lotNumber: asString(json['lotNumber']),
        warehouseId: asString(json['warehouseId']),
        warehouseName: asStringOrNull(json['warehouseName']),
        supplierName: asStringOrNull(json['supplierName']),
        quantityAvailable: asInt(json['quantityAvailable']),
        quantityReceived: asInt(json['quantityReceived']),
        unitName: asStringOrNull(json['unitName']),
        unitCode: asStringOrNull(json['unitCode']),
        costPrice: asDouble(json['costPrice']),
        currency: asStringOrNull(json['currency']),
        receivingDate:
            DateTime.tryParse(asString(json['receivingDate']))?.toLocal() ??
                DateTime.now(),
        expiryDate: DateTime.tryParse(asString(json['expiryDate']))?.toLocal(),
        isExpired: asBool(json['isExpired']),
        hasWarranty: asBool(json['hasWarranty']),
      );
}

/// Response from `GET /api/v1/products/by-code` (not wrapped in `data`).
class ProductByCode {
  const ProductByCode({
    required this.productId,
    required this.name,
    required this.sku,
    required this.partNumber,
    required this.sellingPrice,
    required this.stockLevel,
    this.unitName,
    this.variantId,
    this.variantName,
    this.variantCode,
  });

  final String productId;
  final String name;
  final String sku;
  final String partNumber;
  final double sellingPrice;
  final int stockLevel;
  final String? unitName;
  final String? variantId;
  final String? variantName;
  final String? variantCode;

  factory ProductByCode.fromJson(Map<String, dynamic> json) => ProductByCode(
        productId: asString(json['productId']),
        name: asString(json['name']),
        sku: asString(json['sku']),
        partNumber: asString(json['partNumber']),
        sellingPrice: asDouble(json['sellingPrice']),
        stockLevel: asInt(json['stockLevel']),
        unitName: asStringOrNull(json['unitName']),
        variantId: asStringOrNull(json['variantId']),
        variantName: asStringOrNull(json['variantName']),
        variantCode: asStringOrNull(json['variantCode']),
      );
}
