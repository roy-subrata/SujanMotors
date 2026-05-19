using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IStockLevelRepository : IBaseRepository<StockLevel>
{
    Task<StockLevel?> GetByPartAndWarehouseAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLevel>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLevel>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLevel>> GetLowStockAsync(CancellationToken cancellationToken = default);
}
