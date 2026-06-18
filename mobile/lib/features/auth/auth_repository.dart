import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import 'session.dart';

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
}

final authRepositoryProvider = Provider<AuthRepository>(
  (ref) => AuthRepository(ref.read(dioProvider)),
);
