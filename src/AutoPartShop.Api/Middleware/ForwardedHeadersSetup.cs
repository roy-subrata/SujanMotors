using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace AutoPartShop.Api.Middleware;

/// <summary>
/// Restores the real client IP from <c>X-Forwarded-For</c>.
///
/// <para>The API runs behind a reverse proxy (Azure App Service, or a Cloudflare tunnel), so
/// <c>RemoteIpAddress</c> is the proxy's address, not the caller's. Without this, every request
/// looks like it comes from one machine — which would put the entire shop into a single
/// rate-limit partition and make the per-IP limits useless.</para>
///
/// <para><b>Spoofing:</b> with no <c>KnownProxies</c> configured the header is accepted from
/// anyone, so a caller can forge an IP and sidestep IP-based limits. That is the documented
/// trade-off on Azure App Service, where the front-end address is not fixed and cannot be
/// allow-listed. Set <c>ForwardedHeaders:KnownProxies</c> when the proxy address IS known and
/// the middleware will trust only those hops. Note this affects the anonymous tiers only:
/// authenticated traffic is partitioned by user id, which cannot be spoofed without a valid
/// token.</para>
/// </summary>
public static class ForwardedHeadersSetup
{
    public static IServiceCollection AddProxyForwardedHeaders(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("ForwardedHeaders");

        if (!section.GetValue("Enabled", true))
            return services;

        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Only the nearest hop is trusted by default; a longer chain would let a client
            // prepend entries and choose which address is read as the origin.
            options.ForwardLimit = section.GetValue("ForwardLimit", 1);

            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var proxy in knownProxies)
            {
                if (IPAddress.TryParse(proxy, out var address))
                    options.KnownProxies.Add(address);
            }
        });

        return services;
    }

    /// <summary>
    /// Warns once at startup when the header is trusted from any source, so the weaker
    /// guarantee is visible in the logs rather than assumed.
    /// </summary>
    public static void LogForwardedHeadersTrust(this IApplicationBuilder app, IConfiguration configuration)
    {
        var section = configuration.GetSection("ForwardedHeaders");
        if (!section.GetValue("Enabled", true))
            return;

        var knownProxies = section.GetSection("KnownProxies").Get<string[]>() ?? [];
        if (knownProxies.Length > 0)
            return;

        app.ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ForwardedHeadersSetup).FullName!)
            .LogWarning(
                "X-Forwarded-For is trusted from any source (ForwardedHeaders:KnownProxies is empty). " +
                "Client IPs may be spoofed, weakening IP-based rate limits. Configure KnownProxies " +
                "when the proxy address is known.");
    }
}
