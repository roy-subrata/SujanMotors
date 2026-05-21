using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Services;

/// <summary>
/// Delivery-only service. Knows nothing about sales orders, payments, or any business concept.
/// It checks channel settings, calls the provider, and logs the outcome.
/// Business logic (who to notify, what message) lives in domain event handlers.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AutoPartDbContext _db;
    private readonly IApplicationSettingsRepository _settings;
    private readonly ISmsProvider _sms;
    private readonly IEmailProvider _email;
    private readonly IWhatsAppProvider _whatsApp;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        AutoPartDbContext db,
        IApplicationSettingsRepository settings,
        ISmsProvider sms,
        IEmailProvider email,
        IWhatsAppProvider whatsApp,
        ILogger<NotificationService> logger)
    {
        _db = db;
        _settings = settings;
        _sms = sms;
        _email = email;
        _whatsApp = whatsApp;
        _logger = logger;
    }

    public async Task SendSmsAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        if (!await IsChannelEnabled("NOTIFICATION:SMS_ENABLED", cancellationToken)) return;
        await DeliverAsync("SMS", toPhone, message, ct => _sms.SendAsync(toPhone, message, ct), cancellationToken);
    }

    public async Task SendWhatsAppAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        if (!await IsChannelEnabled("NOTIFICATION:WHATSAPP_ENABLED", cancellationToken)) return;
        await DeliverAsync("WHATSAPP", toPhone, message, ct => _whatsApp.SendAsync(toPhone, message, ct), cancellationToken);
    }

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)

    {
        await DeliverAsync("EMAIL", toEmail, subject, ct => _email.SendAsync(toEmail, subject, htmlBody, ct), cancellationToken);
    }

    // ── private helpers ────────────────────────────────────────────────────

    private async Task DeliverAsync(
        string channel,
        string recipient,
        string logMessage,
        Func<CancellationToken, Task<bool>> sendFunc,
        CancellationToken cancellationToken)
    {
        var log = NotificationLog.Create(channel, recipient, logMessage);
        try
        {
            var sent = await sendFunc(cancellationToken);
            if (sent) log.MarkSent(); else log.MarkFailed("Provider returned failure");
        }
        catch (Exception ex)
        {
            log.MarkFailed(ex.Message);
            _logger.LogWarning(ex, "{Channel} send failed to {Recipient}", channel, recipient);
            await _db.NotificationLogs.AddAsync(log, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);
            throw;
        }

        await _db.NotificationLogs.AddAsync(log, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> IsChannelEnabled(string key, CancellationToken ct)
    {
        var val = await _settings.GetValueAsync(key, ct);
        return val?.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }
}
