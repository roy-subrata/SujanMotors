

namespace AutoPartShop.Application.Warehouse;

public interface IWarehouseReadRepository
{
    Task<(IEnumerable<WarehouseResponse> Warehouses, int TotalCount)> FindAllAsync(WarehouseQueryDto query, CancellationToken cancellationToken = default);
    Task<WarehouseResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WarehouseResponse?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}