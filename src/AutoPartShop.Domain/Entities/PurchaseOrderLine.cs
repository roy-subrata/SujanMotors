namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a line item in a purchase order
/// </summary>
public class PurchaseOrderLine : AuditableEntity
{
    public Guid PurchaseOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? UnitId { get; private set; }  // Unit in which the part is purchased
    public int Quantity { get; private set; }
    public int QuantityInBaseUnit { get; private set; }  // Converted to Part's base unit for stock
    public int ReceivedQuantity { get; private set; } = 0;
    public int ReceivedQuantityInBaseUnit { get; private set; } = 0;
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;
    public string Description { get; private set; } = string.Empty;
    public int LineNumber { get; private set; }

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Part? Part { get; set; }
    public Unit? Unit { get; set; }

    private PurchaseOrderLine() { }

    public static PurchaseOrderLine Create(Guid purchaseOrderId, Guid partId, int quantity,
        decimal unitPrice, int lineNumber, Guid? unitId = null, int quantityInBaseUnit = 0, string description = "")
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

        // If no quantityInBaseUnit provided, assume quantity is already in base unit
        if (quantityInBaseUnit == 0)
            quantityInBaseUnit = quantity;

        return new PurchaseOrderLine
        {
            PurchaseOrderId = purchaseOrderId,
            PartId = partId,
            UnitId = unitId,
            Quantity = quantity,
            QuantityInBaseUnit = quantityInBaseUnit,
            UnitPrice = unitPrice,
            LineNumber = lineNumber,
            Description = description?.Trim() ?? string.Empty
        };
    }

    public void UpdateReceivedQuantity(int quantity, int quantityInBaseUnit = 0)
    {
        if (quantity < 0)
            throw new ArgumentException("Received quantity cannot be negative", nameof(quantity));

        if (quantity > Quantity)
            throw new InvalidOperationException("Received quantity cannot exceed ordered quantity");

        // If no quantityInBaseUnit provided, assume quantity is already in base unit
        if (quantityInBaseUnit == 0)
            quantityInBaseUnit = quantity;

        ReceivedQuantity = quantity;
        ReceivedQuantityInBaseUnit = quantityInBaseUnit;
    }

    /// <summary>
    /// Updates the line item properties (for upsert operations)
    /// </summary>
    public void Update(int quantity, decimal unitPrice, Guid? unitId, int quantityInBaseUnit, string description = "")
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        if (quantityInBaseUnit <= 0)
            quantityInBaseUnit = quantity;

        Quantity = quantity;
        UnitPrice = unitPrice;
        UnitId = unitId;
        QuantityInBaseUnit = quantityInBaseUnit;
        Description = description?.Trim() ?? string.Empty;
    }

    public bool IsFullyReceived => ReceivedQuantity >= Quantity;
}
