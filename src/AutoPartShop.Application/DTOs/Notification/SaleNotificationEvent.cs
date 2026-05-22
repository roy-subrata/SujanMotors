namespace AutoPartShop.Application.DTOs.Notification;

public class SaleNotificationEvent
{
    public Guid SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = "BDT";
    public string SaleChannel { get; set; } = "POS";  // POS | ECOMMERCE | MOBILE
    public string SaleType { get; set; } = "SALE";    // SALE | QUICK_SALE
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
}
