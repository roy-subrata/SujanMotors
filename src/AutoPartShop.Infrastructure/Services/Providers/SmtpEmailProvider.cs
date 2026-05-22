using AutoPartShop.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace AutoPartShop.Infrastructure.Services.Providers;

public class SmtpEmailProvider : IEmailProvider
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly string _fromEmail;
    private readonly string _fromName;
    private readonly SecureSocketOptions _socketOptions;
    private readonly bool _configured;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(IConfiguration config, ILogger<SmtpEmailProvider> logger)
    {
        _logger    = logger;
        _host      = config["Smtp:Host"] ?? "";
        _port      = int.TryParse(config["Smtp:Port"], out var p) ? p : 587;
        _username  = config["Smtp:Username"] ?? "";
        _password  = config["Smtp:Password"] ?? "";
        _fromEmail = config["Smtp:FromEmail"] ?? "";
        _fromName  = config["Smtp:FromName"] ?? "SujanMotors";

        _socketOptions = config["Smtp:UseSsl"]?.Equals("true", StringComparison.OrdinalIgnoreCase) == true
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTlsWhenAvailable;

        _configured = !string.IsNullOrWhiteSpace(_host) && !string.IsNullOrWhiteSpace(_fromEmail);
    }

    public async Task<bool> SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        if (!_configured)
        {
            _logger.LogWarning("SMTP not configured — skipping email to {Email}", toEmail);
            return false;
        }

        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(_fromName, _fromEmail));
        msg.To.Add(MailboxAddress.Parse(toEmail));
        msg.Subject = subject;
        msg.Body    = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(_host, _port, _socketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(_username))
            await client.AuthenticateAsync(_username, _password, cancellationToken);
        await client.SendAsync(msg, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);
        return true;
    }
}
