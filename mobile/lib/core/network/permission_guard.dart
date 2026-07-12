import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/auth_controller.dart';
import 'app_exception.dart';

/// Throws [AppException] with a 403 status when the current session lacks the
/// required permission.  Call this at the top of any repository method that
/// maps to a permission-gated backend endpoint.
///
/// Usage:
/// ```dart
/// await requirePermission(ref, 'sales.create');
/// ```
Future<void> requirePermission(Ref ref, String permission) async {
  final asyncSession = ref.read(authControllerProvider);
  final session = asyncSession.value;
  if (session == null) {
    throw AppException('Not authenticated.', statusCode: 401);
  }
  // Admin role bypasses permission checks, matching the backend's
  // PermissionAuthorizationHandler behavior.
  if (session.hasRole('Admin')) return;

  if (!session.hasPermission(permission)) {
    throw AppException(
      "You don't have permission for this action.",
      statusCode: 403,
    );
  }
}
