using AutoPartShop.Api.Pdf;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
    Task<ShopProfile> GetAsync(string? currencySymbol = null, CancellationToken cancellationToken = default);
}

public sealed class ShopProfileProvider(AutoPartDbContext db) : IShopProfileProvider
{
    public async Task<ShopProfile> GetAsync(string? currencySymbol = null, CancellationToken cancellationToken = default)
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

        var profile = new ShopProfile(
            Name: Get("SHOP_NAME"),
            Address: Get("SHOP_ADDRESS"),
            Phone: Get("SHOP_PHONE"),
            Email: Get("SHOP_EMAIL"),
            TaxNo: Get("SHOP_TAX_NUMBER"),
            Tagline: Get("SHOP_TAGLINE"),
            FooterText: Get("INVOICE_FOOTER_TEXT", "Thank you for your business!"),
            BankDetails: Get("SHOP_BANK_DETAILS"));

        return string.IsNullOrWhiteSpace(currencySymbol)
            ? profile
            : profile with { CurrencySymbol = currencySymbol };
    }
}
