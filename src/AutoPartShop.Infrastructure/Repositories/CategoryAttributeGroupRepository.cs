using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class CategoryAttributeGroupRepository(AutoPartDbContext _db) : ICategoryAttributeGroupRepository
{
    public async Task<IEnumerable<CategoryAttributeGroup>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.CategoryAttributeGroups.Where(x => !x.Isdeleted).ToListAsync(cancellationToken);
    }

    public async Task<CategoryAttributeGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.CategoryAttributeGroups.FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task AddAsync(CategoryAttributeGroup entity, CancellationToken cancellationToken = default)
    {
        _db.CategoryAttributeGroups.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task UpdateAsync(CategoryAttributeGroup entity, CancellationToken cancellationToken = default)
    {
        // Nothing mutable on the link itself — replace via ReplaceForGroupAsync instead.
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _db.CategoryAttributeGroups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity != null)
        {
            _db.CategoryAttributeGroups.Remove(entity);
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.CategoryAttributeGroups.AnyAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);
    }

    public async Task<IEnumerable<ProductAttributeGroup>> GetByCategoryIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var ids = categoryIds.Distinct().ToList();
        if (ids.Count == 0) return [];

        var groupIds = await _db.CategoryAttributeGroups
            .Where(x => !x.Isdeleted && ids.Contains(x.CategoryId))
            .Select(x => x.AttributeGroupId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (groupIds.Count == 0) return [];

        return await _db.ProductAttributeGroups
            .Where(g => groupIds.Contains(g.Id))
            .OrderBy(g => g.SortOrder).ThenBy(g => g.Name)
            .Include(g => g.Attributes.OrderBy(a => a.Name))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Guid>> GetCategoryIdsForGroupAsync(Guid attributeGroupId, CancellationToken cancellationToken = default)
    {
        return await _db.CategoryAttributeGroups
            .Where(x => !x.Isdeleted && x.AttributeGroupId == attributeGroupId)
            .Select(x => x.CategoryId)
            .ToListAsync(cancellationToken);
    }

    public async Task ReplaceForGroupAsync(Guid attributeGroupId, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default)
    {
        var existing = await _db.CategoryAttributeGroups
            .Where(x => x.AttributeGroupId == attributeGroupId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _db.CategoryAttributeGroups.RemoveRange(existing);

        var distinctCategoryIds = categoryIds.Distinct().ToList();
        foreach (var categoryId in distinctCategoryIds)
            _db.CategoryAttributeGroups.Add(CategoryAttributeGroup.Create(categoryId, attributeGroupId));

        await _db.SaveChangesAsync(cancellationToken);
    }
}
