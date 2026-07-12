namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a real vehicle owned by a customer (their actual car), keyed by registration/plate.
/// Distinct from <see cref="Vehicle"/>, which is a Make/Model/Year catalog used for part compatibility.
/// A purchase (sales order) can optionally be linked to one of the customer's vehicles.
/// </summary>
public class CustomerVehicle : AuditableEntity
{
    public Guid CustomerId { get; private set; }
    public string RegistrationNo { get; private set; } = string.Empty;  // Primary human identifier (plate no.)
    public string VIN { get; private set; } = string.Empty;             // Optional chassis/VIN
    public string Make { get; private set; } = string.Empty;            // Brand (Toyota, Honda, etc.)
    public string Model { get; private set; } = string.Empty;
    public int? Year { get; private set; }
    public string EngineType { get; private set; } = string.Empty;      // Petrol, Diesel, Hybrid, Electric
    public string Color { get; private set; } = string.Empty;
    public int? Mileage { get; private set; }                           // Odometer reading
    public string Notes { get; private set; } = string.Empty;
    public Guid? CatalogVehicleId { get; private set; }                 // Optional link to catalog Vehicle (parts-fit)
    public bool IsActive { get; private set; } = true;

    // Navigation properties
    public Customer? Customer { get; set; }
    public Vehicle? CatalogVehicle { get; set; }

    private CustomerVehicle() { }

    public static CustomerVehicle Create(Guid customerId, string registrationNo, string make,
        string model, int? year = null, string engineType = "", string vin = "",
        string color = "", int? mileage = null, string notes = "", Guid? catalogVehicleId = null)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("CustomerId cannot be empty", nameof(customerId));

        if (string.IsNullOrWhiteSpace(registrationNo))
            throw new ArgumentException("RegistrationNo cannot be empty", nameof(registrationNo));

        if (year.HasValue && (year < 1900 || year > DateTime.Now.Year + 1))
            throw new ArgumentException("Year must be valid", nameof(year));

        if (mileage.HasValue && mileage < 0)
            throw new ArgumentException("Mileage cannot be negative", nameof(mileage));

        return new CustomerVehicle
        {
            CustomerId = customerId,
            RegistrationNo = registrationNo.Trim().ToUpper(),
            VIN = vin?.Trim() ?? string.Empty,
            Make = make?.Trim() ?? string.Empty,
            Model = model?.Trim() ?? string.Empty,
            Year = year,
            EngineType = engineType?.Trim() ?? string.Empty,
            Color = color?.Trim() ?? string.Empty,
            Mileage = mileage,
            Notes = notes?.Trim() ?? string.Empty,
            CatalogVehicleId = catalogVehicleId,
            IsActive = true
        };
    }

    public void Update(string registrationNo, string make, string model, int? year,
        string engineType, string vin, string color, int? mileage, string notes,
        Guid? catalogVehicleId)
    {
        if (string.IsNullOrWhiteSpace(registrationNo))
            throw new ArgumentException("RegistrationNo cannot be empty", nameof(registrationNo));

        if (year.HasValue && (year < 1900 || year > DateTime.Now.Year + 1))
            throw new ArgumentException("Year must be valid", nameof(year));

        if (mileage.HasValue && mileage < 0)
            throw new ArgumentException("Mileage cannot be negative", nameof(mileage));

        RegistrationNo = registrationNo.Trim().ToUpper();
        VIN = vin?.Trim() ?? string.Empty;
        Make = make?.Trim() ?? string.Empty;
        Model = model?.Trim() ?? string.Empty;
        Year = year;
        EngineType = engineType?.Trim() ?? string.Empty;
        Color = color?.Trim() ?? string.Empty;
        Mileage = mileage;
        Notes = notes?.Trim() ?? string.Empty;
        CatalogVehicleId = catalogVehicleId;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    /// <summary>
    /// Human-friendly label for denormalized display, e.g. "Toyota Corolla (DHA-1234)".
    /// </summary>
    public string GetLabel()
    {
        var name = $"{Make} {Model}".Trim();
        return string.IsNullOrEmpty(name)
            ? RegistrationNo
            : $"{name} ({RegistrationNo})";
    }
}
