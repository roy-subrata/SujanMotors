using AutoPartShop.Application.DTOs.InventoryDtos;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockLotReadRepository : IStockLotReadRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockLotReadRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyCollection<StockLotResponse> response, int totalCount)> FindAllQuery(
        StockLotQuery query,
        CancellationToken cancellationToken = default)
    {
        var lots = _dbContext.StockLots
            .Include(x => x.Part)
            .Include(x => x.Warehouse)
            .Include(x => x.Supplier)
            .Where(x => !x.Isdeleted);

        if (query.PartId.HasValue && query.PartId.Value != Guid.Empty)
        {
            lots = lots.Where(x => x.PartId == query.PartId.Value);
        }

        if (query.WarehouseId.HasValue && query.WarehouseId.Value != Guid.Empty)
        {
            lots = lots.Where(x => x.WarehouseId == query.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            lots = lots.Where(x =>
                EF.Functions.Like(x.LotNumber.ToLower(), $"%{term}%") ||
                (x.Part != null && EF.Functions.Like(x.Part.Name.ToLower(), $"%{term}%")) ||
                (x.Part != null && EF.Functions.Like(x.Part.SKU.ToLower(), $"%{term}%")) ||
                (x.Warehouse != null && EF.Functions.Like(x.Warehouse.Name.ToLower(), $"%{term}%")) ||
                (x.Supplier != null && EF.Functions.Like(x.Supplier.Name.ToLower(), $"%{term}%")));
        }

        lots = lots.OrderByDescending(x => x.ReceivingDate);

        var totalCount = await lots.CountAsync(cancellationToken);
        var items = await lots
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(lot => new StockLotResponse
            {
                Id = lot.Id,
                LotNumber = lot.LotNumber,
                PartId = lot.PartId,
                PartName = lot.Part != null ? lot.Part.Name : string.Empty,
                PartSKU = lot.Part != null ? lot.Part.SKU : string.Empty,
                WarehouseId = lot.WarehouseId,
                WarehouseName = lot.Warehouse != null ? lot.Warehouse.Name : string.Empty,
                SupplierId = lot.SupplierId,
                SupplierName = lot.Supplier != null ? lot.Supplier.Name : string.Empty,
                QuantityReceived = lot.QuantityReceived,
                QuantityAvailable = lot.QuantityAvailable,
                CostPrice = lot.CostPrice,
                Currency = lot.Currency,
                TotalCost = lot.GetTotalCost(),
                AvailableCost = lot.GetAvailableCost(),
                ReceivingDate = lot.ReceivingDate,
                ExpiryDate = lot.ExpiryDate,
                IsExpired = lot.IsExpired,
                ManufacturerLotNumber = lot.ManufacturerLotNumber,
                Notes = lot.Notes,
                IsActive = lot.IsActive,
                CreatedAt = lot.CreatedDate
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
