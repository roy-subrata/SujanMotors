/// App-wide configuration resolved at build time.
///
/// Override the API base URL when running/building, e.g.:
///   flutter run --dart-define=API_BASE_URL=http://10.0.2.2:5001
///
/// Defaults to the Android emulator loopback host (10.0.2.2) which maps to the
/// developer machine's localhost where the .NET API listens on port 5001.
class ApiConfig {
  const ApiConfig._();

  /// Scheme + host + port of the API, without the `/api/v1` prefix.
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'https://sujanmotors-api-gtetffcscjg3cyfe.southeastasia-01.azurewebsites.net'//'http://10.0.2.2:5001',
  );

  /// Full base used by the Dio client. All endpoints live under `/api/v1`.
  static const String apiBaseUrl = '$baseUrl/api/v1';
}
