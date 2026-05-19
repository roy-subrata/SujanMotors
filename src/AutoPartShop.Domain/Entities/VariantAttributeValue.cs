namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Attribute values assigned to a product variant.
/// </summary>
public class VariantAttributeValue : AuditableEntity
{
    public Guid VariantId { get; private set; }
    public Guid AttributeId { get; private set; }
    public Guid? OptionId { get; private set; }
    public string ValueText { get; private set; } = string.Empty;
    public decimal? ValueNumber { get; private set; }
    public bool? ValueBool { get; private set; }

    public ProductVariant? Variant { get; set; }
    public ProductAttribute? Attribute { get; set; }
    public ProductAttributeOption? Option { get; set; }

    private VariantAttributeValue() { }

    public static VariantAttributeValue Create(
        Guid variantId,
        Guid attributeId,
        Guid? optionId = null,
        string valueText = "",
        decimal? valueNumber = null,
        bool? valueBool = null)
    {
        if (variantId == Guid.Empty)
            throw new ArgumentException("VariantId cannot be empty", nameof(variantId));

        if (attributeId == Guid.Empty)
            throw new ArgumentException("AttributeId cannot be empty", nameof(attributeId));

        return new VariantAttributeValue
        {
            VariantId = variantId,
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
