import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:intl/intl.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/cashbook.dart';

class CashBookRepository {
  CashBookRepository(this._dio);

  final Dio _dio;

  /// Daily cash book for [date] (local calendar day). Sends the device timezone
  /// offset so the server windows "today" to the user's local date.
  Future<CashBookDay> daily(DateTime date) async {
    try {
      final res = await _dio.get('/cash-book/daily', queryParameters: {
        'date': DateFormat('yyyy-MM-dd').format(date),
        'tzOffsetMinutes': DateTime.now().timeZoneOffset.inMinutes,
      });
      final body = res.data as Map<String, dynamic>;
      final data = Map<String, dynamic>.from(body['data'] as Map);
      return CashBookDay.fromJson(data);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final cashBookRepositoryProvider = Provider<CashBookRepository>(
  (ref) => CashBookRepository(ref.read(dioProvider)),
);

/// Cash book for a given day, keyed by the `yyyy-MM-dd` date string.
final cashBookDayProvider =
    FutureProvider.family<CashBookDay, String>((ref, dateKey) {
  return ref.read(cashBookRepositoryProvider).daily(DateTime.parse(dateKey));
});
