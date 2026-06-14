using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Authorize]
public class ApplicationSettingsController : ControllerBase
{
    private readonly IApplicationSettingsRepository _settingsRepository;
    private readonly ILogger<ApplicationSettingsController> _logger;

    public ApplicationSettingsController(
        IApplicationSettingsRepository settingsRepository,
        ILogger<ApplicationSettingsController> logger)
    {
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get the public shop profile used by print templates (no auth required).
    /// Returns all BUSINESS-category settings as a typed object.
    /// </summary>
    [HttpGet("public/shop")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPublicShopProfile()
    {
        var settings = await _settingsRepository.GetByCategoryAsync("BUSINESS");
        var branding = await _settingsRepository.GetByCategoryAsync("BRANDING");

        string Get(string key, string fallback = "")
        {
            var v = settings.FirstOrDefault(s => s.Key == key && !s.Isdeleted)?.Value;
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        string GetBranding(string key, string fallback = "")
        {
            var v = branding.FirstOrDefault(s => s.Key == key && !s.Isdeleted)?.Value;
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        return Ok(new
        {
            // Application brand (white-label) — independent of the business identity below.
            appName           = GetBranding("APP_NAME", "Auto Part Shop"),
            appLogoUrl        = GetBranding("APP_LOGO_URL", "assets/logo.png"),
            name              = Get("SHOP_NAME"),
            address           = Get("SHOP_ADDRESS"),
            phone             = Get("SHOP_PHONE"),
            email             = Get("SHOP_EMAIL"),
            taxNo             = Get("SHOP_TAX_NUMBER"),
            logoUrl           = Get("SHOP_LOGO_URL", "assets/logo.png"),
            tagline           = Get("SHOP_TAGLINE"),
            invoiceFooterText = Get("INVOICE_FOOTER_TEXT", "Thank you for your business!"),
            challanFooterText = Get("CHALLAN_FOOTER_TEXT", "Goods once dispatched will not be accepted back without prior notice.")
        });
    }

    /// <summary>
    /// Dynamic PWA manifest built from the configured application brand (no auth required).
    /// Served at a route without a file extension so static-file middleware does not intercept it;
    /// referenced from index.html via &lt;link rel="manifest"&gt;. Icon and start_url paths are
    /// root-relative so they resolve against the page origin (dev proxy / prod reverse proxy).
    /// </summary>
    [HttpGet("public/manifest")]
    [AllowAnonymous]
    public async Task<ActionResult> GetManifest()
    {
        var appName = await _settingsRepository.GetValueAsync("APP_NAME");
        var name = string.IsNullOrWhiteSpace(appName) ? "Auto Part Shop" : appName;
        var shortName = name.Length > 12 ? name[..12] : name;

        var manifest = new Dictionary<string, object?>
        {
            ["name"] = name,
            ["short_name"] = shortName,
            ["description"] = $"{name} - Inventory and Sales Management System",
            ["theme_color"] = "#667eea",
            ["background_color"] = "#ffffff",
            ["display"] = "standalone",
            ["orientation"] = "portrait-primary",
            ["scope"] = "/",
            ["start_url"] = "/",
            ["id"] = "/",
            ["icons"] = new object[]
            {
                new { src = "/assets/icons/manifest-icon-192.maskable.png", sizes = "192x192", type = "image/png", purpose = "any" },
                new { src = "/assets/icons/manifest-icon-192.maskable.png", sizes = "192x192", type = "image/png", purpose = "maskable" },
                new { src = "/assets/icons/manifest-icon-512.maskable.png", sizes = "512x512", type = "image/png", purpose = "any" },
                new { src = "/assets/icons/manifest-icon-512.maskable.png", sizes = "512x512", type = "image/png", purpose = "maskable" },
                new { src = "/assets/icons/icon.svg", sizes = "any", type = "image/svg+xml", purpose = "any" }
            },
            ["categories"] = new[] { "business", "productivity" }
        };

        return new JsonResult(manifest) { ContentType = "application/manifest+json" };
    }

    /// <summary>
    /// Get all application settings (Admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ApplicationSettings>>> GetAll()
    {
        var settings = await _settingsRepository.GetAllAsync();
        return Ok(settings);
    }

    /// <summary>
    /// Get setting by key
    /// </summary>
    [HttpGet("{key}")]
    public async Task<ActionResult<string>> GetByKey(string key)
    {
        var value = await _settingsRepository.GetValueAsync(key);
        if (value == null)
            return NotFound($"Setting with key '{key}' not found");

        return Ok(new { key, value });
    }

    /// <summary>
    /// Get settings by category
    /// </summary>
    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<ApplicationSettings>>> GetByCategory(string category)
    {
        var settings = await _settingsRepository.GetByCategoryAsync(category);
        return Ok(settings);
    }

    /// <summary>
    /// Get all categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _settingsRepository.GetCategoriesAsync();
        return Ok(categories);
    }

    /// <summary>
    /// Update setting value (Admin only)
    /// </summary>
    [HttpPut("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateValue(string key, [FromBody] UpdateSettingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _settingsRepository.SetValueAsync(
            key,
            request.Value,
            request.DataType ?? "STRING",
            request.Category ?? "GENERAL",
            request.Description ?? string.Empty,
            request.IsSystemSetting);

        return Ok(new { message = $"Setting '{key}' updated successfully" });
    }

    /// <summary>
    /// Create new setting (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Create([FromBody] CreateSettingRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _settingsRepository.ExistsByKeyAsync(request.Key))
            return Conflict($"Setting with key '{request.Key}' already exists");

        var setting = ApplicationSettings.Create(
            request.Key,
            request.Value,
            request.DataType ?? "STRING",
            request.Category ?? "GENERAL",
            request.Description ?? string.Empty,
            request.IsSystemSetting);

        await _settingsRepository.AddAsync(setting);

        return CreatedAtAction(nameof(GetByKey), new { key = request.Key }, setting);
    }

    /// <summary>
    /// Delete setting (Admin only)
    /// </summary>
    [HttpDelete("{key}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(string key)
    {
        try
        {
            await _settingsRepository.DeleteByKeyAsync(key);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get default currency ID
    /// </summary>
    [HttpGet("default-currency")]
    public async Task<ActionResult<Guid?>> GetDefaultCurrency()
    {
        var value = await _settingsRepository.GetValueAsync("DEFAULT_CURRENCY_ID");
        if (value == null)
            return Ok(new { defaultCurrencyId = (Guid?)null });

        if (Guid.TryParse(value, out var currencyId))
            return Ok(new { defaultCurrencyId = currencyId });

        return Ok(new { defaultCurrencyId = (Guid?)null });
    }

    /// <summary>
    /// Set default currency ID (Admin only)
    /// </summary>
    [HttpPut("default-currency")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SetDefaultCurrency([FromBody] SetDefaultCurrencyRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await _settingsRepository.SetValueAsync(
            "DEFAULT_CURRENCY_ID",
            request.CurrencyId.ToString(),
            "GUID",
            "CURRENCY",
            "Default currency ID for new transactions",
            true);

        return Ok(new { message = "Default currency updated successfully", defaultCurrencyId = request.CurrencyId });
    }
}

public record CreateSettingRequest(
    string Key,
    string Value,
    string? DataType = "STRING",
    string? Category = "GENERAL",
    string? Description = "",
    bool IsSystemSetting = false
);

public record UpdateSettingRequest(
    string Value,
    string? DataType = "STRING",
    string? Category = "GENERAL",
    string? Description = "",
    bool IsSystemSetting = false
);

public record SetDefaultCurrencyRequest(
    Guid CurrencyId
);
