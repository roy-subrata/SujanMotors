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
        if (!string.IsNullOrEmpty(query.Search))
        {
            var lowerTerm = query.Search.ToLower();
            categories = categories.Where(x =>
             EF.Functions.Like(x.Name, $"%{lowerTerm}%") || EF.Functions.Like(x.Code, $"%{lowerTerm}%") || EF.Functions.Like(x.Description, $"%{lowerTerm}%")
          );
        }
        var totalCount = await categories.CountAsync(cancellationToken);

        var items = await categories
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
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
            CreatedBy = category.CreatedBy,
            ModifiedBy = category.ModifiedBy,
            BreadcrumbPath = category.BreadcrumbPath,
            DepthLevel = category.DepthLevel,
            ChildCount = category.ChildCount,
            SubCategories = subcategories
        };
    }
}
