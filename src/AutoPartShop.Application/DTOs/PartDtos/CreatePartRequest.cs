namespace AutoPartShop.Application.DTOs.PartDtos;

public class CreatePartRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal CostPrice { get; set; } = 0;
    public decimal SellingPrice { get; set; } = 0;
    public int MinimumStock { get; set; } = 0;

    // Warranty Information
    public bool HasWarranty { get; set; } = false;
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public string? WarrantyCertificateTemplate { get; set; }
}
