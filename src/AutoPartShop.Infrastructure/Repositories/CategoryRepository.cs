using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of CategoryRepository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class CategoryRepository : ICategoryRepository
{
    // In-memory storage for demonstration
    private static readonly List<Category> _categories = new()
    {
        Category.Create("Engine Parts", "Components related to vehicle engines", "ENG-001", 1),
        Category.Create("Electrical", "Electrical components and wiring", "ELE-001", 2),
        Category.Create("Brake System", "Brake pads, rotors, and related components", "BRK-001", 3),
        Category.Create("Suspension", "Suspension parts and shock absorbers", "SUS-001", 4),
        Category.Create("Transmission", "Gearbox and transmission components", "TRN-001", 5),
    };

    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken); // Simulate async operation
        return _categories.Where(c => !c.Isdeleted).ToList();
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Id == id && !c.Isdeleted);
    }

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (_categories.Any(c => c.Code == entity.Code && !c.Isdeleted))
            throw new InvalidOperationException($"Category with code '{entity.Code}' already exists");

        _categories.Add(entity);
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var existingCategory = _categories.FirstOrDefault(c => c.Id == entity.Id && !c.Isdeleted);
        if (existingCategory == null)
            throw new InvalidOperationException($"Category with ID '{entity.Id}' not found");

        // Update the properties
        var index = _categories.IndexOf(existingCategory);
        if (index >= 0)
        {
            _categories[index] = entity;
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var category = _categories.FirstOrDefault(c => c.Id == id && !c.Isdeleted);
        if (category == null)
            throw new InvalidOperationException($"Category with ID '{id}' not found");

        // Soft delete
        _categories.Remove(category);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Any(c => c.Id == id && !c.Isdeleted);
    }

    public async Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.IsActive && !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithSubcategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.ParentCategoryId == null && !c.Isdeleted).ToList();
    }

    public async Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.Where(c => c.ParentCategoryId == parentCategoryId && !c.Isdeleted).ToList();
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var query = _categories.Where(c => c.Code == code && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return query.Any();
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var query = _categories.Where(c => c.Name == name && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return query.Any();
    }

    public async Task<Category?> GetByCategoryCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Code == code && !c.Isdeleted);
    }

    public async Task<Category?> GetByCategoryNameAsync(string name, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);
        return _categories.FirstOrDefault(c => c.Name == name && !c.Isdeleted);
    }

    public async Task<IEnumerable<Category>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var lowerTerm = searchTerm.ToLowerInvariant();
        return _categories.Where(c =>
            !c.Isdeleted &&
            (c.Name.ToLowerInvariant().Contains(lowerTerm) ||
             c.Code.ToLowerInvariant().Contains(lowerTerm) ||
             c.Description.ToLowerInvariant().Contains(lowerTerm))
        ).ToList();
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (pageNumber < 1)
            pageNumber = 1;
        if (pageSize < 1)
            pageSize = 10;

        var query = _categories.Where(c => !c.Isdeleted).AsEnumerable();
        var totalCount = query.Count();

        var items = query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, totalCount);
    }
}
