import 'package:autopartshop_mobile/core/network/app_exception.dart';
import 'package:autopartshop_mobile/shared/models/paged_response.dart';
import 'package:autopartshop_mobile/shared/models/product.dart';
import 'package:dio/dio.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  group('PagedResponse', () {
    test('parses the data + pagination envelope', () {
      final json = {
        'data': [
          {
            'id': 'p1',
            'name': 'Brake Pad',
            'partNumber': 'BP-1',
            'sku': 'SKU-1',
            'pricing': {'sellingPrice': 150.5, 'currency': 'BDT'},
            'variants': [
              {'name': 'Default', 'code': 'DEF', 'isDefault': true}
            ],
          }
        ],
        'pagination': {
          'page': 1,
          'pageSize': 20,
          'totalCount': 1,
          'totalPages': 1,
          'hasNextPage': false,
          'hasPreviousPage': false,
        },
      };

      final result = PagedResponse.fromJson(json, Product.fromJson);

      expect(result.data, hasLength(1));
      expect(result.data.first.name, 'Brake Pad');
      expect(result.data.first.pricing?.sellingPrice, 150.5);
      expect(result.data.first.variants, hasLength(1));
      expect(result.pagination.hasNextPage, isFalse);
    });

    test('tolerates missing pagination block', () {
      final result =
          PagedResponse.fromJson({'data': []}, Product.fromJson);
      expect(result.data, isEmpty);
      expect(result.pagination.totalCount, 0);
    });
  });

  group('AppException.fromDio', () {
    DioException withResponse(int status, dynamic data) => DioException(
          requestOptions: RequestOptions(path: '/x'),
          response: Response(
            requestOptions: RequestOptions(path: '/x'),
            statusCode: status,
            data: data,
          ),
          type: DioExceptionType.badResponse,
        );

    test('reads ApiError.detail', () {
      final e = AppException.fromDio(withResponse(404, {
        'type': 'NOT_FOUND',
        'title': 'Not found',
        'status': 404,
        'detail': 'Product not found',
      }));
      expect(e.message, 'Product not found');
      expect(e.statusCode, 404);
    });

    test('falls back to message then title', () {
      final e = AppException.fromDio(
          withResponse(400, {'message': 'legacy message'}));
      expect(e.message, 'legacy message');
    });

    test('parses field validation errors', () {
      final e = AppException.fromDio(withResponse(400, {
        'detail': 'Validation failed',
        'errors': {
          'Name': ['Name is required']
        },
      }));
      expect(e.fieldErrors?['Name'], contains('Name is required'));
    });

    test('handles connection errors without a response', () {
      final e = AppException.fromDio(DioException(
        requestOptions: RequestOptions(path: '/x'),
        type: DioExceptionType.connectionError,
      ));
      expect(e.message, contains('reach the server'));
    });
  });
}
