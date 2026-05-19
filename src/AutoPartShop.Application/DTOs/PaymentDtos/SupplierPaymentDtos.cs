using AutoPartShop.Domain.Entities;

namespace AutoPartShop.Application.DTOs.PaymentDtos;

public class CreateSupplierPaymentRequest
{
    public Guid SupplierId { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public Guid PaymentProviderId { get; set; }
    public Guid? SupplierPaymentAccountId { get; set; }  // Supplier's payment account (destination)
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; } = PaymentType.REGULAR;
    public string Description { get; set; } = string.Empty;  // For advance payments
}

public class MarkAsPaymentAsAdvanceRequest
{
    public string Description { get; set; } = string.Empty;
}

public class MarkAsPaymentAsRegularRequest
{
    public string Description { get; set; } = string.Empty;
}

public class ApplyAdvanceCreditRequest
{
    public Guid PurchaseOrderId { get; set; }
    public Guid SourceAdvancePaymentId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
}


public class UpdateSupplierPaymentRequest
{
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}


public class SupplierPaymentHistorySummary
{
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public decimal CreditLimit { get; set; }
    public decimal CreditUtilization { get; set; }  // Percentage (0-100)
    public decimal TotalPaid { get; set; }
    public decimal TotalAdvanceAmount { get; set; }
    public decimal TotalRefunds { get; set; }  // Total refunds from purchase returns
    public decimal TotalDue { get; set; }
    public decimal PaymentBalance { get; set; }  // TotalDue - TotalAdvance - TotalPaid
    public decimal TotalFees { get; set; }
    public int OutstandingInvoiceCount { get; set; }
    public int CompletedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPayments { get; set; }
    public int ProcessingPayments { get; set; }
    public int CancelledPayments { get; set; }
    public int ReturnedPayments { get; set; }  // Count of refund payments
    public DateTime? LastPaymentDate { get; set; }
    public decimal LastPaymentAmount { get; set; }
    public PaymentStatusBreakdown? StatusBreakdown { get; set; }
    public List<PaymentHistoryItem> PaymentHistory { get; set; } = new();
}

public class PaymentStatusBreakdown
{
    public int Pending { get; set; }
    public int Completed { get; set; }
    public int Failed { get; set; }
    public int Processing { get; set; }
    public int Cancelled { get; set; }
    public int Reconciled { get; set; }
}

public class PaymentHistoryItem
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public PaymentType PaymentType { get; set; } = PaymentType.REGULAR;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public Guid? SourceAdvancePaymentId { get; set; }
    public string? SourceAdvanceTransactionNumber { get; set; }
    public Guid? PurchaseOrderId { get; set; }
    public string? PurchaseOrderNumber { get; set; }
    public Guid? GoodsReceiptId { get; set; }
    public string? GoodsReceiptNumber { get; set; }
}

public class AvailableAdvancePayment
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal UsedAmount => Amount - RemainingAmount;
    public DateTime PaymentDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ApplyAdvanceCreditResponse
{
    public Guid PaymentId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal AmountApplied { get; set; }
    public decimal RemainingAdvanceBalance { get; set; }
    public string Message { get; set; } = string.Empty;
}
