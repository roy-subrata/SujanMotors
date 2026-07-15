namespace AutoPartShop.Api.Controllers;

using AutoPartShop.Api.Authorization;
using AutoPartShop.Application.DTOs.BackupDtos;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Services.Backup;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Database backup management: history, manual backup, download, restore, Drive status.
/// Schedule settings live in the ApplicationSettings "BACKUP" category.
/// </summary>
[ApiController]
[Route("api/v1/backups")]
[HasPermission(Permissions.BackupsManage)]
public class BackupsController(
    IBackupService backupService,
    BackupCoordinator coordinator,
    IServiceScopeFactory scopeFactory,
    ILogger<BackupsController> logger) : ControllerBase
{
    private readonly IBackupService _backupService = backupService;
    private readonly BackupCoordinator _coordinator = coordinator;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<BackupsController> _logger = logger;

    /// <summary>
    /// Paged backup history, newest first.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var result = await _backupService.GetHistoryAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Trigger a manual backup. Returns 202 with the record id; poll history for completion.
    /// </summary>
    [HttpPost("run")]
    public async Task<IActionResult> Run(CancellationToken cancellationToken)
    {
        if (_coordinator.CurrentOperation != null)
            return Conflict(new { message = $"A {_coordinator.CurrentOperation} operation is already in progress." });

        var initiatedBy = User.Identity?.Name ?? "Admin";
        var recordId = await _backupService.CreateBackupRecordAsync(
            BackupRecord.TriggerTypes.Manual, initiatedBy, cancellationToken);

        // Run in the background on a fresh scope — the request scope is disposed after returning
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<IBackupService>();
                await service.ExecuteBackupAsync(recordId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Background execution of manual backup {RecordId} failed", recordId);
            }
        });

        return Accepted(new { id = recordId });
    }

    /// <summary>
    /// Restore the database from the given backup. Requires confirmation = "RESTORE".
    /// A safety backup of the current state is taken first. Runs synchronously.
    /// </summary>
    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(Guid id, [FromBody] RestoreBackupRequest request, CancellationToken cancellationToken)
    {
        if (request?.Confirmation != "RESTORE")
            return BadRequest(new { message = "Type RESTORE in the confirmation field to restore this backup." });

        if (_coordinator.CurrentOperation != null)
            return Conflict(new { message = $"A {_coordinator.CurrentOperation} operation is already in progress." });

        var initiatedBy = User.Identity?.Name ?? "Admin";

        try
        {
            await _backupService.RestoreAsync(id, initiatedBy, cancellationToken);
            return Ok(new { message = "Database restored successfully. A pre-restore safety backup was recorded." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download the .bak file (re-downloaded from Google Drive if it no longer exists locally).
    /// </summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var (filePath, fileName) = await _backupService.GetBackupFileAsync(id, cancellationToken);
            return PhysicalFile(filePath, "application/octet-stream", fileName, enableRangeProcessing: true);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Validate the Google Drive service account and configured folder.
    /// </summary>
    [HttpGet("drive-status")]
    public async Task<IActionResult> DriveStatus(CancellationToken cancellationToken)
    {
        var status = await _backupService.GetDriveStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>
    /// The operation currently in progress ("backup" / "restore"), or null when idle.
    /// </summary>
    [HttpGet("status")]
    public IActionResult Status() => Ok(new { currentOperation = _coordinator.CurrentOperation });
}
