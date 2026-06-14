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
            .Include(x => x.Part).ThenInclude(p => p != null ? p.BaseUnit : null)
            .Include(x => x.Variant)
            .Include(x => x.Unit)
            .Include(x => x.Warehouse)
            .Include(x => x.Supplier)
            .Where(x => !x.Isdeleted);

        if (!string.IsNullOrWhiteSpace(query.PartId) && Guid.TryParse(query.PartId, out var partId) && partId != Guid.Empty)
        {
            lots = lots.Where(x => x.PartId == partId);
        }

        if (!string.IsNullOrWhiteSpace(query.VariantId) && Guid.TryParse(query.VariantId, out var variantId) && variantId != Guid.Empty)
        {
            lots = lots.Where(x => x.VariantId == variantId);
        }

        if (!string.IsNullOrWhiteSpace(query.WarehouseId) && Guid.TryParse(query.WarehouseId, out var warehouseId) && warehouseId != Guid.Empty)
        {
            lots = lots.Where(x => x.WarehouseId == warehouseId);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            lots = lots.Where(x =>
                EF.Functions.Like(x.LotNumber.ToLower(), $"%{term}%") ||
                (x.Part != null && EF.Functions.Like(x.Part.Name.ToLower(), $"%{term}%")) ||
                (x.Part != null && EF.Functions.Like(x.Part.SKU.ToLower(), $"%{term}%")) ||
                (x.Variant != null && EF.Functions.Like(x.Variant.Name.ToLower(), $"%{term}%")) ||
                (x.Variant != null && x.Variant.SKU != null && EF.Functions.Like(x.Variant.SKU.ToLower(), $"%{term}%")) ||
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
                VariantId = lot.VariantId,
                VariantName = lot.Variant != null ? lot.Variant.Name : null,
                VariantSku = lot.Variant != null ? lot.Variant.SKU : null,
                DisplayName = lot.Variant != null
                    ? (lot.Part != null
                        ? (lot.Variant.Name.StartsWith(lot.Part.Name)
                            ? lot.Variant.Name
                            : lot.Part.Name + " - " + lot.Variant.Name)
                        : lot.Variant.Name)
                    : (lot.Part != null ? lot.Part.Name : string.Empty),
                WarehouseId = lot.WarehouseId,
                WarehouseName = lot.Warehouse != null ? lot.Warehouse.Name : string.Empty,
                SupplierId = lot.SupplierId,
                SupplierName = lot.Supplier != null ? lot.Supplier.Name : string.Empty,
                QuantityReceived = lot.QuantityReceived,
                QuantityReceivedInBaseUnit = lot.QuantityReceivedInBaseUnit,
                QuantityAvailable = lot.QuantityAvailable,
                QuantityAvailableInBaseUnit = lot.QuantityAvailableInBaseUnit,
                UnitId = lot.UnitId,
                UnitName = lot.Unit != null ? lot.Unit.Name : null,
                UnitCode = lot.Unit != null ? lot.Unit.Symbol : null,
                BaseUnitName = lot.Part != null && lot.Part.BaseUnit != null ? lot.Part.BaseUnit.Name : null,
                BaseUnitCode = lot.Part != null && lot.Part.BaseUnit != null ? lot.Part.BaseUnit.Symbol : null,
                CostPrice = lot.CostPrice,
                Currency = lot.Currency,
                TotalCost = lot.GetTotalCost(),
                AvailableCost = lot.GetAvailableCost(),
                HasWarranty = lot.HasWarranty,
                WarrantyPeriodMonths = lot.WarrantyPeriodMonths,
                WarrantyType = lot.WarrantyType,
                WarrantyTerms = lot.WarrantyTerms,
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
