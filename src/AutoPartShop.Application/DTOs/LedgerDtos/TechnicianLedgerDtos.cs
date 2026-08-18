namespace AutoPartShop.Application.DTOs.LedgerDtos;

/// <summary>
/// Transaction types for technician ledger entries.
/// Tracks sales activity and payments for technicians with temporary payment responsibility.
/// </summary>
public enum TechnicianLedgerTransactionType
{
    /// <summary>Sale assigned to technician - increases what the technician owes (debit)</summary>
    SALE,

    /// <summary>Payment received from technician - decreases what the technician owes (credit)</summary>
    PAYMENT,

    /// <summary>Sales return on a technician's sale - decreases what the technician owes (credit)</summary>
    RETURN,

    /// <summary>Adjustment entry - can be debit or credit</summary>
    ADJUSTMENT
}

/// <summary>
/// Represents a single entry in the technician ledger
/// </summary>
public class TechnicianLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public TechnicianLedgerTransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public string ReferenceNumber { get; set; } = string.Empty;
    public Guid? ReferenceId { get; set; }
    public string? CustomerName { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary of technician ledger including totals and entries
/// </summary>
public class TechnicianLedgerSummaryDto
{
    public Guid TechnicianId { get; set; }
    public string TechnicianName { get; set; } = string.Empty;
    public string TechnicianCode { get; set; } = string.Empty;

    /// <summary>Total sales value from non-cancelled orders assigned to this technician</summary>
    public decimal TotalSales { get; set; }

    /// <summary>Total payments received against technician's sales</summary>
    public decimal TotalPayments { get; set; }

    /// <summary>Total value of returns on technician's sales</summary>
    public decimal TotalReturns { get; set; }

    /// <summary>Net balance: TotalSales - TotalPayments - TotalReturns</summary>
    public decimal CurrentBalance { get; set; }

    public int TransactionCount { get; set; }
    public int OrderCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public List<TechnicianLedgerEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Query parameters for fetching technician ledger entries
/// </summary>
public class TechnicianLedgerQueryDto
{
    public Guid TechnicianId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public TechnicianLedgerTransactionType? TransactionType { get; set; }
}

/// <summary>
/// Paginated result for technician ledger entries
/// </summary>
public class PagedTechnicianLedgerResult
{
    public List<TechnicianLedgerEntryDto> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
