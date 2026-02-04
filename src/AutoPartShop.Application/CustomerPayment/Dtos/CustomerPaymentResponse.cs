namespace AutoPartShop.Application.CustomerPayment.Dtos;

public class CustomerPaymentResponse
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public Guid? InvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid? PaymentProviderId { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string TransactionNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PaymentFee { get; set; }
    public decimal NetAmount { get; set; }
    public string Currency { get; set; } = "USD";
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
    public string SourceAdvanceTransactionNumber { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePaymentProviderRequest
{
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingNumber { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;

    // Mobile Banking fields (bKash, Nagad, eZ Cash, etc.)
    public string MobileNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AgentNumber { get; set; } = string.Empty;

    public string TransactionFeeType { get; set; } = "FIXED";
    public decimal TransactionFeeAmount { get; set; } = 0;
    public decimal MinimumAmount { get; set; } = 0;
    public decimal MaximumAmount { get; set; } = 0;
    public int SettlementDays { get; set; } = 1;
    public string SupportedCurrencies { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class UpdatePaymentProviderRequest
{
    public string ProviderName { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingNumber { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;

    // Mobile Banking fields
    public string MobileNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AgentNumber { get; set; } = string.Empty;

    public string TransactionFeeType { get; set; } = string.Empty;
    public decimal TransactionFeeAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public int SettlementDays { get; set; }
    public string SupportedCurrencies { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class PaymentProviderResponse
{
    public Guid Id { get; set; }
    public string ProviderName { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankRoutingNumber { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;

    // Mobile Banking fields
    public string MobileNumber { get; set; } = string.Empty;
    public string AccountHolderName { get; set; } = string.Empty;
    public string AgentNumber { get; set; } = string.Empty;

    public string TransactionFeeType { get; set; } = string.Empty;
    public decimal TransactionFeeAmount { get; set; }
    public decimal MinimumAmount { get; set; }
    public decimal MaximumAmount { get; set; }
    public int SettlementDays { get; set; }
    public string SupportedCurrencies { get; set; } = string.Empty;
    public string WebhookUrl { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public DateTime? LastTestedDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
