namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a Goods Receipt Note
/// </summary>
public class GoodsReceiptLine : AuditableEntity
{
    public Guid GoodsReceiptId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? UnitId { get; private set; }  // Unit of measurement for the received quantity
    public int OrderedQuantity { get; private set; }
    public int OrderedQuantityInBaseUnit { get; private set; }  // Converted to Part's base unit
    public int ReceivedQuantity { get; private set; }
    public int ReceivedQuantityInBaseUnit { get; private set; }  // Converted to Part's base unit
    public int RejectedQuantity { get; private set; } = 0;
    public int RejectedQuantityInBaseUnit { get; private set; } = 0;  // Converted to Part's base unit
    public string Condition { get; private set; } = "GOOD";  // GOOD, DAMAGED, MISSING
    public string SerialNumbers { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Cost Information - actual cost at time of receipt
    public decimal UnitCost { get; private set; } = 0;  // Cost per unit as received
    public decimal UnitCostInBaseUnit { get; private set; } = 0;  // Cost per unit in base unit
    public string Currency { get; private set; } = "INR";  // Currency code

    // Batch / lot identification captured at receipt time
    public string BatchNumber { get; private set; } = string.Empty;  // Supplier's batch or lot number
    public DateTime? ExpiryDate { get; private set; }  // Expiry date for this lot (grocery, pharmacy, etc.)

    // Lot-level selling price & warranty (optional — used as lot overrides when creating the StockLot)
    public decimal? SellingPrice { get; private set; }
    public bool? HasWarranty { get; private set; }
    public int? WarrantyPeriodMonths { get; private set; }
    public string? WarrantyType { get; private set; }
    public string? WarrantyTerms { get; private set; }

    // Navigation properties
    public GoodsReceipt? GoodsReceipt { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public Product? Part { get; set; }

    // Computed properties
    public bool HasDiscrepancy => ReceivedQuantity != OrderedQuantity || RejectedQuantity > 0;
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
        int receivedQuantityInBaseUnit = 0, int rejectedQuantityInBaseUnit = 0, decimal unitCostInBaseUnit = 0,
        decimal? sellingPrice = null, bool? hasWarranty = null, int? warrantyPeriodMonths = null,
        string? warrantyType = null, string? warrantyTerms = null,
        string? batchNumber = null, DateTime? expiryDate = null)
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

        var validConditions = new[] { "GOOD", "DAMAGED", "MISSING" };
        if (!validConditions.Contains(condition.ToUpper()))
            throw new ArgumentException($"Condition must be one of: {string.Join(", ", validConditions)}", nameof(condition));

        return new GoodsReceiptLine
        {
            GoodsReceiptId = goodsReceiptId,
            PurchaseOrderLineId = purchaseOrderLineId,
            PartId = partId,
            OrderedQuantity = orderedQuantity,
            OrderedQuantityInBaseUnit = orderedQuantityInBaseUnit > 0 ? orderedQuantityInBaseUnit : orderedQuantity,
            ReceivedQuantity = receivedQuantity,
            ReceivedQuantityInBaseUnit = receivedQuantityInBaseUnit > 0 ? receivedQuantityInBaseUnit : receivedQuantity,
            Condition = condition.ToUpper(),
            UnitCost = unitCost,
            UnitCostInBaseUnit = unitCostInBaseUnit > 0 ? unitCostInBaseUnit : unitCost,
            Currency = currency?.Trim().ToUpper() ?? "INR",
            UnitId = unitId,
            BatchNumber = batchNumber?.Trim() ?? string.Empty,
            ExpiryDate = expiryDate,
            SellingPrice = sellingPrice,
            HasWarranty = hasWarranty,
            WarrantyPeriodMonths = warrantyPeriodMonths,
            WarrantyType = warrantyType?.Trim(),
            WarrantyTerms = warrantyTerms?.Trim()
        };
    }

    public void RejectQuantity(int quantity, int quantityInBaseUnit = 0, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > ReceivedQuantity)
            throw new InvalidOperationException("Cannot reject more than received");

        RejectedQuantity = quantity;
        RejectedQuantityInBaseUnit = quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity;
        Notes = reason?.Trim() ?? string.Empty;
    }

    public void AddSerialNumbers(string serialNumbers)
    {
        SerialNumbers = serialNumbers?.Trim() ?? string.Empty;
    }

    public void UpdatePricing(decimal? sellingPrice, bool? hasWarranty, int? warrantyPeriodMonths,
        string? warrantyType, string? warrantyTerms)
    {
        SellingPrice = sellingPrice;
        HasWarranty = hasWarranty;
        WarrantyPeriodMonths = warrantyPeriodMonths;
        WarrantyType = warrantyType?.Trim();
        WarrantyTerms = warrantyTerms?.Trim();
    }
}
