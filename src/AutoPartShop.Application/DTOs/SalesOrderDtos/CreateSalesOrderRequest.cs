namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class CreateSalesOrderRequest
{
    public Guid CustomerId { get; set; }
    public Guid WarehouseId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerCity { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public Guid? CustomerVehicleId { get; set; }  // Optional: customer's vehicle this purchase is for
    public DateTime DeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public decimal Discount { get; set; } = 0;
    public string Channel { get; set; } = "ECOMMERCE";  // POS | ECOMMERCE | MOBILE | API
    public List<CreateSalesOrderLineRequest> Lines { get; set; } = new();
}

public class CreateSalesOrderLineRequest
{
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }  // The specific variant the customer selected
    public Guid? UnitId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
}
