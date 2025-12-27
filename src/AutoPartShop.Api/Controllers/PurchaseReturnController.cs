using AutoPartShop.Application.DTOs.PurchaseReturnDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PurchaseReturnController : ControllerBase
{
    private readonly IPurchaseReturnRepository _purchaseReturnRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly IStockLotMovementRepository _stockLotMovementRepository;
    private readonly ILogger<PurchaseReturnController> _logger;

    public PurchaseReturnController(
        IPurchaseReturnRepository purchaseReturnRepository,
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLotRepository stockLotRepository,
        IStockLotMovementRepository stockLotMovementRepository,
        ILogger<PurchaseReturnController> logger)
    {
        _purchaseReturnRepository = purchaseReturnRepository;
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLotRepository = stockLotRepository;
        _stockLotMovementRepository = stockLotMovementRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var returns = await _purchaseReturnRepository.GetAllAsync(cancellationToken);
            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all purchase returns");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase returns");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (returns, totalCount) = string.IsNullOrWhiteSpace(searchTerm)
                ? await _purchaseReturnRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken)
                : await _purchaseReturnRepository.SearchPagedAsync(searchTerm, pageNumber, pageSize, cancellationToken);

            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase returns list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase returns");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase return by ID: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the purchase return");
        }
    }

    [HttpGet("number/{returnNumber}")]
    public async Task<IActionResult> GetByNumber(string returnNumber, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByNumberAsync(returnNumber, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase return by number: {ReturnNumber}", returnNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the purchase return");
        }
    }

    [HttpGet("purchase-order/{purchaseOrderId:guid}")]
    public async Task<IActionResult> GetByPurchaseOrder(Guid purchaseOrderId, CancellationToken cancellationToken)
    {
        try
        {
            var returns = await _purchaseReturnRepository.GetByPurchaseOrderAsync(purchaseOrderId, cancellationToken);
            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase returns by PO: {PurchaseOrderId}", purchaseOrderId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase returns");
        }
    }

    [HttpGet("supplier/{supplierId:guid}")]
    public async Task<IActionResult> GetBySupplier(Guid supplierId, CancellationToken cancellationToken)
    {
        try
        {
            var returns = await _purchaseReturnRepository.GetBySupplierAsync(supplierId, cancellationToken);
            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase returns by supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase returns");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var returns = await _purchaseReturnRepository.GetByStatusAsync(status, cancellationToken);
            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase returns by status: {Status}", status);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving purchase returns");
        }
    }

    [HttpGet("pending-approvals")]
    public async Task<IActionResult> GetPendingApprovals(CancellationToken cancellationToken)
    {
        try
        {
            var returns = await _purchaseReturnRepository.GetPendingApprovalsAsync(cancellationToken);
            var response = returns.Select(MapToPurchaseReturnResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending purchase return approvals");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving pending approvals");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PurchaseOrderId == Guid.Empty || request.SupplierId == Guid.Empty)
                return BadRequest(new { message = "PurchaseOrderId and SupplierId are required" });

            var purchaseReturn = PurchaseReturn.Create(
                $"PR-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.PurchaseOrderId,
                request.SupplierId,
                request.Reason,
                request.ReturnDate,
                request.Notes
            );

            // Add lines
            foreach (var lineRequest in request.Lines)
            {
                var line = PurchaseReturnLine.Create(
                    purchaseReturn.Id,
                    lineRequest.PurchaseOrderLineId,
                    lineRequest.PartId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    lineRequest.Condition
                );
                if (lineRequest.RejectedQuantity > 0)
                {
                    line.RejectQuantity(lineRequest.RejectedQuantity);
                }
                line.AddNotes(lineRequest.Notes);
                purchaseReturn.LineItems.Add(line);
            }

            // Calculate total refund amount from all line items
            purchaseReturn.CalculateRefund();

            purchaseReturn.CreatedBy = "System";
            purchaseReturn.ModifiedBy = "System";

            await _purchaseReturnRepository.AddAsync(purchaseReturn, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = purchaseReturn.Id }, MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating purchase return");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the purchase return");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            if (purchaseReturn.Status != "PENDING")
                return BadRequest(new { message = "Only pending purchase returns can be edited" });

            // Clear existing line items
            purchaseReturn.LineItems.Clear();

            // Add updated lines
            foreach (var lineRequest in request.Lines)
            {
                var line = PurchaseReturnLine.Create(
                    purchaseReturn.Id,
                    lineRequest.PurchaseOrderLineId,
                    lineRequest.PartId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    lineRequest.Condition
                );
                if (lineRequest.RejectedQuantity > 0)
                {
                    line.RejectQuantity(lineRequest.RejectedQuantity);
                }
                line.AddNotes(lineRequest.Notes);
                purchaseReturn.LineItems.Add(line);
            }

            // Calculate total refund amount from all line items
            purchaseReturn.CalculateRefund();

            purchaseReturn.ModifiedBy = "System";
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the purchase return");
        }
    }

    [HttpPatch("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, [FromQuery] string approvedBy, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.Approve(approvedBy);
            purchaseReturn.ModifiedBy = "System";
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while approving the purchase return");
        }
    }

    [HttpPatch("{id:guid}/mark-returned")]
    public async Task<IActionResult> MarkAsReturned(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.MarkAsReturned();
            purchaseReturn.ModifiedBy = "System";

            // Process stock movements for each line item
            foreach (var line in purchaseReturn.LineItems)
            {
                // Get the accepted quantity (returned quantity - rejected)
                int acceptedQuantity = line.Quantity - line.RejectedQuantity;
                if (acceptedQuantity <= 0) continue;

                // Get stock level for this part (we need to determine warehouse from PO/GRN)
                // For now, get the first stock level for this part
                var stockLevels = await _stockLevelRepository.GetByPartAsync(line.PartId, cancellationToken);
                var stockLevel = stockLevels.FirstOrDefault();

                if (stockLevel != null)
                {
                    // Reduce stock - items are being returned to supplier
                    stockLevel.RemoveStock(acceptedQuantity, "Purchase Return");
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    // Create stock movement record
                    var stockMovement = StockMovement.Create(
                        stockLevel.Id,
                        "OUT",
                        acceptedQuantity,
                        $"Purchase Return {purchaseReturn.ReturnNumber}",
                        purchaseReturn.ReturnNumber
                    );
                    stockMovement.Approve("System");
                    stockMovement.CreatedBy = "System";
                    stockMovement.ModifiedBy = "System";
                    await _stockMovementRepository.AddAsync(stockMovement, cancellationToken);

                    // Create lot movements for FIFO/LIFO tracking
                    var lots = await _stockLotRepository.GetByPartAndWarehouseAsync(
                        line.PartId,
                        stockLevel.WarehouseId,
                        cancellationToken);

                    // Use FIFO - return from oldest lots first
                    var availableLots = lots
                        .Where(l => l.QuantityAvailable > 0)
                        .OrderBy(l => l.ReceivingDate)
                        .ToList();

                    int remainingQty = acceptedQuantity;
                    foreach (var lot in availableLots)
                    {
                        if (remainingQty <= 0) break;

                        int qtyToReturn = Math.Min(remainingQty, lot.QuantityAvailable);

                        // Reduce lot quantity
                        lot.RemoveStock(qtyToReturn, "Purchase Return");
                        await _stockLotRepository.UpdateAsync(lot, cancellationToken);

                        // Create lot movement
                        var lotMovement = StockLotMovement.Create(
                            lot.Id,
                            qtyToReturn,
                            "RETURN",
                            purchaseReturn.Id,
                            "PurchaseReturn",
                            DateTime.UtcNow,
                            lot.CostPrice,
                            $"Purchase Return {purchaseReturn.ReturnNumber}",
                            $"Returned to supplier"
                        );
                        lotMovement.CreatedBy = "System";
                        lotMovement.ModifiedBy = "System";
                        await _stockLotMovementRepository.AddAsync(lotMovement, cancellationToken);

                        remainingQty -= qtyToReturn;
                    }
                }
            }

            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking purchase return as returned: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while marking the purchase return");
        }
    }

    [HttpPatch("{id:guid}/mark-received")]
    public async Task<IActionResult> MarkAsReceived(Guid id, [FromQuery] string receivedBy, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.MarkAsReceived(receivedBy);
            purchaseReturn.ModifiedBy = "System";
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking purchase return as received: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while marking the purchase return");
        }
    }

    [HttpPatch("{id:guid}/issue-credit-note")]
    public async Task<IActionResult> IssueCreditNote(Guid id, [FromQuery] decimal creditAmount, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.IssueCreditNote(creditAmount);
            purchaseReturn.ModifiedBy = "System";
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
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
            _logger.LogError(ex, "Error issuing credit note: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while issuing credit note");
        }
    }

    [HttpPatch("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.Reject(reason);
            purchaseReturn.ModifiedBy = "System";
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while rejecting the purchase return");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _purchaseReturnRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Purchase return not found" });

            await _purchaseReturnRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the purchase return");
        }
    }

    private PurchaseReturnResponse MapToPurchaseReturnResponse(PurchaseReturn purchaseReturn)
    {
        return new PurchaseReturnResponse
        {
            Id = purchaseReturn.Id,
            ReturnNumber = purchaseReturn.ReturnNumber,
            PurchaseOrderId = purchaseReturn.PurchaseOrderId,
            PurchaseOrderNumber = purchaseReturn.PurchaseOrder?.PONumber,
            SupplierId = purchaseReturn.SupplierId,
            SupplierName = purchaseReturn.Supplier?.Name,
            SupplierCode = purchaseReturn.Supplier?.Code,
            ReturnDate = purchaseReturn.ReturnDate,
            Reason = purchaseReturn.Reason,
            Status = purchaseReturn.Status,
            RefundAmount = purchaseReturn.RefundAmount,
            CreditNoteAmount = purchaseReturn.CreditNoteAmount,
            Notes = purchaseReturn.Notes,
            ApprovedBy = purchaseReturn.ApprovedBy,
            ApprovedDate = purchaseReturn.ApprovedDate,
            ReceivedDate = purchaseReturn.ReceivedDate,
            ReceivedBy = purchaseReturn.ReceivedBy,
            Lines = purchaseReturn.LineItems.Select(l => new PurchaseReturnLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                Quantity = l.Quantity,
                RejectedQuantity = l.RejectedQuantity,
                UnitPrice = l.UnitPrice,
                RefundAmount = l.RefundAmount,
                Condition = l.Condition,
                Notes = l.Notes
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
