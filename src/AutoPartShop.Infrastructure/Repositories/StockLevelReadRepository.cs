using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockLevelReadRepository : IStockLevelReadRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockLevelReadRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyCollection<StockLevelResponse> response, int totalCount)> FindAllQuery(
        StockLevelQuery query,
        CancellationToken cancellationToken = default)
    {
        var levels = _dbContext.StockLevels
            .Include(x => x.Part)
            .Include(x => x.Warehouse)
            .Where(x => !x.Isdeleted);

        if (query.PartId.HasValue && query.PartId.Value != Guid.Empty)
        {
            levels = levels.Where(x => x.PartId == query.PartId.Value);
        }

        if (query.WarehouseId.HasValue && query.WarehouseId.Value != Guid.Empty)
        {
            levels = levels.Where(x => x.WarehouseId == query.WarehouseId.Value);
        }

        if (query.LowStockOnly)
        {
            levels = levels.Where(x => (x.QuantityOnHand - x.QuantityReserved) <= x.ReorderLevel);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToLowerInvariant();
            levels = status switch
            {
                "out-of-stock" => levels.Where(x => (x.QuantityOnHand - x.QuantityReserved) <= 0),
                "critical" => levels.Where(x => (x.QuantityOnHand - x.QuantityReserved) <= x.ReorderLevel * 0.5),
                "low" => levels.Where(x =>
                    (x.QuantityOnHand - x.QuantityReserved) <= x.ReorderLevel &&
                    (x.QuantityOnHand - x.QuantityReserved) > x.ReorderLevel * 0.5),
                "in-stock" => levels.Where(x => (x.QuantityOnHand - x.QuantityReserved) > x.ReorderLevel),
                _ => levels
            };
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            levels = levels.Where(x =>
                (x.Part != null && EF.Functions.Like(x.Part.Name.ToLower(), $"%{term}%")) ||
                (x.Part != null && EF.Functions.Like(x.Part.SKU.ToLower(), $"%{term}%")) ||
                (x.Warehouse != null && EF.Functions.Like(x.Warehouse.Name.ToLower(), $"%{term}%")) ||
                (x.Warehouse != null && EF.Functions.Like(x.Warehouse.Code.ToLower(), $"%{term}%")));
        }

        // Default sorting: Part Name, then Warehouse Name
        levels = levels
            .OrderBy(x => x.Part != null ? x.Part.Name : string.Empty)
            .ThenBy(x => x.Warehouse != null ? x.Warehouse.Name : string.Empty);

        var totalCount = await levels.CountAsync(cancellationToken);
        var items = await levels
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(level => new StockLevelResponse
            {
                Id = level.Id,
                PartId = level.PartId,
                WarehouseId = level.WarehouseId,
                Quantity = level.QuantityOnHand,
                ReservedQuantity = level.QuantityReserved,
                AvailableQuantity = level.QuantityAvailable,
                ReorderLevel = level.ReorderLevel,
                ReorderQuantity = level.ReorderQuantity,
                NeedsReorder = level.NeedsReorder,
                CreatedAt = level.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
