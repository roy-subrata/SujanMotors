namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Groups attributes for PDP specification sections.
/// </summary>
public class ProductAttributeGroup : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public int SortOrder { get; private set; } = 0;
    public bool IsActive { get; private set; } = true;

    public ICollection<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();

    private ProductAttributeGroup() { }

    public static ProductAttributeGroup Create(string name, int sortOrder = 0, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        return new ProductAttributeGroup
        {
            Name = name.Trim(),
            SortOrder = sortOrder < 0 ? 0 : sortOrder,
            IsActive = isActive
        };
    }

    public void Update(string name, int sortOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
        IsActive = isActive;
    }
}
