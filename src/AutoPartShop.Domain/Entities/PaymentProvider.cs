namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a payment provider/method configuration
/// </summary>
public class PaymentProvider : AuditableEntity
{
    public string ProviderName { get; private set; } = string.Empty;  // PayPal, Stripe, Bank Transfer, Cash, Check, etc.
    public string ProviderType { get; private set; } = string.Empty;  // ONLINE_GATEWAY, BANK_TRANSFER, CASH, CHECK, CRYPTO, MOBILE_BANKING, etc.
    public string Status { get; private set; } = "ACTIVE";  // ACTIVE, INACTIVE, SUSPENDED
    public string ApiKey { get; private set; } = string.Empty;  // Encrypted API key if needed
    public string MerchantId { get; private set; } = string.Empty;  // Merchant or account ID
    public string BankName { get; private set; } = string.Empty;  // For bank transfers
    public string BankAccountNumber { get; private set; } = string.Empty;  // Bank account (partial for security)
    public string BankRoutingNumber { get; private set; } = string.Empty;  // Bank routing number
    public string BankIBAN { get; private set; } = string.Empty;  // IBAN for international transfers
    public string BankSWIFT { get; private set; } = string.Empty;  // SWIFT code
    public string BeneficiaryName { get; private set; } = string.Empty;  // Account holder name

    // Mobile Banking fields (bKash, Nagad, eZ Cash, FriMi, etc.)
    public string MobileNumber { get; private set; } = string.Empty;  // Mobile wallet number
    public string AccountHolderName { get; private set; } = string.Empty;  // Mobile wallet account holder
    public string AgentNumber { get; private set; } = string.Empty;  // Agent/Merchant number for mobile banking
    public string TransactionFeeType { get; private set; } = "FIXED";  // FIXED, PERCENTAGE, TIERED
    public decimal TransactionFeeAmount { get; private set; } = 0;  // Fixed fee or percentage
    public decimal MinimumAmount { get; private set; } = 0;  // Minimum transaction amount
    public decimal MaximumAmount { get; private set; } = 0;  // Maximum transaction amount (0 = unlimited)
    public int SettlementDays { get; private set; } = 1;  // Days to settle payment
    public string SupportedCurrencies { get; private set; } = string.Empty;  // Comma-separated: USD,EUR,GBP
    public string WebhookUrl { get; private set; } = string.Empty;  // Webhook endpoint for payment notifications
    public string Notes { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; } = false;
    public DateTime? LastTestedDate { get; private set; }

    private PaymentProvider() { }

    public static PaymentProvider Create(string providerName, string providerType, string status = "ACTIVE")
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("ProviderName cannot be empty", nameof(providerName));

        if (string.IsNullOrWhiteSpace(providerType))
            throw new ArgumentException("ProviderType cannot be empty", nameof(providerType));

        var validTypes = new[] { "ONLINE_GATEWAY", "BANK_TRANSFER", "CASH", "CHECK", "CRYPTO", "MOBILE_BANKING", "OTHER" };
        if (!validTypes.Contains(providerType.ToUpper()))
            throw new ArgumentException($"ProviderType must be one of: {string.Join(", ", validTypes)}", nameof(providerType));

        return new PaymentProvider
        {
            ProviderName = providerName.Trim(),
            ProviderType = providerType.ToUpper(),
            Status = status
        };
    }

    public void SetBankDetails(string bankName, string accountNumber, string routingNumber, string beneficiaryName)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        BankAccountNumber = accountNumber?.Trim() ?? string.Empty;
        BankRoutingNumber = routingNumber?.Trim() ?? string.Empty;
        BeneficiaryName = beneficiaryName?.Trim() ?? string.Empty;
    }

    public void SetInternationalDetails(string iban, string swift)
    {
        BankIBAN = iban?.Trim() ?? string.Empty;
        BankSWIFT = swift?.Trim() ?? string.Empty;
    }

    public void SetMobileBankingDetails(string mobileNumber, string accountHolderName, string agentNumber)
    {
        MobileNumber = mobileNumber?.Trim() ?? string.Empty;
        AccountHolderName = accountHolderName?.Trim() ?? string.Empty;
        AgentNumber = agentNumber?.Trim() ?? string.Empty;
    }

    public void SetTransactionFees(string feeType, decimal feeAmount, decimal minimumAmount = 0, decimal maximumAmount = 0)
    {
        if (feeAmount < 0)
            throw new ArgumentException("Fee amount cannot be negative", nameof(feeAmount));

        TransactionFeeType = feeType.ToUpper();
        TransactionFeeAmount = feeAmount;
        MinimumAmount = minimumAmount;
        MaximumAmount = maximumAmount;
    }

    public void SetCurrencies(string currencies)
    {
        SupportedCurrencies = currencies?.Trim() ?? string.Empty;
    }

    public void SetApiKey(string apiKey)
    {
        ApiKey = apiKey?.Trim() ?? string.Empty;
    }

    public void SetMerchantId(string merchantId)
    {
        MerchantId = merchantId?.Trim() ?? string.Empty;
    }

    public void SetWebhookUrl(string webhookUrl)
    {
        WebhookUrl = webhookUrl?.Trim() ?? string.Empty;
    }

    public void SetAsDefault(bool isDefault)
    {
        IsDefault = isDefault;
    }

    public void Activate()
    {
        Status = "ACTIVE";
    }

    public void Deactivate()
    {
        Status = "INACTIVE";
    }

    public void TestConnection()
    {
        LastTestedDate = DateTime.UtcNow;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public decimal CalculateFee(decimal amount)
    {
        if (amount < MinimumAmount)
            return 0;

        if (TransactionFeeType == "PERCENTAGE")
            return (amount * TransactionFeeAmount) / 100;

        return TransactionFeeAmount;
    }
}
