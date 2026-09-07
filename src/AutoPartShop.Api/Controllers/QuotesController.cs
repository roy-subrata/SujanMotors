using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Standalone quotation endpoint for the POS / Quick Sale screen. A quote is a DRAFT sales order
/// with no invoice, payment, or stock side effects — it can later be turned into a sale.
/// </summary>
[Route("api/v1/quotes")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesCreate)]
public class QuotesController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICurrencyConversionService _currencyConversionService;
    private readonly ICostResolutionService _costResolutionService;
    private readonly ICostFloorEnforcementService _costFloorService;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(
        ISalesOrderRepository salesOrderRepository,
        IProductRepository productRepository,
        ICodeGenerateService codeGenerateService,
        ICurrentUserService currentUserService,
        ICurrencyConversionService currencyConversionService,
        ICostResolutionService costResolutionService,
        ICostFloorEnforcementService costFloorService,
        ILogger<QuotesController> logger)
    {
        _salesOrderRepository = salesOrderRepository;
        _productRepository = productRepository;
        _codeGenerateService = codeGenerateService;
        _currentUserService = currentUserService;
        _currencyConversionService = currencyConversionService;
        _costResolutionService = costResolutionService;
        _costFloorService = costFloorService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> CreateQuote([FromBody] QuickSaleRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.Items is null || request.Items.Count == 0)
            return BadRequest(new { message = "At least one item is required" });

        try
        {
            var quoteNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);

            // A quote is a DRAFT sales order — never confirmed, so it holds no stock and raises no invoice.
            var quote = SalesOrder.Create(
                quoteNumber,
                request.CustomerId ?? Guid.Empty,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                null,
                request.TechnicianId,
                request.TechnicianName,
                string.Empty,
                request.Notes);

            var costFloorContext = await _costFloorService.PrepareContextAsync(
                request.Items.Select(i => i.PartId), quote.Currency, quote.SODate, request.PriceOverrideApprovalToken, cancellationToken);
            var costLookup = request.Items.Select(i => (i.PartId, i.ProductVariantId)).ToList();
            var resolvedCosts = await _costResolutionService.ResolveCostsPerBaseUnitAsync(costLookup, cancellationToken);
            var usedApproval = false;

            var lineNumber = 1;
            foreach (var item in request.Items)
            {
                var part = await _productRepository.GetByIdAsync(item.PartId, cancellationToken);
                if (part is null)
                    return BadRequest(new { message = $"Part with ID {item.PartId} not found" });

                var unitPrice = item.UnitPrice > 0 ? item.UnitPrice : part.SellingPrice;
                if (_costFloorService.EnforceCeiling(part.Name, unitPrice, part.SellingPrice, costFloorContext.RateToBase, costFloorContext.Approval))
                    usedApproval = true;

                var discountPerUnit = (unitPrice * item.Discount) / 100;

                // Cost-floor enforcement — no real unit conversion happens here (quantityInBaseUnit
                // just mirrors the entered quantity, see below), so the net price is already in
                // base-unit terms.
                var costPerBaseUnit = resolvedCosts.TryGetValue((item.PartId, item.ProductVariantId), out var resolvedCost) ? resolvedCost : 0m;
                var netUnitPrice = Math.Max(0, unitPrice - discountPerUnit);
                var minMarginPercent = costFloorContext.MinMarginFor(item.PartId);
                if (_costFloorService.EnforceLine(part.Name, netUnitPrice, costPerBaseUnit, minMarginPercent, costFloorContext.RateToBase, costFloorContext.Approval))
                    usedApproval = true;

                // Quotes don't move stock, so base-unit quantity is informational — mirror the entered quantity.
                var line = SalesOrderLine.Create(
                    quote.Id,
                    item.PartId,
                    item.Quantity,
                    unitPrice,
                    lineNumber++,
                    unitId: item.UnitId,
                    quantityInBaseUnit: item.Quantity,
                    discount: discountPerUnit,
                    description: string.IsNullOrWhiteSpace(item.PartName) ? part.Name : item.PartName,
                    productVariantId: item.ProductVariantId);
                quote.LineItems.Add(line);
            }

            quote.CalculateTotal();
            quote.SetTax(request.VatAmount);
            var quoteFx = await _currencyConversionService.ConvertToBaseWithRateAsync(quote.GrandTotal, quote.Currency, quote.SODate, cancellationToken);
            quote.SetFxBaseAmount(quoteFx.BaseAmount, quoteFx.RateToBase);
            quote.CreatedBy = _currentUserService.GetCurrentUsername();
            quote.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _salesOrderRepository.AddAsync(quote, cancellationToken);

            if (usedApproval)
                await _costFloorService.MarkApprovalConsumedAsync(costFloorContext.Approval!, quote.SONumber, cancellationToken);

            _logger.LogInformation("Quote {QuoteNumber} created for {CustomerName}", quoteNumber, request.CustomerName);

            return Ok(new { quoteId = quote.Id, quoteNumber });
        }
        catch (PriceOverrideRequiredException ex)
        {
            await _costFloorService.LogBlockedAttemptAsync(ex, _currentUserService.GetCurrentUsername(), cancellationToken);
            return BadRequest(new { message = ex.Message, code = "PRICE_OVERRIDE_REQUIRED", limitType = ex.LimitType, partName = ex.PartName });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the quote" });
        }
    }
}
