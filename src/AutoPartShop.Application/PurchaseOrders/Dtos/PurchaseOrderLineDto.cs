namespace AutoPartShop.Application.PurchaseOrders;

public class PurchaseOrderLineDto
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string? PartName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? PartBaseUnitId { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitSymbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public int ReceivedQuantity { get; set; }
    public int ReceivedQuantityInBaseUnit { get; set; }
    // Qty sitting in not-yet-accepted (PENDING/VERIFIED) GRNs; reserved so it isn't offered again.
    public int InFlightReceivedQuantity { get; set; }
    public int RemainingQuantity => Quantity - ReceivedQuantity - InFlightReceivedQuantity;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal PartDefaultSellingPrice { get; set; }
    public decimal PartMinMarginPercent { get; set; }
}
