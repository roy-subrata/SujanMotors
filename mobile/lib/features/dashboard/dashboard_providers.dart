import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/dashboard.dart';
import 'dashboard_repository.dart';

enum DashboardPeriod { today, month, year }

class DashboardState {
  const DashboardState({
    this.data,
    this.isLoading = false,
    this.error,
    this.period = DashboardPeriod.today,
  });

  final DashboardData? data;
  final bool isLoading;
  final String? error;
  final DashboardPeriod period;

  DashboardState copyWith({
    DashboardData? data,
    bool? isLoading,
    String? error,
    DashboardPeriod? period,
    bool clearError = false,
  }) =>
      DashboardState(
        data: data ?? this.data,
        isLoading: isLoading ?? this.isLoading,
        error: clearError ? null : error ?? this.error,
        period: period ?? this.period,
      );
}

class DashboardController extends Notifier<DashboardState> {
  @override
  DashboardState build() {
    Future.microtask(() => load());
    return const DashboardState(isLoading: true);
  }

  Future<void> load([DashboardPeriod? p]) async {
    final period = p ?? state.period;
    state = state.copyWith(isLoading: true, period: period, clearError: true);

    try {
      final now = DateTime.now();
      final (start, end, periodStr) = switch (period) {
        DashboardPeriod.today => (
            DateTime(now.year, now.month, now.day),
            DateTime(now.year, now.month, now.day),
            'DAILY',
          ),
        DashboardPeriod.month => (
            DateTime(now.year, now.month, 1),
            DateTime(now.year, now.month + 1, 0),
            'MONTHLY',
          ),
        DashboardPeriod.year => (
            DateTime(now.year, 1, 1),
            DateTime(now.year, 12, 31),
            'YEARLY',
          ),
      };

      final data = await ref.read(dashboardRepositoryProvider).getFullDashboard(
            startDate: start,
            endDate: end,
            period: periodStr,
          );
      state = state.copyWith(data: data, isLoading: false);
    } on AppException catch (e) {
      state = state.copyWith(isLoading: false, error: e.message);
    }
  }
}

final dashboardControllerProvider =
    NotifierProvider<DashboardController, DashboardState>(
  DashboardController.new,
);
