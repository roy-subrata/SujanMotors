namespace AutoPartShop.Application.DTOs.LedgerDtos;

/// <summary>
/// Transaction types for supplier ledger entries
/// </summary>
public enum SupplierLedgerTransactionType
{
    /// <summary>Purchase order confirmation - increases debt (debit)</summary>
    PURCHASE,

    /// <summary>Supplier payment - decreases debt (credit)</summary>
    PAYMENT,

    /// <summary>Purchase return settlement - decreases debt (credit)</summary>
    REFUND,

    /// <summary>Advance payment application - decreases debt (credit)</summary>
    ADVANCE,

    /// <summary>Purchase order cancellation - decreases debt (reversal)</summary>
    CANCELLATION
}

/// <summary>
/// Represents a single entry in the supplier ledger
/// </summary>
public class SupplierLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public SupplierLedgerTransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public string ReferenceNumber { get; set; } = string.Empty;  // PO#, Payment TXN#, or Return#
    public Guid? ReferenceId { get; set; }  // Link to source entity
    public decimal DebitAmount { get; set; }     // Increases debt (purchases)
    public decimal CreditAmount { get; set; }    // Decreases debt (payments, refunds)
    public decimal RunningBalance { get; set; }  // Calculated running balance
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary of supplier ledger including totals and entries
/// </summary>
public class SupplierLedgerSummaryDto
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;

    /// <summary>Total amount from confirmed purchase orders</summary>
    public decimal TotalPurchases { get; set; }

    /// <summary>Total amount of completed payments</summary>
    public decimal TotalPayments { get; set; }

    /// <summary>Total amount of settled purchase returns</summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>Available advance credit (unused advance payments)</summary>
    public decimal AvailableAdvanceCredit { get; set; }

    /// <summary>Calculated current balance: TotalPurchases - TotalPayments - TotalRefunds</summary>
    public decimal CurrentBalance { get; set; }

    /// <summary>Total number of transactions</summary>
    public int TransactionCount { get; set; }

    /// <summary>Date of last transaction</summary>
    public DateTime? LastTransactionDate { get; set; }

    /// <summary>Ledger entries</summary>
    public List<SupplierLedgerEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Query parameters for fetching supplier ledger entries
/// </summary>
public class SupplierLedgerQueryDto
{
    public Guid SupplierId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public SupplierLedgerTransactionType? TransactionType { get; set; }
}

/// <summary>
/// Request to settle a purchase return
/// </summary>
public class SettlePurchaseReturnRequest
{
    public decimal Amount { get; set; }
    public string SettlementMethod { get; set; } = string.Empty;  // CREDIT, CASH, BANK_TRANSFER
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Paginated result for ledger entries
/// </summary>
public class PagedLedgerResult
{
    public List<SupplierLedgerEntryDto> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
