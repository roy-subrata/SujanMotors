namespace AutoPartShop.Application.DTOs.WarehouseDtos;

public class UpdateWarehouseRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string ManagerEmail { get; set; } = string.Empty;
    public string ManagerPhone { get; set; } = string.Empty;
    public decimal StorageCapacity { get; set; } = 0;
    public string CapacityUnit { get; set; } = "SQM";
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
