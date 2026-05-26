using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Application.DTOs.PurchaseReturnDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class PurchaseReturnController : ControllerBase
{
    private readonly IPurchaseReturnRepository _purchaseReturnRepository;
    private readonly ICreditNoteRepository _creditNoteRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly IStockMovementRepository _stockMovementRepository;
    private readonly IStockLotRepository _stockLotRepository;
    private readonly IStockLotMovementRepository _stockLotMovementRepository;
    private readonly ISupplierPaymentRepository _supplierPaymentRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<PurchaseReturnController> _logger;

    public PurchaseReturnController(
        IPurchaseReturnRepository purchaseReturnRepository,
        ICreditNoteRepository creditNoteRepository,
        IStockLevelRepository stockLevelRepository,
        IStockMovementRepository stockMovementRepository,
        IStockLotRepository stockLotRepository,
        IStockLotMovementRepository stockLotMovementRepository,
        ISupplierPaymentRepository supplierPaymentRepository,
        ICurrentUserService currentUserService,
        ICodeGenerateService codeGenerateService,
        AutoPartDbContext dbContext,
        ILogger<PurchaseReturnController> logger)
    {
        _purchaseReturnRepository = purchaseReturnRepository;
        _creditNoteRepository = creditNoteRepository;
        _stockLevelRepository = stockLevelRepository;
        _stockMovementRepository = stockMovementRepository;
        _stockLotRepository = stockLotRepository;
        _stockLotMovementRepository = stockLotMovementRepository;
        _supplierPaymentRepository = supplierPaymentRepository;
        _currentUserService = currentUserService;
        _codeGenerateService = codeGenerateService;
        _dbContext = dbContext;
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

            var returnNumber = await _codeGenerateService.GenerateAsync("PR", cancellationToken);
            var purchaseReturn = PurchaseReturn.Create(
                returnNumber,
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
                    lineRequest.Condition,
                    lineRequest.StockLotId  // Optional: specific lot to return from
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

            purchaseReturn.CreatedBy = _currentUserService.GetCurrentUsername();
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();

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
                    lineRequest.Condition,
                    lineRequest.StockLotId  // Optional: specific lot to return from
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

            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
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
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.Approve(_currentUserService.GetCurrentUsername());
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
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

            // Validate all lines have sufficient stock BEFORE modifying anything
            foreach (var line in purchaseReturn.LineItems)
            {
                int acceptedQuantity = line.Quantity - line.RejectedQuantity;
                if (acceptedQuantity <= 0) continue;

                var stockLevels = await _stockLevelRepository.GetByPartAsync(line.PartId, cancellationToken);
                var stockLevel = stockLevels.FirstOrDefault();
                if (stockLevel == null)
                    return BadRequest(new { message = $"No stock level found for part {line.PartId}. Cannot process return." });

                if (stockLevel.QuantityAvailable < acceptedQuantity)
                    return BadRequest(new { message = $"Insufficient available stock for part {line.Part?.Name ?? line.PartId.ToString()}. Available: {stockLevel.QuantityAvailable}, Required: {acceptedQuantity}" });
            }

            purchaseReturn.MarkAsReturned();
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Process stock movements for each line item inside a transaction
            await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
            foreach (var line in purchaseReturn.LineItems)
            {
                int acceptedQuantity = line.Quantity - line.RejectedQuantity;
                if (acceptedQuantity <= 0) continue;

                var stockLevels = await _stockLevelRepository.GetByPartAsync(line.PartId, cancellationToken);
                var stockLevel = stockLevels.FirstOrDefault();

                if (stockLevel != null)
                {
                    // Check if a specific lot was selected for this line
                    if (line.StockLotId.HasValue)
                    {
                        // Use the specific lot selected by user
                        var selectedLot = await _stockLotRepository.GetByIdAsync(line.StockLotId.Value, cancellationToken);
                        if (selectedLot == null)
                        {
                            _logger.LogWarning("Selected lot {LotId} not found for line {LineId}", line.StockLotId, line.Id);
                            continue;
                        }

                        if (selectedLot.QuantityAvailable < acceptedQuantity)
                        {
                            _logger.LogWarning(
                                "Insufficient stock in selected lot {LotId}. Required: {Required}, Available: {Available}",
                                line.StockLotId, acceptedQuantity, selectedLot.QuantityAvailable);
                            return BadRequest(new { message = $"Insufficient stock in selected lot. Available: {selectedLot.QuantityAvailable}, Required: {acceptedQuantity}" });
                        }

                        // Reduce stock - items are being returned to supplier
                        stockLevel.RemoveStock(acceptedQuantity, acceptedQuantity, "Purchase Return");
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
                        stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                        stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _stockMovementRepository.AddAsync(stockMovement, cancellationToken);

                        // Reduce the selected lot quantity
                        selectedLot.RemoveStock(acceptedQuantity, acceptedQuantity, "Purchase Return");
                        await _stockLotRepository.UpdateAsync(selectedLot, cancellationToken);

                        // Create lot movement for the specific lot
                        var lotMovement = StockLotMovement.Create(
                            selectedLot.Id,
                            acceptedQuantity,
                            "RETURN",
                            purchaseReturn.Id,
                            "PurchaseReturn",
                            DateTime.UtcNow,
                            selectedLot.CostPrice,
                            $"Purchase Return {purchaseReturn.ReturnNumber}",
                            $"Returned to supplier (Selected Lot: {selectedLot.LotNumber})"
                        );
                        lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                        lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _stockLotMovementRepository.AddAsync(lotMovement, cancellationToken);
                    }
                    else
                    {
                        // No specific lot selected - use FIFO from supplier lots
                        var allLots = await _stockLotRepository.GetByPartAndWarehouseAsync(
                            line.PartId,
                            stockLevel.WarehouseId,
                            cancellationToken);

                        // IMPORTANT: Filter to get lots from THIS SUPPLIER only (using FIFO within supplier lots)
                        // This ensures we return items from the same supplier we purchased from
                        var supplierLots = allLots
                            .Where(l => l.SupplierId == purchaseReturn.SupplierId && l.QuantityAvailable > 0)
                            .OrderBy(l => l.ReceivingDate)  // FIFO - oldest lots first
                            .ToList();

                        // Check if we have enough stock from this supplier
                        int totalAvailableFromSupplier = supplierLots.Sum(l => l.QuantityAvailable);
                        if (totalAvailableFromSupplier < acceptedQuantity)
                        {
                            _logger.LogWarning(
                                "Insufficient stock from supplier {SupplierId} for part {PartId}. Required: {Required}, Available: {Available}",
                                purchaseReturn.SupplierId, line.PartId, acceptedQuantity, totalAvailableFromSupplier);

                            // Fall back to any available lots if supplier-specific lots are insufficient
                            // This handles edge cases where stock was already sold/moved
                            var otherLots = allLots
                                .Where(l => l.SupplierId != purchaseReturn.SupplierId && l.QuantityAvailable > 0)
                                .OrderBy(l => l.ReceivingDate)
                                .ToList();
                            supplierLots.AddRange(otherLots);
                        }

                        // Reduce stock - items are being returned to supplier
                        stockLevel.RemoveStock(acceptedQuantity, acceptedQuantity, "Purchase Return");
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
                        stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                        stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _stockMovementRepository.AddAsync(stockMovement, cancellationToken);

                        // Process lot movements - prioritizing supplier-specific lots
                        int remainingQty = acceptedQuantity;
                        foreach (var lot in supplierLots)
                        {
                            if (remainingQty <= 0) break;

                            int qtyToReturn = Math.Min(remainingQty, lot.QuantityAvailable);

                            // Reduce lot quantity
                            lot.RemoveStock(qtyToReturn, qtyToReturn, "Purchase Return");
                            await _stockLotRepository.UpdateAsync(lot, cancellationToken);

                            // Create lot movement with supplier reference
                            var lotMovement = StockLotMovement.Create(
                                lot.Id,
                                qtyToReturn,
                                "RETURN",
                                purchaseReturn.Id,
                                "PurchaseReturn",
                                DateTime.UtcNow,
                                lot.CostPrice,
                                $"Purchase Return {purchaseReturn.ReturnNumber}",
                                $"Returned to supplier (Lot from Supplier: {lot.SupplierId})"
                            );
                            lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                            lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _stockLotMovementRepository.AddAsync(lotMovement, cancellationToken);

                            remainingQty -= qtyToReturn;
                        }

                        if (remainingQty > 0)
                        {
                            _logger.LogWarning(
                                "Could not return full quantity for part {PartId}. Remaining: {Remaining}",
                                line.PartId, remainingQty);
                        }
                    }
                }
            }

            // NOTE: Supplier balance is NOT updated here.
            // Balance is now calculated from transactions via SupplierLedgerService.
            // Use the /settle endpoint to record the financial settlement of this return.

            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            } // end try
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

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
    public async Task<IActionResult> MarkAsReceived(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            purchaseReturn.MarkAsReceived(_currentUserService.GetCurrentUsername());
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
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

            // Issue credit note on the return
            purchaseReturn.IssueCreditNote(creditAmount);
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            // Create CreditNote entity
            var creditNoteNumber = $"CN-{DateTime.UtcNow:yyyyMMddHHmmss}";
            // Derive currency from the linked purchase order if available, otherwise fall back to "NPR"
            var linkedPO = await _dbContext.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseReturn.PurchaseOrderId, cancellationToken);
            var currency = linkedPO?.Currency ?? "NPR";

            var creditNote = CreditNote.Create(
                creditNoteNumber: creditNoteNumber,
                supplierId: purchaseReturn.SupplierId,
                purchaseReturnId: purchaseReturn.Id,
                amount: creditAmount,
                currency: currency,
                issueDate: DateTime.UtcNow,
                notes: $"Credit note for return {purchaseReturn.ReturnNumber}",
                issuedBy: _currentUserService.GetCurrentUsername()
            );
            await _creditNoteRepository.AddAsync(creditNote, cancellationToken);

            // Link the credit note to the purchase return
            purchaseReturn.SetCreditNote(creditNote.Id);
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            // Create SupplierPayment record for audit trail (ADVANCE type)
            // Find a default payment provider
            var defaultProvider = await _dbContext.PaymentProviders.FirstOrDefaultAsync(cancellationToken);
            if (defaultProvider != null)
            {
                var supplierPayment = SupplierPayment.Create(
                    supplierId: purchaseReturn.SupplierId,
                    paymentProviderId: defaultProvider.Id,
                    amount: creditAmount,
                    paymentMethod: "CREDIT_NOTE",
                    transactionNumber: creditNoteNumber,
                    referenceNumber: purchaseReturn.ReturnNumber,
                    paymentDate: DateTime.UtcNow
                );
                supplierPayment.MarkAsAdvance();  // This is a credit that can be used later
                supplierPayment.MarkAsProcessed(_currentUserService.GetCurrentUsername());
                await _supplierPaymentRepository.AddAsync(supplierPayment, cancellationToken);
            }

            _logger.LogInformation(
                "Credit note {CreditNoteNumber} issued for return {ReturnId}, amount: {Amount}, supplier: {SupplierId}",
                creditNoteNumber, id, creditAmount, purchaseReturn.SupplierId);

            var response = MapToPurchaseReturnResponse(purchaseReturn);
            response.CreditNoteId = creditNote.Id;
            response.CreditNoteNumber = creditNote.CreditNoteNumber;

            return Ok(response);
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
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            return Ok(MapToPurchaseReturnResponse(purchaseReturn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while rejecting the purchase return");
        }
    }

    /// <summary>
    /// Settle a purchase return - record the financial settlement
    /// </summary>
    /// <param name="id">Purchase return ID</param>
    /// <param name="request">Settlement details</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPatch("{id:guid}/settle")]
    public async Task<IActionResult> SettlePurchaseReturn(
        Guid id,
        [FromBody] SettlePurchaseReturnRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            if (purchaseReturn.SettlementStatus == "SETTLED")
                return BadRequest(new { message = "Purchase return has already been settled" });

            // Settle the return
            purchaseReturn.SettleReturn(request.Amount, request.SettlementMethod, request.Notes);
            purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

            _logger.LogInformation(
                "Purchase return {ReturnNumber} settled. Amount: {Amount}, Method: {Method}",
                purchaseReturn.ReturnNumber, request.Amount, request.SettlementMethod);

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
            _logger.LogError(ex, "Error settling purchase return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while settling the purchase return");
        }
    }

    /// <summary>
    /// Get available stock lots for a part that can be selected for return.
    /// Optionally filter by supplier to show lots from the same supplier first.
    /// </summary>
    [HttpGet("available-lots/{partId:guid}")]
    public async Task<IActionResult> GetAvailableLotsForReturn(
        Guid partId,
        [FromQuery] Guid? supplierId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get stock levels for the part to find the warehouse
            var stockLevels = await _stockLevelRepository.GetByPartAsync(partId, cancellationToken);
            var stockLevel = stockLevels.FirstOrDefault();

            if (stockLevel == null)
            {
                return Ok(new List<AvailableLotForReturnDto>());
            }

            // Get all lots for this part and warehouse
            var allLots = await _stockLotRepository.GetByPartAndWarehouseAsync(
                partId,
                stockLevel.WarehouseId,
                cancellationToken);

            // Filter to lots with available quantity
            var availableLots = allLots
                .Where(l => l.QuantityAvailable > 0)
                .Select(l => new AvailableLotForReturnDto
                {
                    LotId = l.Id,
                    LotNumber = l.LotNumber,
                    PartId = l.PartId,
                    PartName = l.Part?.Name ?? string.Empty,
                    PartSku = l.Part?.SKU ?? string.Empty,
                    SupplierId = l.SupplierId,
                    SupplierName = l.Supplier?.Name ?? string.Empty,
                    QuantityAvailable = l.QuantityAvailable,
                    CostPrice = l.CostPrice,
                    ReceivingDate = l.ReceivingDate,
                    ExpiryDate = l.ExpiryDate,
                    IsFromSameSupplier = supplierId.HasValue && l.SupplierId == supplierId.Value
                })
                .OrderByDescending(l => l.IsFromSameSupplier)  // Same supplier lots first
                .ThenBy(l => l.ReceivingDate)  // Then FIFO
                .ToList();

            return Ok(availableLots);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available lots for part: {PartId}", partId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving available lots");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null)
                return NotFound(new { message = "Purchase return not found" });

            if (purchaseReturn.Status != "PENDING")
                return BadRequest(new { message = $"Only PENDING returns can be deleted. Current status: {purchaseReturn.Status}" });

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
            SettlementStatus = purchaseReturn.SettlementStatus,
            SettledAmount = purchaseReturn.SettledAmount,
            SettledDate = purchaseReturn.SettledDate,
            SettlementMethod = purchaseReturn.SettlementMethod,
            SettlementNotes = purchaseReturn.SettlementNotes,
            IsSettled = purchaseReturn.IsSettled,
            Lines = purchaseReturn.LineItems.Select(l => new PurchaseReturnLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                PartName = l.Part?.Name ?? string.Empty,
                PartSku = l.Part?.SKU ?? string.Empty,
                StockLotId = l.StockLotId,
                LotNumber = l.StockLot?.LotNumber,
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
