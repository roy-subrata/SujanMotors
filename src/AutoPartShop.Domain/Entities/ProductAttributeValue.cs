namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Attribute values assigned to a product (base-level, not a specific variant). Mirrors
/// <see cref="VariantAttributeValue"/> exactly, keyed by ProductId instead of VariantId. A given
/// attribute may have a row here AND rows in <see cref="VariantAttributeValue"/> for different
/// products — the controllers enforce only that the SAME product can't use an attribute both ways
/// at once (see ProductsController/ProductVariantController).
/// </summary>
public class ProductAttributeValue : AuditableEntity
{
    public Guid ProductId { get; private set; }
    public Guid AttributeId { get; private set; }
    public Guid? OptionId { get; private set; }
    public string ValueText { get; private set; } = string.Empty;
    public decimal? ValueNumber { get; private set; }
    public bool? ValueBool { get; private set; }

    public Product? Product { get; set; }
    public ProductAttribute? Attribute { get; set; }
    public ProductAttributeOption? Option { get; set; }

    private ProductAttributeValue() { }

    public static ProductAttributeValue Create(
        Guid productId,
        Guid attributeId,
        Guid? optionId = null,
        string valueText = "",
        decimal? valueNumber = null,
        bool? valueBool = null)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty", nameof(productId));

        if (attributeId == Guid.Empty)
            throw new ArgumentException("AttributeId cannot be empty", nameof(attributeId));

        return new ProductAttributeValue
        {
            ProductId = productId,
            AttributeId = attributeId,
            OptionId = optionId,
            ValueText = valueText?.Trim() ?? string.Empty,
            ValueNumber = valueNumber,
            ValueBool = valueBool
        };
    }

    public void Update(Guid? optionId, string valueText, decimal? valueNumber, bool? valueBool)
    {
        OptionId = optionId;
        ValueText = valueText?.Trim() ?? string.Empty;
        ValueNumber = valueNumber;
        ValueBool = valueBool;
    }
}
