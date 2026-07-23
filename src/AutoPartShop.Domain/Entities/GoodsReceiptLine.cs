namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a Goods Receipt Note
/// </summary>
public class GoodsReceiptLine : AuditableEntity
{
    public Guid GoodsReceiptId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? VariantId { get; private set; }  // Variant received (SKU-level); null for non-variant parts
    public Guid? UnitId { get; private set; }  // Unit of measurement for the received quantity
    public int OrderedQuantity { get; private set; }
    public int OrderedQuantityInBaseUnit { get; private set; }  // Converted to Part's base unit
    public int ReceivedQuantity { get; private set; }
    public int ReceivedQuantityInBaseUnit { get; private set; }  // Converted to Part's base unit
    // Discrepancy buckets (spec: Good = Received - Damaged - Wrong).
    public int DamagedQuantity { get; private set; } = 0;            // Damaged units -> Damaged stock + return
    public int DamagedQuantityInBaseUnit { get; private set; } = 0;  // Converted to Part's base unit
    public int WrongQuantity { get; private set; } = 0;             // Wrong/incorrect units -> Quarantine stock + return
    public int WrongQuantityInBaseUnit { get; private set; } = 0;    // Converted to Part's base unit
    public string Condition { get; private set; } = "GOOD";  // GOOD, ACCEPTABLE, DAMAGED, DEFECTIVE, MISSING
    public string RejectionReason { get; private set; } = string.Empty;  // Why units were rejected (kept separate from Notes)
    public string SerialNumbers { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Cost Information - actual cost at time of receipt
    public decimal UnitCost { get; private set; } = 0;  // Cost per unit as received
    public decimal UnitCostInBaseUnit { get; private set; } = 0;  // Cost per unit in base unit
    public string Currency { get; private set; } = "INR";  // Currency code

    // Batch / lot identification captured at receipt time
    public string BatchNumber { get; private set; } = string.Empty;  // Supplier's batch or lot number
    public DateTime? ExpiryDate { get; private set; }  // Expiry date for this lot (grocery, pharmacy, etc.)

    // Lot-level warranty (optional — used as lot override when creating the StockLot)
    public bool? HasWarranty { get; private set; }
    public int? WarrantyPeriodMonths { get; private set; }
    public string? WarrantyType { get; private set; }
    public string? WarrantyTerms { get; private set; }

    // Navigation properties
    public GoodsReceipt? GoodsReceipt { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public Product? Part { get; set; }
    public ProductVariant? Variant { get; set; }

    // Computed properties
    // Rejected = Damaged + Wrong (kept as a convenience for return creation / reporting)
    public int RejectedQuantity => DamagedQuantity + WrongQuantity;
    public int RejectedQuantityInBaseUnit => DamagedQuantityInBaseUnit + WrongQuantityInBaseUnit;
    public bool HasDiscrepancy => ReceivedQuantity != OrderedQuantity || RejectedQuantity > 0;
    // "Good" quantity = Received - Damaged - Wrong (this is what enters Available stock)
    public int AcceptedQuantity => ReceivedQuantity - RejectedQuantity;
    public int AcceptedQuantityInBaseUnit => ReceivedQuantityInBaseUnit - RejectedQuantityInBaseUnit;
    public decimal TotalCost => ReceivedQuantity * UnitCost;  // Total cost for all received items
    public decimal AcceptedTotalCost => AcceptedQuantity * UnitCost;  // Total cost for accepted items only
    public decimal TotalCostInBaseUnit => ReceivedQuantityInBaseUnit * UnitCostInBaseUnit;
    public decimal AcceptedTotalCostInBaseUnit => AcceptedQuantityInBaseUnit * UnitCostInBaseUnit;

    private GoodsReceiptLine() { }

    public static GoodsReceiptLine Create(Guid goodsReceiptId, Guid purchaseOrderLineId, Guid partId,
        int orderedQuantity, int receivedQuantity, string condition = "GOOD", decimal unitCost = 0,
        string currency = "INR", Guid? unitId = null, int orderedQuantityInBaseUnit = 0,
        int receivedQuantityInBaseUnit = 0, int damagedQuantityInBaseUnit = 0, int wrongQuantityInBaseUnit = 0,
        decimal unitCostInBaseUnit = 0,
        bool? hasWarranty = null, int? warrantyPeriodMonths = null,
        string? warrantyType = null, string? warrantyTerms = null,
        string? batchNumber = null, DateTime? expiryDate = null, Guid? variantId = null,
        int damagedQuantity = 0, int wrongQuantity = 0, string rejectionReason = "")
    {
        if (goodsReceiptId == Guid.Empty)
            throw new ArgumentException("GoodsReceiptId cannot be empty", nameof(goodsReceiptId));

        if (purchaseOrderLineId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderLineId cannot be empty", nameof(purchaseOrderLineId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (orderedQuantity <= 0)
            throw new ArgumentException("OrderedQuantity must be greater than 0", nameof(orderedQuantity));

        if (receivedQuantity < 0)
            throw new ArgumentException("ReceivedQuantity cannot be negative", nameof(receivedQuantity));

        if (unitCost < 0)
            throw new ArgumentException("UnitCost cannot be negative", nameof(unitCost));

        var validConditions = new[] { "GOOD", "ACCEPTABLE", "DAMAGED", "DEFECTIVE", "MISSING" };
        if (!validConditions.Contains(condition.ToUpper()))
            throw new ArgumentException($"Condition must be one of: {string.Join(", ", validConditions)}", nameof(condition));

        if (damagedQuantity < 0)
            throw new ArgumentException("DamagedQuantity cannot be negative", nameof(damagedQuantity));

        if (wrongQuantity < 0)
            throw new ArgumentException("WrongQuantity cannot be negative", nameof(wrongQuantity));

        if (damagedQuantity + wrongQuantity > receivedQuantity)
            throw new InvalidOperationException("Damaged + Wrong quantity cannot exceed ReceivedQuantity");

        return new GoodsReceiptLine
        {
            GoodsReceiptId = goodsReceiptId,
            PurchaseOrderLineId = purchaseOrderLineId,
            PartId = partId,
            VariantId = variantId,
            OrderedQuantity = orderedQuantity,
            OrderedQuantityInBaseUnit = orderedQuantityInBaseUnit > 0 ? orderedQuantityInBaseUnit : orderedQuantity,
            ReceivedQuantity = receivedQuantity,
            ReceivedQuantityInBaseUnit = receivedQuantityInBaseUnit > 0 ? receivedQuantityInBaseUnit : receivedQuantity,
            DamagedQuantity = damagedQuantity,
            DamagedQuantityInBaseUnit = damagedQuantityInBaseUnit > 0 ? damagedQuantityInBaseUnit : damagedQuantity,
            WrongQuantity = wrongQuantity,
            WrongQuantityInBaseUnit = wrongQuantityInBaseUnit > 0 ? wrongQuantityInBaseUnit : wrongQuantity,
            RejectionReason = rejectionReason?.Trim() ?? string.Empty,
            Condition = condition.ToUpper(),
            UnitCost = unitCost,
            UnitCostInBaseUnit = unitCostInBaseUnit > 0 ? unitCostInBaseUnit : unitCost,
            Currency = currency?.Trim().ToUpper() ?? "INR",
            UnitId = unitId,
            BatchNumber = batchNumber?.Trim() ?? string.Empty,
            ExpiryDate = expiryDate,
            HasWarranty = hasWarranty,
            WarrantyPeriodMonths = warrantyPeriodMonths,
            WarrantyType = warrantyType?.Trim(),
            WarrantyTerms = warrantyTerms?.Trim()
        };
    }

    /// <summary>
    /// Records the damaged and wrong (incorrect) quantities for this line.
    /// Damaged -> Damaged stock; Wrong -> Quarantine stock. Both can also raise a Purchase Return.
    /// </summary>
    public void SetDiscrepancy(int damagedQuantity, int wrongQuantity,
        int damagedQuantityInBaseUnit = 0, int wrongQuantityInBaseUnit = 0, string reason = "")
    {
        if (damagedQuantity < 0)
            throw new ArgumentException("DamagedQuantity cannot be negative", nameof(damagedQuantity));

        if (wrongQuantity < 0)
            throw new ArgumentException("WrongQuantity cannot be negative", nameof(wrongQuantity));

        if (damagedQuantity + wrongQuantity > ReceivedQuantity)
            throw new InvalidOperationException("Damaged + Wrong quantity cannot exceed ReceivedQuantity");

        DamagedQuantity = damagedQuantity;
        DamagedQuantityInBaseUnit = damagedQuantityInBaseUnit > 0 ? damagedQuantityInBaseUnit : damagedQuantity;
        WrongQuantity = wrongQuantity;
        WrongQuantityInBaseUnit = wrongQuantityInBaseUnit > 0 ? wrongQuantityInBaseUnit : wrongQuantity;
        RejectionReason = reason?.Trim() ?? string.Empty;
    }

    public void AddSerialNumbers(string serialNumbers)
    {
        SerialNumbers = serialNumbers?.Trim() ?? string.Empty;
    }

    public void SetNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void UpdateWarranty(bool? hasWarranty, int? warrantyPeriodMonths,
        string? warrantyType, string? warrantyTerms)
    {
        HasWarranty = hasWarranty;
        WarrantyPeriodMonths = warrantyPeriodMonths;
        WarrantyType = warrantyType?.Trim();
        WarrantyTerms = warrantyTerms?.Trim();
    }

    /// <summary>
    /// Corrects the unit cost after receipt (e.g. once the supplier invoice arrives). Cost is
    /// lot-driven, so a zero/blank cost would create zero-cost stock — this enforces a positive cost.
    /// <paramref name="unitCostInBaseUnit"/> should be the per-base-unit cost (falls back to unitCost
    /// when the received unit already is the base unit).
    /// </summary>
    public void UpdateCost(decimal unitCost, decimal unitCostInBaseUnit)
    {
        if (unitCost <= 0)
            throw new ArgumentException("UnitCost must be greater than zero", nameof(unitCost));

        UnitCost = unitCost;
        UnitCostInBaseUnit = unitCostInBaseUnit > 0 ? unitCostInBaseUnit : unitCost;
    }
}
