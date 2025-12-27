namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class CreateGoodsReceiptRequest
{
    public Guid PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public List<CreateGoodsReceiptLineRequest> Lines { get; set; } = new();

    // Delivery Information
    public DateTime? DeliveryDate { get; set; }
    public string DeliveryReference { get; set; } = string.Empty;  // Waybill, shipment ID
    public string CarrierName { get; set; } = string.Empty;  // Transport company
    public string DriverName { get; set; } = string.Empty;  // Driver/contact
    public string DeliveryNotes { get; set; } = string.Empty;
}

public class CreateGoodsReceiptLineRequest
{
    public Guid PartId { get; set; }
    public int ReceivedQuantity { get; set; }
    public string Condition { get; set; } = "GOOD"; // GOOD, DAMAGED, MISSING
    public string Notes { get; set; } = string.Empty;

    // Cost Information - actual cost paid
    public decimal UnitCost { get; set; } = 0;  // Cost per unit as received
    public string Currency { get; set; } = "INR";
    public Guid? UnitId { get; set; }  // Unit of measurement
}
