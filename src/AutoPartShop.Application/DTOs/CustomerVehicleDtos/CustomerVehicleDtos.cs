namespace AutoPartShop.Application.DTOs.CustomerVehicleDtos;

public class CreateCustomerVehicleRequest
{
    public string RegistrationNo { get; set; } = string.Empty;
    public string VIN { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string EngineType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? Mileage { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? CatalogVehicleId { get; set; }
}

public class UpdateCustomerVehicleRequest
{
    public string RegistrationNo { get; set; } = string.Empty;
    public string VIN { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string EngineType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? Mileage { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? CatalogVehicleId { get; set; }
}

public class CustomerVehicleResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string VIN { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public string EngineType { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int? Mileage { get; set; }
    public string Notes { get; set; } = string.Empty;
    public Guid? CatalogVehicleId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
