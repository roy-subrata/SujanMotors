using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using System.Linq;

namespace AutoPartShop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SalesReturnController : ControllerBase
    {
        private readonly ISalesReturnRepository _salesReturnRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ICustomerPaymentRepository _customerPaymentRepository;
        private readonly ICustomerCreditNoteRepository _customerCreditNoteRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICodeGenerateService _codeGenerateService;
        private readonly AutoPartDbContext _dbContext;
        private readonly ILogger<SalesReturnController> _logger;

        public SalesReturnController(
            ISalesReturnRepository salesReturnRepository,
            ISalesOrderRepository salesOrderRepository,
            ICustomerPaymentRepository customerPaymentRepository,
            ICustomerCreditNoteRepository customerCreditNoteRepository,
            ICurrentUserService currentUserService,
            ICodeGenerateService codeGenerateService,
            AutoPartDbContext dbContext,
            ILogger<SalesReturnController> logger)
        {
            _salesReturnRepository = salesReturnRepository;
            _salesOrderRepository = salesOrderRepository;
            _customerPaymentRepository = customerPaymentRepository;
            _customerCreditNoteRepository = customerCreditNoteRepository;
            _currentUserService = currentUserService;
            _codeGenerateService = codeGenerateService;
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SalesReturnResponse>> Get(Guid id)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();
            return Ok(MapToResponse(salesReturn));
        }

        [HttpPost]
        public async Task<ActionResult<SalesReturnResponse>> Create([FromBody] CreateSalesReturnRequest request)
        {
            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest("At least one return line is required.");

            var salesOrder = await _salesOrderRepository.GetByIdAsync(request.SalesOrderId);
            if (salesOrder == null)
                return BadRequest("Sales order not found.");

            var returnableStatuses = new[] { "CONFIRMED", "PARTIALLY_SHIPPED", "SHIPPED", "DELIVERED" };
            if (!returnableStatuses.Contains(salesOrder.Status))
                return BadRequest($"Cannot create a return for a sales order with status '{salesOrder.Status}'. Only confirmed, shipped, or delivered orders can be returned.");

            // Load existing returns for this sales order to check cumulative quantities
            var existingReturns = await _salesReturnRepository.GetBySalesOrderAsync(request.SalesOrderId);
            var activeReturns = existingReturns
                .Where(r => r.Status != "REJECTED")
                .ToList();

            var returnNumber = await _codeGenerateService.GenerateAsync("SR");
            var salesReturn = SalesReturn.Create(returnNumber, request.SalesOrderId, null, request.Reason, request.WarehouseId, DateTime.UtcNow, request.Notes);
            salesReturn.SetRefundType(request.RefundType);

            foreach (var line in request.Lines)
            {
                var orderLine = salesOrder.LineItems.FirstOrDefault(ol => ol.Id == line.SalesOrderLineId);
                if (orderLine == null)
                    return BadRequest($"Sales order line {line.SalesOrderLineId} not found on order {salesOrder.SONumber}.");

                // Use shipped quantity when available; fall back to ordered quantity until dispatch tracking is built.
                // Once UpdateShippedQuantity is called by a dispatch endpoint, this automatically enforces the tighter limit.
                var returnableQty = orderLine.ShippedQuantity > 0 ? orderLine.ShippedQuantity : orderLine.Quantity;
                var returnableLabel = orderLine.ShippedQuantity > 0 ? "shipped" : "ordered";

                if (line.Quantity > returnableQty)
                    return BadRequest($"Return quantity ({line.Quantity}) exceeds {returnableLabel} quantity ({returnableQty}) for part {line.PartId}.");

                // Check cumulative returned quantity across all active returns for this SO line
                var alreadyReturned = activeReturns
                    .SelectMany(r => r.LineItems)
                    .Where(rl => rl.SalesOrderLineId == line.SalesOrderLineId)
                    .Sum(rl => rl.Quantity);

                if (alreadyReturned + line.Quantity > returnableQty)
                    return BadRequest($"Total return quantity ({alreadyReturned + line.Quantity}) would exceed {returnableLabel} quantity ({returnableQty}) for part {line.PartId}. Already returned: {alreadyReturned}.");

                var returnLine = SalesReturnLine.Create(
                    salesReturn.Id,
                    line.SalesOrderLineId,
                    line.PartId,
                    line.Quantity,
                    line.UnitPrice,
                    line.Condition,
                    unitId: line.UnitId,
                    quantityInBaseUnit: line.QuantityInBaseUnit,
                    unitPriceInBaseUnit: line.UnitPriceInBaseUnit
                );
                returnLine.AddNotes(line.Notes);
                salesReturn.LineItems.Add(returnLine);
            }
            salesReturn.CalculateRefund();

            await _salesReturnRepository.AddAsync(salesReturn);
            await _codeGenerateService.SaveGenerateCodeAsync("SR");

            return CreatedAtAction(nameof(Get), new { id = salesReturn.Id }, MapToResponse(salesReturn));
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<SalesReturnResponse>> Update(Guid id, [FromBody] CreateSalesReturnRequest request)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();

            if (salesReturn.Status != "PENDING")
                return BadRequest("Only pending returns can be updated.");

            salesReturn.UpdateNotes(request.Notes);
            // For simplicity, not updating lines in this example. Implement as needed.
            await _salesReturnRepository.UpdateAsync(salesReturn);
            return Ok(MapToResponse(salesReturn));
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SalesReturnResponse>>> List()
        {
            var returns = await _salesReturnRepository.GetAllAsync();
            return Ok(returns.Select(MapToResponse));
        }

        [HttpGet("list")]
        public async Task<ActionResult<object>> ListPaged(int pageNumber = 1, int pageSize = 10, string? searchTerm = null)
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, totalCount) = await _salesReturnRepository.GetPagedAsync(pageNumber, pageSize, searchTerm);
            var response = items.Select(MapToResponse);
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }

        [HttpPatch("{id}/approve")]
        public async Task<ActionResult<SalesReturnResponse>> Approve(Guid id, CancellationToken cancellationToken)
        {
            SalesReturn? salesReturn = null;
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                try
                {
                    salesReturn = await _dbContext.SalesReturns
                        .Include(x => x.LineItems)
                            .ThenInclude(li => li.Part)
                        .Include(x => x.SalesOrder)
                        .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

                    if (salesReturn == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    if (salesReturn.Status != "PENDING")
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    var approver = _currentUserService.GetCurrentUsername();
                    if (string.IsNullOrWhiteSpace(approver))
                        approver = "system";

                    salesReturn.Approve(approver);
                    salesReturn.ModifiedBy = approver;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (salesReturn == null)
                return NotFound();

            if (salesReturn.Status != "APPROVED")
                return BadRequest($"Cannot approve return with current status '{salesReturn.Status}'. Only PENDING returns can be approved.");

            return Ok(MapToResponse(salesReturn));
        }

        [HttpPatch("{id}/receive")]
        public async Task<ActionResult<SalesReturnResponse>> Receive(Guid id, CancellationToken cancellationToken)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id, cancellationToken);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "APPROVED")
                return BadRequest("Only approved returns can be marked as received.");
            salesReturn.MarkAsReceived();
            await _salesReturnRepository.UpdateAsync(salesReturn, cancellationToken);
            return Ok(MapToResponse(salesReturn));
        }

        [HttpPatch("{id}/process")]
        public async Task<ActionResult<SalesReturnResponse>> Process(Guid id, CancellationToken cancellationToken)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id, cancellationToken);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "RECEIVED")
                return BadRequest("Only received returns can be processed.");

            // Use execution strategy to support SqlServerRetryingExecutionStrategy with transactions
            var strategy = _dbContext.Database.CreateExecutionStrategy();

            var warehouseId = salesReturn.WarehouseId;
            if (warehouseId == Guid.Empty)
                return BadRequest("WarehouseId is required for stock adjustment.");

            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync();

                    try
                    {
                // --- Stock Adjustment Logic ---

                var salesOrder = await _salesOrderRepository.GetByIdAsync(salesReturn.SalesOrderId);

                foreach (var line in salesReturn.LineItems)
                {
                    // Use InBaseUnit quantities if available, otherwise fall back to regular quantities
                    var returnQuantity = line.QuantityInBaseUnit > 0 ? line.QuantityInBaseUnit : line.Quantity;

                    var stockLevel = await _dbContext.StockLevels.FirstOrDefaultAsync(sl => sl.PartId == line.PartId && sl.WarehouseId == warehouseId);
                    if (stockLevel == null)
                    {
                        stockLevel = Domain.Entities.StockLevel.Create(line.PartId, warehouseId, unitId: line.UnitId);
                        stockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
                        _dbContext.StockLevels.Add(stockLevel);
                    }

                    // Add stock with both unit quantities
                    stockLevel.AddStock(
                        quantity: line.Quantity,
                        quantityInBaseUnit: returnQuantity,
                        reason: $"Sales Return {salesReturn.ReturnNumber}");
                    stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Create stock movement record - RETURN type for customer returns
                    var stockMovement = Domain.Entities.StockMovement.Create(
                        stockLevel.Id,
                        "RETURN",  // RETURN type to distinguish from regular incoming stock
                        quantity: line.Quantity,
                        reason: $"Customer Return - {salesReturn.Reason}",
                        referenceNumber: salesReturn.ReturnNumber,
                        unitId: line.UnitId,
                        quantityInBaseUnit: returnQuantity
                    );
                    stockMovement.Approve(_currentUserService.GetCurrentUsername());
                    stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                    stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                    _dbContext.StockMovements.Add(stockMovement);

                    // Create stock lot movement - add back to a lot
                    try
                    {
                        // Try to find existing lots for this part
                        var existingLot = await _dbContext.StockLots
                            .Where(sl => sl.PartId == line.PartId &&
                                        sl.WarehouseId == warehouseId &&
                                        sl.QuantityAvailable > 0)
                            .OrderByDescending(sl => sl.CreatedDate)
                            .FirstOrDefaultAsync();

                        Domain.Entities.StockLot targetLot;
                        if (existingLot != null)
                        {
                            // Increase capacity first so AddStock won't be capped at original QuantityReceived
                            existingLot.IncreaseCapacity(
                                quantity: line.Quantity,
                                quantityInBaseUnit: returnQuantity);
                            // Add to existing lot with both unit quantities
                            existingLot.AddStock(
                                quantity: line.Quantity,
                                quantityInBaseUnit: returnQuantity,
                                reason: $"Sales Return {salesReturn.ReturnNumber}");
                            existingLot.ModifiedBy = _currentUserService.GetCurrentUsername();
                            targetLot = existingLot;
                        }
                        else
                        {
                            // Create new lot for returned items using return-specific factory
                            var lotNumber = $"RET-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
                            targetLot = Domain.Entities.StockLot.CreateForReturn(
                                lotNumber,
                                line.PartId,
                                warehouseId,
                                quantityReceived: line.Quantity,
                                costPrice: line.UnitPrice,
                                receivingDate: DateTime.UtcNow,
                                returnNumber: $"Return {salesReturn.ReturnNumber}",
                                currency: salesReturn.SalesOrder?.Currency ?? "BDT",
                                notes: $"Created from sales return {salesReturn.ReturnNumber}",
                                unitId: line.UnitId,
                                quantityReceivedInBaseUnit: returnQuantity,
                                costPriceInBaseUnit: line.UnitPriceInBaseUnit > 0 ? line.UnitPriceInBaseUnit : line.UnitPrice
                            );
                            targetLot.CreatedBy = _currentUserService.GetCurrentUsername();
                            targetLot.ModifiedBy = _currentUserService.GetCurrentUsername();
                            _dbContext.StockLots.Add(targetLot);
                            await _dbContext.SaveChangesAsync(cancellationToken); // Save to get the lot ID
                        }

                        // Create lot movement with both unit quantities
                        var lotMovement = Domain.Entities.StockLotMovement.Create(
                            targetLot.Id,
                            quantity: line.Quantity,
                            movementType: "RETURN",
                            referenceId: salesReturn.Id,
                            referenceType: "SalesReturn",
                            movementDate: null,
                            costAtMovement: line.UnitPrice,
                            reason: $"Sales Return {salesReturn.ReturnNumber}",
                            notes: "",
                            unitId: line.UnitId,
                            quantityInBaseUnit: returnQuantity,
                            costAtMovementInBaseUnit: line.UnitPriceInBaseUnit > 0 ? line.UnitPriceInBaseUnit : line.UnitPrice
                        );
                        lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                        lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                        _dbContext.StockLotMovements.Add(lotMovement);
                    }
                    catch (Exception lotEx)
                    {
                        // Continue execution - lot tracking is optional but log the issue
                        _logger.LogWarning(lotEx, "Failed to create lot tracking for part {PartId} in return {ReturnNumber}", line.PartId, salesReturn.ReturnNumber);
                    }
                }
                await _dbContext.SaveChangesAsync(cancellationToken);

                // --- Customer Balance Update & Refund Processing ---
                if (salesOrder != null && salesOrder.CustomerId != Guid.Empty)
                {
                    var customer = await _dbContext.Customers.FindAsync(salesOrder.CustomerId);
                    if (customer != null && salesReturn.RefundAmount > 0)
                    {
                        if (salesReturn.RefundType == "CASH_REFUND")
                        {
                            // CASH REFUND: Give money back, reduce outstanding balance
                            customer.UpdateBalance(-salesReturn.RefundAmount);
                            customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                            // Create CustomerPayment record with NEGATIVE amount and COMPLETED status
                            // Negative amount reduces Total Paid correctly
                            var refundPayment = CustomerPayment.Create(
                                customerId: salesOrder.CustomerId,
                                paymentProviderId: null,
                                amount: -salesReturn.RefundAmount,  // NEGATIVE amount - money going OUT
                                paymentMethod: "REFUND",
                                transactionNumber: $"REFUND-{salesReturn.ReturnNumber}",
                                referenceNumber: salesReturn.ReturnNumber,
                                paymentDate: DateTime.UtcNow
                            );

                            if (salesReturn.InvoiceId.HasValue)
                            {
                                refundPayment.LinkToInvoice(salesReturn.InvoiceId.Value);
                            }

                            // Mark as COMPLETED (not REFUNDED) so it's included in Total Paid calculation
                            refundPayment.MarkAsCompleted();
                            refundPayment.CreatedBy = _currentUserService.GetCurrentUsername();
                            refundPayment.ModifiedBy = _currentUserService.GetCurrentUsername();
                            refundPayment.UpdateNotes($"Cash refund for sales return {salesReturn.ReturnNumber}. Reason: {salesReturn.Reason}");

                            await _customerPaymentRepository.AddAsync(refundPayment, CancellationToken.None);

                            // Update SalesOrder PaidAmount to reflect the refund
                            salesOrder.ProcessRefund(salesReturn.RefundAmount);
                            salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _salesOrderRepository.UpdateAsync(salesOrder, CancellationToken.None);
                        }
                        else if (salesReturn.RefundType == "STORE_CREDIT")
                        {
                            // STORE CREDIT: Create CustomerCreditNote for future purchases
                            // This creates a proper credit note that can be tracked and applied

                            var creditNoteNumber = $"CN-CUST-{DateTime.UtcNow:yyyyMMddHHmmss}";
                            var customerCreditNote = CustomerCreditNote.Create(
                                creditNoteNumber: creditNoteNumber,
                                customerId: salesOrder.CustomerId,
                                salesReturnId: salesReturn.Id,
                                amount: salesReturn.RefundAmount,
                                currency: salesOrder?.Currency ?? "BDT",
                                issueDate: DateTime.UtcNow,
                                expiryDate: DateTime.UtcNow.AddMonths(6),
                                notes: $"Store credit for sales return {salesReturn.ReturnNumber}. Reason: {salesReturn.Reason}",
                                issuedBy: _currentUserService.GetCurrentUsername()
                            );

                            await _customerCreditNoteRepository.AddAsync(customerCreditNote, CancellationToken.None);

                            // Link the credit note to the sales return
                            salesReturn.SetCustomerCreditNote(customerCreditNote.Id);
                        }
                    }
                }

                salesReturn.Process();
                await _salesReturnRepository.UpdateAsync(salesReturn);

                // Commit the transaction
                await transaction.CommitAsync();
                    }
                    catch
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                });

                return Ok(MapToResponse(salesReturn));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing sales return {ReturnId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the sales return");
            }
        }

        public class RejectRequest { public string? Reason { get; set; } }

        [HttpPatch("{id}/reject")]
        public async Task<ActionResult<SalesReturnResponse>> Reject(Guid id, [FromBody] RejectRequest request, CancellationToken cancellationToken)
        {
            SalesReturn? salesReturn = null;
            string? validationError = null;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                try
                {
                    salesReturn = await _dbContext.SalesReturns
                        .Include(x => x.LineItems)
                        .FirstOrDefaultAsync(x => x.Id == id && !x.Isdeleted, cancellationToken);

                    if (salesReturn == null)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    if (salesReturn.Status != "PENDING" && salesReturn.Status != "APPROVED" && salesReturn.Status != "RECEIVED" && salesReturn.Status != "PROCESSED")
                    {
                        validationError = "Only pending, approved, received, or processed returns can be rejected.";
                        await transaction.RollbackAsync(cancellationToken);
                        return;
                    }

                    var actor = _currentUserService.GetCurrentUsername();
                    if (string.IsNullOrWhiteSpace(actor))
                        actor = "system";

                    if (salesReturn.Status == "PROCESSED")
                    {
                        foreach (var line in salesReturn.LineItems)
                        {
                            var reverseQuantity = line.QuantityInBaseUnit > 0 ? line.QuantityInBaseUnit : line.Quantity;

                            var stockLevel = await _dbContext.StockLevels
                                .FirstOrDefaultAsync(sl => sl.PartId == line.PartId && sl.WarehouseId == salesReturn.WarehouseId, cancellationToken);

                            if (stockLevel == null)
                                throw new InvalidOperationException($"Cannot reverse return stock: no stock level found for part {line.PartId}.");

                            stockLevel.RemoveStock(line.Quantity, reverseQuantity, "RETURN_REJECTION_REVERSAL");
                            stockLevel.ModifiedBy = actor;

                            var reversalMovement = Domain.Entities.StockMovement.Create(
                                stockLevel.Id,
                                movementType: "OUT",
                                quantity: line.Quantity,
                                reason: "RETURN_REJECTION_REVERSAL",
                                referenceNumber: salesReturn.ReturnNumber,
                                unitId: line.UnitId,
                                quantityInBaseUnit: reverseQuantity);
                            reversalMovement.Approve(actor);
                            reversalMovement.AddNotes($"Stock reversal due to rejection of processed return {salesReturn.ReturnNumber}");
                            reversalMovement.CreatedBy = actor;
                            reversalMovement.ModifiedBy = actor;
                            _dbContext.StockMovements.Add(reversalMovement);

                            var remaining = line.Quantity;
                            var remainingBase = reverseQuantity;
                            var lots = await _dbContext.StockLots
                                .Where(l => l.PartId == line.PartId && l.WarehouseId == salesReturn.WarehouseId && l.IsActive && l.QuantityAvailable > 0)
                                .OrderByDescending(l => l.Notes.Contains(salesReturn.ReturnNumber))
                                .ThenByDescending(l => l.CreatedDate)
                                .ToListAsync(cancellationToken);

                            foreach (var lot in lots)
                            {
                                if (remaining <= 0)
                                    break;

                                var qtyToRemove = Math.Min(remaining, lot.QuantityAvailable);
                                var baseToRemove = Math.Min(
                                    remainingBase > 0 ? remainingBase : qtyToRemove,
                                    lot.QuantityAvailableInBaseUnit > 0 ? lot.QuantityAvailableInBaseUnit : qtyToRemove);

                                lot.RemoveStock(qtyToRemove, baseToRemove, "RETURN_REJECTION_REVERSAL");
                                lot.ModifiedBy = actor;

                                var lotMovement = Domain.Entities.StockLotMovement.Create(
                                    stockLotId: lot.Id,
                                    quantity: qtyToRemove,
                                    movementType: "ADJUSTMENT",
                                    referenceId: salesReturn.Id,
                                    referenceType: "SalesReturnRejection",
                                    movementDate: DateTime.UtcNow,
                                    costAtMovement: line.UnitPrice,
                                    reason: "RETURN_REJECTION_REVERSAL",
                                    notes: $"Lot reversal due to rejection of processed return {salesReturn.ReturnNumber}",
                                    unitId: line.UnitId,
                                    quantityInBaseUnit: baseToRemove,
                                    costAtMovementInBaseUnit: line.UnitPriceInBaseUnit > 0 ? line.UnitPriceInBaseUnit : line.UnitPrice);
                                lotMovement.CreatedBy = actor;
                                lotMovement.ModifiedBy = actor;
                                _dbContext.StockLotMovements.Add(lotMovement);

                                remaining -= qtyToRemove;
                                remainingBase -= baseToRemove;
                            }

                            if (remaining > 0)
                                throw new InvalidOperationException($"Cannot reverse complete lot quantity for part {line.PartId}. Remaining quantity: {remaining}");
                        }

                        if (salesReturn.RefundAmount > 0)
                        {
                            var salesOrder = await _dbContext.SalesOrders
                                .FirstOrDefaultAsync(so => so.Id == salesReturn.SalesOrderId && !so.Isdeleted, cancellationToken);

                            if (salesOrder != null)
                            {
                                if (salesReturn.RefundType == "STORE_CREDIT" && salesReturn.CustomerCreditNoteId.HasValue)
                                {
                                    var creditNote = await _dbContext.CustomerCreditNotes
                                        .FirstOrDefaultAsync(cn => cn.Id == salesReturn.CustomerCreditNoteId.Value, cancellationToken);

                                    if (creditNote != null)
                                    {
                                        if (creditNote.UsedAmount > 0)
                                            throw new InvalidOperationException("Cannot reject this processed return because its credit note has already been used.");

                                        creditNote.Cancel($"Sales return {salesReturn.ReturnNumber} was rejected");
                                    }
                                }

                                if (salesReturn.RefundType == "CASH_REFUND")
                                {
                                    var refundPaymentExists = await _dbContext.CustomerPayments
                                        .AnyAsync(p => p.ReferenceNumber == salesReturn.ReturnNumber && p.PaymentMethod == "REFUND" && p.Amount < 0, cancellationToken);

                                    var reversalPaymentExists = await _dbContext.CustomerPayments
                                        .AnyAsync(p => p.ReferenceNumber == salesReturn.ReturnNumber && p.PaymentMethod == "REFUND_REVERSAL" && p.Amount > 0, cancellationToken);

                                    if (refundPaymentExists && !reversalPaymentExists)
                                    {
                                        var customer = await _dbContext.Customers
                                            .FirstOrDefaultAsync(c => c.Id == salesOrder.CustomerId && !c.Isdeleted, cancellationToken);

                                        if (customer != null)
                                        {
                                            customer.UpdateBalance(salesReturn.RefundAmount);
                                            customer.ModifiedBy = actor;
                                        }

                                        salesOrder.RecordPayment(salesReturn.RefundAmount);
                                        salesOrder.ModifiedBy = actor;

                                        var reversalPayment = Domain.Entities.CustomerPayment.Create(
                                            customerId: salesOrder.CustomerId,
                                            paymentProviderId: null,
                                            amount: salesReturn.RefundAmount,
                                            paymentMethod: "REFUND_REVERSAL",
                                            transactionNumber: $"RREV-{salesReturn.ReturnNumber}",
                                            referenceNumber: salesReturn.ReturnNumber,
                                            paymentDate: DateTime.UtcNow);
                                        if (salesReturn.InvoiceId.HasValue)
                                            reversalPayment.LinkToInvoice(salesReturn.InvoiceId.Value);

                                        reversalPayment.MarkAsCompleted();
                                        reversalPayment.UpdateNotes($"Compensation payment for rejected processed return {salesReturn.ReturnNumber}");
                                        reversalPayment.CreatedBy = actor;
                                        reversalPayment.ModifiedBy = actor;
                                        _dbContext.CustomerPayments.Add(reversalPayment);
                                    }
                                }
                            }
                        }
                    }

                    salesReturn.Reject(request?.Reason ?? string.Empty);
                    salesReturn.ModifiedBy = actor;

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            if (salesReturn == null)
                return NotFound();

            if (!string.IsNullOrWhiteSpace(validationError))
                return BadRequest(validationError);

            return Ok(MapToResponse(salesReturn));
        }

        private static SalesReturnResponse MapToResponse(SalesReturn entity)
        {
            return new SalesReturnResponse
            {
                Id = entity.Id,
                ReturnNumber = entity.ReturnNumber,
                SalesOrderId = entity.SalesOrderId,
                SalesOrderNumber = entity.SalesOrder?.SONumber,
                WarehouseId = entity.WarehouseId,
                Reason = entity.Reason,
                Status = entity.Status,
                TotalRefundAmount = entity.RefundAmount,
                RefundType = entity.RefundType,
                Notes = entity.Notes,
                CreatedAt = entity.CreatedDate,
                Lines = entity.LineItems.Select(line => new SalesReturnLineResponse
                {
                    Id = line.Id,
                    PartId = line.PartId,
                    PartName = line.Part?.Name ?? string.Empty,
                    PartSku = line.Part?.SKU ?? string.Empty,
                    Quantity = line.Quantity,
                    QuantityInBaseUnit = line.QuantityInBaseUnit,
                    UnitPrice = line.UnitPrice,
                    UnitPriceInBaseUnit = line.UnitPriceInBaseUnit,
                    RefundAmount = line.RefundAmount,
                    UnitId = line.UnitId,
                    UnitName = line.Unit?.Name,
                    UnitSymbol = line.Unit?.Symbol,
                    Condition = line.Condition,
                    Notes = line.Notes
                }).ToList()
            };
        }

    }
}
