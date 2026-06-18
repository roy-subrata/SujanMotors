// Small, tolerant JSON coercion helpers used by hand-written model parsers.
//
// The API serializes decimals as numbers, but we parse defensively so a stray
// string or null never crashes the client.

String asString(dynamic v, {String fallback = ''}) =>
    v == null ? fallback : v.toString();

String? asStringOrNull(dynamic v) {
  if (v == null) return null;
  final s = v.toString();
  return s.isEmpty ? null : s;
}

bool asBool(dynamic v, {bool fallback = false}) {
  if (v is bool) return v;
  if (v is num) return v != 0;
  if (v is String) return v.toLowerCase() == 'true';
  return fallback;
}

int asInt(dynamic v, {int fallback = 0}) {
  if (v is int) return v;
  if (v is num) return v.toInt();
  if (v is String) return int.tryParse(v) ?? fallback;
  return fallback;
}

double asDouble(dynamic v, {double fallback = 0}) {
  if (v is num) return v.toDouble();
  if (v is String) return double.tryParse(v) ?? fallback;
  return fallback;
}

double? asDoubleOrNull(dynamic v) {
  if (v == null) return null;
  if (v is num) return v.toDouble();
  if (v is String) return double.tryParse(v);
  return null;
}

List<String> asStringList(dynamic v) {
  if (v is List) return v.map((e) => e.toString()).toList();
  return const [];
}

List<T> asList<T>(dynamic v, T Function(Map<String, dynamic>) fromJson) {
  if (v is List) {
    return v
        .whereType<Map>()
        .map((e) => fromJson(Map<String, dynamic>.from(e)))
        .toList();
  }
  return const [];
}

Map<String, dynamic>? asMapOrNull(dynamic v) {
  if (v is Map) return Map<String, dynamic>.from(v);
  return null;
}
