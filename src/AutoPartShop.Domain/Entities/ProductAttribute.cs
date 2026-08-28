
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

    /// <summary>
    /// Which entity level this attribute attaches to: "product" (e.g. Material) or
    /// "variant" (e.g. Side, Color). Defaults to "variant" for backward compatibility with
    /// attributes created before this flag existed.
    /// </summary>
    public string Scope { get; private set; } = "variant"; // product, variant

    public ProductAttributeGroup? AttributeGroup { get; set; }
    public ICollection<ProductAttributeOption> Options { get; set; } = new List<ProductAttributeOption>();

    private ProductAttribute() { }

    public static ProductAttribute Create(
        Guid attributeGroupId,
        string name,
        string code,
        string dataType = "text",
        string unit = "",
        bool isActive = true,
        string scope = "variant")
    {
        if (attributeGroupId == Guid.Empty)
            throw new ArgumentException("AttributeGroupId cannot be empty", nameof(attributeGroupId));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        var resolvedScope = NormalizeScope(scope);

        return new ProductAttribute
        {
            AttributeGroupId = attributeGroupId,
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            DataType = string.IsNullOrWhiteSpace(dataType) ? "text" : dataType.Trim().ToLowerInvariant(),
            Unit = unit?.Trim() ?? string.Empty,
            IsActive = isActive,
            Scope = resolvedScope
        };
    }

    public void Update(string name, string code, string dataType, string unit, bool isActive, string scope = "variant")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        var resolvedScope = NormalizeScope(scope);

        Name = name.Trim();
        Code = code.Trim().ToUpperInvariant();
        DataType = string.IsNullOrWhiteSpace(dataType) ? "text" : dataType.Trim().ToLowerInvariant();
        Unit = unit?.Trim() ?? string.Empty;
        IsActive = isActive;
        Scope = resolvedScope;
    }

    private static string NormalizeScope(string scope)
    {
        var normalized = string.IsNullOrWhiteSpace(scope) ? "variant" : scope.Trim().ToLowerInvariant();
        if (normalized != "product" && normalized != "variant")
            throw new ArgumentException("Scope must be 'product' or 'variant'", nameof(scope));
        return normalized;
    }
}
