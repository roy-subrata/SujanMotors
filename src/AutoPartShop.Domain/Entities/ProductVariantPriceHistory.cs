namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Time-based selling price for a Part or ProductVariant.
///
/// ProductVariantId = null  → base product price (applies to the Part itself)
/// ProductVariantId = value → variant-specific price (overrides base product price)
///
/// Resolution at sale time:
///   1. Active price WHERE PartId = X AND VariantId = Y  (variant-specific)
///   2. Active price WHERE PartId = X AND VariantId = null (base product)
///   3. Part.SellingPrice (last resort)
/// </summary>
public class ProductVariantPriceHistory : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }  // null = base product price
    public decimal SellingPrice { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }       // null = currently active
    public string? Reason { get; private set; }

    public Part? Part { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    private ProductVariantPriceHistory() { }

    public static ProductVariantPriceHistory Create(
        Guid partId,
        decimal sellingPrice,
        DateTime startDate,
        Guid? productVariantId = null,
        string currency = "BDT",
        string? reason = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (sellingPrice <= 0)
            throw new ArgumentException("SellingPrice must be greater than 0", nameof(sellingPrice));

        return new ProductVariantPriceHistory
        {
            PartId = partId,
            ProductVariantId = productVariantId,
            SellingPrice = sellingPrice,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper(),
            StartDate = startDate.Date,
            EndDate = null,
            Reason = reason?.Trim()
        };
    }

    /// <summary>Closes this price record. Called before inserting a new price.</summary>
    public void Close(DateTime endDate)
    {
        if (EndDate.HasValue)
            throw new InvalidOperationException("Price record is already closed");

        if (endDate.Date < StartDate)
            throw new ArgumentException("EndDate cannot be before StartDate", nameof(endDate));

        EndDate = endDate.Date;
    }

    public bool IsActiveOn(DateTime date) =>
        date.Date >= StartDate && (!EndDate.HasValue || date.Date <= EndDate.Value);

    public bool IsCurrentlyActive => !EndDate.HasValue;
}
