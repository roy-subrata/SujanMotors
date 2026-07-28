import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/config/api_config.dart';
import '../../core/network/app_exception.dart';
import '../../shared/models/json.dart';
import 'session.dart';

/// Why a renewal ended the way it did.
///
/// [throttled] must stay distinct from [expired]: the API rate-limits token
/// renewal per IP, and a shop NATs every till through one address, so a burst of
/// simultaneous renewals can be rejected even though every session is valid.
/// Treating that as a dead session would sign a cashier out mid-shift.
enum RefreshOutcome { renewed, throttled, expired }

class RefreshAttempt {
  const RefreshAttempt(this.outcome, [this.tokens]);

  final RefreshOutcome outcome;
  final RefreshedTokens? tokens;
}

/// Result of exchanging a refresh token: a new access token plus its successor
/// refresh token. Roles and permissions come back too, so a change made by an
/// admin takes effect on the next rotation instead of requiring a re-login.
class RefreshedTokens {
  const RefreshedTokens({
    required this.token,
    required this.refreshToken,
    this.refreshTokenExpiresAt,
    this.roles = const [],
    this.permissions = const [],
  });

  final String token;
  final String refreshToken;
  final DateTime? refreshTokenExpiresAt;
  final List<String> roles;
  final List<String> permissions;

  factory RefreshedTokens.fromJson(Map<String, dynamic> json) => RefreshedTokens(
        token: asString(json['token']),
        refreshToken: asString(json['refreshToken']),
        refreshTokenExpiresAt: asDateTimeOrNull(json['refreshTokenExpiresAt']),
        roles: asStringList(json['roles']),
        permissions: asStringList(json['permissions']),
      );
}

/// Talks to the anonymous `/auth` endpoints.
///
/// Deliberately uses its own bare [Dio] rather than the shared client: login,
/// refresh and logout are all anonymous, and routing them through the
/// authenticated client's error interceptor would let a 401 from a refresh
/// attempt trigger another refresh attempt.
class AuthRepository {
  AuthRepository(this._dio);

  final Dio _dio;

  Future<Session> login(String username, String password) async {
    try {
      final res = await _dio.post('/auth/login', data: {
        'username': username,
        'password': password,
      });
      return Session.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Exchanges [refreshToken] for a new pair.
  ///
  /// A rejection (401) is terminal — expired, revoked and reuse-detected are
  /// deliberately indistinguishable. Throttling or an unreachable server is not:
  /// the session is probably still valid, so the caller must retry rather than
  /// sign the user out.
  Future<RefreshAttempt> refresh(String refreshToken) async {
    try {
      final res = await _dio.post('/auth/refresh-token', data: {
        'refreshToken': refreshToken,
      });
      final tokens = RefreshedTokens.fromJson(res.data as Map<String, dynamic>);
      return tokens.token.isEmpty
          ? const RefreshAttempt(RefreshOutcome.expired)
          : RefreshAttempt(RefreshOutcome.renewed, tokens);
    } on DioException catch (e) {
      final status = e.response?.statusCode;

      // 429 rate limited, 5xx server trouble, or no response at all (offline,
      // timeout) — all transient.
      if (status == null || status == 429 || status >= 500) {
        return const RefreshAttempt(RefreshOutcome.throttled);
      }

      return const RefreshAttempt(RefreshOutcome.expired);
    } catch (_) {
      return const RefreshAttempt(RefreshOutcome.expired);
    }
  }

  /// Revokes the session server-side. Best-effort: a failure here must never
  /// stop the client from signing out locally.
  Future<void> logout(String refreshToken) async {
    try {
      await _dio.post('/auth/logout', data: {'refreshToken': refreshToken});
    } catch (_) {
      // The token still expires on its own.
    }
  }
}

/// Bare client for the anonymous auth endpoints — no bearer header, no
/// interceptors, so it cannot recurse through the authenticated client.
final authDioProvider = Provider<Dio>((ref) {
  return Dio(BaseOptions(
    baseUrl: ApiConfig.apiBaseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 20),
    contentType: Headers.jsonContentType,
  ));
});

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.read(authDioProvider)),
);
