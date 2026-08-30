import 'json.dart';

/// One selectable value of a [FilterableAttribute] (e.g. "Red" for Color).
class AttributeOption {
  const AttributeOption({required this.id, required this.value});

  final String id;
  final String value;

  factory AttributeOption.fromJson(Map<String, dynamic> json) =>
      AttributeOption(
        id: asString(json['id']),
        value: asString(json['value']),
      );
}

/// One attribute within a [FilterableAttributeGroup] (e.g. Color, Size).
/// Only attributes with `dataType == 'option'` are rendered as filters —
/// text/number/boolean attributes aren't offered in the filter sheet.
class FilterableAttribute {
  const FilterableAttribute({
    required this.id,
    required this.name,
    required this.dataType,
    required this.options,
  });

  final String id;
  final String name;
  final String dataType;
  final List<AttributeOption> options;

  bool get isFilterable => dataType == 'option' && options.isNotEmpty;

  factory FilterableAttribute.fromJson(Map<String, dynamic> json) {
    final rawOptions = json['options'];
    return FilterableAttribute(
      id: asString(json['id']),
      name: asString(json['name']),
      dataType: asString(json['dataType']),
      options: rawOptions is List
          ? rawOptions
              .whereType<Map>()
              .map((o) => AttributeOption.fromJson(Map<String, dynamic>.from(o)))
              .toList()
          : const [],
    );
  }
}

/// An attribute group linked (directly or via an ancestor category) to a
/// category, from `GET /categories/{id}/attribute-groups`.
class FilterableAttributeGroup {
  const FilterableAttributeGroup({
    required this.id,
    required this.name,
    required this.attributes,
  });

  final String id;
  final String name;
  final List<FilterableAttribute> attributes;

  factory FilterableAttributeGroup.fromJson(Map<String, dynamic> json) {
    final rawAttributes = json['attributes'];
    return FilterableAttributeGroup(
      id: asString(json['id']),
      name: asString(json['name']),
      attributes: rawAttributes is List
          ? rawAttributes
              .whereType<Map>()
              .map((a) =>
                  FilterableAttribute.fromJson(Map<String, dynamic>.from(a)))
              .toList()
          : const [],
    );
  }
}
