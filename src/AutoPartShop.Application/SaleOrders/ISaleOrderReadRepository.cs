using AutoPartShop.Application.SaleOrders.Dtos;

namespace AutoPartShop.Application.SaleOrders
{
    public interface ISaleOrderReadRepository
    {
       public Task<(IReadOnlyCollection<SaleOrderResponse> response, int totalCount)> FindAllQuery(SaleOrderQuery query, CancellationToken cancellationToken = default);
    }
}
