using System.Security.Claims;
using AutoPartShop.Api.Middleware;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Api.Authorization;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AutoPartShop.Api.Controllers;

[Route("api/v1/pricing")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class PricingController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly IPricingValidationService _pricingValidationService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly ICostResolutionService _costResolutionService;
    private readonly IDiscountResolutionService _discountResolutionService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IPermissionCheckService _permissionCheckService;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<PricingController> _logger;

    public PricingController(
        IProductRepository productRepository,
        IPricingValidationService pricingValidationService,
        IUnitConversionService unitConversionService,
        ICostResolutionService costResolutionService,
        IDiscountResolutionService discountResolutionService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IPermissionCheckService permissionCheckService,
        AutoPartDbContext dbContext,
        ILogger<PricingController> logger)
    {
        _productRepository = productRepository;
        _pricingValidationService = pricingValidationService;
        _unitConversionService = unitConversionService;
        _costResolutionService = costResolutionService;
        _discountResolutionService = discountResolutionService;
        _userManager = userManager;
        _signInManager = signInManager;
        _permissionCheckService = permissionCheckService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("validate-line")]
    public async Task<IActionResult> ValidateLine([FromBody] PricingValidationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request is null || request.PartId == Guid.Empty)
                return BadRequest(new { message = "PartId is required." });

            var part = await _productRepository.GetByIdAsync(request.PartId, cancellationToken);
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

            var part = await _productRepository.GetByIdAsync(request.PartId, cancellationToken);
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

    /// <summary>
    /// Returns the authoritative per-unit COST for a part/variant — the FIFO (oldest-received) sellable
    /// stock lot cost, falling back to the catalogue variant/part CostPrice. This endpoint is a
    /// read-only lookup and never blocks anything itself; the POS uses it to show a below-cost
    /// warning ahead of submit. The actual enforcement — rejecting a sale whose net price falls
    /// below cost × (1 + SALES_MIN_MARGIN_PERCENT), a hard block with no override for any role —
    /// happens server-side in SalesOrderController via the same ICostResolutionService this
    /// endpoint calls.
    /// </summary>
    [HttpGet("unit-cost")]
    public async Task<IActionResult> UnitCost(Guid partId, Guid? variantId, CancellationToken cancellationToken)
    {
        if (partId == Guid.Empty)
            return BadRequest(new { message = "PartId is required." });

        var costPrice = await _costResolutionService.ResolveCostPerBaseUnitAsync(partId, variantId, cancellationToken);
        return Ok(new { costPrice });
    }

    /// <summary>
    /// Combines UnitCost + Discounts' resolve/item into one round trip — the POS calls both every
    /// time a part is added to the cart, so this halves the request count for that (frequent) case.
    /// The individual endpoints stay as-is for any other caller that only needs one of the two.
    /// </summary>
    [HttpGet("line-info")]
    public async Task<IActionResult> LineInfo(Guid partId, Guid? variantId, decimal unitPrice, CancellationToken cancellationToken)
    {
        if (partId == Guid.Empty)
            return BadRequest(new { message = "PartId is required." });

        var costPrice = await _costResolutionService.ResolveCostPerBaseUnitAsync(partId, variantId, cancellationToken);
        var discount = await _discountResolutionService.ResolveItemDiscountAsync(partId, variantId, unitPrice, cancellationToken);

        return Ok(new
        {
            costPrice,
            discountId = discount.DiscountId,
            discountName = discount.DiscountName,
            discountType = discount.DiscountType,
            discountValue = discount.DiscountValue,
            discountAmount = discount.DiscountAmount,
            appliedLevel = discount.AppliedLevel,
            finalPrice = discount.FinalPrice
        });
    }

    /// <summary>
    /// Requests a price-override approval — the industry-standard "manager authorizes this specific
    /// sale" pattern, not a role-based silent bypass. Verifies the given credentials belong to an
    /// active user who holds SalesApprovePriceOverride, then issues a short-lived (5 min), single-use
    /// token the checkout request can attach as PriceOverrideApprovalToken. One message for every
    /// failure mode (unknown user, wrong password, lockout, valid credentials but no permission) —
    /// this endpoint must not become an oracle for enumerating who can approve overrides.
    /// </summary>
    [HttpPost("request-override")]
    [EnableRateLimiting(RateLimiting.AuthPolicy)]
    public async Task<IActionResult> RequestOverride([FromBody] PriceOverrideRequest request, CancellationToken cancellationToken)
    {
        const string genericError = "Approval failed. Check the manager's username and password, and that they're allowed to approve price overrides.";

        if (request is null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = genericError });

        try
        {
            var manager = await _userManager.FindByNameAsync(request.Username)
                ?? await _userManager.FindByEmailAsync(request.Username);
            if (manager is null || !manager.IsActive)
                return BadRequest(new { message = genericError });

            var signInResult = await _signInManager.CheckPasswordSignInAsync(manager, request.Password, lockoutOnFailure: true);
            if (!signInResult.Succeeded)
                return BadRequest(new { message = genericError });

            var roles = await _userManager.GetRolesAsync(manager);
            var actingAsManager = new ClaimsPrincipal(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r))));
            var canApprove = await _permissionCheckService.UserHasPermissionAsync(actingAsManager, Permissions.SalesApprovePriceOverride, cancellationToken);
            if (!canApprove)
                return BadRequest(new { message = genericError });

            var approval = PriceOverrideApproval.Create(manager.Id, manager.UserName ?? request.Username, TimeSpan.FromMinutes(5));
            _dbContext.PriceOverrideApprovals.Add(approval);

            _dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = "PriceOverrideApproval",
                EntityId = approval.Id.ToString(),
                Action = "PRICE_OVERRIDE_ISSUED",
                PropertyName = "ApprovedBy",
                NewValue = approval.ApprovedByUsername,
                PerformedBy = approval.ApprovedByUsername,
                PerformedAt = approval.IssuedAt
            });

            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(new { token = approval.Id, expiresAt = approval.ExpiresAt });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error requesting price-override approval");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the approval request");
        }
    }

    public class PriceOverrideRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    private async Task<decimal> NormalizeUnitPriceAsync(Product part, decimal unitPrice, Guid? unitId, CancellationToken cancellationToken)
    {
        // Pricing validations are expressed in the part's stock base unit so they agree with the
        // quantities recorded at sale time (BaseUnitId is the stock accounting unit).
        var stockBaseUnit = part.BaseUnitId ?? part.UnitId;
        if (stockBaseUnit is null || unitId is null || unitId.Value == stockBaseUnit.Value)
            return unitPrice;

        var conversionFactor = await _unitConversionService.GetConversionFactorAsync(unitId.Value, stockBaseUnit.Value);
        if (conversionFactor <= 0)
            throw new InvalidOperationException("Invalid unit conversion factor.");

        return unitPrice / conversionFactor;
    }
}
