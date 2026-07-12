import 'json.dart';

/// One row from `GET /api/v1/products/{id}/compatible-vehicles`.
class VehicleCompatibility {
  const VehicleCompatibility({
    required this.id,
    required this.vehicleId,
    required this.make,
    required this.model,
    required this.year,
    this.engineType,
    required this.vehicleInfo,
    required this.isCompatible,
    this.notes,
  });

  final String id;
  final String vehicleId;
  final String make;
  final String model;
  final int year;
  final String? engineType;
  final String vehicleInfo;
  final bool isCompatible;
  final String? notes;

  /// "Make Model Year", falling back to the server-built vehicleInfo.
  String get title {
    final parts = [make, model, if (year > 0) year.toString()]
        .where((p) => p.isNotEmpty)
        .join(' ');
    return parts.isNotEmpty ? parts : vehicleInfo;
  }

  factory VehicleCompatibility.fromJson(Map<String, dynamic> json) {
    return VehicleCompatibility(
      id: asString(json['id']),
      vehicleId: asString(json['vehicleId']),
      make: asString(json['vehicleMake']),
      model: asString(json['vehicleModel']),
      year: asInt(json['vehicleYear']),
      engineType: asStringOrNull(json['vehicleEngineType']),
      vehicleInfo: asString(json['vehicleInfo']),
      isCompatible: asBool(json['isCompatible'], fallback: true),
      notes: asStringOrNull(json['notes']),
    );
  }
}
