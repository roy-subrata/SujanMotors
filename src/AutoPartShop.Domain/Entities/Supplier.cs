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
    public string PaymentTerms { get; private set; } = "NET30";
    public decimal CreditLimit { get; private set; } = 0;
    public decimal CurrentBalance { get; private set; } = 0;  // Outstanding balance we owe to supplier
    public bool IsActive { get; private set; } = true;
    public int Rating { get; private set; } = 5;  // 1-5 star rating

    // Navigation properties
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = [];
    public ICollection<SupplierPaymentAccount> PaymentAccounts { get; set; } = [];

    // Computed properties
    // Available advance balance (sum of remaining amounts from advance payments)
    public decimal AdvanceAmount =>
        SupplierPayments?
            .Where(p => p.PaymentType == PaymentType.ADVANCE &&
                       p.Status == "COMPLETED" &&
                       p.RemainingAmount > 0)
            .Sum(p => p.RemainingAmount) ?? 0;

    private Supplier() { }

    public static Supplier Create(string name, string code, string contactPerson, string email, string phone,
        string address, string city, string state, string country, string postalCode,
        string? paymentTerms = null, decimal creditLimit = 0)
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
            PaymentTerms = paymentTerms?.Trim() ?? "NET30",
            CreditLimit = Math.Max(0, creditLimit),
            IsActive = true,
            Rating = 5
        };
    }

    public void Update(string name, string contactPerson, string email, string phone,
        string address, string city, string state, string country, string postalCode, bool isActive,
        string? paymentTerms = null, decimal creditLimit = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        Name = name.Trim();
        ContactPerson = contactPerson?.Trim() ?? string.Empty;
        Email = email?.Trim() ?? string.Empty;
        Phone = phone?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        State = state?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
        PostalCode = postalCode?.Trim() ?? string.Empty;
        PaymentTerms = paymentTerms?.Trim() ?? "NET30";
        CreditLimit = Math.Max(0, creditLimit);
        IsActive = isActive;
    }

    public void SetRating(int rating)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        Rating = rating;
    }

    /// <summary>
    /// Updates the stored balance.
    /// DEPRECATED: Balance should be calculated from transactions via SupplierLedgerService.
    /// This method is kept for backward compatibility during migration.
    /// </summary>
    [Obsolete("Use SupplierLedgerService.CalculateCurrentBalanceAsync() instead. Balance is now calculated from PurchaseOrders, SupplierPayments, and PurchaseReturns.")]
    public void UpdateBalance(decimal amount)
    {
        CurrentBalance += amount;
        if (CurrentBalance < 0)
            CurrentBalance = 0;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
