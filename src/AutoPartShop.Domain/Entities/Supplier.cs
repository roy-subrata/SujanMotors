namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a supplier of auto parts
/// </summary>
public class Supplier : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;  // Supplier code
    public string ContactPerson { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;  // e.g., "Net 30", "Net 60"
    public decimal CreditLimit { get; set; } = 0;
    public string BankName { get; private set; } = string.Empty;
    public string BankAccountNumber { get; private set; } = string.Empty;
    public string TaxID { get; private set; } = string.Empty;  // GST ID, VAT ID, etc.
    public bool IsActive { get; private set; } = true;
    public int Rating { get; private set; } = 5;  // 1-5 star rating

    // Navigation properties
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = [];

    private Supplier() { }

    public static Supplier Create(string name, string code, string contactPerson, string email, string phone,
        string address, string city, string state, string country, string postalCode)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code cannot be empty", nameof(code));

        if (name.Length > 150)
            throw new ArgumentException("Name cannot exceed 150 characters", nameof(name));

        if (code.Length > 30)
            throw new ArgumentException("Code cannot exceed 30 characters", nameof(code));

        return new Supplier
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpper(),
            ContactPerson = contactPerson?.Trim() ?? string.Empty,
            Email = email?.Trim() ?? string.Empty,
            Phone = phone?.Trim() ?? string.Empty,
            Address = address?.Trim() ?? string.Empty,
            City = city?.Trim() ?? string.Empty,
            State = state?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
            PostalCode = postalCode?.Trim() ?? string.Empty,
            IsActive = true,
            Rating = 5
        };
    }

    public void Update(string name, string contactPerson, string email, string phone,
        string address, string city, string state, string country, string postalCode,
        string paymentTerms, decimal creditLimit, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (creditLimit < 0)
            throw new ArgumentException("Credit limit cannot be negative", nameof(creditLimit));

        Name = name.Trim();
        ContactPerson = contactPerson?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        Phone = phone?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        State = state?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
        PostalCode = postalCode?.Trim() ?? string.Empty;
        PaymentTerms = paymentTerms?.Trim() ?? string.Empty;
        CreditLimit = creditLimit;
        IsActive = isActive;
    }

    public void SetBankDetails(string bankName, string accountNumber, string taxId)
    {
        BankName = bankName?.Trim() ?? string.Empty;
        BankAccountNumber = accountNumber?.Trim() ?? string.Empty;
        TaxID = taxId?.Trim() ?? string.Empty;
    }

    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
