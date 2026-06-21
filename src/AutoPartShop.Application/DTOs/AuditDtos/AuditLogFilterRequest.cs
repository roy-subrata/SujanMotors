namespace AutoPartShop.Application.DTOs.AuditDtos;

/// <summary>
/// Request model for filtering audit logs with advanced options
/// </summary>
public class AuditLogFilterRequest
{
    /// <summary>Largest audit page a client may request.</summary>
    public const int MaxPageSize = 200;
    /// <summary>Hard ceiling on a single export so one call can't stream the entire table.</summary>
    public const int MaxExportRows = 50000;

    private int _pageNumber = 1;
    private int _pageSize = 50;
    private int _exportMaxRows = 10000;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 50 : (value > MaxPageSize ? MaxPageSize : value);
    }
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
    public int ExportMaxRows
    {
        get => _exportMaxRows;
        set => _exportMaxRows = value < 1 ? 10000 : (value > MaxExportRows ? MaxExportRows : value);
    }
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
