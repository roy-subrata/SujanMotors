using AutoPartShop.Domain.Enums;

namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Goods Receipt Note (GRN) - records receipt of goods from a purchase order
/// </summary>
public class GoodsReceipt : AuditableEntity
{
    /// <summary>Optimistic-concurrency token (SQL Server rowversion).</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public string GRNNumber { get; private set; } = string.Empty;
    public Guid PurchaseOrderId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public DateTime ReceiptDate { get; private set; }
    public GoodsReceiptStatus Status { get; private set; } = GoodsReceiptStatus.PENDING;
    public string Notes { get; private set; } = string.Empty;
    public int TotalItemsReceived { get; private set; } = 0;
    public int DiscrepancyCount { get; private set; } = 0;
    public string VerifiedBy { get; private set; } = string.Empty;
    public DateTime? VerificationDate { get; private set; }

    // Supplier Invoice
    public string SupplierInvoiceNumber { get; private set; } = string.Empty;  // Supplier's bill/invoice ref
    public DateTime? SupplierInvoiceDate { get; private set; }  // Date printed on the supplier invoice
    public bool InvoiceNotProvided { get; private set; } = false;  // True = walk-in / no invoice given

    // Delivery Information
    public DateTime? DeliveryDate { get; private set; }
    public string DeliveryReference { get; private set; } = string.Empty;  // Waybill, shipment ID
    public string CarrierName { get; private set; } = string.Empty;
    public string DriverName { get; private set; } = string.Empty;
    public string DeliveryNotes { get; private set; } = string.Empty;

    // Navigation properties
    public PurchaseOrder? PurchaseOrder { get; set; }
    public Warehouse? Warehouse { get; set; }
    public ICollection<GoodsReceiptLine> LineItems { get; set; } = new List<GoodsReceiptLine>();

    private GoodsReceipt() { }

    /// <summary>
    /// Assigns the sequential GRN number once the receipt has passed validation. The controller
    /// builds the receipt and its lines first and only then allocates a number, so a rejected
    /// receipt does not consume one — GRN numbers are expected to be gapless.
    /// </summary>
    public void AssignGRNNumber(string grnNumber)
    {
        if (string.IsNullOrWhiteSpace(grnNumber))
            throw new ArgumentException("GRNNumber cannot be empty", nameof(grnNumber));

        GRNNumber = grnNumber.Trim().ToUpper();
    }

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
            Status = GoodsReceiptStatus.PENDING,
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public void Verify(string verifiedBy)
    {
        if (string.IsNullOrWhiteSpace(verifiedBy))
            throw new ArgumentException("VerifiedBy cannot be empty", nameof(verifiedBy));

        if (Status != GoodsReceiptStatus.PENDING)
            throw new InvalidOperationException("Only pending GRNs can be verified");

        Status = GoodsReceiptStatus.VERIFIED;
        VerifiedBy = verifiedBy.Trim();
        VerificationDate = DateTime.UtcNow;
    }

    public void Accept()
    {
        if (Status != GoodsReceiptStatus.VERIFIED)
            throw new InvalidOperationException("Only verified GRNs can be accepted");

        Status = GoodsReceiptStatus.ACCEPTED;
    }

    public void Reject(string reason = "")
    {
        if (Status == GoodsReceiptStatus.ACCEPTED)
            throw new InvalidOperationException("Cannot reject an accepted GRN");

        if (Status == GoodsReceiptStatus.REJECTED)
            throw new InvalidOperationException("GRN is already rejected");

        Status = GoodsReceiptStatus.REJECTED;
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

    public void SetInvoiceInformation(string invoiceNumber, DateTime? invoiceDate, bool invoiceNotProvided = false)
    {
        InvoiceNotProvided = invoiceNotProvided;
        SupplierInvoiceNumber = invoiceNotProvided ? string.Empty : (invoiceNumber?.Trim() ?? string.Empty);
        SupplierInvoiceDate = invoiceNotProvided ? null : invoiceDate;
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
