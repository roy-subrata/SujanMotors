namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Links an attribute group to a category, scoping which attribute groups show up as filters (and
/// as editable spec sections) for products in that category. A group linked to a parent category
/// also applies to its descendants — the consuming query resolves that, this entity is just the
/// direct link.
/// </summary>
public class CategoryAttributeGroup : AuditableEntity
{
    public Guid CategoryId { get; private set; }
    public Guid AttributeGroupId { get; private set; }

    public Category? Category { get; set; }
    public ProductAttributeGroup? AttributeGroup { get; set; }

    private CategoryAttributeGroup() { }

    public static CategoryAttributeGroup Create(Guid categoryId, Guid attributeGroupId)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (attributeGroupId == Guid.Empty)
            throw new ArgumentException("AttributeGroupId cannot be empty", nameof(attributeGroupId));

        return new CategoryAttributeGroup
        {
            CategoryId = categoryId,
            AttributeGroupId = attributeGroupId
        };
    }
}
