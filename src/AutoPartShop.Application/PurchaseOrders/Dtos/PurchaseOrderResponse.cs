using AutoPartShop.Application.DTOs.PurchaseOrderDtos;
using AutoPartShop.Domain.Enums;

public class PurchaseOrderResponse
{
    public Guid Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime DeliveryDate { get; set; }
    public PurchaseOrderPaymentStatus PaymentStatus { get; set; }
    public PurchaseOrderStatus Status { get; set; }
    public decimal SubTotal { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TaxPercentage { get; set; }
    public decimal Discount { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountAmount { get; set; }
    public string DiscountType { get; set; } = "TOTAL";
    public decimal GrandTotal { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public decimal OutstandingAmount { get; set; }

    /// <summary>Total value of supplier credit notes applied against this order.</summary>
    public decimal CreditAppliedAmount { get; set; }
    public bool IsOverdue { get; set; }
    public string Notes { get; set; } = string.Empty;
    public List<PurchaseOrderLineResponse> Lines { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class PurchaseOrderLineResponse
{
    public Guid Id { get; set; }
    public Guid PartId { get; set; }
    public string PartName { get; set; } = string.Empty;
    public string? PartLocalName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantCode { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? PartBaseUnitId { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public string UnitSymbol { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int QuantityInBaseUnit { get; set; }
    public int ReceivedQuantity { get; set; }
    public int ReceivedQuantityInBaseUnit { get; set; }
    // Qty sitting in not-yet-accepted (PENDING/VERIFIED) GRNs; reserved so it isn't offered again.
    public int InFlightReceivedQuantity { get; set; }
    public int RemainingQuantity => Quantity - ReceivedQuantity - InFlightReceivedQuantity;
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal PartDefaultSellingPrice { get; set; }
    public decimal PartMinMarginPercent { get; set; }
}

