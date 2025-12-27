namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a customer in the system
/// </summary>
public class Customer : AuditableEntity
{
    public string CustomerCode { get; private set; } = string.Empty;  // Unique customer code
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string AlternatePhone { get; private set; } = string.Empty;
    public string CompanyName { get; private set; } = string.Empty;
    public string BillingAddress { get; private set; } = string.Empty;
    public string ShippingAddress { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string State { get; private set; } = string.Empty;
    public string PostalCode { get; private set; } = string.Empty;
    public string Country { get; private set; } = string.Empty;
    public string TaxId { get; private set; } = string.Empty;  // Tax identification number
    public string Status { get; private set; } = "ACTIVE";  // ACTIVE, INACTIVE, SUSPENDED, BLACKLISTED
    public string CustomerType { get; private set; } = "RETAIL";  // RETAIL, WHOLESALE, CORPORATE
    public decimal CreditLimit { get; private set; } = 0;
    public decimal CurrentBalance { get; private set; } = 0;  // Outstanding balance
    public DateTime DateOfBirth { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public DateTime? LastPurchaseDate { get; private set; }
    public decimal TotalPurchaseAmount { get; private set; } = 0;
    public string PrimaryContactPerson { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
    public ICollection<CustomerPayment> CustomerPayments { get; set; } = new List<CustomerPayment>();

    // Computed properties for payment tracking (Single Source of Truth)
    public decimal TotalPaid =>
        CustomerPayments?
            .Where(p => p.Status == "COMPLETED")
            .Sum(p => p.Amount) ?? 0;

    public decimal AccountBalance =>
        CustomerPayments?
            .Where(p => p.Status == "COMPLETED" && p.InvoiceId == null)
            .Sum(p => p.Amount) ?? 0;

    public int PendingPaymentsCount =>
        CustomerPayments?
            .Count(p => p.Status == "PENDING") ?? 0;

    private Customer() { }

    public static Customer Create(string customerCode, string firstName, string lastName,
        string email, string phone, string companyName, string billingAddress,
        string shippingAddress, string city, string state, string postalCode,
        string country, DateTime dateOfBirth, string customerType = "RETAIL", string notes = "")
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            throw new ArgumentException("CustomerCode cannot be empty", nameof(customerCode));

        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("FirstName cannot be empty", nameof(firstName));

        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("LastName cannot be empty", nameof(lastName));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        if (dateOfBirth >= DateTime.UtcNow.Date)
            throw new ArgumentException("Date of birth must be in the past", nameof(dateOfBirth));

        var validCustomerTypes = new[] { "RETAIL", "WHOLESALE", "CORPORATE" };
        if (!validCustomerTypes.Contains(customerType.ToUpper()))
            throw new ArgumentException($"CustomerType must be one of: {string.Join(", ", validCustomerTypes)}", nameof(customerType));

        return new Customer
        {
            CustomerCode = customerCode.Trim().ToUpper(),
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            Email = email.Trim().ToLower(),
            Phone = phone.Trim(),
            CompanyName = companyName?.Trim() ?? string.Empty,
            BillingAddress = billingAddress?.Trim() ?? string.Empty,
            ShippingAddress = shippingAddress?.Trim() ?? string.Empty,
            City = city?.Trim() ?? string.Empty,
            State = state?.Trim() ?? string.Empty,
            PostalCode = postalCode?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
            DateOfBirth = dateOfBirth,
            CustomerType = customerType.ToUpper(),
            Status = "ACTIVE",
            Notes = notes?.Trim() ?? string.Empty
        };
    }

    public string GetFullName() => $"{FirstName} {LastName}";

    public void SetCreditLimit(decimal limit)
    {
        if (limit < 0)
            throw new ArgumentException("Credit limit cannot be negative", nameof(limit));

        CreditLimit = limit;
    }

    public void UpdateBalance(decimal amount)
    {
        CurrentBalance += amount;
        if (CurrentBalance < 0)
            CurrentBalance = 0;
    }

    public void RecordPurchase(decimal amount, DateTime? purchaseDate = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Purchase amount must be greater than 0", nameof(amount));

        TotalPurchaseAmount += amount;
        LastPurchaseDate = purchaseDate ?? DateTime.UtcNow;
    }

    public bool CanPlaceOrder()
    {
        if (Status != "ACTIVE")
            return false;

        if (CreditLimit > 0 && CurrentBalance >= CreditLimit)
            return false;

        return true;
    }

    public void Activate()
    {
        if (Status == "BLACKLISTED")
            throw new InvalidOperationException("Cannot activate a blacklisted customer");

        Status = "ACTIVE";
    }

    public void Deactivate()
    {
        Status = "INACTIVE";
    }

    public void Suspend()
    {
        Status = "SUSPENDED";
    }

    public void Blacklist()
    {
        Status = "BLACKLISTED";
    }

    public void UpdateContactInfo(string email, string phone, string alternatePhone = "")
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        Email = email.Trim().ToLower();
        Phone = phone.Trim();
        AlternatePhone = alternatePhone?.Trim() ?? string.Empty;
    }

    public void UpdateAddress(string billingAddress, string shippingAddress, string city, string state, string postalCode, string country)
    {
        BillingAddress = billingAddress?.Trim() ?? string.Empty;
        ShippingAddress = shippingAddress?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        State = state?.Trim() ?? string.Empty;
        PostalCode = postalCode?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void SetTaxId(string taxId)
    {
        TaxId = taxId?.Trim() ?? string.Empty;
    }

    public void SetPrimaryContactPerson(string contactPerson)
    {
        PrimaryContactPerson = contactPerson?.Trim() ?? string.Empty;
    }
}
