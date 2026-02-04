namespace AutoPartShop.Application.DTOs.CustomerDtos;

public class CreateCustomerRequest
{
    public string CustomerCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AlternatePhone { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string ShippingAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string CustomerType { get; set; } = "RETAIL";
    public string PrimaryContactPerson { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}





public class AuditLogFilterRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Action { get; set; }
    public string? PerformedBy { get; set; }
    public string? PropertyName { get; set; }
    public string? SearchTerm { get; set; }  // Search across all text fields
    public string? SearchValue { get; set; }  // Search in old/new values
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? IpAddress { get; set; }
    public string SortBy { get; set; } = "PerformedAt";
    public bool SortDescending { get; set; } = true;

    // Advanced filters
    public List<string>? EntityNames { get; set; }  // Filter by multiple entities
    public List<string>? Actions { get; set; }  // Filter by multiple actions
    public List<string>? Users { get; set; }  // Filter by multiple users

    // Export settings
    public int ExportMaxRows { get; set; } = 10000;
}

