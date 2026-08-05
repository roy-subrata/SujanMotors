using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Finds stock levels at or below their reorder level and broadcasts one consolidated
/// alert to staff via SignalR. Matches the Low Stock tab query (available &lt;= ReorderLevel)
/// but additionally requires ReorderLevel &gt; 0 — reorder alerting is opt-in per item, so
/// parts nobody configured a threshold for never generate noise.
/// </summary>
public class ReorderAlertScanner(
    AutoPartDbContext _db,
    IReorderAlertBroadcaster _broadcaster,
    ILogger<ReorderAlertScanner> _logger)
{
    /// <summary>Cap the broadcast payload; ItemCount still reports the full total.</summary>
    private const int MaxItemsInPayload = 20;

    /// <summary>Runs the scan and broadcasts if anything needs reordering. Returns null when nothing does.</summary>
    public async Task<ReorderAlertEvent?> ScanAndBroadcastAsync(CancellationToken cancellationToken = default)
    {
        var levels = await _db.StockLevels
            .Where(x => !x.Isdeleted && x.IsActive && x.ReorderLevel > 0
                     && (x.QuantityOnHand - x.QuantityReserved) <= x.ReorderLevel)
            .Include(x => x.Part)
            .Include(x => x.Variant)
            .Include(x => x.Warehouse)
            .AsNoTracking()
            .OrderBy(x => x.QuantityOnHand - x.QuantityReserved)
            .ToListAsync(cancellationToken);

        if (levels.Count == 0)
        {
            _logger.LogInformation("Reorder alert scan: no items at/below reorder level.");
            return null;
        }

        var evt = new ReorderAlertEvent
        {
            ItemCount = levels.Count,
            OccurredAt = DateTime.UtcNow,
            Items = levels.Take(MaxItemsInPayload).Select(x => new ReorderAlertItem
            {
                StockLevelId = x.Id,
                PartId = x.PartId,
                VariantId = x.VariantId,
                PartName = x.Variant != null && x.Part != null
                    ? $"{x.Part.Name} - {x.Variant.Name}"
                    : x.Part?.Name ?? "Unknown part",
                Sku = x.Variant?.SKU ?? x.Part?.SKU,
                WarehouseName = x.Warehouse?.Name ?? string.Empty,
                QuantityAvailable = x.QuantityAvailable,
                ReorderLevel = x.ReorderLevel,
                ReorderQuantity = x.ReorderQuantity
            }).ToList()
        };

        await _broadcaster.BroadcastAsync(evt, cancellationToken);

        // Persist a staff inbox notification so the alert survives a refresh and can be acted on
        // from the Notifications inbox (the SignalR broadcast is transient by design).
        await PersistInboxNotificationAsync(evt, cancellationToken);

        _logger.LogInformation("Reorder alert broadcast: {Count} item(s) at/below reorder level.", evt.ItemCount);
        return evt;
    }

    private async Task PersistInboxNotificationAsync(ReorderAlertEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            var plural = evt.ItemCount == 1 ? "item" : "items";
            var topNames = string.Join(", ", evt.Items.Take(3).Select(i => i.PartName));
            var more = evt.ItemCount > 3 ? $" +{evt.ItemCount - 3} more" : string.Empty;

            var inbox = InboxNotification.Create(
                type: "REORDER_ALERT",
                title: $"Low Stock — {evt.ItemCount} {plural} to reorder",
                message: $"{topNames}{more}",
                routerLink: "/inventory/stock",
                queryParamsJson: "{\"tab\":\"low\"}",
                payloadJson: JsonSerializer.Serialize(new
                {
                    itemCount = evt.ItemCount,
                    occurredAt = evt.OccurredAt,
                    items = evt.Items.Select(i => new
                    {
                        i.StockLevelId,
                        i.PartId,
                        i.VariantId,
                        i.PartName,
                        i.Sku,
                        i.WarehouseName,
                        i.QuantityAvailable,
                        i.ReorderLevel,
                        i.ReorderQuantity
                    })
                }));

            _db.InboxNotifications.Add(inbox);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Persistence is best-effort — a failure must never break the real-time broadcast path.
            _logger.LogError(ex, "Failed to persist reorder alert inbox notification.");
        }
    }
}
