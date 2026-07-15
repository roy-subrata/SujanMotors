import 'json.dart';

/// A vehicle from `GET /api/v1/vehicles/list` — used by the compatibility
/// picker to attach a product to vehicles it fits.
class Vehicle {
  const Vehicle({
    required this.id,
    required this.make,
    required this.model,
    required this.year,
    this.engineType,
  });

  final String id;
  final String make;
  final String model;
  final int year;
  final String? engineType;

  /// "Make Model Year" for display.
  String get label => [make, model, if (year > 0) year.toString()]
      .where((p) => p.isNotEmpty)
      .join(' ');

  factory Vehicle.fromJson(Map<String, dynamic> json) => Vehicle(
        id: asString(json['id']),
        make: asString(json['make']),
        model: asString(json['model']),
        year: asInt(json['year']),
        engineType: asStringOrNull(json['engineType']),
      );
}
