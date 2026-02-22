namespace AutoPartShop.Application.SaleOrders.Dtos
{
    public class SaleOrderResponse
    {
        public Guid Id { get; set; }
        public string SONumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public string CustomerCity { get; set; } = string.Empty;
        public Guid? WarehouseId { get; set; }
        public Guid? TechnicianId { get; set; }
        public string? TechnicianName { get; set; }
        public DateTime OrderDate { get; set; }
        public DateTime DeliveryDate { get; set; }
        public string Status { get; set; } = string.Empty; // DRAFT, CONFIRMED, PARTIALLY_SHIPPED, SHIPPED, DELIVERED, CANCELLED
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public string Currency { get; set; } = string.Empty;
        public decimal AmountPaid { get; set; }
        public decimal OutstandingAmount { get; set; }
        public bool IsOverdue { get; set; }
        public string Notes { get; set; } = string.Empty;
        public List<SalesOrderLineResponse> Lines { get; set; } = new();
        public DateTime CreatedAt { get; set; }
    }

}
