namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a currency supported in the system
/// </summary>
public sealed class Currency : AuditableEntity
{
    /// <summary>
    /// ISO 4217 currency code (e.g., BDT, USD, INR, NPR)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Full name of the currency (e.g., "Bangladeshi Taka")
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Currency symbol (e.g., "৳", "$", "₹", "रू")
    /// </summary>
    public string Symbol { get; private set; } = string.Empty;

    /// <summary>
    /// Number of decimal places for this currency (usually 2)
    /// </summary>
    public int DecimalPlaces { get; private set; } = 2;

    /// <summary>
    /// Whether this currency is currently active and can be used
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Whether this is the base currency for the system (only one can be true)
    /// </summary>
    public bool IsBaseCurrency { get; private set; } = false;

    /// <summary>
    /// Display order for UI sorting
    /// </summary>
    public int DisplayOrder { get; private set; } = 0;

    // Private constructor for EF Core
    private Currency() { }

    /// <summary>
    /// Factory method to create a new currency
    /// </summary>
    public static Currency Create(
        string code,
        string name,
        string symbol,
        int decimalPlaces = 2,
        bool isActive = true,
        bool isBaseCurrency = false,
        int displayOrder = 0)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Currency code cannot be empty", nameof(code));

        if (code.Length != 3)
            throw new ArgumentException("Currency code must be 3 characters (ISO 4217)", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Currency symbol cannot be empty", nameof(symbol));

        if (decimalPlaces < 0 || decimalPlaces > 4)
            throw new ArgumentException("Decimal places must be between 0 and 4", nameof(decimalPlaces));

        return new Currency
        {
            Code = code.Trim().ToUpper(),
            Name = name.Trim(),
            Symbol = symbol.Trim(),
            DecimalPlaces = decimalPlaces,
            IsActive = isActive,
            IsBaseCurrency = isBaseCurrency,
            DisplayOrder = displayOrder,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    /// <summary>
    /// Update currency details
    /// </summary>
    public void Update(
        string name,
        string symbol,
        int decimalPlaces,
        bool isActive,
        int displayOrder)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Currency name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Currency symbol cannot be empty", nameof(symbol));

        if (decimalPlaces < 0 || decimalPlaces > 4)
            throw new ArgumentException("Decimal places must be between 0 and 4", nameof(decimalPlaces));

        Name = name.Trim();
        Symbol = symbol.Trim();
        DecimalPlaces = decimalPlaces;
        IsActive = isActive;
        DisplayOrder = displayOrder;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Set this currency as the base currency
    /// </summary>
    public void SetAsBaseCurrency()
    {
        IsBaseCurrency = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Remove base currency status
    /// </summary>
    public void RemoveBaseCurrencyStatus()
    {
        IsBaseCurrency = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Activate the currency
    /// </summary>
    public void Activate()
    {
        IsActive = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate the currency
    /// </summary>
    public void Deactivate()
    {
        if (IsBaseCurrency)
            throw new InvalidOperationException("Cannot deactivate the base currency");

        IsActive = false;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the currency
    /// </summary>
    public void Delete()
    {
        if (IsBaseCurrency)
            throw new InvalidOperationException("Cannot delete the base currency");

        Isdeleted = true;
        ModifiedDate = DateTime.UtcNow;
    }
}
