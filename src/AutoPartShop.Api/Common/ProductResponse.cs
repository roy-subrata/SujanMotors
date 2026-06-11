namespace AutoPartShop.Api.Common;

/// <summary>
/// Unified product response — same shape for public and admin.
/// CostPrice is null when the caller is not authenticated (public view).
/// Variants always contains at least one entry; products without explicit variants
/// receive a synthesized "Default" variant built from the base product fields.
/// </summary>
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? RichDescription { get; init; }
    public string PartNumber { get; init; } = string.Empty;
    public string SKU { get; init; } = string.Empty;
    public string? OemNumber { get; init; }
    public string? Barcode { get; init; }
    public string? Tags { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public bool IsPerishable { get; init; }
    public bool IsActive { get; init; }
    public int MinimumStock { get; init; }
    public string? TaxCode { get; init; }
    public bool HasVariants { get; init; }

    public ProductCategorySummary Category { get; init; } = new();
    public ProductBrandSummary? Brand { get; init; }
    public ProductUnitSummary? BaseUnit { get; init; }
    public ProductUnitSummary? Unit { get; init; }
    public ProductPricingSummary Pricing { get; init; } = new();
    public ProductDimensionsSummary? Dimensions { get; init; }
    public ProductWarrantySummary? Warranty { get; init; }

    public List<ProductVariantSummary> Variants { get; init; } = [];

    public string? CreatedBy { get; init; }
    public string? ModifiedBy { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class ProductCategorySummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Breadcrumb { get; init; }
}

public sealed class ProductBrandSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class ProductUnitSummary
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}

public sealed class ProductPricingSummary
{
    public decimal? CostPrice { get; init; }  // null for unauthenticated callers
    public decimal SellingPrice { get; init; }
    public string Currency { get; init; } = "BDT";
}

public sealed class ProductDimensionsSummary
{
    public decimal? WeightKg { get; init; }
    public decimal? WidthCm { get; init; }
    public decimal? HeightCm { get; init; }
    public decimal? DepthCm { get; init; }
}

public sealed class ProductWarrantySummary
{
    public bool HasWarranty { get; init; }
    public int? PeriodMonths { get; init; }
    public string? Type { get; init; }
    public string? Terms { get; init; }
    public string? CertificateTemplate { get; init; }
}

public sealed class ProductVariantSummary
{
    public Guid? Id { get; init; }          // null means this is a synthesized default variant
    public string Name { get; init; } = string.Empty;
    public string Code { get; init; } = string.Empty;
    public string? SKU { get; init; }
    public string? Barcode { get; init; }
    public bool IsDefault { get; init; }    // true = synthesized from base product, no DB variant record
    public bool IsActive { get; init; }
    public ProductPricingSummary Pricing { get; init; } = new();
    public List<VariantAttributeSummary> Attributes { get; init; } = [];
}

public sealed class VariantAttributeSummary
{
    public Guid AttributeId { get; init; }
    public string AttributeName { get; init; } = string.Empty;
    public string? DataType { get; init; }
    public Guid? OptionId { get; init; }
    public string? OptionValue { get; init; }
    public string? ValueText { get; init; }
    public decimal? ValueNumber { get; init; }
    public bool? ValueBool { get; init; }
}
