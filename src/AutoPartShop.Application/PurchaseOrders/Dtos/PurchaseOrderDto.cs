namespace AutoPartShop.Application.PurchaseOrders;

public class PurchaseOrderDto
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "TOTAL";
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public bool IsOverdue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseOrderLineDto> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
