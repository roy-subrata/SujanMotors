namespace AutoPartShop.Application.PurchaseOrders;

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string? PartName { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitSymbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public int ReceivedQuantity { get; set; }
    public int ReceivedQuantityInBaseUnit { get; set; }
    public int RemainingQuantity => Quantity - ReceivedQuantity;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
}
