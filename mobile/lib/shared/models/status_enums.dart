// Typed wrappers around the backend's status enums (see
// `src/AutoPartShop.Domain/Enums/` on the API side, and the Angular
// equivalent at `AutoPartShop.WebApp/src/app/shared/models/status.types.ts`).
//
// The wire format is unchanged by this refactor: every member's [wire]
// string is byte-for-byte identical to the corresponding C# enum member name
// (global `JsonStringEnumConverter`, no naming policy — SCREAMING_SNAKE_CASE
// unless noted otherwise). These types exist purely so the mobile app can
// stop comparing status fields as untyped strings.
//
// This app hand-parses JSON (no `json_serializable`/`freezed` codegen — see
// `shared/models/json.dart`), so each enum below ships a `fromWire` factory
// that defensively falls back to an `unknown` member for a null or
// unrecognized wire value. The mobile app ships independently of the
// backend and must never crash just because the server started returning a
// status it doesn't know about yet.
//
// When a value needs to round-trip back out to the server (e.g. as a query
// filter), use `.wire` — it always reproduces the exact original string,
// including for known members. `unknown.wire` is `''` and should never be
// sent to the server; callers filtering by status should omit the parameter
// instead when the enum value is `unknown`.

/// Implemented by every status enum below so shared UI/localization code
/// (see `S.statusName` in `core/i18n/strings.dart`) can render a label for
/// any of them without a separate switch per type.
abstract interface class WireStatus {
  /// The exact string this status serializes to/from on the wire.
  String get wire;
}

/// Broad semantic color bucket for a status, used by `StatusPill` in
/// `shared/widgets/design_system.dart` to pick a color from a typed status
/// instead of pattern-matching the (possibly localized) label text.
enum StatusKind { success, warning, danger, neutral }

// ── InvoiceStatus ────────────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.InvoiceStatus`

enum InvoiceStatus implements WireStatus {
  draft('DRAFT'),
  issued('ISSUED'),
  due('DUE'),
  paid('PAID'),
  partiallyPaid('PARTIALLY_PAID'),
  overdue('OVERDUE'),
  cancelled('CANCELLED'),

  /// Not a real backend value — the defensive fallback for a null/unrecognized
  /// wire string.
  unknown('');

  const InvoiceStatus(this.wire);

  @override
  final String wire;

  static InvoiceStatus fromWire(String? value) {
    if (value == null) return InvoiceStatus.unknown;
    for (final s in InvoiceStatus.values) {
      if (s.wire == value) return s;
    }
    return InvoiceStatus.unknown;
  }

  StatusKind get kind => switch (this) {
        InvoiceStatus.paid => StatusKind.success,
        InvoiceStatus.partiallyPaid => StatusKind.warning,
        InvoiceStatus.due ||
        InvoiceStatus.overdue ||
        InvoiceStatus.cancelled =>
          StatusKind.danger,
        InvoiceStatus.draft ||
        InvoiceStatus.issued ||
        InvoiceStatus.unknown =>
          StatusKind.neutral,
      };
}

// ── SalesReturnStatus ────────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.SalesReturnStatus`

enum SalesReturnStatus implements WireStatus {
  pending('PENDING'),
  approved('APPROVED'),
  received('RECEIVED'),
  rejected('REJECTED'),
  processed('PROCESSED'),
  unknown('');

  const SalesReturnStatus(this.wire);

  @override
  final String wire;

  static SalesReturnStatus fromWire(String? value) {
    if (value == null) return SalesReturnStatus.unknown;
    for (final s in SalesReturnStatus.values) {
      if (s.wire == value) return s;
    }
    return SalesReturnStatus.unknown;
  }

  /// A sales return always reads as a refund/loss event in the sales &
  /// customer lists (matching the pre-existing red "Return" pill on the
  /// sales list), regardless of which stage of the return workflow it's in.
  StatusKind get kind => StatusKind.danger;
}

// ── TillSessionStatus ────────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.TillSessionStatus`

enum TillSessionStatus implements WireStatus {
  open('OPEN'),
  closed('CLOSED'),
  unknown('');

  const TillSessionStatus(this.wire);

  @override
  final String wire;

  static TillSessionStatus fromWire(String? value) {
    if (value == null) return TillSessionStatus.unknown;
    for (final s in TillSessionStatus.values) {
      if (s.wire == value) return s;
    }
    return TillSessionStatus.unknown;
  }
}

// ── PurchaseOrderStatus ──────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.PurchaseOrderStatus`

enum PurchaseOrderStatus implements WireStatus {
  draft('DRAFT'),
  submitted('SUBMITTED'),
  confirmed('CONFIRMED'),
  partial('PARTIAL'),
  delivered('DELIVERED'),
  cancelled('CANCELLED'),
  unknown('');

  const PurchaseOrderStatus(this.wire);

  @override
  final String wire;

  static PurchaseOrderStatus fromWire(String? value) {
    if (value == null) return PurchaseOrderStatus.unknown;
    for (final s in PurchaseOrderStatus.values) {
      if (s.wire == value) return s;
    }
    return PurchaseOrderStatus.unknown;
  }

  StatusKind get kind => switch (this) {
        PurchaseOrderStatus.delivered => StatusKind.success,
        PurchaseOrderStatus.cancelled => StatusKind.danger,
        PurchaseOrderStatus.partial ||
        PurchaseOrderStatus.submitted ||
        PurchaseOrderStatus.confirmed =>
          StatusKind.warning,
        PurchaseOrderStatus.draft || PurchaseOrderStatus.unknown =>
          StatusKind.neutral,
      };
}

// ── CustomerPaymentStatus ────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.CustomerPaymentStatus`

enum CustomerPaymentStatus implements WireStatus {
  pending('PENDING'),
  processing('PROCESSING'),
  completed('COMPLETED'),
  failed('FAILED'),
  refunded('REFUNDED'),
  cancelled('CANCELLED'),
  unknown('');

  const CustomerPaymentStatus(this.wire);

  @override
  final String wire;

  static CustomerPaymentStatus fromWire(String? value) {
    if (value == null) return CustomerPaymentStatus.unknown;
    for (final s in CustomerPaymentStatus.values) {
      if (s.wire == value) return s;
    }
    return CustomerPaymentStatus.unknown;
  }

  StatusKind get kind => switch (this) {
        CustomerPaymentStatus.completed => StatusKind.success,
        CustomerPaymentStatus.failed || CustomerPaymentStatus.cancelled =>
          StatusKind.danger,
        CustomerPaymentStatus.pending || CustomerPaymentStatus.processing =>
          StatusKind.warning,
        CustomerPaymentStatus.refunded || CustomerPaymentStatus.unknown =>
          StatusKind.neutral,
      };
}

// ── CustomerStatus ───────────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.CustomerStatus`

enum CustomerStatus implements WireStatus {
  active('ACTIVE'),
  inactive('INACTIVE'),
  suspended('SUSPENDED'),
  blacklisted('BLACKLISTED'),
  unknown('');

  const CustomerStatus(this.wire);

  @override
  final String wire;

  static CustomerStatus fromWire(String? value) {
    if (value == null) return CustomerStatus.unknown;
    for (final s in CustomerStatus.values) {
      if (s.wire == value) return s;
    }
    return CustomerStatus.unknown;
  }
}

// ── SalesOrderStatus ─────────────────────────────────────────────────────────
// `AutoPartShop.Domain.Enums.SalesOrderStatus`
//
// Used only by `CustomerOrder.status` (the customer's parts-buying history,
// `GET /SalesOrder/customer/{id}`), which no screen currently renders.

enum SalesOrderStatus implements WireStatus {
  pending('PENDING'),
  draft('DRAFT'),
  confirmed('CONFIRMED'),
  readyForDelivery('READY_FOR_DELIVERY'),
  paid('PAID'),
  packed('PACKED'),
  partiallyShipped('PARTIALLY_SHIPPED'),
  shipped('SHIPPED'),
  delivered('DELIVERED'),
  completed('COMPLETED'),
  cancelled('CANCELLED'),
  returned('RETURNED'),
  unknown('');

  const SalesOrderStatus(this.wire);

  @override
  final String wire;

  static SalesOrderStatus fromWire(String? value) {
    if (value == null) return SalesOrderStatus.unknown;
    for (final s in SalesOrderStatus.values) {
      if (s.wire == value) return s;
    }
    return SalesOrderStatus.unknown;
  }
}
