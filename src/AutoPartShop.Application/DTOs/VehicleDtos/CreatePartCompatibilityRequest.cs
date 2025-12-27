namespace AutoPartShop.Application.DTOs.VehicleDtos;

public class CreatePartCompatibilityRequest
{
    public Guid PartId { get; set; }
    public Guid VehicleId { get; set; }
    public bool IsCompatible { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}
