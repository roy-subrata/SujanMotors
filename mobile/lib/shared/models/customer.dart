import 'json.dart';

/// `CustomerResponse` from `/api/v1/customers` and `/api/v1/customers/{id}`.
class Customer {
  const Customer({
    required this.id,
    required this.customerCode,
    required this.fullName,
    this.firstName,
    this.lastName,
    this.companyName,
    this.email,
    this.phone,
    this.alternatePhone,
    this.city,
    this.customerType,
    this.status,
    this.currentBalance = 0,
    this.advanceAmount = 0,
    this.dueAmount = 0,
    this.totalPurchaseAmount = 0,
    this.lastPurchaseDate,
  });

  final String id;
  final String customerCode;
  final String fullName;
  final String? firstName;
  final String? lastName;
  final String? companyName;
  final String? email;
  final String? phone;
  final String? alternatePhone;
  final String? city;
  final String? customerType;
  final String? status;
  final double currentBalance;
  final double advanceAmount;
  final double dueAmount;
  final double totalPurchaseAmount;
  final DateTime? lastPurchaseDate;

  bool get hasDue => dueAmount > 0;

  factory Customer.fromJson(Map<String, dynamic> json) => Customer(
        id: asString(json['id']),
        customerCode: asString(json['customerCode']),
        fullName: asString(json['fullName']),
        firstName: asStringOrNull(json['firstName']),
        lastName: asStringOrNull(json['lastName']),
        companyName: asStringOrNull(json['companyName']),
        email: asStringOrNull(json['email']),
        phone: asStringOrNull(json['phone']),
        alternatePhone: asStringOrNull(json['alternatePhone']),
        city: asStringOrNull(json['city']),
        customerType: asStringOrNull(json['customerType']),
        status: asStringOrNull(json['status']),
        currentBalance: asDouble(json['currentBalance']),
        advanceAmount: asDouble(json['advanceAmount']),
        dueAmount: asDouble(json['dueAmount']),
        totalPurchaseAmount: asDouble(json['totalPurchaseAmount']),
        lastPurchaseDate:
            DateTime.tryParse(asString(json['lastPurchaseDate']))?.toLocal(),
      );
}

/// `CustomerPaymentHistorySummary` from
/// `/api/v1/customer-payments/customer/{id}/summary`.
class CustomerPaymentSummary {
  const CustomerPaymentSummary({
    required this.totalPaid,
    required this.amountDue,
    required this.totalInvoiceAmount,
    required this.availableAdvance,
    this.totalInvoices = 0,
    this.unpaidInvoices = 0,
    this.overdueInvoices = 0,
    this.lastPaymentDate,
    this.lastPaymentAmount = 0,
    this.history = const [],
  });

  final double totalPaid;
  final double amountDue;
  final double totalInvoiceAmount;
  final double availableAdvance;
  final int totalInvoices;
  final int unpaidInvoices;
  final int overdueInvoices;
  final DateTime? lastPaymentDate;
  final double lastPaymentAmount;
  final List<PaymentHistoryItem> history;

  factory CustomerPaymentSummary.fromJson(Map<String, dynamic> json) =>
      CustomerPaymentSummary(
        totalPaid: asDouble(json['totalPaid']),
        amountDue: asDouble(json['amountDue']),
        totalInvoiceAmount: asDouble(json['totalInvoiceAmount']),
        availableAdvance: asDouble(json['availableAdvance']),
        totalInvoices: asInt(json['totalInvoices']),
        unpaidInvoices: asInt(json['unpaidInvoices']),
        overdueInvoices: asInt(json['overdueInvoices']),
        lastPaymentDate:
            DateTime.tryParse(asString(json['lastPaymentDate']))?.toLocal(),
        lastPaymentAmount: asDouble(json['lastPaymentAmount']),
        history: asList(json['paymentHistory'], PaymentHistoryItem.fromJson),
      );
}

class PaymentHistoryItem {
  const PaymentHistoryItem({
    required this.amount,
    required this.paymentDate,
    this.status,
    this.paymentMethod,
    this.invoiceNumber,
    this.providerName,
  });

  final double amount;
  final DateTime paymentDate;
  final String? status;
  final String? paymentMethod;
  final String? invoiceNumber;
  final String? providerName;

  factory PaymentHistoryItem.fromJson(Map<String, dynamic> json) =>
      PaymentHistoryItem(
        amount: asDouble(json['amount']),
        paymentDate:
            DateTime.tryParse(asString(json['paymentDate']))?.toLocal() ??
                DateTime.now(),
        status: asStringOrNull(json['status']),
        paymentMethod: asStringOrNull(json['paymentMethod']),
        invoiceNumber: asStringOrNull(json['invoiceNumber']),
        providerName: asStringOrNull(json['providerName']),
      );
}

/// `SaleOrderResponse` from `/api/v1/SalesOrder/customer/{id}` — the customer's
/// parts-buying history.
class CustomerOrder {
  const CustomerOrder({
    required this.id,
    required this.soNumber,
    required this.orderDate,
    required this.grandTotal,
    required this.amountPaid,
    required this.outstandingAmount,
    this.currency,
    this.status,
    this.lines = const [],
  });

  final String id;
  final String soNumber;
  final DateTime orderDate;
  final double grandTotal;
  final double amountPaid;
  final double outstandingAmount;
  final String? currency;
  final String? status;
  final List<CustomerOrderLine> lines;

  int get itemCount => lines.fold(0, (sum, l) => sum + l.quantity);

  factory CustomerOrder.fromJson(Map<String, dynamic> json) => CustomerOrder(
        id: asString(json['id']),
        soNumber: asString(json['soNumber']),
        orderDate: DateTime.tryParse(asString(json['orderDate']))?.toLocal() ??
            DateTime.now(),
        grandTotal: asDouble(json['grandTotal']),
        amountPaid: asDouble(json['amountPaid']),
        outstandingAmount: asDouble(json['outstandingAmount']),
        currency: asStringOrNull(json['currency']),
        status: asStringOrNull(json['status']),
        lines: asList(json['lines'], CustomerOrderLine.fromJson),
      );
}

class CustomerOrderLine {
  const CustomerOrderLine({
    required this.displayName,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
    this.unitSymbol,
    this.partSku,
  });

  final String displayName;
  final int quantity;
  final double unitPrice;
  final double lineTotal;
  final String? unitSymbol;
  final String? partSku;

  factory CustomerOrderLine.fromJson(Map<String, dynamic> json) =>
      CustomerOrderLine(
        displayName: asString(
          json['displayName'] ?? json['partName'],
          fallback: 'Item',
        ),
        quantity: asInt(json['quantity']),
        unitPrice: asDouble(json['unitPrice']),
        lineTotal: asDouble(json['lineTotal']),
        unitSymbol: asStringOrNull(json['unitSymbol']),
        partSku: asStringOrNull(json['partSku']),
      );
}

// ── Account Statement ─────────────────────────────────────────────────────────

class CustomerAccountSummary {
  const CustomerAccountSummary({
    required this.customerName,
    required this.customerCode,
    this.fromDate,
    this.toDate,
    required this.totalPurchaseAmount,
    required this.totalPaidAmount,
    required this.currentDue,
    required this.totalInvoices,
    required this.purchaseItems,
    required this.purchaseItemsTotalCount,
    required this.purchaseItemsTotalPages,
  });

  final String customerName;
  final String customerCode;
  final DateTime? fromDate;
  final DateTime? toDate;
  final double totalPurchaseAmount;
  final double totalPaidAmount;
  final double currentDue;
  final int totalInvoices;
  final List<CustomerPurchaseItem> purchaseItems;
  final int purchaseItemsTotalCount;
  final int purchaseItemsTotalPages;

  factory CustomerAccountSummary.fromJson(Map<String, dynamic> json) =>
      CustomerAccountSummary(
        customerName: asString(json['customerName']),
        customerCode: asString(json['customerCode']),
        fromDate: DateTime.tryParse(asString(json['fromDate']))?.toLocal(),
        toDate: DateTime.tryParse(asString(json['toDate']))?.toLocal(),
        totalPurchaseAmount: asDouble(json['totalPurchaseAmount']),
        totalPaidAmount: asDouble(json['totalPaidAmount']),
        currentDue: asDouble(json['currentDue']),
        totalInvoices: asInt(json['totalInvoices']),
        purchaseItems:
            asList(json['purchaseItems'], CustomerPurchaseItem.fromJson),
        purchaseItemsTotalCount: asInt(json['purchaseItemsTotalCount']),
        purchaseItemsTotalPages: asInt(json['purchaseItemsTotalPages']),
      );
}

class CustomerPurchaseItem {
  const CustomerPurchaseItem({
    required this.invoiceDate,
    required this.invoiceNumber,
    required this.invoiceStatus,
    this.vehicleLabel,
    required this.itemName,
    this.itemLocalName,
    required this.sku,
    required this.quantity,
    required this.unitPrice,
    required this.lineTotal,
  });

  final DateTime invoiceDate;
  final String invoiceNumber;
  final String invoiceStatus;
  final String? vehicleLabel;
  final String itemName;
  final String? itemLocalName;
  final String sku;
  final int quantity;
  final double unitPrice;
  final double lineTotal;

  factory CustomerPurchaseItem.fromJson(Map<String, dynamic> json) =>
      CustomerPurchaseItem(
        invoiceDate:
            DateTime.tryParse(asString(json['invoiceDate']))?.toLocal() ??
                DateTime.now(),
        invoiceNumber: asString(json['invoiceNumber']),
        invoiceStatus: asString(json['invoiceStatus']),
        vehicleLabel: asStringOrNull(json['vehicleLabel']),
        itemName: asString(json['itemName']),
        itemLocalName: asStringOrNull(json['itemLocalName']),
        sku: asString(json['sku']),
        quantity: asInt(json['quantity']),
        unitPrice: asDouble(json['unitPrice']),
        lineTotal: asDouble(json['lineTotal']),
      );
}
