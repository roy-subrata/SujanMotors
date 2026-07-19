using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Domain.Repositories;

/// <summary>
/// Repository for standalone warehouse bin/shelf locations (Zone-Aisle-Rack-Bin),
/// independent of any specific product. See <see cref="WarehouseLocation"/>.
/// </summary>
public interface IWarehouseLocationRepository : IBaseRepository<WarehouseLocation>
{
    Task<IEnumerable<WarehouseLocation>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);

    Task<(IEnumerable<WarehouseLocation> Locations, int TotalCount)> SearchPagedAsync(
        WarehouseLocationQuery query, CancellationToken cancellationToken = default);

    Task<bool> LocationExistsAsync(
        Guid warehouseId,
        string zone,
        string aisle,
        string rack,
        string bin,
        Guid? excludeLocationId = null,
        CancellationToken cancellationToken = default);
}

public class WarehouseLocationQuery
{
    public Guid? WarehouseId { get; set; }
    public Guid? CategoryId { get; set; }

    /// <summary>Matches against Zone, Aisle, Rack, Bin, or the composed location code.</summary>
    public string? Search { get; set; }

    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
