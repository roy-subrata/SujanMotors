using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Applies a signed stock adjustment (in BASE UNITS) to a StockLevel AND keeps StockLots in sync,
/// so lot quantities don't drift from level quantities (lot data drives product costing).
/// Shared by the manual adjust endpoint and the stock-take reconciliation flow.
/// Does NOT call SaveChanges — the caller owns the transaction boundary.
/// </summary>
public class StockAdjustmentApplier
{
    private readonly AutoPartDbContext _dbContext;

    public StockAdjustmentApplier(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public sealed record AdjustmentOutcome(StockMovement Movement, int LotsTouched, bool LotSyncSkipped);

    /// <summary>
    /// Adjusts the given (tracked) stock level by <paramref name="deltaBaseUnits"/> and mirrors the
    /// change into stock lots: decreases consume AVAILABLE lots FIFO (oldest first, matching how
    /// sales consume cost); increases go into the newest active lot (capacity raised so the add
    /// isn't capped). Increases with no existing lot skip lot sync — a lot cannot be fabricated
    /// without a supplier/goods-receipt — and report <c>LotSyncSkipped</c>.
    /// Throws InvalidOperationException when a decrease exceeds sellable stock.
    /// </summary>
    public async Task<AdjustmentOutcome> ApplyAsync(
        StockLevel stockLevel,
        int deltaQuantity,
        int deltaBaseUnits,
        string reason,
        string referenceNumber,
        string notes,
        string username,
        Guid? unitId = null,
        Guid? referenceId = null,
        string referenceType = "StockAdjustment",
        CancellationToken cancellationToken = default)
    {
        if (deltaQuantity == 0 || deltaBaseUnits == 0)
            throw new ArgumentException("Adjustment delta cannot be zero", nameof(deltaQuantity));
        if (Math.Sign(deltaQuantity) != Math.Sign(deltaBaseUnits))
            throw new ArgumentException("Display and base-unit deltas must have the same sign", nameof(deltaBaseUnits));

        var movement = StockMovement.Create(
            stockLevel.Id,
            "ADJUST",
            deltaQuantity,
            reason,
            referenceNumber,
            unitId: unitId ?? stockLevel.UnitId,
            quantityInBaseUnit: deltaBaseUnits);
        if (!string.IsNullOrWhiteSpace(notes))
            movement.AddNotes(notes);
        movement.Approve(username);
        movement.CreatedBy = username;
        movement.ModifiedBy = username;
        await _dbContext.StockMovements.AddAsync(movement, cancellationToken);

        if (deltaQuantity > 0)
            stockLevel.AddStock(deltaQuantity, deltaBaseUnits, reason);
        else
            stockLevel.RemoveStock(-deltaQuantity, -deltaBaseUnits, reason);
        stockLevel.ModifiedBy = username;

        var (lotsTouched, skipped) = await SyncLotsAsync(
            stockLevel, deltaBaseUnits, reason, referenceNumber, username, referenceId, referenceType, cancellationToken);

        return new AdjustmentOutcome(movement, lotsTouched, skipped);
    }

    private async Task<(int lotsTouched, bool skipped)> SyncLotsAsync(
        StockLevel stockLevel,
        int deltaBaseUnits,
        string reason,
        string referenceNumber,
        string username,
        Guid? referenceId,
        string referenceType,
        CancellationToken cancellationToken)
    {
        if (deltaBaseUnits < 0)
        {
            // Shrinkage / count-down: consume lots FIFO like a sale so costing stays accurate.
            var lots = await _dbContext.StockLots
                .Where(l => l.PartId == stockLevel.PartId
                    && l.VariantId == stockLevel.VariantId
                    && l.WarehouseId == stockLevel.WarehouseId
                    && l.Status == "AVAILABLE"
                    && l.IsActive && !l.Isdeleted
                    && l.QuantityAvailableInBaseUnit > 0)
                .OrderBy(l => l.ReceivingDate)
                .ToListAsync(cancellationToken);

            var remaining = -deltaBaseUnits;
            var touched = 0;
            foreach (var lot in lots)
            {
                if (remaining <= 0) break;
                var deduct = Math.Min(lot.QuantityAvailableInBaseUnit, remaining);
                lot.RemoveStock(deduct, deduct, reason);
                lot.ModifiedBy = username;
                await AddLotMovementAsync(lot, deduct, reason, referenceNumber, username, referenceId, referenceType, cancellationToken);
                remaining -= deduct;
                touched++;
            }
            // remaining > 0 means level had more stock than the lots record (pre-existing drift);
            // the level was still adjusted correctly, so don't fail — just report partial sync.
            return (touched, remaining > 0);
        }

        // Count-up / found stock: put it back into the newest lot so it sells at the latest cost.
        var newestLot = await _dbContext.StockLots
            .Where(l => l.PartId == stockLevel.PartId
                && l.VariantId == stockLevel.VariantId
                && l.WarehouseId == stockLevel.WarehouseId
                && l.Status == "AVAILABLE"
                && l.IsActive && !l.Isdeleted)
            .OrderByDescending(l => l.ReceivingDate)
            .FirstOrDefaultAsync(cancellationToken);

        if (newestLot == null)
            return (0, true); // no lot to attach found stock to — level adjusted, lot sync skipped

        newestLot.IncreaseCapacity(deltaBaseUnits, deltaBaseUnits);
        newestLot.AddStock(deltaBaseUnits, deltaBaseUnits, reason);
        newestLot.ModifiedBy = username;
        await AddLotMovementAsync(newestLot, deltaBaseUnits, reason, referenceNumber, username, referenceId, referenceType, cancellationToken);
        return (1, false);
    }

    private async Task AddLotMovementAsync(
        StockLot lot, int quantityBase, string reason, string referenceNumber, string username,
        Guid? referenceId, string referenceType, CancellationToken cancellationToken)
    {
        var lotMovement = StockLotMovement.Create(
            lot.Id,
            quantityBase,
            "ADJUSTMENT",
            referenceId,
            referenceType,
            DateTime.UtcNow,
            lot.CostPrice,
            reason,
            referenceNumber,
            unitId: lot.UnitId,
            quantityInBaseUnit: quantityBase,
            costAtMovementInBaseUnit: lot.CostPriceInBaseUnit);
        lotMovement.CreatedBy = username;
        lotMovement.ModifiedBy = username;
        await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);
    }
}
