namespace AutoPartShop.Application.DTOs.SupplierDtos;

public class CreateSupplierPaymentAccountRequest
{
    public Guid SupplierId { get; set; }
    public string AccountType { get; set; } = string.Empty;  // BANK_TRANSFER, MOBILE_BANKING, CASH, CHECK, OTHER
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;

    // Bank Transfer fields
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankBranchName { get; set; } = string.Empty;
    public string BankBranchCode { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;

    // Mobile Banking fields
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileAccountHolderName { get; set; } = string.Empty;
    public string MobileProvider { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public class UpdateSupplierPaymentAccountRequest
{
    public string AccountName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Bank Transfer fields
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankBranchName { get; set; } = string.Empty;
    public string BankBranchCode { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;

    // Mobile Banking fields
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileAccountHolderName { get; set; } = string.Empty;
    public string MobileProvider { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
}

public class SupplierPaymentAccountResponse
{
    public Guid Id { get; set; }
    public Guid SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }

    // Bank Transfer fields
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string BankBranchName { get; set; } = string.Empty;
    public string BankBranchCode { get; set; } = string.Empty;
    public string BeneficiaryName { get; set; } = string.Empty;
    public string BankIBAN { get; set; } = string.Empty;
    public string BankSWIFT { get; set; } = string.Empty;

    // Mobile Banking fields
    public string MobileNumber { get; set; } = string.Empty;
    public string MobileAccountHolderName { get; set; } = string.Empty;
    public string MobileProvider { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
    public string DisplayText { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
