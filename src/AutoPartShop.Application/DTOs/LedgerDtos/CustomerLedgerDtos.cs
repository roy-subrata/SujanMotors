namespace AutoPartShop.Application.DTOs.LedgerDtos;

/// <summary>
/// Transaction types for customer ledger entries. Mirrors SupplierLedgerTransactionType's
/// shape from the customer's side of the relationship.
/// </summary>
public enum CustomerLedgerTransactionType
{
    /// <summary>Invoice issued - increases what the customer owes (debit)</summary>
    INVOICE,

    /// <summary>Customer payment - decreases what the customer owes (credit)</summary>
    PAYMENT,

    /// <summary>Advance payment - decreases what the customer owes (credit)</summary>
    ADVANCE,

    /// <summary>Processed sales return - decreases what the customer owes (credit)</summary>
    REFUND,

    /// <summary>Customer debit note - increases what the customer owes (debit)</summary>
    DEBIT_NOTE,

    /// <summary>Customer credit note applied - decreases what the customer owes (credit)</summary>
    CREDIT_NOTE
}

/// <summary>
/// Represents a single entry in the customer ledger
/// </summary>
public class CustomerLedgerEntryDto
{
    public Guid Id { get; set; }
    public DateTime TransactionDate { get; set; }
    public CustomerLedgerTransactionType TransactionType { get; set; }
    public string TransactionTypeName => TransactionType.ToString();
    public string ReferenceNumber { get; set; } = string.Empty;  // Invoice#, Payment TXN#, or Return#
    public Guid? ReferenceId { get; set; }
    public decimal DebitAmount { get; set; }     // Increases what's owed (invoices)
    public decimal CreditAmount { get; set; }    // Decreases what's owed (payments, refunds)
    public decimal RunningBalance { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Summary of customer ledger including totals and entries
/// </summary>
public class CustomerLedgerSummaryDto
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;

    /// <summary>Total amount from non-cancelled invoices</summary>
    public decimal TotalInvoiced { get; set; }

    /// <summary>Total amount of completed payments</summary>
    public decimal TotalPayments { get; set; }

    /// <summary>Total amount of processed sales returns</summary>
    public decimal TotalRefunds { get; set; }

    /// <summary>Total amount of issued debit notes</summary>
    public decimal TotalDebitNotes { get; set; }

    /// <summary>Total amount of credit notes applied against invoices</summary>
    public decimal TotalCreditNotesApplied { get; set; }

    /// <summary>Available advance credit (unused advance payments)</summary>
    public decimal AvailableAdvanceCredit { get; set; }

    /// <summary>Calculated current balance: TotalInvoiced - TotalPayments - TotalRefunds + TotalDebitNotes - TotalCreditNotesApplied</summary>
    public decimal CurrentBalance { get; set; }

    public int TransactionCount { get; set; }
    public DateTime? LastTransactionDate { get; set; }
    public List<CustomerLedgerEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Query parameters for fetching customer ledger entries
/// </summary>
public class CustomerLedgerQueryDto
{
    public Guid CustomerId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public CustomerLedgerTransactionType? TransactionType { get; set; }
}

/// <summary>
/// Paginated result for customer ledger entries
/// </summary>
public class PagedCustomerLedgerResult
{
    public List<CustomerLedgerEntryDto> Entries { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
