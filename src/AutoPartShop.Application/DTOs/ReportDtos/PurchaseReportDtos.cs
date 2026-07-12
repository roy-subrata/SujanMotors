namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>One period bucket of the purchase summary report. Excludes DRAFT/SUBMITTED/CANCELLED POs.</summary>
public class PurchaseSummaryRowDto
{
    public DateTime PeriodStart { get; set; }
    public int PoCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Outstanding { get; set; }
}

/// <summary>
/// One supplier row of the purchases-by-supplier report, scoped to the selected period
/// (not an all-time balance — see usp_Report_PurchasesBySupplier header for the exact rules).
/// </summary>
public class PurchasesBySupplierRowDto
{
    public Guid SupplierId { get; set; }
    public string SupplierCode { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public int PoCount { get; set; }
    public decimal TotalAmount { get; set; }
    /// <summary>Σ (ReceivedQuantity − DamagedQuantity − WrongQuantity) × UnitCost from ACCEPTED/VERIFIED GRNs.</summary>
    public decimal ReceivedValue { get; set; }
    public decimal PaidAmount { get; set; }
    /// <summary>Σ SettledAmount from SETTLED purchase returns.</summary>
    public decimal ReturnedValue { get; set; }
    /// <summary>TotalAmount − PaidAmount − ReturnedValue.</summary>
    public decimal Balance { get; set; }
}

/// <summary>One purchase return document row; Currency comes from the originating purchase order.</summary>
public class PurchaseReturnRowDto
{
    public DateTime ReturnDate { get; set; }
    public string ReturnNumber { get; set; } = "";
    public string PoNumber { get; set; } = "";
    public string SupplierName { get; set; } = "";
    public string Status { get; set; } = "";
    public string SettlementStatus { get; set; } = "";
    public decimal RefundAmount { get; set; }
    public string Currency { get; set; } = "";
}
