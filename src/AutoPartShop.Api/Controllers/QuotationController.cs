using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.QuotationDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;
[Route("api/v1/quotations")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class QuotationController(
    IQuotationRepository quotationRepository,
    ISalesOrderRepository salesOrderRepository,
    ICodeGenerateService codeGenerateService,
    ICurrentUserService currentUserService,
    IUnitConversionService unitConversionService,
    ICurrencyConversionService currencyConversionService,
    AutoPartDbContext dbContext,
    ILogger<QuotationController> logger,
    IApplicationSettingsRepository settingsRepository,
    ICostResolutionService costResolutionService,
    ICostFloorEnforcementService costFloorService) : ControllerBase
{
    /// <summary>
    /// Configurable document-number prefix (Company Profile &gt; Document Numbering) —
    /// falls back to the historical hardcoded prefix if no setting has been configured yet.
    /// </summary>
    private async Task<string> GetPrefixAsync(string settingKey, string fallback, CancellationToken cancellationToken)
    {
        var value = await settingsRepository.GetValueAsync(settingKey, cancellationToken);
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    [HttpPost]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Create(CreateQuotationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerName))
                return BadRequest(new { message = "CustomerId and CustomerName are required" });

            if (request.Lines is not { Count: > 0 })
                return BadRequest(new { message = "At least one line item is required." });

            Quotation? quotation = null;
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                // Allocated inside the transaction: CodeGenerateService enlists in the ambient
                // transaction, so a rejected create rolls the sequence back instead of burning
                // a number. Fiscal documents are expected to be gapless.
                var quotationNumber = await codeGenerateService.GenerateAsync(await GetPrefixAsync("QUOTATION_NUMBER_PREFIX", "QT", cancellationToken), cancellationToken);
                try
                {
                    quotation = Quotation.Create(
                        quotationNumber,
                        request.CustomerId,
                        request.CustomerName,
                        request.CustomerEmail,
                        request.CustomerPhone,
                        request.ValidUntil,
                        request.Notes,
                        request.Currency);

                    var costFloorContext = await costFloorService.PrepareContextAsync(
                        request.Lines.Select(l => l.PartId), quotation.Currency, DateTime.UtcNow, request.PriceOverrideApprovalToken, cancellationToken);
                    var costLookup = request.Lines.Select(l => (l.PartId, l.ProductVariantId)).ToList();
                    var resolvedCosts = await costResolutionService.ResolveCostsPerBaseUnitAsync(costLookup, cancellationToken);

                    var lineNumber = 1;
                    var lineFloorChecks = new List<LineCostFloorCheck>();
                    var usedApproval = false;
                    foreach (var lineRequest in request.Lines)
                    {
                        var costPerBaseUnit = resolvedCosts.TryGetValue((lineRequest.PartId, lineRequest.ProductVariantId), out var resolvedCost) ? resolvedCost : 0m;
                        var (line, lineCheck, lineUsedApproval) = await BuildLineAsync(quotation, lineRequest, lineNumber, costPerBaseUnit, costFloorContext, cancellationToken);
                        quotation.LineItems.Add(line);
                        lineFloorChecks.Add(lineCheck);
                        usedApproval |= lineUsedApproval;
                        lineNumber++;
                    }

                    quotation.SetDiscountPercentage(request.Discount);
                    quotation.CalculateTotal();
                    quotation.SetTax(request.TaxAmount);

                    // quotation.DiscountAmount is the whole quotation's cart-level (percentage) discount
                    // — a line whose own discount passed the floor above can still end up below cost
                    // once this is spread across the quote. Blocked here too (not just on conversion to
                    // a real sale) so pricing is consistent whether it's still a draft or already a sale.
                    if (costFloorService.EnforceCartDiscount(lineFloorChecks, quotation.SubTotal, quotation.DiscountAmount, costFloorContext.RateToBase, costFloorContext.Approval))
                        usedApproval = true;

                    var username = currentUserService.GetCurrentUsername();
                    quotation.CreatedBy = username;
                    quotation.ModifiedBy = username;

                    await quotationRepository.AddAsync(quotation, cancellationToken);

                    if (usedApproval)
                        await costFloorService.MarkApprovalConsumedAsync(costFloorContext.Approval!, quotation.QuotationNumber, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return CreatedAtAction(nameof(GetById), new { id = quotation!.Id }, MapToResponse(quotation!));
        }
        catch (PriceOverrideRequiredException ex)
        {
            await costFloorService.LogBlockedAttemptAsync(ex, currentUserService.GetCurrentUsername(), cancellationToken);
            return BadRequest(new { message = ex.Message, code = "PRICE_OVERRIDE_REQUIRED", limitType = ex.LimitType, partName = ex.PartName });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating quotation");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the quotation");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        return Ok(MapToResponse(quotation));
    }

    [HttpGet("number/{quotationNumber}")]
    public async Task<IActionResult> GetByNumber(string quotationNumber, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByNumberAsync(quotationNumber, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        return Ok(MapToResponse(quotation));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var quotations = await quotationRepository.GetByCustomerAsync(customerId, cancellationToken);
        return Ok(quotations.Select(MapToResponse));
    }

    [HttpPost("list")]
    public async Task<IActionResult> Search(QuotationQuery query, CancellationToken cancellationToken)
    {
        var (quotations, totalCount) = await quotationRepository.SearchPagedAsync(query, cancellationToken);
        return Ok(new
        {
            data = quotations.Select(MapToResponse),
            totalCount,
            query.PageNumber,
            query.PageSize
        });
    }

    [HttpPatch("{id:guid}/send")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Send(Guid id, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        try
        {
            quotation.Send();
            quotation.ModifiedBy = currentUserService.GetCurrentUsername();
            await quotationRepository.UpdateAsync(quotation, cancellationToken);
            return Ok(MapToResponse(quotation));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/accept")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Accept(Guid id, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        try
        {
            quotation.Accept();
            quotation.ModifiedBy = currentUserService.GetCurrentUsername();
            await quotationRepository.UpdateAsync(quotation, cancellationToken);
            return Ok(MapToResponse(quotation));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/reject")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Reject(Guid id, RejectQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        try
        {
            quotation.Reject(request.Reason);
            quotation.ModifiedBy = currentUserService.GetCurrentUsername();
            await quotationRepository.UpdateAsync(quotation, cancellationToken);
            return Ok(MapToResponse(quotation));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Converts an ACCEPTED quotation into a new SalesOrder by copying its lines, then marks the
    /// quote CONVERTED. No stock is reserved or checked here — that happens the same way it would
    /// for any manually-created SalesOrder, at confirmation.
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> ConvertToSalesOrder(Guid id, [FromBody] ConvertQuotationRequest request, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        if (quotation.Status != QuotationStatus.ACCEPTED)
            return BadRequest(new { message = $"Only ACCEPTED quotations can be converted. Current: {quotation.Status}" });

        // The converted order deducts stock at Confirm, so it needs a warehouse. Without this
        // the conversion succeeds and the resulting order can never be confirmed.
        if (request is null || request.WarehouseId == Guid.Empty)
            return BadRequest(new { message = "WarehouseId is required" });

        var warehouseExists = await dbContext.Warehouses
            .AnyAsync(w => w.Id == request.WarehouseId && !w.Isdeleted, cancellationToken);
        if (!warehouseExists)
            return BadRequest(new { message = "Warehouse not found" });

        try
        {
            SalesOrder? order = null;
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                // Allocated inside the transaction: CodeGenerateService enlists in the ambient
                // transaction, so a rejected create rolls the sequence back instead of burning
                // a number. Fiscal documents are expected to be gapless.
                var soNumber = await codeGenerateService.GenerateAsync(await GetPrefixAsync("SALES_ORDER_NUMBER_PREFIX", "SO", cancellationToken), cancellationToken);
                try
                {
                    order = SalesOrder.Create(
                        soNumber,
                        quotation.CustomerId,
                        quotation.CustomerName,
                        quotation.CustomerEmail,
                        quotation.CustomerPhone,
                        request.WarehouseId,
                        notes: $"Converted from quotation {quotation.QuotationNumber}.",
                        currency: quotation.Currency);

                    // A quotation's stored price/discount was never floor-checked against cost at any
                    // point up to here (Create() checks it against the cost AT QUOTE TIME, but cost
                    // may have moved since, and the copy below builds SalesOrderLine directly rather
                    // than through SalesOrderController's validated path) — so a below-cost quotation
                    // must not silently become a real, stock-consuming sale here.
                    var costFloorContext = await costFloorService.PrepareContextAsync(
                        quotation.LineItems.Select(l => l.PartId), order.Currency, order.SODate, request.PriceOverrideApprovalToken, cancellationToken);
                    var costLookup = quotation.LineItems.Select(l => (l.PartId, l.ProductVariantId)).ToList();
                    var resolvedCosts = await costResolutionService.ResolveCostsPerBaseUnitAsync(costLookup, cancellationToken);
                    var lineFloorChecks = new List<LineCostFloorCheck>();
                    var usedApproval = false;

                    var lineNumber = 1;
                    foreach (var ql in quotation.LineItems.OrderBy(l => l.LineNumber))
                    {
                        var part = ql.Part ?? await dbContext.Parts
                            .FirstOrDefaultAsync(p => p.Id == ql.PartId && !p.Isdeleted, cancellationToken);
                        if (part is null)
                            throw new InvalidOperationException($"Part {ql.PartId} on the quotation no longer exists.");

                        if (costFloorService.EnforceCeiling(part.Name, ql.UnitPrice, part.SellingPrice, costFloorContext.RateToBase, costFloorContext.Approval))
                            usedApproval = true;

                        var (quantityInBaseUnit, unitId, baseUnitPrice) =
                            await ResolveUnitPricingAsync(part, ql.Quantity, ql.UnitId, ql.UnitPrice, cancellationToken);

                        var costPerBaseUnit = resolvedCosts.TryGetValue((ql.PartId, ql.ProductVariantId), out var resolvedCost) ? resolvedCost : 0m;
                        var unitFactor = ql.Quantity > 0 && quantityInBaseUnit > 0 ? (decimal)quantityInBaseUnit / ql.Quantity : 1m;
                        var discountPerBaseUnit = unitFactor <= 0 ? ql.Discount : ql.Discount / unitFactor;
                        var netUnitPriceInBaseUnit = Math.Max(0, baseUnitPrice - discountPerBaseUnit);
                        var minMarginPercent = costFloorContext.MinMarginFor(ql.PartId);
                        if (costFloorService.EnforceLine(part.Name, netUnitPriceInBaseUnit, costPerBaseUnit, minMarginPercent, costFloorContext.RateToBase, costFloorContext.Approval))
                            usedApproval = true;

                        var line = SalesOrderLine.Create(
                            order.Id, ql.PartId, ql.Quantity, ql.UnitPrice, lineNumber,
                            unitId, quantityInBaseUnit, ql.Discount, ql.Description, ql.ProductVariantId);

                        order.LineItems.Add(line);
                        lineFloorChecks.Add(new LineCostFloorCheck(part.Name, line.TotalPrice, line.Quantity, line.QuantityInBaseUnit, netUnitPriceInBaseUnit, costPerBaseUnit, minMarginPercent));
                        lineNumber++;
                    }

                    order.SetDiscountPercentage(quotation.DiscountPercentage);
                    order.CalculateTotal();
                    order.SetTax(quotation.TaxAmount);

                    // order.DiscountAmount is the whole order's cart-level (percentage) discount — a
                    // line whose own discount passed the floor above can still end up below cost once
                    // this is spread across the order.
                    if (costFloorService.EnforceCartDiscount(lineFloorChecks, order.SubTotal, order.DiscountAmount, costFloorContext.RateToBase, costFloorContext.Approval))
                        usedApproval = true;

                    var orderFx = await currencyConversionService.ConvertToBaseWithRateAsync(order.GrandTotal, order.Currency, order.SODate, cancellationToken);
                    order.SetFxBaseAmount(orderFx.BaseAmount, orderFx.RateToBase);

                    var username = currentUserService.GetCurrentUsername();
                    order.SetCashier(currentUserService.GetCurrentUserGuid(), username);
                    order.CreatedBy = username;
                    order.ModifiedBy = username;

                    await salesOrderRepository.AddAsync(order, cancellationToken);

                    quotation.MarkAsConverted(order.Id);
                    quotation.ModifiedBy = username;
                    await quotationRepository.UpdateAsync(quotation, cancellationToken);

                    if (usedApproval)
                        await costFloorService.MarkApprovalConsumedAsync(costFloorContext.Approval!, order.SONumber, cancellationToken);

                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(new ConvertQuotationResponse
            {
                QuotationId = quotation.Id,
                SalesOrderId = order!.Id,
                SONumber = order!.SONumber
            });
        }
        catch (PriceOverrideRequiredException ex)
        {
            await costFloorService.LogBlockedAttemptAsync(ex, currentUserService.GetCurrentUsername(), cancellationToken);
            return BadRequest(new { message = ex.Message, code = "PRICE_OVERRIDE_REQUIRED", limitType = ex.LimitType, partName = ex.PartName });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error converting quotation {QuotationId} to a sales order", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while converting the quotation");
        }
    }

    /// <summary>Download the Quotation as a PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        var customer = quotation.Customer;
        var address = customer is null
            ? string.Empty
            : string.Join(", ", new[] { customer.BillingAddress, customer.City, customer.PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);

        var data = new QuotationDocumentData(
            QuotationNumber: quotation.QuotationNumber,
            QuoteDate: quotation.QuoteDate,
            ValidUntil: quotation.ValidUntil,
            CustomerName: quotation.CustomerName,
            CustomerAddress: address,
            CustomerPhone: quotation.CustomerPhone,
            Lines: quotation.LineItems
                .OrderBy(l => l.LineNumber)
                .Select((l, i) => new QuotationDocumentLine(
                    SlNo: i + 1,
                    PartNumber: l.Part?.PartNumber?.Value ?? l.Part?.SKU ?? string.Empty,
                    DisplayName: l.ProductVariant is not null
                        ? $"{l.Part?.Name} - {l.ProductVariant.Name}"
                        : (l.Part?.Name ?? l.Description),
                    LocalName: l.Part?.LocalName,
                    Quantity: l.Quantity,
                    UnitSymbol: l.Unit?.Symbol ?? string.Empty,
                    UnitPrice: l.UnitPrice,
                    LineTotal: l.TotalPrice))
                .ToList(),
            SubTotal: quotation.SubTotal,
            DiscountAmount: quotation.DiscountAmount,
            TaxAmount: quotation.TaxAmount,
            GrandTotal: quotation.GrandTotal,
            Notes: quotation.Notes);

        var pdfBytes = new QuotationDocument(data, shop, DocTheme.Default with { Lang = this.GetLanguage() }).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"quotation-{quotation.QuotationNumber}.pdf");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<(QuotationLine Line, LineCostFloorCheck Check, bool UsedApproval)> BuildLineAsync(
        Quotation quotation, CreateQuotationLineRequest lineRequest, int lineNumber,
        decimal costPerBaseUnit, CostFloorContext costFloorContext, CancellationToken cancellationToken)
    {
        if (lineRequest.Quantity <= 0)
            throw new ArgumentException($"Line item quantity must be greater than zero (got {lineRequest.Quantity}).");

        var part = await dbContext.Parts
            .FirstOrDefaultAsync(p => p.Id == lineRequest.PartId && !p.Isdeleted, cancellationToken);
        if (part is null)
            throw new ArgumentException($"Part with ID {lineRequest.PartId} not found");

        ProductVariant? variant = null;
        if (lineRequest.ProductVariantId.HasValue)
        {
            variant = await dbContext.Set<ProductVariant>()
                .FirstOrDefaultAsync(v => v.Id == lineRequest.ProductVariantId.Value
                                       && v.PartId == lineRequest.PartId
                                       && v.IsActive, cancellationToken);
            if (variant is null)
                throw new ArgumentException($"Variant {lineRequest.ProductVariantId} not found for part '{part.Name}'");
        }

        // Price resolution: manual entry → variant price → product base price → error.
        // A quotation is allowed to quote a price the product doesn't currently carry (e.g. a
        // special negotiated rate), so this only fills in a default — it never blocks the manual
        // entry a salesperson typed in.
        var unitPrice = lineRequest.UnitPrice > 0
            ? lineRequest.UnitPrice
            : (variant?.SellingPrice > 0 ? variant.SellingPrice : part.SellingPrice);

        if (unitPrice <= 0)
            throw new ArgumentException($"No selling price set for '{part.Name}'. Please set a selling price on the product or variant, or enter one manually.");

        var usedApproval = costFloorService.EnforceCeiling(part.Name, unitPrice, part.SellingPrice, costFloorContext.RateToBase, costFloorContext.Approval);

        var (quantityInBaseUnit, unitId, baseUnitPrice) = await ResolveUnitPricingAsync(
            part, lineRequest.Quantity, lineRequest.UnitId, unitPrice, cancellationToken);

        // Cost-floor enforcement — same base-unit normalization SalesOrderController uses, so a
        // below-cost line is caught here too, not just when the quote later converts to a real sale.
        var unitFactor = lineRequest.Quantity > 0 && quantityInBaseUnit > 0 ? (decimal)quantityInBaseUnit / lineRequest.Quantity : 1m;
        var discountPerBaseUnit = unitFactor <= 0 ? lineRequest.Discount : lineRequest.Discount / unitFactor;
        var netUnitPriceInBaseUnit = Math.Max(0, baseUnitPrice - discountPerBaseUnit);
        var minMarginPercent = costFloorContext.MinMarginFor(lineRequest.PartId);
        if (costFloorService.EnforceLine(part.Name, netUnitPriceInBaseUnit, costPerBaseUnit, minMarginPercent, costFloorContext.RateToBase, costFloorContext.Approval))
            usedApproval = true;

        var line = QuotationLine.Create(
            quotation.Id, lineRequest.PartId, lineRequest.Quantity, unitPrice, lineNumber,
            unitId ?? part.UnitId, lineRequest.Discount, string.Empty, lineRequest.ProductVariantId);
        var check = new LineCostFloorCheck(part.Name, line.TotalPrice, line.Quantity, quantityInBaseUnit, netUnitPriceInBaseUnit, costPerBaseUnit, minMarginPercent);
        return (line, check, usedApproval);
    }

    /// <summary>
    /// Resolves a line's chosen unit against the part's own base unit, converting quantity and
    /// price when they differ. Mirrors SalesOrderController's private helper of the same shape —
    /// duplicated rather than extracted since it's the only place outside SalesOrderController that
    /// needs it, and pulling it into a shared service is more scope than this conversion step needs.
    /// </summary>
    private async Task<(int quantityInBaseUnit, Guid? unitId, decimal baseUnitPrice)> ResolveUnitPricingAsync(
        Product part, int quantity, Guid? unitId, decimal unitPrice, CancellationToken cancellationToken)
    {
        if (part.UnitId is null) return (quantity, unitId, unitPrice);
        if (!unitId.HasValue) return (quantity, part.UnitId, unitPrice);

        // Stock is tracked in the part's BASE unit — convert display quantity to BaseUnitId so
        // quotation conversion agrees with GRN posting, which also anchors on BaseUnitId.
        var stockBaseUnit = part.BaseUnitId ?? part.UnitId;
        if (unitId.Value == stockBaseUnit.Value) return (quantity, unitId, unitPrice);

        var conversionFactor = await unitConversionService.GetConversionFactorAsync(unitId.Value, stockBaseUnit.Value);
        if (conversionFactor <= 0)
            throw new InvalidOperationException("Invalid unit conversion factor.");

        var quantityInBaseUnit = (int)Math.Round(quantity * conversionFactor, MidpointRounding.AwayFromZero);
        var baseUnitPrice = unitPrice / conversionFactor;
        return (quantityInBaseUnit, unitId, baseUnitPrice);
    }

    private static QuotationResponse MapToResponse(Quotation q) => new()
    {
        Id = q.Id,
        QuotationNumber = q.QuotationNumber,
        CustomerId = q.CustomerId,
        CustomerName = q.CustomerName,
        CustomerEmail = q.CustomerEmail,
        CustomerPhone = q.CustomerPhone,
        QuoteDate = q.QuoteDate,
        ValidUntil = q.ValidUntil,
        Status = q.Status,
        IsExpired = q.IsExpired,
        SubTotal = q.SubTotal,
        DiscountPercentage = q.DiscountPercentage,
        DiscountAmount = q.DiscountAmount,
        TotalAmount = q.TotalAmount,
        TaxAmount = q.TaxAmount,
        GrandTotal = q.GrandTotal,
        Currency = q.Currency,
        Notes = q.Notes,
        ConvertedToSalesOrderId = q.ConvertedToSalesOrderId,
        CreatedAt = q.CreatedDate,
        Lines = q.LineItems.OrderBy(l => l.LineNumber).Select(l => new QuotationLineResponse
        {
            Id = l.Id,
            PartId = l.PartId,
            PartName = l.Part?.Name ?? string.Empty,
            VariantName = l.ProductVariant?.Name,
            SKU = l.ProductVariant?.SKU ?? l.Part?.SKU ?? string.Empty,
            Quantity = l.Quantity,
            UnitPrice = l.UnitPrice,
            Discount = l.Discount,
            TotalPrice = l.TotalPrice,
            UnitSymbol = l.Unit?.Symbol ?? string.Empty
        }).ToList()
    };
}
