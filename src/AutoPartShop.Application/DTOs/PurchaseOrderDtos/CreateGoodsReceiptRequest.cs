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

    // When true, accepting this receipt also raises a draft Purchase Return for the damaged/wrong
    // lines (spec: "Post & Create Return"). Used by the accept endpoint.
    public bool CreateReturn { get; set; } = false;
}

public class CreateGoodsReceiptLineRequest
{
    public Guid PartId { get; set; }
    // Identifies the exact PO line this receipt is for. Required to disambiguate when a PO has
    // multiple lines for the same PartId (e.g. different variants). Falls back to PartId match when absent.
    public Guid? PurchaseOrderLineId { get; set; }
    public int ReceivedQuantity { get; set; }
    public int DamagedQuantity { get; set; } = 0;  // Damaged units -> Damaged stock (+ optional Purchase Return)
    public int WrongQuantity { get; set; } = 0;    // Wrong/incorrect units -> Quarantine stock (+ optional Purchase Return)
    public string RejectionReason { get; set; } = string.Empty;  // Why units were damaged/wrong
    public string Condition { get; set; } = "GOOD"; // GOOD, ACCEPTABLE, DAMAGED, DEFECTIVE, MISSING
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
