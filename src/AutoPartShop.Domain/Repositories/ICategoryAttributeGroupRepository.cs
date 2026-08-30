using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// Repository for the category &lt;-&gt; attribute group links that scope which attribute groups
/// apply to a category (and, via lookup, to its descendants).
/// </summary>
public interface ICategoryAttributeGroupRepository : IBaseRepository<CategoryAttributeGroup>
{
    /// <summary>
    /// Attribute groups linked to any of the given category ids, with nested Attributes+Options
    /// loaded (same shape as <c>GET /api/v1/attribute-groups</c>).
    /// </summary>
    Task<IEnumerable<ProductAttributeGroup>> GetByCategoryIdsAsync(IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);

    /// <summary>Category ids currently linked to the given attribute group.</summary>
    Task<IEnumerable<Guid>> GetCategoryIdsForGroupAsync(Guid attributeGroupId, CancellationToken cancellationToken = default);

    /// <summary>Full-replace: deletes existing links for the group, then inserts one link per category id.</summary>
    Task ReplaceForGroupAsync(Guid attributeGroupId, IEnumerable<Guid> categoryIds, CancellationToken cancellationToken = default);
}
