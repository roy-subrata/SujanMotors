using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.QuotationDtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/quotations")]
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
    AutoPartDbContext dbContext,
    ILogger<QuotationController> logger) : ControllerBase
{
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
                var quotationNumber = await codeGenerateService.GenerateAsync("QT", cancellationToken);
                await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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

                    var lineNumber = 1;
                    foreach (var lineRequest in request.Lines)
                    {
                        var line = await BuildLineAsync(quotation, lineRequest, lineNumber, cancellationToken);
                        quotation.LineItems.Add(line);
                        lineNumber++;
                    }

                    quotation.SetDiscountPercentage(request.Discount);
                    quotation.CalculateTotal();
                    quotation.SetTax(request.TaxAmount);

                    var username = currentUserService.GetCurrentUsername();
                    quotation.CreatedBy = username;
                    quotation.ModifiedBy = username;

                    await quotationRepository.AddAsync(quotation, cancellationToken);
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
    public async Task<IActionResult> ConvertToSalesOrder(Guid id, CancellationToken cancellationToken)
    {
        var quotation = await quotationRepository.GetByIdAsync(id, cancellationToken);
        if (quotation is null) return NotFound(new { message = "Quotation not found" });

        if (quotation.Status != "ACCEPTED")
            return BadRequest(new { message = $"Only ACCEPTED quotations can be converted. Current: {quotation.Status}" });

        try
        {
            SalesOrder? order = null;
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                var soNumber = await codeGenerateService.GenerateAsync("SO", cancellationToken);
                await using var tx = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    order = SalesOrder.Create(
                        soNumber,
                        quotation.CustomerId,
                        quotation.CustomerName,
                        quotation.CustomerEmail,
                        quotation.CustomerPhone,
                        notes: $"Converted from quotation {quotation.QuotationNumber}.",
                        currency: quotation.Currency);

                    var lineNumber = 1;
                    foreach (var ql in quotation.LineItems.OrderBy(l => l.LineNumber))
                    {
                        var part = ql.Part ?? await dbContext.Parts
                            .FirstOrDefaultAsync(p => p.Id == ql.PartId && !p.Isdeleted, cancellationToken);
                        if (part is null)
                            throw new InvalidOperationException($"Part {ql.PartId} on the quotation no longer exists.");

                        var (quantityInBaseUnit, unitId, baseUnitPrice) =
                            await ResolveUnitPricingAsync(part, ql.Quantity, ql.UnitId, ql.UnitPrice, cancellationToken);

                        var line = SalesOrderLine.Create(
                            order.Id, ql.PartId, ql.Quantity, ql.UnitPrice, lineNumber,
                            unitId, quantityInBaseUnit, ql.Discount, ql.Description, ql.ProductVariantId);

                        order.LineItems.Add(line);
                        lineNumber++;
                        _ = baseUnitPrice; // resolved for correctness of quantityInBaseUnit; line pricing stays in the quote's own unit
                    }

                    order.SetDiscountPercentage(quotation.DiscountPercentage);
                    order.CalculateTotal();
                    order.SetTax(quotation.TaxAmount);

                    var username = currentUserService.GetCurrentUsername();
                    order.SetCashier(currentUserService.GetCurrentUserGuid(), username);
                    order.CreatedBy = username;
                    order.ModifiedBy = username;

                    await salesOrderRepository.AddAsync(order, cancellationToken);

                    quotation.MarkAsConverted(order.Id);
                    quotation.ModifiedBy = username;
                    await quotationRepository.UpdateAsync(quotation, cancellationToken);

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

        var pdfBytes = new QuotationDocument(data, shop).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"quotation-{quotation.QuotationNumber}.pdf");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<QuotationLine> BuildLineAsync(
        Quotation quotation, CreateQuotationLineRequest lineRequest, int lineNumber, CancellationToken cancellationToken)
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

        return QuotationLine.Create(
            quotation.Id, lineRequest.PartId, lineRequest.Quantity, unitPrice, lineNumber,
            lineRequest.UnitId ?? part.UnitId, lineRequest.Discount, string.Empty, lineRequest.ProductVariantId);
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
        if (unitId.Value == part.UnitId.Value) return (quantity, unitId, unitPrice);

        var conversionFactor = await unitConversionService.GetConversionFactorAsync(unitId.Value, part.UnitId.Value);
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
