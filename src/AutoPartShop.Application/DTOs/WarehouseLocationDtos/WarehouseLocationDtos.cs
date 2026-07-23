namespace AutoPartShop.Application.DTOs.WarehouseLocationDtos;

public record WarehouseLocationResponse
{
    public Guid Id { get; init; }
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;
    public string Aisle { get; init; } = string.Empty;
    public string Rack { get; init; } = string.Empty;
    public string Bin { get; init; } = string.Empty;

    /// <summary>Computed "Zone-Aisle-Rack-Bin", e.g. "A-04-B-12".</summary>
    public string LocationCode { get; init; } = string.Empty;

    public Guid? CategoryId { get; init; }
    public string? CategoryName { get; init; }
    public string? Notes { get; init; }
    public bool IsActive { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateWarehouseLocationRequest
{
    public Guid WarehouseId { get; init; }
    public string Zone { get; init; } = string.Empty;
    public string Aisle { get; init; } = string.Empty;
    public string Rack { get; init; } = string.Empty;
    public string Bin { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? Notes { get; init; }
}

public record UpdateWarehouseLocationRequest
{
    public Guid WarehouseId { get; init; }
    public string Zone { get; init; } = string.Empty;
    public string Aisle { get; init; } = string.Empty;
    public string Rack { get; init; } = string.Empty;
    public string Bin { get; init; } = string.Empty;
    public Guid? CategoryId { get; init; }
    public string? Notes { get; init; }
}
