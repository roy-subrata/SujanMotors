namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Line item in a sales order
/// </summary>
public class SalesOrderLine : AuditableEntity
{
    public Guid SalesOrderId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }  // The specific variant sold (SKU-level)
    public Guid? UnitId { get; private set; }  // Unit in which the part is sold
    public int Quantity { get; private set; }
    public int QuantityInBaseUnit { get; private set; }  // Converted to Part's base unit for stock
    public int ShippedQuantity { get; private set; } = 0;
    public int ShippedQuantityInBaseUnit { get; private set; } = 0;
    public decimal UnitPrice { get; private set; }
    public decimal Discount { get; private set; } = 0;  // Discount per unit
    public decimal TotalPrice => (Quantity * UnitPrice) - (Quantity * Discount);
    public string Description { get; private set; } = string.Empty;
    public int LineNumber { get; private set; }

    // Navigation properties
    public SalesOrder? SalesOrder { get; set; }
    public Product? Part { get; set; }
    public ProductVariant? ProductVariant { get; set; }
    public Unit? Unit { get; set; }

    // Computed properties
    public bool IsFullyShipped => ShippedQuantity >= Quantity;
    public int PendingQuantity => Quantity - ShippedQuantity;

    private SalesOrderLine() { }

    public static SalesOrderLine Create(Guid salesOrderId, Guid partId, int quantity,
        decimal unitPrice, int lineNumber, Guid? unitId = null, int quantityInBaseUnit = 0,
        decimal discount = 0, string description = "", Guid? productVariantId = null)
    {
        if (salesOrderId == Guid.Empty)
            throw new ArgumentException("SalesOrderId cannot be empty", nameof(salesOrderId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("UnitPrice must be greater than 0", nameof(unitPrice));

        if (discount < 0 || discount >= unitPrice)
            throw new ArgumentException("Discount must be non-negative and less than unit price", nameof(discount));

        if (lineNumber <= 0)
            throw new ArgumentException("LineNumber must be greater than 0", nameof(lineNumber));

        // If no quantityInBaseUnit provided, assume quantity is already in base unit
        if (quantityInBaseUnit == 0)
            quantityInBaseUnit = quantity;

        return new SalesOrderLine
        {
            SalesOrderId = salesOrderId,
            PartId = partId,
            ProductVariantId = productVariantId,
            UnitId = unitId,
            Quantity = quantity,
            QuantityInBaseUnit = quantityInBaseUnit,
            UnitPrice = unitPrice,
            LineNumber = lineNumber,
            Discount = discount,
            Description = description?.Trim() ?? string.Empty
        };
    }

    public void UpdateShippedQuantity(int quantity, int quantityInBaseUnit = 0)
    {
        if (quantity < 0)
            throw new ArgumentException("Shipped quantity cannot be negative", nameof(quantity));

        if (quantity > Quantity)
            throw new InvalidOperationException("Shipped quantity cannot exceed ordered quantity");

        // If no quantityInBaseUnit provided, assume quantity is already in base unit
        if (quantityInBaseUnit == 0)
            quantityInBaseUnit = quantity;

        ShippedQuantity = quantity;
        ShippedQuantityInBaseUnit = quantityInBaseUnit;
    }
}
