using AutoPartShop.Application.DTOs.Notification;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Events;

/// <summary>
/// Reacts to SaleOrderConfirmedEvent.
/// Builds the customer message from event data (no DB query needed) and
/// delegates delivery to INotificationService and ISaleEventBroadcaster.
/// </summary>
public class SaleOrderConfirmedNotificationHandler : IDomainEventHandler<SaleOrderConfirmedEvent>
{
    private readonly INotificationService _notifications;
    private readonly ISaleEventBroadcaster _broadcaster;
    private readonly ILogger<SaleOrderConfirmedNotificationHandler> _logger;

    public SaleOrderConfirmedNotificationHandler(
        INotificationService notifications,
        ISaleEventBroadcaster broadcaster,
        ILogger<SaleOrderConfirmedNotificationHandler> logger)
    {
        _notifications = notifications;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    public async Task HandleAsync(SaleOrderConfirmedEvent evt, CancellationToken cancellationToken = default)
    {
        var customerMessage =
            $"Hi {evt.CustomerName}, your order {evt.SONumber} for " +
            $"{evt.Currency} {evt.GrandTotal:F2} has been confirmed. Thank you!";

        // Send to customer — INotificationService checks if each channel is enabled
        if (!string.IsNullOrWhiteSpace(evt.CustomerPhone))
        {
            await TrySendAsync(() =>
                _notifications.SendSmsAsync(evt.CustomerPhone, customerMessage, cancellationToken));

            await TrySendAsync(() =>
                _notifications.SendWhatsAppAsync(evt.CustomerPhone, customerMessage, cancellationToken));
        }

        // Broadcast real-time alert to all connected staff
        await TrySendAsync(() =>
            _broadcaster.BroadcastAsync(new SaleNotificationEvent
            {
                SalesOrderId = evt.SalesOrderId,
                SONumber     = evt.SONumber,
                CustomerName = evt.CustomerName,
                GrandTotal   = evt.GrandTotal,
                Currency     = evt.Currency,
                SaleChannel  = evt.Channel,
                OccurredAt   = evt.OccurredAt,
                CreatedBy    = evt.CreatedBy
            }, cancellationToken));
    }

    private async Task TrySendAsync(Func<Task> action)
    {
        try   { await action(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Notification step failed in {Handler}", nameof(SaleOrderConfirmedNotificationHandler)); }
    }
}
