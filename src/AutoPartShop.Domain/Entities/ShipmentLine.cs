namespace AutoPartShop.Domain.Entities;

public class ShipmentLine : AuditableEntity
{
    public Guid ShipmentId { get; private set; }
    public Guid SalesOrderLineId { get; private set; }
    public Guid PartId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public int Quantity { get; private set; }
    public int QuantityInBaseUnit { get; private set; }

    // Navigation
    public Shipment? Shipment { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }
    public Product? Part { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    private ShipmentLine() { }

    public static ShipmentLine Create(
        Guid shipmentId,
        Guid salesOrderLineId,
        Guid partId,
        int quantity,
        int quantityInBaseUnit = 0,
        Guid? productVariantId = null)
    {
        if (shipmentId == Guid.Empty)
            throw new ArgumentException("ShipmentId cannot be empty", nameof(shipmentId));

        if (salesOrderLineId == Guid.Empty)
            throw new ArgumentException("SalesOrderLineId cannot be empty", nameof(salesOrderLineId));

        if (partId == Guid.Empty)
            throw new ArgumentException("PartId cannot be empty", nameof(partId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than 0", nameof(quantity));

        return new ShipmentLine
        {
            ShipmentId = shipmentId,
            SalesOrderLineId = salesOrderLineId,
            PartId = partId,
            ProductVariantId = productVariantId,
            Quantity = quantity,
            QuantityInBaseUnit = quantityInBaseUnit > 0 ? quantityInBaseUnit : quantity
        };
    }
}
