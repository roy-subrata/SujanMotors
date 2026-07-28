import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/auth_repository.dart';
import '../storage/token_storage.dart';

/// Renews the access token, at most one renewal at a time.
///
/// Refresh tokens are single-use server-side. If several requests fail with 401
/// together — which is the normal case, since a screen usually fires a handful
/// of calls at once — each firing its own rotation would spend the same token
/// repeatedly, and the second attempt would look like a replay and revoke the
/// entire session. So callers share one in-flight refresh and all wait on it.
class TokenRefresher {
  TokenRefresher(this._repository, this._storage);

  final AuthRepository _repository;
  final TokenStorage _storage;

  Future<RefreshOutcome>? _inFlight;

  /// [RefreshOutcome.renewed] means a fresh access token is persisted and the
  /// caller may retry. [RefreshOutcome.throttled] means try again later but keep
  /// the session. [RefreshOutcome.expired] means sign out.
  Future<RefreshOutcome> refresh() {
    return _inFlight ??= _refresh().whenComplete(() => _inFlight = null);
  }

  Future<RefreshOutcome> _refresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      // Nothing to renew with — e.g. a session stored by a build that predates
      // refresh tokens.
      return RefreshOutcome.expired;
    }

    final attempt = await _repository.refresh(refreshToken);
    if (attempt.outcome != RefreshOutcome.renewed) return attempt.outcome;

    final tokens = attempt.tokens!;
    final session = await _storage.readSession();
    if (session == null) return RefreshOutcome.expired;

    await _storage.saveSession(session.withRefreshedTokens(
      token: tokens.token,
      refreshToken: tokens.refreshToken,
      refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
      roles: tokens.roles.isEmpty ? null : tokens.roles,
      permissions: tokens.permissions.isEmpty ? null : tokens.permissions,
    ));

    return RefreshOutcome.renewed;
  }
}

final tokenRefresherProvider = Provider<TokenRefresher>((ref) {
  return TokenRefresher(
    ref.read(authRepositoryProvider),
    ref.read(tokenStorageProvider),
  );
});
