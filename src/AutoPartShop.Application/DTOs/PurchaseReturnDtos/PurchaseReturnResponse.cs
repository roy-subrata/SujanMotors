namespace AutoPartShop.Application.DTOs.PurchaseReturnDtos;

public class PurchaseReturnResponse
{
    public Guid Id { get; set; }
    public string ReturnNumber { get; set; } = string.Empty;
    public Guid PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? SupplierCode { get; set; }
    public DateTime ReturnDate { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
    public decimal CreditNoteAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTime? ApprovedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public string ReceivedBy { get; set; } = string.Empty;

    // Settlement tracking fields
    public string SettlementStatus { get; set; } = string.Empty;
    public decimal SettledAmount { get; set; }
    public DateTime? SettledDate { get; set; }
    public string SettlementMethod { get; set; } = string.Empty;
    public string SettlementNotes { get; set; } = string.Empty;
    public bool IsSettled { get; set; }

    public List<PurchaseReturnLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseReturnLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public Guid? StockLotId { get; set; }
    public string? LotNumber { get; set; }  // Lot number for display
    public int Quantity { get; set; }
    public int RejectedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// DTO for available stock lots that can be selected for return
/// </summary>
public class AvailableLotForReturnDto
{
    public Guid LotId { get; set; }
    public string LotNumber { get; set; } = string.Empty;
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string PartSku { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public decimal CostPrice { get; set; }
    public DateTime ReceivingDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsFromSameSupplier { get; set; }  // True if lot is from the return's supplier
}
