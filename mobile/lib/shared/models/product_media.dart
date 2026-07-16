import '../../core/config/api_config.dart';
import 'json.dart';

/// One media entry of a product, from `GET /products/{id}/media`.
/// Ordered by the API for display (primary/sortOrder first).
class ProductMedia {
  const ProductMedia({
    required this.id,
    required this.url,
    this.mediaType = 'image',
    this.altText,
    this.fileName,
    this.sortOrder = 0,
    this.isPrimary = false,
    this.variantId,
  });

  final String id;

  /// Either an API-relative stored-file URL (`/api/v1/files/{id}/content`)
  /// or an absolute external URL.
  final String url;
  final String mediaType; // image | video
  final String? altText;
  final String? fileName;
  final int sortOrder;
  final bool isPrimary;
  final String? variantId;

  bool get isImage => mediaType.toLowerCase() == 'image';

  /// Absolute URL usable by Image.network — relative stored-file URLs are
  /// resolved against the configured API host (`/files/{id}/content` is
  /// AllowAnonymous, so no auth header is needed).
  String get resolvedUrl =>
      url.startsWith('http') ? url : '${ApiConfig.baseUrl}$url';

  factory ProductMedia.fromJson(Map<String, dynamic> json) => ProductMedia(
        id: asString(json['id']),
        url: asString(json['url']),
        mediaType: asString(json['mediaType'], fallback: 'image'),
        altText: asStringOrNull(json['altText']),
        fileName: asStringOrNull(json['fileName']),
        sortOrder: asInt(json['sortOrder']),
        isPrimary: asBool(json['isPrimary']),
        variantId: asStringOrNull(json['variantId']),
      );
}
