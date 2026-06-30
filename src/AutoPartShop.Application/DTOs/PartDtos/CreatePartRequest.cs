namespace AutoPartShop.Application.DTOs.PartDtos;

public class CreatePartRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;       // Short summary (max 255 chars)
    public string? RichDescription { get; set; }                  // Full HTML/markdown for product pages
    public string PartNumber { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? BaseUnitId { get; set; }  // Base unit for stock/inventory tracking
    public Guid? UnitId { get; set; }  // Display/sales unit (defaults to BaseUnitId if not specified)
    public decimal CostPrice { get; set; } = 0;
    public decimal SellingPrice { get; set; } = 0;
    public int MinimumStock { get; set; } = 0;
    // Warranty Information
    public bool HasWarranty { get; set; } = false;
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? WarrantyCertificateTemplate { get; set; }

    public string? OemNumber { get; set; }                     // Manufacturer OEM part number (optional)
    public string? LocalName { get; set; }                     // Local-language name for staff display

    // Universal product fields
    public string? Barcode { get; set; }                      // UPC / EAN / QR
    public string? Tags { get; set; }                         // Comma-separated search tags
    public string ProductType { get; set; } = "PHYSICAL";     // PHYSICAL, DIGITAL, SERVICE
    public bool IsPerishable { get; set; } = false;           // Grocery / pharmacy expiry tracking
    public decimal? WeightKg { get; set; }
    public decimal? WidthCm { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? DepthCm { get; set; }
    public string? TaxCode { get; set; }                      // e.g. STANDARD, FOOD, MEDICINE, EXEMPT
}
