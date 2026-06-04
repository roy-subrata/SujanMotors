namespace AutoPartShop.Domain.Entities;

public class ProductVariant : AuditableEntity
{
    public Guid PartId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? SKU { get; private set; }
    public string? Barcode { get; private set; }

    public string PricingMode { get; private set; } = "OVERRIDE";

    public decimal CostPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public string Currency { get; private set; } = "BDT";
    public bool IsActive { get; private set; } = true;

    public decimal? WeightKg { get; private set; }
    public decimal? WidthCm { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? DepthCm { get; private set; }

    // null = inherit from parent Part; true = variant has warranty; false = variant explicitly has no warranty
    public bool? HasWarrantyOverride { get; private set; }
    public int? WarrantyPeriodMonthsOverride { get; private set; }
    public string? WarrantyTypeOverride { get; private set; }

    public Product? Part { get; set; }
    public ICollection<VariantAttributeValue> Attributes { get; set; } = new List<VariantAttributeValue>();
    public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();

    private ProductVariant() { }

    public static ProductVariant Create(
        Guid partId,
        string name,
        string code,
        decimal costPrice,
        decimal sellingPrice,
        string? sku = null,
        string? barcode = null,
        string currency = "BDT",
        bool isActive = true,
        decimal? weightKg = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal? depthCm = null)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));
        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        return new ProductVariant
        {
            PartId = partId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            SKU = sku?.Trim().ToUpperInvariant(),
            Barcode = barcode?.Trim(),
            PricingMode = "OVERRIDE",
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpperInvariant(),
            IsActive = isActive,
            WeightKg = weightKg,
            WidthCm = widthCm,
            HeightCm = heightCm,
            DepthCm = depthCm
        };
    }

    public void Update(
        string name,
        string code,
        decimal costPrice,
        decimal sellingPrice,
        string? sku = null,
        string? barcode = null,
        string currency = "BDT",
        bool isActive = true,
        decimal? weightKg = null,
        decimal? widthCm = null,
        decimal? heightCm = null,
        decimal? depthCm = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));
        if (costPrice < 0)
            throw new ArgumentException("Cost price cannot be negative", nameof(costPrice));
        if (sellingPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(sellingPrice));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        SKU = sku?.Trim().ToUpperInvariant();
        Barcode = barcode?.Trim();
        CostPrice = costPrice;
        SellingPrice = sellingPrice;
        Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpperInvariant();
        IsActive = isActive;
        WeightKg = weightKg;
        WidthCm = widthCm;
        HeightCm = heightCm;
        DepthCm = depthCm;
    }

    public void UpdateSellingPrice(decimal newPrice, string currency = "BDT")
    {
        if (newPrice < 0)
            throw new ArgumentException("Selling price cannot be negative", nameof(newPrice));
        SellingPrice = newPrice;
        if (!string.IsNullOrWhiteSpace(currency))
            Currency = currency.Trim().ToUpperInvariant();
    }

    public void SetWarrantyOverride(bool hasWarranty, int? periodMonths, string? warrantyType)
    {
        if (hasWarranty)
        {
            if (!periodMonths.HasValue || periodMonths.Value <= 0)
                throw new ArgumentException("Warranty period must be greater than 0 when enabling warranty override", nameof(periodMonths));

            var validTypes = new[] { "MANUFACTURER", "SELLER", "EXTENDED" };
            var normalizedType = warrantyType?.Trim().ToUpper();
            if (string.IsNullOrWhiteSpace(normalizedType) || !validTypes.Contains(normalizedType))
                throw new ArgumentException($"Warranty type must be one of: {string.Join(", ", validTypes)}", nameof(warrantyType));

            HasWarrantyOverride = true;
            WarrantyPeriodMonthsOverride = periodMonths;
            WarrantyTypeOverride = normalizedType;
        }
        else
        {
            HasWarrantyOverride = false;
            WarrantyPeriodMonthsOverride = null;
            WarrantyTypeOverride = null;
        }
    }

    public void ClearWarrantyOverride()
    {
        HasWarrantyOverride = null;
        WarrantyPeriodMonthsOverride = null;
        WarrantyTypeOverride = null;
    }

    public (bool hasWarranty, int? periodMonths, string? warrantyType) ResolveWarranty(Product part)
    {
        if (HasWarrantyOverride.HasValue)
            return (HasWarrantyOverride.Value, WarrantyPeriodMonthsOverride, WarrantyTypeOverride);
        return (part.HasWarranty, part.WarrantyPeriodMonths, part.WarrantyType);
    }
}
