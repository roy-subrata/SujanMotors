using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Application.DTOs.PurchaseReturnDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Common;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[Route("api/v1/[controller]")]
[ApiController]
[Produces("application/json")]
[Authorize] // reads open to any authenticated user (cashiers view returns); mutations gated per-action
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create(CreatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.PurchaseOrderId == Guid.Empty || request.SupplierId == Guid.Empty)
                return BadRequest(new { message = "PurchaseOrderId and SupplierId are required" });

            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest(new { message = "A purchase return must have at least one line item" });

            // Option A: every line must belong to the selected purchase order (and supplier).
            var createValidationError = await ValidateReturnLinesAgainstPoAsync(
                request.PurchaseOrderId, request.SupplierId, request.Lines, cancellationToken);
            if (createValidationError != null)
                return BadRequest(new { message = createValidationError });

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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(Guid id, UpdatePurchaseReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            if (purchaseReturn.Status != "PENDING")
                return BadRequest(new { message = "Only pending purchase returns can be edited" });

            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest(new { message = "A purchase return must have at least one line item" });

            // Option A: every line must belong to this return's purchase order (and supplier).
            var updateValidationError = await ValidateReturnLinesAgainstPoAsync(
                purchaseReturn.PurchaseOrderId, purchaseReturn.SupplierId, request.Lines, cancellationToken);
            if (updateValidationError != null)
                return BadRequest(new { message = updateValidationError });

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
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> MarkAsReturned(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            // Validate all lines have sufficient stock BEFORE modifying anything.
            // The inventory bucket (Available/Damaged/Quarantine) is derived from the selected lot's status;
            // lines without a lot draw from the sellable Available bucket (legacy FIFO behaviour).
            foreach (var line in purchaseReturn.LineItems)
            {
                int acceptedQuantity = line.Quantity - line.RejectedQuantity;
                if (acceptedQuantity <= 0) continue;

                var variantId = purchaseReturn.PurchaseOrder?.LineItems
                    .FirstOrDefault(pol => pol.Id == line.PurchaseOrderLineId)?.VariantId;
                var stockLevels = await _stockLevelRepository.GetByPartAndVariantAsync(line.PartId, variantId, cancellationToken);
                var stockLevel = stockLevels.FirstOrDefault();
                if (stockLevel == null)
                    return BadRequest(new { message = $"No stock level found for part {line.PartId}. Cannot process return." });

                var bucket = "AVAILABLE";
                if (line.StockLotId.HasValue)
                {
                    var lot = await _stockLotRepository.GetByIdAsync(line.StockLotId.Value, cancellationToken);
                    if (lot == null)
                        return BadRequest(new { message = $"Selected stock lot not found for part {line.Part?.Name ?? line.PartId.ToString()}." });
                    bucket = NormalizeBucket(lot.Status);
                }

                int bucketAvailable = GetBucketAvailable(stockLevel, bucket);
                if (bucketAvailable < acceptedQuantity)
                    return BadRequest(new { message = $"Insufficient {bucket.ToLower()} stock for part {line.Part?.Name ?? line.PartId.ToString()}. {bucket}: {bucketAvailable}, Required: {acceptedQuantity}" });
            }

            // Process stock movements for each line item inside a transaction, run under the EF
            // execution strategy (global retry policy). The aggregate + stock entities are reloaded
            // INSIDE the lambda so a retry after rollback applies movements exactly once.
            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new InvalidOperationException("Purchase return not found");
                    purchaseReturn.MarkAsReturned();
                    purchaseReturn.ModifiedBy = _currentUserService.GetCurrentUsername();

                    foreach (var line in purchaseReturn.LineItems)
                    {
                        int acceptedQuantity = line.Quantity - line.RejectedQuantity;
                        if (acceptedQuantity <= 0) continue;

                        var variantId = purchaseReturn.PurchaseOrder?.LineItems
                            .FirstOrDefault(pol => pol.Id == line.PurchaseOrderLineId)?.VariantId;
                        var stockLevels = await _stockLevelRepository.GetByPartAndVariantAsync(line.PartId, variantId, cancellationToken);
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
                                    throw new InvalidOperationException($"Insufficient stock in selected lot. Available: {selectedLot.QuantityAvailable}, Required: {acceptedQuantity}");
                                }

                                // Reduce stock from the bucket matching the lot's status (Available/Damaged/Quarantine).
                                // Items are being returned to supplier.
                                var bucket = NormalizeBucket(selectedLot.Status);
                                RemoveFromBucket(stockLevel, bucket, acceptedQuantity);
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
                                // No lot selected => sellable Available bucket only (Damaged/Quarantine returns must pick a lot).
                                var supplierLots = allLots
                                    .Where(l => l.SupplierId == purchaseReturn.SupplierId && l.VariantId == variantId && l.QuantityAvailable > 0 && l.Status == "AVAILABLE")
                                    .OrderBy(l => l.ReceivingDate)  // FIFO - oldest lots first
                                    .ToList();

                                // Must have enough AVAILABLE stock from THIS supplier. We never draw a return
                                // from another supplier's lots — that would destroy lot/supplier traceability.
                                // If the originating supplier's lots are short, the operator must pick specific
                                // lots on the return lines instead.
                                int totalAvailableFromSupplier = supplierLots.Sum(l => l.QuantityAvailable);
                                if (totalAvailableFromSupplier < acceptedQuantity)
                                {
                                    throw new InvalidOperationException(
                                        $"Insufficient available stock from this supplier for part {line.Part?.Name ?? line.PartId.ToString()} " +
                                        $"(available: {totalAvailableFromSupplier}, required: {acceptedQuantity}). " +
                                        "Select specific stock lots on the return lines to proceed.");
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
            }); // end execution strategy

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
    [Authorize(Roles = "Admin,Manager")]
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
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> IssueCreditNote(Guid id, [FromQuery] decimal creditAmount, CancellationToken cancellationToken)
    {
        try
        {
            var purchaseReturn = await _purchaseReturnRepository.GetByIdAsync(id, cancellationToken);
            if (purchaseReturn is null) return NotFound(new { message = "Purchase return not found" });

            // Pre-fetch read-only data before the transaction
            var linkedPO = await _dbContext.PurchaseOrders
                .FirstOrDefaultAsync(po => po.Id == purchaseReturn.PurchaseOrderId, cancellationToken);
            var currency = linkedPO?.Currency ?? "NPR";
            var defaultProvider = await _dbContext.PaymentProviders.FirstOrDefaultAsync(cancellationToken);

            var creditNoteNumber = await _codeGenerateService.GenerateAsync("CN", cancellationToken);
            var currentUser = _currentUserService.GetCurrentUsername();

            var creditNote = CreditNote.Create(
                creditNoteNumber: creditNoteNumber,
                supplierId: purchaseReturn.SupplierId,
                purchaseReturnId: purchaseReturn.Id,
                amount: creditAmount,
                currency: currency,
                issueDate: DateTime.UtcNow,
                notes: $"Credit note for return {purchaseReturn.ReturnNumber}",
                issuedBy: currentUser
            );

            // All writes in one transaction — if the SupplierPayment insert fails, the
            // credit note and return status update are rolled back together.
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                purchaseReturn.IssueCreditNote(creditAmount);
                purchaseReturn.ModifiedBy = currentUser;
                await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

                await _creditNoteRepository.AddAsync(creditNote, cancellationToken);

                purchaseReturn.SetCreditNote(creditNote.Id);
                await _purchaseReturnRepository.UpdateAsync(purchaseReturn, cancellationToken);

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
                    supplierPayment.MarkAsAdvance();
                    supplierPayment.MarkAsProcessed(currentUser);
                    supplierPayment.ConfirmReceipt(currentUser); // advance to COMPLETED so it surfaces in GetAvailableAdvanceCreditAsync
                    await _supplierPaymentRepository.AddAsync(supplierPayment, cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
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
    [Authorize(Roles = "Admin,Manager")]
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
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
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
    [Authorize(Roles = "Admin,Manager")]
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
        [FromQuery] string? bucket = null,
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

            // Filter to lots with available quantity, optionally restricted to a single inventory bucket
            // (AVAILABLE / DAMAGED / QUARANTINE) so the return form can show only matching-status lots.
            var normalizedBucket = string.IsNullOrWhiteSpace(bucket) ? null : NormalizeBucket(bucket);
            var availableLots = allLots
                .Where(l => l.QuantityAvailable > 0)
                .Where(l => normalizedBucket == null || NormalizeBucket(l.Status) == normalizedBucket)
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
                    IsFromSameSupplier = supplierId.HasValue && l.SupplierId == supplierId.Value,
                    Status = NormalizeBucket(l.Status)
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

    /// <summary>
    /// Builds a draft Purchase Return payload from an accepted Goods Receipt: one line per GRN line that
    /// still has un-returned damaged or wrong units, each pointing at the specific DAMAGED/QUARANTINE lot
    /// that GRN line created. Powers the "Create Return" action on the Goods Receipt view.
    /// </summary>
    [HttpGet("from-goods-receipt/{goodsReceiptId:guid}")]
    public async Task<IActionResult> GetReturnPrefillFromGrn(Guid goodsReceiptId, CancellationToken cancellationToken)
    {
        try
        {
            var grn = await _dbContext.GoodsReceipts
                .AsNoTracking()
                .Include(g => g.LineItems).ThenInclude(l => l.Part)
                .Include(g => g.LineItems).ThenInclude(l => l.Variant)
                .Include(g => g.PurchaseOrder).ThenInclude(p => p!.Supplier)
                .Include(g => g.PurchaseOrder).ThenInclude(p => p!.LineItems)
                .FirstOrDefaultAsync(g => g.Id == goodsReceiptId, cancellationToken);

            if (grn == null)
                return NotFound(new { message = "Goods receipt not found" });

            var po = grn.PurchaseOrder;
            if (po == null)
                return BadRequest(new { message = "Goods receipt has no associated purchase order" });

            // Lots created by this GRN's lines (damaged/quarantine lots carry the matching Status)
            var grnLineIds = grn.LineItems.Select(l => l.Id).ToList();
            var lots = await _dbContext.StockLots
                .AsNoTracking()
                .Where(l => grnLineIds.Contains(l.GoodsReceiptLineId))
                .ToListAsync(cancellationToken);

            // Units already returned per lot (so re-opening the draft excludes what's gone back already)
            var lotIds = lots.Select(l => l.Id).ToList();
            var returnedMap = (await _dbContext.PurchaseReturns
                .AsNoTracking()
                .SelectMany(r => r.LineItems)
                .Where(li => li.StockLotId != null && lotIds.Contains(li.StockLotId.Value))
                .GroupBy(li => li.StockLotId!.Value)
                .Select(g => new { LotId = g.Key, Qty = g.Sum(x => x.Quantity) })
                .ToListAsync(cancellationToken))
                .ToDictionary(x => x.LotId, x => x.Qty);

            var prefill = new ReturnPrefillFromGrnDto
            {
                GoodsReceiptId = grn.Id,
                GrnNumber = grn.GRNNumber,
                PurchaseOrderId = po.Id,
                PurchaseOrderNumber = po.PONumber,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier?.Name
            };

            bool anyDamaged = false, anyWrong = false;

            foreach (var grnLine in grn.LineItems)
            {
                var unitPrice = grnLine.UnitCost > 0
                    ? grnLine.UnitCost
                    : (po.LineItems.FirstOrDefault(l => l.Id == grnLine.PurchaseOrderLineId)?.UnitPrice ?? grnLine.UnitCost);
                var displayName = VariantNaming.Compose(grnLine.Part?.Name ?? string.Empty, grnLine.Variant?.Name);
                var partSku = grnLine.Part?.SKU ?? string.Empty;

                void AddLine(int bucketQty, string bucket, string condition, string defaultNote)
                {
                    var lot = lots.FirstOrDefault(l => l.GoodsReceiptLineId == grnLine.Id && NormalizeBucket(l.Status) == bucket);
                    int already = lot != null && returnedMap.TryGetValue(lot.Id, out var q) ? q : 0;
                    int remaining = bucketQty - already;
                    if (lot != null) remaining = Math.Min(remaining, lot.QuantityAvailable);
                    if (remaining <= 0) return;

                    if (bucket == "DAMAGED") anyDamaged = true; else anyWrong = true;
                    prefill.Lines.Add(new ReturnPrefillLineDto
                    {
                        PurchaseOrderLineId = grnLine.PurchaseOrderLineId,
                        PartId = grnLine.PartId,
                        DisplayName = displayName,
                        PartSku = partSku,
                        StockLotId = lot?.Id,
                        LotNumber = lot?.LotNumber,
                        Bucket = bucket,
                        Quantity = remaining,
                        UnitPrice = unitPrice,
                        Condition = condition,
                        Notes = string.IsNullOrWhiteSpace(grnLine.RejectionReason) ? defaultNote : grnLine.RejectionReason
                    });
                }

                if (grnLine.DamagedQuantity > 0)
                    AddLine(grnLine.DamagedQuantity, "DAMAGED", MapGrnConditionToReturn(grnLine.Condition), "Damaged on receipt");

                if (grnLine.WrongQuantity > 0)
                    AddLine(grnLine.WrongQuantity, "QUARANTINE", "OPENED", "Wrong / incorrect item");
            }

            prefill.Reason = anyWrong && !anyDamaged ? "WRONG_ITEM" : "DAMAGED";
            return Ok(prefill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error building return prefill from goods receipt: {GoodsReceiptId}", goodsReceiptId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while building the return draft");
        }
    }

    /// <summary>
    /// Maps a GoodsReceiptLine condition (GOOD/ACCEPTABLE/DAMAGED/DEFECTIVE/MISSING) to a valid
    /// PurchaseReturnLine condition (UNOPENED/OPENED/DAMAGED/DEFECTIVE).
    /// </summary>
    private static string MapGrnConditionToReturn(string? grnCondition) => grnCondition?.ToUpper() switch
    {
        "DEFECTIVE" => "DEFECTIVE",
        _ => "DAMAGED"
    };

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
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

    /// <summary>
    /// Option A guard: every return line must reference a line on the given purchase order,
    /// the line's part must match that PO line, the PO must belong to the supplier, and the
    /// return quantity may not exceed the ordered quantity. This keeps returns scoped to what
    /// was actually purchased from this supplier (correct variant/lot/supplier resolution and
    /// credit-note valuation downstream). Returns an error message, or null when valid.
    /// </summary>
    private async Task<string?> ValidateReturnLinesAgainstPoAsync(
        Guid purchaseOrderId, Guid supplierId,
        IEnumerable<CreatePurchaseReturnLineRequest> lines,
        CancellationToken cancellationToken)
    {
        var po = await _dbContext.PurchaseOrders
            .Include(p => p.LineItems)
            .FirstOrDefaultAsync(p => p.Id == purchaseOrderId, cancellationToken);

        if (po == null)
            return "Selected purchase order not found.";
        if (po.SupplierId != supplierId)
            return "Supplier does not match the selected purchase order.";

        foreach (var line in lines)
        {
            var poLine = po.LineItems.FirstOrDefault(l => l.Id == line.PurchaseOrderLineId);
            if (poLine == null)
                return $"A return line references an item that is not on purchase order {po.PONumber}. Only items from the selected purchase order can be returned.";
            if (poLine.PartId != line.PartId)
                return "A return line's product does not match its purchase order line.";
            if (line.Quantity > poLine.Quantity)
                return $"Return quantity ({line.Quantity}) exceeds the ordered quantity ({poLine.Quantity}) for an item on purchase order {po.PONumber}.";
        }

        return null;
    }

    /// <summary>
    /// Normalises a StockLot.Status into one of the three inventory buckets.
    /// Anything that isn't DAMAGED/QUARANTINE is treated as the sellable AVAILABLE bucket.
    /// </summary>
    private static string NormalizeBucket(string? status) => status?.Trim().ToUpper() switch
    {
        "DAMAGED" => "DAMAGED",
        "QUARANTINE" => "QUARANTINE",
        _ => "AVAILABLE"
    };

    /// <summary>Available units in the given StockLevel bucket.</summary>
    private static int GetBucketAvailable(StockLevel stockLevel, string bucket) => bucket switch
    {
        "DAMAGED" => stockLevel.QuantityDamaged,
        "QUARANTINE" => stockLevel.QuantityQuarantine,
        _ => stockLevel.QuantityAvailable
    };

    /// <summary>Removes units from the given StockLevel bucket (Available/Damaged/Quarantine).</summary>
    private static void RemoveFromBucket(StockLevel stockLevel, string bucket, int quantity)
    {
        switch (bucket)
        {
            case "DAMAGED":
                stockLevel.RemoveDamagedStock(quantity, quantity, "Purchase Return");
                break;
            case "QUARANTINE":
                stockLevel.RemoveQuarantineStock(quantity, quantity, "Purchase Return");
                break;
            default:
                stockLevel.RemoveStock(quantity, quantity, "Purchase Return");
                break;
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
            Lines = purchaseReturn.LineItems.Select(l =>
            {
                var variantName = purchaseReturn.PurchaseOrder?.LineItems
                    .FirstOrDefault(pol => pol.Id == l.PurchaseOrderLineId)?.Variant?.Name;
                var partName = l.Part?.Name ?? string.Empty;
                return new PurchaseReturnLineResponse
                {
                    Id = l.Id,
                    PartId = l.PartId,
                    PartName = partName,
                    PartLocalName = l.Part?.LocalName,
                    PartSku = l.Part?.SKU ?? string.Empty,
                    VariantName = variantName,
                    DisplayName = VariantNaming.Compose(partName, variantName),
                    StockLotId = l.StockLotId,
                    LotNumber = l.StockLot?.LotNumber,
                    Quantity = l.Quantity,
                    RejectedQuantity = l.RejectedQuantity,
                    UnitPrice = l.UnitPrice,
                    RefundAmount = l.RefundAmount,
                    Condition = l.Condition,
                    Notes = l.Notes
                };
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
