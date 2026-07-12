import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../shared/models/dashboard.dart';
import 'dashboard_repository.dart';

class DashboardState {
  const DashboardState({
    this.data,
    this.isLoading = false,
    this.error,
    required this.rangeStart,
    required this.rangeEnd,
  });

  final DashboardData? data;
  final bool isLoading;
  final String? error;

  /// Inclusive date range the dashboard is reporting on. Defaults to today.
  final DateTime rangeStart;
  final DateTime rangeEnd;

  DashboardState copyWith({
    DashboardData? data,
    bool? isLoading,
    String? error,
    DateTime? rangeStart,
    DateTime? rangeEnd,
    bool clearError = false,
  }) =>
      DashboardState(
        data: data ?? this.data,
        isLoading: isLoading ?? this.isLoading,
        error: clearError ? null : error ?? this.error,
        rangeStart: rangeStart ?? this.rangeStart,
        rangeEnd: rangeEnd ?? this.rangeEnd,
      );
}

class DashboardController extends Notifier<DashboardState> {
  @override
  DashboardState build() {
    final today = DateTime.now();
    final start = DateTime(today.year, today.month, today.day);
    Future.microtask(() => load());
    return DashboardState(isLoading: true, rangeStart: start, rangeEnd: start);
  }

  /// Reloads the dashboard, optionally over a new [start]..[end] range.
  /// Omit both to refresh the currently selected range.
  Future<void> load({DateTime? start, DateTime? end}) async {
    final rangeStart = start ?? state.rangeStart;
    final rangeEnd = end ?? state.rangeEnd;
    state = state.copyWith(
      isLoading: true,
      rangeStart: rangeStart,
      rangeEnd: rangeEnd,
      clearError: true,
    );

    try {
      final isSingleDay = rangeStart.year == rangeEnd.year &&
          rangeStart.month == rangeEnd.month &&
          rangeStart.day == rangeEnd.day;

      final data = await ref.read(dashboardRepositoryProvider).getFullDashboard(
            startDate: rangeStart,
            endDate: rangeEnd,
            period: isSingleDay ? 'DAILY' : 'CUSTOM',
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
