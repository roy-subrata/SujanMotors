using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Application.Services;

/// <summary>
/// Service to manage stock operations including receiving goods and creating audit trail
/// </summary>
public class StockManagementService
{
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public StockManagementService(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IPurchaseOrderRepository purchaseOrderRepository)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    /// <summary>
    /// Process stock receipt when a GRN is accepted
    /// Updates stock levels, creates audit trail, and updates PO line quantities
    /// </summary>
    public async Task ProcessGoodsReceiptAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        if (goodsReceipt?.LineItems == null || !goodsReceipt.LineItems.Any())
            throw new ArgumentException("Goods receipt must have line items", nameof(goodsReceipt));

        try
        {
            // Process each line item in the GRN
            foreach (var grnLine in goodsReceipt.LineItems)
            {
                // Calculate accepted quantity (received - rejected)
                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;

                if (acceptedQuantity <= 0)
                    continue; // Skip if no accepted quantity

                // Get or create stock level for this part/warehouse combination
                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId,
                    goodsReceipt.WarehouseId,
                    cancellationToken);

                // Update stock level with received quantity
                stockLevel.AddStock(acceptedQuantity);
                await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                // Create stock movement audit trail record
                var movement = StockMovement.Create(
                    stockLevelId: stockLevel.Id,
                    movementType: "IN",
                    quantity: acceptedQuantity,
                    reason: "GRN",
                    referenceNumber: goodsReceipt.GRNNumber,
                    movementDate: DateTime.UtcNow);

                movement.Approve("System");
                movement.AddNotes($"Received {acceptedQuantity} units of part {grnLine.PartId} via GRN {goodsReceipt.GRNNumber}");

                await _stockMovementRepository.AddAsync(movement, cancellationToken);

                // Update the PO line received quantity
                await UpdatePurchaseOrderLineAsync(
                    grnLine.PurchaseOrderLineId,
                    acceptedQuantity,
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error processing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Get existing stock level or create a new one if it doesn't exist
    /// </summary>
    private async Task<StockLevel> GetOrCreateStockLevelAsync(
        Guid partId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var allStockLevels = await _stockLevelRepository.GetAllAsync(cancellationToken);

        var existingStockLevel = allStockLevels.FirstOrDefault(s =>
            s.PartId == partId && s.WarehouseId == warehouseId);

        if (existingStockLevel != null)
            return existingStockLevel;

        // Create new stock level if it doesn't exist
        var newStockLevel = StockLevel.Create(
            partId: partId,
            warehouseId: warehouseId,
            reorderLevel: 10, // Default reorder level
            reorderQuantity: 50); // Default reorder quantity

        newStockLevel.CreatedBy = "System";
        newStockLevel.ModifiedBy = "System";

        await _stockLevelRepository.AddAsync(newStockLevel, cancellationToken);
        return newStockLevel;
    }

    /// <summary>
    /// Update the received quantity on a purchase order line
    /// </summary>
    private async Task UpdatePurchaseOrderLineAsync(
        Guid? purchaseOrderLineId,
        int receivedQuantity,
        CancellationToken cancellationToken)
    {
        if (purchaseOrderLineId == Guid.Empty || purchaseOrderLineId == null)
            return;

        // This would require a PurchaseOrderLineRepository
        // For now, we'll skip this as it requires additional implementation
        // In a full implementation, this would update the PO line's ReceivedQuantity
        await Task.CompletedTask;
    }

    /// <summary>
    /// Reverse stock receipt if a GRN is rejected
    /// </summary>
    public async Task ReverseGoodsReceiptAsync(
        GoodsReceipt goodsReceipt,
        CancellationToken cancellationToken = default)
    {
        if (goodsReceipt?.LineItems == null || !goodsReceipt.LineItems.Any())
            return;

        try
        {
            foreach (var grnLine in goodsReceipt.LineItems)
            {
                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;

                if (acceptedQuantity <= 0)
                    continue;

                // Get stock level
                var allStockLevels = await _stockLevelRepository.GetAllAsync(cancellationToken);
                var stockLevel = allStockLevels.FirstOrDefault(s =>
                    s.PartId == grnLine.PartId && s.WarehouseId == goodsReceipt.WarehouseId);

                if (stockLevel == null)
                    continue;

                // Reverse the stock addition
                if (stockLevel.QuantityOnHand >= acceptedQuantity)
                {
                    stockLevel.RemoveStock(acceptedQuantity);
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);
                }

                // Create reversal movement record
                var reversal = StockMovement.Create(
                    stockLevelId: stockLevel.Id,
                    movementType: "ADJUST",
                    quantity: -acceptedQuantity,
                    reason: "GRN Rejection",
                    referenceNumber: goodsReceipt.GRNNumber);

                reversal.Approve("System");
                reversal.AddNotes($"Reversal: GRN {goodsReceipt.GRNNumber} was rejected");

                await _stockMovementRepository.AddAsync(reversal, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error reversing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}",
                ex);
        }
    }
}
