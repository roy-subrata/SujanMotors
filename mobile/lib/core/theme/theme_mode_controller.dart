import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';

class ThemeModeController extends Notifier<ThemeMode> {
  static const _key = 'app_theme_mode';
  static const _storage = FlutterSecureStorage();

  @override
  ThemeMode build() {
    Future.microtask(_load);
    return ThemeMode.system;
  }

  Future<void> _load() async {
    final raw = await _storage.read(key: _key);
    if (!_isValidRaw(raw)) return;
    state = _fromRaw(raw!);
  }

  Future<void> setMode(ThemeMode mode) async {
    state = mode;
    await _storage.write(key: _key, value: _toRaw(mode));
  }

  static bool _isValidRaw(String? raw) =>
      raw == 'light' || raw == 'dark' || raw == 'system';

  static ThemeMode _fromRaw(String raw) => switch (raw) {
        'light' => ThemeMode.light,
        'dark' => ThemeMode.dark,
        _ => ThemeMode.system,
      };

  static String _toRaw(ThemeMode mode) => switch (mode) {
        ThemeMode.light => 'light',
        ThemeMode.dark => 'dark',
        ThemeMode.system => 'system',
      };
}

final themeModeProvider =
    NotifierProvider<ThemeModeController, ThemeMode>(ThemeModeController.new);
