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

  /// Every distinct terminal label ever used, most-recently-used first
  /// (`GET /till-sessions/terminal-labels`) — powers a suggest-as-you-type on
  /// the Terminal field so cashiers converge on consistent naming.
  Future<List<String>> getTerminalLabels() async {
    try {
      final res = await _dio.get('/till-sessions/terminal-labels');
      return (res.data as List).map((e) => e.toString()).toList();
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

  /// Whether the current user's role requires an open till session before
  /// starting a sale, and whether they currently have one
  /// (`GET /till-sessions/requires-open-session`).
  Future<TillSessionRequirement> checkRequiresOpenSession() async {
    try {
      final res = await _dio.get('/till-sessions/requires-open-session');
      return TillSessionRequirement.fromJson(
          res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Suggested Open Till defaults (`GET /till-sessions/suggested-opening-float`).
  /// Opening float is scoped by [terminalLabel] (pass null/empty before the
  /// cashier has typed one yet — you'll just get no float suggestion back);
  /// shift label is always resolved from the current cashier regardless.
  Future<SuggestedOpeningFloat> getSuggestedOpeningFloat(
      {String? terminalLabel}) async {
    try {
      final res = await _dio.get('/till-sessions/suggested-opening-float',
          queryParameters: (terminalLabel != null && terminalLabel.isNotEmpty)
              ? {'terminalLabel': terminalLabel}
              : null);
      return SuggestedOpeningFloat.fromJson(res.data as Map<String, dynamic>);
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

/// Whether the current user must have an open till session before starting a
/// sale, and whether they currently have one. See [TillSessionRequirement].
final tillSessionRequirementProvider =
    FutureProvider<TillSessionRequirement>((ref) {
  return ref.read(tillSessionRepositoryProvider).checkRequiresOpenSession();
});
