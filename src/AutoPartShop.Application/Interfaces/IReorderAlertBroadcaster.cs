using AutoPartShop.Application.DTOs.Notification;

namespace AutoPartShop.Application.Interfaces;

/// <summary>Decouples the reorder-alert scan from the SignalR hub so Application/Infrastructure have no web dependency.</summary>
public interface IReorderAlertBroadcaster
{
    Task BroadcastAsync(ReorderAlertEvent evt, CancellationToken cancellationToken = default);
}
