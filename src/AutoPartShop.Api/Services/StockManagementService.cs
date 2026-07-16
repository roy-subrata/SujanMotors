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
    private readonly IProductRepository _productRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IPurchaseReturnRepository _purchaseReturnRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly IUnitConversionService _unitConversionService;

    public StockManagementService(
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLotRepository stockLotRepository,
        IStockLotMovementRepository stockLotMovementRepository,
        IProductRepository productRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IPurchaseReturnRepository purchaseReturnRepository,
        ICodeGenerateService codeGenerateService,
        IUnitConversionService unitConversionService)
    {
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLotRepository = stockLotRepository;
        _stockLotMovementRepository = stockLotMovementRepository;
        _productRepository = productRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _purchaseReturnRepository = purchaseReturnRepository;
        _codeGenerateService = codeGenerateService;
        _unitConversionService = unitConversionService;
    }

    /// <summary>
    /// Processes a goods receipt by updating stock levels, creating stock lots, and creating audit trail
    /// </summary>
    public async Task ProcessGoodsReceiptAsync(GoodsReceipt goodsReceipt, bool createReturn = false, CancellationToken cancellationToken = default)
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

            // Capture the damaged/quarantine lots created per GRN line so the auto-return (below) can
            // link each return line to the specific lot it should draw down on "mark returned".
            var rejectedLotsByGrnLine = new Dictionary<Guid, (StockLot? damaged, StockLot? quarantine)>();

            foreach (var grnLine in goodsReceipt.LineItems)
            {
                var part = await _productRepository.GetByIdAsync(grnLine.PartId, cancellationToken);
                if (part == null)
                    throw new InvalidOperationException($"Part not found for goods receipt line {grnLine.Id}");

                // Resolve the unit conversion factor once for the line. Stock is always tracked in
                // BASE UNITS so buying/selling in different units doesn't introduce rounding drift.
                decimal conversionFactor = 1m;
                if (grnLine.ReceivedQuantityInBaseUnit == 0 && part.BaseUnitId.HasValue && grnLine.UnitId.HasValue
                    && grnLine.UnitId.Value != part.BaseUnitId.Value)
                {
                    conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                        grnLine.UnitId.Value, part.BaseUnitId.Value);
                    if (conversionFactor <= 0)
                        throw new InvalidOperationException("Invalid unit conversion factor.");
                }

                var baseUnitCost = grnLine.UnitCostInBaseUnit > 0
                    ? grnLine.UnitCostInBaseUnit
                    : grnLine.UnitCost / conversionFactor;

                // Helper: convert a per-bucket display quantity to base units, preferring pre-computed values.
                int ToBase(int qty, int providedBase) =>
                    providedBase > 0 ? providedBase : (int)Math.Round(qty * conversionFactor, MidpointRounding.AwayFromZero);

                // Post each condition bucket to its own inventory status (spec):
                //  Good    -> Available (sellable)
                //  Damaged -> Damaged   (held, not sellable)
                //  Wrong   -> Quarantine(held, not sellable)
                await PostBucketAsync(goodsReceipt, grnLine, part, purchaseOrder,
                    qty: grnLine.AcceptedQuantity, baseQty: ToBase(grnLine.AcceptedQuantity, grnLine.AcceptedQuantityInBaseUnit),
                    baseUnitCost, status: "AVAILABLE", movementReason: "GRN", cancellationToken);

                var damagedLot = await PostBucketAsync(goodsReceipt, grnLine, part, purchaseOrder,
                    qty: grnLine.DamagedQuantity, baseQty: ToBase(grnLine.DamagedQuantity, grnLine.DamagedQuantityInBaseUnit),
                    baseUnitCost, status: "DAMAGED", movementReason: "GRN-DAMAGED", cancellationToken);

                var quarantineLot = await PostBucketAsync(goodsReceipt, grnLine, part, purchaseOrder,
                    qty: grnLine.WrongQuantity, baseQty: ToBase(grnLine.WrongQuantity, grnLine.WrongQuantityInBaseUnit),
                    baseUnitCost, status: "QUARANTINE", movementReason: "GRN-QUARANTINE", cancellationToken);

                if (damagedLot != null || quarantineLot != null)
                    rejectedLotsByGrnLine[grnLine.Id] = (damagedLot, quarantineLot);
            }

            // Optionally raise a draft Purchase Return for the damaged/wrong lines (spec: "Post & Create
            // Return"). PO -> GoodsReceipt -> PurchaseReturn audit trail. Created PENDING for approval/settlement.
            if (createReturn)
                await CreatePurchaseReturnForRejectedLinesAsync(goodsReceipt, purchaseOrder, rejectedLotsByGrnLine, cancellationToken);

            // Update Purchase Order line received quantities and receipt status
            // This is done here because the PO is already loaded and tracked by DbContext
            await UpdatePurchaseOrderReceiptStatusAsync(purchaseOrder, goodsReceipt.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error processing goods receipt {goodsReceipt.GRNNumber}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Posts a single condition bucket (Good/Damaged/Wrong) of a GRN line into the matching inventory
    /// status: updates the StockLevel bucket, records a StockMovement, and creates a status-tagged StockLot
    /// with its own RECEIPT movement. No-op when quantity is 0.
    /// </summary>
    private async Task<StockLot?> PostBucketAsync(
        GoodsReceipt goodsReceipt, GoodsReceiptLine grnLine, Product part, PurchaseOrder purchaseOrder,
        int qty, int baseQty, decimal baseUnitCost, string status, string movementReason,
        CancellationToken cancellationToken)
    {
        if (qty <= 0 || baseQty <= 0)
            return null;

        // Get or create stock level for this part/variant in this warehouse
        var stockLevel = await GetOrCreateStockLevelAsync(
            grnLine.PartId, grnLine.VariantId, goodsReceipt.WarehouseId, part.BaseUnitId, cancellationToken);

        // Route the quantity into the correct (non-)sellable bucket
        switch (status)
        {
            case "DAMAGED":
                stockLevel.AddDamagedStock(baseQty, baseQty, movementReason);
                break;
            case "QUARANTINE":
                stockLevel.AddQuarantineStock(baseQty, baseQty, movementReason);
                break;
            default:
                stockLevel.AddStock(baseQty, baseQty, movementReason);
                break;
        }
        await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

        // Stock movement audit record (always in BASE UNITS)
        var movement = StockMovement.Create(
            stockLevelId: stockLevel.Id,
            movementType: "IN",
            quantity: baseQty,
            reason: movementReason,
            referenceNumber: goodsReceipt.GRNNumber,
            movementDate: DateTime.UtcNow,
            unitId: part.BaseUnitId,
            quantityInBaseUnit: baseQty);
        movement.Approve("System");
        await _stockMovementRepository.AddAsync(movement, cancellationToken);

        // Status-tagged stock lot for batch/lot + cost tracking (Damaged/Quarantine lots are excluded from sale)
        var lotNumber = await _codeGenerateService.GenerateAsync("LOT", cancellationToken);
        var stockLot = StockLot.Create(
            lotNumber: lotNumber,
            partId: grnLine.PartId,
            warehouseId: goodsReceipt.WarehouseId,
            supplierId: purchaseOrder.SupplierId,
            goodsReceiptLineId: grnLine.Id,
            quantityReceived: baseQty,
            costPrice: baseUnitCost,
            receivingDate: goodsReceipt.ReceiptDate,
            manufacturerLotNumber: grnLine.BatchNumber,
            expiryDate: grnLine.ExpiryDate,
            currency: grnLine.Currency,
            notes: $"Created from GRN {goodsReceipt.GRNNumber} ({status})",
            unitId: part.BaseUnitId,
            quantityReceivedInBaseUnit: baseQty,
            costPriceInBaseUnit: baseUnitCost,
            hasWarranty: grnLine.HasWarranty ?? part.HasWarranty,
            warrantyPeriodMonths: grnLine.WarrantyPeriodMonths ?? part.WarrantyPeriodMonths,
            warrantyType: grnLine.WarrantyType ?? part.WarrantyType,
            warrantyTerms: grnLine.WarrantyTerms ?? part.WarrantyTerms,
            variantId: grnLine.VariantId,
            status: status);
        stockLot.CreatedBy = "System";
        stockLot.ModifiedBy = "System";
        await _stockLotRepository.AddAsync(stockLot, cancellationToken);

        var lotMovement = StockLotMovement.Create(
            stockLotId: stockLot.Id,
            quantity: baseQty,
            movementType: "RECEIPT",
            referenceId: goodsReceipt.Id,
            referenceType: "GoodsReceipt",
            movementDate: goodsReceipt.ReceiptDate,
            costAtMovement: baseUnitCost,
            reason: movementReason,
            notes: goodsReceipt.GRNNumber,
            unitId: part.BaseUnitId,
            quantityInBaseUnit: baseQty,
            costAtMovementInBaseUnit: baseUnitCost);
        lotMovement.CreatedBy = "System";
        lotMovement.ModifiedBy = "System";
        await _stockLotMovementRepository.AddAsync(lotMovement, cancellationToken);

        return stockLot;
    }

    /// <summary>
    /// Creates a single PENDING Purchase Return aggregating all rejected/damaged lines of a GRN.
    /// Linked to the originating GoodsReceipt for the PO -> GR -> PR audit trail. No-op when nothing
    /// was rejected.
    /// </summary>
    private async Task CreatePurchaseReturnForRejectedLinesAsync(
        GoodsReceipt goodsReceipt, PurchaseOrder purchaseOrder,
        IReadOnlyDictionary<Guid, (StockLot? damaged, StockLot? quarantine)> rejectedLotsByGrnLine,
        CancellationToken cancellationToken)
    {
        var rejectedLines = goodsReceipt.LineItems
            .Where(l => l.DamagedQuantity > 0 || l.WrongQuantity > 0)
            .ToList();

        if (rejectedLines.Count == 0)
            return;

        // Header reason: WRONG_ITEM only when there are wrong items and no damaged ones, otherwise DAMAGED.
        var hasDamaged = rejectedLines.Any(l => l.DamagedQuantity > 0);
        var hasWrong = rejectedLines.Any(l => l.WrongQuantity > 0);
        var headerReason = hasWrong && !hasDamaged ? "WRONG_ITEM" : "DAMAGED";

        var returnNumber = await _codeGenerateService.GenerateAsync("PR", cancellationToken);
        var purchaseReturn = PurchaseReturn.Create(
            returnNumber,
            purchaseOrder.Id,
            purchaseOrder.SupplierId,
            reason: headerReason,
            notes: $"Auto-created from GRN {goodsReceipt.GRNNumber}");
        purchaseReturn.LinkToGoodsReceipt(goodsReceipt.Id);
        purchaseReturn.CreatedBy = "System";
        purchaseReturn.ModifiedBy = "System";

        foreach (var grnLine in rejectedLines)
        {
            // PurchaseReturnLine requires a positive unit price; fall back to the PO line price
            // (always > 0) when the receipt didn't capture a cost.
            var poLine = purchaseOrder.LineItems.FirstOrDefault(l => l.Id == grnLine.PurchaseOrderLineId);
            var unitPrice = grnLine.UnitCost > 0 ? grnLine.UnitCost : (poLine?.UnitPrice ?? grnLine.UnitCost);
            rejectedLotsByGrnLine.TryGetValue(grnLine.Id, out var lots);

            // One return line per bucket so the physical condition is recorded accurately:
            //  Damaged units -> DAMAGED/DEFECTIVE; Wrong units -> OPENED (physically fine but incorrect item).
            // Each line is linked to the specific lot it should draw down (bucket is derived from lot.Status).
            if (grnLine.DamagedQuantity > 0)
            {
                var damagedLine = PurchaseReturnLine.Create(
                    purchaseReturn.Id, grnLine.PurchaseOrderLineId, grnLine.PartId,
                    quantity: grnLine.DamagedQuantity, unitPrice: unitPrice,
                    condition: MapToReturnCondition(grnLine.Condition),
                    stockLotId: lots.damaged?.Id);
                damagedLine.AddNotes(string.IsNullOrWhiteSpace(grnLine.RejectionReason) ? "Damaged on receipt" : grnLine.RejectionReason);
                damagedLine.CreatedBy = "System";
                damagedLine.ModifiedBy = "System";
                purchaseReturn.LineItems.Add(damagedLine);
            }

            if (grnLine.WrongQuantity > 0)
            {
                var wrongLine = PurchaseReturnLine.Create(
                    purchaseReturn.Id, grnLine.PurchaseOrderLineId, grnLine.PartId,
                    quantity: grnLine.WrongQuantity, unitPrice: unitPrice,
                    condition: "OPENED",
                    stockLotId: lots.quarantine?.Id);
                wrongLine.AddNotes(string.IsNullOrWhiteSpace(grnLine.RejectionReason) ? "Wrong / incorrect item" : grnLine.RejectionReason);
                wrongLine.CreatedBy = "System";
                wrongLine.ModifiedBy = "System";
                purchaseReturn.LineItems.Add(wrongLine);
            }
        }

        purchaseReturn.CalculateRefund();
        await _purchaseReturnRepository.AddAsync(purchaseReturn, cancellationToken);
    }

    /// <summary>
    /// Maps a GoodsReceiptLine condition (GOOD/ACCEPTABLE/DAMAGED/DEFECTIVE/MISSING) to a valid
    /// PurchaseReturnLine condition (UNOPENED/OPENED/DAMAGED/DEFECTIVE).
    /// </summary>
    private static string MapToReturnCondition(string grnCondition) => grnCondition?.ToUpper() switch
    {
        "DEFECTIVE" => "DEFECTIVE",
        _ => "DAMAGED"
    };

    /// <summary>
    /// Updates Purchase Order line received quantities and receipt status
    /// </summary>
    private async Task UpdatePurchaseOrderReceiptStatusAsync(PurchaseOrder purchaseOrder, Guid acceptingGrnId, CancellationToken cancellationToken = default)
    {
        if (purchaseOrder?.LineItems == null)
            return;

        try
        {
            // Update received quantities for each line item based on ALL accepted GRNs
            foreach (var poLine in purchaseOrder.LineItems)
            {
                // Calculate total accepted quantity for THIS PO line from ALL accepted GRNs.
                // Match on PurchaseOrderLineId so multiple lines of the same part (e.g. different
                // variants) each receive only their own quantity instead of the combined total.
                // The GRN being accepted right now is still VERIFIED in this snapshot (its status
                // flips only after stock processing succeeds), so include it by id — otherwise the
                // PO's received quantities lag one receipt behind and it never reaches DELIVERED.
                var totalAcceptedForLine = purchaseOrder.GoodsReceipts
                    .Where(gr => gr.Status == "ACCEPTED" || gr.Id == acceptingGrnId)
                    .SelectMany(gr => gr.LineItems)
                    .Where(l => l.PurchaseOrderLineId == poLine.Id)
                    .Sum(l => l.AcceptedQuantity);

                if (totalAcceptedForLine > 0)
                {
                    poLine.UpdateReceivedQuantity(totalAcceptedForLine);
                }
            }

            // Update PO receipt status (PARTIAL or DELIVERED)
            purchaseOrder.UpdateReceiptStatus();
            purchaseOrder.ModifiedBy = "System";

            // GetByIdAsync uses AsNoTracking, so we must explicitly persist via UpdateAsync.
            await _purchaseOrderRepository.UpdateAsync(purchaseOrder, cancellationToken);
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
                var part = await _productRepository.GetByIdAsync(grnLine.PartId, cancellationToken);
                if (part == null)
                    throw new InvalidOperationException($"Part not found for goods receipt line {grnLine.Id}");

                decimal conversionFactor = 1m;
                if (grnLine.ReceivedQuantityInBaseUnit == 0 && part.BaseUnitId.HasValue && grnLine.UnitId.HasValue
                    && grnLine.UnitId.Value != part.BaseUnitId.Value)
                {
                    conversionFactor = await _unitConversionService.GetConversionFactorAsync(
                        grnLine.UnitId.Value, part.BaseUnitId.Value);
                    if (conversionFactor <= 0)
                        throw new InvalidOperationException("Invalid unit conversion factor.");
                }

                int ToBase(int qty, int providedBase) =>
                    providedBase > 0 ? providedBase : (int)Math.Round(qty * conversionFactor, MidpointRounding.AwayFromZero);

                var stockLevel = await GetOrCreateStockLevelAsync(
                    grnLine.PartId, grnLine.VariantId, goodsReceipt.WarehouseId, part.BaseUnitId, cancellationToken);

                // Reverse each bucket out of its inventory status
                ReverseBucket(stockLevel, "AVAILABLE", grnLine.AcceptedQuantity, ToBase(grnLine.AcceptedQuantity, grnLine.AcceptedQuantityInBaseUnit));
                ReverseBucket(stockLevel, "DAMAGED", grnLine.DamagedQuantity, ToBase(grnLine.DamagedQuantity, grnLine.DamagedQuantityInBaseUnit));
                ReverseBucket(stockLevel, "QUARANTINE", grnLine.WrongQuantity, ToBase(grnLine.WrongQuantity, grnLine.WrongQuantityInBaseUnit));
                await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                // Record a single OUT movement for the whole reversal of this line (base units)
                var reversedBase = ToBase(grnLine.AcceptedQuantity, grnLine.AcceptedQuantityInBaseUnit)
                    + ToBase(grnLine.DamagedQuantity, grnLine.DamagedQuantityInBaseUnit)
                    + ToBase(grnLine.WrongQuantity, grnLine.WrongQuantityInBaseUnit);
                if (reversedBase > 0)
                {
                    var movement = StockMovement.Create(
                        stockLevelId: stockLevel.Id,
                        movementType: "OUT",
                        quantity: reversedBase,
                        reason: "GRN Reversal",
                        referenceNumber: goodsReceipt.GRNNumber,
                        movementDate: DateTime.UtcNow,
                        unitId: part.BaseUnitId,
                        quantityInBaseUnit: reversedBase);
                    movement.Approve("System");
                    await _stockMovementRepository.AddAsync(movement, cancellationToken);
                }

                // Deactivate the lots this GRN line created so they no longer surface anywhere
                var lots = await _stockLotRepository.GetByGoodsReceiptLineAsync(grnLine.Id, cancellationToken);
                foreach (var lot in lots)
                {
                    lot.Deactivate();
                    lot.ModifiedBy = "System";
                    await _stockLotRepository.UpdateAsync(lot, cancellationToken);
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
    /// Removes a quantity from the given stock-level bucket, capped at what's actually on hand so a
    /// reversal can never drive a bucket negative. No-op when quantity is 0.
    /// </summary>
    private static void ReverseBucket(StockLevel stockLevel, string status, int qty, int baseQty)
    {
        if (qty <= 0 || baseQty <= 0)
            return;

        switch (status)
        {
            case "DAMAGED":
                stockLevel.RemoveDamagedStock(qty, baseQty, "GRN Reversal");
                break;
            case "QUARANTINE":
                stockLevel.RemoveQuarantineStock(qty, baseQty, "GRN Reversal");
                break;
            default:
                if (baseQty <= stockLevel.QuantityOnHandInBaseUnit && qty <= stockLevel.QuantityAvailable)
                    stockLevel.RemoveStock(qty, baseQty, "GRN Reversal");
                break;
        }
    }

    /// <summary>
    /// Gets existing stock level or creates a new one if it doesn't exist
    /// </summary>
    private async Task<StockLevel> GetOrCreateStockLevelAsync(
        Guid partId,
        Guid? variantId,
        Guid warehouseId,
        Guid? baseUnitId,
        CancellationToken cancellationToken = default)
    {
        // Try to find existing stock level for this exact (part, variant, warehouse)
        var stockLevel = await _stockLevelRepository.GetByPartVariantAndWarehouseAsync(
            partId, variantId, warehouseId, cancellationToken);

        if (stockLevel != null)
            return stockLevel;

        // Create new stock level if it doesn't exist
        stockLevel = StockLevel.Create(partId, warehouseId, unitId: baseUnitId, variantId: variantId);
        await _stockLevelRepository.AddAsync(stockLevel, cancellationToken);
        return stockLevel;
    }
}
