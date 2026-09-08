import 'package:dio/dio.dart';

/// A user-presentable error mapped from the API's structured `ApiError` shape:
/// `{ type, title, status, detail, instance, errors? }`.
///
/// Mirrors the Angular web app which reads `error.error?.detail ?? error.error?.message`.
class AppException implements Exception {
  AppException(
    this.message, {
    this.statusCode,
    this.fieldErrors,
    this.code,
    this.limitType,
    this.partName,
  });

  final String message;
  final int? statusCode;

  /// Field-level validation errors keyed by field name (from `ApiError.errors`).
  final Map<String, List<String>>? fieldErrors;

  /// Machine-readable error code (e.g. `PRICE_OVERRIDE_REQUIRED`) some endpoints
  /// return alongside `message` so callers can branch on behavior instead of
  /// matching localized/free-text error strings. Null for ordinary errors.
  final String? code;

  /// Present only on `PRICE_OVERRIDE_REQUIRED` — which limit was breached
  /// (e.g. "cost floor" or "MRP ceiling"), for display in the approval dialog.
  final String? limitType;

  /// Present only on `PRICE_OVERRIDE_REQUIRED` — the offending part's name.
  final String? partName;

  factory AppException.fromDio(DioException e) {
    final response = e.response;
    final status = response?.statusCode;

    // Network/timeout style failures have no response body.
    if (response == null) {
      return AppException(_connectionMessage(e), statusCode: status);
    }

    final data = response.data;
    if (data is Map<String, dynamic>) {
      final detail = data['detail'] ?? data['message'] ?? data['title'];
      final errors = _parseFieldErrors(data['errors']);
      return AppException(
        detail is String && detail.isNotEmpty
            ? detail
            : _fallbackMessage(status),
        statusCode: status,
        fieldErrors: errors,
        code: data['code'] as String?,
        limitType: data['limitType'] as String?,
        partName: data['partName'] as String?,
      );
    }

    if (data is String && data.isNotEmpty) {
      return AppException(data, statusCode: status);
    }

    return AppException(_fallbackMessage(status), statusCode: status);
  }

  /// Permission-policy denials (403) come back with an empty body, so give
  /// them a human message instead of "Request failed (403).".
  static String _fallbackMessage(int? status) => status == 403
      ? "You don't have permission for this action. Ask an admin to grant it."
      : 'Request failed (${status ?? 'unknown'}).';

  static Map<String, List<String>>? _parseFieldErrors(dynamic raw) {
    if (raw is! Map) return null;
    final out = <String, List<String>>{};
    raw.forEach((key, value) {
      if (value is List) {
        out['$key'] = value.map((v) => '$v').toList();
      } else if (value != null) {
        out['$key'] = ['$value'];
      }
    });
    return out.isEmpty ? null : out;
  }

  static String _connectionMessage(DioException e) {
    switch (e.type) {
      case DioExceptionType.connectionTimeout:
      case DioExceptionType.sendTimeout:
      case DioExceptionType.receiveTimeout:
        return 'The server took too long to respond. Please try again.';
      case DioExceptionType.connectionError:
        return 'Cannot reach the server. Check your connection and the API address.';
      default:
        return e.message ?? 'An unexpected network error occurred.';
    }
  }

  @override
  String toString() => message;
}
