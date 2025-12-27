namespace AutoPartShop.Application.DTOs.PurchaseReturnDtos;

public class PurchaseReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCode { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal CreditNoteAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;
    public List<PurchaseReturnLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseReturnLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public int RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}
