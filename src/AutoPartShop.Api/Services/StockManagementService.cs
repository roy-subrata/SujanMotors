using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for managing stock operations, including GRN processing and stock movements
/// </summary>
public class StockManagementService
{
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly IPartRepository _partRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ICodeGenerateService _codeGenerateService;

    public StockManagementService(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLotRepository stockLotRepository,
        IPartRepository partRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ICodeGenerateService codeGenerateService)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLotRepository = stockLotRepository;
        _partRepository = partRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _codeGenerateService = codeGenerateService;
    }

    /// <summary>
    /// Processes a goods receipt by updating stock levels, creating stock lots, and creating audit trail
    /// </summary>
    public async Task ProcessGoodsReceiptAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        if (goodsReceipt?.LineItems == null || !goodsReceipt.LineItems.Any())
            throw new ArgumentException("Goods receipt must have line items", nameof(goodsReceipt));

        try
        {
            // Get Purchase Order to retrieve supplier information
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(goodsReceipt.PurchaseOrderId, cancellationToken);
            if (purchaseOrder == null)
                throw new InvalidOperationException($"Purchase order not found for goods receipt {goodsReceipt.GRNNumber}");

            foreach (var grnLine in goodsReceipt.LineItems)
            {
                // Calculate accepted quantity (received - rejected)
                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;
                if (acceptedQuantity <= 0)
                    continue;

                // Get or create stock level for this part in this warehouse
                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId,
                    goodsReceipt.WarehouseId,
                    cancellationToken);

                // Update stock level
                stockLevel.AddStock(acceptedQuantity, "GRN");
                await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                // Create stock movement audit record
                var movement = StockMovement.Create(
                    stockLevelId: stockLevel.Id,
                    movementType: "IN",
                    quantity: acceptedQuantity,
                    reason: "GRN",
                    referenceNumber: goodsReceipt.GRNNumber,
                    movementDate: DateTime.UtcNow);

                // Auto-approve system movements
                movement.Approve("System");
                await _stockMovementRepository.AddAsync(movement, cancellationToken);

                // Create Stock Lot for batch/lot tracking and cost tracking
                var lotNumber = await _codeGenerateService.GenerateAsync("LOT", cancellationToken);
                var stockLot = StockLot.Create(
                    lotNumber: lotNumber,
                    partId: grnLine.PartId,
                    warehouseId: goodsReceipt.WarehouseId,
                    supplierId: purchaseOrder.SupplierId,
                    goodsReceiptLineId: grnLine.Id,
                    quantityReceived: grnLine.ReceivedQuantity,
                    costPrice: grnLine.UnitCost,
                    receivingDate: goodsReceipt.ReceiptDate,
                    manufacturerLotNumber: grnLine.SerialNumbers, // Using serial numbers field for mfr lot
                    expiryDate: null, // Can be set later if needed
                    currency: grnLine.Currency,
                    notes: $"Created from GRN {goodsReceipt.GRNNumber}"
                );

                stockLot.CreatedBy = "System";
                stockLot.ModifiedBy = "System";
                await _stockLotRepository.AddAsync(stockLot, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error processing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Reverses stock movements for a rejected goods receipt
    /// </summary>
    public async Task ReverseGoodsReceiptAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        if (goodsReceipt?.LineItems == null || !goodsReceipt.LineItems.Any())
            throw new ArgumentException("Goods receipt must have line items", nameof(goodsReceipt));

        try
        {
            foreach (var grnLine in goodsReceipt.LineItems)
            {
                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;
                if (acceptedQuantity <= 0)
                    continue;

                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId,
                    goodsReceipt.WarehouseId,
                    cancellationToken);

                // Remove the stock that was added
                if (acceptedQuantity <= stockLevel.QuantityOnHand)
                {
                    stockLevel.RemoveStock(acceptedQuantity, "GRN Reversal");
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    // Create reversal movement record
                    var movement = StockMovement.Create(
                        stockLevelId: stockLevel.Id,
                        movementType: "OUT",
                        quantity: acceptedQuantity,
                        reason: "GRN Reversal",
                        referenceNumber: goodsReceipt.GRNNumber,
                        movementDate: DateTime.UtcNow);

                    movement.Approve("System");
                    await _stockMovementRepository.AddAsync(movement, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error reversing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets existing stock level or creates a new one if it doesn't exist
    /// </summary>
    private async Task<StockLevel> GetOrCreateStockLevelAsync(Guid partId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        // Try to find existing stock level
        var existingStockLevels = await _stockLevelRepository.GetAllAsync(cancellationToken);
        var stockLevel = existingStockLevels.FirstOrDefault(sl => sl.PartId == partId && sl.WarehouseId == warehouseId);

        if (stockLevel != null)
            return stockLevel;

        // Create new stock level if it doesn't exist
        stockLevel = StockLevel.Create(partId, warehouseId);
        await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);
        return stockLevel;
    }
}
