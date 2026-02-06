using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.Catalog.Dtos;

public class CatalogSearchRequest : BaseQuery
{
    public Guid? CategoryId { get; set; }
    public bool IncludeDescendants { get; set; } = true;
    public decimal? PriceMin { get; set; }
    public decimal? PriceMax { get; set; }
    public bool InStockOnly { get; set; } = false;
    public List<AttributeFilterRequest> AttributeFilters { get; set; } = new();
}

public class AttributeFilterRequest
{
    public Guid AttributeId { get; set; }
    public List<string> Values { get; set; } = new();
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
}
