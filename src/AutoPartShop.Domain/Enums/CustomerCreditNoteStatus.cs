namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.CustomerCreditNote"/>.</summary>
public enum CustomerCreditNoteStatus
{
    AVAILABLE,
    PARTIALLY_USED,
    FULLY_USED,
    EXPIRED,
    CANCELLED
}
