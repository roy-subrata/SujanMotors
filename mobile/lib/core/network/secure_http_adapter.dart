import 'dart:convert';
import 'dart:io';

import 'package:dio/io.dart';
import 'package:flutter/services.dart';

/// Pins Dart's HTTP layer to the VPS API's self-signed certificate.
///
/// Why this exists: the API (see [ApiConfig.apiBaseUrl]) terminates TLS with a
/// SELF-SIGNED certificate. Android's `network_security_config.xml` is the
/// usual place to express that trust, but Flutter's Dart HTTP stack
/// (dio → dart:io HttpClient → BoringSSL) does NOT read that XML — the pin-set
/// and custom trust-anchor there are ignored. The stock trust store therefore
/// rejects our self-signed cert, and every request dies with a generic
/// "unexpected network error". This wires the same trust into Dart instead.
///
/// Trust is pinned to the exact bundled certificate (byte-for-byte DER pinning,
/// mirroring the SPKI pin in network_security_config.xml). The self-signed cert
/// also lacks an IP Subject Alternative Name, so we must allow it through the
/// hostname check — but only for the one pinned cert, so nothing else is
/// accepted and cleartext stays forbidden.
///
/// When the cert is regenerated on the VPS, replace
/// `assets/certs/sujanmotors-selfsigned.pem` (and the Android raw DER cert used
/// by network_security_config.xml) with the new bytes.
class SecureHttpAdapter {
  SecureHttpAdapter._(this.adapter);

  final IOHttpClientAdapter adapter;

  /// Loads the pinned cert from the bundle and builds the adapter.
  static Future<SecureHttpAdapter> build() async {
    final pemText = (await rootBundle
            .loadString('assets/certs/sujanmotors-selfsigned.pem'))
        .replaceAll('\r', '');
    final pinnedDer = _pemToDer(pemText);

    final securityContext = SecurityContext(withTrustedRoots: false)
      ..setTrustedCertificatesBytes(utf8.encode(pemText));

    final adapter = IOHttpClientAdapter(
      createHttpClient: () => HttpClient(context: securityContext)
        ..badCertificateCallback = (cert, host, port) =>
            _isPinnedCert(cert, pinnedDer),
    );

    return SecureHttpAdapter._(adapter);
  }

  static List<int> _pemToDer(String pemText) {
    final body = pemText
        .replaceAll('-----BEGIN CERTIFICATE-----', '')
        .replaceAll('-----END CERTIFICATE-----', '')
        .replaceAll('\n', '')
        .trim();
    return base64Decode(body);
  }

  /// Exact (byte-for-byte) certificate pinning: accept only the bundled cert.
  static bool _isPinnedCert(X509Certificate? cert, List<int> pinnedDer) {
    if (cert == null) return false;
    final der = cert.der;
    if (der.length != pinnedDer.length) return false;
    for (var i = 0; i < der.length; i++) {
      if (der[i] != pinnedDer[i]) return false;
    }
    return true;
  }
}
