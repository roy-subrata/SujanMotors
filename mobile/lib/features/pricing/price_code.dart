import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/dio_provider.dart';
import '../../shared/format.dart';

/// Cost-price obfuscation mirroring the web app: a 10-letter "magic word" maps
/// digits to letters (1→[0], 2→[1] … 9→[8], 0→[9]); an optional prefix/suffix
/// wraps the result. Cost prices render as letters until the user reveals them.
///
/// This is obfuscation for over-the-shoulder privacy, not encryption — the same
/// magic word the web uses is fetched here so the codes match across clients.
class PriceCode {
  const PriceCode({this.magicWord = '', this.prefix = '', this.suffix = ''});

  final String magicWord;
  final String prefix;
  final String suffix;

  /// Valid only with exactly 10 unique letters (matches the web validation).
  bool get isConfigured {
    final w = magicWord.toUpperCase();
    return w.length == 10 &&
        RegExp(r'^[A-Z]+$').hasMatch(w) &&
        w.split('').toSet().length == 10;
  }

  /// Encodes a numeric price into magic-word letters (decimal point preserved).
  String encode(num price) {
    if (!isConfigured) return '***';
    final word = magicWord.toUpperCase();
    final buf = StringBuffer();
    for (final ch in price.toString().split('')) {
      if (ch == '.') {
        buf.write('.');
        continue;
      }
      final d = int.tryParse(ch);
      buf.write(d == null ? ch : word[d == 0 ? 9 : d - 1]);
    }
    return '$prefix$buf$suffix';
  }
}

/// Loads the magic word + optional prefix/suffix once. Tolerant: any missing
/// setting (404) leaves it unconfigured, so masking simply stays off.
final priceCodeProvider = FutureProvider<PriceCode>((ref) async {
  final dio = ref.read(dioProvider);

  Future<String> read(String key) async {
    try {
      final res = await dio.get('/applicationsettings/$key');
      final value = (res.data as Map<String, dynamic>)['value'];
      return value?.toString() ?? '';
    } catch (_) {
      return '';
    }
  }

  return PriceCode(
    magicWord: await read('PRICE_CODE_WORD'),
    prefix: await read('PRICE_CODE_PREFIX'),
    suffix: await read('PRICE_CODE_SUFFIX'),
  );
});

/// Whether actual numeric cost prices are revealed (preview). Default: masked.
class ShowActualPriceController extends Notifier<bool> {
  @override
  bool build() => false;

  void toggle() => state = !state;
}

final showActualPriceProvider =
    NotifierProvider<ShowActualPriceController, bool>(
        ShowActualPriceController.new);

/// Formats a cost price: the masked code when a magic word is configured and the
/// user hasn't revealed prices; otherwise the normal currency amount.
String formatCostMasked(
  PriceCode? code,
  bool showActual,
  double price, {
  String? currency,
}) {
  if (code != null && code.isConfigured && !showActual) {
    return code.encode(price);
  }
  return formatCurrency(price, currency: currency);
}
