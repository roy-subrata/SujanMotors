import 'package:flutter/foundation.dart';
import 'package:flutter/widgets.dart';

/// App strings for the supported languages (English / Bengali).
///
/// Usage: `S.of(context).products`. New screens add a getter here with both
/// translations — screens not yet migrated simply keep their English literals
/// until they are.
class S {
  const S(this.locale);

  final Locale locale;

  static S of(BuildContext context) =>
      Localizations.of<S>(context, S) ?? const S(Locale('en'));

  static const delegate = _SDelegate();

  static const supportedLocales = [Locale('en'), Locale('bn')];

  bool get _bn => locale.languageCode == 'bn';

  String _t(String en, String bn) => _bn ? bn : en;

  // ── Navigation chrome ───────────────────────────────────────────────────
  String get home => _t('Home', 'হোম');
  String get dashboard => _t('Dashboard', 'ড্যাশবোর্ড');
  String get products => _t('Products', 'পণ্য');
  String get customers => _t('Customers', 'গ্রাহক');
  String get sales => _t('Sales', 'বিক্রয়');
  String get suppliers => _t('Suppliers', 'সরবরাহকারী');
  String get cashBook => _t('Cash Book', 'ক্যাশ বুক');
  String get tillSession => _t('Till Session', 'টিল সেশন');
  String get stockIn => _t('Stock In', 'স্টক ইন');
  String get newStockIn => _t('New Stock In', 'নতুন স্টক ইন');
  String get notifications => _t('Notifications', 'নোটিফিকেশন');
  String get logOut => _t('Log out', 'লগ আউট');
  String get language => _t('Language', 'ভাষা');

  // ── Common actions ──────────────────────────────────────────────────────
  String get cart => _t('Cart', 'কার্ট');
  String get scanBarcode => _t('Scan barcode', 'বারকোড স্ক্যান');
  String get search => _t('Search', 'খুঁজুন');
  String get switchToLight => _t('Switch to light', 'লাইট মোডে যান');
  String get switchToDark => _t('Switch to dark', 'ডার্ক মোডে যান');
}

class _SDelegate extends LocalizationsDelegate<S> {
  const _SDelegate();

  @override
  bool isSupported(Locale locale) =>
      S.supportedLocales.any((l) => l.languageCode == locale.languageCode);

  @override
  Future<S> load(Locale locale) => SynchronousFuture(S(locale));

  @override
  bool shouldReload(_SDelegate old) => false;
}
