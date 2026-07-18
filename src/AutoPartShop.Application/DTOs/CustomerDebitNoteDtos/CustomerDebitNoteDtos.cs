namespace AutoPartShop.Application.DTOs.CustomerDebitNoteDtos;

public class CreateCustomerDebitNoteRequest
{
    public Guid CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public string Notes { get; set; } = string.Empty;
}

public class CustomerDebitNoteResponse
{
    public Guid Id { get; set; }
    public string DebitNoteNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string IssuedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
