namespace AutoPartShop.Application.Catalog.Dtos;

public class CatalogProductListItem
{
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public decimal Price { get; set; }          // Effective selling price (after discount if any)
    public decimal? OriginalPrice { get; set; } // Base price before discount
    public decimal? SalePrice { get; set; }     // Discounted price (same as Price when on sale)
    public bool IsOnSale { get; set; }
    public string Currency { get; set; } = "BDT";
    public bool InStock { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string PrimaryImageUrl { get; set; } = string.Empty;
}
