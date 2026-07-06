using AutoPartShop.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace AutoPartShop.Infrastructure.Services.Providers;

public class TwilioWhatsAppProvider : IWhatsAppProvider
{
    private readonly string? _msgServiceSid;
    private readonly bool _configured;
    private readonly ILogger<TwilioWhatsAppProvider> _logger;

    public TwilioWhatsAppProvider(IConfiguration config, ILogger<TwilioWhatsAppProvider> logger)
    {
        _logger = logger;
        var sid = config["Twilio:AccountSid"];
        var token = config["Twilio:AuthToken"];
        _msgServiceSid = config["Twilio:WhatsAppMsgServiceSid"];

        if (!string.IsNullOrWhiteSpace(sid) && !string.IsNullOrWhiteSpace(token))
        {
            TwilioClient.Init(sid, token);
            _configured = true;
        }
    }

    public async Task<bool> SendAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        if (!_configured)
        {
            _logger.LogWarning("Twilio WhatsApp not configured — skipping send to {Phone}", toPhone);
            return false;
        }

        var to = toPhone.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase)
            ? toPhone
            : $"whatsapp:{toPhone}";

        var msg = await MessageResource.CreateAsync(
            to: new PhoneNumber(to),
            messagingServiceSid: _msgServiceSid,
            body: message);

        return msg.Status != MessageResource.StatusEnum.Failed
            && msg.Status != MessageResource.StatusEnum.Undelivered;
    }
}
