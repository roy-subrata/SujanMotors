namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of an <see cref="Entities.Invoice"/>.</summary>
public enum InvoiceStatus
{
    DRAFT,
    ISSUED,
    DUE,
    PAID,
    PARTIALLY_PAID,
    OVERDUE,
    CANCELLED
}
