namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a warehouse/storage location for inventory
/// </summary>
public class Warehouse : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;  // Warehouse code
    public string Location { get; private set; } = string.Empty;  // Physical location/address
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Manager { get; private set; } = string.Empty;  // Manager name
    public string ManagerEmail { get; set; } = string.Empty;
    public string ManagerPhone { get; set; } = string.Empty;
    public decimal StorageCapacity { get; set; } = 0;  // Maximum storage in units
    public string CapacityUnit { get; set; } = "SQM";  // Square meters, cubic meters, etc.
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; private set; } = true;

    private Warehouse() { }

    public static Warehouse Create(string name, string code, string location, string city,
        string state, string country, string postalCode, string manager = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty", nameof(location));

        if (name.Length > 150)
            throw new ArgumentException("Name cannot exceed 150 characters", nameof(name));

        if (code.Length > 30)
            throw new ArgumentException("Code cannot exceed 30 characters", nameof(code));

        return new Warehouse
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpper(),
            Location = location.Trim(),
            City = city?.Trim() ?? string.Empty,
            State = state?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
            PostalCode = postalCode?.Trim() ?? string.Empty,
            Manager = manager?.Trim() ?? string.Empty,
            IsActive = true,
            CapacityUnit = "SQM"
        };
    }

    public void Update(string name, string location, string city, string state,
        string country, string postalCode, string manager, string managerEmail,
        string managerPhone, decimal storageCapacity, string capacityUnit,
        string description, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException("Location cannot be empty", nameof(location));

        if (storageCapacity < 0)
            throw new ArgumentException("Storage capacity cannot be negative", nameof(storageCapacity));

        Name = name.Trim();
        Location = location.Trim();
        City = city?.Trim() ?? string.Empty;
        State = state?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
        PostalCode = postalCode?.Trim() ?? string.Empty;
        Manager = manager?.Trim() ?? string.Empty;
        ManagerEmail = managerEmail?.Trim() ?? string.Empty;
        ManagerPhone = managerPhone?.Trim() ?? string.Empty;
        StorageCapacity = storageCapacity;
        CapacityUnit = capacityUnit?.Trim() ?? "SQM";
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
