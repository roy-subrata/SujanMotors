namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks historical price changes for parts
/// </summary>
public class PriceHistory : AuditableEntity
{
    public Guid PartId { get; private set; }
    public decimal OldPrice { get; private set; }
    public decimal NewPrice { get; private set; }
    public DateTime EffectiveDate { get; private set; }
    public string Reason { get; private set; } = string.Empty;  // MARKET_ADJUSTMENT, SUPPLIER_CHANGE, PROMOTION, COST_INCREASE, etc.
    public string ChangedBy { get; private set; } = string.Empty;

    // Navigation properties
    public Part? Part { get; set; }

    private PriceHistory() { }

    public static PriceHistory Create(Guid partId, decimal oldPrice, decimal newPrice,
        DateTime? effectiveDate = null, string reason = "", string changedBy = "")
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (oldPrice < 0)
            throw new ArgumentException("OldPrice cannot be negative", nameof(oldPrice));

        if (newPrice < 0)
            throw new ArgumentException("NewPrice cannot be negative", nameof(newPrice));

        if (oldPrice == newPrice)
            throw new ArgumentException("NewPrice must be different from OldPrice", nameof(newPrice));

        return new PriceHistory
        {
            PartId = partId,
            OldPrice = oldPrice,
            NewPrice = newPrice,
            EffectiveDate = effectiveDate ?? DateTime.UtcNow,
            Reason = reason?.Trim() ?? string.Empty,
            ChangedBy = changedBy?.Trim() ?? string.Empty
        };
    }

    public decimal PriceDifference => NewPrice - OldPrice;
    public decimal PercentageChange => OldPrice == 0 ? 0 : (PriceDifference / OldPrice) * 100;
}
