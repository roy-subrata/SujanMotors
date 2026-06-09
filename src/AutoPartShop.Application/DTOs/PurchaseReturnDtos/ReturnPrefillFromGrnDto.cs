namespace AutoPartShop.Application.DTOs.PurchaseReturnDtos;

/// <summary>
/// Draft payload used to pre-fill a new Purchase Return from an already-accepted Goods Receipt.
/// One line per GRN line that still has un-returned damaged or wrong units.
/// </summary>
public class ReturnPrefillFromGrnDto
{
    public Guid GoodsReceiptId { get; set; }
    public string GrnNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string Reason { get; set; } = string.Empty;  // Suggested header reason (DAMAGED / WRONG_ITEM)
    public List<ReturnPrefillLineDto> Lines { get; set; } = new();
}

public class ReturnPrefillLineDto
{
    public Guid PurchaseOrderLineId { get; set; }
    public Guid PartId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public Guid? StockLotId { get; set; }  // The DAMAGED/QUARANTINE lot created by this GRN line
    public string? LotNumber { get; set; }
    public string Bucket { get; set; } = string.Empty;  // DAMAGED or QUARANTINE (informational; bucket is derived from the lot)
    public int Quantity { get; set; }  // Remaining returnable units (bucket qty minus already returned)
    public decimal UnitPrice { get; set; }
    public string Condition { get; set; } = string.Empty;  // Suggested PurchaseReturnLine condition
    public string Notes { get; set; } = string.Empty;
}
