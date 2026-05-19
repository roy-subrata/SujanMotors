
namespace AutoPartShop.Application.Warehouse;

public class WarehouseResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Manager { get; set; } = string.Empty;
    public string ManagerEmail { get; set; } = string.Empty;
    public string ManagerPhone { get; set; } = string.Empty;
    public decimal StorageCapacity { get; set; }
    public string CapacityUnit { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string ModifiedBy { get; set; } = string.Empty;
}
