namespace AutoPartShop.Application.DTOs.VehicleDtos;

public class CreateVehicleRequest
{
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public string EngineType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
