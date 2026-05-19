using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.VariantPricingDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Manages the price schedule for a product (or a specific variant of it).
/// Price history/audit is in AuditLog — these endpoints handle time-based scheduled prices only.
/// Leave variantId null to target the base product price.
/// </summary>
[ApiController]
[Route("api/v1/products/{productId:guid}/price-schedule")]
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
    /// Get the currently active price for a product or one of its variants.
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<ActiveVariantPriceResponse>> GetActive(
        Guid productId, [FromQuery] Guid? variantId, CancellationToken cancellationToken)
    {
        var price = await _priceHistoryRepository.ResolveActivePriceAsync(productId, variantId, cancellationToken);
        return Ok(ApiResponse<ActiveVariantPriceResponse?>.Ok(price is null ? null : MapToActive(price)));
    }

    /// <summary>
    /// Get the price that was active on a specific date (for auditing past orders).
    /// </summary>
    [HttpGet("at")]
    public async Task<ActionResult<ActiveVariantPriceResponse>> GetAt(
        Guid productId, [FromQuery] Guid? variantId, [FromQuery] DateTime date, CancellationToken cancellationToken)
    {
        var price = await _priceHistoryRepository.GetPriceOnDateAsync(productId, variantId, date, cancellationToken);
        if (price is null)
            return NotFound(ApiError.NotFound($"No price found for {date:yyyy-MM-dd}", Request.Path));

        return Ok(ApiResponse<ActiveVariantPriceResponse>.Ok(MapToActive(price)));
    }

    /// <summary>
    /// Get full price schedule for a product (all variants, newest first).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<VariantPriceResponse>>> GetHistory(
        Guid productId, CancellationToken cancellationToken)
    {
        var history = await _priceHistoryRepository.GetByPartAsync(productId, cancellationToken);
        return Ok(ApiResponse<object>.Ok(history.Select(MapToResponse)));
    }

    /// <summary>
    /// Set a new selling price for a product or variant.
    /// Automatically closes the previous active price entry.
    /// Use variantId=null to set the base product price.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<VariantPriceResponse>> SetPrice(
        Guid productId, [FromQuery] Guid? variantId, [FromBody] SetVariantPriceRequest request, CancellationToken cancellationToken)
    {
        var currency = request.Currency?.Trim().ToUpperInvariant() ?? "BDT";
        var user     = _currentUserService.GetCurrentUsername();
        var today    = DateTime.UtcNow.Date;

        var newPrice = ProductVariantPriceHistory.Create(
            productId, request.SellingPrice, request.StartDate, variantId, currency, request.Reason);
        newPrice.CreatedBy  = user;
        newPrice.ModifiedBy = user;

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

            await _priceHistoryRepository.SetNewPriceAsync(newPrice, cancellationToken);

            // Only sync the denormalized field when the price is effective today or earlier.
            // A future-dated price must not be visible in the catalog yet.
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
                        .FirstOrDefaultAsync(p => p.Id == productId && !p.Isdeleted, cancellationToken);
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

        return CreatedAtAction(nameof(GetActive), new { productId },
            ApiResponse<VariantPriceResponse>.Ok(MapToResponse(newPrice)));
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
        Source = p.ProductVariantId.HasValue ? "VARIANT_SCHEDULE" : "PRODUCT_SCHEDULE",
        ValidFrom = p.StartDate,
        ValidTo = p.EndDate
    };
}
