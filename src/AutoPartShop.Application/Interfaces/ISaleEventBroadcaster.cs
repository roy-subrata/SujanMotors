using AutoPartShop.Application.DTOs.Notification;

namespace AutoPartShop.Application.Interfaces;

/// <summary>Decouples NotificationService from the SignalR hub so Infrastructure has no web dependency.</summary>
public interface ISaleEventBroadcaster
{
    Task BroadcastAsync(SaleNotificationEvent evt, CancellationToken cancellationToken = default);
}
