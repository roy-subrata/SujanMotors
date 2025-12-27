namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a vehicle model for part compatibility
/// </summary>
public class Vehicle : AuditableEntity
{
    public string Make { get; private set; } = string.Empty;  // Brand (Toyota, Honda, Ford, etc.)
    public string Model { get; private set; } = string.Empty;  // Model name
    public int Year { get; private set; }  // Manufacturing year
    public string EngineType { get; private set; } = string.Empty;  // Petrol, Diesel, Hybrid, Electric
    public string Description { get; private set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public ICollection<PartVehicleCompatibility> PartCompatibilities { get; set; } = new List<PartVehicleCompatibility>();

    private Vehicle() { }

    public static Vehicle Create(string make, string model, int year, string engineType, string description = "")
    {
        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("Make cannot be empty", nameof(make));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty", nameof(model));

        if (year < 1900 || year > DateTime.Now.Year + 1)
            throw new ArgumentException("Year must be valid", nameof(year));

        if (string.IsNullOrWhiteSpace(engineType))
            throw new ArgumentException("EngineType cannot be empty", nameof(engineType));

        if (make.Length > 50)
            throw new ArgumentException("Make cannot exceed 50 characters", nameof(make));

        if (model.Length > 100)
            throw new ArgumentException("Model cannot exceed 100 characters", nameof(model));

        if (engineType.Length > 50)
            throw new ArgumentException("EngineType cannot exceed 50 characters", nameof(engineType));

        return new Vehicle
        {
            Make = make.Trim(),
            Model = model.Trim(),
            Year = year,
            EngineType = engineType.Trim(),
            Description = description?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void Update(string make, string model, int year, string engineType, string description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("Make cannot be empty", nameof(make));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be empty", nameof(model));

        if (year < 1900 || year > DateTime.Now.Year + 1)
            throw new ArgumentException("Year must be valid", nameof(year));

        if (string.IsNullOrWhiteSpace(engineType))
            throw new ArgumentException("EngineType cannot be empty", nameof(engineType));

        Make = make.Trim();
        Model = model.Trim();
        Year = year;
        EngineType = engineType.Trim();
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
