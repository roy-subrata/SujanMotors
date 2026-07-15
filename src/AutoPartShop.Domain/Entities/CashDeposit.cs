namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Manual cash-in entry recorded from the cash book (owner deposit, misc income).
/// The counterpart of DailyExpense for the IN side — customer payments cover
/// sale receipts, so this only exists for money that enters outside a sale.
/// </summary>
public class CashDeposit : AuditableEntity
{
    public DateTime DepositDate { get; private set; }
    public string Category { get; private set; } = string.Empty;  // OWNER_DEPOSIT, OTHER
    public decimal Amount { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public string PaymentMethod { get; private set; } = string.Empty;  // CASH, BANK_TRANSFER, etc.
    public string ReferenceNumber { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string Currency { get; private set; } = "BDT";

    private CashDeposit() { }

    public static CashDeposit Create(DateTime depositDate, string category, decimal amount,
        string description, string paymentMethod, string referenceNumber = "",
        string notes = "", string currency = "BDT")
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero", nameof(amount));

        if (string.IsNullOrWhiteSpace(category))
            throw new ArgumentException("Category cannot be empty", nameof(category));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Description cannot be empty", nameof(description));

        if (string.IsNullOrWhiteSpace(paymentMethod))
            throw new ArgumentException("PaymentMethod cannot be empty", nameof(paymentMethod));

        return new CashDeposit
        {
            DepositDate = depositDate,
            Category = category.Trim().ToUpper(),
            Amount = amount,
            Description = description.Trim(),
            PaymentMethod = paymentMethod.Trim().ToUpper(),
            ReferenceNumber = referenceNumber?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            Currency = string.IsNullOrWhiteSpace(currency) ? "BDT" : currency.Trim().ToUpper()
        };
    }
}
