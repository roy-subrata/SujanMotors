import 'json.dart';

class ProductLocation {
  const ProductLocation({
    required this.id,
    required this.partId,
    required this.warehouseId,
    required this.warehouseName,
    required this.section,
    required this.shelf,
    required this.fullLocation,
    this.notes,
    required this.isPrimary,
  });

  final String id;
  final String partId;
  final String warehouseId;
  final String warehouseName;
  final String section;
  final String shelf;
  final String fullLocation;
  final String? notes;
  final bool isPrimary;

  factory ProductLocation.fromJson(Map<String, dynamic> json) =>
      ProductLocation(
        id: asString(json['id']),
        partId: asString(json['partId']),
        warehouseId: asString(json['warehouseId']),
        warehouseName: asString(json['warehouseName']),
        section: asString(json['section']),
        shelf: asString(json['shelf']),
        fullLocation: asString(json['fullLocation']),
        notes: asStringOrNull(json['notes']),
        isPrimary: asBool(json['isPrimary']),
      );
}
