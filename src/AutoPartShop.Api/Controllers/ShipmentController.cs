using AutoPartShop.Api.Services;
using AutoPartShop.Application.Shipments.Dtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[HasPermission(Permissions.SalesView)]
[Produces("application/json")]
public class ShipmentController(
    AutoPartDbContext _dbContext,
    ICodeGenerateService _codeGenerateService,
    ICurrentUserService _currentUserService,
    ILogger<ShipmentController> _logger) : ControllerBase
{
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var shipment = await LoadShipmentAsync(id, cancellationToken);
        if (shipment is null) return NotFound(new { message = "Shipment not found" });
        return Ok(MapToResponse(shipment));
    }

    [HttpGet("sales-order/{salesOrderId:guid}")]
    public async Task<IActionResult> GetBySalesOrder(Guid salesOrderId, CancellationToken cancellationToken)
    {
        var shipments = await _dbContext.Shipments
            .Include(s => s.SalesOrder)
            .Include(s => s.Lines).ThenInclude(l => l.Part)
            .Include(s => s.Lines).ThenInclude(l => l.ProductVariant)
            .Where(s => s.SalesOrderId == salesOrderId && !s.Isdeleted)
            .OrderByDescending(s => s.CreatedDate)
            .ToListAsync(cancellationToken);

        return Ok(shipments.Select(MapToResponse));
    }

    [HttpPost]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Create(CreateShipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.SalesOrderId == Guid.Empty)
                return BadRequest(new { message = "SalesOrderId is required" });

            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest(new { message = "Shipment must have at least one line" });

            var salesOrder = await _dbContext.SalesOrders
                .Include(so => so.LineItems)
                .FirstOrDefaultAsync(so => so.Id == request.SalesOrderId && !so.Isdeleted, cancellationToken);

            if (salesOrder is null)
                return NotFound(new { message = "Sales order not found" });

            if (salesOrder.Status is not ("CONFIRMED" or "PAID" or "PACKED" or "PARTIALLY_SHIPPED"))
                return BadRequest(new { message = $"Cannot create shipment for a {salesOrder.Status} order. Order must be CONFIRMED, PAID, PACKED or PARTIALLY_SHIPPED." });

            // Validate each line references a real SalesOrderLine on this order
            foreach (var line in request.Lines)
            {
                var orderLine = salesOrder.LineItems.FirstOrDefault(l => l.Id == line.SalesOrderLineId);
                if (orderLine is null)
                    return BadRequest(new { message = $"SalesOrderLine {line.SalesOrderLineId} does not belong to this order" });

                if (line.Quantity <= 0)
                    return BadRequest(new { message = $"Quantity must be greater than 0 for line {line.SalesOrderLineId}" });

                if (line.Quantity > orderLine.PendingQuantity)
                    return BadRequest(new { message = $"Quantity {line.Quantity} exceeds pending quantity {orderLine.PendingQuantity} for part {orderLine.PartId}" });
            }

            var shipmentNumber = await _codeGenerateService.GenerateAsync("SHP", cancellationToken);
            var actor = _currentUserService.GetCurrentUsername();

            var shipment = Shipment.Create(
                shipmentNumber,
                request.SalesOrderId,
                request.CourierName,
                request.TrackingNumber,
                request.EstimatedDeliveryDate,
                request.Notes);

            shipment.CreatedBy = actor;
            shipment.ModifiedBy = actor;

            foreach (var lineReq in request.Lines)
            {
                var line = ShipmentLine.Create(
                    shipment.Id,
                    lineReq.SalesOrderLineId,
                    lineReq.PartId,
                    lineReq.Quantity,
                    lineReq.QuantityInBaseUnit,
                    lineReq.ProductVariantId);

                line.CreatedBy = actor;
                line.ModifiedBy = actor;
                shipment.Lines.Add(line);
            }

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _dbContext.Shipments.AddAsync(shipment, cancellationToken);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            var created = await LoadShipmentAsync(shipment.Id, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = shipment.Id }, MapToResponse(created!));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating shipment for order {SalesOrderId}", request.SalesOrderId);
            return StatusCode(500, "An error occurred while creating the shipment");
        }
    }

    [HttpPatch("{id:guid}/dispatch")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Dispatch(Guid id, [FromBody] DispatchShipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await LoadShipmentAsync(id, cancellationToken);
            if (shipment is null) return NotFound(new { message = "Shipment not found" });

            var actor = _currentUserService.GetCurrentUsername();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Reload inside the lambda so a retry after rollback transitions exactly once.
                    shipment = await LoadShipmentAsync(id, cancellationToken)
                        ?? throw new InvalidOperationException("Shipment not found");

                    shipment.Dispatch(request.TrackingNumber, request.CourierName);
                    shipment.ModifiedBy = actor;

                    // Update shipped quantities on SalesOrderLines
                    var salesOrder = await _dbContext.SalesOrders
                        .Include(so => so.LineItems)
                        .FirstOrDefaultAsync(so => so.Id == shipment.SalesOrderId && !so.Isdeleted, cancellationToken);

                    if (salesOrder != null)
                    {
                        foreach (var shipLine in shipment.Lines)
                        {
                            var orderLine = salesOrder.LineItems.FirstOrDefault(l => l.Id == shipLine.SalesOrderLineId);
                            orderLine?.UpdateShippedQuantity(
                                orderLine.ShippedQuantity + shipLine.Quantity,
                                orderLine.ShippedQuantityInBaseUnit + shipLine.QuantityInBaseUnit);
                        }

                        // Move SalesOrder status: PARTIALLY_SHIPPED if not all lines done, SHIPPED if all done
                        bool allShipped = salesOrder.LineItems.All(l => l.IsFullyShipped);
                        if (allShipped)
                            salesOrder.MarkAsShipped();
                        else
                            salesOrder.MarkAsPartiallyShipped();

                        salesOrder.ModifiedBy = actor;
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(MapToResponse(shipment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching shipment {ShipmentId}", id);
            return StatusCode(500, "An error occurred while dispatching the shipment");
        }
    }

    [HttpPatch("{id:guid}/in-transit")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> MarkInTransit(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await LoadShipmentAsync(id, cancellationToken);
            if (shipment is null) return NotFound(new { message = "Shipment not found" });

            shipment.MarkInTransit();
            shipment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(MapToResponse(shipment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking shipment in-transit {ShipmentId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/deliver")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> MarkDelivered(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await LoadShipmentAsync(id, cancellationToken);
            if (shipment is null) return NotFound(new { message = "Shipment not found" });

            var actor = _currentUserService.GetCurrentUsername();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Reload inside the lambda so a retry after rollback transitions exactly once.
                    shipment = await LoadShipmentAsync(id, cancellationToken)
                        ?? throw new InvalidOperationException("Shipment not found");

                    shipment.MarkDelivered();
                    shipment.ModifiedBy = actor;

                    // If all shipments for this order are delivered, mark order as DELIVERED
                    var allShipments = await _dbContext.Shipments
                        .Where(s => s.SalesOrderId == shipment.SalesOrderId && !s.Isdeleted)
                        .ToListAsync(cancellationToken);

                    bool allDelivered = allShipments.All(s => s.Status == "DELIVERED");
                    if (allDelivered)
                    {
                        var salesOrder = await _dbContext.SalesOrders
                            .FirstOrDefaultAsync(so => so.Id == shipment.SalesOrderId && !so.Isdeleted, cancellationToken);

                        if (salesOrder != null)
                        {
                            salesOrder.MarkAsDelivered();
                            salesOrder.ModifiedBy = actor;
                        }
                    }

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(MapToResponse(shipment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking shipment delivered {ShipmentId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/fail")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> MarkFailed(Guid id, [FromBody] FailShipmentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest(new { message = "Failure reason is required" });

            var shipment = await LoadShipmentAsync(id, cancellationToken);
            if (shipment is null) return NotFound(new { message = "Shipment not found" });

            shipment.MarkFailed(request.Reason);
            shipment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(MapToResponse(shipment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking shipment failed {ShipmentId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/tracking")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> UpdateTracking(Guid id, [FromBody] UpdateTrackingRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var shipment = await LoadShipmentAsync(id, cancellationToken);
            if (shipment is null) return NotFound(new { message = "Shipment not found" });

            shipment.UpdateTracking(request.CourierName, request.TrackingNumber, request.EstimatedDeliveryDate);
            shipment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Ok(MapToResponse(shipment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tracking for shipment {ShipmentId}", id);
            return StatusCode(500, "An error occurred");
        }
    }

    // â”€â”€ Helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private Task<Shipment?> LoadShipmentAsync(Guid id, CancellationToken ct) =>
        _dbContext.Shipments
            .Include(s => s.SalesOrder)
            .Include(s => s.Lines).ThenInclude(l => l.Part)
            .Include(s => s.Lines).ThenInclude(l => l.ProductVariant)
            .FirstOrDefaultAsync(s => s.Id == id && !s.Isdeleted, ct);

    private static ShipmentResponse MapToResponse(Shipment s) => new()
    {
        Id = s.Id,
        ShipmentNumber = s.ShipmentNumber,
        SalesOrderId = s.SalesOrderId,
        SalesOrderNumber = s.SalesOrder?.SONumber,
        CourierName = s.CourierName,
        TrackingNumber = s.TrackingNumber,
        Status = s.Status,
        EstimatedDeliveryDate = s.EstimatedDeliveryDate,
        DispatchedDate = s.DispatchedDate,
        DeliveredDate = s.DeliveredDate,
        FailedDate = s.FailedDate,
        FailureReason = s.FailureReason,
        Notes = s.Notes,
        CreatedAt = s.CreatedDate,
        Lines = s.Lines.Select(l => new ShipmentLineResponse
        {
            Id = l.Id,
            SalesOrderLineId = l.SalesOrderLineId,
            PartId = l.PartId,
            PartName = l.Part?.Name ?? string.Empty,
            ProductVariantId = l.ProductVariantId,
            VariantName = l.ProductVariant?.Name,
            VariantSku = l.ProductVariant?.SKU,
            Quantity = l.Quantity,
            QuantityInBaseUnit = l.QuantityInBaseUnit
        }).ToList()
    };
}
