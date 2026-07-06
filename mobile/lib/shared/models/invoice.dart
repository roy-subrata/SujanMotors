import 'json.dart';

/// `InvoiceResponse` from `GET /api/v1/SalesOrder/invoices` — a customer invoice
/// header. The list endpoint returns no line items; lines are loaded on demand
/// from `/SalesOrder/invoices/{id}/print-data` (see [InvoiceLine]).
///
/// Note: the invoice API does not return a currency code, so amounts are
/// formatted with the app default (see `formatCurrency`).
class Invoice {
  const Invoice({
    required this.id,
    required this.invoiceNumber,
    required this.salesOrderNumber,
    required this.invoiceDate,
    required this.dueDate,
    required this.grandTotal,
    required this.amountPaid,
    required this.outstandingAmount,
    required this.isOverdue,
    this.currency,
    this.status,
  });

  final String id;
  final String invoiceNumber;
  final String salesOrderNumber;
  final DateTime invoiceDate;
  final DateTime? dueDate;
  final double grandTotal;
  final double amountPaid;
  final double outstandingAmount;
  final bool isOverdue;
  final String? currency;
  final String? status;

  factory Invoice.fromJson(Map<String, dynamic> json) => Invoice(
        id: asString(json['id']),
        invoiceNumber: asString(json['invoiceNumber']),
        salesOrderNumber: asString(json['salesOrderNumber']),
        invoiceDate:
            DateTime.tryParse(asString(json['invoiceDate']))?.toLocal() ??
                DateTime.now(),
        dueDate: DateTime.tryParse(asString(json['dueDate']))?.toLocal(),
        grandTotal: asDouble(json['grandTotal']),
        amountPaid: asDouble(json['amountPaid']),
        outstandingAmount: asDouble(json['outstandingAmount']),
        isOverdue: asBool(json['isOverdue']),
        currency: asStringOrNull(json['currency']),
        status: asStringOrNull(json['status']),
      );
}

/// A single invoice line, parsed from the `lines` array of the invoice
/// print-data payload. The backend already composes `displayName` as
/// "Part - Variant"; we fall back to `partName` then a generic label so the
/// product name never renders blank.
class InvoiceLine {
  const InvoiceLine({
    required this.displayName,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
    this.partId,
    this.partSku,
    this.variantName,
    this.unitSymbol,
  });

  final String displayName;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  final String? partId;
  final String? partSku;
  final String? variantName;
  final String? unitSymbol;

  factory InvoiceLine.fromJson(Map<String, dynamic> json) => InvoiceLine(
        displayName: asStringOrNull(json['displayName']) ??
            asStringOrNull(json['partName']) ??
            'Item',
        quantity: asInt(json['quantity']),
        unitPrice: asDouble(json['unitPrice']),
        lineTotal: asDouble(json['lineTotal']),
        partId: asStringOrNull(json['partId']),
        partSku: asStringOrNull(json['partSku']),
        variantName: asStringOrNull(json['variantName']),
        unitSymbol: asStringOrNull(json['unitSymbol']),
      );
}
