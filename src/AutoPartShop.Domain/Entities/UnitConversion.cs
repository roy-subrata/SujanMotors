namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a conversion rate between two units
/// Example: 1000 grams = 1 kilogram
/// </summary>
public class UnitConversion : AuditableEntity
{
    public Guid FromUnitId { get; private set; }
    public Guid ToUnitId { get; private set; }
    public decimal ConversionFactor { get; private set; }  // Multiply FromUnit value by this to get ToUnit value
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Unit FromUnit { get; set; } = null!;
    public Unit ToUnit { get; set; } = null!;

    // Private constructor for EF Core
    private UnitConversion() { }

    /// <summary>
    /// Factory method to create a unit conversion with validation
    /// </summary>
    public static UnitConversion Create(Guid fromUnitId, Guid toUnitId, decimal conversionFactor, string description = "")
    {
        if (fromUnitId == Guid.Empty)
            throw new ArgumentException("FromUnitId cannot be empty", nameof(fromUnitId));

        if (toUnitId == Guid.Empty)
            throw new ArgumentException("ToUnitId cannot be empty", nameof(toUnitId));

        if (fromUnitId == toUnitId)
            throw new InvalidOperationException("FromUnitId and ToUnitId cannot be the same");

        if (conversionFactor <= 0)
            throw new ArgumentException("ConversionFactor must be greater than 0", nameof(conversionFactor));

        return new UnitConversion
        {
            FromUnitId = fromUnitId,
            ToUnitId = toUnitId,
            ConversionFactor = conversionFactor,
            Description = description?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    /// <summary>
    /// Update unit conversion details
    /// </summary>
    public void Update(decimal conversionFactor, string description, bool isActive)
    {
        if (conversionFactor <= 0)
            throw new ArgumentException("ConversionFactor must be greater than 0", nameof(conversionFactor));

        ConversionFactor = conversionFactor;
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    /// <summary>
    /// Activate the conversion
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate the conversion
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }

    /// <summary>
    /// Convert a value from the FromUnit to ToUnit
    /// </summary>
    public decimal Convert(decimal value)
    {
        if (!IsActive)
            throw new InvalidOperationException("This conversion is not active and cannot be used");

        return value * ConversionFactor;
    }

    /// <summary>
    /// Convert a value from the ToUnit back to FromUnit
    /// </summary>
    public decimal ConvertReverse(decimal value)
    {
        if (!IsActive)
            throw new InvalidOperationException("This conversion is not active and cannot be used");

        if (ConversionFactor == 0)
            throw new InvalidOperationException("ConversionFactor cannot be zero");

        return value / ConversionFactor;
    }
}
