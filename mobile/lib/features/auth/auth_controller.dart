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

  Future<void> logout() async {
    await ref.read(tokenStorageProvider).clear();
    state = const AsyncData(null);
  }

  /// Called by the Dio interceptor when the API returns 401.
  Future<void> forceLogout() => logout();
}

final authControllerProvider =
    AsyncNotifierProvider<AuthController, Session?>(AuthController.new);
