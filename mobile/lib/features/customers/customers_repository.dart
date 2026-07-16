import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/customer.dart';
import '../../shared/models/customer_vehicle.dart';
import '../../shared/models/invoice.dart';
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

  /// Creates a customer (`POST /customers`). Phone must be unique — the API
  /// returns 409 (surfaced as an [AppException]) if it already exists. The
  /// customer code is generated server-side. Returns the new customer id.
  Future<String> createCustomer({
    required String firstName,
    required String lastName,
    String? phone,
    String? email,
    String? companyName,
    String? city,
    String customerType = 'RETAIL',
    String? notes,
  }) async {
    try {
      final res = await _dio.post('/customers', data: {
        'firstName': firstName,
        'lastName': lastName,
        'phone': ?phone,
        'email': ?email,
        'companyName': ?companyName,
        'city': ?city,
        'customerType': customerType,
        'notes': ?notes,
      });
      final body = res.data;
      final map = body is Map ? Map<String, dynamic>.from(body) : null;
      return asString(map?['id']);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Updates a customer (`PUT /customers/{id}`). Phone must stay unique — the
  /// API returns 409 (surfaced as [AppException]) if another customer has it.
  Future<void> updateCustomer({
    required String id,
    required String firstName,
    required String lastName,
    String? phone,
    String? email,
    String? companyName,
    String? city,
    String customerType = 'RETAIL',
    String? notes,
  }) async {
    try {
      await _dio.put('/customers/$id', data: {
        'firstName': firstName,
        'lastName': lastName,
        'phone': ?phone,
        'email': ?email,
        'companyName': ?companyName,
        'city': ?city,
        'customerType': customerType,
        'notes': ?notes,
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Adds a vehicle to a customer
  /// (`POST /customers/{customerId}/vehicles`).
  Future<void> addVehicle({
    required String customerId,
    required String make,
    required String model,
    int? year,
    String? registrationNo,
    String? engineType,
    String? color,
    String? notes,
  }) async {
    try {
      await _dio.post('/customers/$customerId/vehicles', data: {
        'make': make,
        'model': model,
        'year': ?year,
        'registrationNo': ?registrationNo,
        'engineType': ?engineType,
        'color': ?color,
        'notes': ?notes,
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Vehicles registered to a customer (`GET /api/v1/customers/{id}/vehicles`).
  Future<List<CustomerVehicle>> vehicles(String customerId) async {
    try {
      final res = await _dio.get(
        '/customers/$customerId/vehicles',
        queryParameters: {'activeOnly': true},
      );
      final data = res.data;
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) => CustomerVehicle.fromJson(Map<String, dynamic>.from(e)))
          .toList();
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

  /// One page of the customer's invoices, filterable by status and
  /// invoice-number search (`GET /SalesOrder/invoices`). The list response
  /// carries invoice headers only — line items are loaded on demand via
  /// [invoiceLines].
  Future<PagedChunk<Invoice>> invoicesPage({
    required String customerId,
    String? status,
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.get('/SalesOrder/invoices', queryParameters: {
        'customerId': customerId,
        'searchTerm': ?search,
        'status': ?status,
        'pageNumber': page,
        'pageSize': pageSize,
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        Invoice.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Line items for one invoice, from the invoice print-data payload
  /// (`GET /SalesOrder/invoices/{id}/print-data`). Loaded lazily when a row is
  /// expanded.
  Future<List<InvoiceLine>> invoiceLines(String invoiceId) async {
    try {
      final res =
          await _dio.get('/SalesOrder/invoices/$invoiceId/print-data');
      final data = asMapOrNull((res.data as Map<String, dynamic>)['data']);
      return asList(data?['lines'], InvoiceLine.fromJson);
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

  /// Records an incoming payment from a customer
  /// (`POST /customer-payments`). When [invoiceId] is provided the payment is
  /// linked to that invoice. When [isAdvance] is true the payment is recorded as
  /// advance credit on the account (server marks it ADVANCE at creation so a
  /// cash advance completes correctly and counts toward available balance).
  Future<void> recordPayment({
    required String customerId,
    required double amount,
    required String paymentMethod,
    String? transactionNumber,
    DateTime? paymentDate,
    String? notes,
    String currency = 'BDT',
    String? invoiceId,
    bool isAdvance = false,
  }) async {
    try {
      await _dio.post('/customer-payments', data: {
        'customerId': customerId,
        'amount': amount,
        'paymentMethod': paymentMethod,
        if (transactionNumber != null && transactionNumber.isNotEmpty)
          'transactionNumber': transactionNumber,
        'paymentDate': (paymentDate ?? DateTime.now()).toIso8601String(),
        if (notes != null && notes.isNotEmpty) 'notes': notes,
        'currency': currency,
        'invoiceId': ?invoiceId,
        'isAdvance': isAdvance,
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Paginated account statement for a customer
  /// (`POST /customer-account-summary/{id}`).
  Future<CustomerAccountSummary> accountSummary({
    required String customerId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageNumber = 1,
    int pageSize = 30,
  }) async {
    try {
      final res = await _dio.post(
        '/customer-account-summary/$customerId',
        data: {
          'customerId': customerId,
          if (fromDate != null) 'fromDate': fromDate.toIso8601String(),
          if (toDate != null) 'toDate': toDate.toIso8601String(),
          'pageNumber': pageNumber,
          'pageSize': pageSize,
        },
      );
      return CustomerAccountSummary.fromJson(
          res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Downloads a statement as a PDF and returns the raw bytes
  /// (`POST /customer-account-summary/{id}/pdf`).
  Future<Uint8List> accountSummaryPdf(
    String customerId, {
    DateTime? fromDate,
    DateTime? toDate,
  }) async {
    try {
      final res = await _dio.post<List<int>>(
        '/customer-account-summary/$customerId/pdf',
        data: {
          'customerId': customerId,
          if (fromDate != null) 'fromDate': fromDate.toIso8601String(),
          if (toDate != null) 'toDate': toDate.toIso8601String(),
          'pageNumber': 1,
          'pageSize': 9999,
        },
        options: Options(responseType: ResponseType.bytes),
      );
      return Uint8List.fromList(res.data ?? []);
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

/// First page of the account statement — seeds header metrics on the
/// statement screen before the user scrolls.
final customerStatementProvider =
    FutureProvider.family<CustomerAccountSummary, String>((ref, id) {
  return ref
      .read(customersRepositoryProvider)
      .accountSummary(customerId: id);
});

