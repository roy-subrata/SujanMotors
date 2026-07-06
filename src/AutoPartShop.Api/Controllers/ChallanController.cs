using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/challans")]
[Route("api/v1/challans")]
[ApiController]
[Authorize]
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
    // ── Generate challan for an order ────────────────────────────────────────

    /// <summary>
    /// Generates a Challan (delivery note) for a Confirmed or ReadyForDelivery order.
    /// Marks the order as ReadyForDelivery if it was still in Confirmed state.
    /// </summary>
    [HttpPost("sales-order/{salesOrderId:guid}")]
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
                        $"Challan can no longer be generated — order status is '{freshSo?.Status ?? "deleted"}'.");

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

    // ── Get challans for an order ─────────────────────────────────────────────

    [HttpGet("sales-order/{salesOrderId:guid}")]
    public async Task<IActionResult> GetBySalesOrder(Guid salesOrderId, CancellationToken ct)
    {
        var challans = await _challanRepo.GetBySalesOrderAsync(salesOrderId, ct);
        return Ok(ApiResponse<object>.Ok(challans.Select(MapToResponse)));
    }

    // ── Get single challan ────────────────────────────────────────────────────

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var challan = await _challanRepo.GetByIdAsync(id, ct);
        if (challan is null)
            return NotFound(ApiError.NotFound("Challan not found", Request.Path));

        return Ok(ApiResponse<object>.Ok(MapToResponse(challan)));
    }

    // ── Pending challans (delivery queue) ─────────────────────────────────────

    /// <summary>
    /// Returns all DRAFT and ISSUED challans — the delivery staff's dispatch queue.
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var draft = await _challanRepo.GetByStatusAsync("DRAFT", ct);
        var issued = await _challanRepo.GetByStatusAsync("ISSUED", ct);
        var all = draft.Concat(issued).OrderBy(c => c.CreatedDate);
        return Ok(ApiResponse<object>.Ok(all.Select(MapToResponse)));
    }

    // ── Issue challan ─────────────────────────────────────────────────────────

    /// <summary>Issues the challan — it can now travel with the goods.</summary>
    [HttpPatch("{id:guid}/issue")]
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

    // ── Deliver (with challan) ────────────────────────────────────────────────

    /// <summary>
    /// Marks challan as delivered and transitions the linked Sales Order to DELIVERED.
    /// Also issues the invoice if it is still in DRAFT status.
    /// </summary>
    [HttpPatch("{id:guid}/deliver")]
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

                // Transition SO → DELIVERED
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
                        // to guarantee consistency — never issue without recording the liability.
                        var customer = await _customerRepo.GetByIdAsync(so.CustomerId, ct);
                        if (customer is null)
                        {
                            _logger.LogWarning("Customer {Id} not found while issuing invoice on challan delivery — balance not updated.", so.CustomerId);
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

    // ── Mapping ───────────────────────────────────────────────────────────────

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
