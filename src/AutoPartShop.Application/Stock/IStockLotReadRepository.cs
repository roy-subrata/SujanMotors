using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Application.Stock.Dtos;

namespace AutoPartShop.Application.Stock;

public interface IStockLotReadRepository
{
    Task<(IReadOnlyCollection<StockLotResponse> response, int totalCount)> FindAllQuery(
        StockLotQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Aggregates cost/quantity totals across all lots matching the query's filters
    /// (ignoring paging) — computed in SQL so the frontend never sums a partial page.
    /// </summary>
    Task<StockLotFilterSummaryResponse> GetSummaryAsync(
        StockLotQuery query,
        CancellationToken cancellationToken = default);
}
