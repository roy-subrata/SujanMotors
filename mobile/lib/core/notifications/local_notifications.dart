import 'package:flutter_local_notifications/flutter_local_notifications.dart';

/// Wraps `flutter_local_notifications` to surface incoming realtime events as
/// system notifications with sound. Initialized once at app start; events
/// arrive over the SignalR sale-notifications hub while the app is running.
class LocalNotifications {
  LocalNotifications._();
  static final LocalNotifications instance = LocalNotifications._();

  final FlutterLocalNotificationsPlugin _plugin =
      FlutterLocalNotificationsPlugin();

  // Android 8+ requires a channel (with sound enabled) for the sound to play.
  static const AndroidNotificationChannel _salesChannel =
      AndroidNotificationChannel(
    'sales',
    'Sales',
    description: 'Plays a sound when a new sale is recorded.',
    importance: Importance.high,
  );

  bool _initialized = false;
  int _nextId = 0;

  Future<void> init() async {
    if (_initialized) return;

    const androidInit = AndroidInitializationSettings('@mipmap/ic_launcher');
    const darwinInit = DarwinInitializationSettings(
      requestAlertPermission: true,
      requestBadgePermission: true,
      requestSoundPermission: true,
    );

    await _plugin.initialize(
      settings:
          const InitializationSettings(android: androidInit, iOS: darwinInit),
    );

    final android = _plugin.resolvePlatformSpecificImplementation<
        AndroidFlutterLocalNotificationsPlugin>();
    await android?.createNotificationChannel(_salesChannel);
    // Android 13+: runtime POST_NOTIFICATIONS permission.
    await android?.requestNotificationsPermission();

    _initialized = true;
  }

  /// Shows a heads-up notification with sound. No-ops gracefully if init failed.
  Future<void> show({required String title, required String body}) async {
    if (!_initialized) await init();

    const details = NotificationDetails(
      android: AndroidNotificationDetails(
        'sales',
        'Sales',
        channelDescription: 'Plays a sound when a new sale is recorded.',
        importance: Importance.high,
        priority: Priority.high,
        ticker: 'New sale',
      ),
      iOS: DarwinNotificationDetails(presentSound: true),
    );

    await _plugin.show(
      id: _nextId++,
      title: title,
      body: body,
      notificationDetails: details,
    );
  }
}
