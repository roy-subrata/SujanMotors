import 'json.dart';

class SalesReturn {
  const SalesReturn({
    required this.id,
    required this.returnNumber,
    required this.salesOrderId,
    this.salesOrderNumber,
    required this.reason,
    required this.status,
    required this.totalRefundAmount,
    required this.refundType,
    required this.createdAt,
    this.lines = const [],
  });

  final String id;
  final String returnNumber;
  final String salesOrderId;
  final String? salesOrderNumber;
  final String reason;
  final String status;
  final double totalRefundAmount;
  final String refundType;
  final DateTime createdAt;
  final List<SalesReturnLine> lines;

  factory SalesReturn.fromJson(Map<String, dynamic> json) => SalesReturn(
        id: asString(json['id']),
        returnNumber: asString(json['returnNumber']),
        salesOrderId: asString(json['salesOrderId']),
        salesOrderNumber: asStringOrNull(json['salesOrderNumber']),
        reason: asString(json['reason']),
        status: asString(json['status']),
        totalRefundAmount: asDouble(json['totalRefundAmount']),
        refundType: asString(json['refundType']),
        createdAt:
            DateTime.tryParse(asString(json['createdAt']))?.toLocal() ??
                DateTime.now(),
        lines: asList(json['lines'], SalesReturnLine.fromJson),
      );
}

class SalesReturnLine {
  const SalesReturnLine({
    required this.id,
    required this.partId,
    required this.displayName,
    this.partSku,
    required this.quantity,
    required this.refundAmount,
    required this.condition,
    this.unitSymbol,
  });

  final String id;
  final String partId;
  final String displayName;
  final String? partSku;
  final int quantity;
  final double refundAmount;
  final String condition;
  final String? unitSymbol;

  factory SalesReturnLine.fromJson(Map<String, dynamic> json) =>
      SalesReturnLine(
        id: asString(json['id']),
        partId: asString(json['partId']),
        displayName: asStringOrNull(json['displayName']) ??
            asStringOrNull(json['partName']) ??
            'Item',
        partSku: asStringOrNull(json['partSku']),
        quantity: asInt(json['quantity']),
        refundAmount: asDouble(json['refundAmount']),
        condition: asStringOrNull(json['condition']) ?? 'UNOPENED',
        unitSymbol: asStringOrNull(json['unitSymbol']),
      );
}

/// A single item in a quick-return submission.
class QuickReturnItem {
  const QuickReturnItem({
    required this.partId,
    required this.quantity,
    this.reason,
    this.salesOrderLineId,
  });

  final String partId;
  final int quantity;
  final String? reason;
  final String? salesOrderLineId;

  Map<String, dynamic> toJson() => {
        'partId': partId,
        'quantity': quantity,
        if (reason != null && reason!.isNotEmpty) 'reason': reason,
        if (salesOrderLineId != null) 'salesOrderLineId': salesOrderLineId,
      };
}

class QuickReturnResult {
  const QuickReturnResult({
    required this.returnNumber,
    required this.refundAmount,
    required this.message,
  });

  final String returnNumber;
  final double refundAmount;
  final String message;

  factory QuickReturnResult.fromJson(Map<String, dynamic> json) {
    final d = json['data'] is Map
        ? Map<String, dynamic>.from(json['data'] as Map)
        : json;
    return QuickReturnResult(
      returnNumber: asStringOrNull(d['returnNumber']) ?? '',
      refundAmount: asDouble(d['refundAmount']),
      message: asStringOrNull(json['message']) ?? 'Return submitted',
    );
  }
}
