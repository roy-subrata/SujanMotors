using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// In-memory implementation of CategoryRepository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class CategoryRepository(AutoPartDbContext dbContext) : ICategoryRepository
{
    public async Task<IEnumerable<Category>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.Where(c => !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(Category entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        if (dbContext.Categories.Any(c => c.Code == entity.Code && !c.Isdeleted))
            throw new InvalidOperationException($"Category with code '{entity.Code}' already exists");

        // If this category has a parent, calculate depth level and breadcrumb path
        if (entity.ParentCategoryId.HasValue)
        {
            var parentCategory = dbContext.Categories.FirstOrDefault(c => c.Id == entity.ParentCategoryId && !c.Isdeleted);
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
        dbContext.Categories.Add(entity);
        await dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Category entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        var existingCategory = dbContext.Categories.FirstOrDefault(c => c.Id == entity.Id && !c.Isdeleted);
        if (existingCategory == null)
            throw new InvalidOperationException($"Category with ID '{entity.Id}' not found");

        //update property here 
        existingCategory.Update(entity.Name, entity.Description, entity.DisplayOrder, entity.IsActive);
        existingCategory.UpdateBreadcrumbPath(entity.BreadcrumbPath);
        existingCategory.UpdateDepthLevel(entity.DepthLevel);
        existingCategory.UpdateChildCount(entity.ChildCount);

        await dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = dbContext.Categories.FirstOrDefault(c => c.Id == id && !c.Isdeleted);
        if (category == null)
            throw new InvalidOperationException($"Category with ID '{id}' not found");

        // Soft delete
        dbContext.Categories.Remove(category);
        await dbContext.SaveChangesAsync();
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.AnyAsync(c => c.Id == id && !c.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.Where(c => c.IsActive && !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetCategoriesWithSubcategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.Where(c => !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetTopLevelCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.Where(c => c.ParentCategoryId == null && !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Category>> GetSubcategoriesAsync(Guid parentCategoryId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Categories.Where(c => c.ParentCategoryId == parentCategoryId && !c.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.Where(c => c.Code == code && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.Where(c => c.Name == name && !c.Isdeleted);
        if (excludeId.HasValue)
            query = query.Where(c => c.Id != excludeId.Value);

        return await query.AnyAsync(cancellationToken);
    }

    public async Task<Category?> GetByCategoryCodeAsync(string code, CancellationToken cancellationToken = default)
    {

        return await dbContext.Categories.FirstOrDefaultAsync(c => c.Code == code && !c.Isdeleted, cancellationToken);
    }

    public async Task<Category?> GetByCategoryNameAsync(string name, CancellationToken cancellationToken = default)
    {

        return await dbContext.Categories.FirstOrDefaultAsync(c => c.Name == name && !c.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<Category>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        var lowerTerm = searchTerm.ToLowerInvariant();
        return await dbContext.Categories.Where(c =>
            !c.Isdeleted &&
            (c.Name.ToLowerInvariant().Contains(lowerTerm) ||
             c.Code.ToLowerInvariant().Contains(lowerTerm) ||
             c.Description.ToLowerInvariant().Contains(lowerTerm))
        ).ToListAsync(cancellationToken);
    }


    public async Task<IEnumerable<Category>> GetAncestorsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var ancestors = new List<Category>();
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && !c.Isdeleted, cancellationToken);

        while (category?.ParentCategoryId.HasValue == true)
        {
            var parent = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId && !c.Isdeleted, cancellationToken);
            if (parent == null)
                break;

            ancestors.Add(parent);
            category = parent;
        }

        return ancestors;
    }

    public async Task<IEnumerable<Category>> GetAllDescendantsAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var descendants = new List<Category>();
        var queue = new Queue<Guid>(new[] { categoryId });

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            var children = await dbContext.Categories.Where(c => c.ParentCategoryId == currentId && !c.Isdeleted).ToListAsync(cancellationToken);

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


        int depth = 0;
        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && !c.Isdeleted, cancellationToken);

        while (category?.ParentCategoryId.HasValue == true)
        {
            depth++;
            category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == category.ParentCategoryId && !c.Isdeleted, cancellationToken);
            if (category == null)
                break;
        }

        return depth;
    }

    public async Task<string> GetBreadcrumbPathAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {


        var category = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryId && !c.Isdeleted, cancellationToken);
        if (category == null)
            return string.Empty;

        var path = new Stack<string>();
        var current = category;

        while (current != null)
        {
            path.Push(current.Name);
            if (!current.ParentCategoryId.HasValue)
                break;

            current = await dbContext.Categories.FirstOrDefaultAsync(c => c.Id == current.ParentCategoryId && !c.Isdeleted, cancellationToken);
        }

        return string.Join(" > ", path);
    }

    public async Task<(IEnumerable<Category> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber = 10, int pageSize = 1, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Categories.Where(c => !c.Isdeleted);
        if (!string.IsNullOrEmpty(searchTerm))
        {
            var lowerTerm = searchTerm.ToLower();
            query = query.Where(x =>
             EF.Functions.Like(x.Name, $"%{searchTerm}%") || EF.Functions.Like(x.Code, $"%{searchTerm}%") || EF.Functions.Like(x.Description, $"%{searchTerm}%")
          );
        }
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
