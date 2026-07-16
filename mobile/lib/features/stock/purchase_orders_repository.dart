import 'package:dio/dio.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/network/app_exception.dart';
import '../../core/network/dio_provider.dart';
import '../../shared/models/json.dart';
import '../../shared/models/paged_response.dart';

/// A purchase order header + lines, from the PurchaseOrder API.
/// Statuses: DRAFT → SUBMITTED → CONFIRMED → PARTIAL → DELIVERED | CANCELLED.
class PurchaseOrder {
  const PurchaseOrder({
    required this.id,
    required this.poNumber,
    required this.supplierId,
    required this.supplierName,
    required this.orderDate,
    required this.status,
    required this.grandTotal,
    this.currency,
    this.notes,
    this.lines = const [],
  });

  final String id;
  final String poNumber;
  final String supplierId;
  final String supplierName;
  final DateTime orderDate;
  final String status;
  final double grandTotal;
  final String? currency;
  final String? notes;
  final List<PurchaseOrderLine> lines;

  bool get isReceived => status == 'DELIVERED';
  bool get isCancelled => status == 'CANCELLED';

  /// True when the receive flow can (eventually) run for this PO.
  bool get isReceivable => !isReceived && !isCancelled;

  factory PurchaseOrder.fromJson(Map<String, dynamic> json) => PurchaseOrder(
        id: asString(json['id']),
        poNumber: asString(json['poNumber']),
        supplierId: asString(json['supplierId']),
        supplierName: asString(json['supplierName']),
        orderDate: DateTime.tryParse(asString(json['orderDate']))?.toLocal() ??
            DateTime.now(),
        status: asString(json['status']),
        grandTotal: asDouble(json['grandTotal']),
        currency: asStringOrNull(json['currency']),
        notes: asStringOrNull(json['notes']),
        lines: asList(json['lines'], PurchaseOrderLine.fromJson),
      );
}

class PurchaseOrderLine {
  const PurchaseOrderLine({
    required this.id,
    required this.partId,
    required this.displayName,
    required this.quantity,
    required this.receivedQuantity,
    required this.remainingQuantity,
    required this.unitPrice,
    required this.lineTotal,
    this.variantId,
    this.unitId,
    this.unitSymbol,
  });

  final String id;
  final String partId;
  final String displayName;
  final int quantity;
  final int receivedQuantity;
  final int remainingQuantity;
  final double unitPrice;
  final double lineTotal;
  final String? variantId;
  final String? unitId;
  final String? unitSymbol;

  factory PurchaseOrderLine.fromJson(Map<String, dynamic> json) =>
      PurchaseOrderLine(
        id: asString(json['id']),
        partId: asString(json['partId']),
        displayName: asStringOrNull(json['displayName']) ??
            asStringOrNull(json['partName']) ??
            'Item',
        quantity: asInt(json['quantity']),
        receivedQuantity: asInt(json['receivedQuantity']),
        remainingQuantity: asInt(json['remainingQuantity']),
        unitPrice: asDouble(json['unitPrice']),
        lineTotal: asDouble(json['lineTotal']),
        variantId: asStringOrNull(json['variantId']),
        unitId: asStringOrNull(json['unitId']),
        unitSymbol: asStringOrNull(json['unitSymbol']),
      );
}

class Warehouse {
  const Warehouse({required this.id, required this.name});

  final String id;
  final String name;

  factory Warehouse.fromJson(Map<String, dynamic> json) => Warehouse(
        id: asString(json['id']),
        name: asString(json['name']),
      );
}

/// A draft line in the New Stock In entry (E4) before it becomes a PO line.
class StockInDraftLine {
  const StockInDraftLine({
    required this.partId,
    required this.displayName,
    required this.quantity,
    required this.unitCost,
    this.variantId,
    this.batchNumber,
    this.expiryDate,
  });

  final String partId;
  final String displayName;
  final int quantity;
  final double unitCost;
  final String? variantId;
  final String? batchNumber;
  final DateTime? expiryDate;

  double get lineTotal => unitCost * quantity;

  StockInDraftLine copyWith({
    int? quantity,
    double? unitCost,
    String? batchNumber,
    DateTime? expiryDate,
  }) =>
      StockInDraftLine(
        partId: partId,
        displayName: displayName,
        quantity: quantity ?? this.quantity,
        unitCost: unitCost ?? this.unitCost,
        variantId: variantId,
        batchNumber: batchNumber ?? this.batchNumber,
        expiryDate: expiryDate ?? this.expiryDate,
      );
}

class PurchaseOrdersRepository {
  PurchaseOrdersRepository(this._dio);

  final Dio _dio;

  /// Paged PO list. [pendingOnly] keeps POs that still have quantity left to
  /// receive (the "Pending" chip); [status] filters by exact backend status.
  Future<PagedChunk<PurchaseOrder>> list({
    String? search,
    String? status,
    bool pendingOnly = false,
    int page = 1,
    int pageSize = 20,
  }) async {
    try {
      final res = await _dio.post('/PurchaseOrder/list', data: {
        'pageNumber': page,
        'pageSize': pageSize,
        'search': search ?? '',
        'status': ?status,
        if (pendingOnly) 'hasReceivableQuantity': true,
      });
      return PagedChunk.fromPagedResult(
        res.data as Map<String, dynamic>,
        PurchaseOrder.fromJson,
      );
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<PurchaseOrder> getById(String id) async {
    try {
      final res = await _dio.get('/PurchaseOrder/$id');
      return PurchaseOrder.fromJson(
          Map<String, dynamic>.from(res.data as Map));
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  /// Creates a DRAFT purchase order and returns it (with server-assigned
  /// PO number and line ids).
  Future<PurchaseOrder> create({
    required String supplierId,
    required DateTime deliveryDate,
    required List<StockInDraftLine> lines,
    String notes = '',
  }) async {
    try {
      final res = await _dio.post('/PurchaseOrder', data: {
        'supplierId': supplierId,
        'deliveryDate': deliveryDate.toIso8601String(),
        'notes': notes,
        'lineItems': lines
            .map((l) => {
                  'partId': l.partId,
                  'variantId': ?l.variantId,
                  'quantity': l.quantity,
                  'unitPrice': l.unitCost,
                })
            .toList(),
      });
      return PurchaseOrder.fromJson(
          Map<String, dynamic>.from(res.data as Map));
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<void> submit(String id) => _patch('/PurchaseOrder/$id/submit');

  Future<void> confirm(String id) => _patch('/PurchaseOrder/$id/confirm');

  /// Creates a goods receipt (PENDING) against a CONFIRMED/PARTIAL PO and
  /// returns the GRN id.
  Future<String> createGrn({
    required String purchaseOrderId,
    required String warehouseId,
    required DateTime receivedDate,
    required List<GrnDraftLine> lines,
    String? supplierInvoiceNumber,
  }) async {
    try {
      final hasInvoice =
          supplierInvoiceNumber != null && supplierInvoiceNumber.isNotEmpty;
      final res = await _dio.post('/PurchaseOrder/grn', data: {
        'purchaseOrderId': purchaseOrderId,
        'warehouseId': warehouseId,
        'receivedDate': receivedDate.toIso8601String(),
        'supplierInvoiceNumber': ?(hasInvoice ? supplierInvoiceNumber : null),
        'invoiceNotProvided': !hasInvoice,
        'lines': lines
            .map((l) => {
                  'partId': l.partId,
                  'purchaseOrderLineId': ?l.purchaseOrderLineId,
                  'receivedQuantity': l.receivedQuantity,
                  'unitCost': l.unitCost,
                  'currency': 'BDT',
                  'batchNumber': ?l.batchNumber,
                  'expiryDate': ?l.expiryDate?.toIso8601String(),
                })
            .toList(),
      });
      return asString((res.data as Map)['id']);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<void> verifyGrn(String id, {required String verifiedBy}) =>
      _patch('/PurchaseOrder/grn/$id/verify',
          query: {'verifiedBy': verifiedBy});

  /// Accepts a VERIFIED GRN — posts stock levels + lots atomically.
  Future<void> acceptGrn(String id) => _patch('/PurchaseOrder/grn/$id/accept');

  Future<List<Warehouse>> warehouses() async {
    try {
      final res = await _dio.post('/Warehouses/list',
          data: {'pageNumber': 1, 'pageSize': 100});
      final data = (res.data as Map<String, dynamic>)['data'];
      if (data is! List) return const [];
      return data
          .whereType<Map>()
          .map((e) => Warehouse.fromJson(Map<String, dynamic>.from(e)))
          .toList();
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }

  Future<void> _patch(String path, {Map<String, dynamic>? query}) async {
    try {
      await _dio.patch(path, queryParameters: query);
    } on DioException catch (e) {
      throw AppException.fromDio(e);
    }
  }
}

/// One line of a goods receipt being drafted.
class GrnDraftLine {
  const GrnDraftLine({
    required this.partId,
    required this.receivedQuantity,
    required this.unitCost,
    this.purchaseOrderLineId,
    this.batchNumber,
    this.expiryDate,
  });

  final String partId;
  final int receivedQuantity;
  final double unitCost;
  final String? purchaseOrderLineId;
  final String? batchNumber;
  final DateTime? expiryDate;
}

final purchaseOrdersRepositoryProvider = Provider<PurchaseOrdersRepository>(
  (ref) => PurchaseOrdersRepository(ref.read(dioProvider)),
);

/// Active warehouses for the receive flow's picker.
final warehousesProvider = FutureProvider.autoDispose<List<Warehouse>>(
  (ref) => ref.read(purchaseOrdersRepositoryProvider).warehouses(),
);
