import 'package:intl/intl.dart';

/// Formats a money amount with the API's currency (defaults to BDT / Taka).
String formatCurrency(double amount, {String? currency}) {
  final code = (currency == null || currency.isEmpty) ? 'BDT' : currency;
  final symbol = _symbols[code] ?? '$code ';
  final formatter = NumberFormat.currency(symbol: symbol, decimalDigits: 2);
  return formatter.format(amount);
}

/// Default (BDT) currency symbol — single source of truth. Use
/// [kCurrencyPrefix] for amount-input `prefixText` / inline amount prefixes.
const kCurrencySymbol = '৳';
const kCurrencyPrefix = '$kCurrencySymbol ';

const _symbols = <String, String>{
  'BDT': kCurrencyPrefix,
  'USD': '\$',
  'EUR': '€',
  'GBP': '£',
};

/// Like [formatCurrency] but without decimals, e.g. "৳ 5,000" — for
/// quick-amount chips and other compact labels.
String formatCurrencyWhole(double amount, {String? currency}) {
  final code = (currency == null || currency.isEmpty) ? 'BDT' : currency;
  final symbol = _symbols[code] ?? '$code ';
  final formatter = NumberFormat.currency(symbol: symbol, decimalDigits: 0);
  return formatter.format(amount);
}

/// Short calendar date, e.g. "12 May 2026".
String formatDate(DateTime date) => DateFormat('d MMM yyyy').format(date);

/// Time of day, e.g. "2:05 PM".
String formatTime(DateTime time) => DateFormat.jm().format(time);

/// Day with weekday, e.g. "Thu, 12 Jun 2026".
String formatDayLong(DateTime date) => DateFormat('EEE, d MMM yyyy').format(date);

/// Compact "time ago" label, e.g. "just now", "5m ago", "2h ago", "3d ago".
///
/// Pass an [S] instance for localized output, or leave null for English.
String formatRelative(DateTime time, {dynamic s}) {
  final diff = DateTime.now().difference(time);
  if (diff.inSeconds < 45) return s?.justNow ?? 'just now';
  if (diff.inMinutes < 60) {
    final n = diff.inMinutes;
    return s != null ? s.minutesAgo(n) : '${n}m ago';
  }
  if (diff.inHours < 24) {
    final n = diff.inHours;
    return s != null ? s.hoursAgo(n) : '${n}h ago';
  }
  if (diff.inDays < 7) {
    final n = diff.inDays;
    return s != null ? s.daysAgo(n) : '${n}d ago';
  }
  return DateFormat.MMMd().format(time);
}
