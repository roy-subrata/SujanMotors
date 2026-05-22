using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Events;

/// <summary>
/// Reacts to CustomerPaymentDueEvent — completely independent of SalesOrder.
/// Uses the same INotificationService but builds a payment-specific message.
/// </summary>
public class CustomerPaymentDueNotificationHandler : IDomainEventHandler<CustomerPaymentDueEvent>
{
    private readonly INotificationService _notifications;
    private readonly ILogger<CustomerPaymentDueNotificationHandler> _logger;

    public CustomerPaymentDueNotificationHandler(
        INotificationService notifications,
        ILogger<CustomerPaymentDueNotificationHandler> logger)
    {
        _notifications = notifications;
        _logger = logger;
    }

    public async Task HandleAsync(CustomerPaymentDueEvent evt, CancellationToken cancellationToken = default)
    {
        var daysUntilDue = (evt.DueDate.Date - DateTime.UtcNow.Date).Days;
        var duePhrase = daysUntilDue == 0 ? "today"
            : daysUntilDue > 0            ? $"in {daysUntilDue} day(s)"
            :                               $"{Math.Abs(daysUntilDue)} day(s) ago";

        var message =
            $"Dear {evt.CustomerName}, your invoice {evt.InvoiceNumber} " +
            $"for {evt.Currency} {evt.AmountDue:F2} is due {duePhrase}. " +
            $"Please contact us to settle your balance.";

        if (!string.IsNullOrWhiteSpace(evt.CustomerPhone))
        {
            try
            {
                await _notifications.SendSmsAsync(evt.CustomerPhone, message, cancellationToken);

                await _notifications.SendWhatsAppAsync(evt.CustomerPhone, message, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payment due notification failed for customer {CustomerId}", evt.CustomerId);
            }
        }
    }
}
