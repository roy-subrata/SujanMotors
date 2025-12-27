using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

public interface IStockMovementRepository : IBaseRepository<StockMovement>
{
    Task<IEnumerable<StockMovement>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovement>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovement>> GetByTypeAsync(string movementType, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovement>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockMovement>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockMovement> movements, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
