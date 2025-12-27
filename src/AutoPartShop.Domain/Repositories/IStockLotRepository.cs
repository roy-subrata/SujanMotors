using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IStockLotRepository : IBaseRepository<StockLot>
{
    Task<IEnumerable<StockLot>> GetByPartAsync(Guid partId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetByWarehouseAsync(Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetByPartAndWarehouseAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<StockLot?> GetByLotNumberAsync(string lotNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetAvailableLotsAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetExpiredLotsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetLowStockLotsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<StockLot>> GetByGoodsReceiptLineAsync(Guid goodsReceiptLineId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<StockLot> items, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
