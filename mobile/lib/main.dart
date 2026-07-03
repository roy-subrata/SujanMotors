import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import 'core/notifications/local_notifications.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';

Future<void> main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await LocalNotifications.instance.init();
  runApp(const ProviderScope(child: AutoPartShopApp()));
}

class AutoPartShopApp extends ConsumerWidget {
  const AutoPartShopApp({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final router = ref.watch(routerProvider);
    return MaterialApp.router(
      title: 'Auto Parts Shop',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      routerConfig: router,
      // Many list/detail screens lean on compact 9-12px labels for density —
      // a flat floor nudges everything (including those literal sizes)
      // slightly larger app-wide, while still respecting/allowing a bigger
      // system accessibility font setting up to the cap.
      builder: (context, child) => MediaQuery.withClampedTextScaling(
        minScaleFactor: 1.1,
        maxScaleFactor: 1.3,
        child: child!,
      ),
    );
  }
}
