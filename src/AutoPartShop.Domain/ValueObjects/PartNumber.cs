// src/Domain/ValueObjects/PartNumber.cs
namespace AutoPartsShop.Domain.Entities;

public class PartNumber
{
    public string Value { get; private set; }

    private PartNumber(string value)
    {
        Value = value;
    }

    public static PartNumber Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Part number cannot be empty", nameof(value));

        var trimmed = value.Trim();
        if (trimmed.Length < 1 || trimmed.Length > 30)
            throw new ArgumentException("Part number must be between 1 and 30 characters", nameof(value));

        return new PartNumber(trimmed);
    }

    public override bool Equals(object? obj)
    {
        if (obj is PartNumber other)
            return Value.Equals(other.Value, StringComparison.OrdinalIgnoreCase);
        return false;
    }

    public override int GetHashCode() => Value.GetHashCode(StringComparison.OrdinalIgnoreCase);

    public override string ToString() => Value;
}