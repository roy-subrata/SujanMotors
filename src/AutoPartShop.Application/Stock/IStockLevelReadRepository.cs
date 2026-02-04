using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Stock.Dtos;

namespace AutoPartShop.Application.Stock;

public interface IStockLevelReadRepository
{
    Task<(IReadOnlyCollection<StockLevelResponse> response, int totalCount)> FindAllQuery(
        StockLevelQuery query,
        CancellationToken cancellationToken = default);
}
