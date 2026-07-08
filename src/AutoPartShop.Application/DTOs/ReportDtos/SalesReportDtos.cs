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

/// <summary>One category slice of the sales-by-category report; parts without a category group as "Uncategorized".</summary>
public class SalesByCategoryRowDto
{
    public string CategoryName { get; set; } = "";
    public int OrderCount { get; set; }
    public int QuantitySold { get; set; }
    public decimal NetRevenue { get; set; }
    /// <summary>Share of total net revenue across the whole result, 0–100.</summary>
    public decimal PercentOfTotal { get; set; }
}

/// <summary>One customer row of the sales-by-customer report.</summary>
public class SalesByCustomerRowDto
{
    public Guid CustomerId { get; set; }
    public string CustomerCode { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string CustomerType { get; set; } = "";
    public int OrderCount { get; set; }
    /// <summary>Σ TotalAmount + TaxAmount (invoice value incl. tax).</summary>
    public decimal Revenue { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
    public DateTime LastPurchaseDate { get; set; }
}

/// <summary>One salesperson row; orders without a technician report as "Unassigned".</summary>
public class SalesBySalespersonRowDto
{
    public string TechnicianName { get; set; } = "";
    public int OrderCount { get; set; }
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
    public decimal AverageOrderValue { get; set; }
}

/// <summary>One sales return document row; Currency comes from the originating sales order.</summary>
public class SalesReturnRowDto
{
    public DateTime ReturnDate { get; set; }
    public string ReturnNumber { get; set; } = "";
    public string SoNumber { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Status { get; set; } = "";
    public string RefundType { get; set; } = "";
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "";
    public string Reason { get; set; } = "";
}

/// <summary>
/// One bucket of the payment collections report — COMPLETED customer payments with advance
/// re-applications excluded (cash-basis, same rule as the dashboard). GroupKey is a
/// yyyy-MM-dd date or a payment method depending on the groupBy filter.
/// </summary>
public class PaymentCollectionRowDto
{
    public string GroupKey { get; set; } = "";
    public int PaymentCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>One product row of the profit-by-product report; COGS from stock lot movements (actual cost).</summary>
public class ProfitByProductRowDto
{
    public Guid PartId { get; set; }
    public string PartNumber { get; set; } = "";
    public string PartName { get; set; } = "";
    public int QuantitySold { get; set; }
    public decimal NetRevenue { get; set; }
    /// <summary>Σ SALE lot-movement cost minus RETURN reversals, base currency.</summary>
    public decimal Cogs { get; set; }
    public decimal GrossProfit { get; set; }
    /// <summary>GrossProfit / NetRevenue × 100; null when revenue is zero.</summary>
    public decimal? MarginPercent { get; set; }
}
