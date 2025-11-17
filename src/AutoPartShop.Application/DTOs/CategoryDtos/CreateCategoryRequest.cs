namespace AutoPartShop.Application.DTOs.CategoryDtos;

/// <summary>
/// Request DTO for creating a new category
/// </summary>
public class CreateCategoryRequest
{
    /// <summary>
    /// Category name (required, max 100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description (optional)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Category code (required, max 20 characters, will be converted to uppercase)
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Parent category ID for subcategories (optional)
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    /// <summary>
    /// Display order in lists
    /// </summary>
    public int DisplayOrder { get; set; } = 0;
}
