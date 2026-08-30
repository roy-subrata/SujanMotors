using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using AutoPartShop.Api.Common;
using Microsoft.AspNetCore.RateLimiting;

namespace AutoPartShop.Api.Middleware;

/// <summary>
/// Request rate limiting.
///
/// <para>Three tiers, because one limit cannot serve both a cashier ringing up sales and an
/// anonymous caller guessing passwords:</para>
/// <list type="bullet">
/// <item><b>auth</b> — the credential endpoints. Tight and per-IP. Identity lockout already caps
/// guesses against a single account; this caps the spray across many accounts.</item>
/// <item><b>session</b> — token renewal and sign-out. Separate from <b>auth</b>, and more
/// generous, because these are not guessable: a refresh token is 256 bits of CSPRNG output and
/// replaying one revokes the whole family, so a tight limit buys almost nothing. Meanwhile the
/// shop NATs every till through one address, and staff who signed in together expire together —
/// a burst of simultaneous renewals must not be mistaken for an attack.</item>
/// <item><b>public</b> — the handful of <c>[AllowAnonymous]</c> data endpoints (invoice print
/// data, file downloads, public settings). Per-IP and moderate: enough for a print page and its
/// assets, not enough to enumerate.</item>
/// <item><b>global</b> — everything else, partitioned per authenticated user. Deliberately
/// generous; it exists to stop a looping client, not to shape normal POS traffic.</item>
/// </list>
///
/// <para>Every tier can be retuned from configuration, and the whole thing switched off with
/// <c>RateLimiting:Enabled=false</c> if it ever misfires in production.</para>
/// </summary>
public static class RateLimiting
{
    /// <summary>Credential endpoints: staff login, customer login and registration.</summary>
    public const string AuthPolicy = "auth";

    /// <summary>Token renewal and sign-out.</summary>
    public const string SessionPolicy = "session";

    /// <summary>Anonymous data endpoints.</summary>
    public const string PublicPolicy = "public";

    /// <summary>
    /// File uploads. Bodies here are orders of magnitude larger than a normal request
    /// (up to 100 MB for video), so the generous global tier is the wrong bucket for them.
    /// </summary>
    public const string UploadPolicy = "upload";

    // Long-lived or infrastructural paths that must never be throttled: SignalR holds a
    // connection open and reconnects in bursts, and a throttled health probe would take the
    // container down.
    private static readonly string[] ExemptPathPrefixes =
    [
        "/hubs/",
        "/live",
        "/health",
        "/docs",
        "/swagger"
    ];

    public static IServiceCollection AddApiRateLimiting(
        this IServiceCollection services, IConfiguration configuration)
    {
        var options = RateLimitSettings.FromConfiguration(configuration);

        if (!options.Enabled)
            return services;

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiter.AddPolicy(AuthPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"auth:{ClientKey(context)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.AuthPermitLimit,
                    Window = TimeSpan.FromSeconds(options.AuthWindowSeconds),
                    QueueLimit = 0 // fail fast; queueing a login attempt only delays the 429
                }));

            limiter.AddPolicy(SessionPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"session:{ClientKey(context)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.SessionPermitLimit,
                    Window = TimeSpan.FromSeconds(options.SessionWindowSeconds),
                    QueueLimit = 0
                }));

            limiter.AddPolicy(PublicPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"public:{ClientKey(context)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PublicPermitLimit,
                    Window = TimeSpan.FromSeconds(options.PublicWindowSeconds),
                    QueueLimit = 0
                }));

            limiter.AddPolicy(UploadPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: $"upload:{ClientKey(context)}",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.UploadPermitLimit,
                    Window = TimeSpan.FromSeconds(options.UploadWindowSeconds),
                    QueueLimit = 0
                }));

            limiter.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (IsExempt(context))
                    return RateLimitPartition.GetNoLimiter("exempt");

                // Endpoints carrying their own policy are handled by that policy; applying the
                // global limiter too would double-count them against a much larger bucket.
                if (context.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>() is not null)
                    return RateLimitPartition.GetNoLimiter("policy-handled");

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: $"global:{ClientKey(context)}",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalPermitLimit,
                        Window = TimeSpan.FromSeconds(options.GlobalWindowSeconds),
                        QueueLimit = 0
                    });
            });

            limiter.OnRejected = async (context, cancellationToken) =>
            {
                var http = context.HttpContext;

                // Tell the client when to come back, when the limiter knows.
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    http.Response.Headers.RetryAfter =
                        ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(NumberFormatInfo.InvariantInfo);
                }

                var logger = http.RequestServices.GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(RateLimiting).FullName!);

                logger.LogWarning(
                    "Rate limit rejected {Method} {Path} for {Client}",
                    http.Request.Method, http.Request.Path, ClientKey(http));

                http.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                http.Response.ContentType = "application/json";

                var error = ApiError.TooManyRequests(
                    "Too many requests. Please slow down and try again shortly.",
                    http.Request.Path);

                await http.Response.WriteAsync(
                    JsonSerializer.Serialize(error, SerializerOptions),
                    cancellationToken);
            };
        });

        return services;
    }

    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private static bool IsExempt(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (string.IsNullOrEmpty(path)) return false;

        foreach (var prefix in ExemptPathPrefixes)
        {
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Partition key for a caller: the user id when authenticated, otherwise the client IP.
    ///
    /// Keying authenticated traffic by user rather than IP matters here — a shop runs every
    /// till through one NAT address, so an IP partition would make cashiers throttle each other.
    /// Anonymous traffic has nothing better than the IP, which is why
    /// <c>UseForwardedHeaders</c> must be configured for the limits to bite per client rather
    /// than per proxy.
    /// </summary>
    private static string ClientKey(HttpContext context)
    {
        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(userId))
            return $"user:{userId}";

        var ip = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrEmpty(ip) ? "anonymous" : $"ip:{ip}";
    }
}

/// <summary>Bound from the <c>RateLimiting</c> configuration section.</summary>
public sealed record RateLimitSettings
{
    public bool Enabled { get; init; } = true;

    public int AuthPermitLimit { get; init; } = 10;
    public int AuthWindowSeconds { get; init; } = 60;

    public int SessionPermitLimit { get; init; } = 60;
    public int SessionWindowSeconds { get; init; } = 60;

    public int PublicPermitLimit { get; init; } = 60;
    public int PublicWindowSeconds { get; init; } = 60;

    public int UploadPermitLimit { get; init; } = 30;
    public int UploadWindowSeconds { get; init; } = 60;

    public int GlobalPermitLimit { get; init; } = 600;
    public int GlobalWindowSeconds { get; init; } = 60;

    public static RateLimitSettings FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection("RateLimiting");
        var defaults = new RateLimitSettings();

        // Clamped so a typo (0, or a negative) cannot lock the shop out of its own API.
        return new RateLimitSettings
        {
            Enabled = section.GetValue("Enabled", defaults.Enabled),
            AuthPermitLimit = Positive(section, "AuthPermitLimit", defaults.AuthPermitLimit),
            AuthWindowSeconds = Positive(section, "AuthWindowSeconds", defaults.AuthWindowSeconds),
            SessionPermitLimit = Positive(section, "SessionPermitLimit", defaults.SessionPermitLimit),
            SessionWindowSeconds = Positive(section, "SessionWindowSeconds", defaults.SessionWindowSeconds),
            PublicPermitLimit = Positive(section, "PublicPermitLimit", defaults.PublicPermitLimit),
            PublicWindowSeconds = Positive(section, "PublicWindowSeconds", defaults.PublicWindowSeconds),
            UploadPermitLimit = Positive(section, "UploadPermitLimit", defaults.UploadPermitLimit),
            UploadWindowSeconds = Positive(section, "UploadWindowSeconds", defaults.UploadWindowSeconds),
            GlobalPermitLimit = Positive(section, "GlobalPermitLimit", defaults.GlobalPermitLimit),
            GlobalWindowSeconds = Positive(section, "GlobalWindowSeconds", defaults.GlobalWindowSeconds)
        };
    }

    private static int Positive(IConfiguration section, string key, int fallback)
    {
        var value = section.GetValue(key, fallback);
        return value > 0 ? value : fallback;
    }
}
