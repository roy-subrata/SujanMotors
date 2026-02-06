namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Generic compatibility rule between parts, variants, or attribute options.
/// </summary>
public class CompatibilityRule : AuditableEntity
{
    public string SourceType { get; private set; } = string.Empty; // Part, Variant, AttributeOption
    public Guid SourceId { get; private set; }
    public string TargetType { get; private set; } = string.Empty; // Part, Variant, AttributeOption
    public Guid TargetId { get; private set; }
    public bool IsCompatible { get; private set; } = true;
    public string Notes { get; private set; } = string.Empty;

    private CompatibilityRule() { }

    public static CompatibilityRule Create(
        string sourceType,
        Guid sourceId,
        string targetType,
        Guid targetId,
        bool isCompatible = true,
        string notes = "")
    {
        if (string.IsNullOrWhiteSpace(sourceType))
            throw new ArgumentException("SourceType cannot be empty", nameof(sourceType));

        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("TargetType cannot be empty", nameof(targetType));

        if (sourceId == Guid.Empty)
            throw new ArgumentException("SourceId cannot be empty", nameof(sourceId));

        if (targetId == Guid.Empty)
            throw new ArgumentException("TargetId cannot be empty", nameof(targetId));

        return new CompatibilityRule
        {
            SourceType = sourceType.Trim(),
            SourceId = sourceId,
            TargetType = targetType.Trim(),
            TargetId = targetId,
            IsCompatible = isCompatible,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void Update(bool isCompatible, string notes)
    {
        IsCompatible = isCompatible;
        Notes = notes?.Trim() ?? string.Empty;
    }
}
