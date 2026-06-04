namespace AutoPartShop.Domain.Common;

/// <summary>
/// Composes the user-facing product display name from a base part name and a variant label.
/// Defensive against legacy/seed data where the variant name already embeds the full part
/// name (e.g. variant "Bosch Alternator 65A — Standard" under part "Bosch Alternator 65A"),
/// which would otherwise produce a duplicated "Part - Part — Label".
/// Convention: Variant.Name should be the short label only (e.g. "LH", "Standard").
/// </summary>
public static class VariantNaming
{
    public static string Compose(string? partName, string? variantName)
    {
        var part = (partName ?? string.Empty).Trim();
        var variant = (variantName ?? string.Empty).Trim();

        if (variant.Length == 0) return part;
        if (part.Length == 0) return variant;
        // Variant label already contains the part name → don't prepend it again.
        if (variant.StartsWith(part, System.StringComparison.OrdinalIgnoreCase)) return variant;
        return $"{part} - {variant}";
    }
}
