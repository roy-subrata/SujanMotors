namespace AutoPartShop.Application.DTOs.QuotationDtos;

public class CreateQuotationRequest
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime? ValidUntil { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public decimal Discount { get; set; } = 0;  // percentage
    public decimal TaxAmount { get; set; } = 0;
    public List<CreateQuotationLineRequest> Lines { get; set; } = new();
}

public class CreateQuotationLineRequest
{
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }
    public Guid? UnitId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; } = 0;
}

public class RejectQuotationRequest
{
    public string Reason { get; set; } = string.Empty;
}

public class QuotationResponse
{
    public Guid Id { get; set; }
    public string QuotationNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsExpired { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public Guid? ConvertedToSalesOrderId { get; set; }
    public List<QuotationLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class QuotationLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
    public string UnitSymbol { get; set; } = string.Empty;
}

/// <summary>Result of converting an accepted quotation into a SalesOrder.</summary>
public class ConvertQuotationResponse
{
    public Guid QuotationId { get; set; }
    public Guid SalesOrderId { get; set; }
    public string SONumber { get; set; } = string.Empty;
}
