namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Links a category to the attributes used for filters and specifications.
/// </summary>
public class CategoryAttribute : AuditableEntity
{
    public Guid CategoryId { get; private set; }
    public Guid AttributeId { get; private set; }
    public bool IsRequired { get; private set; } = false;
    public bool IsFilterable { get; private set; } = true;
    public string FilterType { get; private set; } = "select"; // select, multi, range
    public int SortOrder { get; private set; } = 0;

    public Category? Category { get; set; }
    public ProductAttribute? Attribute { get; set; }

    private CategoryAttribute() { }

    public static CategoryAttribute Create(
        Guid categoryId,
        Guid attributeId,
        bool isRequired = false,
        bool isFilterable = true,
        string filterType = "select",
        int sortOrder = 0)
    {
        if (categoryId == Guid.Empty)
            throw new ArgumentException("CategoryId cannot be empty", nameof(categoryId));

        if (attributeId == Guid.Empty)
            throw new ArgumentException("AttributeId cannot be empty", nameof(attributeId));

        return new CategoryAttribute
        {
            CategoryId = categoryId,
            AttributeId = attributeId,
            IsRequired = isRequired,
            IsFilterable = isFilterable,
            FilterType = string.IsNullOrWhiteSpace(filterType) ? "select" : filterType.Trim().ToLowerInvariant(),
            SortOrder = sortOrder < 0 ? 0 : sortOrder
        };
    }

    public void Update(bool isRequired, bool isFilterable, string filterType, int sortOrder)
    {
        IsRequired = isRequired;
        IsFilterable = isFilterable;
        FilterType = string.IsNullOrWhiteSpace(filterType) ? "select" : filterType.Trim().ToLowerInvariant();
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
    }
}
