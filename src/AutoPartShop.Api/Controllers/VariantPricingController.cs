using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.VariantPricingDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/variant-pricing")]
[Authorize]
[Produces("application/json")]
public class VariantPricingController : ControllerBase
{
    private readonly IProductVariantPriceHistoryRepository _priceHistoryRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<VariantPricingController> _logger;
    private readonly AutoPartDbContext _db;

    public VariantPricingController(
        IProductVariantPriceHistoryRepository priceHistoryRepository,
        ICurrentUserService currentUserService,
        ILogger<VariantPricingController> logger,
        AutoPartDbContext db)
    {
        _priceHistoryRepository = priceHistoryRepository;
        _currentUserService = currentUserService;
        _logger = logger;
        _db = db;
    }

    /// <summary>
    /// Get the currently active price for a part or variant.
    /// variantId is optional — omit for base product price.
    /// </summary>
    [HttpGet("{partId:guid}/active")]
    public async Task<ActionResult<ActiveVariantPriceResponse>> GetActivePrice(
        Guid partId, [FromQuery] Guid? variantId, CancellationToken cancellationToken)
    {
        try
        {
            var price = await _priceHistoryRepository.ResolveActivePriceAsync(partId, variantId, cancellationToken);
            if (price is null)
                return NotFound(new { message = "No active price found" });

            return Ok(MapToActive(price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active price for part {PartId}", partId);
            return StatusCode(500, "An error occurred while retrieving the price");
        }
    }

    /// <summary>Get the price that was active on a specific date (for auditing past orders).</summary>
    [HttpGet("{partId:guid}/price-at")]
    public async Task<ActionResult<ActiveVariantPriceResponse>> GetPriceAt(
        Guid partId, [FromQuery] Guid? variantId, [FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        try
        {
            var price = await _priceHistoryRepository.GetPriceOnDateAsync(partId, variantId, date, cancellationToken);
            if (price is null)
                return NotFound(new { message = $"No price found on {date:yyyy-MM-dd}" });

            return Ok(MapToActive(price));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price at date for part {PartId}", partId);
            return StatusCode(500, "An error occurred while retrieving the price");
        }
    }

    /// <summary>Get full price history for a part (all variants included), newest first.</summary>
    [HttpGet("{partId:guid}/history")]
    public async Task<ActionResult<IEnumerable<VariantPriceResponse>>> GetHistory(
        Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _priceHistoryRepository.GetByPartAsync(partId, cancellationToken);
            return Ok(history.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price history for part {PartId}", partId);
            return StatusCode(500, "An error occurred while retrieving price history");
        }
    }

    /// <summary>
    /// Set a new selling price for a part or variant.
    /// Automatically closes the previous active price.
    /// Leave variantId null to set the base product price.
    /// </summary>
    [HttpPost("{partId:guid}/set-price")]
    public async Task<ActionResult<VariantPriceResponse>> SetPrice(
        Guid partId, [FromQuery] Guid? variantId, SetVariantPriceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var currency = request.Currency?.Trim().ToUpperInvariant() ?? "BDT";
            var user     = _currentUserService.GetCurrentUsername();
            var today    = DateTime.UtcNow.Date;

            var newPrice = ProductVariantPriceHistory.Create(
                partId, request.SellingPrice, request.StartDate, variantId, currency, request.Reason);
            newPrice.CreatedBy  = user;
            newPrice.ModifiedBy = user;

            // EnableRetryOnFailure requires IExecutionStrategy to safely span a manual transaction.
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

                // 1 — persist the price schedule record (closes previous active price internally)
                await _priceHistoryRepository.SetNewPriceAsync(newPrice, cancellationToken);

                // 2 — sync the denormalized field ONLY if the price is effective today or earlier
                //     A future-dated price (StartDate > today) must NOT be visible in the catalog yet
                if (request.StartDate.Date <= today)
                {
                    if (variantId.HasValue)
                    {
                        var variant = await _db.ProductVariants
                            .FirstOrDefaultAsync(v => v.Id == variantId.Value && !v.Isdeleted, cancellationToken);
                        if (variant != null)
                        {
                            variant.UpdateSellingPrice(request.SellingPrice, currency);
                            variant.ModifiedBy = user;
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }
                    else
                    {
                        var part = await _db.Parts
                            .FirstOrDefaultAsync(p => p.Id == partId && !p.Isdeleted, cancellationToken);
                        if (part != null)
                        {
                            part.UpdateSellingPrice(request.SellingPrice, currency);
                            part.ModifiedBy = user;
                            await _db.SaveChangesAsync(cancellationToken);
                        }
                    }
                }

                await tx.CommitAsync(cancellationToken);
            });

            return CreatedAtAction(nameof(GetActivePrice), new { partId }, MapToResponse(newPrice));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting price for part {PartId}", partId);
            return StatusCode(500, "An error occurred while setting the price");
        }
    }

    private static VariantPriceResponse MapToResponse(ProductVariantPriceHistory p) => new()
    {
        Id = p.Id,
        PartId = p.PartId,
        ProductVariantId = p.ProductVariantId,
        SellingPrice = p.SellingPrice,
        Currency = p.Currency,
        StartDate = p.StartDate,
        EndDate = p.EndDate,
        IsActive = p.IsCurrentlyActive,
        Reason = p.Reason,
        CreatedAt = p.CreatedDate,
        CreatedBy = p.CreatedBy
    };

    private static ActiveVariantPriceResponse MapToActive(ProductVariantPriceHistory p) => new()
    {
        PartId = p.PartId,
        ProductVariantId = p.ProductVariantId,
        SellingPrice = p.SellingPrice,
        Currency = p.Currency,
        Source = p.ProductVariantId.HasValue ? "VARIANT_HISTORY" : "PRODUCT_HISTORY",
        ValidFrom = p.StartDate,
        ValidTo = p.EndDate
    };
}
