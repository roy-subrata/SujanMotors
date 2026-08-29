using AutoPartShop.Api.Pdf;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Builds the <see cref="ShopProfile"/> that every PDF document header needs from the BUSINESS
/// application settings. Centralised so the settings keys and fallbacks live in one place rather
/// than being re-typed in each PDF endpoint.
/// </summary>
public interface IShopProfileProvider
{
    /// <param name="currencySymbol">
    /// Overrides the default taka symbol — pass the resolved symbol for documents whose amount is
    /// in a specific currency (e.g. a payment made in USD). Null keeps the ShopProfile default.
    /// </param>
    /// <param name="defaultFooterText">
    /// Fallback footer text used when INVOICE_FOOTER_TEXT is unset — lets a document type keep its
    /// own wording (e.g. a payment receipt's "Thank you for your payment.") instead of the shared
    /// "Thank you for your business!" default. Ignored once the setting is actually configured.
    /// </param>
    /// <param name="cancellationToken"></param>
    Task<ShopProfile> GetAsync(string? currencySymbol = null, string? defaultFooterText = null, CancellationToken cancellationToken = default);
}

public sealed class ShopProfileProvider(
    AutoPartDbContext db,
    HttpClient httpClient,
    IMemoryCache cache,
    ILogger<ShopProfileProvider> logger) : IShopProfileProvider
{
    private const string LogoCacheKeyPrefix = "ShopProfileProvider:Logo:";

    // The logo setting rarely changes; re-fetching it on every PDF request would add real network
    // latency to document generation for no benefit.
    private static readonly TimeSpan LogoCacheTtl = TimeSpan.FromMinutes(20);

    public async Task<ShopProfile> GetAsync(string? currencySymbol = null, string? defaultFooterText = null, CancellationToken cancellationToken = default)
    {
        var settings = await db.Set<ApplicationSettings>()
            .AsNoTracking()
            .Where(s => !s.Isdeleted)
            .ToListAsync(cancellationToken);

        string Get(string key, string fallback = "")
        {
            var v = settings.FirstOrDefault(s => s.Key == key)?.Value;
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        var logoBytes = await GetLogoBytesAsync(Get("SHOP_LOGO_URL"), cancellationToken);

        var profile = new ShopProfile(
            Name: Get("SHOP_NAME"),
            Address: Get("SHOP_ADDRESS"),
            Phone: Get("SHOP_PHONE"),
            Email: Get("SHOP_EMAIL"),
            TaxNo: Get("SHOP_TAX_NUMBER"),
            Tagline: Get("SHOP_TAGLINE"),
            FooterText: Get("INVOICE_FOOTER_TEXT", defaultFooterText ?? "Thank you for your business!"),
            BankDetails: Get("SHOP_BANK_DETAILS"),
            LogoBytes: logoBytes);

        return string.IsNullOrWhiteSpace(currencySymbol)
            ? profile
            : profile with { CurrencySymbol = currencySymbol };
    }

    /// <summary>
    /// SHOP_LOGO_URL is free text: either an absolute http(s) URL to a real uploaded logo, or the
    /// frontend admin form's default "assets/logo.png" placeholder — a relative path that only
    /// resolves in the browser, never on the server. Only the former is fetchable; everything else
    /// (blank, relative, unreachable, non-image) falls back to DocHeader's initials-square mark.
    /// </summary>
    private async Task<byte[]?> GetLogoBytesAsync(string logoUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(logoUrl) ||
            !(logoUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
              logoUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            return null;
        }

        var cacheKey = LogoCacheKeyPrefix + logoUrl;
        if (cache.TryGetValue(cacheKey, out byte[]? cached))
            return cached;

        var bytes = await FetchLogoAsync(logoUrl, cancellationToken);

        // Cache the miss too, at the same TTL — a broken URL should not be retried on every single
        // PDF request in the meantime.
        cache.Set(cacheKey, bytes, LogoCacheTtl);
        return bytes;
    }

    private async Task<byte[]?> FetchLogoAsync(string logoUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(logoUrl, HttpCompletionOption.ResponseContentRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;

            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is null || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return null;

            return await response.Content.ReadAsByteArrayAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // A broken/unreachable logo URL must never break PDF generation — log and fall back to
            // the initials-square placeholder instead of surfacing an error to the caller.
            logger.LogWarning(ex, "Failed to fetch shop logo from {LogoUrl}; falling back to initials placeholder", logoUrl);
            return null;
        }
    }
}
