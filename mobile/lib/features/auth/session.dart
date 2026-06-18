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
