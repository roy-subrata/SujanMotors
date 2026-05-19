namespace AutoPartShop.Application.DTOs.ProductLocationDtos;

public record ProductLocationResponse
{
    public Guid Id { get; init; }
    public Guid PartId { get; init; }
    public string PartName { get; init; } = string.Empty;
    public string PartSKU { get; init; } = string.Empty;
    public Guid WarehouseId { get; init; }
    public string WarehouseName { get; init; } = string.Empty;
    public string WarehouseCode { get; init; } = string.Empty;
    public string Section { get; init; } = string.Empty;
    public string Shelf { get; init; } = string.Empty;
    public string FullLocation { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

public record CreateProductLocationRequest
{
    public Guid PartId { get; init; }
    public Guid WarehouseId { get; init; }
    public string Section { get; init; } = string.Empty;
    public string Shelf { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
}

public record UpdateProductLocationRequest
{
    public string Section { get; init; } = string.Empty;
    public string Shelf { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public bool IsPrimary { get; init; }
}

public record SetPrimaryLocationRequest
{
    public Guid LocationId { get; init; }
}
