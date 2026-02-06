using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PricingController : ControllerBase
{
    private readonly IPartRepository _partRepository;
    private readonly IPricingValidationService _pricingValidationService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IPartRepository partRepository,
        IPricingValidationService pricingValidationService,
        IUnitConversionService unitConversionService,
        ILogger<PricingController> logger)
    {
        _partRepository = partRepository;
        _pricingValidationService = pricingValidationService;
        _unitConversionService = unitConversionService;
        _logger = logger;
    }

    [HttpPost("validate-line")]
    public async Task<IActionResult> ValidateLine([FromBody] PricingValidationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required." });

            var part = await _partRepository.GetByIdAsync(request.PartId, cancellationToken);
            if (part is null)
                return NotFound(new { message = "Part not found." });

            var baseUnitPrice = await NormalizeUnitPriceAsync(part, request.UnitPrice, request.UnitId, cancellationToken);
            var effectivePrice = _pricingValidationService.ValidateLinePricing(part, baseUnitPrice, request.DiscountPercent);

            return Ok(new { effectivePrice });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating line pricing");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while validating pricing");
        }
    }

    [HttpPost("calculate-line")]
    public async Task<IActionResult> CalculateLine([FromBody] PricingValidationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required." });

            var part = await _partRepository.GetByIdAsync(request.PartId, cancellationToken);
            if (part is null)
                return NotFound(new { message = "Part not found." });

            var baseUnitPrice = await NormalizeUnitPriceAsync(part, request.UnitPrice, request.UnitId, cancellationToken);
            var snapshot = _pricingValidationService.CalculateLinePricingSnapshot(part, baseUnitPrice, request.DiscountPercent);
            return Ok(snapshot);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating line pricing");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while calculating pricing");
        }
    }

    public class PricingValidationRequest
    {
        public Guid PartId { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal DiscountPercent { get; set; }
        public Guid? UnitId { get; set; }
    }

    private async Task<decimal> NormalizeUnitPriceAsync(Part part, decimal unitPrice, Guid? unitId, CancellationToken cancellationToken)
    {
        if (part.UnitId is null || unitId is null || unitId.Value == part.UnitId.Value)
            return unitPrice;

        var conversionFactor = await _unitConversionService.GetConversionFactorAsync(unitId.Value, part.UnitId.Value, cancellationToken);
        if (conversionFactor <= 0)
            throw new InvalidOperationException("Invalid unit conversion factor.");

        return unitPrice / conversionFactor;
    }
}
