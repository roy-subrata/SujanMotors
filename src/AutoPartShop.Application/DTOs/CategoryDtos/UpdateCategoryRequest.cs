namespace AutoPartShop.Application.DTOs.CategoryDtos;

/// <summary>
/// Request DTO for updating a category
/// </summary>
public class UpdateCategoryRequest
{
    /// <summary>
    /// Category ID (required)
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Category name (required, max 100 characters)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category description (optional)
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Display order in lists
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Whether the category is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}
