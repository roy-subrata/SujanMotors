
namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Defines a product attribute (e.g., RAM, Processor, Storage).
/// </summary>
public class ProductAttribute : AuditableEntity
{
    public Guid AttributeGroupId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string DataType { get; private set; } = "text"; // text, number, boolean, option
    public string Unit { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    public ProductAttributeGroup? AttributeGroup { get; set; }
    public ICollection<ProductAttributeOption> Options { get; set; } = new List<ProductAttributeOption>();

    private ProductAttribute() { }

    public static ProductAttribute Create(
        Guid attributeGroupId,
        string name,
        string code,
        string dataType = "text",
        string unit = "",
        bool isActive = true)
    {
        if (attributeGroupId == Guid.Empty)
            throw new ArgumentException("AttributeGroupId cannot be empty", nameof(attributeGroupId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        return new ProductAttribute
        {
            AttributeGroupId = attributeGroupId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            DataType = string.IsNullOrWhiteSpace(dataType) ? "text" : dataType.Trim().ToLowerInvariant(),
            Unit = unit?.Trim() ?? string.Empty,
            IsActive = isActive
        };
    }

    public void Update(string name, string code, string dataType, string unit, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        DataType = string.IsNullOrWhiteSpace(dataType) ? "text" : dataType.Trim().ToLowerInvariant();
        Unit = unit?.Trim() ?? string.Empty;
        IsActive = isActive;
    }
}
