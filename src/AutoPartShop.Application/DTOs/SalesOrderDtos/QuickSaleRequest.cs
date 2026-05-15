namespace AutoPartShop.Application.DTOs.SalesOrderDtos;

public class QuickSaleRequest
{
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public string? TechnicianNotes { get; set; }
    public string PaymentResponsibility { get; set; } = "CUSTOMER"; // CUSTOMER or TECHNICIAN_TEMPORARY
    public Guid? PurchaseOrderId { get; set; }
    public bool AutoCreatePO { get; set; } = false;
    public List<QuickSaleLineItem> Items { get; set; } = new();
    public List<QuickSalePayment> Payments { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    /// <summary>NONE | PERCENTAGE | FIXED — source of the cart-level discount</summary>
    public string DiscountType { get; set; } = "NONE";
    /// <summary>Reason for applying the discount — required for audit trail</summary>
    public string? DiscountReason { get; set; }
    public decimal VatAmount { get; set; }
    public decimal VatPercentage { get; set; } = 15;
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Advance Payment Support
    public bool UseAdvanceBalance { get; set; } = false;
    public decimal AdvanceAmountToApply { get; set; } = 0;

    // Quotation Support
    public bool SaveAsQuotation { get; set; } = false;
    public string Channel { get; set; } = "POS";  // POS | ECOMMERCE | MOBILE | API
}

public class QuickSaleLineItem
{
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }  // Optional: Unit in which to sell. If null, uses Part's base unit
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
    public int? StockAvailable { get; set; }
}

public class QuickSalePayment
{
    public string Method { get; set; } = "CASH"; // CASH, MOBILE_BANKING, CARD, DUE, PART_PAY
    public decimal Amount { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class QuickSaleResponse
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid SalesOrderId { get; set; }
    public string SalesOrderNumber { get; set; } = string.Empty;
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? TechnicianId { get; set; }
    public string? TechnicianName { get; set; }
    public string PaymentResponsibility { get; set; } = string.Empty;
    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal VatAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal DueAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsQuotation { get; set; }
    public DateTime CreatedAt { get; set; }
}
