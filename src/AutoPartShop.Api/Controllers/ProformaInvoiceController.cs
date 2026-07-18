using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.ProformaInvoiceDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/proforma-invoices")]
[Route("api/v1/proforma-invoices")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class ProformaInvoiceController(
    IProformaInvoiceRepository proformaRepository,
    ISalesOrderRepository salesOrderRepository,
    ICodeGenerateService codeGenerateService,
    ICurrentUserService currentUserService,
    ILogger<ProformaInvoiceController> logger) : ControllerBase
{
    [HttpPost]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Create(CreateProformaInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.SalesOrderId == Guid.Empty)
                return BadRequest(new { message = "SalesOrderId is required" });

            var order = await salesOrderRepository.GetByIdAsync(request.SalesOrderId, cancellationToken);
            if (order is null)
                return BadRequest(new { message = "Sales order not found" });

            var proformaNumber = await codeGenerateService.GenerateAsync("PI", cancellationToken);
            var username = currentUserService.GetCurrentUsername();

            var proforma = ProformaInvoice.Create(
                proformaNumber, request.SalesOrderId, request.ValidUntil, username, request.Notes);
            proforma.CreatedBy = username;
            proforma.ModifiedBy = username;

            await proformaRepository.AddAsync(proforma, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = proforma.Id }, MapToResponse(proforma, order));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating proforma invoice");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the proforma invoice");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var proforma = await proformaRepository.GetByIdAsync(id, cancellationToken);
        if (proforma is null) return NotFound(new { message = "Proforma invoice not found" });

        return Ok(MapToResponse(proforma, proforma.SalesOrder));
    }

    [HttpGet("sales-order/{salesOrderId:guid}")]
    public async Task<IActionResult> GetBySalesOrder(Guid salesOrderId, CancellationToken cancellationToken)
    {
        var proformas = await proformaRepository.GetBySalesOrderAsync(salesOrderId, cancellationToken);
        return Ok(proformas.Select(p => MapToResponse(p, p.SalesOrder)));
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var (proformas, totalCount) = await proformaRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken);
        return Ok(new
        {
            data = proformas.Select(p => MapToResponse(p, p.SalesOrder)),
            totalCount,
            pageNumber,
            pageSize
        });
    }

    /// <summary>
    /// Download the Proforma Invoice as a PDF. Line items and totals are read live from the linked
    /// SalesOrder — the proforma itself stores no pricing data, so it can never drift from the order.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken)
    {
        var proforma = await proformaRepository.GetByIdAsync(id, cancellationToken);
        if (proforma is null) return NotFound(new { message = "Proforma invoice not found" });

        var order = proforma.SalesOrder;
        if (order is null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Linked sales order could not be loaded" });

        var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);

        var customer = order.Customer;
        var address = customer is null
            ? string.Empty
            : string.Join(", ", new[] { customer.BillingAddress, customer.City, customer.PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var data = new ProformaInvoiceDocumentData(
            ProformaNumber: proforma.ProformaNumber,
            IssueDate: proforma.IssueDate,
            ValidUntil: proforma.ValidUntil,
            RefOrderNumber: order.SONumber,
            CustomerName: order.CustomerName,
            CustomerAddress: address,
            CustomerPhone: order.CustomerPhone,
            Lines: order.LineItems
                .OrderBy(l => l.LineNumber)
                .Select((l, i) => new ProformaInvoiceDocumentLine(
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
            SubTotal: order.SubTotal,
            DiscountAmount: order.DiscountAmount,
            TaxAmount: order.TaxAmount,
            GrandTotal: order.GrandTotal,
            Notes: proforma.Notes);

        var pdfBytes = new ProformaInvoiceDocument(data, shop).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"proforma-invoice-{proforma.ProformaNumber}.pdf");
    }

    private static ProformaInvoiceResponse MapToResponse(ProformaInvoice p, SalesOrder? order) => new()
    {
        Id = p.Id,
        ProformaNumber = p.ProformaNumber,
        SalesOrderId = p.SalesOrderId,
        SONumber = order?.SONumber ?? string.Empty,
        CustomerName = order?.CustomerName ?? string.Empty,
        GrandTotal = order?.GrandTotal ?? 0,
        IssueDate = p.IssueDate,
        ValidUntil = p.ValidUntil,
        Status = p.Status,
        IsExpired = p.IsExpired,
        Notes = p.Notes,
        CreatedAt = p.CreatedDate
    };
}
