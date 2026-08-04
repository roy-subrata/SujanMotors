namespace AutoPartShop.Domain.Enums;

/// <summary>Lifecycle status of a <see cref="Entities.Challan"/>: DRAFT → ISSUED → DELIVERED.</summary>
public enum ChallanStatus
{
    DRAFT,
    ISSUED,
    DELIVERED
}
