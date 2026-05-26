using AutoPartsShop.Domain.Entities;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Product catalog record. Per-lot cost/price/warranty overrides live on StockLot
/// (created from GoodsReceiptLine when a GRN is accepted).
/// </summary>
public class Product : AuditableEntity
{
    // ── Core identity ────────────────────────────────────────────────────────
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;       // Short summary (255 chars)
    public string? RichDescription { get; private set; }                  // Full HTML/markdown for product pages
    public PartNumber PartNumber { get; private set; } = null!;  // Item/product code
    public string SKU { get; private set; } = string.Empty;
    public string? OemNumber { get; private set; }  // Manufacturer OEM part number (optional)
    public string? Barcode { get; private set; }  // UPC / EAN / QR — for POS scanner & ecommerce

    // ── Classification ───────────────────────────────────────────────────────
    public Guid CategoryId { get; private set; }
    public Guid? BrandId { get; private set; }
    public string? Tags { get; private set; }  // Comma-separated search/filter tags

    // ── Units ────────────────────────────────────────────────────────────────
    public Guid? BaseUnitId { get; private set; }  // Stock / inventory tracking unit
    public Guid? UnitId { get; private set; }  // Display / sales unit

    // ── Default pricing (per-lot overrides stored on StockLot) ───────────────
    public decimal CostPrice { get; private set; } = 0;
    public string CostPriceCurrency { get; private set; } = "BDT";
    public decimal SellingPrice { get; private set; } = 0;
    public string SellingPriceCurrency { get; private set; } = "BDT";
    public string? TaxCode { get; private set; }  // e.g. STANDARD, FOOD, MEDICINE, EXEMPT

    // ── Physical attributes (shipping / shelf planning) ───────────────────────
    public decimal? WeightKg { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? DepthCm { get; private set; }

    // ── Product behaviour flags ───────────────────────────────────────────────
    public string ProductType { get; private set; } = "PHYSICAL";  // PHYSICAL, DIGITAL, SERVICE
    public bool IsPerishable { get; private set; } = false;  // Grocery / pharmacy expiry tracking
    public int MinimumStock { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    // ── Default warranty (per-lot overrides stored on StockLot) ──────────────
    public bool HasWarranty { get; private set; } = false;
    public int? WarrantyPeriodMonths { get; private set; }
    public string? WarrantyType { get; private set; }  // MANUFACTURER, SELLER, EXTENDED
    public string? WarrantyTerms { get; private set; }
    public string? WarrantyCertificateTemplate { get; private set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    public Category? Category { get; set; }
    public Brand? Brand { get; set; }
    public Unit? BaseUnit { get; set; }
    public Unit? Unit { get; set; }
    public ICollection<PartVehicleCompatibility> VehicleCompatibilities { get; set; } = new List<PartVehicleCompatibility>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();
    public ProductCatalogEntry? CatalogEntry { get; set; }

    private Product() { }

    public static Product Create(string name, PartNumber partNumber, string sku, Guid categoryId,
        Guid? brandId = null, Guid? baseUnitId = null, Guid? unitId = null, string description = "",
        string? richDescription = null,
        decimal costPrice = 0, decimal sellingPrice = 0, int minimumStock = 0,
        bool hasWarranty = false, int? warrantyPeriodMonths = null, string? warrantyType = null,
        string? warrantyTerms = null, string? warrantyCertificateTemplate = null,
        string? barcode = null, string? tags = null, string productType = "PHYSICAL",
        bool isPerishable = false, decimal? weightKg = null,
        decimal? widthCm = null, decimal? heightCm = null, decimal? depthCm = null,
        string? taxCode = null, string? oemNumber = null)
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

        if (sku.Length > 100)
            throw new ArgumentException("SKU cannot exceed 100 characters", nameof(sku));

        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));

        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        if (minimumStock < 0)
            throw new ArgumentException("Minimum stock cannot be negative", nameof(minimumStock));

        if (weightKg.HasValue && weightKg.Value < 0)
            throw new ArgumentException("Weight cannot be negative", nameof(weightKg));

        if (hasWarranty)
        {
            if (!warrantyPeriodMonths.HasValue || warrantyPeriodMonths.Value <= 0)
                throw new ArgumentException("Warranty period is required and must be greater than 0 when HasWarranty is true", nameof(warrantyPeriodMonths));

            if (string.IsNullOrWhiteSpace(warrantyType))
                throw new ArgumentException("Warranty type is required when HasWarranty is true", nameof(warrantyType));
        }

        var validProductTypes = new[] { "PHYSICAL", "DIGITAL", "SERVICE" };
        var resolvedType = string.IsNullOrWhiteSpace(productType) ? "PHYSICAL" : productType.Trim().ToUpper();
        if (!validProductTypes.Contains(resolvedType))
            throw new ArgumentException("ProductType must be PHYSICAL, DIGITAL, or SERVICE", nameof(productType));

        return new Product
        {
            Name = name.Trim(),
            Description = description?.Trim() ?? string.Empty,
            RichDescription = richDescription?.Trim(),
            PartNumber = partNumber,
            SKU = sku.Trim().ToUpper(),
            OemNumber = oemNumber?.Trim().ToUpperInvariant(),
            Barcode = barcode?.Trim(),
            CategoryId = categoryId,
            BrandId = brandId,
            Tags = tags?.Trim(),
            BaseUnitId = baseUnitId,
            UnitId = unitId ?? baseUnitId,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            MinimumStock = minimumStock,
            IsActive = true,
            TaxCode = taxCode?.Trim().ToUpper(),
            WeightKg = weightKg,
            WidthCm = widthCm,
            HeightCm = heightCm,
            DepthCm = depthCm,
            ProductType = resolvedType,
            IsPerishable = isPerishable,
            HasWarranty = hasWarranty,
            WarrantyPeriodMonths = hasWarranty ? warrantyPeriodMonths : null,
            WarrantyType = hasWarranty ? warrantyType?.Trim().ToUpper() : null,
            WarrantyTerms = hasWarranty ? warrantyTerms?.Trim() : null,
            WarrantyCertificateTemplate = hasWarranty ? warrantyCertificateTemplate?.Trim() : null
        };
    }

    public void Update(string name, string description, string sku, Guid categoryId, Guid? brandId,
        Guid? baseUnitId, Guid? unitId,
        decimal costPrice, decimal sellingPrice, int minimumStock, bool isActive,
        bool hasWarranty = false, int? warrantyPeriodMonths = null, string? warrantyType = null,
        string? warrantyTerms = null, string? warrantyCertificateTemplate = null,
        string? barcode = null, string? tags = null, string productType = "PHYSICAL",
        bool isPerishable = false, decimal? weightKg = null,
        decimal? widthCm = null, decimal? heightCm = null, decimal? depthCm = null,
        string? taxCode = null, string? richDescription = null, string? oemNumber = null)
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

        if (weightKg.HasValue && weightKg.Value < 0)
            throw new ArgumentException("Weight cannot be negative", nameof(weightKg));

        if (hasWarranty)
        {
            if (!warrantyPeriodMonths.HasValue || warrantyPeriodMonths.Value <= 0)
                throw new ArgumentException("Warranty period is required and must be greater than 0 when HasWarranty is true", nameof(warrantyPeriodMonths));

            if (string.IsNullOrWhiteSpace(warrantyType))
                throw new ArgumentException("Warranty type is required when HasWarranty is true", nameof(warrantyType));
        }

        var validProductTypes = new[] { "PHYSICAL", "DIGITAL", "SERVICE" };
        var resolvedType = string.IsNullOrWhiteSpace(productType) ? "PHYSICAL" : productType.Trim().ToUpper();
        if (!validProductTypes.Contains(resolvedType))
            throw new ArgumentException("ProductType must be PHYSICAL, DIGITAL, or SERVICE", nameof(productType));

        Name = name.Trim();
        Description = description?.Trim() ?? string.Empty;
        RichDescription = richDescription?.Trim();
        SKU = sku.Trim().ToUpper();
        OemNumber = oemNumber?.Trim().ToUpperInvariant();
        Barcode = barcode?.Trim();
        CategoryId = categoryId;
        BrandId = brandId;
        Tags = tags?.Trim();
        BaseUnitId = baseUnitId;
        UnitId = unitId ?? baseUnitId;
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        MinimumStock = minimumStock;
        IsActive = isActive;
        TaxCode = taxCode?.Trim().ToUpper();
        WeightKg = weightKg;
        WidthCm = widthCm;
        HeightCm = heightCm;
        DepthCm = depthCm;
        ProductType = resolvedType;
        IsPerishable = isPerishable;
        HasWarranty = hasWarranty;
        WarrantyPeriodMonths = hasWarranty ? warrantyPeriodMonths : null;
        WarrantyType = hasWarranty ? warrantyType?.Trim().ToUpper() : null;
        WarrantyTerms = hasWarranty ? warrantyTerms?.Trim() : null;
        WarrantyCertificateTemplate = hasWarranty ? warrantyCertificateTemplate?.Trim() : null;
    }

    public void UpdateSellingPrice(decimal newPrice, string currency = "BDT")
    {
        if (newPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(newPrice));
        SellingPrice = newPrice;
        SellingPriceCurrency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpperInvariant();
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
