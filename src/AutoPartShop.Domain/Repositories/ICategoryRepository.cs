using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// Repository interface for Category entity with specific query methods
/// </summary>
public interface ICategoryRepository : IBaseRepository<Category>
{
    /// <summary>
    /// Get all active categories
    /// </summary>
    Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get categories with their subcategories
    /// </summary>
    Task<IEnumerable<Category>> GetCategoriesWithSubcategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get top-level categories (without parents)
    /// </summary>
    Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get subcategories of a specific parent category
    /// </summary>
    Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if category code already exists
    /// </summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if category name already exists
    /// </summary>
    Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category by code
    /// </summary>
    Task<Category?> GetByCategoryCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get category by name
    /// </summary>
    Task<Category?> GetByCategoryNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search categories by name or code
    /// </summary>
    Task<IEnumerable<Category>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);


    /// <summary>
    /// Search categories by name or code with pagination
    /// </summary>
    Task<(IEnumerable<Category> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all ancestors of a category (path to root)
    /// </summary>
    Task<IEnumerable<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all descendants of a category at all levels
    /// </summary>
    Task<IEnumerable<Category>> GetAllDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if moving a category to a new parent would create a circular reference
    /// </summary>
    Task<bool> WouldCreateCircularReferenceAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the depth of a category (distance from root)
    /// </summary>
    Task<int> GetDepthAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get full breadcrumb path for a category
    /// </summary>
    Task<string> GetBreadcrumbPathAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
