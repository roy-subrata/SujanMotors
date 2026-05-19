using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;


public interface IPurchaseReturnRepository : IBaseRepository<PurchaseReturn>
{
    Task<PurchaseReturn?> GetByNumberAsync(string returnNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReturn>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReturn>> GetBySupplierAsync(Guid supplierId, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReturn>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<PurchaseReturn>> GetPendingApprovalsAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseReturn> returns, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<(IEnumerable<PurchaseReturn> returns, int totalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
