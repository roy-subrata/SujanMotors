namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class CreateSalesOrderRequest
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerCity { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public DateTime DeliveryDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public decimal Discount { get; set; } = 0;
    public List<CreateSalesOrderLineRequest> Lines { get; set; } = new();
}

public class CreateSalesOrderLineRequest
{
    public Guid PartId { get; set; }
    public Guid? UnitId { get; set; }  // Optional: Unit in which to sell. If null, uses Part's base unit
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
}
