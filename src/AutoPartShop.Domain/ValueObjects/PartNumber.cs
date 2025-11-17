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
        if (value.Length < 3 || value.Length > 20)
            throw new ArgumentException("Part number must be between 3 and 20 characters", nameof(value));

        // Example: Ensure part number starts with a letter (customize as needed)
        if (!char.IsLetter(value[0]))
            throw new ArgumentException("Part number must start with a letter", nameof(value));

        return new PartNumber(value);
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