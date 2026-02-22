using AutoPartsShop.Domain.Entities;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents an auto part/product in the inventory system
/// </summary>
public class Part : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public PartNumber PartNumber { get; private set; } = null!;
    public string SKU { get; private set; } = string.Empty;  // Stock Keeping Unit
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }  // Optional reference to Brand
    public Guid? UnitId { get; private set; }  // Optional reference to Unit for measuring/stocking
    public decimal CostPrice { get; private set; } = 0;  // Purchase price
    public string CostPriceCurrency { get; private set; } = "BDT";  // ISO 4217 currency code for cost price
    public decimal SellingPrice { get; private set; } = 0;  // Retail/Sale price
    public string SellingPriceCurrency { get; private set; } = "BDT";  // ISO 4217 currency code for selling price
    public int MinimumStock { get; private set; } = 0;  // Reorder level
    public bool IsActive { get; private set; } = true;
    public decimal? MinMarginPercentOverride { get; private set; } = null;
    public decimal? MaxDiscountPercentOverride { get; private set; } = null;

    // Warranty Information
    public bool HasWarranty { get; private set; } = false;
    public int? WarrantyPeriodMonths { get; private set; }  // 6, 12, 24, 36 months
    public string? WarrantyType { get; private set; }  // MANUFACTURER, SELLER, EXTENDED, NO_WARRANTY
    public string? WarrantyTerms { get; private set; }  // Terms & conditions
    public string? WarrantyCertificateTemplate { get; private set; }  // Template for certificate number generation

    // Navigation properties
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<PartVehicleCompatibility> VehicleCompatibilities { get; set; } = new List<PartVehicleCompatibility>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();
    public ProductCatalogEntry? CatalogEntry { get; set; }

    private Part() { }

    public static Part Create(string name, PartNumber partNumber, string sku, Guid categoryId, Guid? brandId = null, Guid? unitId = null, string description = "",
        decimal costPrice = 0, decimal sellingPrice = 0, int minimumStock = 0,
        decimal? minMarginPercentOverride = null, decimal? maxDiscountPercentOverride = null,
        bool hasWarranty = false, int? warrantyPeriodMonths = null, string? warrantyType = null,
        string? warrantyTerms = null, string? warrantyCertificateTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (partNumber is null)
            throw new InvalidOperationException($"PartNumber cannot be empty {nameof(partNumber)}");

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (name.Length > 200)
            throw new ArgumentException("Name cannot exceed 200 characters", nameof(name));

        if (sku.Length > 50)
            throw new ArgumentException("SKU cannot exceed 50 characters", nameof(sku));

        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        if (minimumStock < 0)
            throw new ArgumentException("Minimum stock cannot be negative", nameof(minimumStock));

        if (minMarginPercentOverride.HasValue && (minMarginPercentOverride.Value < 0 || minMarginPercentOverride.Value > 100))
            throw new ArgumentException("Min margin percent must be between 0 and 100.", nameof(minMarginPercentOverride));

        if (maxDiscountPercentOverride.HasValue && (maxDiscountPercentOverride.Value < 0 || maxDiscountPercentOverride.Value > 100))
            throw new ArgumentException("Max discount percent must be between 0 and 100.", nameof(maxDiscountPercentOverride));

        // Warranty validation
        if (hasWarranty)
        {
            if (!warrantyPeriodMonths.HasValue || warrantyPeriodMonths.Value <= 0)
                throw new ArgumentException("Warranty period is required and must be greater than 0 when HasWarranty is true", nameof(warrantyPeriodMonths));

            if (string.IsNullOrWhiteSpace(warrantyType))
                throw new ArgumentException("Warranty type is required when HasWarranty is true", nameof(warrantyType));
        }

        return new Part
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            PartNumber = partNumber,
            SKU = sku.Trim().ToUpper(),
            CategoryId = categoryId,
            BrandId = brandId,
            UnitId = unitId,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            MinimumStock = minimumStock,
            IsActive = true,
            MinMarginPercentOverride = minMarginPercentOverride,
            MaxDiscountPercentOverride = maxDiscountPercentOverride,
            HasWarranty = hasWarranty,
            WarrantyPeriodMonths = hasWarranty ? warrantyPeriodMonths : null,
            WarrantyType = hasWarranty ? warrantyType?.Trim().ToUpper() : null,
            WarrantyTerms = hasWarranty ? warrantyTerms?.Trim() : null,
            WarrantyCertificateTemplate = hasWarranty ? warrantyCertificateTemplate?.Trim() : null
        };
    }

    public void Update(string name, string description, string sku, Guid categoryId, Guid? brandId, Guid? unitId,
        decimal costPrice, decimal sellingPrice, int minimumStock, bool isActive,
        decimal? minMarginPercentOverride = null, decimal? maxDiscountPercentOverride = null,
        bool hasWarranty = false, int? warrantyPeriodMonths = null, string? warrantyType = null,
        string? warrantyTerms = null, string? warrantyCertificateTemplate = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU cannot be empty", nameof(sku));

        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        if (minimumStock < 0)
            throw new ArgumentException("Minimum stock cannot be negative", nameof(minimumStock));

        if (minMarginPercentOverride.HasValue && (minMarginPercentOverride.Value < 0 || minMarginPercentOverride.Value > 100))
            throw new ArgumentException("Min margin percent must be between 0 and 100.", nameof(minMarginPercentOverride));

        if (maxDiscountPercentOverride.HasValue && (maxDiscountPercentOverride.Value < 0 || maxDiscountPercentOverride.Value > 100))
            throw new ArgumentException("Max discount percent must be between 0 and 100.", nameof(maxDiscountPercentOverride));

        // Warranty validation
        if (hasWarranty)
        {
            if (!warrantyPeriodMonths.HasValue || warrantyPeriodMonths.Value <= 0)
                throw new ArgumentException("Warranty period is required and must be greater than 0 when HasWarranty is true", nameof(warrantyPeriodMonths));

            if (string.IsNullOrWhiteSpace(warrantyType))
                throw new ArgumentException("Warranty type is required when HasWarranty is true", nameof(warrantyType));
        }

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        SKU = sku.Trim().ToUpper();
        CategoryId = categoryId;
        BrandId = brandId;
        UnitId = unitId;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        MinimumStock = minimumStock;
        IsActive = isActive;
        MinMarginPercentOverride = minMarginPercentOverride;
        MaxDiscountPercentOverride = maxDiscountPercentOverride;

        // Update warranty fields
        HasWarranty = hasWarranty;
        WarrantyPeriodMonths = hasWarranty ? warrantyPeriodMonths : null;
        WarrantyType = hasWarranty ? warrantyType?.Trim().ToUpper() : null;
        WarrantyTerms = hasWarranty ? warrantyTerms?.Trim() : null;
        WarrantyCertificateTemplate = hasWarranty ? warrantyCertificateTemplate?.Trim() : null;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
