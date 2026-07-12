using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AutoPartShop.Api.Services;

public class SignalRReorderAlertBroadcaster : IReorderAlertBroadcaster
{
    private readonly IHubContext<SaleNotificationHub> _hub;
    private readonly IApplicationSettingsRepository _settings;

    public SignalRReorderAlertBroadcaster(
        IHubContext<SaleNotificationHub> hub,
        IApplicationSettingsRepository settings)
    {
        _hub = hub;
        _settings = settings;
    }

    public async Task BroadcastAsync(ReorderAlertEvent evt, CancellationToken cancellationToken = default)
    {
        // Same audience filter as sale notifications: NOTIFICATION:SIGNALR_ROLES limits
        // which role groups receive staff broadcasts; empty means everyone connected.
        var rolesValue = await _settings.GetValueAsync("NOTIFICATION:SIGNALR_ROLES", cancellationToken);

        IClientProxy target;
        if (string.IsNullOrWhiteSpace(rolesValue))
        {
            target = _hub.Clients.Group("staff");
        }
        else
        {
            var groups = rolesValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(r => $"role:{r}")
                .ToList();
            target = _hub.Clients.Groups(groups);
        }

        await target.SendAsync("ReceiveReorderAlert", evt, cancellationToken);
    }
}
