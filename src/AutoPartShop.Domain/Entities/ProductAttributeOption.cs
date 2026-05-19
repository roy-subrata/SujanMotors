namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Discrete option for an attribute (e.g., 8GB, 16GB for RAM).
/// </summary>
public class ProductAttributeOption : AuditableEntity
{
    public Guid AttributeId { get; private set; }
    public string Value { get; private set; } = string.Empty;
    public int SortOrder { get; private set; } = 0;

    public ProductAttribute? Attribute { get; set; }

    private ProductAttributeOption() { }

    public static ProductAttributeOption Create(Guid attributeId, string value, int sortOrder = 0)
    {
        if (attributeId == Guid.Empty)
            throw new ArgumentException("AttributeId cannot be empty", nameof(attributeId));

        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty", nameof(value));

        return new ProductAttributeOption
        {
            AttributeId = attributeId,
            Value = value.Trim(),
            SortOrder = sortOrder < 0 ? 0 : sortOrder
        };
    }

    public void Update(string value, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be empty", nameof(value));

        Value = value.Trim();
        SortOrder = sortOrder < 0 ? 0 : sortOrder;
    }
}
