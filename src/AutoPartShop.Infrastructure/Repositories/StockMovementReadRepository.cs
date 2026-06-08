using AutoPartShop.Application.DTOs.StockDtos;
using AutoPartShop.Application.Stock;
using AutoPartShop.Application.Stock.Dtos;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

public class StockMovementReadRepository : IStockMovementReadRepository
{
    private readonly AutoPartDbContext _dbContext;

    public StockMovementReadRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyCollection<StockMovementResponse> response, int totalCount)> FindAllQuery(
        StockMovementQuery query,
        CancellationToken cancellationToken = default)
    {
        var movements = _dbContext.StockMovements
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Part)
                    .ThenInclude(p => p!.BaseUnit)
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Variant)
            .Include(x => x.StockLevel)
                .ThenInclude(sl => sl!.Warehouse)
            .Include(x => x.Unit)
            .Where(x => !x.Isdeleted);

        if (query.PartId.HasValue && query.PartId.Value != Guid.Empty)
        {
            movements = movements.Where(x => x.StockLevel != null && x.StockLevel.PartId == query.PartId.Value);
        }

        if (query.WarehouseId.HasValue && query.WarehouseId.Value != Guid.Empty)
        {
            movements = movements.Where(x => x.StockLevel != null && x.StockLevel.WarehouseId == query.WarehouseId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Type))
        {
            var type = query.Type.Trim().ToUpperInvariant();
            movements = movements.Where(x => x.MovementType == type);
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            var status = query.Status.Trim().ToUpperInvariant();
            movements = status switch
            {
                "PENDING" => movements.Where(x => string.IsNullOrEmpty(x.ApprovedBy)),
                "APPROVED" => movements.Where(x => !string.IsNullOrEmpty(x.ApprovedBy)),
                _ => movements
            };
        }

        if (query.FromDate.HasValue)
        {
            var from = query.FromDate.Value.Date;
            movements = movements.Where(x => x.MovementDate >= from);
        }

        if (query.ToDate.HasValue)
        {
            var to = query.ToDate.Value.Date.AddDays(1); // inclusive of the entire end day
            movements = movements.Where(x => x.MovementDate < to);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            movements = movements.Where(x =>
                (x.StockLevel != null && x.StockLevel.Part != null &&
                 (EF.Functions.Like(x.StockLevel.Part.Name.ToLower(), $"%{term}%") ||
                  EF.Functions.Like(x.StockLevel.Part.SKU.ToLower(), $"%{term}%") ||
                  (x.StockLevel.Part.PartNumber != null && EF.Functions.Like(x.StockLevel.Part.PartNumber.Value.ToLower(), $"%{term}%")))) ||
                (x.StockLevel != null && x.StockLevel.Variant != null &&
                 (EF.Functions.Like(x.StockLevel.Variant.Name.ToLower(), $"%{term}%") ||
                  (x.StockLevel.Variant.SKU != null && EF.Functions.Like(x.StockLevel.Variant.SKU.ToLower(), $"%{term}%")))) ||
                (x.StockLevel != null && x.StockLevel.Warehouse != null &&
                 (EF.Functions.Like(x.StockLevel.Warehouse.Name.ToLower(), $"%{term}%") ||
                  EF.Functions.Like(x.StockLevel.Warehouse.Code.ToLower(), $"%{term}%"))) ||
                EF.Functions.Like(x.ReferenceNumber.ToLower(), $"%{term}%") ||
                EF.Functions.Like(x.Reason.ToLower(), $"%{term}%"));
        }

        // Default sorting: newest first
        movements = movements.OrderByDescending(x => x.MovementDate);

        var totalCount = await movements.CountAsync(cancellationToken);
        var items = await movements
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(movement => new StockMovementResponse
            {
                Id = movement.Id,
                PartId = movement.StockLevel != null ? movement.StockLevel.PartId : Guid.Empty,
                PartName = movement.StockLevel != null && movement.StockLevel.Part != null ? movement.StockLevel.Part.Name : string.Empty,
                PartCode = movement.StockLevel != null && movement.StockLevel.Part != null
                    ? (movement.StockLevel.Part.PartNumber != null ? movement.StockLevel.Part.PartNumber.Value : movement.StockLevel.Part.SKU)
                    : string.Empty,
                VariantId = movement.StockLevel != null ? movement.StockLevel.VariantId : null,
                VariantName = movement.StockLevel != null && movement.StockLevel.Variant != null ? movement.StockLevel.Variant.Name : null,
                VariantSku = movement.StockLevel != null && movement.StockLevel.Variant != null ? movement.StockLevel.Variant.SKU : null,
                DisplayName = movement.StockLevel != null && movement.StockLevel.Variant != null
                    ? (movement.StockLevel.Part != null
                        ? (movement.StockLevel.Variant.Name.StartsWith(movement.StockLevel.Part.Name)
                            ? movement.StockLevel.Variant.Name
                            : movement.StockLevel.Part.Name + " - " + movement.StockLevel.Variant.Name)
                        : movement.StockLevel.Variant.Name)
                    : (movement.StockLevel != null && movement.StockLevel.Part != null ? movement.StockLevel.Part.Name : string.Empty),
                WarehouseId = movement.StockLevel != null ? movement.StockLevel.WarehouseId : Guid.Empty,
                WarehouseName = movement.StockLevel != null && movement.StockLevel.Warehouse != null ? movement.StockLevel.Warehouse.Name : string.Empty,
                WarehouseCode = movement.StockLevel != null && movement.StockLevel.Warehouse != null ? movement.StockLevel.Warehouse.Code : string.Empty,
                Type = movement.MovementType,
                Quantity = movement.Quantity,
                QuantityInBaseUnit = movement.QuantityInBaseUnit,
                UnitId = movement.UnitId,
                UnitName = movement.Unit != null ? movement.Unit.Name : null,
                UnitSymbol = movement.Unit != null ? movement.Unit.Symbol : null,
                BaseUnitSymbol = movement.StockLevel != null && movement.StockLevel.Part != null && movement.StockLevel.Part.BaseUnit != null
                    ? movement.StockLevel.Part.BaseUnit.Symbol : null,
                Reason = movement.Reason,
                Reference = movement.ReferenceNumber,
                Status = string.IsNullOrEmpty(movement.ApprovedBy) ? "PENDING" : "APPROVED",
                Notes = movement.Notes,
                ApprovedBy = movement.ApprovedBy,
                ApprovedAt = null,
                CreatedAt = movement.MovementDate
            })
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
