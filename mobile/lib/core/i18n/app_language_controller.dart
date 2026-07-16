import 'dart:ui';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

/// Persisted app language: English (en) or Bengali (bn). Same storage pattern
/// as [ThemeModeController].
class AppLanguageController extends Notifier<Locale> {
  static const _key = 'app_language';
  static const _storage = FlutterSecureStorage();

  static const english = Locale('en');
  static const bengali = Locale('bn');

  @override
  Locale build() {
    Future.microtask(_load);
    return english;
  }

  Future<void> _load() async {
    final raw = await _storage.read(key: _key);
    if (raw == 'bn') state = bengali;
  }

  Future<void> setLocale(Locale locale) async {
    state = locale;
    await _storage.write(key: _key, value: locale.languageCode);
  }
}

final appLanguageProvider =
    NotifierProvider<AppLanguageController, Locale>(AppLanguageController.new);
