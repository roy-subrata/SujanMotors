using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Application.Stock.Dtos;

namespace AutoPartShop.Application.Stock;

public interface IStockLotReadRepository
{
    Task<(IReadOnlyCollection<StockLotResponse> response, int totalCount)> FindAllQuery(
        StockLotQuery query,
        CancellationToken cancellationToken = default);
}
