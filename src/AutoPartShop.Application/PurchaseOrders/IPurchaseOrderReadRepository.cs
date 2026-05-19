using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.PurchaseOrders;

public interface IPurchaseOrderReadRepository
{
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<(IEnumerable<PurchaseOrderResponse> response, int total)> FindAllAsync(PurcahseQueryDto purcahseQuery, CancellationToken cancellationToken = default);
}
