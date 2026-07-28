import 'package:dio/dio.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:autopartshop_mobile/core/network/token_refresher.dart';
import 'package:autopartshop_mobile/core/storage/token_storage.dart';
import 'package:autopartshop_mobile/features/auth/auth_repository.dart';
import 'package:autopartshop_mobile/features/auth/session.dart';

/// In-memory [TokenStorage]; every method is overridden so the platform
/// secure-storage channel is never touched.
class _FakeStorage extends TokenStorage {
  _FakeStorage({this.session}) : super(const FlutterSecureStorage());

  Session? session;
  int saveCount = 0;

  @override
  Future<String?> readToken() async => session?.token;

  @override
  Future<String?> readRefreshToken() async => session?.refreshToken;

  @override
  Future<Session?> readSession() async => session;

  @override
  Future<void> saveSession(Session session) async {
    this.session = session;
    saveCount++;
  }

  @override
  Future<void> clear() async => session = null;
}

class _FakeAuthRepository extends AuthRepository {
  _FakeAuthRepository({this.result, this.delay = Duration.zero})
      : super(Dio());

  RefreshAttempt? result;
  Duration delay;
  int refreshCalls = 0;
  final List<String> presentedTokens = [];

  @override
  Future<RefreshAttempt> refresh(String refreshToken) async {
    refreshCalls++;
    presentedTokens.add(refreshToken);
    if (delay > Duration.zero) await Future<void>.delayed(delay);
    return result ?? const RefreshAttempt(RefreshOutcome.expired);
  }
}

RefreshAttempt _renewed(String token, String refreshToken,
        {List<String> roles = const [], List<String> permissions = const []}) =>
    RefreshAttempt(
      RefreshOutcome.renewed,
      RefreshedTokens(
        token: token,
        refreshToken: refreshToken,
        roles: roles,
        permissions: permissions,
      ),
    );

Session _session({String refreshToken = 'rt-1'}) => Session(
      token: 'access-1',
      username: 'admin',
      refreshToken: refreshToken,
      roles: const ['Admin'],
      permissions: const ['sales.view'],
    );

void main() {
  group('TokenRefresher single-flight', () {
    test('concurrent refreshes share one rotation', () async {
      // Refresh tokens are single-use: a second concurrent rotation would
      // present the same spent token and trip server-side reuse detection,
      // revoking the whole session.
      final repo = _FakeAuthRepository(
        result: _renewed('access-2', 'rt-2'),
        delay: const Duration(milliseconds: 50),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      final results = await Future.wait([
        refresher.refresh(),
        refresher.refresh(),
        refresher.refresh(),
      ]);

      expect(results, everyElement(RefreshOutcome.renewed));
      expect(repo.refreshCalls, 1, reason: 'three callers, one rotation');
    });

    test('a later refresh starts a new rotation', () async {
      final repo = _FakeAuthRepository(
        result: _renewed('access-2', 'rt-2'),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.renewed);
      expect(await refresher.refresh(), RefreshOutcome.renewed);
      expect(repo.refreshCalls, 2, reason: 'in-flight must clear on completion');
    });

    test('persists the rotated pair', () async {
      final repo = _FakeAuthRepository(
        result: _renewed('access-2', 'rt-2',
            roles: const ['Admin', 'Manager'],
            permissions: const ['sales.view', 'sales.edit']),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.renewed);

      final saved = await storage.readSession();
      expect(saved!.token, 'access-2');
      expect(saved.refreshToken, 'rt-2',
          reason: 'the spent token must be replaced');
      expect(saved.username, 'admin', reason: 'identity fields are preserved');
      expect(saved.roles, ['Admin', 'Manager'],
          reason: 'role changes apply without a re-login');
    });

    test('rejection reports failure and leaves the session untouched', () async {
      final repo = _FakeAuthRepository(result: const RefreshAttempt(RefreshOutcome.expired));
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.expired);
      expect(storage.saveCount, 0);
    });

    test('a failed rotation does not poison later attempts', () async {
      final repo = _FakeAuthRepository(result: const RefreshAttempt(RefreshOutcome.expired));
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.expired);
      repo.result = _renewed('access-2', 'rt-2');
      expect(await refresher.refresh(), RefreshOutcome.renewed);
    });

    test('a session with no refresh token cannot renew', () async {
      // Sessions stored by a build that predates refresh tokens.
      final repo = _FakeAuthRepository(
        result: _renewed('x', 'y'),
      );
      final storage = _FakeStorage(session: _session(refreshToken: ''));
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.expired);
      expect(repo.refreshCalls, 0, reason: 'no point calling the API');
    });

    test('throttling is reported as retryable, not as a dead session', () async {
      // The API rate-limits renewal per IP and a shop NATs every till through one
      // address, so a burst of simultaneous renewals can be rejected while every
      // session is still valid. Reporting that as expired would sign a cashier out.
      final repo = _FakeAuthRepository(
        result: const RefreshAttempt(RefreshOutcome.throttled),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.throttled);
      expect(storage.saveCount, 0, reason: 'nothing to persist');

      final kept = await storage.readSession();
      expect(kept, isNotNull, reason: 'the session must survive a 429');
      expect(kept!.refreshToken, 'rt-1',
          reason: 'the unspent token is still usable');
    });

    test('a throttled attempt can be retried successfully', () async {
      final repo = _FakeAuthRepository(
        result: const RefreshAttempt(RefreshOutcome.throttled),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), RefreshOutcome.throttled);
      repo.result = _renewed('access-2', 'rt-2');
      expect(await refresher.refresh(), RefreshOutcome.renewed);
      expect(repo.presentedTokens, ['rt-1', 'rt-1'],
          reason: 'a throttled attempt does not spend the token');
    });

    test('presents the current token, not a stale one', () async {
      final repo = _FakeAuthRepository(
        result: _renewed('access-2', 'rt-2'),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      await refresher.refresh();
      await refresher.refresh();

      expect(repo.presentedTokens, ['rt-1', 'rt-2'],
          reason: 'the second rotation must use the token the first returned');
    });
  });

  group('Session serialization', () {
    test('round-trips the refresh token and its expiry', () {
      final expiry = DateTime.utc(2026, 8, 4, 7, 42, 17);
      final original = Session(
        token: 'a',
        username: 'admin',
        refreshToken: 'r',
        refreshTokenExpiresAt: expiry,
        roles: const ['Admin'],
      );

      final restored = Session.fromJson(original.toJson());

      expect(restored.token, 'a');
      expect(restored.refreshToken, 'r');
      expect(restored.refreshTokenExpiresAt, expiry);
      expect(restored.canRefresh, isTrue);
    });

    test('tolerates a legacy payload with no refresh token', () {
      final restored = Session.fromJson({
        'token': 'a',
        'username': 'admin',
        'roles': ['Admin'],
      });

      expect(restored.refreshToken, isEmpty);
      expect(restored.refreshTokenExpiresAt, isNull);
      expect(restored.canRefresh, isFalse);
    });

    test('parses a UTC expiry from the API as UTC', () {
      final restored = Session.fromJson({
        'token': 'a',
        'username': 'admin',
        'refreshToken': 'r',
        'refreshTokenExpiresAt': '2026-08-04T07:42:17.741719Z',
      });

      expect(restored.refreshTokenExpiresAt!.isUtc, isTrue);
    });
  });
}
