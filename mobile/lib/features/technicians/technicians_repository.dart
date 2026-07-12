import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../core/network/permission_guard.dart';
import '../../shared/models/technician.dart';

class TechniciansRepository {
  TechniciansRepository(this._dio, this._ref);

  final Dio _dio;
  final Ref _ref;

  Future<List<Technician>> list() async {
    await requirePermission(_ref, 'sales.view');
    try {
      final res = await _dio.get('/Technician');
      final data = res.data;
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) => Technician.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<Technician> create({
    required String name,
    required String phone,
    String? email,
    String? shopName,
  }) async {
    await requirePermission(_ref, 'sales.create');
    try {
      final res = await _dio.post('/Technician', data: {
        'name': name,
        'phone': phone,
        if (email != null && email.isNotEmpty) 'email': email,
        if (shopName != null && shopName.isNotEmpty) 'shopName': shopName,
      });
      return Technician.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final techniciansRepositoryProvider = Provider<TechniciansRepository>(
  (ref) => TechniciansRepository(ref.read(dioProvider), ref),
);

final techniciansListProvider =
    FutureProvider<List<Technician>>((ref) async {
  return ref.read(techniciansRepositoryProvider).list();
});
