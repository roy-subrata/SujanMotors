import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/till_session.dart';

class TillSessionRepository {
  TillSessionRepository(this._dio);

  final Dio _dio;

  /// Opens a till session for the current user (`POST /till-sessions/open`).
  /// 400s if the caller already has an open session — the server's message is
  /// surfaced via [AppException].
  Future<TillSession> open({
    required String terminalLabel,
    required double openingFloat,
    String? shiftLabel,
    required String notes,
  }) async {
    try {
      final res = await _dio.post('/till-sessions/open', data: {
        'terminalLabel': terminalLabel,
        'openingFloat': openingFloat,
        'shiftLabel': ?shiftLabel,
        'notes': notes,
      });
      return TillSession.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// The calling user's own open session, or null if none
  /// (`GET /till-sessions/current`, 200 OK with a null body when closed/none).
  Future<TillSession?> getCurrent() async {
    try {
      final res = await _dio.get('/till-sessions/current');
      final data = res.data;
      if (data is! Map) return null;
      return TillSession.fromJson(Map<String, dynamic>.from(data));
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Records a cash drop against an OPEN session
  /// (`POST /till-sessions/{id}/cash-drops`). Returns the updated session.
  Future<TillSession> recordCashDrop({
    required String sessionId,
    required double amount,
    required String notes,
  }) async {
    try {
      final res = await _dio.post('/till-sessions/$sessionId/cash-drops',
          data: {
            'amount': amount,
            'notes': notes,
          });
      return TillSession.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Closes the session and freezes reconciliation
  /// (`POST /till-sessions/{id}/close`). Returns the updated session with
  /// `overShortAmount` populated.
  Future<TillSession> close({
    required String sessionId,
    required double countedAmount,
    required String notes,
  }) async {
    try {
      final res = await _dio.post('/till-sessions/$sessionId/close', data: {
        'countedAmount': countedAmount,
        'notes': notes,
      });
      return TillSession.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Downloads the Shift Report PDF for a CLOSED session
  /// (`GET /till-sessions/{id}/pdf`) and returns the raw bytes.
  Future<Uint8List> downloadPdf(String sessionId) async {
    try {
      final res = await _dio.get<List<int>>(
        '/till-sessions/$sessionId/pdf',
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(res.data ?? []);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final tillSessionRepositoryProvider = Provider<TillSessionRepository>(
  (ref) => TillSessionRepository(ref.read(dioProvider)),
);

/// The current user's open till session, or null if they have none.
final currentTillSessionProvider = FutureProvider<TillSession?>((ref) {
  return ref.read(tillSessionRepositoryProvider).getCurrent();
});
