using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

// Microsoft.AspNetCore.HttpOverrides also defines IPNetwork, but it is the obsolete one;
// ForwardedHeadersOptions.KnownIPNetworks takes the System.Net type.
using IPNetwork = System.Net.IPNetwork;

namespace AutoPartShop.Api.Middleware;

/// <summary>
/// Restores the real client IP from <c>X-Forwarded-For</c>.
///
/// <para>The API runs behind a reverse proxy — nginx in the Docker/VPS deployment, App Service
/// or a Cloudflare tunnel on Azure — so <c>RemoteIpAddress</c> is the proxy's address, not the
/// caller's. Without this, every request looks like it comes from one machine, which would put
/// the entire shop into a single rate-limit partition and make the per-IP limits useless.</para>
///
/// <para><b>Hop count.</b> <c>ForwardLimit</c> is how many entries of the header are walked,
/// right to left. nginx uses <c>$proxy_add_x_forwarded_for</c>, which appends, so the rightmost
/// entry is the hop nearest the API. One proxy (nginx only) needs <c>ForwardLimit=1</c>; adding
/// Cloudflare or a load balancer in front of nginx makes it 2. Set it too low and every visitor
/// buckets under the upstream proxy's address.</para>
///
/// <para><b>Spoofing.</b> The header is client-supplied. Unless the middleware is told which
/// hops are legitimate it accepts the value from anyone, so a caller can forge an IP and
/// sidestep the per-IP tiers. Pin the trusted hops with either:</para>
/// <list type="bullet">
/// <item><c>KnownProxies</c> — individual addresses, for a fixed proxy IP.</item>
/// <item><c>KnownNetworks</c> — CIDR ranges, which is what a Docker deployment needs: the nginx
/// container's address is assigned from the bridge subnet and is not stable, but the subnet
/// itself is (e.g. <c>172.16.0.0/12</c>).</item>
/// </list>
/// <para>On Azure App Service neither can be pinned — the front-end address is not fixed — so
/// the open configuration is the documented trade-off there. Either way this affects the
/// anonymous tiers only: authenticated traffic partitions by user id, which cannot be spoofed
/// without a valid token.</para>
/// </summary>
public static class ForwardedHeadersSetup
{
    public static IServiceCollection AddProxyForwardedHeaders(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("ForwardedHeaders");

        if (!section.GetValue("Enabled", true))
            return services;

        var (proxies, networks) = ReadTrustedHops(section);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = Math.Max(1, section.GetValue("ForwardLimit", 1));

            // Defaults trust only loopback, which is never the proxy in a container
            // deployment. Clear them and use whatever the configuration names instead.
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var address in proxies)
                options.KnownProxies.Add(address);

            foreach (var network in networks)
                options.KnownIPNetworks.Add(network);
        });

        return services;
    }

    /// <summary>
    /// Reports the trust posture once at startup: which hops are pinned, or a warning that any
    /// source is accepted. Keeps the weaker guarantee visible in the logs rather than assumed.
    /// </summary>
    public static void LogForwardedHeadersTrust(this IApplicationBuilder app, IConfiguration configuration)
    {
        var section = configuration.GetSection("ForwardedHeaders");
        var logger = app.ApplicationServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(ForwardedHeadersSetup).FullName!);

        if (!section.GetValue("Enabled", true))
        {
            logger.LogWarning(
                "X-Forwarded-For handling is disabled. Behind a reverse proxy the client IP will " +
                "be the proxy's, putting every caller in one rate-limit partition.");
            return;
        }

        var (proxies, networks) = ReadTrustedHops(section);
        var forwardLimit = Math.Max(1, section.GetValue("ForwardLimit", 1));

        if (proxies.Count == 0 && networks.Count == 0)
        {
            logger.LogWarning(
                "X-Forwarded-For is trusted from any source (ForwardedHeaders:KnownProxies and " +
                "KnownNetworks are both empty). Client IPs may be spoofed, weakening IP-based rate " +
                "limits. Pin the proxy with KnownNetworks (CIDR, e.g. the Docker bridge subnet) or " +
                "KnownProxies where the address is fixed. ForwardLimit={ForwardLimit}.",
                forwardLimit);
            return;
        }

        logger.LogInformation(
            "X-Forwarded-For trusted from {ProxyCount} proxy address(es) and {NetworkCount} " +
            "network(s); ForwardLimit={ForwardLimit}.",
            proxies.Count, networks.Count, forwardLimit);
    }

    /// <summary>
    /// Parses the configured hops, ignoring entries that are not valid addresses or CIDR ranges
    /// so one bad value cannot stop the API from starting.
    /// </summary>
    private static (List<IPAddress> Proxies, List<IPNetwork> Networks) ReadTrustedHops(IConfiguration section)
    {
        var proxies = new List<IPAddress>();
        var networks = new List<IPNetwork>();

        foreach (var value in section.GetSection("KnownProxies").Get<string[]>() ?? [])
        {
            if (IPAddress.TryParse(value?.Trim(), out var address))
                proxies.Add(address);
        }

        foreach (var value in section.GetSection("KnownNetworks").Get<string[]>() ?? [])
        {
            if (IPNetwork.TryParse(value?.Trim(), out var network))
                networks.Add(network);
        }

        return (proxies, networks);
    }
}
