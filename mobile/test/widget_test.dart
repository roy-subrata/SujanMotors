import 'package:autopartshop_mobile/core/storage/token_storage.dart';
import 'package:autopartshop_mobile/features/auth/login_screen.dart';
import 'package:autopartshop_mobile/features/auth/session.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_secure_storage/flutter_secure_storage.dart';
import 'package:flutter_test/flutter_test.dart';

/// In-memory token store so widget tests don't touch platform channels.
class _FakeTokenStorage extends TokenStorage {
  _FakeTokenStorage() : super(const FlutterSecureStorage());

  Session? _session;

  @override
  Future<Session?> readSession() async => _session;

  @override
  Future<String?> readToken() async => _session?.token;

  @override
  Future<void> saveSession(Session session) async => _session = session;

  @override
  Future<void> clear() async => _session = null;
}

void main() {
  testWidgets('login form validates required fields', (tester) async {
    await tester.pumpWidget(
      ProviderScope(
        overrides: [
          tokenStorageProvider.overrideWithValue(_FakeTokenStorage()),
        ],
        child: const MaterialApp(home: LoginScreen()),
      ),
    );
    await tester.pumpAndSettle();

    expect(find.text('Sign in'), findsOneWidget);

    await tester.tap(find.text('Sign in'));
    await tester.pump();

    expect(find.text('Username is required'), findsOneWidget);
    expect(find.text('Password is required'), findsOneWidget);
  });
}
