using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <inheritdoc cref="IStockConsumptionService"/>
public sealed class StockConsumptionService(
    IStockLevelRepository stockLevelRepository,
    AutoPartDbContext dbContext,
    ILogger<StockConsumptionService> logger) : IStockConsumptionService
{
    public async Task ConsumeStockAsync(
        Guid partId,
        Guid? variantId,
        int quantityInBaseUnit,
        Guid salesOrderId,
        string reason,
        string referenceNumber,
        string sourceType,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var stockLevels = await stockLevelRepository.GetByPartAndVariantAsync(partId, variantId, cancellationToken);
        var remainingQty = quantityInBaseUnit;

        foreach (var stockLevel in stockLevels.OrderByDescending(sl => sl.QuantityAvailableInBaseUnit))
        {
            if (remainingQty <= 0) break;

            var qtyToDecrease = Math.Min(remainingQty, stockLevel.QuantityAvailableInBaseUnit);
            if (qtyToDecrease <= 0) continue;

            stockLevel.RemoveStock(qtyToDecrease, qtyToDecrease, reason);
            stockLevel.ModifiedBy = actor;
            await stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

            var stockMovement = StockMovement.Create(stockLevel.Id, "OUT", qtyToDecrease, reason, referenceNumber,
                unitId: stockLevel.UnitId, quantityInBaseUnit: qtyToDecrease);
            stockMovement.Approve(actor);
            stockMovement.CreatedBy = actor;
            stockMovement.ModifiedBy = actor;
            await dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

            // Lot-level (FEFO) tracking is audit detail on top of the StockLevel decrement above,
            // which is the part that must succeed — a failure here is logged, not fatal.
            try
            {
                var stockLots = await dbContext.StockLots
                    .Where(l => l.PartId == partId && l.VariantId == variantId && l.WarehouseId == stockLevel.WarehouseId
                        && l.QuantityAvailableInBaseUnit > 0 && !l.Isdeleted)
                    .OrderBy(l => l.ExpiryDate == null ? DateTime.MaxValue : l.ExpiryDate)
                    .ThenBy(l => l.ReceivingDate)
                    .ToListAsync(cancellationToken);

                var lotRemaining = qtyToDecrease;
                foreach (var lot in stockLots)
                {
                    if (lotRemaining <= 0) break;

                    var qtyFromLot = Math.Min(lot.QuantityAvailableInBaseUnit, lotRemaining);
                    lot.RemoveStock(qtyFromLot, qtyFromLot, reason);
                    lot.ModifiedBy = actor;

                    var lotMovement = StockLotMovement.Create(lot.Id, qtyFromLot, "SALE",
                        salesOrderId, sourceType, DateTime.UtcNow, lot.CostPrice, reason);
                    lotMovement.CreatedBy = actor;
                    lotMovement.ModifiedBy = actor;
                    await dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);

                    lotRemaining -= qtyFromLot;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Could not record stock lot movements for {SourceType} {ReferenceNumber}, part {PartId}",
                    sourceType, referenceNumber, partId);
            }

            remainingQty -= qtyToDecrease;
        }

        if (remainingQty > 0)
            throw new InvalidOperationException(
                $"Insufficient stock for part {partId}: requested {quantityInBaseUnit}, only {quantityInBaseUnit - remainingQty} available.");
    }
}
