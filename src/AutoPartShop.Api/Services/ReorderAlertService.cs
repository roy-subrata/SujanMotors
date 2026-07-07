namespace AutoPartShop.Api.Services;

/// <summary>
/// Runs the reorder-level scan once a day at a configured local time and broadcasts
/// a consolidated low-stock alert to staff (SignalR bell). Config section:
///
///   "ReorderAlerts": { "Enabled": true, "LocalTime": "09:30", "TzOffsetMinutes": 360 }
///
/// TzOffsetMinutes shifts UTC to the shop's local clock (360 = UTC+6, Bangladesh),
/// mirroring the tz handling in CashBookController.
/// </summary>
public class ReorderAlertService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<ReorderAlertService> _logger;

    public ReorderAlertService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<ReorderAlertService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("ReorderAlerts:Enabled", true))
        {
            _logger.LogInformation("Reorder alerts are disabled via configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun();
            _logger.LogInformation("Next reorder alert scan in {Delay:hh\\:mm\\:ss}.", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { return; }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<ReorderAlertScanner>();
                await scanner.ScanAndBroadcastAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let a transient failure kill the loop — log and wait for the next scheduled run.
                _logger.LogError(ex, "Reorder alert scan failed; will retry at the next scheduled run.");
            }
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var tzOffsetMinutes = Math.Clamp(_config.GetValue("ReorderAlerts:TzOffsetMinutes", 360), -840, 840);
        var tzShift = TimeSpan.FromMinutes(tzOffsetMinutes);

        if (!TimeOnly.TryParse(_config.GetValue<string>("ReorderAlerts:LocalTime") ?? "09:30", out var runAt))
            runAt = new TimeOnly(9, 30);

        var nowLocal = DateTime.UtcNow + tzShift;
        var next = nowLocal.Date + runAt.ToTimeSpan();
        if (next <= nowLocal) next = next.AddDays(1);

        return next - nowLocal;
    }
}
