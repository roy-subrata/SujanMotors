using AutoPartShop.Application.DTOs.AuditDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[HasPermission(Permissions.AuditView)]
public class AuditLogController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogController> _logger;

    public AuditLogController(IAuditLogService auditLogService, ILogger<AuditLogController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Get paginated audit logs with advanced filtering
    /// </summary>
    [HttpGet("list")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] AuditLogFilterRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetAuditLogsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit logs");
            return StatusCode(500, new { message = "An error occurred while fetching audit logs" });
        }
    }

    /// <summary>
    /// Get dashboard insights and statistics
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] AuditDashboardRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetDashboardAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit dashboard");
            return StatusCode(500, new { message = "An error occurred while fetching dashboard data" });
        }
    }

    /// <summary>
    /// Get complete timeline for a specific entity
    /// </summary>
    [HttpGet("entity/{entityName}/{entityId}/timeline")]
    public async Task<IActionResult> GetEntityTimeline(
        string entityName,
        string entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetEntityTimelineAsync(entityName, entityId, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity timeline");
            return StatusCode(500, new { message = "An error occurred while fetching entity timeline" });
        }
    }

    /// <summary>
    /// Compare entity state between two timestamps
    /// </summary>
    [HttpGet("entity/{entityName}/{entityId}/compare")]
    public async Task<IActionResult> CompareEntityStates(
        string entityName,
        string entityId,
        [FromQuery] DateTime? fromTimestamp = null,
        [FromQuery] DateTime? toTimestamp = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.CompareEntityStatesAsync(
                entityName, entityId, fromTimestamp, toTimestamp, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error comparing entity states");
            return StatusCode(500, new { message = "An error occurred while comparing entity states" });
        }
    }

    /// <summary>
    /// Get activity summary for a specific user
    /// </summary>
    [HttpGet("user/{userName}/summary")]
    public async Task<IActionResult> GetUserActivitySummary(
        string userName,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetUserActivitySummaryAsync(
                userName, fromDate, toDate, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user activity summary");
            return StatusCode(500, new { message = "An error occurred while fetching user activity" });
        }
    }

    /// <summary>
    /// Get activity trends for charts
    /// </summary>
    [HttpGet("trends")]
    public async Task<IActionResult> GetActivityTrends(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] string? entityName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var from = fromDate ?? DateTime.UtcNow.Date.AddDays(-30);
            var to = toDate ?? DateTime.UtcNow.Date;
            var result = await _auditLogService.GetActivityTrendsAsync(from, to, entityName, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting activity trends");
            return StatusCode(500, new { message = "An error occurred while fetching activity trends" });
        }
    }

    /// <summary>
    /// Get list of entities that have audit logs
    /// </summary>
    [HttpGet("entities")]
    public async Task<IActionResult> GetAuditedEntities(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetAuditedEntitiesAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audited entities");
            return StatusCode(500, new { message = "An error occurred while fetching entities" });
        }
    }

    /// <summary>
    /// Get list of all users who have performed actions
    /// </summary>
    [HttpGet("users")]
    public async Task<IActionResult> GetAuditUsers(CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _auditLogService.GetAuditUsersAsync(cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit users");
            return StatusCode(500, new { message = "An error occurred while fetching users" });
        }
    }

    /// <summary>
    /// Export audit logs to CSV or JSON
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportAuditLogs(
        [FromQuery] AuditLogFilterRequest filter,
        [FromQuery] AuditExportFormat format = AuditExportFormat.Csv,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var bytes = await _auditLogService.ExportAuditLogsAsync(filter, format, cancellationToken);

            var contentType = format switch
            {
                AuditExportFormat.Json => "application/json",
                _ => "text/csv"
            };

            var extension = format switch
            {
                AuditExportFormat.Json => "json",
                _ => "csv"
            };

            var fileName = $"audit_logs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{extension}";

            return File(bytes, contentType, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs");
            return StatusCode(500, new { message = "An error occurred while exporting audit logs" });
        }
    }
}
