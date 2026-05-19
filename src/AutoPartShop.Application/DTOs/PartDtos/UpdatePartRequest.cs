namespace AutoPartShop.Application.DTOs.PartDtos;

public class UpdatePartRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RichDescription { get; set; }
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? BaseUnitId { get; set; }  // Base unit for stock/inventory tracking
    public Guid? UnitId { get; set; }  // Display/sales unit (defaults to BaseUnitId if not specified)
    public decimal CostPrice { get; set; } = 0;
    public decimal SellingPrice { get; set; } = 0;
    public int MinimumStock { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    // Warranty Information
    public bool HasWarranty { get; set; } = false;
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? WarrantyCertificateTemplate { get; set; }

    // Universal product fields
    public string? Barcode { get; set; }
    public string? Tags { get; set; }
    public string ProductType { get; set; } = "PHYSICAL";
    public bool IsPerishable { get; set; } = false;
    public decimal? WeightKg { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public string? TaxCode { get; set; }
}
