using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Services;

/// <summary>
/// Promotes scheduled selling prices into the denormalized
/// <c>Product.SellingPrice</c> / <c>ProductVariant.SellingPrice</c> columns once their
/// <see cref="Domain.Entities.ProductVariantPriceHistory.StartDate"/> arrives.
///
/// The "Set Price" flow only syncs the denormalized column when the price is effective
/// immediately; a future-dated price would otherwise never become visible to the catalog,
/// Quick Sale, or ecommerce because those read the denormalized column rather than the
/// price-history schedule. This service closes that gap by running shortly after startup
/// and then hourly, applying whichever history row is active today.
/// </summary>
public class ScheduledPriceSyncService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);
    private static readonly TimeSpan StartupDelay = TimeSpan.FromSeconds(30);
    private const string SystemUser = "system:price-scheduler";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ScheduledPriceSyncService> _logger;

    public ScheduledPriceSyncService(
        IServiceScopeFactory scopeFactory,
        ILogger<ScheduledPriceSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the app finish startup (and dev-time migrations) before the first run.
        try { await Task.Delay(StartupDelay, stoppingToken); }
        catch (OperationCanceledException) { return; }

        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                await SyncAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Never let a transient failure kill the loop — log and retry next tick.
                _logger.LogError(ex, "Scheduled price sync run failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// <summary>
    /// Reconciles every part/variant whose currently-active scheduled price differs from
    /// its denormalized selling price. Exposed (internal) so it can be unit-tested directly.
    /// </summary>
    internal async Task<int> SyncAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutoPartDbContext>();
        var today = DateTime.UtcNow.Date;

        // All history rows in effect today; the newest StartDate per scope wins
        // (SetNewPriceAsync closes prior rows, so this is normally one row per scope).
        var activeRows = await db.ProductVariantPriceHistories
            .Where(x => !x.Isdeleted
                        && x.StartDate <= today
                        && (x.EndDate == null || x.EndDate >= today))
            .ToListAsync(cancellationToken);

        var current = activeRows
            .GroupBy(x => new { x.PartId, x.ProductVariantId })
            .Select(g => g.OrderByDescending(x => x.StartDate).First())
            .ToList();

        var updated = 0;

        foreach (var price in current)
        {
            if (price.ProductVariantId.HasValue)
            {
                var variant = await db.ProductVariants
                    .FirstOrDefaultAsync(v => v.Id == price.ProductVariantId.Value && !v.Isdeleted, cancellationToken);
                if (variant != null && variant.SellingPrice != price.SellingPrice)
                {
                    variant.UpdateSellingPrice(price.SellingPrice, price.Currency);
                    variant.ModifiedBy = SystemUser;
                    updated++;
                }
            }
            else
            {
                var part = await db.Parts
                    .FirstOrDefaultAsync(p => p.Id == price.PartId && !p.Isdeleted, cancellationToken);
                if (part != null && part.SellingPrice != price.SellingPrice)
                {
                    part.UpdateSellingPrice(price.SellingPrice, price.Currency);
                    part.ModifiedBy = SystemUser;
                    updated++;
                }
            }
        }

        if (updated > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Scheduled price sync promoted {Count} price(s) to the catalog.", updated);
        }

        return updated;
    }
}