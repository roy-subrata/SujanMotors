namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>
/// One stock row per (part, variant, warehouse) of the stock summary &amp; valuation report.
/// Valuation comes from AVAILABLE stock lots (actual purchase cost, base currency).
/// </summary>
public class StockSummaryRowDto
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? VariantName { get; set; }
    public string? CategoryName { get; set; }
    public string WarehouseName { get; set; } = "";
    public int QuantityOnHand { get; set; }
    public int QuantityReserved { get; set; }
    public int QuantityDamaged { get; set; }
    public int QuantityAvailable { get; set; }
    /// <summary>Weighted average lot cost of the AVAILABLE quantity; null when no available lots.</summary>
    public decimal? AverageCost { get; set; }
    /// <summary>Σ QuantityAvailable × CostPrice over AVAILABLE lots for this row's part/variant/warehouse.</summary>
    public decimal StockValue { get; set; }
}

/// <summary>Grand totals over the whole filtered stock summary, not just the current page.</summary>
public class StockSummaryTotalsDto
{
    public int RowCount { get; set; }
    public int DistinctPartCount { get; set; }
    public int TotalQuantityOnHand { get; set; }
    public decimal TotalStockValue { get; set; }
}

/// <summary>One low-stock row: MinimumStock > 0 (opt-in threshold) and on-hand at or below it — the dashboard's rule.</summary>
public class LowStockRowDto
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? VariantName { get; set; }
    public string? CategoryName { get; set; }
    public string WarehouseName { get; set; } = "";
    public int QuantityOnHand { get; set; }
    public int MinimumStock { get; set; }
    public int ReorderLevel { get; set; }
    public int ReorderQuantity { get; set; }
    /// <summary>MinimumStock − QuantityOnHand.</summary>
    public int Shortfall { get; set; }
}

/// <summary>One stock movement ledger row (level-based audit trail).</summary>
public class StockMovementRowDto
{
    public DateTime MovementDate { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string? VariantName { get; set; }
    public string WarehouseName { get; set; } = "";
    public string MovementType { get; set; } = "";
    public int Quantity { get; set; }
    public string Reason { get; set; } = "";
    public string ReferenceNumber { get; set; } = "";
}

/// <summary>One AVAILABLE lot expiring within the daysAhead horizon; negative DaysToExpiry = already expired.</summary>
public class ExpiringLotRowDto
{
    public string LotNumber { get; set; } = "";
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string WarehouseName { get; set; } = "";
    public string? SupplierName { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysToExpiry { get; set; }
    public int QuantityAvailable { get; set; }
    public decimal StockValue { get; set; }
}

/// <summary>One slow-moving/dead stock row; null LastSaleDate means the part never sold from this warehouse.</summary>
public class SlowMovingStockRowDto
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string? CategoryName { get; set; }
    public string WarehouseName { get; set; } = "";
    public int QuantityOnHand { get; set; }
    public decimal StockValue { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public int? DaysSinceLastSale { get; set; }
}
