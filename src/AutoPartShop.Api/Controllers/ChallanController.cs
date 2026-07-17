using AutoPartShop.Api.Common;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/challans")]
[Route("api/v1/challans")]
[ApiController]
[HasPermission(Permissions.SalesView)]
[Produces("application/json")]
public class ChallanController(
    IChallanRepository _challanRepo,
    ISalesOrderRepository _soRepo,
    ICustomerRepository _customerRepo,
    ICodeGenerateService _codeGen,
    ICurrentUserService _currentUser,
    AutoPartDbContext _db,
    ILogger<ChallanController> _logger) : ControllerBase
{
    // â”€â”€ Generate challan for an order â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Generates a Challan (delivery note) for a Confirmed or ReadyForDelivery order.
    /// Marks the order as ReadyForDelivery if it was still in Confirmed state.
    /// </summary>
    [HttpPost("sales-order/{salesOrderId:guid}")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Generate(
        Guid salesOrderId,
        [FromBody] GenerateChallanRequest? req,
        CancellationToken ct)
    {
        var so = await _soRepo.GetByIdAsync(salesOrderId, ct);
        if (so is null)
            return NotFound(ApiError.NotFound("Sales order not found", Request.Path));

        if (so.Status is not ("CONFIRMED" or "READY_FOR_DELIVERY"))
            return BadRequest(ApiError.BusinessRule(
                $"Challan can only be generated for Confirmed or Ready-For-Delivery orders. Current status: {so.Status}",
                Request.Path));

        var strategy = _db.Database.CreateExecutionStrategy();
        Challan? challan = null;

        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                // Re-check status inside the transaction to prevent race conditions
                var freshSo = await _db.SalesOrders
                    .FirstOrDefaultAsync(s => s.Id == salesOrderId && !s.Isdeleted, ct);
                if (freshSo is null || freshSo.Status is not ("CONFIRMED" or "READY_FOR_DELIVERY"))
                    throw new InvalidOperationException(
                        $"Challan can no longer be generated â€” order status is '{freshSo?.Status ?? "deleted"}'.");

                var challanNumber = await _codeGen.GenerateAsync("CHN", ct);
                var deliveryAddress = req?.DeliveryAddress ?? so.DeliveryAddress;
                var receiverName = req?.ReceiverName ?? string.Empty;
                var receiverPhone = req?.ReceiverPhone ?? string.Empty;
                var notes = req?.Notes ?? string.Empty;

                challan = Challan.Create(challanNumber, so.Id, deliveryAddress, receiverName, receiverPhone, notes,
                req?.TransportCompany ?? string.Empty,
                req?.VehicleNumber ?? string.Empty,
                req?.DriverName ?? string.Empty,
                req?.DriverPhone ?? string.Empty);
                challan.CreatedBy = _currentUser.GetCurrentUsername();
                challan.ModifiedBy = _currentUser.GetCurrentUsername();

                // Copy SO line items to challan lines
                int lineNo = 1;
                foreach (var line in so.LineItems.OrderBy(l => l.LineNumber))
                {
                    var cl = ChallanLine.Create(
                        challan.Id,
                        line.PartId,
                        line.Quantity,
                        line.Part?.Name ?? string.Empty,
                        line.Part?.SKU ?? string.Empty,
                        line.ProductVariant != null
                            ? $"{line.Part?.Name} - {line.ProductVariant.Name}"
                            : (line.Part?.Name ?? string.Empty),
                        line.Unit?.Name ?? string.Empty,
                        lineNo++,
                        line.ProductVariantId);
                    cl.CreatedBy = _currentUser.GetCurrentUsername();
                    cl.ModifiedBy = _currentUser.GetCurrentUsername();
                    challan.Lines.Add(cl);
                }

                // Link to invoice if one exists
                var invoice = await _db.Invoices
                    .FirstOrDefaultAsync(i => i.SalesOrderId == so.Id && !i.Isdeleted, ct);
                if (invoice != null)
                    challan.LinkToInvoice(invoice.Id);

                await _db.Challans.AddAsync(challan, ct);

                // Transition SO to READY_FOR_DELIVERY if still CONFIRMED
                if (so.Status == "CONFIRMED")
                {
                    so.MarkAsReadyForDelivery();
                    so.ModifiedBy = _currentUser.GetCurrentUsername();
                    await _soRepo.UpdateAsync(so, ct);
                }

                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        return CreatedAtAction(nameof(GetById), new { id = challan!.Id },
            ApiResponse<object>.Ok(MapToResponse(challan!)));
    }

    // â”€â”€ Get challans for an order â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("sales-order/{salesOrderId:guid}")]
    public async Task<IActionResult> GetBySalesOrder(Guid salesOrderId, CancellationToken ct)
    {
        var challans = await _challanRepo.GetBySalesOrderAsync(salesOrderId, ct);
        return Ok(ApiResponse<object>.Ok(challans.Select(MapToResponse)));
    }

    // â”€â”€ Get single challan â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var challan = await _challanRepo.GetByIdAsync(id, ct);
        if (challan is null)
            return NotFound(ApiError.NotFound("Challan not found", Request.Path));

        return Ok(ApiResponse<object>.Ok(MapToResponse(challan)));
    }

    /// <summary>
    /// Download the Delivery Challan as a PDF. Quantities only — no prices, per the document spec.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var challan = await _db.Set<Challan>()
            .AsNoTracking()
            .Include(c => c.Lines)
            .Include(c => c.Invoice)
            .Include(c => c.SalesOrder!).ThenInclude(so => so.Customer)
            .FirstOrDefaultAsync(c => c.Id == id && !c.Isdeleted, ct);

        if (challan is null)
            return NotFound(ApiError.NotFound("Challan not found", Request.Path));

        // Part numbers and local names aren't denormalised onto ChallanLine, so pull them for the
        // lines in play rather than N+1 per line.
        var partIds = challan.Lines.Select(l => l.PartId).Distinct().ToList();
        var parts = await _db.Set<Product>()
            .AsNoTracking()
            .Where(p => partIds.Contains(p.Id))
            .Select(p => new { p.Id, PartNumber = p.PartNumber!.Value, p.LocalName })
            .ToDictionaryAsync(p => p.Id, ct);

        var settings = await _db.Set<ApplicationSettings>()
            .AsNoTracking()
            .Where(s => !s.Isdeleted)
            .ToListAsync(ct);

        string Get(string key, string fallback = "")
        {
            var v = settings.FirstOrDefault(s => s.Key == key)?.Value;
            return string.IsNullOrWhiteSpace(v) ? fallback : v;
        }

        var shopProfile = new ShopProfile(
            Name: Get("SHOP_NAME"),
            Address: Get("SHOP_ADDRESS"),
            Phone: Get("SHOP_PHONE"),
            Email: Get("SHOP_EMAIL"),
            TaxNo: Get("SHOP_TAX_NUMBER"),
            Tagline: Get("SHOP_TAGLINE"),
            FooterText: Get("INVOICE_FOOTER_TEXT", "Thank you for your business!"),
            BankDetails: Get("SHOP_BANK_DETAILS"));

        var customer = challan.SalesOrder?.Customer;
        var customerName = customer is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(customer.CompanyName)
                ? customer.CompanyName
                : $"{customer.FirstName} {customer.LastName}".Trim();

        var data = new ChallanDocumentData(
            ChallanNumber: challan.ChallanNumber,
            ChallanDate: challan.IssuedAt ?? challan.CreatedDate,
            SalesOrderNumber: challan.SalesOrder?.SONumber ?? string.Empty,
            InvoiceNumber: challan.Invoice?.InvoiceNumber ?? string.Empty,
            CustomerName: customerName,
            DeliveryAddress: challan.DeliveryAddress,
            ReceiverName: challan.ReceiverName,
            ReceiverPhone: challan.ReceiverPhone,
            TransportCompany: challan.TransportCompany,
            VehicleNumber: challan.VehicleNumber,
            DriverName: challan.DriverName,
            DriverPhone: challan.DriverPhone,
            DispatchedAt: challan.IssuedAt,
            Lines: challan.Lines
                .OrderBy(l => l.LineNumber)
                .Select((l, i) => new ChallanDocumentLine(
                    SlNo: i + 1,
                    PartNumber: parts.TryGetValue(l.PartId, out var p) ? p.PartNumber ?? string.Empty : l.PartSku,
                    DisplayName: !string.IsNullOrWhiteSpace(l.DisplayName) ? l.DisplayName : l.PartName,
                    LocalName: parts.TryGetValue(l.PartId, out var p2) ? p2.LocalName : null,
                    Quantity: l.Quantity,
                    UnitName: l.UnitName))
                .ToList(),
            Notes: challan.Notes);

        var pdfBytes = new DeliveryChallanDocument(data, shopProfile).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"challan-{challan.ChallanNumber}.pdf");
    }

    // â”€â”€ Pending challans (delivery queue) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Returns all DRAFT and ISSUED challans â€” the delivery staff's dispatch queue.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var draft = await _challanRepo.GetByStatusAsync("DRAFT", ct);
        var issued = await _challanRepo.GetByStatusAsync("ISSUED", ct);
        var all = draft.Concat(issued).OrderBy(c => c.CreatedDate);
        return Ok(ApiResponse<object>.Ok(all.Select(MapToResponse)));
    }

    // â”€â”€ Issue challan â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>Issues the challan â€” it can now travel with the goods.</summary>
    [HttpPatch("{id:guid}/issue")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Issue(Guid id, CancellationToken ct)
    {
        var challan = await _challanRepo.GetByIdAsync(id, ct);
        if (challan is null)
            return NotFound(ApiError.NotFound("Challan not found", Request.Path));

        challan.Issue();
        challan.ModifiedBy = _currentUser.GetCurrentUsername();
        await _challanRepo.UpdateAsync(challan, ct);

        return Ok(ApiResponse<object>.Ok(MapToResponse(challan)));
    }

    // â”€â”€ Deliver (with challan) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Marks challan as delivered and transitions the linked Sales Order to DELIVERED.
    /// Also issues the invoice if it is still in DRAFT status.
    /// </summary>
    [HttpPatch("{id:guid}/deliver")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> MarkDelivered(
        Guid id,
        [FromBody] DeliverChallanRequest? req,
        CancellationToken ct)
    {
        var challan = await _challanRepo.GetByIdAsync(id, ct);
        if (challan is null)
            return NotFound(ApiError.NotFound("Challan not found", Request.Path));

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                challan.MarkDelivered(req?.ReceiverName, req?.ReceiverPhone);
                challan.ModifiedBy = _currentUser.GetCurrentUsername();
                await _challanRepo.UpdateAsync(challan, ct);

                // Transition SO â†’ DELIVERED
                var so = await _soRepo.GetByIdAsync(challan.SalesOrderId, ct);
                if (so != null && so.Status != "DELIVERED")
                {
                    so.MarkAsDelivered(DateTime.UtcNow);
                    so.ModifiedBy = _currentUser.GetCurrentUsername();
                    await _soRepo.UpdateAsync(so, ct);

                    // Issue invoice if still DRAFT and update customer balance atomically
                    var invoice = await _db.Invoices
                        .FirstOrDefaultAsync(i => i.SalesOrderId == so.Id && !i.Isdeleted, ct);

                    if (invoice is { Status: "DRAFT" })
                    {
                        invoice.Issue();
                        invoice.ModifiedBy = _currentUser.GetCurrentUsername();

                        // Customer balance must be updated in the same save as the invoice status
                        // to guarantee consistency â€” never issue without recording the liability.
                        var customer = await _customerRepo.GetByIdAsync(so.CustomerId, ct);
                        if (customer is null)
                        {
                            _logger.LogWarning("Customer {Id} not found while issuing invoice on challan delivery â€” balance not updated.", so.CustomerId);
                        }
                        else
                        {
                            customer.UpdateBalance(invoice.GrandTotal);
                            customer.ModifiedBy = _currentUser.GetCurrentUsername();
                        }

                        // Single SaveChanges: invoice status + customer balance together
                        _db.Invoices.Update(invoice);
                        await _db.SaveChangesAsync(ct);
                    }
                }

                await tx.CommitAsync(ct);
            }
            catch { await tx.RollbackAsync(ct); throw; }
        });

        return Ok(ApiResponse<object>.Ok(MapToResponse(challan)));
    }

    // â”€â”€ Mapping â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static object MapToResponse(Challan c) => new
    {
        id = c.Id,
        challanNumber = c.ChallanNumber,
        salesOrderId = c.SalesOrderId,
        salesOrderNumber = c.SalesOrder?.SONumber,
        invoiceId = c.InvoiceId,
        status = c.Status,
        issuedAt = c.IssuedAt,
        deliveredAt = c.DeliveredAt,
        deliveryAddress = c.DeliveryAddress,
        receiverName = c.ReceiverName,
        receiverPhone = c.ReceiverPhone,
        notes = c.Notes,
        transportCompany = c.TransportCompany,
        vehicleNumber = c.VehicleNumber,
        driverName = c.DriverName,
        driverPhone = c.DriverPhone,
        createdAt = c.CreatedDate,
        createdBy = c.CreatedBy,
        lines = c.Lines.OrderBy(l => l.LineNumber).Select(l => new
        {
            id = l.Id,
            partId = l.PartId,
            productVariantId = l.ProductVariantId,
            partName = l.PartName,
            partSku = l.PartSku,
            displayName = l.DisplayName,
            unitName = l.UnitName,
            quantity = l.Quantity,
            lineNumber = l.LineNumber
        })
    };
}

public record GenerateChallanRequest(
    string? DeliveryAddress,
    string? ReceiverName,
    string? ReceiverPhone,
    string? Notes,
    string? TransportCompany,
    string? VehicleNumber,
    string? DriverName,
    string? DriverPhone);

public record DeliverChallanRequest(
    string? ReceiverName,
    string? ReceiverPhone);
