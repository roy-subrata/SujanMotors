namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents application-wide settings stored in the database
/// </summary>
public sealed class ApplicationSettings : AuditableEntity
{
    /// <summary>
    /// Setting key (e.g., "BASE_CURRENCY", "TAX_RATE", "COMPANY_NAME")
    /// </summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>
    /// Setting value stored as string
    /// </summary>
    public string Value { get; private set; } = string.Empty;

    /// <summary>
    /// Data type of the value (STRING, INT, DECIMAL, BOOL, GUID, DATE, JSON)
    /// </summary>
    public string DataType { get; private set; } = "STRING";

    /// <summary>
    /// Category for grouping settings (CURRENCY, TAX, GENERAL, NOTIFICATION, etc.)
    /// </summary>
    public string Category { get; private set; } = "GENERAL";

    /// <summary>
    /// Human-readable description of this setting
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this setting is system-managed and should not be deleted
    /// </summary>
    public bool IsSystemSetting { get; private set; } = false;

    // Private constructor for EF Core
    private ApplicationSettings() { }

    /// <summary>
    /// Factory method to create a new application setting
    /// </summary>
    public static ApplicationSettings Create(
        string key,
        string value,
        string dataType = "STRING",
        string category = "GENERAL",
        string description = "",
        bool isSystemSetting = false)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Setting key cannot be empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var validDataTypes = new[] { "STRING", "INT", "DECIMAL", "BOOL", "GUID", "DATE", "JSON" };
        if (!validDataTypes.Contains(dataType.ToUpper()))
            throw new ArgumentException($"Data type must be one of: {string.Join(", ", validDataTypes)}", nameof(dataType));

        return new ApplicationSettings
        {
            Key = key.Trim().ToUpper().Replace(" ", "_"),
            Value = value,
            DataType = dataType.Trim().ToUpper(),
            Category = category.Trim().ToUpper(),
            Description = description?.Trim() ?? string.Empty,
            IsSystemSetting = isSystemSetting,
            CreatedDate = DateTime.UtcNow,
            ModifiedDate = DateTime.UtcNow,
            Isdeleted = false
        };
    }

    /// <summary>
    /// Update setting value
    /// </summary>
    public void UpdateValue(string value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        Value = value;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Update setting details
    /// </summary>
    public void Update(
        string value,
        string dataType,
        string category,
        string description)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        var validDataTypes = new[] { "STRING", "INT", "DECIMAL", "BOOL", "GUID", "DATE", "JSON" };
        if (!validDataTypes.Contains(dataType.ToUpper()))
            throw new ArgumentException($"Data type must be one of: {string.Join(", ", validDataTypes)}", nameof(dataType));

        Value = value;
        DataType = dataType.Trim().ToUpper();
        Category = category.Trim().ToUpper();
        Description = description?.Trim() ?? string.Empty;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Soft delete the setting
    /// </summary>
    public void Delete()
    {
        if (IsSystemSetting)
            throw new InvalidOperationException("Cannot delete system settings");

        Isdeleted = true;
        ModifiedDate = DateTime.UtcNow;
    }

    /// <summary>
    /// Get typed value as integer
    /// </summary>
    public int GetIntValue()
    {
        if (DataType != "INT")
            throw new InvalidOperationException($"Setting '{Key}' is not an INT type");

        return int.Parse(Value);
    }

    /// <summary>
    /// Get typed value as decimal
    /// </summary>
    public decimal GetDecimalValue()
    {
        if (DataType != "DECIMAL")
            throw new InvalidOperationException($"Setting '{Key}' is not a DECIMAL type");

        return decimal.Parse(Value);
    }

    /// <summary>
    /// Get typed value as boolean
    /// </summary>
    public bool GetBoolValue()
    {
        if (DataType != "BOOL")
            throw new InvalidOperationException($"Setting '{Key}' is not a BOOL type");

        return bool.Parse(Value);
    }

    /// <summary>
    /// Get typed value as GUID
    /// </summary>
    public Guid GetGuidValue()
    {
        if (DataType != "GUID")
            throw new InvalidOperationException($"Setting '{Key}' is not a GUID type");

        return Guid.Parse(Value);
    }

    /// <summary>
    /// Get typed value as DateTime
    /// </summary>
    public DateTime GetDateValue()
    {
        if (DataType != "DATE")
            throw new InvalidOperationException($"Setting '{Key}' is not a DATE type");

        return DateTime.Parse(Value);
    }
}
