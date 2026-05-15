namespace AutoPartShop.Application.DTOs.CustomerCreditNoteDtos;

/// <summary>
/// Response DTO for customer credit notes
/// </summary>
public class CustomerCreditNoteResponse
{
    public Guid Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? SalesReturnId { get; set; }
    public string? ReturnNumber { get; set; }
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public Guid? SalesOrderId { get; set; }
    public string? SalesOrderNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal UsedAmount { get; set; }
    public decimal AvailableAmount { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime IssueDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Status { get; set; } = "AVAILABLE";
    public string Notes { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for applying a customer credit note to an invoice
/// </summary>
public class ApplyCustomerCreditNoteRequest
{
    public Guid CreditNoteId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid SalesOrderId { get; set; }
    public decimal AmountToApply { get; set; }
}

/// <summary>
/// Query DTO for listing customer credit notes
/// </summary>
public class CustomerCreditNoteListQuery
{
    public Guid? CustomerId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
