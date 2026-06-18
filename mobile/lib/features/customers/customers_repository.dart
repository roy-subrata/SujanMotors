import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/customer.dart';
import '../../shared/models/json.dart';
import '../../shared/models/paged_response.dart';

/// One page of customers plus whether more pages remain (for infinite scroll).
class CustomerPage {
  const CustomerPage({required this.items, required this.hasMore});
  final List<Customer> items;
  final bool hasMore;
}

class CustomersRepository {
  CustomersRepository(this._dio);

  final Dio _dio;

  /// Paged customer list via `POST /customers/list` (`PagedResult` envelope).
  Future<CustomerPage> list({
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.post('/customers/list', data: {
        'search': search ?? '',
        'pageNumber': page,
        'pageSize': pageSize,
        'sorts': const [],
      });
      final body = res.data as Map<String, dynamic>;
      final items = asList(body['data'], Customer.fromJson);
      final pg = asMapOrNull(body['pagination']);
      final pageNumber = asInt(pg?['pageNumber'], fallback: page);
      final totalPages = asInt(pg?['totalPages']);
      return CustomerPage(items: items, hasMore: pageNumber < totalPages);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<Customer> getById(String id) async {
    try {
      final res = await _dio.get('/customers/$id');
      return Customer.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<CustomerPaymentSummary> paymentSummary(String customerId) async {
    try {
      final res =
          await _dio.get('/customer-payments/customer/$customerId/summary');
      return CustomerPaymentSummary.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// The customer's parts-buying history (`GET /SalesOrder/customer/{id}`).
  Future<List<CustomerOrder>> orders(String customerId) async {
    try {
      final res = await _dio.get('/SalesOrder/customer/$customerId');
      final list = res.data as List<dynamic>;
      return list
          .map((e) => CustomerOrder.fromJson(Map<String, dynamic>.from(e as Map)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// One page of the customer's invoices (sales orders), filterable by status
  /// and invoice-number search (`POST /SalesOrder/list`).
  Future<PagedChunk<CustomerOrder>> ordersPage({
    required String customerId,
    String? status,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.post('/SalesOrder/list', data: {
        'customerId': customerId,
        'status': ?status,
        'search': search ?? '',
        'pageNumber': page,
        'pageSize': pageSize,
        'sorts': const [],
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        CustomerOrder.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// One page of the customer's payments, filterable by status and search
  /// (`POST /customer-payments/list`).
  Future<PagedChunk<PaymentHistoryItem>> paymentsPage({
    required String customerId,
    String? status,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.post('/customer-payments/list', data: {
        'customerId': customerId,
        'status': ?status,
        'search': search ?? '',
        'pageNumber': page,
        'pageSize': pageSize,
        'sorts': const [],
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        PaymentHistoryItem.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Sends a payment-due reminder to the customer. Returns the API's message.
  Future<String> sendPaymentReminder({
    required String customerId,
    required String channel, // SMS | WHATSAPP | EMAIL
    String? message,
  }) async {
    try {
      final res = await _dio.post(
        '/notifications/send-payment-reminder/$customerId',
        data: {'channel': channel, 'message': ?message},
      );
      final body = res.data;
      if (body is Map && body['message'] is String) return body['message'];
      return 'Reminder sent';
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final customersRepositoryProvider = Provider<CustomersRepository>(
  (ref) => CustomersRepository(ref.read(dioProvider)),
);

/// Single customer by id.
final customerDetailProvider =
    FutureProvider.family<Customer, String>((ref, id) {
  return ref.read(customersRepositoryProvider).getById(id);
});

/// Payment + due summary for a customer.
final customerPaymentSummaryProvider =
    FutureProvider.family<CustomerPaymentSummary, String>((ref, id) {
  return ref.read(customersRepositoryProvider).paymentSummary(id);
});

/// Parts-buying history for a customer.
final customerOrdersProvider =
    FutureProvider.family<List<CustomerOrder>, String>((ref, id) {
  return ref.read(customersRepositoryProvider).orders(id);
});
