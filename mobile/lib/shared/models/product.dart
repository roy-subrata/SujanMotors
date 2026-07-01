import 'json.dart';

/// `{ id, name }` reference (e.g. brand, unit).
class NamedRef {
  const NamedRef({required this.id, required this.name});
  final String id;
  final String name;

  factory NamedRef.fromJson(Map<String, dynamic> json) => NamedRef(
        id: asString(json['id']),
        name: asString(json['name']),
      );
}

class Category {
  const Category({required this.id, required this.name, this.breadcrumb});
  final String id;
  final String name;
  final String? breadcrumb;

  factory Category.fromJson(Map<String, dynamic> json) => Category(
        id: asString(json['id']),
        name: asString(json['name']),
        breadcrumb: asStringOrNull(json['breadcrumb']),
      );
}

/// `{ costPrice?, sellingPrice, currency }`. costPrice is null for anonymous
/// callers; staff sessions see it.
class Pricing {
  const Pricing({this.costPrice, required this.sellingPrice, this.currency});
  final double? costPrice;
  final double sellingPrice;
  final String? currency;

  factory Pricing.fromJson(Map<String, dynamic> json) => Pricing(
        costPrice: asDoubleOrNull(json['costPrice']),
        sellingPrice: asDouble(json['sellingPrice']),
        currency: asStringOrNull(json['currency']),
      );
}

class ProductVariant {
  const ProductVariant({
    this.id,
    required this.name,
    required this.code,
    this.sku,
    this.barcode,
    this.isDefault = false,
    this.isActive = true,
    this.pricing,
  });

  final String? id; // null when synthesized "Default" variant
  final String name;
  final String code;
  final String? sku;
  final String? barcode;
  final bool isDefault;
  final bool isActive;
  final Pricing? pricing;

  factory ProductVariant.fromJson(Map<String, dynamic> json) => ProductVariant(
        id: asStringOrNull(json['id']),
        name: asString(json['name']),
        code: asString(json['code']),
        sku: asStringOrNull(json['sku']),
        barcode: asStringOrNull(json['barcode']),
        isDefault: asBool(json['isDefault']),
        isActive: asBool(json['isActive'], fallback: true),
        pricing: asMapOrNull(json['pricing']) == null
            ? null
            : Pricing.fromJson(asMapOrNull(json['pricing'])!),
      );
}

/// Product as returned by `/api/v1/products` (list) and `/api/v1/products/{id}`.
/// `variants` always has at least one entry.
class Product {
  const Product({
    required this.id,
    required this.name,
    this.localName,
    this.description,
    required this.partNumber,
    required this.sku,
    this.oemNumber,
    this.barcode,
    this.productType,
    this.isActive = true,
    this.hasVariants = false,
    this.category,
    this.brand,
    this.pricing,
    this.variants = const [],
    this.unitName,
    this.totalStock,
  });

  final String id;
  final String name;
  final String? localName;
  final String? description;
  final String partNumber;
  final String sku;
  final String? oemNumber;
  final String? barcode;
  final String? productType;
  final bool isActive;
  final bool hasVariants;
  final Category? category;
  final NamedRef? brand;
  final Pricing? pricing;
  final List<ProductVariant> variants;
  final String? unitName;
  final int? totalStock;

  factory Product.fromJson(Map<String, dynamic> json) => Product(
        id: asString(json['id']),
        name: asString(json['name']),
        localName: asStringOrNull(json['localName']),
        description: asStringOrNull(json['description']),
        partNumber: asString(json['partNumber']),
        sku: asString(json['sku']),
        oemNumber: asStringOrNull(json['oemNumber']),
        barcode: asStringOrNull(json['barcode']),
        productType: asStringOrNull(json['productType']),
        isActive: asBool(json['isActive'], fallback: true),
        hasVariants: asBool(json['hasVariants']),
        // Nested format (detail endpoint) takes priority; flat format (list endpoint) is fallback.
        category: asMapOrNull(json['category']) != null
            ? Category.fromJson(asMapOrNull(json['category'])!)
            : asStringOrNull(json['categoryName']) != null
                ? Category(
                    id: asString(json['categoryId']),
                    name: asString(json['categoryName']),
                  )
                : null,
        brand: asMapOrNull(json['brand']) != null
            ? NamedRef.fromJson(asMapOrNull(json['brand'])!)
            : asStringOrNull(json['brandName']) != null
                ? NamedRef(
                    id: asString(json['brandId']),
                    name: asString(json['brandName']),
                  )
                : null,
        pricing: asMapOrNull(json['pricing']) != null
            ? Pricing.fromJson(asMapOrNull(json['pricing'])!)
            : (json['effectiveSellingPrice'] ?? json['sellingPrice']) != null
                ? Pricing(
                    sellingPrice:
                        asDouble(json['effectiveSellingPrice'] ?? json['sellingPrice']),
                  )
                : null,
        variants: asList(json['variants'], ProductVariant.fromJson),
        unitName: asStringOrNull(json['unitName']) ??
            asStringOrNull(asMapOrNull(json['unit'])?['name']),
        totalStock:
            json['totalStock'] != null ? asInt(json['totalStock']) : null,
      );
}
