import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/storage/token_storage.dart';
import 'auth_repository.dart';
import 'session.dart';

/// Holds the current staff session. `null` data == logged out.
/// On first build it rehydrates the session from secure storage.
class AuthController extends AsyncNotifier<Session?> {
  @override
  Future<Session?> build() async {
    return ref.read(tokenStorageProvider).readSession();
  }

  /// Does NOT set `state = AsyncLoading()` first: the router redirects to
  /// `/splash` on any loading auth state, which would tear down and rebuild
  /// LoginScreen (wiping the typed username/password) on every attempt.
  /// `LoginScreen` tracks its own local submitting flag for the button spinner.
  Future<void> login(String username, String password) async {
    state = await AsyncValue.guard(() async {
      try {
        final session =
            await ref.read(authRepositoryProvider).login(username, password);
        await ref.read(tokenStorageProvider).saveSession(session);
        return session;
      } on AppException catch (e, st) {
        // Surface the friendly message; keep AsyncError typed for the UI.
        Error.throwWithStackTrace(e, st);
      }
    });
  }

  /// Signs out, revoking the session server-side so the refresh token cannot be
  /// replayed. The revoke is best-effort and never blocks the local sign-out —
  /// a network failure must not trap the user in a signed-in state.
  Future<void> logout() async {
    final storage = ref.read(tokenStorageProvider);
    final refreshToken = await storage.readRefreshToken();

    if (refreshToken != null && refreshToken.isNotEmpty) {
      await ref.read(authRepositoryProvider).logout(refreshToken);
    }

    await storage.clear();
    state = const AsyncData(null);
  }

  /// Called by the Dio interceptor when a request is unauthorized and the
  /// session could not be renewed. Skips the server-side revoke: the refresh
  /// token is already dead, so the call would be pointless.
  Future<void> forceLogout() async {
    await ref.read(tokenStorageProvider).clear();
    state = const AsyncData(null);
  }

  /// Re-reads the session from storage after the interceptor rotated the
  /// tokens, so roles and permissions held in memory stay current.
  Future<void> syncFromStorage() async {
    final session = await ref.read(tokenStorageProvider).readSession();
    if (session != null) state = AsyncData(session);
  }
}

final authControllerProvider =
    AsyncNotifierProvider<AuthController, Session?>(AuthController.new);
