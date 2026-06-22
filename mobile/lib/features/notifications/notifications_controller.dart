import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:signalr_netcore/signalr_client.dart';

import '../../core/config/api_config.dart';
import '../../core/notifications/local_notifications.dart';
import '../../core/storage/token_storage.dart';
import '../auth/auth_controller.dart';
import 'notification_models.dart';

/// Connection state of the realtime notification channel, surfaced to the UI so
/// the bell / inbox can show a live vs reconnecting indicator.
enum HubStatus { disconnected, connecting, connected }

class NotificationsState {
  const NotificationsState({
    this.items = const [],
    this.status = HubStatus.disconnected,
  });

  final List<AppNotification> items;
  final HubStatus status;

  int get unreadCount => items.where((n) => !n.read).length;

  NotificationsState copyWith({
    List<AppNotification>? items,
    HubStatus? status,
  }) =>
      NotificationsState(
        items: items ?? this.items,
        status: status ?? this.status,
      );
}

/// Owns the SignalR connection to `/hubs/sale-notifications` and the in-app
/// notification inbox. The connection follows the auth session: it starts when
/// a staff member is logged in and tears down on logout.
class NotificationsController extends Notifier<NotificationsState> {
  HubConnection? _connection;

  @override
  NotificationsState build() {
    ref.onDispose(_disconnect);

    // React to later login/logout transitions. (No fireImmediately: the
    // callbacks touch `state`, which isn't initialized until build returns.)
    ref.listen(authControllerProvider, (_, next) {
      final token = next.asData?.value?.token;
      if (token != null && token.isNotEmpty) {
        _connect(token);
      } else {
        _disconnect();
      }
    });

    // If a session already exists when this provider is first read (the common
    // case — the bell mounts after login), connect once the initial state is in.
    final token = ref.read(authControllerProvider).asData?.value?.token;
    if (token != null && token.isNotEmpty) {
      Future.microtask(() => _connect(token));
    }

    return const NotificationsState();
  }

  Future<void> _connect(String token) async {
    if (_connection != null) return; // already connected/connecting
    state = state.copyWith(status: HubStatus.connecting);

    final connection = HubConnectionBuilder()
        .withUrl(
          '${ApiConfig.baseUrl}/hubs/sale-notifications',
          options: HttpConnectionOptions(
            // SignalR appends this as `?access_token=` for the WS handshake,
            // matching the API's JwtBearer query-string token reader.
            accessTokenFactory: () async =>
                await ref.read(tokenStorageProvider).readToken() ?? token,
          ),
        )
        .withAutomaticReconnect()
        .build();

    connection.on('ReceiveSaleNotification', _onSaleNotification);
    connection.onclose(({error}) {
      state = state.copyWith(status: HubStatus.disconnected);
    });
    connection.onreconnecting(({error}) {
      state = state.copyWith(status: HubStatus.connecting);
    });
    connection.onreconnected(({connectionId}) {
      state = state.copyWith(status: HubStatus.connected);
    });

    _connection = connection;
    try {
      await connection.start();
      state = state.copyWith(status: HubStatus.connected);
    } catch (_) {
      // Leave the inbox usable even if realtime is unavailable.
      state = state.copyWith(status: HubStatus.disconnected);
    }
  }

  void _onSaleNotification(List<Object?>? args) {
    if (args == null || args.isEmpty) return;
    final payload = args.first;
    if (payload is! Map) return;
    final sale =
        SaleNotification.fromJson(Map<String, dynamic>.from(payload));
    state = state.copyWith(
      items: [AppNotification(sale: sale), ...state.items],
    );

    // Surface it as a system notification with sound (fire-and-forget).
    final who = sale.customerName.isEmpty ? 'Walk-in' : sale.customerName;
    LocalNotifications.instance.show(
      title: 'New sale · ${sale.soNumber}',
      body: '$who · ${sale.currency} ${sale.grandTotal.toStringAsFixed(2)}',
    );
  }

  Future<void> _disconnect() async {
    final conn = _connection;
    _connection = null;
    if (conn != null) {
      try {
        await conn.stop();
      } catch (_) {/* ignore */}
    }
    state = const NotificationsState();
  }

  void markAllRead() {
    state = state.copyWith(
      items: [for (final n in state.items) n.copyWith(read: true)],
    );
  }

  /// Marks a specific notification read by identity — safe even if new
  /// notifications have arrived (and shifted indices) since it was rendered.
  void markRead(AppNotification notification) {
    final i = state.items.indexOf(notification);
    if (i < 0 || state.items[i].read) return;
    final updated = [...state.items];
    updated[i] = updated[i].copyWith(read: true);
    state = state.copyWith(items: updated);
  }

  void clearAll() => state = state.copyWith(items: const []);
}

final notificationsControllerProvider =
    NotifierProvider<NotificationsController, NotificationsState>(
        NotificationsController.new);
