namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents an exchange rate between two currencies
/// </summary>
public sealed class ExchangeRate : AuditableEntity
{
    /// <summary>
    /// Source currency ID
    /// </summary>
    public Guid FromCurrencyId { get; private set; }

    /// <summary>
    /// Target currency ID
    /// </summary>
    public Guid ToCurrencyId { get; private set; }

    /// <summary>
    /// Exchange rate: 1 unit of FromCurrency = Rate units of ToCurrency
    /// Example: If 1 USD = 110 BDT, then Rate = 110
    /// </summary>
    public decimal Rate { get; private set; }

    /// <summary>
    /// Date from which this rate is effective
    /// </summary>
    public DateTime EffectiveDate { get; private set; }

    /// <summary>
    /// Optional date when this rate expires (null if no expiry)
    /// </summary>
    public DateTime? ExpiryDate { get; private set; }

    /// <summary>
    /// Source of the exchange rate (MANUAL, BANK, API, etc.)
    /// </summary>
    public string Source { get; private set; } = "MANUAL";

    /// <summary>
    /// Whether this rate is currently active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Optional notes about this exchange rate
    /// </summary>
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public Currency? FromCurrency { get; set; }
    public Currency? ToCurrency { get; set; }

    // Private constructor for EF Core
    private ExchangeRate() { }

    /// <summary>
    /// Factory method to create a new exchange rate
    /// </summary>
    public static ExchangeRate Create(
        Guid fromCurrencyId,
        Guid toCurrencyId,
        decimal rate,
        DateTime effectiveDate,
        DateTime? expiryDate = null,
        string source = "MANUAL",
        string notes = "")
    {
        if (fromCurrencyId == Guid.Empty)
            throw new ArgumentException("From currency ID cannot be empty", nameof(fromCurrencyId));

        if (toCurrencyId == Guid.Empty)
            throw new ArgumentException("To currency ID cannot be empty", nameof(toCurrencyId));

        if (fromCurrencyId == toCurrencyId)
            throw new ArgumentException("From currency and To currency cannot be the same");

        if (rate <= 0)
            throw new ArgumentException("Exchange rate must be greater than zero", nameof(rate));

        if (expiryDate.HasValue && expiryDate.Value <= effectiveDate)
            throw new ArgumentException("Expiry date must be after effective date", nameof(expiryDate));

        return new ExchangeRate
        {
            FromCurrencyId = fromCurrencyId,
            ToCurrencyId = toCurrencyId,
            Rate = rate,
            EffectiveDate = effectiveDate,
            ExpiryDate = expiryDate,
            Source = string.IsNullOrWhiteSpace(source) ? "MANUAL" : source.Trim().ToUpper(),
            IsActive = true,
            Notes = notes?.Trim() ?? string.Empty,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    /// <summary>
    /// Update exchange rate details
    /// </summary>
    public void Update(
        decimal rate,
        DateTime effectiveDate,
        DateTime? expiryDate = null,
        string source = "MANUAL",
        string notes = "")
    {
        if (rate <= 0)
            throw new ArgumentException("Exchange rate must be greater than zero", nameof(rate));

        if (expiryDate.HasValue && expiryDate.Value <= effectiveDate)
            throw new ArgumentException("Expiry date must be after effective date", nameof(expiryDate));

        Rate = rate;
        EffectiveDate = effectiveDate;
        ExpiryDate = expiryDate;
        Source = string.IsNullOrWhiteSpace(source) ? "MANUAL" : source.Trim().ToUpper();
        Notes = notes?.Trim() ?? string.Empty;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the exchange rate
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the exchange rate
    /// </summary>
    public void Deactivate()
    {
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the exchange rate
    /// </summary>
    public void Delete()
    {
        Isdeleted = true;
        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if this rate is valid for a given date
    /// </summary>
    public bool IsValidForDate(DateTime date)
    {
        if (!IsActive || Isdeleted)
            return false;

        if (date < EffectiveDate)
            return false;

        if (ExpiryDate.HasValue && date > ExpiryDate.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Convert amount using this exchange rate
    /// </summary>
    public decimal Convert(decimal amount)
    {
        return Math.Round(amount * Rate, 2, MidpointRounding.ToEven);
    }
}
