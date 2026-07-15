namespace AutoPartShop.Api.Services;

using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;

/// <summary>
/// Runs the daily database backup at the admin-configured local time.
///
/// Unlike ReorderAlertService (which sleeps until the next run), this polls every few minutes
/// and re-reads the BACKUP:* application settings each cycle, so schedule changes made in the
/// admin UI take effect without a restart. It also self-heals: if the app was down at the
/// scheduled time, the backup runs on the next wake ("has a scheduled backup run since local
/// midnight?" is the once-per-day guard).
///
/// Resilience mirrors ReorderAlertService: every iteration is wrapped in a catch-all so a
/// failed backup (recorded as Failed on its BackupRecord) never kills the loop.
/// </summary>
public class BackupSchedulerService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackupSchedulerService> _logger;

    public BackupSchedulerService(IServiceScopeFactory scopeFactory, ILogger<BackupSchedulerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Small startup delay so migrations/seeding finish before the first settings read
        try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
        catch (OperationCanceledException) { return; }

        try
        {
            await FailInterruptedRunsAsync(stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean up interrupted backup records at startup.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunIfDueAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let a transient failure kill the loop — log and check again next cycle.
                _logger.LogError(ex, "Backup scheduler cycle failed; will retry at the next poll.");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    /// <summary>
    /// Any record still Pending/Running at startup belongs to a run that was killed mid-flight
    /// (app restart/crash) — mark it Failed so it doesn't show as running forever.
    /// </summary>
    private async Task FailInterruptedRunsAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var backupRepository = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();

        foreach (var record in await backupRepository.GetInProgressAsync(stoppingToken))
        {
            record.MarkFailed("Interrupted by an application restart before it completed.");
            await backupRepository.UpdateAsync(record, stoppingToken);
            _logger.LogWarning("Marked interrupted backup {FileName} as Failed.", record.FileName);
        }
    }

    private async Task RunIfDueAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IApplicationSettingsRepository>();

        var enabledValue = await settings.GetValueAsync("BACKUP:ENABLED", stoppingToken);
        if (!bool.TryParse(enabledValue, out var enabled) || !enabled)
            return;

        var tzValue = await settings.GetValueAsync("BACKUP:TZ_OFFSET_MINUTES", stoppingToken);
        var tzOffsetMinutes = int.TryParse(tzValue, out var tz) ? Math.Clamp(tz, -840, 840) : 360;
        var tzShift = TimeSpan.FromMinutes(tzOffsetMinutes);

        var timeValue = await settings.GetValueAsync("BACKUP:LOCAL_TIME", stoppingToken);
        if (!TimeOnly.TryParse(timeValue ?? "02:00", out var runAt))
        {
            _logger.LogWarning("Invalid BACKUP:LOCAL_TIME value '{Value}'; falling back to 02:00.", timeValue);
            runAt = new TimeOnly(2, 0);
        }

        var nowLocal = DateTime.UtcNow + tzShift;
        if (nowLocal.TimeOfDay < runAt.ToTimeSpan())
            return;

        // At most one scheduled backup per local day
        var localMidnightUtc = nowLocal.Date - tzShift;
        var backupRepository = scope.ServiceProvider.GetRequiredService<IBackupRecordRepository>();
        if (await backupRepository.HasScheduledRunSinceAsync(localMidnightUtc, stoppingToken))
            return;

        _logger.LogInformation("Scheduled backup is due (local time {LocalTime:HH:mm}); starting.", nowLocal);

        var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
        var recordId = await backupService.CreateBackupRecordAsync(
            BackupRecord.TriggerTypes.Scheduled, "System", stoppingToken);

        // ExecuteBackupAsync never throws — failures are recorded on the BackupRecord
        await backupService.ExecuteBackupAsync(recordId, stoppingToken);
    }
}
