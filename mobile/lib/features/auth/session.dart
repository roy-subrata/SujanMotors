import '../../shared/models/json.dart';

/// Authenticated staff session, built from `POST /api/v1/auth/login`.
///
/// [token] is the short-lived access JWT; [refreshToken] is the long-lived,
/// single-use credential used to renew it. The refresh token rotates on every
/// renewal, so whatever is held here must always be the newest one — presenting
/// a spent token trips server-side reuse detection and kills the whole session.
class Session {
  const Session({
    required this.token,
    required this.username,
    this.refreshToken = '',
    this.refreshTokenExpiresAt,
    this.email,
    this.fullName,
    this.roles = const [],
    this.permissions = const [],
  });

  final String token;
  final String username;

  /// Empty for sessions restored from a build that predates refresh tokens;
  /// such a session simply cannot renew and ends at access-token expiry.
  final String refreshToken;

  final DateTime? refreshTokenExpiresAt;

  final String? email;
  final String? fullName;
  final List<String> roles;
  final List<String> permissions;

  String get displayName =>
      (fullName != null && fullName!.isNotEmpty) ? fullName! : username;

  bool hasRole(String role) => roles.contains(role);

  bool get canRefresh => refreshToken.isNotEmpty;

  /// Applies a rotation result, keeping the identity fields already held.
  Session withRefreshedTokens({
    required String token,
    required String refreshToken,
    DateTime? refreshTokenExpiresAt,
    List<String>? roles,
    List<String>? permissions,
  }) =>
      Session(
        token: token,
        username: username,
        refreshToken: refreshToken,
        refreshTokenExpiresAt: refreshTokenExpiresAt ?? this.refreshTokenExpiresAt,
        email: email,
        fullName: fullName,
        roles: roles ?? this.roles,
        permissions: permissions ?? this.permissions,
      );

  factory Session.fromJson(Map<String, dynamic> json) {
    return Session(
      token: asString(json['token']),
      username: asString(json['username']),
      refreshToken: asString(json['refreshToken']),
      refreshTokenExpiresAt: asDateTimeOrNull(json['refreshTokenExpiresAt']),
      email: asStringOrNull(json['email']),
      fullName: asStringOrNull(json['fullName']),
      roles: asStringList(json['roles']),
      permissions: asStringList(json['permissions']),
    );
  }

  Map<String, dynamic> toJson() => {
        'token': token,
        'username': username,
        'refreshToken': refreshToken,
        'refreshTokenExpiresAt': refreshTokenExpiresAt?.toIso8601String(),
        'email': email,
        'fullName': fullName,
        'roles': roles,
        'permissions': permissions,
      };
}
