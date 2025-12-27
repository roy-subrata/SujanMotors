namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a line item in a purchase order
/// </summary>
public class PurchaseOrderLine : AuditableEntity
{
    public Guid PurchaseOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public int Quantity { get; private set; }
    public int ReceivedQuantity { get; private set; } = 0;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public string Description { get; private set; } = string.Empty;
    public int LineNumber { get; private set; }

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Part? Part { get; set; }

    private PurchaseOrderLine() { }

    public static PurchaseOrderLine Create(Guid purchaseOrderId, Guid partId, int quantity,
        decimal unitPrice, int lineNumber, string description = "")
    {
        if (purchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId cannot be empty", nameof(purchaseOrderId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        if (lineNumber <= 0)
            throw new ArgumentException("LineNumber must be greater than 0", nameof(lineNumber));

        return new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrderId,
            PartId = partId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            LineNumber = lineNumber,
            Description = description?.Trim() ?? string.Empty
        };
    }

    public void UpdateReceivedQuantity(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Received quantity cannot be negative", nameof(quantity));

        if (quantity > Quantity)
            throw new InvalidOperationException("Received quantity cannot exceed ordered quantity");

        ReceivedQuantity = quantity;
    }

    public bool IsFullyReceived => ReceivedQuantity >= Quantity;
}
