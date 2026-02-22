namespace AutoPartShop.Application.DTOs.AuditDtos;

/// <summary>
/// Request model for filtering audit logs with advanced options
/// </summary>
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

/// <summary>
/// Request for dashboard insights
/// </summary>
public class AuditDashboardRequest
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? EntityName { get; set; }
    public string? PerformedBy { get; set; }
    public int TopCount { get; set; } = 10;
}
