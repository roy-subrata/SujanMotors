namespace AutoPartShop.Application.Parts.Dtos
{
    public class PartResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PartNumber { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public Guid CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public Guid? BrandId { get; set; }
        public string? BrandName { get; set; }
        public string? BrandCode { get; set; }
        public Guid? UnitId { get; set; }
        public string? UnitName { get; set; }
        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public int MinimumStock { get; set; }
        public bool IsActive { get; set; }
        public decimal? MinMarginPercentOverride { get; set; }
        public decimal? MaxDiscountPercentOverride { get; set; }

        // Warranty Information
        public bool HasWarranty { get; set; }
        public int? WarrantyPeriodMonths { get; set; }
        public string? WarrantyType { get; set; }
        public string? WarrantyTerms { get; set; }
        public string? WarrantyCertificateTemplate { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public string ModifiedBy { get; set; } = string.Empty;
    }
}
