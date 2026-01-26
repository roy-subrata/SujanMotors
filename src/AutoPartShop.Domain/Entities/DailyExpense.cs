namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Tracks daily operational expenses (rent, utilities, salaries, etc.)
/// This is separate from purchase orders which track inventory purchases
/// </summary>
public class DailyExpense : AuditableEntity
{
    public DateTime ExpenseDate { get; private set; }
    public string Category { get; private set; } = string.Empty;  // RENT, UTILITIES, SALARIES, OFFICE_SUPPLIES, MARKETING, MAINTENANCE, TRANSPORTATION, etc.
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;  // CASH, BANK_TRANSFER, CHECK, CREDIT_CARD, etc.
    public string ReferenceNumber { get; private set; } = string.Empty;  // Check number, transfer ref, etc.
    public string Notes { get; private set; } = string.Empty;
    public string VendorName { get; private set; } = string.Empty;  // Who was paid (utility company, landlord, etc.)
    public bool IsRecurring { get; private set; } = false;  // Is this a recurring expense?
    public string RecurrencePattern { get; private set; } = string.Empty;  // MONTHLY, WEEKLY, YEARLY, etc.
    public Guid? AttachmentId { get; private set; }  // Optional: link to receipt/invoice attachment
    public string Currency { get; private set; } = "BDT";  // ISO 4217 currency code

    private DailyExpense() { }

    public static DailyExpense Create(DateTime expenseDate, string category, decimal amount,
        string description, string paymentMethod, string vendorName = "", string currency = "BDT")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        return new DailyExpense
        {
            ExpenseDate = expenseDate.Date,  // Store as date only
            Category = category.Trim().ToUpper(),
            Amount = amount,
            Description = description.Trim(),
            PaymentMethod = paymentMethod.Trim().ToUpper(),
            VendorName = vendorName?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }

    public void Update(DateTime expenseDate, string category, decimal amount,
        string description, string paymentMethod, string vendorName)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        ExpenseDate = expenseDate.Date;
        Category = category.Trim().ToUpper();
        Amount = amount;
        Description = description.Trim();
        PaymentMethod = paymentMethod.Trim().ToUpper();
        VendorName = vendorName?.Trim() ?? string.Empty;
    }

    public void SetReferenceNumber(string referenceNumber)
    {
        ReferenceNumber = referenceNumber?.Trim() ?? string.Empty;
    }

    public void UpdateNotes(string notes)
    {
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void SetRecurring(bool isRecurring, string pattern = "")
    {
        IsRecurring = isRecurring;
        RecurrencePattern = isRecurring ? (pattern?.Trim().ToUpper() ?? string.Empty) : string.Empty;
    }

    public void AttachDocument(Guid attachmentId)
    {
        AttachmentId = attachmentId;
    }
}
