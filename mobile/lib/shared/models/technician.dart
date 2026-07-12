import '../models/json.dart';

/// A technician/mechanic who brings parts for customers.
class Technician {
  const Technician({
    required this.id,
    required this.technicianCode,
    required this.name,
    required this.phone,
    this.email,
    this.shopName,
    this.address,
    this.city,
    this.status = 'ACTIVE',
    this.notes,
  });

  final String id;
  final String technicianCode;
  final String name;
  final String phone;
  final String? email;
  final String? shopName;
  final String? address;
  final String? city;
  final String status;
  final String? notes;

  bool get isActive => status.toUpperCase() == 'ACTIVE';

  factory Technician.fromJson(Map<String, dynamic> json) {
    return Technician(
      id: asString(json['id']),
      technicianCode: asString(json['technicianCode']),
      name: asString(json['name']),
      phone: asString(json['phone']),
      email: asStringOrNull(json['email']),
      shopName: asStringOrNull(json['shopName']),
      address: asStringOrNull(json['address']),
      city: asStringOrNull(json['city']),
      status: asString(json['status']),
      notes: asStringOrNull(json['notes']),
    );
  }
}
