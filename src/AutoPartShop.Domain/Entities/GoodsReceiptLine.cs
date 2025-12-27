namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a Goods Receipt Note
/// </summary>
public class GoodsReceiptLine : AuditableEntity
{
    public Guid GoodsReceiptId { get; private set; }
    public Guid PurchaseOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public int OrderedQuantity { get; private set; }
    public int ReceivedQuantity { get; private set; }
    public int RejectedQuantity { get; private set; } = 0;
    public string Condition { get; private set; } = "GOOD";  // GOOD, DAMAGED, MISSING
    public string SerialNumbers { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;

    // Cost Information - actual cost at time of receipt
    public decimal UnitCost { get; private set; } = 0;  // Cost per unit as received
    public string Currency { get; private set; } = "INR";  // Currency code
    public Guid? UnitId { get; private set; }  // Unit of measurement for the received quantity

    // Navigation properties
    public GoodsReceipt? GoodsReceipt { get; set; }

    public PurchaseOrderLine? PurchaseOrderLine { get; set; }
    public Part? Part { get; set; }

    // Computed properties
    public bool HasDiscrepancy => ReceivedQuantity != OrderedQuantity || RejectedQuantity > 0;
    public int AcceptedQuantity => ReceivedQuantity - RejectedQuantity;
    public decimal TotalCost => ReceivedQuantity * UnitCost;  // Total cost for all received items
    public decimal AcceptedTotalCost => AcceptedQuantity * UnitCost;  // Total cost for accepted items only

    private GoodsReceiptLine() { }

    public static GoodsReceiptLine Create(Guid goodsReceiptId, Guid purchaseOrderLineId, Guid partId,
        int orderedQuantity, int receivedQuantity, string condition = "GOOD", decimal unitCost = 0,
        string currency = "INR", Guid? unitId = null)
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
            ReceivedQuantity = receivedQuantity,
            Condition = condition.ToUpper(),
            UnitCost = unitCost,
            Currency = currency?.Trim().ToUpper() ?? "INR",
            UnitId = unitId
        };
    }

    public void RejectQuantity(int quantity, string reason = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (quantity > ReceivedQuantity)
            throw new InvalidOperationException("Cannot reject more than received");

        RejectedQuantity = quantity;
        Notes = reason?.Trim() ?? string.Empty;
    }

    public void AddSerialNumbers(string serialNumbers)
    {
        SerialNumbers = serialNumbers?.Trim() ?? string.Empty;
    }
}
