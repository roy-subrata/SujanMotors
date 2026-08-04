namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.SalesOrder"/>.
/// Primary flow: PENDING → CONFIRMED → DELIVERED (direct handover, invoice only)
///           or: PENDING → CONFIRMED → READY_FOR_DELIVERY → DELIVERED (later delivery, invoice + challan)
/// Legacy statuses retained for backward compat: DRAFT, PAID, PACKED, SHIPPED, PARTIALLY_SHIPPED, COMPLETED, RETURNED.
/// Member names are serialized as-is (see global JsonStringEnumConverter with no naming policy) and
/// must match the historical string literals exactly.
/// </summary>
public enum SalesOrderStatus
{
    PENDING,
    DRAFT,
    CONFIRMED,
    READY_FOR_DELIVERY,
    PAID,
    PACKED,
    PARTIALLY_SHIPPED,
    SHIPPED,
    DELIVERED,
    COMPLETED,
    CANCELLED,
    RETURNED
}
