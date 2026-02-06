namespace AutoPartShop.Application.Catalog.Dtos;

public class CatalogLandingResponse
{
    public List<CatalogCategoryDto> Categories { get; set; } = new();
    public List<CatalogProductListItem> Featured { get; set; } = new();
    public List<CatalogProductListItem> Popular { get; set; } = new();
    public List<CatalogProductListItem> Latest { get; set; } = new();
}

public class CatalogCategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int DepthLevel { get; set; }
    public int DisplayOrder { get; set; }
    public int ChildCount { get; set; }
}
