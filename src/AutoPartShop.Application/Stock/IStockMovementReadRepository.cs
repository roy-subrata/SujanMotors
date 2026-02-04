using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Stock.Dtos;

namespace AutoPartShop.Application.Stock;

public interface IStockMovementReadRepository
{
    Task<(IReadOnlyCollection<StockMovementResponse> response, int totalCount)> FindAllQuery(
        StockMovementQuery query,
        CancellationToken cancellationToken = default);
}
