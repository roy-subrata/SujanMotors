using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace AutoPartShop.Api.Services;

public class SignalRSaleEventBroadcaster : ISaleEventBroadcaster
{
    private readonly IHubContext<SaleNotificationHub> _hub;
    private readonly IApplicationSettingsRepository _settings;

    public SignalRSaleEventBroadcaster(
        IHubContext<SaleNotificationHub> hub,
        IApplicationSettingsRepository settings)
    {
        _hub = hub;
        _settings = settings;
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
    }
}
