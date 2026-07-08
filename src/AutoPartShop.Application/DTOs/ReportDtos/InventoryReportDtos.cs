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
