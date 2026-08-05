using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Hubs;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace AutoPartShop.Api.Services;

public class SignalRSaleEventBroadcaster : ISaleEventBroadcaster
{
    private readonly IHubContext<SaleNotificationHub> _hub;
    private readonly IApplicationSettingsRepository _settings;
    private readonly AutoPartDbContext _db;
    private readonly ILogger<SignalRSaleEventBroadcaster> _logger;

    public SignalRSaleEventBroadcaster(
        IHubContext<SaleNotificationHub> hub,
        IApplicationSettingsRepository settings,
        AutoPartDbContext db,
        ILogger<SignalRSaleEventBroadcaster> logger)
    {
        _hub = hub;
        _settings = settings;
        _db = db;
        _logger = logger;
    }

    public async Task BroadcastAsync(SaleNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        var rolesValue = await _settings.GetValueAsync("NOTIFICATION:SIGNALR_ROLES", cancellationToken);

        IClientProxy target;
        if (string.IsNullOrWhiteSpace(rolesValue))
        {
            target = _hub.Clients.Group("staff"); // no filter → all staff
        }
        else
        {
            var groups = rolesValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(r => $"role:{r}")
                .ToList();
            target = _hub.Clients.Groups(groups);
        }

        await target.SendAsync("ReceiveSaleNotification", evt, cancellationToken);

        // Persist a staff inbox notification so the sale survives a refresh and can be acted
        // on from the Notifications inbox (the SignalR broadcast is transient by design).
        await PersistInboxNotificationAsync(evt, cancellationToken);
    }

    private async Task PersistInboxNotificationAsync(SaleNotificationEvent evt, CancellationToken cancellationToken)
    {
        try
        {
            var inbox = InboxNotification.Create(
                type: "SALE",
                title: $"New Sale — {evt.SONumber}",
                message: $"{evt.CustomerName} · {evt.SaleChannel} · {evt.Currency} {evt.GrandTotal:F2}",
                routerLink: "/sales/sales-orders/view",
                queryParamsJson: JsonSerializer.Serialize(new { id = evt.SalesOrderId }),
                createdBy: string.IsNullOrWhiteSpace(evt.CreatedBy) ? "System" : evt.CreatedBy);

            _db.InboxNotifications.Add(inbox);
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Persistence is best-effort — a failure must never break the real-time broadcast path.
            _logger.LogError(ex, "Failed to persist sale inbox notification.");
        }
    }
}
