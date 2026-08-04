namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Payment status of a <see cref="Entities.PurchaseOrder"/>.
/// </summary>
public enum PurchaseOrderPaymentStatus
{
    PENDING,
    PARTIAL,
    PAID
}
