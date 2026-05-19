namespace AutoPartShop.Application.DTOs.PurchaseReturnDtos;

public class CreatePurchaseReturnRequest
{
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ReturnDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreatePurchaseReturnLineRequest> Lines { get; set; } = new();
}

public class CreatePurchaseReturnLineRequest
{
    public Guid PurchaseOrderLineId { get; set; }
    public Guid PartId { get; set; }
    public Guid? StockLotId { get; set; }  // Optional: specific lot to return from
    public int Quantity { get; set; }
    public int RejectedQuantity { get; set; } = 0;
    public decimal UnitPrice { get; set; }
    public string Condition { get; set; } = "UNOPENED";
    public string Notes { get; set; } = string.Empty;
}

public class UpdatePurchaseReturnRequest
{
    public Guid Id { get; set; }
    public Guid PurchaseOrderId { get; set; }
    public Guid SupplierId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime? ReturnDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<CreatePurchaseReturnLineRequest> Lines { get; set; } = new();
}
