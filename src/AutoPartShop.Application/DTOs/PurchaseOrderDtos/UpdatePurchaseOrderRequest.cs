namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

/// <summary>
/// Request DTO for updating a purchase order with line item upsert support
/// </summary>
public class UpdatePurchaseOrderRequest
{
    public Guid SupplierId { get; set; }
    public DateTime DeliveryDate { get; set; }
    public decimal TaxPercentage { get; set; } = 0;
    public decimal DiscountPercentage { get; set; } = 0;
    public string Notes { get; set; } = string.Empty;
    public string Currency { get; set; } = "BDT";
    public List<UpdatePurchaseOrderLineRequest> LineItems { get; set; } = new();
}

/// <summary>
/// Request DTO for purchase order line item with upsert support
/// - If Id is null or empty: creates new line item
/// - If Id is provided: updates existing line item
/// - Any existing line items not in this list will be removed
/// </summary>
public class UpdatePurchaseOrderLineRequest
{
    /// <summary>
    /// Line item ID. Null or empty for new items, existing ID for updates
    /// </summary>
    public Guid? Id { get; set; }
    public Guid PartId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? UnitId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
