namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty; // DRAFT, SUBMITTED, CONFIRMED, PARTIAL, DELIVERED, CANCELLED
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public bool IsOverdue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseOrderLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseOrderLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int Quantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity => Quantity - ReceivedQuantity;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
