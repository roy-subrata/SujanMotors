import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/json.dart';

// ── Models ────────────────────────────────────────────────────────────────────

/// Supplier as returned by `POST /suppliers/list` and `GET /suppliers/{id}`.
class Supplier {
  const Supplier({
    required this.id,
    required this.name,
    required this.code,
    this.contactPerson,
    this.phone,
    this.email,
    this.paymentTerms,
    this.currentBalance = 0,
    this.isActive = true,
  });

  final String id;
  final String name;
  final String code;
  final String? contactPerson;
  final String? phone;
  final String? email;
  final String? paymentTerms;

  /// Outstanding payable to this supplier ("we owe").
  final double currentBalance;
  final bool isActive;

  bool get hasPayable => currentBalance > 0;

  factory Supplier.fromJson(Map<String, dynamic> json) => Supplier(
        id: asString(json['id']),
        name: asString(json['name']),
        code: asString(json['code']),
        contactPerson: asStringOrNull(json['contactPerson']),
        phone: asStringOrNull(json['phone']),
        email: asStringOrNull(json['email']),
        paymentTerms: asStringOrNull(json['paymentTerms']),
        currentBalance: asDouble(json['currentBalance']),
        isActive: asBool(json['isActive'], fallback: true),
      );
}

/// One page of suppliers plus whether more pages remain (for infinite scroll).
class SupplierPage {
  const SupplierPage({required this.items, required this.hasMore});
  final List<Supplier> items;
  final bool hasMore;
}

/// An open purchase bill (purchase order with outstanding balance) that a
/// payment can be applied against.
class SupplierBill {
  const SupplierBill({
    required this.id,
    required this.billNumber,
    required this.billDate,
    required this.grandTotal,
    required this.outstandingAmount,
    required this.status,
  });

  final String id;
  final String billNumber;
  final DateTime billDate;
  final double grandTotal;
  final double outstandingAmount;
  final String status;

  factory SupplierBill.fromJson(Map<String, dynamic> json) => SupplierBill(
        id: asString(json['id']),
        billNumber: asString(json['poNumber']),
        billDate: DateTime.tryParse(asString(json['orderDate'])) ??
            DateTime.now(),
        grandTotal: asDouble(json['grandTotal']),
        outstandingAmount: asDouble(json['outstandingAmount']),
        status: asString(json['status']),
      );
}

/// Payment provider (`GET /payment-provider/active`). Supplier payments must
/// reference one; the mobile method grid maps onto `providerType`.
class PaymentProviderInfo {
  const PaymentProviderInfo({
    required this.id,
    required this.name,
    required this.type,
  });

  final String id;
  final String name;
  final String type; // CASH, BANK_TRANSFER, CHECK, MOBILE_BANKING, ...

  factory PaymentProviderInfo.fromJson(Map<String, dynamic> json) =>
      PaymentProviderInfo(
        id: asString(json['id']),
        name: asString(json['providerName']),
        type: asString(json['providerType']),
      );
}

/// One row of the supplier ledger (`/supplier-ledger/{id}/entries`).
class SupplierLedgerEntry {
  const SupplierLedgerEntry({
    required this.id,
    required this.date,
    required this.type,
    required this.referenceNumber,
    required this.description,
    required this.debit,
    required this.credit,
    required this.runningBalance,
  });

  final String id;
  final DateTime date;
  final String type; // PURCHASE, PAYMENT, REFUND, ADVANCE, CANCELLATION
  final String referenceNumber;
  final String description;
  final double debit; // purchases — increases what we owe
  final double credit; // payments/refunds — decreases what we owe
  final double runningBalance;

  factory SupplierLedgerEntry.fromJson(Map<String, dynamic> json) =>
      SupplierLedgerEntry(
        id: asString(json['id']),
        date: DateTime.tryParse(asString(json['transactionDate'])) ??
            DateTime.now(),
        type: asString(json['transactionTypeName'],
            fallback: asString(json['transactionType'])),
        referenceNumber: asString(json['referenceNumber']),
        description: asString(json['description']),
        debit: asDouble(json['debitAmount']),
        credit: asDouble(json['creditAmount']),
        runningBalance: asDouble(json['runningBalance']),
      );
}

/// Ledger totals for a supplier (`GET /supplier-ledger/{id}/summary`).
class SupplierLedgerSummary {
  const SupplierLedgerSummary({
    required this.supplierName,
    required this.totalPurchases,
    required this.totalPayments,
    required this.totalRefunds,
    required this.currentBalance,
  });

  final String supplierName;
  final double totalPurchases;
  final double totalPayments;
  final double totalRefunds;
  final double currentBalance;

  factory SupplierLedgerSummary.fromJson(Map<String, dynamic> json) =>
      SupplierLedgerSummary(
        supplierName: asString(json['supplierName']),
        totalPurchases: asDouble(json['totalPurchases']),
        totalPayments: asDouble(json['totalPayments']),
        totalRefunds: asDouble(json['totalRefunds']),
        currentBalance: asDouble(json['currentBalance']),
      );
}

// ── Repository ────────────────────────────────────────────────────────────────

class SuppliersRepository {
  SuppliersRepository(this._dio);

  final Dio _dio;

  /// Paged supplier list via `POST /suppliers/list` (`PagedResult` envelope).
  Future<SupplierPage> list({
    String? search,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.post('/suppliers/list', data: {
        'search': search ?? '',
        'pageNumber': page,
        'pageSize': pageSize,
        'sorts': const [],
      });
      final body = res.data as Map<String, dynamic>;
      final items = asList(body['data'], Supplier.fromJson);
      final pg = asMapOrNull(body['pagination']);
      final pageNumber = asInt(pg?['pageNumber'], fallback: page);
      final totalPages = asInt(pg?['totalPages']);
      return SupplierPage(items: items, hasMore: pageNumber < totalPages);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<Supplier> getById(String id) async {
    try {
      final res = await _dio.get('/suppliers/$id');
      return Supplier.fromJson(res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Purchase orders with an outstanding balance for this supplier
  /// (`GET /PurchaseOrder/supplier/{id}`), oldest first — payments are applied
  /// against these.
  Future<List<SupplierBill>> openBills(String supplierId) async {
    try {
      final res = await _dio.get('/PurchaseOrder/supplier/$supplierId');
      final list = res.data;
      if (list is! List) return const [];
      const payable = {'CONFIRMED', 'PARTIAL', 'DELIVERED'};
      final bills = list
          .whereType<Map>()
          .map((e) => SupplierBill.fromJson(Map<String, dynamic>.from(e)))
          .where((b) =>
              b.outstandingAmount > 0 &&
              payable.contains(b.status.toUpperCase()))
          .toList()
        ..sort((a, b) => a.billDate.compareTo(b.billDate));
      return bills;
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Active payment providers (`GET /payment-provider/active`).
  Future<List<PaymentProviderInfo>> activeProviders() async {
    try {
      final res = await _dio.get('/payment-provider/active');
      final list = res.data;
      if (list is! List) return const [];
      return list
          .whereType<Map>()
          .map((e) =>
              PaymentProviderInfo.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Records one supplier payment (`POST /supplier-payments`). Regular
  /// payments must reference a purchase order; without one the API requires
  /// the ADVANCE payment type.
  Future<void> createPayment({
    required String supplierId,
    required String paymentProviderId,
    required double amount,
    required String paymentMethod,
    String? purchaseOrderId,
    String? referenceNumber,
    String? notes,
    DateTime? paymentDate,
  }) async {
    try {
      await _dio.post('/supplier-payments', data: {
        'supplierId': supplierId,
        'purchaseOrderId': ?purchaseOrderId,
        'paymentProviderId': paymentProviderId,
        'amount': amount,
        'paymentMethod': paymentMethod,
        'transactionNumber': '',
        'referenceNumber': referenceNumber ?? '',
        'invoiceNumber': '',
        'paymentDate': (paymentDate ?? DateTime.now()).toIso8601String(),
        'notes': notes ?? '',
        'paymentType': purchaseOrderId == null ? 'ADVANCE' : 'REGULAR',
        'description': purchaseOrderId == null ? (notes ?? '') : '',
      });
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Ledger totals (`GET /supplier-ledger/{id}/summary`).
  Future<SupplierLedgerSummary> ledgerSummary(String supplierId) async {
    try {
      final res = await _dio.get(
        '/supplier-ledger/$supplierId/summary',
        queryParameters: {'entryLimit': 1},
      );
      return SupplierLedgerSummary.fromJson(
          res.data as Map<String, dynamic>);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Ledger entries for a period (`POST /supplier-ledger/{id}/entries`),
  /// oldest first so running balances read naturally.
  Future<List<SupplierLedgerEntry>> ledgerEntries({
    required String supplierId,
    DateTime? fromDate,
    DateTime? toDate,
    int pageSize = 200,
  }) async {
    try {
      final res = await _dio.post(
        '/supplier-ledger/$supplierId/entries',
        data: {
          if (fromDate != null) 'fromDate': fromDate.toIso8601String(),
          if (toDate != null) 'toDate': toDate.toIso8601String(),
          'pageNumber': 1,
          'pageSize': pageSize,
        },
      );
      final body = res.data as Map<String, dynamic>;
      final entries = asList(body['entries'], SupplierLedgerEntry.fromJson)
        ..sort((a, b) => a.date.compareTo(b.date));
      return entries;
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

final suppliersRepositoryProvider = Provider<SuppliersRepository>(
  (ref) => SuppliersRepository(ref.read(dioProvider)),
);

/// Single supplier by id.
final supplierDetailProvider =
    FutureProvider.family<Supplier, String>((ref, id) {
  return ref.read(suppliersRepositoryProvider).getById(id);
});

/// Open purchase bills (outstanding POs) for a supplier, oldest first.
final supplierBillsProvider =
    FutureProvider.family<List<SupplierBill>, String>((ref, id) {
  return ref.read(suppliersRepositoryProvider).openBills(id);
});

/// Active payment providers for the method picker.
final paymentProvidersProvider =
    FutureProvider<List<PaymentProviderInfo>>((ref) {
  return ref.read(suppliersRepositoryProvider).activeProviders();
});

/// Ledger totals for the statement header.
final supplierLedgerSummaryProvider =
    FutureProvider.family<SupplierLedgerSummary, String>((ref, id) {
  return ref.read(suppliersRepositoryProvider).ledgerSummary(id);
});
