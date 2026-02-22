namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a brand/manufacturer of auto parts
/// </summary>
public class Brand : AuditableEntity
{
    /// <summary>
    /// Brand name (e.g., Bosch, Denso, NGK)
    /// </summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Brand code for reference (e.g., BOSCH, DENSO)
    /// </summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>
    /// Description of the brand
    /// </summary>
    public string Description { get; private set; } = string.Empty;

    /// <summary>
    /// URL to the brand's logo image
    /// </summary>
    public string LogoUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Brand's official website
    /// </summary>
    public string Website { get; private set; } = string.Empty;

    /// <summary>
    /// Country of origin for the brand
    /// </summary>
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Contact email for the brand
    /// </summary>
    public string ContactEmail { get; private set; } = string.Empty;

    /// <summary>
    /// Contact phone number for the brand
    /// </summary>
    public string ContactPhone { get; private set; } = string.Empty;

    /// <summary>
    /// Whether this brand is active
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Display order in lists
    /// </summary>
    public int DisplayOrder { get; private set; } = 0;

    /// <summary>
    /// Collection of parts from this brand
    /// </summary>
    public ICollection<Part> Parts { get; set; } = new List<Part>();

    private Brand() { }

    public static Brand Create(string name, string code, string description = "", string country = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Brand code cannot be empty", nameof(code));

        if (name.Length > 100)
            throw new ArgumentException("Brand name cannot exceed 100 characters", nameof(name));

        if (code.Length > 20)
            throw new ArgumentException("Brand code cannot exceed 20 characters", nameof(code));

        return new Brand
        {
            Name = name.Trim(),
            Code = code.Trim().ToUpper(),
            Description = description?.Trim() ?? string.Empty,
            Country = country?.Trim() ?? string.Empty,
            IsActive = true
        };
    }

    public void Update(string name, string code, string description, string logoUrl, 
        string website, string country, string contactEmail, string contactPhone, 
        int displayOrder, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Brand name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Brand code cannot be empty", nameof(code));

        Name = name.Trim();
        Code = code.Trim().ToUpper();
        Description = description?.Trim() ?? string.Empty;
        LogoUrl = logoUrl?.Trim() ?? string.Empty;
        Website = website?.Trim() ?? string.Empty;
        Country = country?.Trim() ?? string.Empty;
        ContactEmail = contactEmail?.Trim() ?? string.Empty;
        ContactPhone = contactPhone?.Trim() ?? string.Empty;
        DisplayOrder = displayOrder;
        IsActive = isActive;
    }

    public void SetLogo(string logoUrl)
    {
        LogoUrl = logoUrl?.Trim() ?? string.Empty;
    }

    public void SetWebsite(string website)
    {
        Website = website?.Trim() ?? string.Empty;
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
