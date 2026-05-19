namespace AutoPartShop.Application.DTOs.VariantPricingDtos;

public class VariantPriceResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }   // null = base product price
    public decimal SellingPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
}

public class SetVariantPriceRequest
{
    public decimal SellingPrice { get; set; }
    public DateTime StartDate { get; set; }
    public string Currency { get; set; } = "BDT";
    public string? Reason { get; set; }
}

public class ActiveVariantPriceResponse
{
    public Guid PartId { get; set; }
    public Guid? ProductVariantId { get; set; }   // null = base product price
    public decimal SellingPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // VARIANT_HISTORY | PRODUCT_HISTORY
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
