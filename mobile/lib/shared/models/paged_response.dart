import 'json.dart';

/// Pagination metadata returned by list endpoints.
class Pagination {
  const Pagination({
    required this.page,
    required this.pageSize,
    required this.totalCount,
    required this.totalPages,
    required this.hasNextPage,
    required this.hasPreviousPage,
  });

  final int page;
  final int pageSize;
  final int totalCount;
  final int totalPages;
  final bool hasNextPage;
  final bool hasPreviousPage;

  static const empty = Pagination(
    page: 1,
    pageSize: 0,
    totalCount: 0,
    totalPages: 0,
    hasNextPage: false,
    hasPreviousPage: false,
  );

  factory Pagination.fromJson(Map<String, dynamic> json) => Pagination(
        page: asInt(json['page'], fallback: 1),
        pageSize: asInt(json['pageSize']),
        totalCount: asInt(json['totalCount']),
        totalPages: asInt(json['totalPages']),
        hasNextPage: asBool(json['hasNextPage']),
        hasPreviousPage: asBool(json['hasPreviousPage']),
      );
}

/// One fetched page of a server-paginated list: the items, the server's total
/// count, and whether more pages remain. Used by `PagedListView`.
class PagedChunk<T> {
  const PagedChunk({
    required this.items,
    required this.totalCount,
    required this.hasMore,
  });

  final List<T> items;
  final int totalCount;
  final bool hasMore;

  /// Parses a backend `PagedResult` envelope: `{ data, pagination }`.
  factory PagedChunk.fromPagedResult(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) {
    final items = asList(json['data'], fromItem);
    final pg = json['pagination'] is Map
        ? Map<String, dynamic>.from(json['pagination'])
        : const <String, dynamic>{};
    final pageNumber = asInt(pg['pageNumber'], fallback: 1);
    final totalPages = asInt(pg['totalPages']);
    return PagedChunk<T>(
      items: items,
      totalCount: asInt(pg['totalCount']),
      hasMore: pageNumber < totalPages,
    );
  }
}

/// Generic `{ data: [...], pagination: {...} }` envelope.
class PagedResponse<T> {
  const PagedResponse({required this.data, required this.pagination});

  final List<T> data;
  final Pagination pagination;

  factory PagedResponse.fromJson(
    Map<String, dynamic> json,
    T Function(Map<String, dynamic>) fromItem,
  ) {
    return PagedResponse<T>(
      data: asList(json['data'], fromItem),
      pagination: json['pagination'] is Map
          ? Pagination.fromJson(Map<String, dynamic>.from(json['pagination']))
          : Pagination.empty,
    );
  }
}
