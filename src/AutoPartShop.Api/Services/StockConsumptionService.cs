using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <inheritdoc cref="IStockConsumptionService"/>
public sealed class StockConsumptionService(
    IStockLevelRepository stockLevelRepository,
    AutoPartDbContext dbContext) : IStockConsumptionService
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

            // Only AVAILABLE + active lots are sellable — DAMAGED/QUARANTINE/deactivated lots (e.g.
            // from a reversed GRN) must never be consumed by a sale. FEFO: lots with no expiry are
            // never sold before lots that are about to expire.
            var stockLots = await dbContext.StockLots
                .Where(l => l.PartId == partId && l.VariantId == variantId && l.WarehouseId == stockLevel.WarehouseId
                    && l.Status == StockLotStatus.AVAILABLE
                    && l.IsActive
                    && l.QuantityAvailableInBaseUnit > 0 && !l.Isdeleted)
                .OrderBy(l => l.ExpiryDate == null ? DateTime.MaxValue : l.ExpiryDate)
                .ThenBy(l => l.ReceivingDate)
                .ToListAsync(cancellationToken);

            // Lot coverage is verified BEFORE mutating the level so a lot shortfall fails the sale the
            // same way the sales-confirm path does — never silently leave a decremented level behind.
            var lotTotal = stockLots.Sum(l => l.QuantityAvailableInBaseUnit);
            if (lotTotal < qtyToDecrease)
            {
                throw new InvalidOperationException(
                    $"Insufficient lot stock for part {partId}: stock level shows sufficient quantity but individual lot records are short. Run a stock reconciliation.");
            }

            stockLevel.RemoveStock(qtyToDecrease, qtyToDecrease, reason);
            stockLevel.ModifiedBy = actor;
            await stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

            var stockMovement = StockMovement.Create(stockLevel.Id, "OUT", qtyToDecrease, reason, referenceNumber,
                unitId: stockLevel.UnitId, quantityInBaseUnit: qtyToDecrease);
            stockMovement.Approve(actor);
            stockMovement.CreatedBy = actor;
            stockMovement.ModifiedBy = actor;
            await dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

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

            remainingQty -= qtyToDecrease;
        }

        if (remainingQty > 0)
            throw new InvalidOperationException(
                $"Insufficient stock for part {partId}: requested {quantityInBaseUnit}, only {quantityInBaseUnit - remainingQty} available.");
    }
}
