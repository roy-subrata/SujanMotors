using AutoPartShop.Application.DTOs.AuditDtos;

namespace AutoPartShop.Application.Services;

/// <summary>
/// Service interface for audit log operations
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Get paginated audit logs with advanced filtering
    /// </summary>
    Task<PaginatedResponse<AuditLogResponse>> GetAuditLogsAsync(AuditLogFilterRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get complete audit history for a specific entity
    /// </summary>
    Task<EntityTimeline> GetEntityTimelineAsync(string entityName, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get dashboard insights and statistics
    /// </summary>
    Task<AuditDashboardResponse> GetDashboardAsync(AuditDashboardRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activity summary for a specific user
    /// </summary>
    Task<UserActivitySummary> GetUserActivitySummaryAsync(string userName, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Compare entity state between two timestamps
    /// </summary>
    Task<EntityStateComparison> CompareEntityStatesAsync(string entityName, string entityId, DateTime? fromTimestamp, DateTime? toTimestamp, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of all audited entity types
    /// </summary>
    Task<List<string>> GetAuditedEntitiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of all users who have performed actions
    /// </summary>
    Task<List<string>> GetAuditUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get activity trends for charts
    /// </summary>
    Task<List<ActivityTrend>> GetActivityTrendsAsync(DateTime fromDate, DateTime toDate, string? entityName = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export audit logs
    /// </summary>
    Task<byte[]> ExportAuditLogsAsync(AuditLogFilterRequest filter, AuditExportFormat format, CancellationToken cancellationToken = default);
}
