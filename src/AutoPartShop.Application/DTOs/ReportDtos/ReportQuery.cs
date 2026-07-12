using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>
/// Shared query for every report endpoint. Each report reads only the filters that apply to it
/// and ignores the rest, so the frontend can post one uniform payload from the generic report page.
/// Amounts returned by report procedures are aggregated in base currency (BDT) — SQL-side grouping
/// prevents per-row exchange-rate conversion, the same simplification the dashboard sales trend makes.
/// </summary>
public class ReportQuery : BaseQuery
{
    /// <summary>Inclusive range start. Required by date-ranged reports (400 when missing).</summary>
    public DateTime? FromDate { get; set; }

    /// <summary>Inclusive range end. Required by date-ranged reports (400 when missing).</summary>
    public DateTime? ToDate { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? SupplierId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? PartId { get; set; }

    /// <summary>Period bucket for summary reports: day | week | month.</summary>
    public string? GroupBy { get; set; }

    /// <summary>Sales channel filter (POS, ECOMMERCE, ...).</summary>
    public string? Channel { get; set; }

    /// <summary>StockMovement type filter (IN, OUT, RETURN, ADJUST, TRANSFER).</summary>
    public string? MovementType { get; set; }

    public string? PaymentMethod { get; set; }
    public string? CustomerType { get; set; }
    public string? ExpenseCategory { get; set; }

    /// <summary>Expiring-lots horizon; lots expiring within this many days are included.</summary>
    public int? DaysAhead { get; set; }

    /// <summary>Slow-moving threshold; parts with no sale for at least this many days are included.</summary>
    public int? NoSaleDays { get; set; }

    /// <summary>Snapshot date for aging reports; defaults to today when omitted.</summary>
    public DateTime? AsOfDate { get; set; }

    public bool IncludeZeroStock { get; set; }
    public bool IncludeExpired { get; set; }
}
