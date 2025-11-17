using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Infrastructure.Repositories;

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
    /// Get categories with pagination
    /// </summary>
    Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
