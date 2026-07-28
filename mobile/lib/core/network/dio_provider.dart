import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/auth_controller.dart';
import '../../features/auth/auth_repository.dart';
import '../config/api_config.dart';
import '../storage/token_storage.dart';
import 'token_refresher.dart';

/// Marks a request that has already been replayed after a token refresh, so a
/// second 401 ends the session instead of looping.
const _retriedFlag = '__auth_retried';

/// Shared Dio client. Attaches the bearer token on every request; on 401 it
/// renews the access token once and replays the request, falling back to logout
/// when the session can no longer be renewed.
///
/// The anonymous `/auth` endpoints deliberately do NOT go through this client —
/// see [authDioProvider] in auth_repository.dart — so a failing refresh can
/// never re-enter this interceptor.
final dioProvider = Provider<Dio>((ref) {
  final dio = Dio(BaseOptions(
    baseUrl: ApiConfig.apiBaseUrl,
    connectTimeout: const Duration(seconds: 15),
    receiveTimeout: const Duration(seconds: 20),
    contentType: Headers.jsonContentType,
  ));

  final storage = ref.read(tokenStorageProvider);

  dio.interceptors.add(InterceptorsWrapper(
    onRequest: (options, handler) async {
      final token = await storage.readToken();
      if (token != null && token.isNotEmpty) {
        options.headers['Authorization'] = 'Bearer $token';
      }
      handler.next(options);
    },
    onError: (error, handler) async {
      if (error.response?.statusCode != 401) {
        return handler.next(error);
      }

      final options = error.requestOptions;

      // Already replayed once — the new token was rejected too, so this is a
      // real authorization failure rather than an expired token.
      if (options.extra[_retriedFlag] == true) {
        await ref.read(authControllerProvider.notifier).forceLogout();
        return handler.next(error);
      }

      final outcome = await ref.read(tokenRefresherProvider).refresh();

      if (outcome == RefreshOutcome.throttled) {
        // Rate limited or the server was unreachable. The session is probably
        // still valid, so keep the user signed in and surface the failure; the
        // next request will try again.
        return handler.next(error);
      }

      if (outcome == RefreshOutcome.expired) {
        // Session is over; clearing it makes the router redirect to /login.
        await ref.read(authControllerProvider.notifier).forceLogout();
        return handler.next(error);
      }

      // Rotation may have returned updated roles/permissions; refresh the
      // in-memory session so UI permission checks see them.
      await ref.read(authControllerProvider.notifier).syncFromStorage();

      try {
        final token = await storage.readToken();
        options.extra[_retriedFlag] = true;
        options.headers['Authorization'] = 'Bearer $token';

        // A FormData body (image upload) can only be finalised once — its file
        // stream is consumed by the failed attempt, so the replay needs a fresh
        // copy. clone() re-reads from the underlying file.
        final body = options.data;
        if (body is FormData) {
          options.data = body.clone();
        }

        return handler.resolve(await dio.fetch(options));
      } on DioException catch (e) {
        return handler.next(e);
      } catch (_) {
        return handler.next(error);
      }
    },
  ));

  return dio;
});
