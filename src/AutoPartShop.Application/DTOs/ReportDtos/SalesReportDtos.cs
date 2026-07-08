namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>
/// One period bucket (day/week/month) of the sales summary report.
/// Amounts are raw sums in base currency (BDT); see ReportQuery currency note.
/// </summary>
public class SalesSummaryRowDto
{
    public DateTime PeriodStart { get; set; }
    public int OrderCount { get; set; }
    /// <summary>Sum of SubTotal — before discount and tax.</summary>
    public decimal GrossAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    /// <summary>Sum of TotalAmount — post-discount, pre-tax (the dashboard's accrual sales figure).</summary>
    public decimal NetAmount { get; set; }
    /// <summary>Sum of TotalAmount + TaxAmount.</summary>
    public decimal GrandTotal { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>One product row of the sales-by-product report.</summary>
public class SalesByProductRowDto
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public string Sku { get; set; } = "";
    public string? CategoryName { get; set; }
    public string? BrandName { get; set; }
    public int QuantitySold { get; set; }
    /// <summary>Σ Quantity × UnitPrice.</summary>
    public decimal GrossRevenue { get; set; }
    /// <summary>Σ Quantity × Discount (Discount is a per-unit flat amount).</summary>
    public decimal DiscountAmount { get; set; }
    /// <summary>GrossRevenue − DiscountAmount.</summary>
    public decimal NetRevenue { get; set; }
}
