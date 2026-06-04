namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class CreatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TaxPercentage { get; set; } = 0;
    public decimal DiscountPercentage { get; set; } = 0;
    public decimal DiscountAmount { get; set; } = 0;
    public string DiscountType { get; set; } = "TOTAL";
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public List<CreatePurchaseOrderLineRequest> LineItems { get; set; } = new();
}

public class CreatePurchaseOrderLineRequest
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? UnitId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
