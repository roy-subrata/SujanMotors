using AutoPartShop.Application.Categories.Dtos;
using AutoPartShop.Application.Catgories;
using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Application.Categories;

/// <summary>
/// In-memory implementation of CategoryRepository
/// TODO: Replace with Entity Framework Core implementation when database is configured
/// </summary>
public class CategoryReadRepository(AutoPartDbContext dbContext) : ICategoryReadRepository
{
    public async Task<(List<CategoryResponse> categoryResponse, int total)> FindAllyAsync(CategoryQuery query, CancellationToken cancellationToken = default)
    {
        var categories = dbContext.Categories.Where(c => !c.Isdeleted);
        
        // Apply search filter
        if (!string.IsNullOrEmpty(query.Search))
        {
            var lowerTerm = query.Search.ToLower();
            categories = categories.Where(x =>
             EF.Functions.Like(x.Name, $"%{lowerTerm}%") || EF.Functions.Like(x.Code, $"%{lowerTerm}%") || EF.Functions.Like(x.Description, $"%{lowerTerm}%")
          );
        }
        
        // Apply status filter
        if (query.IsActive.HasValue)
        {
            categories = categories.Where(x => x.IsActive == query.IsActive.Value);
        }
        
        // Apply sorting
        if (query.Sorts != null && query.Sorts.Count > 0)
        {
            var firstSort = query.Sorts[0];
            IOrderedQueryable<Category> orderedCategories;
            
            // Apply first sort
            orderedCategories = firstSort.Field.ToLower() switch
            {
                "name" => firstSort.IsAscending ? categories.OrderBy(c => c.Name) : categories.OrderByDescending(c => c.Name),
                "code" => firstSort.IsAscending ? categories.OrderBy(c => c.Code) : categories.OrderByDescending(c => c.Code),
                "displayorder" => firstSort.IsAscending ? categories.OrderBy(c => c.DisplayOrder) : categories.OrderByDescending(c => c.DisplayOrder),
                "isactive" => firstSort.IsAscending ? categories.OrderBy(c => c.IsActive) : categories.OrderByDescending(c => c.IsActive),
                "createddate" => firstSort.IsAscending ? categories.OrderBy(c => c.CreatedDate) : categories.OrderByDescending(c => c.CreatedDate),
                _ => categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            };
            
            // Apply additional sorts
            for (int i = 1; i < query.Sorts.Count; i++)
            {
                var sort = query.Sorts[i];
                orderedCategories = sort.Field.ToLower() switch
                {
                    "name" => sort.IsAscending ? orderedCategories.ThenBy(c => c.Name) : orderedCategories.ThenByDescending(c => c.Name),
                    "code" => sort.IsAscending ? orderedCategories.ThenBy(c => c.Code) : orderedCategories.ThenByDescending(c => c.Code),
                    "displayorder" => sort.IsAscending ? orderedCategories.ThenBy(c => c.DisplayOrder) : orderedCategories.ThenByDescending(c => c.DisplayOrder),
                    "isactive" => sort.IsAscending ? orderedCategories.ThenBy(c => c.IsActive) : orderedCategories.ThenByDescending(c => c.IsActive),
                    "createddate" => sort.IsAscending ? orderedCategories.ThenBy(c => c.CreatedDate) : orderedCategories.ThenByDescending(c => c.CreatedDate),
                    _ => orderedCategories
                };
            }
            
            categories = orderedCategories;
        }
        else
        {
            // Default sorting
            categories = categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name);
        }
        
        var totalCount = await categories.CountAsync(cancellationToken);

        var items = await categories
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);


        return (items.Select(x => MapToResponse(x)).ToList(), totalCount);
    }

    private CategoryResponse MapToResponse(Category category, int depth = 0, int maxDepth = 3)
    {
        // Prevent deep recursion to avoid serialization issues
        var subcategories = (depth < maxDepth && category.SubCategories != null)
            ? category.SubCategories.Select(sc => MapToResponse(sc, depth + 1, maxDepth)).ToList()
            : new List<CategoryResponse>();

        return new CategoryResponse
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Code = category.Code,
            ParentCategoryId = category.ParentCategoryId,
            IsActive = category.IsActive,
            DisplayOrder = category.DisplayOrder,
            BreadcrumbPath = category.BreadcrumbPath,
            DepthLevel = category.DepthLevel,
            ChildCount = category.ChildCount,
            SubCategories = subcategories
        };
    }
}
