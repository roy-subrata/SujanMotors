namespace AutoPartShop.Application.Catalog.Dtos;

public class CatalogFilterResponse
{
    public Guid CategoryId { get; set; }
    public List<CatalogFilterDto> Filters { get; set; } = new();
    public PriceRangeFilterDto PriceRange { get; set; } = new();
    public AvailabilityFilterDto Availability { get; set; } = new();
}

public class CatalogFilterDto
{
    public Guid AttributeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string FilterType { get; set; } = "select";
    public int SortOrder { get; set; }
    public List<CatalogFilterOptionDto> Options { get; set; } = new();
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string Unit { get; set; } = string.Empty;
}

public class CatalogFilterOptionDto
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PriceRangeFilterDto
{
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public string Currency { get; set; } = "BDT";
}

public class AvailabilityFilterDto
{
    public bool InStockAvailable { get; set; } = true;
}
