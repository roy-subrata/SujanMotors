namespace AutoPartShop.Application.DTOs.PaymentDtos;


public class CreateCustomerPaymentRequest
{
    public Guid CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentProviderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public DateTime? PaymentDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class UpdateCustomerPaymentRequest
{
    public string Status { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string AuthorizationCode { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class MarkAsCustomerPaymentAdvanceRequest
{
    public string Description { get; set; } = string.Empty;
}

public class MarkAsCustomerPaymentRegularRequest
{
    public string Description { get; set; } = string.Empty;
}

public class CustomerPaymentResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public Guid? PaymentProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
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
    public string Notes { get; set; } = string.Empty;
    public DateTime? SettledDate { get; set; }
    public string SettledBy { get; set; } = string.Empty;
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledDate { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public decimal RemainingAmount { get; set; }
    public Guid? SourceAdvancePaymentId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CustomerPaymentHistorySummary
{
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalPaid { get; set; }
    public decimal TotalFees { get; set; }
    public int CompletedPayments { get; set; }
    public int PendingPayments { get; set; }
    public int FailedPayments { get; set; }
    public DateTime? LastPaymentDate { get; set; }
    public decimal LastPaymentAmount { get; set; }

    // Invoice and Outstanding Balance Information
    public decimal TotalInvoiceAmount { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal AmountDue { get; set; }
    public int TotalInvoices { get; set; }
    public int UnpaidInvoices { get; set; }
    public int OverdueInvoices { get; set; }

    public List<PaymentHistoryItem> PaymentHistory { get; set; } = new();

    // Advance/Available balance (unlinked payments)
    public decimal AvailableAdvance { get; set; }
}

public class AvailableCustomerAdvancePayment
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal UsedAmount => Amount - RemainingAmount;
    public DateTime PaymentDate { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ApplyCustomerAdvanceCreditRequest
{
    public Guid InvoiceId { get; set; }
    public Guid SourceAdvancePaymentId { get; set; }
    public decimal Amount { get; set; }
    public string? Description { get; set; }
}

public class ApplyCustomerAdvanceCreditResponse
{
    public Guid PaymentId { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal AmountApplied { get; set; }
    public decimal RemainingAdvanceBalance { get; set; }
    public string Message { get; set; } = string.Empty;
}
