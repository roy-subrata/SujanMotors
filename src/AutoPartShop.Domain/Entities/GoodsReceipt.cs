namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Goods Receipt Note (GRN) - records receipt of goods from a purchase order
/// </summary>
public class GoodsReceipt : AuditableEntity
{
    public string GRNNumber { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime ReceiptDate { get; private set; }
    public string Status { get; private set; } = "PENDING";  // PENDING, VERIFIED, ACCEPTED, REJECTED
    public string Notes { get; private set; } = string.Empty;
    public int TotalItemsReceived { get; private set; } = 0;
    public int DiscrepancyCount { get; private set; } = 0;
    public string VerifiedBy { get; private set; } = string.Empty;
    public DateTime? VerificationDate { get; private set; }

    // Delivery Information
    public DateTime? DeliveryDate { get; private set; }  // Actual delivery/arrival date
    public string DeliveryReference { get; private set; } = string.Empty;  // Waybill, shipment ID, etc.
    public string CarrierName { get; private set; } = string.Empty;  // Transport/courier company
    public string DriverName { get; private set; } = string.Empty;  // Driver or contact person
    public string DeliveryNotes { get; private set; } = string.Empty;  // Delivery-related notes

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<GoodsReceiptLine> LineItems { get; set; } = new List<GoodsReceiptLine>();

    private GoodsReceipt() { }

    public static GoodsReceipt Create(string grnNumber, Guid purchaseOrderId, Guid warehouseId,
        DateTime? receiptDate = null, string notes = "")
    {
        if (string.IsNullOrWhiteSpace(grnNumber))
            throw new ArgumentException("GRNNumber cannot be empty", nameof(grnNumber));

        if (purchaseOrderId == Guid.Empty)
            throw new ArgumentException("PurchaseOrderId cannot be empty", nameof(purchaseOrderId));

        if (warehouseId == Guid.Empty)
            throw new ArgumentException("WarehouseId cannot be empty", nameof(warehouseId));

        return new GoodsReceipt
        {
            GRNNumber = grnNumber.Trim().ToUpper(),
            PurchaseOrderId = purchaseOrderId,
            WarehouseId = warehouseId,
            ReceiptDate = receiptDate ?? DateTime.UtcNow,
            Status = "PENDING",
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void Verify(string verifiedBy)
    {
        if (string.IsNullOrWhiteSpace(verifiedBy))
            throw new ArgumentException("VerifiedBy cannot be empty", nameof(verifiedBy));

        if (Status != "PENDING")
            throw new InvalidOperationException("Only pending GRNs can be verified");

        Status = "VERIFIED";
        VerifiedBy = verifiedBy.Trim();
        VerificationDate = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (Status != "VERIFIED")
            throw new InvalidOperationException("Only verified GRNs can be accepted");

        Status = "ACCEPTED";
    }

    public void Reject(string reason = "")
    {
        if (Status == "ACCEPTED")
            throw new InvalidOperationException("Cannot reject an accepted GRN");

        if (Status == "REJECTED")
            throw new InvalidOperationException("GRN is already rejected");

        Status = "REJECTED";
        Notes = reason?.Trim() ?? string.Empty;
    }

    public void UpdateCounts()
    {
        TotalItemsReceived = LineItems.Sum(l => l.ReceivedQuantity);
        DiscrepancyCount = LineItems.Count(l => l.HasDiscrepancy);
    }

    public void AddNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void SetDeliveryInformation(DateTime? deliveryDate, string deliveryReference = "",
        string carrierName = "", string driverName = "", string deliveryNotes = "")
    {
        DeliveryDate = deliveryDate;
        DeliveryReference = deliveryReference?.Trim() ?? string.Empty;
        CarrierName = carrierName?.Trim() ?? string.Empty;
        DriverName = driverName?.Trim() ?? string.Empty;
        DeliveryNotes = deliveryNotes?.Trim() ?? string.Empty;
    }
}
