using System.Text;
using System.Text.Json;
using AutoPartShop.Application.DTOs.AuditDtos;
using AutoPartShop.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Application.Services;

/// <summary>
/// Implementation of audit log service with advanced querying capabilities
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly AutoPartDbContext _dbContext;

    public AuditLogService(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaginatedResponse<AuditLogResponse>> GetAuditLogsAsync(
        AuditLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsQueryable();

        // Apply filters
        query = ApplyFilters(query, request);

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply sorting
        query = ApplySorting(query, request.SortBy, request.SortDescending);

        // Apply pagination
        var logs = await query
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                PropertyName = x.PropertyName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                PerformedBy = x.PerformedBy,
                PerformedAt = x.PerformedAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToListAsync(cancellationToken);

        return new PaginatedResponse<AuditLogResponse>
        {
            Data = logs,
            Pagination = new PaginationInfo
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
            }
        };
    }

    public async Task<EntityTimeline> GetEntityTimelineAsync(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        var logs = await _dbContext.AuditLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId)
            .OrderByDescending(x => x.PerformedAt)
            .ToListAsync(cancellationToken);

        var events = logs
            .GroupBy(x => new { x.Action, x.PerformedBy, x.PerformedAt, x.IpAddress })
            .Select(g => new TimelineEvent
            {
                Timestamp = g.Key.PerformedAt,
                Action = g.Key.Action,
                PerformedBy = g.Key.PerformedBy,
                IpAddress = g.Key.IpAddress,
                Changes = g.Select(x => new PropertyChange
                {
                    PropertyName = x.PropertyName,
                    OldValue = x.OldValue,
                    NewValue = x.NewValue
                }).ToList()
            })
            .OrderByDescending(x => x.Timestamp)
            .ToList();

        return new EntityTimeline
        {
            EntityName = entityName,
            EntityId = entityId,
            Events = events
        };
    }

    public async Task<AuditDashboardResponse> GetDashboardAsync(
        AuditDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek);
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var query = _dbContext.AuditLogs.AsQueryable();

        if (request.FromDate.HasValue)
            query = query.Where(x => x.PerformedAt >= request.FromDate.Value);
        if (request.ToDate.HasValue)
            query = query.Where(x => x.PerformedAt <= request.ToDate.Value);
        if (!string.IsNullOrEmpty(request.EntityName))
            query = query.Where(x => x.EntityName == request.EntityName);
        if (!string.IsNullOrEmpty(request.PerformedBy))
            query = query.Where(x => x.PerformedBy == request.PerformedBy);

        var allLogs = await query.ToListAsync(cancellationToken);
        var allTimeLogs = await _dbContext.AuditLogs.ToListAsync(cancellationToken);

        // Overview
        var overview = new AuditOverview
        {
            TotalChangesToday = allTimeLogs.Count(x => x.PerformedAt.Date == today),
            TotalChangesThisWeek = allTimeLogs.Count(x => x.PerformedAt >= weekStart),
            TotalChangesThisMonth = allTimeLogs.Count(x => x.PerformedAt >= monthStart),
            TotalChangesAllTime = allTimeLogs.Count,
            UniqueUsersToday = allTimeLogs.Where(x => x.PerformedAt.Date == today).Select(x => x.PerformedBy).Distinct().Count(),
            UniqueEntitiesModified = allLogs.Select(x => x.EntityName).Distinct().Count(),
            LastActivityTime = allTimeLogs.Any() ? allTimeLogs.Max(x => x.PerformedAt) : null
        };

        if (allTimeLogs.Any())
        {
            var firstDate = allTimeLogs.Min(x => x.PerformedAt).Date;
            var daysSinceFirst = Math.Max(1, (today - firstDate).Days + 1);
            overview.AverageChangesPerDay = Math.Round((double)allTimeLogs.Count / daysSinceFirst, 2);
        }

        // Daily trends (last 30 days or specified range)
        var trendStartDate = request.FromDate ?? today.AddDays(-30);
        var trendEndDate = request.ToDate ?? today;
        var dailyTrends = Enumerable.Range(0, (trendEndDate - trendStartDate).Days + 1)
            .Select(offset => trendStartDate.AddDays(offset))
            .Select(date => new ActivityTrend
            {
                Date = date,
                InsertCount = allLogs.Count(x => x.PerformedAt.Date == date && x.Action == "INSERT"),
                UpdateCount = allLogs.Count(x => x.PerformedAt.Date == date && x.Action == "UPDATE"),
                DeleteCount = allLogs.Count(x => x.PerformedAt.Date == date && x.Action == "DELETE"),
                TotalCount = allLogs.Count(x => x.PerformedAt.Date == date)
            })
            .ToList();

        // Hourly distribution
        var hourlyDistribution = Enumerable.Range(0, 24)
            .Select(hour => new HourlyActivity
            {
                Hour = hour,
                ActivityCount = allLogs.Count(x => x.PerformedAt.Hour == hour)
            })
            .ToList();

        // Top entities
        var topEntities = allLogs
            .GroupBy(x => x.EntityName)
            .Select(g => new EntityChangeCount
            {
                EntityName = g.Key,
                ChangeCount = g.Count()
            })
            .OrderByDescending(x => x.ChangeCount)
            .Take(request.TopCount)
            .ToList();

        // Top users
        var topUsers = allLogs
            .GroupBy(x => x.PerformedBy)
            .Select(g => new UserActivityCount
            {
                UserName = g.Key,
                ActivityCount = g.Count()
            })
            .OrderByDescending(x => x.ActivityCount)
            .Take(request.TopCount)
            .ToList();

        // Action breakdown
        var totalActions = allLogs.Count;
        var actionBreakdown = allLogs
            .GroupBy(x => x.Action)
            .Select(g => new ActionDistribution
            {
                Action = g.Key,
                Count = g.Count(),
                Percentage = totalActions > 0 ? Math.Round((double)g.Count() / totalActions * 100, 2) : 0
            })
            .OrderByDescending(x => x.Count)
            .ToList();

        // Recent activity
        var recentActivity = allLogs
            .OrderByDescending(x => x.PerformedAt)
            .Take(10)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                PropertyName = x.PropertyName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                PerformedBy = x.PerformedBy,
                PerformedAt = x.PerformedAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToList();

        return new AuditDashboardResponse
        {
            Overview = overview,
            DailyTrends = dailyTrends,
            HourlyDistribution = hourlyDistribution,
            TopEntities = topEntities,
            TopUsers = topUsers,
            ActionBreakdown = actionBreakdown,
            RecentActivity = recentActivity
        };
    }

    public async Task<UserActivitySummary> GetUserActivitySummaryAsync(
        string userName,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.Where(x => x.PerformedBy == userName);

        if (fromDate.HasValue)
            query = query.Where(x => x.PerformedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(x => x.PerformedAt <= toDate.Value);

        var logs = await query.ToListAsync(cancellationToken);

        var entityBreakdown = logs
            .GroupBy(x => x.EntityName)
            .Select(g => new EntityChangeCount
            {
                EntityName = g.Key,
                ChangeCount = g.Count()
            })
            .OrderByDescending(x => x.ChangeCount)
            .ToList();

        var actionBreakdown = logs
            .GroupBy(x => x.Action)
            .Select(g => new ActionDistribution
            {
                Action = g.Key,
                Count = g.Count(),
                Percentage = logs.Count > 0 ? Math.Round((double)g.Count() / logs.Count * 100, 2) : 0
            })
            .ToList();

        return new UserActivitySummary
        {
            UserName = userName,
            TotalActions = logs.Count,
            InsertCount = logs.Count(x => x.Action == "INSERT"),
            UpdateCount = logs.Count(x => x.Action == "UPDATE"),
            DeleteCount = logs.Count(x => x.Action == "DELETE"),
            FirstActivityDate = logs.Any() ? logs.Min(x => x.PerformedAt) : null,
            LastActivityDate = logs.Any() ? logs.Max(x => x.PerformedAt) : null,
            EntityBreakdown = entityBreakdown,
            ActionBreakdown = actionBreakdown
        };
    }

    public async Task<EntityStateComparison> CompareEntityStatesAsync(
        string entityName,
        string entityId,
        DateTime? fromTimestamp,
        DateTime? toTimestamp,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs
            .Where(x => x.EntityName == entityName && x.EntityId == entityId);

        if (fromTimestamp.HasValue)
            query = query.Where(x => x.PerformedAt >= fromTimestamp.Value);
        if (toTimestamp.HasValue)
            query = query.Where(x => x.PerformedAt <= toTimestamp.Value);

        var logs = await query.OrderBy(x => x.PerformedAt).ToListAsync(cancellationToken);

        var changes = logs
            .Where(x => !string.IsNullOrEmpty(x.PropertyName))
            .Select(x => new PropertyStateChange
            {
                PropertyName = x.PropertyName,
                OriginalValue = x.OldValue,
                CurrentValue = x.NewValue,
                LastChangedAt = x.PerformedAt,
                LastChangedBy = x.PerformedBy
            })
            .GroupBy(x => x.PropertyName)
            .Select(g => g.Last())
            .ToList();

        return new EntityStateComparison
        {
            EntityName = entityName,
            EntityId = entityId,
            FromTimestamp = fromTimestamp ?? logs.FirstOrDefault()?.PerformedAt,
            ToTimestamp = toTimestamp ?? logs.LastOrDefault()?.PerformedAt,
            PropertyChanges = changes,
            TotalChanges = logs.Count
        };
    }

    public async Task<List<string>> GetAuditedEntitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Select(x => x.EntityName)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<string>> GetAuditUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.AuditLogs
            .Select(x => x.PerformedBy)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ActivityTrend>> GetActivityTrendsAsync(
        DateTime fromDate,
        DateTime toDate,
        string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs
            .Where(x => x.PerformedAt >= fromDate && x.PerformedAt <= toDate.AddDays(1));

        if (!string.IsNullOrEmpty(entityName))
            query = query.Where(x => x.EntityName == entityName);

        var logs = await query.ToListAsync(cancellationToken);

        return Enumerable.Range(0, (toDate - fromDate).Days + 1)
            .Select(offset => fromDate.AddDays(offset))
            .Select(date => new ActivityTrend
            {
                Date = date,
                InsertCount = logs.Count(x => x.PerformedAt.Date == date && x.Action == "INSERT"),
                UpdateCount = logs.Count(x => x.PerformedAt.Date == date && x.Action == "UPDATE"),
                DeleteCount = logs.Count(x => x.PerformedAt.Date == date && x.Action == "DELETE"),
                TotalCount = logs.Count(x => x.PerformedAt.Date == date)
            })
            .ToList();
    }

    public async Task<byte[]> ExportAuditLogsAsync(
        AuditLogFilterRequest filter,
        AuditExportFormat format,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsQueryable();
        query = ApplyFilters(query, filter);
        query = ApplySorting(query, filter.SortBy, filter.SortDescending);

        var logs = await query
            .Take(filter.ExportMaxRows)
            .Select(x => new AuditLogResponse
            {
                Id = x.Id,
                EntityName = x.EntityName,
                EntityId = x.EntityId,
                Action = x.Action,
                PropertyName = x.PropertyName,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                PerformedBy = x.PerformedBy,
                PerformedAt = x.PerformedAt,
                IpAddress = x.IpAddress,
                UserAgent = x.UserAgent
            })
            .ToListAsync(cancellationToken);

        return format switch
        {
            AuditExportFormat.Json => ExportToJson(logs),
            _ => ExportToCsv(logs)
        };
    }

    #region Private Methods

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogFilterRequest request)
    {
        if (!string.IsNullOrEmpty(request.EntityName))
            query = query.Where(x => x.EntityName == request.EntityName);

        if (!string.IsNullOrEmpty(request.EntityId))
            query = query.Where(x => x.EntityId == request.EntityId);

        if (!string.IsNullOrEmpty(request.Action))
            query = query.Where(x => x.Action == request.Action);

        if (!string.IsNullOrEmpty(request.PerformedBy))
            query = query.Where(x => x.PerformedBy.Contains(request.PerformedBy));

        if (request.FromDate.HasValue)
            query = query.Where(x => x.PerformedAt >= request.FromDate.Value);

        if (request.ToDate.HasValue)
            query = query.Where(x => x.PerformedAt <= request.ToDate.Value);

        if (!string.IsNullOrEmpty(request.PropertyName))
            query = query.Where(x => x.PropertyName != null && x.PropertyName.Contains(request.PropertyName));

        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(x =>
                x.EntityName.ToLower().Contains(term) ||
                x.EntityId.ToLower().Contains(term) ||
                x.PerformedBy.ToLower().Contains(term) ||
                (x.PropertyName != null && x.PropertyName.ToLower().Contains(term)) ||
                (x.OldValue != null && x.OldValue.ToLower().Contains(term)) ||
                (x.NewValue != null && x.NewValue.ToLower().Contains(term)));
        }

        if (request.EntityNames != null && request.EntityNames.Any())
            query = query.Where(x => request.EntityNames.Contains(x.EntityName));

        if (request.Actions != null && request.Actions.Any())
            query = query.Where(x => request.Actions.Contains(x.Action));

        if (!string.IsNullOrEmpty(request.IpAddress))
            query = query.Where(x => x.IpAddress == request.IpAddress);

        return query;
    }

    private static IQueryable<AuditLog> ApplySorting(IQueryable<AuditLog> query, string? sortBy, bool sortDescending)
    {
        query = sortBy?.ToLower() switch
        {
            "entityname" => sortDescending ? query.OrderByDescending(x => x.EntityName) : query.OrderBy(x => x.EntityName),
            "action" => sortDescending ? query.OrderByDescending(x => x.Action) : query.OrderBy(x => x.Action),
            "performedby" => sortDescending ? query.OrderByDescending(x => x.PerformedBy) : query.OrderBy(x => x.PerformedBy),
            "propertyname" => sortDescending ? query.OrderByDescending(x => x.PropertyName) : query.OrderBy(x => x.PropertyName),
            _ => sortDescending ? query.OrderByDescending(x => x.PerformedAt) : query.OrderBy(x => x.PerformedAt)
        };

        return query;
    }

    private static byte[] ExportToCsv(List<AuditLogResponse> logs)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Id,EntityName,EntityId,Action,PropertyName,OldValue,NewValue,PerformedBy,PerformedAt,IpAddress,UserAgent");

        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.Id}\",\"{EscapeCsv(log.EntityName)}\",\"{EscapeCsv(log.EntityId)}\",\"{EscapeCsv(log.Action)}\",\"{EscapeCsv(log.PropertyName)}\",\"{EscapeCsv(log.OldValue)}\",\"{EscapeCsv(log.NewValue)}\",\"{EscapeCsv(log.PerformedBy)}\",\"{log.PerformedAt:yyyy-MM-dd HH:mm:ss}\",\"{EscapeCsv(log.IpAddress)}\",\"{EscapeCsv(log.UserAgent)}\"");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static byte[] ExportToJson(List<AuditLogResponse> logs)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.SerializeToUtf8Bytes(logs, options);
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return value.Replace("\"", "\"\"").Replace("\n", " ").Replace("\r", " ");
    }

    #endregion
}
