namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a measurement unit (e.g., Pieces, Kilogram, Liter, etc.)
/// Used to define how parts are measured and stored in inventory
/// </summary>
public class Unit : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;  // e.g., "PC", "KG", "LTR"
    public string Symbol { get; private set; } = string.Empty;  // e.g., "pcs", "kg", "L"
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation properties
    public ICollection<Product> Parts { get; set; } = new List<Product>();
    public ICollection<UnitConversion> FromConversions { get; set; } = new List<UnitConversion>();
    public ICollection<UnitConversion> ToConversions { get; set; } = new List<UnitConversion>();

    // Private constructor for EF Core
    private Unit() { }

    /// <summary>
    /// Factory method to create a new Unit with validation
    /// </summary>
    public static Unit Create(string name, string code, string symbol, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Unit name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Unit code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Unit symbol cannot be empty", nameof(symbol));

        if (name.Length > 100)
            throw new ArgumentException("Unit name cannot exceed 100 characters", nameof(name));

        if (code.Length > 20)
            throw new ArgumentException("Unit code cannot exceed 20 characters", nameof(code));

        if (symbol.Length > 10)
            throw new ArgumentException("Unit symbol cannot exceed 10 characters", nameof(symbol));

        return new Unit
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpper(),
            Symbol = symbol.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsActive = true,
            DisplayOrder = 0
        };
    }

    /// <summary>
    /// Update unit details
    /// </summary>
    public void Update(string name, string code, string symbol, string description, bool isActive, int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Unit name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Unit code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Unit symbol cannot be empty", nameof(symbol));

        if (name.Length > 100)
            throw new ArgumentException("Unit name cannot exceed 100 characters", nameof(name));

        if (code.Length > 20)
            throw new ArgumentException("Unit code cannot exceed 20 characters", nameof(code));

        if (symbol.Length > 10)
            throw new ArgumentException("Unit symbol cannot exceed 10 characters", nameof(symbol));

        Name = name.Trim();
        Code = code.Trim().ToUpper();
        Symbol = symbol.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
        DisplayOrder = displayOrder;
    }

    /// <summary>
    /// Activate the unit
    /// </summary>
    public void Activate()
    {
        IsActive = true;
    }

    /// <summary>
    /// Deactivate the unit
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
    }
}
