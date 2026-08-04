namespace AutoPartShop.Application.DTOs.Notification;

using AutoPartShop.Domain.Enums;

/// <summary>
/// Staff-triggered request to remind a customer about their outstanding due.
/// </summary>
public class SendPaymentReminderRequest
{
    /// <summary>Delivery channel: SMS (default), WHATSAPP or EMAIL.</summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.SMS;

    /// <summary>Optional override message. When empty a default due reminder is composed.</summary>
    public string? Message { get; set; }
}
