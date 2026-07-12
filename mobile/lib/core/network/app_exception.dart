import 'package:dio/dio.dart';

/// A user-presentable error mapped from the API's structured `ApiError` shape:
/// `{ type, title, status, detail, instance, errors? }`.
///
/// Mirrors the Angular web app which reads `error.error?.detail ?? error.error?.message`.
class AppException implements Exception {
  AppException(this.message, {this.statusCode, this.fieldErrors});

  final String message;
  final int? statusCode;

  /// Field-level validation errors keyed by field name (from `ApiError.errors`).
  final Map<String, List<String>>? fieldErrors;

  /// Whether this error represents a 403 Forbidden response.
  bool get isForbidden => statusCode == 403;

  factory AppException.fromDio(DioException e) {
    final response = e.response;
    final status = response?.statusCode;

    // Network/timeout style failures have no response body.
    if (response == null) {
      return AppException(_connectionMessage(e), statusCode: status);
    }

    // 403 Forbidden — user is authenticated but lacks the required permission.
    if (status == 403) {
      return AppException(
        'You don\'t have permission for this action.',
        statusCode: 403,
      );
    }

    final data = response.data;
    if (data is Map<String, dynamic>) {
      final detail = data['detail'] ?? data['message'] ?? data['title'];
      final errors = _parseFieldErrors(data['errors']);
      return AppException(
        detail is String && detail.isNotEmpty
            ? detail
            : 'Request failed (${status ?? 'unknown'}).',
        statusCode: status,
        fieldErrors: errors,
      );
    }

    if (data is String && data.isNotEmpty) {
      return AppException(data, statusCode: status);
    }

    return AppException('Request failed (${status ?? 'unknown'}).',
        statusCode: status);
  }

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
