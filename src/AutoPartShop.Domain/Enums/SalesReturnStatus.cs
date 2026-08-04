namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.SalesReturn"/>.</summary>
public enum SalesReturnStatus
{
    PENDING,
    APPROVED,
    RECEIVED,
    REJECTED,
    PROCESSED
}
