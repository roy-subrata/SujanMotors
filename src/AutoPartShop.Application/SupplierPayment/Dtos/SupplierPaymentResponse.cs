using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Application.SupplierPayment.Dtos;

public class SupplierPaymentResponse
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public Guid PaymentProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public Guid? SupplierPaymentAccountId { get; set; }
    public string SupplierPaymentAccountName { get; set; } = string.Empty;  // e.g., "BOC Savings - 12345678"
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaymentFee { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? ProcessedDate { get; set; }
    public string ProcessedBy { get; set; } = string.Empty;
    public DateTime? ConfirmedDate { get; set; }
    public string ConfirmedBy { get; set; } = string.Empty;
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public PaymentType PaymentType { get; set; } = PaymentType.REGULAR;
    public string Description { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public Guid? SourceAdvancePaymentId { get; set; }
}