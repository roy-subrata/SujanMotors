import 'package:intl/intl.dart';

/// Formats a money amount with the API's currency (defaults to BDT / Taka).
String formatCurrency(double amount, {String? currency}) {
  final code = (currency == null || currency.isEmpty) ? 'BDT' : currency;
  final symbol = _symbols[code] ?? '$code ';
  final formatter = NumberFormat.currency(symbol: symbol, decimalDigits: 2);
  return formatter.format(amount);
}

const _symbols = <String, String>{
  'BDT': '৳ ', // ৳
  'USD': '\$',
  'EUR': '€',
  'GBP': '£',
};

/// Short calendar date, e.g. "12 May 2026".
String formatDate(DateTime date) => DateFormat('d MMM yyyy').format(date);

/// Time of day, e.g. "2:05 PM".
String formatTime(DateTime time) => DateFormat.jm().format(time);

/// Day with weekday, e.g. "Thu, 12 Jun 2026".
String formatDayLong(DateTime date) => DateFormat('EEE, d MMM yyyy').format(date);

/// Compact "time ago" label, e.g. "just now", "5m ago", "2h ago", "3d ago".
String formatRelative(DateTime time) {
  final diff = DateTime.now().difference(time);
  if (diff.inSeconds < 45) return 'just now';
  if (diff.inMinutes < 60) return '${diff.inMinutes}m ago';
  if (diff.inHours < 24) return '${diff.inHours}h ago';
  if (diff.inDays < 7) return '${diff.inDays}d ago';
  return DateFormat.MMMd().format(time);
}
