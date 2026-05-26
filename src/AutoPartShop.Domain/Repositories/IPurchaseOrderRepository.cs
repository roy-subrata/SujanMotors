using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IPurchaseOrderRepository : IBaseRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByNumberAsync(string poNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseOrder>> GetBySuppliersAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseOrder>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseOrder>> GetOverdueAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseOrder> orders, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPurchaseAmountBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
}
