namespace AutoPartShop.Application.Parts.Dtos;

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public bool? IsActive { get; set; } = true;
}
