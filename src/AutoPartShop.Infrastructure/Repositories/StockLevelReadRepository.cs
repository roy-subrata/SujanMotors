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
                .ThenInclude(p => p.BaseUnit)
            .Include(x => x.Variant)
            .Include(x => x.Warehouse)
            .Include(x => x.Unit)
            .Where(x => !x.Isdeleted);

        if (!string.IsNullOrWhiteSpace(query.PartId) && Guid.TryParse(query.PartId, out var partId) && partId != Guid.Empty)
        {
            levels = levels.Where(x => x.PartId == partId);
        }

        if (!string.IsNullOrWhiteSpace(query.VariantId) && Guid.TryParse(query.VariantId, out var variantId) && variantId != Guid.Empty)
        {
            levels = levels.Where(x => x.VariantId == variantId);
        }

        if (!string.IsNullOrWhiteSpace(query.WarehouseId) && Guid.TryParse(query.WarehouseId, out var warehouseId) && warehouseId != Guid.Empty)
        {
            levels = levels.Where(x => x.WarehouseId == warehouseId);
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
                (x.Variant != null && EF.Functions.Like(x.Variant.Name.ToLower(), $"%{term}%")) ||
                (x.Variant != null && x.Variant.SKU != null && EF.Functions.Like(x.Variant.SKU.ToLower(), $"%{term}%")) ||
                (x.Warehouse != null && EF.Functions.Like(x.Warehouse.Name.ToLower(), $"%{term}%")) ||
                (x.Warehouse != null && EF.Functions.Like(x.Warehouse.Code.ToLower(), $"%{term}%")));
        }

        levels = levels.OrderByDescending(x => x.CreatedDate);

        var totalCount = await levels.CountAsync(cancellationToken);
        var items = await levels
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(level => new StockLevelResponse
            {
                Id = level.Id,
                PartId = level.PartId,
                PartName = level.Part != null ? level.Part.Name : null,
                PartSku = level.Part != null ? level.Part.SKU : null,
                VariantId = level.VariantId,
                VariantName = level.Variant != null ? level.Variant.Name : null,
                VariantSku = level.Variant != null ? level.Variant.SKU : null,
                // Composed display name: "Base - Variant" (mirrors PurchaseOrderReadRepository).
                DisplayName = level.Variant != null
                    ? (level.Part != null
                        ? (level.Variant.Name.StartsWith(level.Part.Name)
                            ? level.Variant.Name
                            : level.Part.Name + " - " + level.Variant.Name)
                        : level.Variant.Name)
                    : (level.Part != null ? level.Part.Name : null),
                WarehouseId = level.WarehouseId,
                WarehouseName = level.Warehouse != null ? level.Warehouse.Name : null,
                UnitId = level.UnitId,
                UnitName = level.Unit != null ? level.Unit.Name : null,
                UnitSymbol = level.Unit != null ? level.Unit.Symbol : null,
                BaseUnitName = level.Part != null && level.Part.BaseUnit != null ? level.Part.BaseUnit.Name : null,
                BaseUnitSymbol = level.Part != null && level.Part.BaseUnit != null ? level.Part.BaseUnit.Symbol : null,
                Quantity = level.QuantityOnHand,
                QuantityInBaseUnit = level.QuantityOnHandInBaseUnit,
                ReservedQuantity = level.QuantityReserved,
                ReservedQuantityInBaseUnit = level.QuantityReservedInBaseUnit,
                AvailableQuantity = level.QuantityAvailable,
                AvailableQuantityInBaseUnit = level.QuantityAvailableInBaseUnit,
                DamagedQuantity = level.QuantityDamaged,
                QuarantineQuantity = level.QuantityQuarantine,
                ReorderLevel = level.ReorderLevel,
                ReorderQuantity = level.ReorderQuantity,
                NeedsReorder = level.NeedsReorder,
                CreatedAt = level.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
