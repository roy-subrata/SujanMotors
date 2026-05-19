namespace AutoPartShop.Application.DTOs.AuditDtos;

/// <summary>
/// Comprehensive dashboard response with insights
/// </summary>
public class AuditDashboardResponse
{
    public AuditOverview Overview { get; set; } = new();
    public List<ActivityTrend> DailyTrends { get; set; } = new();
    public List<HourlyActivity> HourlyDistribution { get; set; } = new();
    public List<EntityChangeCount> TopEntities { get; set; } = new();
    public List<UserActivityCount> TopUsers { get; set; } = new();
    public List<ActionDistribution> ActionBreakdown { get; set; } = new();
    public List<AuditLogResponse> RecentActivity { get; set; } = new();
}

/// <summary>
/// High-level overview statistics
/// </summary>
public class AuditOverview
{
    public int TotalChangesToday { get; set; }
    public int TotalChangesThisWeek { get; set; }
    public int TotalChangesThisMonth { get; set; }
    public int TotalChangesAllTime { get; set; }
    public int UniqueUsersToday { get; set; }
    public int UniqueEntitiesModified { get; set; }
    public double AverageChangesPerDay { get; set; }
    public DateTime? LastActivityTime { get; set; }
}

/// <summary>
/// Daily activity trend for charts
/// </summary>
public class ActivityTrend
{
    public DateTime Date { get; set; }
    public int InsertCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// Hourly activity distribution (0-23 hours)
/// </summary>
public class HourlyActivity
{
    public int Hour { get; set; }
    public int ActivityCount { get; set; }
}

/// <summary>
/// Distribution of actions (INSERT, UPDATE, DELETE)
/// </summary>
public class ActionDistribution
{
    public string Action { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Entity timeline showing chronological changes
/// </summary>
public class EntityTimeline
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public List<TimelineEvent> Events { get; set; } = new();
}

/// <summary>
/// Single event in the timeline
/// </summary>
public class TimelineEvent
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string PerformedBy { get; set; } = string.Empty;
    public List<PropertyChange> Changes { get; set; } = new();
    public string? IpAddress { get; set; }
}

/// <summary>
/// Comparison of entity state at two points in time
/// </summary>
public class EntityStateComparison
{
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime? FromTimestamp { get; set; }
    public DateTime? ToTimestamp { get; set; }
    public List<PropertyStateChange> PropertyChanges { get; set; } = new();
    public int TotalChanges { get; set; }
}

/// <summary>
/// Property state at different points
/// </summary>
public class PropertyStateChange
{
    public string PropertyName { get; set; } = string.Empty;
    public string? OriginalValue { get; set; }
    public string? CurrentValue { get; set; }
    public DateTime? LastChangedAt { get; set; }
    public string? LastChangedBy { get; set; }
}

/// <summary>
/// User activity summary
/// </summary>
public class UserActivitySummary
{
    public string UserName { get; set; } = string.Empty;
    public int TotalActions { get; set; }
    public int InsertCount { get; set; }
    public int UpdateCount { get; set; }
    public int DeleteCount { get; set; }
    public DateTime? FirstActivityDate { get; set; }
    public DateTime? LastActivityDate { get; set; }
    public List<EntityChangeCount> EntityBreakdown { get; set; } = new();
    public List<ActionDistribution> ActionBreakdown { get; set; } = new();
}

/// <summary>
/// Export format options
/// </summary>
public enum AuditExportFormat
{
    Csv,
    Json,
    Excel
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PaginatedResponse<T>
{
    public List<T> Data { get; set; } = new();
    public PaginationInfo Pagination { get; set; } = new();
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationInfo
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}
