using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Automatically marks ACTIVE warranties as EXPIRED once their WarrantyExpiryDate passes,
/// so warranty status stays accurate without any manual action. Runs once a day at a
/// configured local time (default 00:10). Config section:
///
///   "WarrantyExpiry": { "Enabled": true, "LocalTime": "00:10", "TzOffsetMinutes": 360 }
///
/// TzOffsetMinutes shifts UTC to the shop's local clock (360 = UTC+6, Bangladesh),
/// mirroring the tz handling used by the other scheduled services.
/// </summary>
public class WarrantyExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<WarrantyExpiryService> _logger;

    public WarrantyExpiryService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<WarrantyExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.GetValue("WarrantyExpiry:Enabled", true))
        {
            _logger.LogInformation("Automatic warranty expiry is disabled via configuration.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = DelayUntilNextRun();
            _logger.LogInformation("Next warranty expiry sweep in {Delay:hh\\:mm\\:ss}.", delay);

            try { await Task.Delay(delay, stoppingToken); }
            catch (OperationCanceledException) { return; }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<IWarrantyRegistrationRepository>();
                await SweepAsync(repository, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                // Never let a transient failure kill the loop — log and wait for the next scheduled run.
                _logger.LogError(ex, "Warranty expiry sweep failed; will retry at the next scheduled run.");
            }
        }
    }

    private async Task SweepAsync(IWarrantyRegistrationRepository repository, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var due = (await repository.GetDueForExpiryAsync(now, cancellationToken)).ToList();

        var expiredCount = 0;
        foreach (var warranty in due)
        {
            if (warranty.Status != WarrantyRegistrationStatus.ACTIVE || warranty.WarrantyExpiryDate >= now)
                continue;

            warranty.CheckAndUpdateExpiry();
            await repository.UpdateAsync(warranty, cancellationToken);
            expiredCount++;
        }

        if (expiredCount > 0)
        {
            _logger.LogInformation("Automatically expired {Count} warranty registrations.", expiredCount);
        }
    }

    private TimeSpan DelayUntilNextRun()
    {
        var tzOffsetMinutes = Math.Clamp(_config.GetValue("WarrantyExpiry:TzOffsetMinutes", 360), -840, 840);
        var tzShift = TimeSpan.FromMinutes(tzOffsetMinutes);

        if (!TimeOnly.TryParse(_config.GetValue<string>("WarrantyExpiry:LocalTime") ?? "00:10", out var runAt))
            runAt = new TimeOnly(0, 10);

        var nowLocal = DateTime.UtcNow + tzShift;
        var next = nowLocal.Date + runAt.ToTimeSpan();
        if (next <= nowLocal) next = next.AddDays(1);

        return next - nowLocal;
    }
}
