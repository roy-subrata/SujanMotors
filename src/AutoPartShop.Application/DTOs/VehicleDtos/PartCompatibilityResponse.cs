namespace AutoPartShop.Application.DTOs.VehicleDtos;

public class PartCompatibilityResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public Guid VehicleId { get; set; }
    public string VehicleInfo { get; set; } = string.Empty;  // Make Model Year
    public bool IsCompatible { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
