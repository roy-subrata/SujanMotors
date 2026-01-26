using AutoPartShop.Application.DTOs.CustomerDtos;
using AutoPartShop.Application.DTOs.PurchaseOrderDtos;

namespace AutoPartShop.Application.PurchaseOrders;

public interface IPurchaseOrderReadRepository
{
    Task<PurchaseOrderDto?> GetPurchaseOrderByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<PurchaseOrderResponse>> GetPurchaseOrderAsync(PurcahseQueryDto purcahseQuery,CancellationToken cancellationToken=default);
}
