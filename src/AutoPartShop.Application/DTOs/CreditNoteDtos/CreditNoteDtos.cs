namespace AutoPartShop.Application.DTOs.CreditNoteDtos;

/// <summary>
/// Response DTO for credit notes
/// </summary>
public class CreditNoteResponse
{
    public Guid Id { get; set; }
    public string CreditNoteNumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? PurchaseReturnId { get; set; }
    public string? ReturnNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
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
/// DTO for applying a credit note to a purchase order
/// </summary>
public class ApplyCreditNoteRequest
{
    public Guid CreditNoteId { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public decimal AmountToApply { get; set; }
}

/// <summary>
/// Query DTO for listing credit notes
/// </summary>
public class CreditNoteListQuery
{
    public Guid? SupplierId { get; set; }
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
