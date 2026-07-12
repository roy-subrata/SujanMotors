import '../../shared/models/json.dart';

/// Authenticated staff session, built from `POST /api/v1/auth/login`.
class Session {
  const Session({
    required this.token,
    required this.username,
    this.email,
    this.fullName,
    this.roles = const [],
    this.permissions = const [],
  });

  final String token;
  final String username;
  final String? email;
  final String? fullName;
  final List<String> roles;
  final List<String> permissions;

  String get displayName =>
      (fullName != null && fullName!.isNotEmpty) ? fullName! : username;

  bool hasRole(String role) => roles.contains(role);

  /// Check if the session has a specific permission.
  /// Permission strings match the backend's `Permissions.*` constants
  /// (e.g. `sales.create`, `inventory.view`, `inventory.adjust-stock`).
  bool hasPermission(String permission) => permissions.contains(permission);

  /// Convenience getters for mobile-app-relevant permissions.
  bool get canCreateSale => hasPermission('sales.create');
  bool get canViewSales => hasPermission('sales.view');
  bool get canEditSales => hasPermission('sales.edit');
  bool get canProcessPayment => hasPermission('sales.process-payment');
  bool get canViewInventory => hasPermission('inventory.view');
  bool get canCreateInventory => hasPermission('inventory.create');
  bool get canEditInventory => hasPermission('inventory.edit');
  bool get canAdjustStock => hasPermission('inventory.adjust-stock');

  factory Session.fromJson(Map<String, dynamic> json) {
    return Session(
      token: asString(json['token']),
      username: asString(json['username']),
      email: asStringOrNull(json['email']),
      fullName: asStringOrNull(json['fullName']),
      roles: asStringList(json['roles']),
      permissions: asStringList(json['permissions']),
    );
  }

  Map<String, dynamic> toJson() => {
        'token': token,
        'username': username,
        'email': email,
        'fullName': fullName,
        'roles': roles,
        'permissions': permissions,
      };
}
