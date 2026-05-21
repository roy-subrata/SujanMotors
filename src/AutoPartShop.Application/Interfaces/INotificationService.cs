namespace AutoPartShop.Application.Interfaces;

/// <summary>
/// Pure delivery service — sends messages to recipients.
/// Has no knowledge of the business events that triggered them.
/// Handlers (IDomainEventHandler) are responsible for deciding what to send and to whom.
/// </summary>
public interface INotificationService
{
    Task SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default);

    Task SendWhatsAppAsync(string toPhone, string message, CancellationToken cancellationToken = default);

    Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default);
}
