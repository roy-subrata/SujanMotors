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
    private readonly IStockLotMovementRepository _stockLotMovementRepository;
    private readonly IPartRepository _partRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly IUnitConversionService _unitConversionService;

    public StockManagementService(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLotRepository stockLotRepository,
        IStockLotMovementRepository stockLotMovementRepository,
        IPartRepository partRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        ICodeGenerateService codeGenerateService,
        IUnitConversionService unitConversionService)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLotRepository = stockLotRepository;
        _stockLotMovementRepository = stockLotMovementRepository;
        _partRepository = partRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _codeGenerateService = codeGenerateService;
        _unitConversionService = unitConversionService;
    }

    /// <summary>
    /// Processes a goods receipt by updating stock levels, creating stock lots, and creating audit trail
    /// </summary>
    public async Task ProcessGoodsReceiptAsync(GoodsReceipt goodsReceipt, CancellationToken cancellationToken = default)
    {
        if (goodsReceipt?.LineItems == null || !goodsReceipt.LineItems.Any())
            throw new ArgumentException("Goods receipt must have line items", nameof(goodsReceipt));

        // Prevent duplicate processing - check if GRN is already accepted
        if (goodsReceipt.Status == "ACCEPTED")
            throw new InvalidOperationException($"Goods receipt {goodsReceipt.GRNNumber} has already been processed");

        try
        {
            // Get Purchase Order to retrieve supplier information
            var purchaseOrder = await _purchaseOrderRepository.GetByIdAsync(goodsReceipt.PurchaseOrderId, cancellationToken);
            if (purchaseOrder == null)
                throw new InvalidOperationException($"Purchase order not found for goods receipt {goodsReceipt.GRNNumber}");

            foreach (var grnLine in goodsReceipt.LineItems)
            {
                var part = await _partRepository.GetByIdAsync(grnLine.PartId, cancellationToken);
                if (part == null)
                    throw new InvalidOperationException($"Part not found for goods receipt line {grnLine.Id}");

                // Calculate accepted quantity (received - rejected)
                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;
                if (acceptedQuantity <= 0)
                    continue;

                // Use the InBaseUnit quantities if available, otherwise calculate them
                var receivedBaseQuantity = grnLine.ReceivedQuantityInBaseUnit > 0
                    ? grnLine.ReceivedQuantityInBaseUnit
                    : grnLine.ReceivedQuantity;

                var acceptedBaseQuantity = grnLine.AcceptedQuantityInBaseUnit > 0
                    ? grnLine.AcceptedQuantityInBaseUnit
                    : acceptedQuantity;

                var baseUnitCost = grnLine.UnitCostInBaseUnit > 0
                    ? grnLine.UnitCostInBaseUnit
                    : grnLine.UnitCost;

                // If InBaseUnit fields are not populated, calculate conversion
                if (grnLine.ReceivedQuantityInBaseUnit == 0 && part.BaseUnitId.HasValue && grnLine.UnitId.HasValue)
                {
                    var conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                        grnLine.UnitId.Value,
                        part.BaseUnitId.Value);

                    if (conversionFactor <= 0)
                        throw new InvalidOperationException("Invalid unit conversion factor.");

                    receivedBaseQuantity = (int)Math.Round(grnLine.ReceivedQuantity * conversionFactor, MidpointRounding.AwayFromZero);
                    acceptedBaseQuantity = (int)Math.Round(acceptedQuantity * conversionFactor, MidpointRounding.AwayFromZero);
                    baseUnitCost = grnLine.UnitCost / conversionFactor;
                }

                // Get or create stock level for this part in this warehouse
                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId,
                    goodsReceipt.WarehouseId,
                    part.BaseUnitId,
                    cancellationToken);

                // Update stock level with both display and base unit quantities
                // IMPORTANT: Stock levels always track in BASE UNITS to avoid rounding issues
                // when buying/selling in different units (e.g., buy dozen, sell pieces)
                stockLevel.AddStock(
                    quantity: acceptedBaseQuantity,  // Use BASE UNIT for display tracking
                    quantityInBaseUnit: acceptedBaseQuantity,
                    reason: "GRN");
                await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                // Create stock movement audit record with both units
                var movement = StockMovement.Create(
                    stockLevelId: stockLevel.Id,
                    movementType: "IN",
                    quantity: acceptedBaseQuantity,  // Record in BASE UNITS
                    reason: "GRN",
                    referenceNumber: goodsReceipt.GRNNumber,
                    movementDate: DateTime.UtcNow,
                    unitId: part.BaseUnitId,  // Record movement in BASE UNITS
                    quantityInBaseUnit: acceptedBaseQuantity);

                // Auto-approve system movements
                movement.Approve("System");
                await _stockMovementRepository.AddAsync(movement, cancellationToken);

                // Create Stock Lot for batch/lot tracking and cost tracking
                // IMPORTANT: Always store quantities in BASE UNITS to avoid rounding issues
                // when selling in different units (e.g., buy dozen, sell pieces)
                var lotNumber = await _codeGenerateService.GenerateAsync("LOT", cancellationToken);
                var stockLot = StockLot.Create(
                    lotNumber: lotNumber,
                    partId: grnLine.PartId,
                    warehouseId: goodsReceipt.WarehouseId,
                    supplierId: purchaseOrder.SupplierId,
                    goodsReceiptLineId: grnLine.Id,
                    quantityReceived: receivedBaseQuantity,
                    costPrice: baseUnitCost,
                    receivingDate: goodsReceipt.ReceiptDate,
                    manufacturerLotNumber: grnLine.BatchNumber,  // Supplier's batch/lot number from GRN
                    expiryDate: grnLine.ExpiryDate,             // Per-lot expiry from GRN (grocery, pharmacy)
                    currency: grnLine.Currency,
                    notes: $"Created from GRN {goodsReceipt.GRNNumber}",
                    unitId: part.BaseUnitId,
                    quantityReceivedInBaseUnit: receivedBaseQuantity,
                    costPriceInBaseUnit: baseUnitCost,
                    // Lot-level overrides from GRN line; fall back to Part master defaults
                    sellingPrice: grnLine.SellingPrice ?? part.SellingPrice,
                    hasWarranty: grnLine.HasWarranty ?? part.HasWarranty,
                    warrantyPeriodMonths: grnLine.WarrantyPeriodMonths ?? part.WarrantyPeriodMonths,
                    warrantyType: grnLine.WarrantyType ?? part.WarrantyType,
                    warrantyTerms: grnLine.WarrantyTerms ?? part.WarrantyTerms
                );

                stockLot.CreatedBy = "System";
                stockLot.ModifiedBy = "System";
                await _stockLotRepository.AddAsync(stockLot, cancellationToken);
                // Persist LOT sequence so future receipts don't reuse the same lot number.
                await _codeGenerateService.SaveGenerateCodeAsync("LOT", cancellationToken);

                // Record initial RECEIPT movement so the lot has a full movement history from day one
                var lotMovement = StockLotMovement.Create(
                    stockLotId: stockLot.Id,
                    quantity: receivedBaseQuantity,
                    movementType: "RECEIPT",
                    referenceId: goodsReceipt.Id,
                    referenceType: "GoodsReceipt",
                    movementDate: goodsReceipt.ReceiptDate,
                    costAtMovement: baseUnitCost,
                    reason: "GRN",
                    notes: goodsReceipt.GRNNumber,
                    unitId: part.BaseUnitId,
                    quantityInBaseUnit: receivedBaseQuantity,
                    costAtMovementInBaseUnit: baseUnitCost
                );
                lotMovement.CreatedBy = "System";
                lotMovement.ModifiedBy = "System";
                await _stockLotMovementRepository.AddAsync(lotMovement, cancellationToken);
            }

            // Update Purchase Order line received quantities and receipt status
            // This is done here because the PO is already loaded and tracked by DbContext
            await UpdatePurchaseOrderReceiptStatusAsync(purchaseOrder, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error processing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Updates Purchase Order line received quantities and receipt status
    /// </summary>
    private async Task UpdatePurchaseOrderReceiptStatusAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        if (purchaseOrder?.LineItems == null)
            return;

        try
        {
            // Update received quantities for each line item based on ALL accepted GRNs
            foreach (var poLine in purchaseOrder.LineItems)
            {
                // Calculate total accepted quantity for this part from ALL accepted GRNs
                var totalAcceptedForPart = purchaseOrder.GoodsReceipts
                    .Where(gr => gr.Status == "ACCEPTED")
                    .SelectMany(gr => gr.LineItems)
                    .Where(l => l.PartId == poLine.PartId)
                    .Sum(l => l.AcceptedQuantity);

                if (totalAcceptedForPart > 0)
                {
                    poLine.UpdateReceivedQuantity(totalAcceptedForPart);
                }
            }

            // Update PO receipt status (PARTIAL or DELIVERED)
            purchaseOrder.UpdateReceiptStatus();
            purchaseOrder.ModifiedBy = "System";

            // Don't call UpdateAsync - the purchaseOrder is already tracked by DbContext
            // EF Core will automatically detect changes and persist them on next SaveChanges
        }
        catch (Exception ex)
        {
            // Log but don't throw - stock updates are more critical than PO status
            // The PO status can be manually updated later if needed
            throw new InvalidOperationException(
                $"Error updating purchase order receipt status: {ex.Message}", ex);
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
                var part = await _partRepository.GetByIdAsync(grnLine.PartId, cancellationToken);
                if (part == null)
                    throw new InvalidOperationException($"Part not found for goods receipt line {grnLine.Id}");

                int acceptedQuantity = grnLine.ReceivedQuantity - grnLine.RejectedQuantity;
                if (acceptedQuantity <= 0)
                    continue;

                // Use InBaseUnit quantities if available
                var acceptedBaseQuantity = grnLine.AcceptedQuantityInBaseUnit > 0 
                    ? grnLine.AcceptedQuantityInBaseUnit 
                    : acceptedQuantity;

                // If InBaseUnit fields are not populated, calculate conversion
                if (grnLine.AcceptedQuantityInBaseUnit == 0 && part.BaseUnitId.HasValue && grnLine.UnitId.HasValue)
                {
                    var conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                        grnLine.UnitId.Value,
                        part.BaseUnitId.Value);

                    if (conversionFactor <= 0)
                        throw new InvalidOperationException("Invalid unit conversion factor.");

                    acceptedBaseQuantity = (int)Math.Round(acceptedQuantity * conversionFactor, MidpointRounding.AwayFromZero);
                }

                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId,
                    goodsReceipt.WarehouseId,
                    part.BaseUnitId,
                    cancellationToken);

                // Remove the stock that was added (using both units)
                if (acceptedBaseQuantity <= stockLevel.QuantityOnHandInBaseUnit)
                {
                    stockLevel.RemoveStock(
                        quantity: acceptedQuantity, 
                        quantityInBaseUnit: acceptedBaseQuantity, 
                        reason: "GRN Reversal");
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    // Create reversal movement record with both units
                    var movement = StockMovement.Create(
                        stockLevelId: stockLevel.Id,
                        movementType: "OUT",
                        quantity: acceptedQuantity,
                        reason: "GRN Reversal",
                        referenceNumber: goodsReceipt.GRNNumber,
                        movementDate: DateTime.UtcNow,
                        unitId: grnLine.UnitId ?? part.BaseUnitId,
                        quantityInBaseUnit: acceptedBaseQuantity);

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
    private async Task<StockLevel> GetOrCreateStockLevelAsync(
        Guid partId, 
        Guid warehouseId, 
        Guid? baseUnitId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing stock level
        var existingStockLevels = await _stockLevelRepository.GetAllAsync(cancellationToken);
        var stockLevel = existingStockLevels.FirstOrDefault(sl => sl.PartId == partId && sl.WarehouseId == warehouseId);

        if (stockLevel != null)
            return stockLevel;

        // Create new stock level if it doesn't exist
        stockLevel = StockLevel.Create(partId, warehouseId, unitId: baseUnitId);
        await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);
        return stockLevel;
    }
}
