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

  RefreshedTokens? result;
  Duration delay;
  int refreshCalls = 0;
  final List<String> presentedTokens = [];

  @override
  Future<RefreshedTokens?> refresh(String refreshToken) async {
    refreshCalls++;
    presentedTokens.add(refreshToken);
    if (delay > Duration.zero) await Future<void>.delayed(delay);
    return result;
  }
}

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
        result: const RefreshedTokens(token: 'access-2', refreshToken: 'rt-2'),
        delay: const Duration(milliseconds: 50),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      final results = await Future.wait([
        refresher.refresh(),
        refresher.refresh(),
        refresher.refresh(),
      ]);

      expect(results, everyElement(isTrue));
      expect(repo.refreshCalls, 1, reason: 'three callers, one rotation');
    });

    test('a later refresh starts a new rotation', () async {
      final repo = _FakeAuthRepository(
        result: const RefreshedTokens(token: 'access-2', refreshToken: 'rt-2'),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), isTrue);
      expect(await refresher.refresh(), isTrue);
      expect(repo.refreshCalls, 2, reason: 'in-flight must clear on completion');
    });

    test('persists the rotated pair', () async {
      final repo = _FakeAuthRepository(
        result: const RefreshedTokens(
          token: 'access-2',
          refreshToken: 'rt-2',
          roles: ['Admin', 'Manager'],
          permissions: ['sales.view', 'sales.edit'],
        ),
      );
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), isTrue);

      final saved = await storage.readSession();
      expect(saved!.token, 'access-2');
      expect(saved.refreshToken, 'rt-2',
          reason: 'the spent token must be replaced');
      expect(saved.username, 'admin', reason: 'identity fields are preserved');
      expect(saved.roles, ['Admin', 'Manager'],
          reason: 'role changes apply without a re-login');
    });

    test('rejection reports failure and leaves the session untouched', () async {
      final repo = _FakeAuthRepository(result: null);
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), isFalse);
      expect(storage.saveCount, 0);
    });

    test('a failed rotation does not poison later attempts', () async {
      final repo = _FakeAuthRepository(result: null);
      final storage = _FakeStorage(session: _session());
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), isFalse);
      repo.result =
          const RefreshedTokens(token: 'access-2', refreshToken: 'rt-2');
      expect(await refresher.refresh(), isTrue);
    });

    test('a session with no refresh token cannot renew', () async {
      // Sessions stored by a build that predates refresh tokens.
      final repo = _FakeAuthRepository(
        result: const RefreshedTokens(token: 'x', refreshToken: 'y'),
      );
      final storage = _FakeStorage(session: _session(refreshToken: ''));
      final refresher = TokenRefresher(repo, storage);

      expect(await refresher.refresh(), isFalse);
      expect(repo.refreshCalls, 0, reason: 'no point calling the API');
    });

    test('presents the current token, not a stale one', () async {
      final repo = _FakeAuthRepository(
        result: const RefreshedTokens(token: 'access-2', refreshToken: 'rt-2'),
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
