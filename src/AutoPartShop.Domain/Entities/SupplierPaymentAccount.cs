namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a payment account/method for a supplier (where to send money)
/// A supplier can have multiple payment accounts (bank, mobile banking, etc.)
/// </summary>
public class SupplierPaymentAccount : AuditableEntity
{
    public Guid SupplierId { get; private set; }
    public Supplier Supplier { get; private set; } = null!;

    public string AccountType { get; private set; } = string.Empty;  // BANK_TRANSFER, MOBILE_BANKING, CASH, CHECK, OTHER
    public string AccountName { get; private set; } = string.Empty;  // Friendly name: "BOC Savings", "bKash Personal"
    public bool IsDefault { get; private set; } = false;
    public bool IsActive { get; private set; } = true;

    // Bank Transfer fields
    public string BankName { get; private set; } = string.Empty;
    public string BankAccountNumber { get; private set; } = string.Empty;
    public string BankBranchName { get; private set; } = string.Empty;
    public string BankBranchCode { get; private set; } = string.Empty;
    public string BeneficiaryName { get; private set; } = string.Empty;
    public string BankIBAN { get; private set; } = string.Empty;
    public string BankSWIFT { get; private set; } = string.Empty;

    // Mobile Banking fields (bKash, Nagad, eZ Cash, FriMi, etc.)
    public string MobileNumber { get; private set; } = string.Empty;
    public string MobileAccountHolderName { get; private set; } = string.Empty;
    public string MobileProvider { get; private set; } = string.Empty;  // bKash, Nagad, eZ Cash, etc.

    public string Notes { get; private set; } = string.Empty;

    private SupplierPaymentAccount() { }

    public static SupplierPaymentAccount Create(
        Guid supplierId,
        string accountType,
        string accountName,
        bool isDefault = false)
    {
        if (supplierId == Guid.Empty)
            throw new ArgumentException("SupplierId cannot be empty", nameof(supplierId));

        if (string.IsNullOrWhiteSpace(accountType))
            throw new ArgumentException("AccountType cannot be empty", nameof(accountType));

        var validTypes = new[] { "BANK_TRANSFER", "MOBILE_BANKING", "CASH", "CHECK", "OTHER" };
        if (!validTypes.Contains(accountType.ToUpper()))
            throw new ArgumentException($"AccountType must be one of: {string.Join(", ", validTypes)}", nameof(accountType));

        return new SupplierPaymentAccount
        {
            SupplierId = supplierId,
            AccountType = accountType.ToUpper(),
            AccountName = accountName?.Trim() ?? string.Empty,
            IsDefault = isDefault,
            IsActive = true
        };
    }

    public void SetBankDetails(
        string bankName,
        string accountNumber,
        string beneficiaryName,
        string branchName = "",
        string branchCode = "")
    {
        BankName = bankName?.Trim() ?? string.Empty;
        BankAccountNumber = accountNumber?.Trim() ?? string.Empty;
        BeneficiaryName = beneficiaryName?.Trim() ?? string.Empty;
        BankBranchName = branchName?.Trim() ?? string.Empty;
        BankBranchCode = branchCode?.Trim() ?? string.Empty;
    }

    public void SetInternationalDetails(string iban, string swift)
    {
        BankIBAN = iban?.Trim() ?? string.Empty;
        BankSWIFT = swift?.Trim() ?? string.Empty;
    }

    public void SetMobileBankingDetails(
        string mobileNumber,
        string accountHolderName,
        string mobileProvider)
    {
        MobileNumber = mobileNumber?.Trim() ?? string.Empty;
        MobileAccountHolderName = accountHolderName?.Trim() ?? string.Empty;
        MobileProvider = mobileProvider?.Trim() ?? string.Empty;
    }

    public void SetAsDefault(bool isDefault) => IsDefault = isDefault;
    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateNotes(string notes) => Notes = notes?.Trim() ?? string.Empty;

    public void Update(string accountName, bool isActive)
    {
        AccountName = accountName?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    /// <summary>
    /// Get display text for this payment account
    /// </summary>
    public string GetDisplayText()
    {
        return AccountType switch
        {
            "BANK_TRANSFER" => $"{BankName} - {BankAccountNumber}",
            "MOBILE_BANKING" => $"{MobileProvider} - {MobileNumber}",
            _ => AccountName
        };
    }
}
