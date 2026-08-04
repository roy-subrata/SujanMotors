namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.PurchaseOrder"/>.
/// </summary>
public enum PurchaseOrderStatus
{
    DRAFT,
    SUBMITTED,
    CONFIRMED,
    PARTIAL,
    DELIVERED,
    CANCELLED
}
