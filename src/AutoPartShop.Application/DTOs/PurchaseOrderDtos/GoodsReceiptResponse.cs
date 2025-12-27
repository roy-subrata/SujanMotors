namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class GoodsReceiptResponse
{
    public Guid Id { get; set; }
    public string GRNNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime ReceivedDate { get; set; }
    public string Status { get; set; } = string.Empty; // PENDING_VERIFICATION, VERIFIED, CLOSED, REJECTED
    public int TotalItemsReceived { get; set; }
    public int DiscrepancyCount { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public DateTime? VerificationDate { get; set; }
    public List<GoodsReceiptLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    // Delivery Information
    public DateTime? DeliveryDate { get; set; }
    public string DeliveryReference { get; set; } = string.Empty;
    public string CarrierName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DeliveryNotes { get; set; } = string.Empty;
}

public class GoodsReceiptLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public int ReceivedQuantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool HasDiscrepancy { get; set; }

    // Cost Information
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = "INR";
    public decimal TotalCost { get; set; }  // ReceivedQuantity * UnitCost
    public int AcceptedQuantity { get; set; }
    public decimal AcceptedTotalCost { get; set; }  // AcceptedQuantity * UnitCost
    public Guid? UnitId { get; set; }
}
