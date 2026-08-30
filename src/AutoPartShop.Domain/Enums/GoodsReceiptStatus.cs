namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.GoodsReceipt"/> (GRN).
/// </summary>
public enum GoodsReceiptStatus
{
    PENDING,
    VERIFIED,
    ACCEPTED,
    REJECTED
}
