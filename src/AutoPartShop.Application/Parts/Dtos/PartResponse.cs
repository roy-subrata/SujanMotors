namespace AutoPartShop.Application.Parts.Dtos
{
    public class PartResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? RichDescription { get; set; }
        public string PartNumber { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? BrandCode { get; set; }
        public Guid? BaseUnitId { get; set; }
        public string? BaseUnitName { get; set; }
        public string? BaseUnitCode { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int MinimumStock { get; set; }
        public bool IsActive { get; set; }
        // Warranty Information
        public bool HasWarranty { get; set; }
        public int? WarrantyPeriodMonths { get; set; }
        public string? WarrantyType { get; set; }
        public string? WarrantyTerms { get; set; }
        public string? WarrantyCertificateTemplate { get; set; }

        // Universal product fields
        public string? Barcode { get; set; }
        public string? Tags { get; set; }
        public string ProductType { get; set; } = "PHYSICAL";
        public bool IsPerishable { get; set; }
        public decimal? WeightKg { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? DepthCm { get; set; }
        public string? TaxCode { get; set; }

        // Variant fields — populated when FlattenVariants=true
        public bool HasVariants { get; set; } = false;
        public int VariantCount { get; set; } = 0;
        public bool IsVariant { get; set; } = false;
        public Guid? VariantId { get; set; }
        public string? VariantName { get; set; }
        public string? VariantCode { get; set; }
        public string? VariantSKU { get; set; }
        public string? VariantBarcode { get; set; }
        public decimal EffectiveCostPrice { get; set; }
        public decimal EffectiveSellingPrice { get; set; }
        // "Engine Oil" for base, "Engine Oil - 5W-30 1L" for variant
        public string DisplayName { get; set; } = string.Empty;

        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
