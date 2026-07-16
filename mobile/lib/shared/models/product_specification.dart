import 'json.dart';

/// A simple descriptive product spec (Label/Value), from
/// `GET /products/{id}/specifications`. The normalized [key] is server-derived
/// and used for ecommerce facet grouping; the editor only touches label/value.
class ProductSpecification {
  const ProductSpecification({
    required this.label,
    required this.value,
    this.key,
  });

  final String label;
  final String value;
  final String? key;

  factory ProductSpecification.fromJson(Map<String, dynamic> json) =>
      ProductSpecification(
        label: asString(json['label']),
        value: asString(json['value']),
        key: asStringOrNull(json['key']),
      );

  Map<String, dynamic> toJson() => {'label': label, 'value': value};

  ProductSpecification copyWith({String? label, String? value}) =>
      ProductSpecification(
        label: label ?? this.label,
        value: value ?? this.value,
        key: key,
      );
}
