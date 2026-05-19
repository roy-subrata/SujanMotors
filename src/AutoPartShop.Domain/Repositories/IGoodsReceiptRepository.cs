using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;



public interface IGoodsReceiptRepository : IBaseRepository<GoodsReceipt>
{
    Task<GoodsReceipt?> GetByNumberAsync(string grnNumber, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoodsReceipt>> GetByStatusAsync(string status, CancellationToken cancellationToken = default);
    Task<IEnumerable<GoodsReceipt>> GetPendingVerificationAsync(CancellationToken cancellationToken = default);
    Task<(IEnumerable<GoodsReceipt> receipts, int totalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
