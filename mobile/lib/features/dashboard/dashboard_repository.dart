import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/dashboard.dart';

class DashboardRepository {
  DashboardRepository(this._dio);

  final Dio _dio;

  Future<DashboardData> getFullDashboard({
    required DateTime startDate,
    required DateTime endDate,
    required String period,
  }) async {
    try {
      final res = await _dio.post('/dashboard/financial-summary', data: {
        'startDate': startDate.toIso8601String(),
        'endDate': endDate.toIso8601String(),
        'period': period,
      });
      return DashboardData.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    } catch (e) {
      throw AppException('Failed to load dashboard data.');
    }
  }
}

final dashboardRepositoryProvider = Provider<DashboardRepository>(
  (ref) => DashboardRepository(ref.read(dioProvider)),
);
