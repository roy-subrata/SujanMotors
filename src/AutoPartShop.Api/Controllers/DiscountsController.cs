using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.DiscountDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[HasPermission(Permissions.InventoryView)]
[Produces("application/json")]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountRepository _discountRepository;
    private readonly IDiscountResolutionService _discountResolutionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<DiscountsController> _logger;

    public DiscountsController(
        IDiscountRepository discountRepository,
        IDiscountResolutionService discountResolutionService,
        ICurrentUserService currentUserService,
        ILogger<DiscountsController> logger)
    {
        _discountRepository = discountRepository;
        _discountResolutionService = discountResolutionService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DiscountResponse>>> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var discounts = await _discountRepository.GetAllAsync(cancellationToken);
            return Ok(discounts.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all discounts");
            return StatusCode(500, "An error occurred while retrieving discounts");
        }
    }

    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<DiscountResponse>>> GetActive(CancellationToken cancellationToken)
    {
        try
        {
            var discounts = await _discountRepository.GetActiveDiscountsAsync(cancellationToken);
            return Ok(discounts.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active discounts");
            return StatusCode(500, "An error occurred while retrieving active discounts");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DiscountResponse>> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var discount = await _discountRepository.GetByIdAsync(id, cancellationToken);
            if (discount is null)
                return NotFound(new { message = "Discount not found" });

            return Ok(MapToResponse(discount));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discount {DiscountId}", id);
            return StatusCode(500, "An error occurred while retrieving the discount");
        }
    }

    /// <summary>
    /// Get all discounts assigned to a specific part (product + variant level).
    /// </summary>
    [HttpGet("part/{partId:guid}")]
    public async Task<ActionResult<IEnumerable<DiscountResponse>>> GetByPart(Guid partId, CancellationToken cancellationToken)
    {
        try
        {
            var discounts = await _discountRepository.GetByPartAsync(partId, cancellationToken);
            return Ok(discounts.Select(MapToResponse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting discounts for part {PartId}", partId);
            return StatusCode(500, "An error occurred while retrieving discounts");
        }
    }

    [HttpPost]
    [HasPermission(Permissions.InventoryCreate)]
    public async Task<ActionResult<DiscountResponse>> Create(CreateDiscountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var discount = Discount.Create(
                request.Name,
                request.Type,
                request.Value,
                request.StartDate,
                request.PartId,
                request.ProductVariantId,
                request.EndDate,
                request.Description,
                request.PromoCode,
                request.MinimumCartAmount);

            var user = _currentUserService.GetCurrentUsername();
            discount.CreatedBy = user;
            discount.ModifiedBy = user;

            await _discountRepository.AddAsync(discount, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = discount.Id }, MapToResponse(discount));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating discount");
            return StatusCode(500, "An error occurred while creating the discount");
        }
    }

    [HttpPut("{id:guid}")]
    [HasPermission(Permissions.InventoryEdit)]
    public async Task<ActionResult<DiscountResponse>> Update(Guid id, UpdateDiscountRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (id != request.Id)
                return BadRequest(new { message = "ID mismatch" });

            var discount = await _discountRepository.GetByIdAsync(id, cancellationToken);
            if (discount is null)
                return NotFound(new { message = "Discount not found" });

            discount.Update(
                request.Name,
                request.Type,
                request.Value,
                request.StartDate,
                request.IsActive,
                request.EndDate,
                request.Description,
                request.PromoCode,
                request.MinimumCartAmount);

            discount.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _discountRepository.UpdateAsync(discount, cancellationToken);
            return Ok(MapToResponse(discount));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating discount {DiscountId}", id);
            return StatusCode(500, "An error occurred while updating the discount");
        }
    }

    [HttpDelete("{id:guid}")]
    [HasPermission(Permissions.InventoryDelete)]
    public async Task<ActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var discount = await _discountRepository.GetByIdAsync(id, cancellationToken);
            if (discount is null)
                return NotFound(new { message = "Discount not found" });

            await _discountRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting discount {DiscountId}", id);
            return StatusCode(500, "An error occurred while deleting the discount");
        }
    }

    // â”€â”€ Resolve endpoints â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("resolve/item")]
    public async Task<ActionResult<DiscountResolutionResult>> ResolveItemDiscount(
        [FromQuery] Guid partId,
        [FromQuery] Guid? variantId,
        [FromQuery] decimal unitPrice,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _discountResolutionService.ResolveItemDiscountAsync(
                partId, variantId, unitPrice, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving item discount");
            return StatusCode(500, "An error occurred while resolving the discount");
        }
    }

    [HttpGet("resolve/cart")]
    public async Task<ActionResult<DiscountResolutionResult>> ResolveCartDiscount(
        [FromQuery] decimal cartSubtotal,
        [FromQuery] string? promoCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _discountResolutionService.ResolveCartDiscountAsync(
                cartSubtotal, promoCode, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resolving cart discount");
            return StatusCode(500, "An error occurred while resolving the cart discount");
        }
    }

    private static DiscountResponse MapToResponse(Discount d) => new()
    {
        Id = d.Id,
        Name = d.Name,
        Description = d.Description,
        Type = d.Type,
        Value = d.Value,
        Scope = d.Scope,
        PartId = d.PartId,
        ProductVariantId = d.ProductVariantId,
        PromoCode = d.PromoCode,
        MinimumCartAmount = d.MinimumCartAmount,
        StartDate = d.StartDate,
        EndDate = d.EndDate,
        IsActive = d.IsActive,
        CreatedAt = d.CreatedDate,
        ModifiedAt = d.ModifiedDate == default ? null : d.ModifiedDate
    };
}
