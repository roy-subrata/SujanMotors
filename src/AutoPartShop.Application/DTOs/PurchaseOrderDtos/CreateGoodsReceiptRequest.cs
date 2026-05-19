namespace AutoPartShop.Application.DTOs.PurchaseOrderDtos;

public class CreateGoodsReceiptRequest
{
    public Guid PurchaseOrderId { get; set; }
    public Guid WarehouseId { get; set; }
    public DateTime ReceivedDate { get; set; }
    public List<CreateGoodsReceiptLineRequest> Lines { get; set; } = new();

    // Supplier Invoice
    public string? SupplierInvoiceNumber { get; set; }   // Bill / invoice number from supplier
    public DateTime? SupplierInvoiceDate { get; set; }   // Date on the supplier invoice
    public bool InvoiceNotProvided { get; set; } = false; // True = no invoice given (cash / walk-in)

    // Delivery Information
    public DateTime? DeliveryDate { get; set; }
    public string DeliveryReference { get; set; } = string.Empty;  // Waybill, shipment ID
    public string CarrierName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
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

    // Batch / lot identification from supplier documentation
    public string? BatchNumber { get; set; }      // Supplier's batch / lot number
    public DateTime? ExpiryDate { get; set; }     // Expiry date for this lot (grocery, pharmacy, etc.)

    // Lot-level selling price & warranty (overrides Part master defaults)
    public decimal? SellingPrice { get; set; }
    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}
