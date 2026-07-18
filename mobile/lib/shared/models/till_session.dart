import 'json.dart';

/// A cashier's open/close till session, mirroring `TillSessionResponse` from
/// `GET/POST /api/v1/till-sessions/...`.
///
/// [cashSalesTotal], [cashRefundsTotal], [cashDropsTotal], [expectedAmount]
/// and [overShortAmount] are only computed by the server at close time (see
/// `TillSession.Close` in the domain entity) — while the session is still
/// OPEN they all read 0. Use [cashDropsRunningTotal] instead of
/// [cashDropsTotal] to show a live running total for an open session.
class TillSession {
  const TillSession({
    required this.id,
    required this.cashierId,
    required this.cashierName,
    required this.terminalLabel,
    this.shiftLabel,
    required this.openedAt,
    this.closedAt,
    required this.openingFloat,
    this.closingCountedAmount,
    required this.status,
    required this.cashSalesTotal,
    required this.cashRefundsTotal,
    required this.cashDropsTotal,
    required this.expectedAmount,
    required this.overShortAmount,
    required this.notes,
    this.cashDrops = const [],
  });

  final String id;
  final String cashierId;
  final String cashierName;
  final String terminalLabel;
  final String? shiftLabel;
  final DateTime openedAt;
  final DateTime? closedAt;
  final double openingFloat;
  final double? closingCountedAmount;
  final String status; // OPEN | CLOSED
  final double cashSalesTotal;
  final double cashRefundsTotal;
  final double cashDropsTotal;
  final double expectedAmount;
  final double overShortAmount;
  final String notes;
  final List<TillCashDrop> cashDrops;

  bool get isOpen => status.toUpperCase() == 'OPEN';
  bool get isClosed => status.toUpperCase() == 'CLOSED';

  /// Sum of this session's recorded cash drops, computed client-side. Prefer
  /// this over [cashDropsTotal] for an OPEN session — see class doc.
  double get cashDropsRunningTotal =>
      cashDrops.fold<double>(0, (sum, d) => sum + d.amount);

  factory TillSession.fromJson(Map<String, dynamic> json) => TillSession(
        id: asString(json['id']),
        cashierId: asString(json['cashierId']),
        cashierName: asString(json['cashierName']),
        terminalLabel: asString(json['terminalLabel']),
        shiftLabel: asStringOrNull(json['shiftLabel']),
        openedAt: DateTime.tryParse(asString(json['openedAt']))?.toLocal() ??
            DateTime.now(),
        closedAt: json['closedAt'] == null
            ? null
            : DateTime.tryParse(asString(json['closedAt']))?.toLocal(),
        openingFloat: asDouble(json['openingFloat']),
        closingCountedAmount: asDoubleOrNull(json['closingCountedAmount']),
        status: asString(json['status'], fallback: 'OPEN'),
        cashSalesTotal: asDouble(json['cashSalesTotal']),
        cashRefundsTotal: asDouble(json['cashRefundsTotal']),
        cashDropsTotal: asDouble(json['cashDropsTotal']),
        expectedAmount: asDouble(json['expectedAmount']),
        overShortAmount: asDouble(json['overShortAmount']),
        notes: asString(json['notes']),
        cashDrops: asList(json['cashDrops'], TillCashDrop.fromJson),
      );
}

/// Whether the current user's role requires an open till session before
/// starting a sale, mirroring `GET /api/v1/till-sessions/requires-open-session`.
///
/// [required] is opt-in per role (`sales.require-till-session` permission) —
/// most roles won't have it, in which case [hasOpenSession] is always
/// `false` and irrelevant. Only gate the UI when both [required] is `true`
/// and [hasOpenSession] is `false`.
class TillSessionRequirement {
  const TillSessionRequirement({
    required this.required,
    required this.hasOpenSession,
  });

  final bool required;
  final bool hasOpenSession;

  bool get blocksSale => required && !hasOpenSession;

  factory TillSessionRequirement.fromJson(Map<String, dynamic> json) =>
      TillSessionRequirement(
        required: asBool(json['required']),
        hasOpenSession: asBool(json['hasOpenSession']),
      );
}

/// Suggested defaults for the Open Till form. Mirrors
/// `GET /api/v1/till-sessions/suggested-opening-float`. UI hints only; every
/// Open Till field always stays editable.
///
/// [suggestedOpeningFloat] is scoped by TERMINAL, not cashier — the cash
/// physically sitting in a drawer is a fact about that drawer, not about
/// whoever counted it last. It's only populated when a `terminalLabel` was
/// passed to the request; [suggestedOpeningFloatFromCashier] names whoever
/// last closed a session on that terminal, for context.
///
/// [suggestedShiftLabel] IS a fact about the cashier (their HR-assigned
/// shift), so it's resolved from the current user regardless of terminal —
/// null when they have no linked Employee record or no ShiftId assigned.
class SuggestedOpeningFloat {
  const SuggestedOpeningFloat({
    this.suggestedOpeningFloat,
    this.suggestedOpeningFloatFromCashier,
    this.suggestedShiftLabel,
    this.suggestedShiftHours,
  });

  final double? suggestedOpeningFloat;
  final String? suggestedOpeningFloatFromCashier;
  final String? suggestedShiftLabel;
  final String? suggestedShiftHours;

  factory SuggestedOpeningFloat.fromJson(Map<String, dynamic> json) =>
      SuggestedOpeningFloat(
        suggestedOpeningFloat: asDoubleOrNull(json['suggestedOpeningFloat']),
        suggestedOpeningFloatFromCashier:
            asStringOrNull(json['suggestedOpeningFloatFromCashier']),
        suggestedShiftLabel: asStringOrNull(json['suggestedShiftLabel']),
        suggestedShiftHours: asStringOrNull(json['suggestedShiftHours']),
      );
}

/// One recorded cash drop against a [TillSession].
class TillCashDrop {
  const TillCashDrop({
    required this.id,
    required this.amount,
    required this.droppedAt,
    required this.notes,
  });

  final String id;
  final double amount;
  final DateTime droppedAt;
  final String notes;

  factory TillCashDrop.fromJson(Map<String, dynamic> json) => TillCashDrop(
        id: asString(json['id']),
        amount: asDouble(json['amount']),
        droppedAt:
            DateTime.tryParse(asString(json['droppedAt']))?.toLocal() ??
                DateTime.now(),
        notes: asString(json['notes']),
      );
}
