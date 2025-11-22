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

        // If this category has a parent, calculate depth level and breadcrumb path
        if (entity.ParentCategoryId.HasValue)
        {
            var parentCategory = _categories.FirstOrDefault(c => c.Id == entity.ParentCategoryId && !c.Isdeleted);
            if (parentCategory != null)
            {
                // Calculate depth level: parent's depth + 1
                var newDepthLevel = parentCategory.DepthLevel + 1;

                // Validate depth doesn't exceed max
                if (newDepthLevel > Category.MaxCategoryDepth - 1)
                    throw new InvalidOperationException($"Cannot create subcategory! Parent is at level {parentCategory.DepthLevel + 1}. Maximum allowed level is {Category.MaxCategoryDepth}.");

                // Update the entity's depth level
                entity.UpdateDepthLevel(newDepthLevel);

                // Calculate breadcrumb path: parent's breadcrumb + " > " + this category's name
                var breadcrumb = string.IsNullOrEmpty(parentCategory.BreadcrumbPath)
                    ? parentCategory.Name
                    : $"{parentCategory.BreadcrumbPath} > {entity.Name}";
                entity.UpdateBreadcrumbPath(breadcrumb);

                // Add it to the parent's subcategories
                parentCategory.SubCategories.Add(entity);
                parentCategory.IncrementChildCount();
            }
        }

        // Add the category to the list
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

    public async Task<IEnumerable<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var ancestors = new List<Category>();
        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);

        while (category?.ParentCategoryId.HasValue == true)
        {
            var parent = _categories.FirstOrDefault(c => c.Id == category.ParentCategoryId && !c.Isdeleted);
            if (parent == null)
                break;

            ancestors.Add(parent);
            category = parent;
        }

        return ancestors;
    }

    public async Task<IEnumerable<Category>> GetAllDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var descendants = new List<Category>();
        var queue = new Queue<Guid>(new[] { categoryId });

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = _categories.Where(c => c.ParentCategoryId == currentId && !c.Isdeleted).ToList();

            foreach (var child in children)
            {
                descendants.Add(child);
                queue.Enqueue(child.Id);
            }
        }

        return descendants;
    }

    public async Task<bool> WouldCreateCircularReferenceAsync(Guid categoryId, Guid? newParentId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        if (newParentId == null)
            return false;

        if (categoryId == newParentId)
            return true;

        // Check if newParent is a descendant of categoryId
        var descendants = await GetAllDescendantsAsync(categoryId, cancellationToken);
        return descendants.Any(d => d.Id == newParentId);
    }

    public async Task<int> GetDepthAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        int depth = 0;
        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);

        while (category?.ParentCategoryId.HasValue == true)
        {
            depth++;
            category = _categories.FirstOrDefault(c => c.Id == category.ParentCategoryId && !c.Isdeleted);
            if (category == null)
                break;
        }

        return depth;
    }

    public async Task<string> GetBreadcrumbPathAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(0, cancellationToken);

        var category = _categories.FirstOrDefault(c => c.Id == categoryId && !c.Isdeleted);
        if (category == null)
            return string.Empty;

        var path = new Stack<string>();
        var current = category;

        while (current != null)
        {
            path.Push(current.Name);
            if (!current.ParentCategoryId.HasValue)
                break;

            current = _categories.FirstOrDefault(c => c.Id == current.ParentCategoryId && !c.Isdeleted);
        }

        return string.Join(" > ", path);
    }
}
