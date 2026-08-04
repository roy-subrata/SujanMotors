namespace AutoPartShop.Domain.Enums;

/// <summary>
/// Lifecycle status of a <see cref="Entities.Quotation"/>.
/// DRAFT → SENT → ACCEPTED → CONVERTED (to a SalesOrder), or SENT → REJECTED,
/// or any non-terminal state → EXPIRED once ValidUntil has passed.
/// </summary>
public enum QuotationStatus
{
    DRAFT,
    SENT,
    ACCEPTED,
    REJECTED,
    EXPIRED,
    CONVERTED
}
