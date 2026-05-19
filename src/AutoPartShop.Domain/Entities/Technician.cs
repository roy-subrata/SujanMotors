namespace AutoPartShop.Domain.Entities;

/// <summary>
/// Represents a technician who repairs vehicles and recommends parts to customers
/// </summary>
public class Technician : AuditableEntity
{
    public string TechnicianCode { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string ShopName { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string City { get; private set; } = string.Empty;
    public string Status { get; private set; } = "ACTIVE";  // ACTIVE, INACTIVE
    public string Notes { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();

    private Technician() { }

    public static Technician Create(string technicianCode, string name, string phone,
        string email = "", string shopName = "", string address = "", string city = "", string notes = "")
    {
        if (string.IsNullOrWhiteSpace(technicianCode))
            throw new ArgumentException("TechnicianCode cannot be empty", nameof(technicianCode));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        return new Technician
        {
            TechnicianCode = technicianCode.Trim().ToUpper(),
            Name = name.Trim(),
            Phone = phone.Trim(),
            Email = email?.Trim() ?? string.Empty,
            ShopName = shopName?.Trim() ?? string.Empty,
            Address = address?.Trim() ?? string.Empty,
            City = city?.Trim() ?? string.Empty,
            Notes = notes?.Trim() ?? string.Empty,
            Status = "ACTIVE"
        };
    }

    public void UpdateInfo(string name, string phone, string email, string shopName,
        string address, string city, string notes)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        if (string.IsNullOrWhiteSpace(phone))
            throw new ArgumentException("Phone cannot be empty", nameof(phone));

        Name = name.Trim();
        Phone = phone.Trim();
        Email = email?.Trim() ?? string.Empty;
        ShopName = shopName?.Trim() ?? string.Empty;
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        Notes = notes?.Trim() ?? string.Empty;
    }

    public void Activate() => Status = "ACTIVE";
    public void Deactivate() => Status = "INACTIVE";
}
