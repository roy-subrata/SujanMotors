namespace AutoPartShop.Web.Services;

/// <summary>
/// Service interface for category operations in Blazor Web
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// Get all categories
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetAllCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active categories
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top-level categories (root only)
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category by ID
    /// </summary>
    Task<CategoryDto?> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subcategories of a parent category
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search categories
    /// </summary>
    Task<IEnumerable<CategoryDto>> SearchCategoriesAsync(string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get categories with pagination
    /// </summary>
    Task<PaginatedResult<CategoryDto>> GetCategoriesPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Create a new category
    /// </summary>
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Update an existing category
    /// </summary>
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a category
    /// </summary>
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate a category
    /// </summary>
    Task<CategoryDto> ActivateCategoryAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivate a category
    /// </summary>
    Task<CategoryDto> DeactivateCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}

/// <summary>
/// Category Data Transfer Object with n-level hierarchy support
/// </summary>
public class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
    public List<CategoryDto> SubCategories { get; set; } = new();

    /// <summary>
    /// Breadcrumb path for navigation (e.g., "Engines > Diesel > Small")
    /// </summary>
    public string BreadcrumbPath { get; set; } = string.Empty;

    /// <summary>
    /// Depth level in the hierarchy (0 = root, max 7)
    /// </summary>
    public int DepthLevel { get; set; } = 0;

    /// <summary>
    /// Count of direct child categories
    /// </summary>
    public int ChildCount { get; set; } = 0;
}

/// <summary>
/// Create category request
/// </summary>
public class CreateCategoryRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentCategoryId { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Update category request
/// </summary>
public class UpdateCategoryRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Paginated result wrapper
/// </summary>
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
