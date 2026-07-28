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

  Future<bool>? _inFlight;

  /// Returns true when a fresh access token has been persisted and the caller
  /// may retry, false when the session is over and the caller should log out.
  Future<bool> refresh() {
    return _inFlight ??= _refresh().whenComplete(() => _inFlight = null);
  }

  Future<bool> _refresh() async {
    final refreshToken = await _storage.readRefreshToken();
    if (refreshToken == null || refreshToken.isEmpty) {
      // Nothing to renew with — e.g. a session stored by a build that predates
      // refresh tokens.
      return false;
    }

    final tokens = await _repository.refresh(refreshToken);
    if (tokens == null) return false;

    final session = await _storage.readSession();
    if (session == null) return false;

    await _storage.saveSession(session.withRefreshedTokens(
      token: tokens.token,
      refreshToken: tokens.refreshToken,
      refreshTokenExpiresAt: tokens.refreshTokenExpiresAt,
      roles: tokens.roles.isEmpty ? null : tokens.roles,
      permissions: tokens.permissions.isEmpty ? null : tokens.permissions,
    ));

    return true;
  }
}

final tokenRefresherProvider = Provider<TokenRefresher>((ref) {
  return TokenRefresher(
    ref.read(authRepositoryProvider),
    ref.read(tokenStorageProvider),
  );
});
