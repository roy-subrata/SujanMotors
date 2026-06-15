using System.Text.Json;
using AutoPartShop.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutoPartShop.Infrastructure.Services.Providers;

/// <summary>
/// SMS provider for sms.net.bd (https://api.sms.net.bd).
///
/// Send endpoint: POST/GET https://api.sms.net.bd/sendsms with api_key, msg, to.
/// A successful submission returns JSON with <c>"error": 0</c>; any non-zero
/// <c>error</c> is a failure (human-readable reason in <c>msg</c>).
///
/// Config (case-insensitive):
///   Sms:ApiKey    – required; the account API key
///   Sms:SenderId  – optional approved sender/masking id
/// Leave Sms:ApiKey blank to disable (sends become a logged no-op).
/// </summary>
public class SmsNetBdSmsProvider : ISmsProvider
{
    private const string SendUrl = "https://api.sms.net.bd/sendsms";

    private readonly HttpClient _http;
    private readonly string? _apiKey;
    private readonly string? _senderId;
    private readonly ILogger<SmsNetBdSmsProvider> _logger;

    public SmsNetBdSmsProvider(HttpClient http, IConfiguration config, ILogger<SmsNetBdSmsProvider> logger)
    {
        _http = http;
        _logger = logger;
        _apiKey = config["Sms:ApiKey"];
        _senderId = config["Sms:SenderId"];
    }

    public async Task<bool> SendAsync(string toPhone, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogWarning("sms.net.bd not configured (Sms:ApiKey missing) — skipping send to {Phone}", toPhone);
            return false;
        }

        // sms.net.bd expects a local/international number without a leading '+'.
        var to = (toPhone ?? string.Empty).Trim().TrimStart('+').Replace(" ", "");
        if (string.IsNullOrWhiteSpace(to))
        {
            _logger.LogWarning("sms.net.bd send skipped — empty recipient");
            return false;
        }

        var form = new Dictionary<string, string>
        {
            ["api_key"] = _apiKey!,
            ["msg"] = message ?? string.Empty,
            ["to"] = to
        };
        if (!string.IsNullOrWhiteSpace(_senderId))
            form["sender_id"] = _senderId!;

        try
        {
            using var content = new FormUrlEncodedContent(form);
            using var resp = await _http.PostAsync(SendUrl, content, cancellationToken);
            var body = await resp.Content.ReadAsStringAsync(cancellationToken);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("sms.net.bd HTTP {Status} sending to {Phone}: {Body}", (int)resp.StatusCode, to, body);
                return false;
            }

            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            // "error" can come back as a number or a numeric string; 0 means success.
            var error = -1;
            if (root.TryGetProperty("error", out var e))
            {
                error = e.ValueKind == JsonValueKind.Number ? e.GetInt32()
                    : (e.ValueKind == JsonValueKind.String && int.TryParse(e.GetString(), out var parsed) ? parsed : -1);
            }

            if (error == 0)
                return true;

            var msg = root.TryGetProperty("msg", out var m) ? m.GetString() : null;
            _logger.LogWarning("sms.net.bd send to {Phone} failed (error {Error}): {Msg}", to, error, msg);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "sms.net.bd send to {Phone} threw", to);
            return false;
        }
    }
}
