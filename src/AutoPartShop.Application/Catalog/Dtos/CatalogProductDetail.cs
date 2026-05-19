namespace AutoPartShop.Application.Catalog.Dtos;

public class CatalogProductDetail
{
    public Guid PartId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? RichDescription { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? SKU { get; set; }
    public string? Tags { get; set; }
    public bool InStock { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public decimal? SalePrice { get; set; }
    public decimal? OriginalPrice { get; set; }
    public bool IsOnSale { get; set; }
    public string Currency { get; set; } = "BDT";
    // Warranty
    public bool HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
    public List<CatalogVariantDto> Variants { get; set; } = new();
    public List<CatalogAttributeGroupDto> Specifications { get; set; } = new();
    public List<CatalogMediaDto> Media { get; set; } = new();
    public List<CatalogProductListItem> RelatedProducts { get; set; } = new();
}

public class CatalogVariantDto
{
    public Guid VariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? SKU { get; set; }
    public decimal Price { get; set; }        // Effective price after discount
    public decimal? OriginalPrice { get; set; }
    public decimal? SalePrice { get; set; }
    public bool IsOnSale { get; set; }
    public string Currency { get; set; } = "BDT";
    public bool InStock { get; set; }
    public List<CatalogAttributeValueDto> Attributes { get; set; } = new();
    public List<CatalogAttributeGroupDto> Specifications { get; set; } = new();
}

public class CatalogAttributeGroupDto
{
    public string GroupName { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public List<CatalogAttributeValueDto> Attributes { get; set; } = new();
}

public class CatalogAttributeValueDto
{
    public Guid AttributeId { get; set; }
    public string AttributeName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
}

public class CatalogMediaDto
{
    public string Url { get; set; } = string.Empty;
    public string MediaType { get; set; } = "image";
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
}
