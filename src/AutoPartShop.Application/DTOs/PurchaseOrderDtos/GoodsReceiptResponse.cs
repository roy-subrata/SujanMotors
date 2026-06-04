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
    public string Status { get; set; } = string.Empty;
    public int TotalItemsReceived { get; set; }
    public int DiscrepancyCount { get; set; }
    public string VerifiedBy { get; set; } = string.Empty;
    public DateTime? VerificationDate { get; set; }
    public List<GoodsReceiptLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }

    // Supplier Invoice
    public string SupplierInvoiceNumber { get; set; } = string.Empty;
    public DateTime? SupplierInvoiceDate { get; set; }
    public bool InvoiceNotProvided { get; set; }

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
    public Guid PurchaseOrderLineId { get; set; }  // The PO line this receipt line is for (disambiguates same-part variant lines)
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSKU { get; set; } = string.Empty;
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RejectedQuantity { get; set; }
    public int AcceptedQuantity { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool HasDiscrepancy { get; set; }

    // Batch / lot identification
    public string? BatchNumber { get; set; }   // Supplier's batch / lot number
    public DateTime? ExpiryDate { get; set; }  // Per-lot expiry (grocery, pharmacy, etc.)

    // Cost Information
    public decimal UnitCost { get; set; }
    public string Currency { get; set; } = "INR";
    public Guid? UnitId { get; set; }
    public decimal TotalCost { get; set; }
    public decimal AcceptedTotalCost { get; set; }

    // Lot-level selling price & warranty overrides
    public decimal? SellingPrice { get; set; }
    public bool? HasWarranty { get; set; }
    public int? WarrantyPeriodMonths { get; set; }
    public string? WarrantyType { get; set; }
    public string? WarrantyTerms { get; set; }
}
