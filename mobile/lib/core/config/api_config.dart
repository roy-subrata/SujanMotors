/// App-wide configuration resolved at build time.
///
/// Override the API base URL when running/building, e.g.:
///   flutter run --dart-define-from-file=config/dev.json
///
/// Per-flavor config files live in mobile/config/:
///   dev.json   → Android emulator loopback (10.0.2.2:5001)
///   test.json  → staging Azure App Service
///   prod.json  → production Azure App Service
class ApiConfig {
  const ApiConfig._();

  /// Scheme + host + port of the API, without the `/api/v1` prefix.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5001',
  );

  /// Full base used by the Dio client. All endpoints live under `/api/v1`.
  static const String apiBaseUrl = '$baseUrl/api/v1';
}
