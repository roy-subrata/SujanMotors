import 'json.dart';

/// `CustomerVehicleResponse` from `GET /api/v1/customers/{id}/vehicles`.
class CustomerVehicle {
  const CustomerVehicle({
    required this.id,
    required this.customerId,
    required this.make,
    required this.model,
    this.year,
    this.registrationNo,
    this.color,
    required this.label,
    this.isActive = true,
  });

  final String id;
  final String customerId;
  final String make;
  final String model;
  final int? year;
  final String? registrationNo;
  final String? color;

  /// Pre-built display label from the API, e.g. "Toyota Hilux (2020) - ABC-123".
  final String label;
  final bool isActive;

  factory CustomerVehicle.fromJson(Map<String, dynamic> json) => CustomerVehicle(
        id: asString(json['id']),
        customerId: asString(json['customerId']),
        make: asString(json['make']),
        model: asString(json['model']),
        year: json['year'] == null ? null : asInt(json['year']),
        registrationNo: asStringOrNull(json['registrationNo']),
        color: asStringOrNull(json['color']),
        label: asString(json['label']),
        isActive: asBool(json['isActive'], fallback: true),
      );
}
