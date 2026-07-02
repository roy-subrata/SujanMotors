import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/auth_controller.dart';
import '../config/api_config.dart';
import '../storage/token_storage.dart';

/// Shared Dio client. Attaches the bearer token on every request and triggers a
/// logout when the API responds 401 (mirrors the web app's auth interceptor).
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
    onError: (error, handler) {
      final isLoginRequest = error.requestOptions.path.contains('/auth/login');
      if (error.response?.statusCode == 401 && !isLoginRequest) {
        // Fire-and-forget: clear session so the router redirects to /login.
        // Excludes the login endpoint itself — a bad-password 401 there must
        // surface as a login error, not blow away the in-progress attempt.
        ref.read(authControllerProvider.notifier).forceLogout();
      }
      handler.next(error);
    },
  ));

  return dio;
});
