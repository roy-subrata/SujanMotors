using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IStockLotMovementRepository : IBaseRepository<StockLotMovement>
{
    Task<IEnumerable<StockLotMovement>> GetByStockLotAsync(Guid stockLotId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLotMovement>> GetByMovementTypeAsync(string movementType, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLotMovement>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLotMovement>> GetByReferenceAsync(Guid referenceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLotMovement>> GetSalesMovementsAsync(Guid stockLotId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLotMovement>> GetByStockLotAndDateRangeAsync(Guid stockLotId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockLotMovement> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
