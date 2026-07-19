namespace AutoPartShop.Application.DTOs.ProductLocationDtos;

public record ProductLocationResponse
{
    public Guid Id { get; init; }
    public Guid PartId { get; init; }
    public string PartName { get; init; } = string.Empty;
    public string PartSKU { get; init; } = string.Empty;
    public Guid WarehouseLocationId { get; init; }
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string Zone { get; init; } = string.Empty;
    public string Aisle { get; init; } = string.Empty;
    public string Rack { get; init; } = string.Empty;
    public string Bin { get; init; } = string.Empty;

    /// <summary>Computed "Zone-Aisle-Rack-Bin", e.g. "A-04-B-12" — matches the printed bin label.</summary>
    public string LocationCode { get; init; } = string.Empty;

    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateProductLocationRequest
{
    public Guid PartId { get; init; }
    public Guid WarehouseLocationId { get; init; }
    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
}

public record UpdateProductLocationRequest
{
    public Guid WarehouseLocationId { get; init; }
    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
}

public record SetPrimaryLocationRequest
{
    public Guid LocationId { get; init; }
}
