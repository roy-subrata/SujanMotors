namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.PurchaseReturn"/>.
/// </summary>
public enum PurchaseReturnStatus
{
    PENDING,
    APPROVED,
    RETURNED,
    RECEIVED,
    REJECTED,
    CREDITED
}
