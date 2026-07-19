namespace AutoPartShop.Domain.Entities;

/// <summary>
/// One item to count in a stock take. ExpectedQuantity is the base-unit on-hand quantity
/// snapshotted when the stock take was created; the counter records only CountedQuantity.
/// Variance (counted − expected) is applied as a delta adjustment on approval, so sales that
/// happen between snapshot and approval are not double-counted.
/// Part identity fields are denormalized so the document stays readable if parts are renamed.
/// </summary>
public class StockTakeLine : AuditableEntity
{
    public Guid StockTakeId { get; private set; }
    public Guid StockLevelId { get; private set; }  // The level this line snapshots and adjusts
    public Guid PartId { get; private set; }
    public Guid? VariantId { get; private set; }
    public string PartName { get; private set; } = string.Empty;    // Denormalized at snapshot
    public string PartCode { get; private set; } = string.Empty;    // Part number / SKU at snapshot
    public string VariantName { get; private set; } = string.Empty; // Variant label at snapshot ("" = part-level)
    public string Location { get; private set; } = string.Empty;    // Shelf/bin from the stock level, for count sheets
    public int ExpectedQuantity { get; private set; }   // Base units, snapshot of QuantityOnHandInBaseUnit
    public int? CountedQuantity { get; private set; }   // Base units; null = not counted yet
    public decimal UnitCost { get; private set; }       // Base-unit lot cost at snapshot — values the variance
    public string CountedBy { get; private set; } = string.Empty;
    public DateTime? CountedAt { get; private set; }
    public string Notes { get; private set; } = string.Empty;

    /// <summary>counted − expected; null while uncounted. Negative = shrinkage.</summary>
    public int? Variance => CountedQuantity.HasValue ? CountedQuantity.Value - ExpectedQuantity : null;

    // Navigation properties
    public StockTake? StockTake { get; set; }
    public Product? Part { get; set; }
    public ProductVariant? Variant { get; set; }

    private StockTakeLine() { }

    public static StockTakeLine Create(Guid stockTakeId, Guid stockLevelId, Guid partId, Guid? variantId,
        string partName, string partCode, string variantName, string location,
        int expectedQuantity, decimal unitCost)
    {
        if (stockTakeId == Guid.Empty)
            throw new ArgumentException("StockTakeId cannot be empty", nameof(stockTakeId));

        if (stockLevelId == Guid.Empty)
            throw new ArgumentException("StockLevelId cannot be empty", nameof(stockLevelId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (expectedQuantity < 0)
            throw new ArgumentException("ExpectedQuantity cannot be negative", nameof(expectedQuantity));

        if (unitCost < 0)
            throw new ArgumentException("UnitCost cannot be negative", nameof(unitCost));

        return new StockTakeLine
        {
            StockTakeId = stockTakeId,
            StockLevelId = stockLevelId,
            PartId = partId,
            VariantId = variantId,
            PartName = partName?.Trim() ?? string.Empty,
            PartCode = partCode?.Trim() ?? string.Empty,
            VariantName = variantName?.Trim() ?? string.Empty,
            Location = location?.Trim() ?? string.Empty,
            ExpectedQuantity = expectedQuantity,
            UnitCost = unitCost
        };
    }

    public void RecordCount(int countedQuantity, string countedBy, string notes = "")
    {
        if (countedQuantity < 0)
            throw new ArgumentException("Counted quantity cannot be negative", nameof(countedQuantity));

        if (string.IsNullOrWhiteSpace(countedBy))
            throw new ArgumentException("CountedBy cannot be empty", nameof(countedBy));

        CountedQuantity = countedQuantity;
        CountedBy = countedBy.Trim();
        CountedAt = DateTime.UtcNow;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void ClearCount()
    {
        CountedQuantity = null;
        CountedBy = string.Empty;
        CountedAt = null;
    }
}
