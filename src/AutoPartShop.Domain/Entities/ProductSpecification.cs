namespace AutoPartShop.Domain.Entities;

/// <summary>
/// A simple descriptive spec on a product, e.g. Label "Material" / Value
/// "Ceramic". Deliberately product-scoped free text (not the variant-scoped
/// EAV attribute system) so shop staff can edit specs quickly.
///
/// <see cref="Key"/> is a normalized slug of the label so that "Material",
/// "material" and " Material " collapse to one facet when these specs power
/// ecommerce filters later.
/// </summary>
public class ProductSpecification : AuditableEntity
{
    public Guid PartId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public string Key { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public int DisplayOrder { get; private set; }

    // Navigation
    public Product? Part { get; set; }

    private ProductSpecification() { }

    public static ProductSpecification Create(Guid partId, string label, string value, int displayOrder)
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be empty", nameof(label));

        return new ProductSpecification
        {
            PartId = partId,
            Label = label.Trim(),
            Key = Normalize(label),
            Value = value?.Trim() ?? string.Empty,
            DisplayOrder = displayOrder,
        };
    }

    /// <summary>
    /// Lowercases, trims and collapses whitespace/separators to single dashes so
    /// label variants map to the same filter key. "Front Axle" -> "front-axle".
    /// </summary>
    public static string Normalize(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return string.Empty;
        var lowered = label.Trim().ToLowerInvariant();
        var chars = new char[lowered.Length];
        var len = 0;
        var lastDash = false;
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                chars[len++] = c;
                lastDash = false;
            }
            else if (!lastDash && len > 0)
            {
                chars[len++] = '-';
                lastDash = true;
            }
        }
        var result = new string(chars, 0, len);
        return result.TrimEnd('-');
    }
}
