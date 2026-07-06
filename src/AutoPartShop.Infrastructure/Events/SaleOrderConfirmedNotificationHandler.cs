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

        if (!string.IsNullOrWhiteSpace(evt.CustomerEmail))
        {
            var subject = $"Order Confirmed — {evt.SONumber}";
            var htmlBody = $"""
                <div style="font-family:Arial,sans-serif;max-width:600px;margin:0 auto;padding:24px;color:#1e293b">
                  <h2 style="color:#1e3a8a;margin-bottom:4px">Order Confirmed</h2>
                  <p style="color:#64748b;margin-top:0">Order #{evt.SONumber}</p>
                  <hr style="border:none;border-top:1px solid #e2e8f0;margin:16px 0"/>
                  <p>Hi {evt.CustomerName},</p>
                  <p>Your order has been confirmed. Here is your summary:</p>
                  <table style="width:100%;border-collapse:collapse;margin:16px 0">
                    <tr>
                      <td style="padding:8px 0;color:#64748b">Order Number</td>
                      <td style="padding:8px 0;font-weight:bold">{evt.SONumber}</td>
                    </tr>
                    <tr>
                      <td style="padding:8px 0;color:#64748b">Total Amount</td>
                      <td style="padding:8px 0;font-weight:bold;color:#15803d">{evt.Currency} {evt.GrandTotal:F2}</td>
                    </tr>
                  </table>
                  <hr style="border:none;border-top:1px solid #e2e8f0;margin:16px 0"/>
                  <p style="color:#64748b;font-size:13px">Thank you for your business. Please keep this email as your digital receipt.</p>
                </div>
                """;
            await TrySendAsync(() =>
                _notifications.SendEmailAsync(evt.CustomerEmail, subject, htmlBody, cancellationToken));
        }

        // Broadcast real-time alert to all connected staff
        await TrySendAsync(() =>
            _broadcaster.BroadcastAsync(new SaleNotificationEvent
            {
                SalesOrderId = evt.SalesOrderId,
                SONumber = evt.SONumber,
                CustomerName = evt.CustomerName,
                GrandTotal = evt.GrandTotal,
                Currency = evt.Currency,
                SaleChannel = evt.Channel,
                OccurredAt = evt.OccurredAt,
                CreatedBy = evt.CreatedBy
            }, cancellationToken));
    }

    private async Task TrySendAsync(Func<Task> action)
    {
        try { await action(); }
        catch (Exception ex) { _logger.LogWarning(ex, "Notification step failed in {Handler}", nameof(SaleOrderConfirmedNotificationHandler)); }
    }
}
