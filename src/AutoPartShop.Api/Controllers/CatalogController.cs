using AutoPartShop.Application.Catalog;
using AutoPartShop.Application.Catalog.Dtos;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CatalogController(
    ICatalogReadRepository _catalogReadRepository,
    IApplicationSettingsRepository _settingsRepository,
    ILogger<CatalogController> _logger) : ControllerBase
{
    [HttpGet("landing")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogLandingResponse>> GetLanding(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _catalogReadRepository.GetLandingAsync(cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog landing data");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving landing data");
        }
    }

    [HttpGet("categories/{categoryId:guid}/filters")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<CatalogFilterResponse>> GetFilters(Guid categoryId, [FromQuery] bool includeDescendants = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _catalogReadRepository.GetFiltersAsync(categoryId, includeDescendants, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filters for category {CategoryId}", categoryId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving filters");
        }
    }

    [HttpPost("products/search")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<object>> Search([FromBody] CatalogSearchRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var (items, total) = await _catalogReadRepository.SearchAsync(request, cancellationToken);
            return Ok(new
            {
                items,
                request.PageNumber,
                request.PageSize,
                totalCount = total
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching catalog products");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while searching products");
        }
    }

    [HttpGet("shop-policies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ShopPoliciesResponse>> GetShopPolicies(CancellationToken cancellationToken = default)
    {
        var defaults = ShopPoliciesResponse.Defaults;
        try
        {
            var settings = await _settingsRepository.GetByCategoryAsync("SHOP", cancellationToken);
            var lookup = settings.ToDictionary(s => s.Key, s => s.Value);

            bool GetBool(string key, bool fallback)    => lookup.TryGetValue(key, out var v) && bool.TryParse(v, out var b) ? b : fallback;
            decimal GetDecimal(string key, decimal fallback) => lookup.TryGetValue(key, out var v) && decimal.TryParse(v, out var d) ? d : fallback;
            int GetInt(string key, int fallback)       => lookup.TryGetValue(key, out var v) && int.TryParse(v, out var i) ? i : fallback;
            string GetStr(string key, string fallback) => lookup.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

            return Ok(new ShopPoliciesResponse
            {
                FreeShippingEnabled   = GetBool   ("SHOP_FREE_SHIPPING_ENABLED",   defaults.FreeShippingEnabled),
                FreeShippingThreshold = GetDecimal ("SHOP_FREE_SHIPPING_THRESHOLD", defaults.FreeShippingThreshold),
                FreeShippingCurrency  = GetStr     ("SHOP_FREE_SHIPPING_CURRENCY",  defaults.FreeShippingCurrency),
                ReturnPolicyDays      = GetInt     ("SHOP_RETURN_POLICY_DAYS",      defaults.ReturnPolicyDays),
                ReturnPolicyText      = GetStr     ("SHOP_RETURN_POLICY_TEXT",      defaults.ReturnPolicyText),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving shop policies");
            return Ok(defaults); // never break the storefront
        }
    }

    [HttpGet("products/{partId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CatalogProductDetail>> GetProductDetail(Guid partId, CancellationToken cancellationToken = default)
    {
        try
        {
            var detail = await _catalogReadRepository.GetProductDetailAsync(partId, cancellationToken);
            if (detail == null)
                return NotFound(new { message = "Product not found" });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting catalog product detail for {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving product details");
        }
    }
}

public class ShopPoliciesResponse
{
    public bool FreeShippingEnabled { get; set; }
    public decimal FreeShippingThreshold { get; set; }
    public string FreeShippingCurrency { get; set; } = string.Empty;
    public int ReturnPolicyDays { get; set; }
    public string ReturnPolicyText { get; set; } = string.Empty;

    // Single source of truth for fallback values — used by both the controller
    // and the catch-block so inline code and class defaults can never diverge.
    public static ShopPoliciesResponse Defaults => new()
    {
        FreeShippingEnabled   = true,
        FreeShippingThreshold = 5000,
        FreeShippingCurrency  = "BDT",
        ReturnPolicyDays      = 30,
        ReturnPolicyText      = "30-day return policy",
    };
}
