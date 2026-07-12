namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class InvoiceResponse
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;  // SO Number for reference
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public Guid? CustomerVehicleId { get; set; }
    public string VehicleLabel { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime DueDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // DRAFT, ISSUED, PAID, PARTIALLY_PAID, OVERDUE, CANCELLED
    public bool IsOverdue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<InvoicePaymentResponse> Payments { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class InvoicePaymentResponse
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
}
