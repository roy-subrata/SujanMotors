namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.CreditNote"/> (supplier credit note).</summary>
public enum CreditNoteStatus
{
    AVAILABLE,
    PARTIALLY_USED,
    FULLY_USED,
    EXPIRED,
    CANCELLED
}
