import 'json.dart';

/// The daily cash book from `GET /api/v1/cash-book/daily` (unwrapped `data`).
class CashBookDay {
  const CashBookDay({
    required this.from,
    required this.to,
    required this.openingBalance,
    required this.closingBalance,
    required this.totalCashIn,
    required this.totalActualCashIn,
    required this.totalCreditIn,
    required this.totalCashOut,
    required this.netCash,
    required this.entryCount,
    this.ledger = const [],
    this.breakdown = const [],
  });

  final String from;
  final String to;
  final double openingBalance;
  final double closingBalance;
  final double totalCashIn;
  final double totalActualCashIn;
  final double totalCreditIn;
  final double totalCashOut;
  final double netCash;
  final int entryCount;
  final List<CashLedgerRow> ledger;
  final List<CashMethodBreakdown> breakdown;

  factory CashBookDay.fromJson(Map<String, dynamic> json) => CashBookDay(
        from: asString(json['from']),
        to: asString(json['to']),
        openingBalance: asDouble(json['openingBalance']),
        closingBalance: asDouble(json['closingBalance']),
        totalCashIn: asDouble(json['totalCashIn']),
        totalActualCashIn: asDouble(json['totalActualCashIn']),
        totalCreditIn: asDouble(json['totalCreditIn']),
        totalCashOut: asDouble(json['totalCashOut']),
        netCash: asDouble(json['netCash']),
        entryCount: asInt(json['entryCount']),
        ledger: asList(json['ledger'], CashLedgerRow.fromJson),
        breakdown:
            asList(json['paymentMethodBreakdown'], CashMethodBreakdown.fromJson),
      );
}

/// One running-balance row of the day's ledger.
class CashLedgerRow {
  const CashLedgerRow({
    required this.time,
    required this.flow, // IN | OUT
    required this.type,
    required this.description,
    required this.balance,
    this.reference,
    this.paymentMethod,
    this.cashIn,
    this.cashOut,
    this.currency,
    this.category,
    this.vendor,
  });

  final DateTime time;
  final String flow;
  final String type;
  final String description;
  final double balance;
  final String? reference;
  final String? paymentMethod;
  final double? cashIn;
  final double? cashOut;
  final String? currency;
  final String? category;
  final String? vendor;

  bool get isIn => flow.toUpperCase() == 'IN';
  double get amount => isIn ? (cashIn ?? 0) : (cashOut ?? 0);

  factory CashLedgerRow.fromJson(Map<String, dynamic> json) => CashLedgerRow(
        time: DateTime.tryParse(asString(json['time']))?.toLocal() ??
            DateTime.now(),
        flow: asString(json['flow'], fallback: 'IN'),
        type: asString(json['type']),
        description: asString(json['description']),
        balance: asDouble(json['balance']),
        reference: asStringOrNull(json['reference']),
        paymentMethod: asStringOrNull(json['paymentMethod']),
        cashIn: asDoubleOrNull(json['cashIn']),
        cashOut: asDoubleOrNull(json['cashOut']),
        currency: asStringOrNull(json['currency']),
        category: asStringOrNull(json['category']),
        vendor: asStringOrNull(json['vendor']),
      );
}

/// Per-payment-method in/out/net totals for the day.
class CashMethodBreakdown {
  const CashMethodBreakdown({
    required this.method,
    required this.cashIn,
    required this.cashOut,
    required this.net,
  });

  final String method;
  final double cashIn;
  final double cashOut;
  final double net;

  factory CashMethodBreakdown.fromJson(Map<String, dynamic> json) =>
      CashMethodBreakdown(
        method: asString(json['method'], fallback: 'Other'),
        cashIn: asDouble(json['in']),
        cashOut: asDouble(json['out']),
        net: asDouble(json['net']),
      );
}
