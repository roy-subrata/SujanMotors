namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TaxPercentage { get; set; } = 0;
    public decimal DiscountPercentage { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public List<CreatePurchaseOrderLineRequest> LineItems { get; set; } = new();
}

public class CreatePurchaseOrderLineRequest
{
    public Guid PartId { get; set; }
    public Guid? UnitId { get; set; }  // Optional: Unit in which to purchase. If null, uses Part's base unit
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
