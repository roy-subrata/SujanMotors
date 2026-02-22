using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AutoPartShop.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesReturnController : ControllerBase
    {
        private readonly ISalesReturnRepository _salesReturnRepository;
        private readonly ISalesOrderRepository _salesOrderRepository;
        private readonly ICustomerPaymentRepository _customerPaymentRepository;
        private readonly ICurrentUserService _currentUserService;
        private readonly ICodeGenerateService _codeGenerateService;
        private readonly AutoPartDbContext _dbContext;

        public SalesReturnController(
            ISalesReturnRepository salesReturnRepository,
            ISalesOrderRepository salesOrderRepository,
            ICustomerPaymentRepository customerPaymentRepository,
            ICurrentUserService currentUserService,
            ICodeGenerateService codeGenerateService,
            AutoPartDbContext dbContext)
        {
            _salesReturnRepository = salesReturnRepository;
            _salesOrderRepository = salesOrderRepository;
            _customerPaymentRepository = customerPaymentRepository;
            _currentUserService = currentUserService;
            _codeGenerateService = codeGenerateService;
            _dbContext = dbContext;
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

            var returnNumber = await _codeGenerateService.GenerateAsync("SR");
            var salesReturn = SalesReturn.Create(returnNumber, request.SalesOrderId, null, request.Reason, request.WarehouseId, DateTime.UtcNow, request.Notes);
            salesReturn.SetRefundType(request.RefundType);

            foreach (var line in request.Lines)
            {
                var returnLine = SalesReturnLine.Create(
                    salesReturn.Id,
                    line.SalesOrderLineId,
                    line.PartId,
                    line.Quantity,
                    line.UnitPrice,
                    line.Condition
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

            var (items, totalCount) = await _salesReturnRepository.GetPagedAsync(pageNumber, pageSize);
            var response = items.Select(MapToResponse);
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }

        [HttpPatch("{id}/approve")]
        public async Task<ActionResult<SalesReturnResponse>> Approve(Guid id)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "PENDING")
                return BadRequest("Only pending returns can be approved.");
            // TODO: Replace with actual user
            salesReturn.Approve("system");
            await _salesReturnRepository.UpdateAsync(salesReturn);
            return Ok(MapToResponse(salesReturn));
        }

        [HttpPatch("{id}/receive")]
        public async Task<ActionResult<SalesReturnResponse>> Receive(Guid id)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "APPROVED")
                return BadRequest("Only approved returns can be marked as received.");
            salesReturn.MarkAsReceived();
            await _salesReturnRepository.UpdateAsync(salesReturn);
            return Ok(MapToResponse(salesReturn));
        }

        [HttpPatch("{id}/process")]
        public async Task<ActionResult<SalesReturnResponse>> Process(Guid id)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "RECEIVED")
                return BadRequest("Only received returns can be processed.");

            // --- Stock Adjustment Logic ---
            var warehouseId = salesReturn.WarehouseId;
            if (warehouseId == Guid.Empty)
                return BadRequest("WarehouseId is required for stock adjustment.");

            var salesOrder = await _salesOrderRepository.GetByIdAsync(salesReturn.SalesOrderId);

            foreach (var line in salesReturn.LineItems)
            {
                var stockLevel = _dbContext.StockLevels.FirstOrDefault(sl => sl.PartId == line.PartId && sl.WarehouseId == warehouseId);
                if (stockLevel == null)
                {
                    stockLevel = Domain.Entities.StockLevel.Create(line.PartId, warehouseId);
                    stockLevel.CreatedBy = _currentUserService.GetCurrentUsername();
                    _dbContext.StockLevels.Add(stockLevel);
                }
                stockLevel.AddStock(line.Quantity, $"Sales Return {salesReturn.ReturnNumber}");
                stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();

                // Create stock movement record - RETURN type for customer returns
                var stockMovement = Domain.Entities.StockMovement.Create(
                    stockLevel.Id,
                    "RETURN",  // RETURN type to distinguish from regular incoming stock
                    line.Quantity,
                    $"Customer Return - {salesReturn.Reason}",
                    salesReturn.ReturnNumber
                );
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
                        // Add to existing lot
                        existingLot.AddStock(line.Quantity, $"Sales Return {salesReturn.ReturnNumber}");
                        existingLot.ModifiedBy = _currentUserService.GetCurrentUsername();
                        targetLot = existingLot;
                    }
                    else
                    {
                        // Create new lot for returned items
                        var lotNumber = $"RET-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8]}";
                        // For returns, we use default/empty GUIDs for supplier and goods receipt since they're not from procurement
                        targetLot = Domain.Entities.StockLot.Create(
                            lotNumber,
                            line.PartId,
                            warehouseId,
                            Guid.Empty,  // No supplier for returns
                            Guid.Empty,  // No goods receipt line for returns
                            line.Quantity,
                            line.UnitPrice,
                            DateTime.UtcNow,
                            $"Return {salesReturn.ReturnNumber}",
                            null,  // No expiry date
                            "USD",
                            $"Created from sales return {salesReturn.ReturnNumber}"
                        );
                        targetLot.CreatedBy = _currentUserService.GetCurrentUsername();
                        targetLot.ModifiedBy = _currentUserService.GetCurrentUsername();
                        _dbContext.StockLots.Add(targetLot);
                        await _dbContext.SaveChangesAsync(); // Save to get the lot ID
                    }

                    // Create lot movement
                    var lotMovement = Domain.Entities.StockLotMovement.Create(
                        targetLot.Id,
                        line.Quantity,
                        "RETURN",
                        salesReturn.Id,
                        "SalesReturn",
                        null,
                        line.UnitPrice,
                        $"Sales Return {salesReturn.ReturnNumber}"
                    );
                    lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                    lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                    _dbContext.StockLotMovements.Add(lotMovement);
                }
                catch (Exception)
                {
                    // Continue execution - lot tracking is optional
                }
            }
            await _dbContext.SaveChangesAsync();

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
                        await _dbContext.SaveChangesAsync();

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
                    }
                    else if (salesReturn.RefundType == "STORE_CREDIT")
                    {
                        // STORE CREDIT: Create available credit for future purchases
                        // Do NOT reduce current balance - this becomes available credit

                        // Create CustomerPayment with COMPLETED status and NO invoice link
                        // This makes it show up in AccountBalance as available credit
                        var creditPayment = CustomerPayment.Create(
                            customerId: salesOrder.CustomerId,
                            paymentProviderId: null,
                            amount: salesReturn.RefundAmount,
                            paymentMethod: "STORE_CREDIT",
                            transactionNumber: $"CREDIT-{salesReturn.ReturnNumber}",
                            referenceNumber: salesReturn.ReturnNumber,
                            paymentDate: DateTime.UtcNow
                        );

                        // DO NOT link to invoice - unlinked completed payments = available credit
                        creditPayment.MarkAsCompleted();
                        creditPayment.CreatedBy = _currentUserService.GetCurrentUsername();
                        creditPayment.ModifiedBy = _currentUserService.GetCurrentUsername();
                        creditPayment.UpdateNotes($"Store credit for sales return {salesReturn.ReturnNumber}. Reason: {salesReturn.Reason}. Can be applied to future purchases.");

                        await _customerPaymentRepository.AddAsync(creditPayment, CancellationToken.None);
                    }
                }
            }

            salesReturn.Process();
            await _salesReturnRepository.UpdateAsync(salesReturn);
            return Ok(MapToResponse(salesReturn));
        }

        public class RejectRequest { public string? Reason { get; set; } }

        [HttpPatch("{id}/reject")]
        public async Task<ActionResult<SalesReturnResponse>> Reject(Guid id, [FromBody] RejectRequest request)
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id);
            if (salesReturn == null)
                return NotFound();
            if (salesReturn.Status != "PENDING" && salesReturn.Status != "APPROVED")
                return BadRequest("Only pending or approved returns can be rejected.");

            salesReturn.Reject(request?.Reason ?? string.Empty);
            await _salesReturnRepository.UpdateAsync(salesReturn);
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
                    UnitPrice = line.UnitPrice,
                    RefundAmount = line.RefundAmount,
                    Condition = line.Condition,
                    Notes = line.Notes
                }).ToList()
            };
        }

    }
}
