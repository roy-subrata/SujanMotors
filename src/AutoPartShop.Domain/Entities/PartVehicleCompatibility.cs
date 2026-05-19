namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents the compatibility relationship between a part and a vehicle
/// </summary>
public class PartVehicleCompatibility : AuditableEntity
{
    public Guid PartId { get; private set; }
    public Guid VehicleId { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public bool IsCompatible { get; private set; } = true;

    // Navigation properties
    public Product? Part { get; set; }
    public Vehicle? Vehicle { get; set; }

    private PartVehicleCompatibility() { }

    public static PartVehicleCompatibility Create(Guid partId, Guid vehicleId, bool isCompatible = true, string notes = "")
    {
        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (vehicleId == Guid.Empty)
            throw new ArgumentException("VehicleId cannot be empty", nameof(vehicleId));

        if (partId == vehicleId)
            throw new InvalidOperationException("PartId and VehicleId cannot be the same");

        return new PartVehicleCompatibility
        {
            PartId = partId,
            VehicleId = vehicleId,
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
