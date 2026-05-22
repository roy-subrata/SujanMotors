using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.Interfaces;
using AutoPartShop.Domain.Events;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Application.SaleOrders;
using AutoPartShop.Application.SaleOrders.Dtos;
using AutoPartShop.Application.Services;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ISaleOrderReadRepository _saleOrderReadRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _partRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly ICustomerPaymentRepository _customerPaymentRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly IWarrantyService _warrantyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPricingValidationService _pricingValidationService;
    private readonly INotificationService _notificationService;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<SalesOrderController> _logger;
    private readonly AutoPartDbContext _dbContext;

    public SalesOrderController(
        ISalesOrderRepository salesOrderRepository,
        ISaleOrderReadRepository saleOrderReadRepository,
        IInvoiceRepository invoiceRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository,
        IStockLevelRepository stockLevelRepository,
        ICustomerPaymentRepository customerPaymentRepository,
        ICodeGenerateService codeGenerateService,
        IUnitConversionService unitConversionService,
        IWarrantyService warrantyService,
        ICurrentUserService currentUserService,
        IPricingValidationService pricingValidationService,
        INotificationService notificationService,
        IDomainEventDispatcher eventDispatcher,
        ILogger<SalesOrderController> logger,
        AutoPartDbContext dbContext)
    {
        _salesOrderRepository = salesOrderRepository;
        _saleOrderReadRepository = saleOrderReadRepository;
        _invoiceRepository = invoiceRepository;
        _customerRepository = customerRepository;
        _partRepository = partRepository;
        _stockLevelRepository = stockLevelRepository;
        _customerPaymentRepository = customerPaymentRepository;
        _codeGenerateService = codeGenerateService;
        _unitConversionService = unitConversionService;
        _warrantyService = warrantyService;
        _currentUserService = currentUserService;
        _pricingValidationService = pricingValidationService;
        _notificationService = notificationService;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _salesOrderRepository.GetAllAsync(cancellationToken);
            var response = orders.Select(MapToSalesOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all sales orders");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving sales orders");
        }
    }

    [HttpPost("list")]
    public async Task<IActionResult> GetList(SaleOrderQuery query, CancellationToken cancellationToken = default)
    {
        if (query is null)
            return BadRequest("Request body is required.");

        if (query.PageNumber < 1)
            return BadRequest("PageNumber must be greater than 0.");

        if (query.PageSize < 1)
            return BadRequest("PageSize must be greater than 0.");

        try
        {
            var (saleOrders, totalCount) =
                await _saleOrderReadRepository.FindAllQuery(query, cancellationToken);

            var result = PagedResult<SaleOrderResponse>
                .Create(saleOrders, totalCount, query);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales orders list");
            return StatusCode(500, "An error occurred while retrieving sales orders");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            return Ok(MapToSalesOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order by ID: {SOId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the sales order");
        }
    }

    [HttpGet("number/{soNumber}")]
    public async Task<IActionResult> GetByNumber(string soNumber, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByNumberAsync(soNumber, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            return Ok(MapToSalesOrderResponse(order));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales order by number: {SONumber}", soNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the sales order");
        }
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _salesOrderRepository.GetByCustomerAsync(customerId, cancellationToken);
            var response = orders.Select(MapToSalesOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales orders by customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving sales orders");
        }
    }

    [HttpGet("status/{status}")]
    public async Task<IActionResult> GetByStatus(string status, CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _salesOrderRepository.GetByStatusAsync(status, cancellationToken);
            var response = orders.Select(MapToSalesOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales orders by status: {Status}", status);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving sales orders");
        }
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdue(CancellationToken cancellationToken)
    {
        try
        {
            var orders = await _salesOrderRepository.GetOverdueAsync(cancellationToken);
            var response = orders.Select(MapToSalesOrderResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting overdue sales orders");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving overdue sales orders");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.CustomerId == Guid.Empty || request.WarehouseId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerName) || request.DeliveryDate == default)
                return BadRequest(new { message = "CustomerId, WarehouseId, CustomerName and DeliveryDate are required" });

            // Fix #4: use code service to guarantee unique SO numbers
            var soNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);

            var order = SalesOrder.Create(
                soNumber,
                request.CustomerId,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                request.WarehouseId,
                request.TechnicianId,
                request.TechnicianName,
                request.CustomerCity,
                request.Notes,
                request.Currency,
                request.Channel
            );

            // Add lines
            int lineNumber = 1;
            foreach (var lineRequest in request.Lines)
            {
                var line = await BuildSalesOrderLineAsync(order, lineRequest, lineNumber, cancellationToken);
                order.LineItems.Add(line);
                lineNumber++;
            }

            order.UpdateDeliveryDate(request.DeliveryDate);
            order.SetDiscountPercentage(request.Discount);
            order.CalculateTotal();

            order.CreatedBy = _currentUserService.GetCurrentUsername();
            order.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _codeGenerateService.SaveGenerateCodeAsync("SO", cancellationToken);
            await _salesOrderRepository.AddAsync(order, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = order.Id }, MapToSalesOrderResponse(order));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales order");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the sales order");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, CreateSalesOrderRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            if (order.Status != "DRAFT")
                return BadRequest(new { message = "Only draft sales orders can be edited" });

            if (request.CustomerId == Guid.Empty || request.WarehouseId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerName) || request.DeliveryDate == default)
                return BadRequest(new { message = "CustomerId, WarehouseId, CustomerName and DeliveryDate are required" });

            if (request.Lines == null || request.Lines.Count == 0)
                return BadRequest(new { message = "Sales order must have at least one line item" });

            var executionStrategy = _dbContext.Database.CreateExecutionStrategy();
            SaleOrderResponse? response = null;

            await executionStrategy.ExecuteAsync(async () =>
            {
                var newLines = new List<SalesOrderLine>();
                int lineNumber = 1;
                foreach (var lineRequest in request.Lines)
                {
                    var line = await BuildSalesOrderLineAsync(order, lineRequest, lineNumber, cancellationToken);
                    newLines.Add(line);
                    lineNumber++;
                }

                // Delegate discount/total calculation to the domain so clamping and rounding stay consistent
                order.ClearLineItems();
                foreach (var l in newLines) order.LineItems.Add(l);
                order.SetDiscountPercentage(request.Discount);
                order.CalculateTotal();

                var subtotal = order.SubTotal;
                var discountPercentage = order.DiscountPercentage;
                var discountAmount = order.DiscountAmount;
                var totalAmount = order.TotalAmount;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var updated = await _dbContext.Set<SalesOrder>()
                    .Where(so => so.Id == order.Id && !so.Isdeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(so => so.CustomerId, request.CustomerId)
                        .SetProperty(so => so.CustomerName, request.CustomerName)
                        .SetProperty(so => so.CustomerEmail, request.CustomerEmail)
                        .SetProperty(so => so.CustomerPhone, request.CustomerPhone)
                        .SetProperty(so => so.WarehouseId, request.WarehouseId)
                        .SetProperty(so => so.DeliveryAddress, request.CustomerCity)
                        .SetProperty(so => so.TechnicianId, request.TechnicianId)
                        .SetProperty(so => so.TechnicianName, request.TechnicianName)
                        .SetProperty(so => so.DeliveryDate, request.DeliveryDate)
                        .SetProperty(so => so.Notes, request.Notes)
                        .SetProperty(so => so.Currency, request.Currency)
                        .SetProperty(so => so.SubTotal, subtotal)
                        .SetProperty(so => so.DiscountPercentage, discountPercentage)
                        .SetProperty(so => so.DiscountAmount, discountAmount)
                        .SetProperty(so => so.TotalAmount, totalAmount)
                        .SetProperty(so => so.TaxAmount, order.TaxAmount)
                        .SetProperty(so => so.ModifiedBy, _currentUserService.GetCurrentUsername())
                        .SetProperty(so => so.ModifiedDate, DateTime.UtcNow),
                        cancellationToken);

                if (updated == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw new DbUpdateConcurrencyException("Sales order update affected 0 rows.");
                }

                await _dbContext.Set<SalesOrderLine>()
                    .Where(l => l.SalesOrderId == order.Id)
                    .ExecuteDeleteAsync(cancellationToken);

                await _dbContext.Set<SalesOrderLine>().AddRangeAsync(newLines, cancellationToken);
                await _dbContext.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                var updatedOrder = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
                response = MapToSalesOrderResponse(updatedOrder!);
            });

            return Ok(response!);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "Sales order was modified or deleted by another user. Please reload and try again." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sales order: {SOId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the sales order");
        }
    }

    [HttpPatch("{id:guid}/confirm")]
    public async Task<IActionResult> Confirm(Guid id, [FromBody] ConfirmSalesOrderRequest? confirmRequest, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            var warehouseId = order.WarehouseId ?? Guid.Empty;
            if (warehouseId == Guid.Empty)
                return BadRequest(new { message = "Warehouse is required for stock deduction" });

            var confirmStrategy = _dbContext.Database.CreateExecutionStrategy();
            await confirmStrategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Check stock — throw so the transaction rolls back on insufficient stock
                    foreach (var line in order.LineItems)
                    {
                        var stockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(line.PartId, warehouseId, cancellationToken);
                        if (stockLevel == null || stockLevel.QuantityAvailableInBaseUnit < line.QuantityInBaseUnit)
                        {
                            var part = await _partRepository.GetByIdAsync(line.PartId, cancellationToken);
                            throw new InvalidOperationException($"Insufficient stock for part: {part?.Name ?? line.PartId.ToString()}. Required: {line.QuantityInBaseUnit}, Available: {stockLevel?.QuantityAvailableInBaseUnit ?? 0}");
                        }
                    }

                    // Deduct stock and create movement history
                    foreach (var line in order.LineItems)
                    {
                        var stockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(line.PartId, warehouseId, cancellationToken);
                        if (stockLevel != null)
                        {
                            stockLevel.RemoveStock(line.QuantityInBaseUnit, line.QuantityInBaseUnit, $"Sales Order {order.SONumber}");
                            stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                            var stockMovement = StockMovement.Create(
                                stockLevel.Id, "OUT", line.QuantityInBaseUnit,
                                $"Sales Order {order.SONumber}", order.SONumber,
                                unitId: stockLevel.UnitId, quantityInBaseUnit: line.QuantityInBaseUnit);
                            stockMovement.Approve(_currentUserService.GetCurrentUsername());
                            stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                            stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

                            // Deduct from lots — FIFO or manual selection
                            // StockLotMovement records (ReferenceId = line.Id) serve as the audit trail
                            var manualAlloc = confirmRequest?.ManualAllocations
                                ?.FirstOrDefault(a => a.SalesOrderLineId == line.Id);

                            if (manualAlloc != null)
                            {
                                // Manual lot selection — validate then deduct specified lots
                                int totalManual = manualAlloc.Lots.Sum(l => l.Quantity);
                                if (totalManual != line.QuantityInBaseUnit)
                                    throw new InvalidOperationException(
                                        $"Manual lot quantities ({totalManual}) do not match order line quantity ({line.QuantityInBaseUnit}) for line {line.Id}");

                                foreach (var alloc in manualAlloc.Lots)
                                {
                                    var lot = await _dbContext.StockLots
                                        .FirstOrDefaultAsync(sl =>
                                            sl.Id == alloc.StockLotId &&
                                            sl.WarehouseId == warehouseId &&
                                            !sl.Isdeleted, cancellationToken);

                                    if (lot == null)
                                        throw new InvalidOperationException($"Stock lot {alloc.StockLotId} not found in warehouse");

                                    if (lot.QuantityAvailableInBaseUnit < alloc.Quantity)
                                        throw new InvalidOperationException(
                                            $"Lot {lot.LotNumber} has only {lot.QuantityAvailableInBaseUnit} available, requested {alloc.Quantity}");

                                    lot.RemoveStock(alloc.Quantity, alloc.Quantity, $"Sales Order {order.SONumber}");
                                    lot.ModifiedBy = _currentUserService.GetCurrentUsername();

                                    var lotMovement = StockLotMovement.Create(
                                        lot.Id, alloc.Quantity, "SALE", line.Id, "SalesOrderLine",
                                        null, lot.CostPrice, $"Sales Order {order.SONumber}");
                                    lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                                    lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                                    await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);
                                }
                            }
                            else
                            {
                                // FIFO — oldest lot first (expiry date, then receipt date)
                                var stockLots = await _dbContext.StockLots
                                    .Where(sl => sl.PartId == line.PartId &&
                                                sl.WarehouseId == warehouseId &&
                                                sl.QuantityAvailableInBaseUnit > 0 &&
                                                !sl.Isdeleted)
                                    .OrderBy(sl => sl.ExpiryDate)
                                    .ThenBy(sl => sl.CreatedDate)
                                    .ToListAsync(cancellationToken);

                                int remainingBaseQty = line.QuantityInBaseUnit;
                                foreach (var lot in stockLots)
                                {
                                    if (remainingBaseQty <= 0) break;
                                    int qtyToDeduct = Math.Min(lot.QuantityAvailableInBaseUnit, remainingBaseQty);
                                    lot.RemoveStock(qtyToDeduct, qtyToDeduct, $"Sales Order {order.SONumber}");
                                    lot.ModifiedBy = _currentUserService.GetCurrentUsername();

                                    var lotMovement = StockLotMovement.Create(
                                        lot.Id, qtyToDeduct, "SALE", line.Id, "SalesOrderLine",
                                        null, lot.CostPrice, $"Sales Order {order.SONumber}");
                                    lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                                    lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                                    await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);

                                    remainingBaseQty -= qtyToDeduct;
                                }
                            }
                        }
                    }

                    order.Confirm();
                    order.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Atomic status transition — WHERE Status = 'DRAFT' prevents double-confirm:
                    // if a concurrent request already confirmed this order, rowsUpdated = 0 and we throw,
                    // rolling back all stock deductions made above.
                    var rowsUpdated = await _dbContext.Set<SalesOrder>()
                        .Where(so => so.Id == order.Id && so.Status == "DRAFT" && !so.Isdeleted)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(so => so.Status, order.Status)
                            .SetProperty(so => so.ConfirmedDate, order.ConfirmedDate)
                            .SetProperty(so => so.ModifiedBy, order.ModifiedBy)
                            .SetProperty(so => so.ModifiedDate, DateTime.UtcNow),
                            cancellationToken);

                    if (rowsUpdated == 0)
                        throw new InvalidOperationException("Sales order has already been confirmed or no longer exists.");

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch
                {
                    await tx.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            // Auto-create warranty registrations for parts with warranty
            try
            {
                foreach (var line in order.LineItems)
                {
                    var part = await _partRepository.GetByIdAsync(line.PartId, cancellationToken);
                    // Check part-level warranty as a quick gate; actual lot-level check is inside the service
                    if (part != null && (part.HasWarranty || true))
                    {
                        try
                        {
                            await _warrantyService.CreateWarrantyForSalesOrderLineAsync(
                                line,
                                order.Id,
                                order.CustomerId,
                                order.SODate,
                                warehouseId,
                                cancellationToken);

                            _logger.LogInformation("Created warranty registration for part {PartName} in order {SONumber}",
                                part.Name, order.SONumber);
                        }
                        catch (InvalidOperationException)
                        {
                            // Part/lot has no warranty — silently skip
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create warranty registrations for SO {SOId}. Warranties can be created manually.", id);
            }

            // Dispatch domain events raised by order.Confirm().
            // Reading directly from the entity after CommitAsync guarantees
            // a failed commit never triggers external side-effects (SMS, SignalR, etc.).
            var confirmedEvents = order.DomainEvents.ToList();
            order.ClearEvents();
            await _eventDispatcher.DispatchAsync(confirmedEvents, cancellationToken);

            return Ok(MapToSalesOrderResponse(order));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming sales order: {SOId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while confirming the sales order");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            // Fix #6: block deletion of confirmed/paid orders to protect stock and financial records
            if (order.Status != "DRAFT")
                return BadRequest(new
                {
                    message = $"Cannot delete sales order {order.SONumber} with status '{order.Status}'. Only DRAFT orders can be deleted."
                });

            if (order.PaidAmount > 0)
                return BadRequest(new
                {
                    message = $"Cannot delete sales order {order.SONumber}: payments of {order.PaidAmount} {order.Currency} have been recorded."
                });

            await _salesOrderRepository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sales order: {SOId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the sales order");
        }
    }

    // Invoice endpoints
    [HttpGet("invoices")]
    public async Task<IActionResult> GetAllInvoices(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        Guid? customerId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (invoices, totalCount) = await _invoiceRepository.GetPagedAsync(pageNumber, pageSize,
                searchTerm, status, customerId, fromDate, toDate, cancellationToken);

            var response = invoices.Select(MapToInvoiceResponse);
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving invoices");
        }
    }

    [HttpGet("invoices/customer/{customerId:guid}")]
    public async Task<IActionResult> GetInvoicesByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var allInvoices = await _dbContext.Invoices
                .Include(i => i.SalesOrder)
                .Include(i => i.CustomerPayments)
                .Where(i => !i.Isdeleted &&
                            i.SalesOrder != null &&
                            i.SalesOrder.CustomerId == customerId &&
                            !i.SalesOrder.Isdeleted)
                .OrderByDescending(i => i.InvoiceDate)
                .ToListAsync(cancellationToken);

            var response = allInvoices.Select(MapToInvoiceResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices for customer: {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customer invoices");
        }
    }

    [HttpPost("invoices")]
    public async Task<IActionResult> CreateInvoice(CreateInvoiceRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.SalesOrderId == Guid.Empty || request.SubTotal <= 0)
                return BadRequest(new { message = "SalesOrderId and SubTotal are required" });

            // Fix #3: use code service to guarantee unique invoice numbers
            var invoiceNumber = await _codeGenerateService.GenerateAsync("INV", cancellationToken);

            var invoice = Invoice.Create(
                invoiceNumber,
                request.SalesOrderId,
                request.SubTotal,
                request.TaxAmount,
                request.DueDate,
                request.Notes
            );
            invoice.CreatedBy = _currentUserService.GetCurrentUsername();
            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _invoiceRepository.AddAsync(invoice, cancellationToken);
            await _codeGenerateService.SaveGenerateCodeAsync("INV", cancellationToken);

            return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.Id }, MapToInvoiceResponse(invoice));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating invoice");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the invoice");
        }
    }

    [HttpGet("invoices/{id:guid}")]
    public async Task<IActionResult> GetInvoiceById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
            if (invoice is null) return NotFound(new { message = "Invoice not found" });

            return Ok(MapToInvoiceResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice by ID: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the invoice");
        }
    }

    [HttpGet("invoices/number/{invoiceNumber}")]
    public async Task<IActionResult> GetInvoiceByNumber(string invoiceNumber, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceRepository.GetByNumberAsync(invoiceNumber, cancellationToken);
            if (invoice is null) return NotFound(new { message = "Invoice not found" });

            return Ok(MapToInvoiceResponse(invoice));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoice by number: {InvoiceNumber}", invoiceNumber);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the invoice");
        }
    }

    [HttpPatch("invoices/{id:guid}/issue")]
    public async Task<IActionResult> IssueInvoice(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, cancellationToken);
            if (invoice is null) return NotFound(new { message = "Invoice not found" });

            var salesOrder = await _salesOrderRepository.GetByIdAsync(invoice.SalesOrderId, cancellationToken);
            if (salesOrder is null) return NotFound(new { message = "Sales order not found" });

            var customer = await _customerRepository.GetByIdAsync(salesOrder.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            // Fix #2: invoice status + customer balance update must be atomic
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                invoice.Issue();
                invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                customer.UpdateBalance(invoice.GrandTotal);
                customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                await _customerRepository.UpdateAsync(customer, cancellationToken);
                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            return Ok(MapToInvoiceResponse(invoice));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error issuing invoice: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while issuing the invoice");
        }
    }

    [HttpPatch("invoices/{id:guid}/payment")]
    public async Task<IActionResult> RecordPayment(Guid id, [FromBody] RecordPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
                return BadRequest(new { message = "Payment amount must be greater than 0" });

            var invoice = await _dbContext.Invoices
                .Include(i => i.SalesOrder)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (invoice is null) return NotFound(new { message = "Invoice not found" });
            if (invoice.SalesOrder is null) return BadRequest(new { message = "Sales order not found for invoice" });

            var customer = await _customerRepository.GetByIdAsync(invoice.SalesOrder.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            // Fix #5: use a unique transaction number (claim number + GUID suffix avoids timestamp collision)
            var transactionNumber = await _codeGenerateService.GenerateAsync("TXN", cancellationToken);

            // Fix #8: default payment date to UtcNow when not provided
            var effectivePaymentDate = request.PaymentDate ?? DateTime.UtcNow;

            var payment = CustomerPayment.Create(
                customerId: invoice.SalesOrder.CustomerId,
                paymentProviderId: request.PaymentProviderId,
                amount: request.Amount,
                paymentMethod: request.PaymentMethod ?? "CASH",
                transactionNumber: transactionNumber,
                referenceNumber: request.ReferenceNumber ?? "",
                paymentDate: effectivePaymentDate
            );

            payment.LinkToInvoice(invoice.Id);
            payment.CreatedBy = _currentUserService.GetCurrentUsername();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            var isCash = request.PaymentMethod?.Trim().ToUpper() == "CASH";

            // Fix #1: wrap all saves in a single transaction
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                if (isCash)
                {
                    payment.MarkAsCompleted();
                    customer.UpdateBalance(-request.Amount);
                    customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                    await _customerPaymentRepository.AddAsync(payment, cancellationToken);
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    // Reload inside transaction to get accurate AmountPaid from all payments
                    var freshInvoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .Include(i => i.SalesOrder)
                        .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

                    freshInvoice!.UpdatePaymentStatus();
                    freshInvoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);

                    var salesOrder = await _salesOrderRepository.GetByIdAsync(freshInvoice.SalesOrderId, cancellationToken);
                    if (salesOrder != null)
                    {
                        salesOrder.RecordPayment(request.Amount);
                        salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                    }

                    invoice = freshInvoice;
                }
                else
                {
                    // Non-CASH: keep as PENDING; balance updates on manual confirmation
                    await _customerPaymentRepository.AddAsync(payment, cancellationToken);

                    invoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .Include(i => i.SalesOrder)
                        .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }

            await _codeGenerateService.SaveGenerateCodeAsync("TXN", cancellationToken);

            return Ok(new
            {
                PaymentId = payment.Id,
                PaymentStatus = payment.Status,
                PaymentMethod = payment.PaymentMethod,
                InvoiceStatus = invoice!.Status,
                AmountPaid = invoice.AmountPaid,
                OutstandingAmount = invoice.OutstandingAmount,
                CustomerBalance = customer.CurrentBalance,
                Message = payment.Status == "COMPLETED"
                    ? "Payment completed and customer balance updated"
                    : "Payment created as PENDING. Mark as completed to update customer balance.",
                Invoice = MapToInvoiceResponse(invoice)
            });
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
            _logger.LogError(ex, "Error recording payment: {InvoiceId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while recording the payment", error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private SaleOrderResponse MapToSalesOrderResponse(SalesOrder order)
    {
        return new SaleOrderResponse
        {
            Id = order.Id,
            SONumber = order.SONumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerCity = order.DeliveryAddress,
            WarehouseId = order.WarehouseId,
            TechnicianId = order.TechnicianId,
            TechnicianName = order.TechnicianName,
            OrderDate = order.SODate,
            DeliveryDate = order.DeliveryDate ?? DateTime.MinValue,
            Status = order.Status,
            Channel = order.Channel,
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            Discount = order.DiscountPercentage,
            GrandTotal = order.GrandTotal,
            Currency = order.Currency,
            AmountPaid = order.PaidAmount,
            OutstandingAmount = order.GrandTotal - order.PaidAmount,
            IsOverdue = order.DeliveryDate.HasValue && DateTime.UtcNow > order.DeliveryDate.Value && order.Status != "DELIVERED" && order.Status != "CANCELLED",
            Notes = order.Notes,
            PaidDate = order.PaidDate,
            PackedDate = order.PackedDate,
            CompletedDate = order.CompletedDate,
            Lines = order.LineItems.Select(l => new SalesOrderLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                PartName = l.Part?.Name ?? string.Empty,
                PartSku = l.Part?.SKU ?? string.Empty,
                ProductVariantId = l.ProductVariantId,
                VariantName = l.ProductVariant?.Name,
                VariantCode = l.ProductVariant?.Code,
                VariantSku = l.ProductVariant?.SKU,
                DisplayName = l.ProductVariant != null
                    ? (l.Part != null ? l.Part.Name + " - " + l.ProductVariant.Name : l.ProductVariant.Name)
                    : (l.Part?.Name ?? string.Empty),
                UnitId = l.UnitId,
                UnitName = l.Unit?.Name ?? string.Empty,
                UnitSymbol = l.Unit?.Symbol ?? string.Empty,
                Quantity = l.Quantity,
                QuantityInBaseUnit = l.QuantityInBaseUnit,
                ShippedQuantity = l.ShippedQuantity,
                ShippedQuantityInBaseUnit = l.ShippedQuantityInBaseUnit,
                UnitPrice = l.UnitPrice,
                Discount = l.UnitPrice == 0 ? 0 : Math.Round((l.Discount / l.UnitPrice) * 100, 2),
                LineTotal = l.TotalPrice
            }).ToList(),
            CreatedAt = order.CreatedDate
        };
    }

    private async Task<SalesOrderLine> BuildSalesOrderLineAsync(
        SalesOrder order,
        CreateSalesOrderLineRequest lineRequest,
        int lineNumber,
        CancellationToken cancellationToken)
    {
        var part = await _dbContext.Parts
            .FirstOrDefaultAsync(p => p.Id == lineRequest.PartId && !p.Isdeleted, cancellationToken);

        if (part == null)
            throw new ArgumentException($"Part with ID {lineRequest.PartId} not found");

        // Validate variant belongs to the same part when provided
        ProductVariant? variant = null;
        if (lineRequest.ProductVariantId.HasValue)
        {
            variant = await _dbContext.Set<ProductVariant>()
                .FirstOrDefaultAsync(v => v.Id == lineRequest.ProductVariantId.Value
                                       && v.PartId == lineRequest.PartId
                                       && v.IsActive, cancellationToken);
            if (variant == null)
                throw new ArgumentException($"Variant {lineRequest.ProductVariantId} not found for part '{part.Name}'");
        }

        // Price resolution: manual entry → variant price → product base price → error
        var unitPrice = lineRequest.UnitPrice > 0
            ? lineRequest.UnitPrice
            : (variant?.SellingPrice > 0 ? variant.SellingPrice : part.SellingPrice);

        if (unitPrice <= 0)
            throw new ArgumentException($"No selling price set for '{part.Name}'. Please set a selling price on the product or variant.");

        var (quantityInBaseUnit, unitId, baseUnitPrice) = await ResolveUnitPricingAsync(
            part,
            lineRequest.Quantity,
            lineRequest.UnitId,
            unitPrice,
            cancellationToken);

        _pricingValidationService.ValidateLinePricing(part, baseUnitPrice, lineRequest.Discount);

        var discountPerUnit = unitPrice * (lineRequest.Discount / 100);

        return SalesOrderLine.Create(
            order.Id,
            lineRequest.PartId,
            lineRequest.Quantity,
            unitPrice,
            lineNumber,
            unitId: unitId,
            quantityInBaseUnit: quantityInBaseUnit,
            discount: discountPerUnit,
            productVariantId: lineRequest.ProductVariantId
        );
    }

    private async Task<(int quantityInBaseUnit, Guid? unitId, decimal baseUnitPrice)> ResolveUnitPricingAsync(
        Part part,
        int quantity,
        Guid? unitId,
        decimal unitPrice,
        CancellationToken cancellationToken)
    {
        if (part.UnitId is null)
        {
            return (quantity, unitId, unitPrice);
        }

        if (!unitId.HasValue)
        {
            return (quantity, part.UnitId, unitPrice);
        }

        if (unitId.Value == part.UnitId.Value)
        {
            return (quantity, unitId, unitPrice);
        }

        try
        {
            var conversionFactor = await _unitConversionService.GetConversionFactorAsync(unitId.Value, part.UnitId.Value);
            if (conversionFactor <= 0)
                throw new InvalidOperationException("Invalid unit conversion factor.");

            var quantityInBaseUnit = (int)Math.Round(quantity * conversionFactor, MidpointRounding.AwayFromZero);
            var baseUnitPrice = unitPrice / conversionFactor;
            return (quantityInBaseUnit, unitId, baseUnitPrice);
        }
        catch (InvalidOperationException ex)
        {
            throw new ArgumentException($"Unit conversion not configured: {ex.Message}");
        }
    }

    private InvoiceResponse MapToInvoiceResponse(Invoice invoice)
    {
        return new InvoiceResponse
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            SalesOrderId = invoice.SalesOrderId,
            SalesOrderNumber = invoice.SalesOrder?.SONumber ?? string.Empty,
            CustomerId = invoice.SalesOrder?.CustomerId ?? Guid.Empty,
            CustomerName = invoice.SalesOrder?.CustomerName ?? string.Empty,
            CustomerPhone = invoice.SalesOrder?.CustomerPhone ?? string.Empty,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            SubTotal = invoice.SubTotal,
            TaxAmount = invoice.TaxAmount,
            DiscountAmount = invoice.DiscountAmount,
            GrandTotal = invoice.GrandTotal,
            AmountPaid = invoice.AmountPaid,
            OutstandingAmount = invoice.OutstandingAmount,
            Status = invoice.Status,
            IsOverdue = invoice.IsOverdue,
            Notes = invoice.Notes,
            Payments = (invoice.CustomerPayments ?? new List<CustomerPayment>()).Select(p => new InvoicePaymentResponse
            {
                Id = p.Id,
                InvoiceId = p.InvoiceId ?? Guid.Empty,
                Amount = p.Amount,
                PaymentDate = p.PaymentDate,
                PaymentMethod = p.PaymentMethod,
                ReferenceNumber = p.ReferenceNumber
            }).ToList(),
            CreatedAt = invoice.CreatedDate
        };
    }

    [HttpPost("quick-sale")]
    public async Task<IActionResult> CreateQuickSale(QuickSaleRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating quick sale for customer: {CustomerName}", request.CustomerName);

            // 1. Validate request
            if (request.Items == null || !request.Items.Any())
                return BadRequest(new { message = "At least one item is required" });

            if (request.GrandTotal <= 0)
                return BadRequest(new { message = "Grand total must be greater than 0" });

            if (request.DiscountAmount < 0)
                return BadRequest(new { message = "Discount amount cannot be negative" });

            if (request.DiscountAmount > request.Subtotal)
                return BadRequest(new { message = "Discount amount cannot exceed the order subtotal" });

            // 2. Validate customer
            Customer? customer = null;
            if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
            {
                customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                if (customer is null)
                    return NotFound(new { message = "Customer not found" });

                // Check credit limit if there's due amount
                if (request.DueAmount > 0 && !customer.CanPlaceOrder())
                    return BadRequest(new { message = "Customer has exceeded credit limit" });
            }

            // 3. Validate parts and check stock
            var partIds = request.Items.Select(x => x.PartId).ToList();
            var parts = new List<Part>();
            foreach (var partId in partIds)
            {
                var part = await _partRepository.GetByIdAsync(partId, cancellationToken);
                if (part is null)
                    return BadRequest(new { message = $"Part with ID {partId} not found" });
                parts.Add(part);
            }

            // Check stock availability
            var stockCheckFailed = false;
            var insufficientStockMessage = "";
            foreach (var item in request.Items)
            {
                var stockLevels = await _stockLevelRepository.GetByPartAsync(item.PartId, cancellationToken);
                // Use base unit quantities for accurate comparison across different units
                var totalAvailable = stockLevels.Sum(sl => 
                    (sl.QuantityOnHandInBaseUnit > 0 ? sl.QuantityOnHandInBaseUnit : sl.QuantityOnHand) - 
                    (sl.QuantityReservedInBaseUnit > 0 ? sl.QuantityReservedInBaseUnit : sl.QuantityReserved));

                var part = parts.First(p => p.Id == item.PartId);
                var (requiredBaseQty, _, _) = await ResolveUnitPricingAsync(
                    part,
                    item.Quantity,
                    item.UnitId,
                    item.UnitPrice,
                    cancellationToken);

                if (totalAvailable < requiredBaseQty)
                {
                    insufficientStockMessage = $"Insufficient stock for {part.Name}. Available: {totalAvailable}, Required: {requiredBaseQty}";
                    stockCheckFailed = true;
                    break;
                }
            }

            if (stockCheckFailed)
            {
                _logger.LogWarning("Stock check failed: {Message}", insufficientStockMessage);
                return BadRequest(new { message = insufficientStockMessage });
            }

            // 4. Generate codes
            var soNumber = await _codeGenerateService.GenerateAsync("SO", cancellationToken);
            var invoiceNumber = await _codeGenerateService.GenerateAsync("INV", cancellationToken);

            // 5. Create Sales Order
            var salesOrder = SalesOrder.Create(
                soNumber,
                request.CustomerId ?? Guid.Empty,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                null, // WarehouseId (optional)
                request.TechnicianId,
                request.TechnicianName,
                "", // DeliveryAddress
                request.Notes
            );

            // Add line items
            int lineNumber = 1;
            foreach (var item in request.Items)
            {
                // Get part to determine base unit
                var part = await _dbContext.Parts
                    .FirstOrDefaultAsync(p => p.Id == item.PartId && !p.Isdeleted, cancellationToken);

                if (part == null)
                    return BadRequest(new { message = $"Part with ID {item.PartId} not found" });

                var itemUnitPrice = item.UnitPrice > 0 ? item.UnitPrice : part.SellingPrice;
                if (itemUnitPrice <= 0)
                    return BadRequest(new { message = $"No selling price set for '{part.Name}'. Please set a selling price on the product." });

                var (quantityInBaseUnit, unitId, baseUnitPrice) = await ResolveUnitPricingAsync(
                    part,
                    item.Quantity,
                    item.UnitId,
                    itemUnitPrice,
                    cancellationToken);

                try
                {
                    _pricingValidationService.ValidateLinePricing(part, baseUnitPrice, item.Discount);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                var discountPerUnit = (itemUnitPrice * item.Discount) / 100;
                var salesOrderLine = SalesOrderLine.Create(
                    salesOrder.Id,
                    item.PartId,
                    item.Quantity,
                    itemUnitPrice,
                    lineNumber++,
                    unitId: unitId,
                    quantityInBaseUnit: quantityInBaseUnit,
                    discount: discountPerUnit,
                    description: item.PartName,
                    productVariantId: item.ProductVariantId
                );
                salesOrder.LineItems.Add(salesOrderLine);
            }

            // Calculate totals
            salesOrder.CalculateTotal();
            salesOrder.SetTax(request.VatAmount);

            // If SaveAsQuotation = true, keep as DRAFT. Otherwise, confirm the order
            if (!request.SaveAsQuotation)
            {
                salesOrder.Confirm();
            }
            salesOrder.CreatedBy = _currentUserService.GetCurrentUsername();
            salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Persist everything in a single transaction
            Invoice? savedInvoice = null;
            var qsStrategy = _dbContext.Database.CreateExecutionStrategy();
            await qsStrategy.ExecuteAsync(async () =>
            {
                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    await _salesOrderRepository.AddAsync(salesOrder, cancellationToken);

                    if (request.SaveAsQuotation)
                    {
                        await tx.CommitAsync(cancellationToken);
                        return;
                    }

                    var invoice = Invoice.Create(invoiceNumber, salesOrder.Id, request.Subtotal,
                        request.VatAmount, DateTime.UtcNow.AddDays(30), request.Notes);
                    invoice.Issue();

                    // Propagate discount so Invoice.GrandTotal matches the actual payable amount
                    if (request.DiscountAmount > 0)
                        invoice.SetDiscount(request.DiscountAmount);

                    invoice.CreatedBy = _currentUserService.GetCurrentUsername();
                    invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _invoiceRepository.AddAsync(invoice, cancellationToken);

                    decimal advancePaymentAmount = 0;
                    if (request.UseAdvanceBalance && request.AdvanceAmountToApply > 0 && request.CustomerId.HasValue)
                    {
                        try
                        {
                            var advancePayments = await _customerPaymentRepository.GetByCustomerAsync(request.CustomerId.Value, cancellationToken);
                            var availableAdvance = advancePayments
                                .Where(p => p.PaymentType == Domain.Entities.CustomerPaymentType.ADVANCE &&
                                           p.Status == "COMPLETED" && p.RemainingAmount > 0)
                                .OrderBy(p => p.PaymentDate)
                                .FirstOrDefault();

                            if (availableAdvance != null && availableAdvance.RemainingAmount >= request.AdvanceAmountToApply)
                            {
                                var advancePayment = CustomerPayment.CreateFromAdvance(
                                    request.CustomerId.Value, invoice.Id, availableAdvance.Id, null,
                                    request.AdvanceAmountToApply,
                                    $"Applied from advance {availableAdvance.TransactionNumber} - Quick Sale");
                                advancePayment.MarkAsCompleted();
                                advancePayment.MarkAsSettled("System");
                                advancePayment.CreatedBy = _currentUserService.GetCurrentUsername();
                                advancePayment.ModifiedBy = _currentUserService.GetCurrentUsername();
                                availableAdvance.ReduceRemainingAmount(request.AdvanceAmountToApply);
                                availableAdvance.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _customerPaymentRepository.AddAsync(advancePayment, cancellationToken);
                                await _customerPaymentRepository.UpdateAsync(availableAdvance, cancellationToken);
                                advancePaymentAmount = request.AdvanceAmountToApply;
                                _logger.LogInformation("Applied advance credit of {Amount} to invoice {InvoiceNumber}",
                                    request.AdvanceAmountToApply, invoiceNumber);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying advance credit in quick sale for customer {CustomerId}", request.CustomerId.Value);
                        }
                    }

                    decimal manualPaymentAmount = 0;
                    if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
                    {
                        var customerForBalance = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                        if (customerForBalance != null)
                        {
                            customerForBalance.UpdateBalance(invoice.GrandTotal);
                            if (advancePaymentAmount > 0)
                                customerForBalance.UpdateBalance(-advancePaymentAmount);

                            if (request.Payments != null && request.Payments.Any())
                            {
                                foreach (var payment in request.Payments.Where(p => p.Amount > 0 && p.Method != "DUE"))
                                {
                                    var transactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                                    var customerPayment = CustomerPayment.Create(
                                        request.CustomerId.Value, null, payment.Amount,
                                        payment.Method ?? "CASH", transactionNumber, payment.Reference ?? "", DateTime.UtcNow);
                                    customerPayment.LinkToInvoice(invoice.Id);
                                    customerPayment.CreatedBy = _currentUserService.GetCurrentUsername();
                                    customerPayment.ModifiedBy = _currentUserService.GetCurrentUsername();
                                    if (payment.Method?.Trim().ToUpper() == "CASH")
                                    {
                                        customerPayment.MarkAsCompleted();
                                        customerPayment.MarkAsSettled("System");
                                        manualPaymentAmount += payment.Amount;
                                        _logger.LogInformation("Completed CASH payment {TransactionNumber} for {Amount}", transactionNumber, payment.Amount);
                                    }
                                    else
                                    {
                                        _logger.LogInformation("Created PENDING payment {TransactionNumber} for {Amount} via {Method}", transactionNumber, payment.Amount, payment.Method);
                                    }
                                    await _customerPaymentRepository.AddAsync(customerPayment, cancellationToken);
                                }
                                if (manualPaymentAmount > 0)
                                    customerForBalance.UpdateBalance(-manualPaymentAmount);
                            }

                            customerForBalance.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _customerRepository.UpdateAsync(customerForBalance, cancellationToken);
                        }
                    }

                    decimal totalPaymentAmount = advancePaymentAmount + manualPaymentAmount;
                    _dbContext.Entry(invoice).State = EntityState.Detached;
                    var freshInvoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (freshInvoice != null)
                    {
                        freshInvoice.UpdatePaymentStatus();
                        freshInvoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);
                        savedInvoice = freshInvoice;
                    }

                    if (totalPaymentAmount > 0)
                    {
                        salesOrder.RecordPayment(totalPaymentAmount);
                        salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                    }

                    foreach (var line in salesOrder.LineItems)
                    {
                        var stockLevels = await _stockLevelRepository.GetByPartAsync(line.PartId, cancellationToken);
                        var remainingQty = line.QuantityInBaseUnit;
                        foreach (var stockLevel in stockLevels.OrderByDescending(sl => sl.QuantityAvailableInBaseUnit))
                        {
                            if (remainingQty <= 0) break;
                            var qtyToDecrease = Math.Min(remainingQty, stockLevel.QuantityAvailableInBaseUnit);
                            if (qtyToDecrease > 0)
                            {
                                stockLevel.RemoveStock(qtyToDecrease, qtyToDecrease, "Quick Sale - " + invoiceNumber);
                                stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);
                                var stockMovement = StockMovement.Create(stockLevel.Id, "OUT", qtyToDecrease,
                                    $"Quick Sale {invoiceNumber}", invoiceNumber,
                                    unitId: stockLevel.UnitId, quantityInBaseUnit: qtyToDecrease);
                                stockMovement.Approve(_currentUserService.GetCurrentUsername());
                                stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                                stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);
                                try
                                {
                                    var stockLots = await _dbContext.StockLots
                                        .Where(l => l.PartId == line.PartId &&
                                                   l.WarehouseId == stockLevel.WarehouseId &&
                                                   l.QuantityAvailableInBaseUnit > 0)
                                        .OrderBy(l => l.ReceivingDate)
                                        .ToListAsync(cancellationToken);
                                    var lotRemainingQty = qtyToDecrease;
                                    foreach (var lot in stockLots)
                                    {
                                        if (lotRemainingQty <= 0) break;
                                        int qtyFromLot = Math.Min(lot.QuantityAvailableInBaseUnit, lotRemainingQty);
                                        lot.RemoveStock(qtyFromLot, qtyFromLot, $"Quick Sale {invoiceNumber}");
                                        lot.ModifiedBy = _currentUserService.GetCurrentUsername();
                                        var lotMovement = StockLotMovement.Create(lot.Id, qtyFromLot, "SALE",
                                            salesOrder.Id, "QuickSale", DateTime.UtcNow, lot.CostPrice,
                                            $"Quick Sale {invoiceNumber}");
                                        lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                                        lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                                        await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);
                                        lotRemainingQty -= qtyFromLot;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogWarning(ex, "Could not create stock lot movements for quick sale {InvoiceNumber}, part {PartId}",
                                        invoiceNumber, line.PartId);
                                }
                                remainingQty -= qtyToDecrease;
                            }
                        }
                    }

                    // Audit trail: salesperson discount
                    if (request.DiscountAmount > 0)
                    {
                        await _dbContext.AuditLogs.AddAsync(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = "SalesOrder",
                            EntityId = salesOrder.Id.ToString(),
                            Action = "DISCOUNT_APPLIED",
                            PropertyName = "DiscountAmount",
                            OldValue = "0",
                            NewValue = request.DiscountAmount.ToString("F2"),
                            PerformedBy = _currentUserService.GetCurrentUsername(),
                            PerformedAt = DateTime.UtcNow,
                            UserAgent = $"Type:{request.DiscountType ?? "NONE"}|Reason:{request.DiscountReason ?? string.Empty}"
                        }, cancellationToken);
                    }

                    // Audit trail: due/credit balance
                    if (request.DueAmount > 0)
                    {
                        await _dbContext.AuditLogs.AddAsync(new AuditLog
                        {
                            Id = Guid.NewGuid(),
                            EntityName = "SalesOrder",
                            EntityId = salesOrder.Id.ToString(),
                            Action = "DUE_CREDIT_RECORDED",
                            PropertyName = "DueBalance",
                            OldValue = "0",
                            NewValue = request.DueAmount.ToString("F2"),
                            PerformedBy = _currentUserService.GetCurrentUsername(),
                            PerformedAt = DateTime.UtcNow
                        }, cancellationToken);
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

            _logger.LogInformation("Quick sale created successfully. Invoice: {InvoiceNumber}, SO: {SONumber}",
                invoiceNumber, soNumber);

            // Dispatch domain events raised by salesOrder.Confirm() — after commit only
            if (!request.SaveAsQuotation)
            {
                var quickSaleEvents = salesOrder.DomainEvents.ToList();
                salesOrder.ClearEvents();
                await _eventDispatcher.DispatchAsync(quickSaleEvents, cancellationToken);
            }

            if (request.SaveAsQuotation)
            {
                return Ok(new QuickSaleResponse
                {
                    Id = Guid.NewGuid(),
                    InvoiceNumber = "",
                    SalesOrderId = salesOrder.Id,
                    SalesOrderNumber = salesOrder.SONumber,
                    CustomerId = request.CustomerId,
                    CustomerName = request.CustomerName,
                    TechnicianId = request.TechnicianId,
                    TechnicianName = request.TechnicianName,
                    PaymentResponsibility = request.PaymentResponsibility,
                    Subtotal = request.Subtotal,
                    DiscountAmount = request.DiscountAmount,
                    VatAmount = request.VatAmount,
                    GrandTotal = request.GrandTotal,
                    PaidAmount = 0,
                    DueAmount = request.GrandTotal,
                    Status = "DRAFT",
                    IsQuotation = true,
                    CreatedAt = salesOrder.SODate
                });
            }

            return CreatedAtAction(nameof(GetById), new { id = salesOrder.Id }, new QuickSaleResponse
            {
                Id = savedInvoice?.Id ?? Guid.NewGuid(),
                InvoiceNumber = invoiceNumber,
                SalesOrderId = salesOrder.Id,
                SalesOrderNumber = salesOrder.SONumber,
                CustomerId = request.CustomerId,
                CustomerName = request.CustomerName,
                TechnicianId = request.TechnicianId,
                TechnicianName = request.TechnicianName,
                PaymentResponsibility = request.PaymentResponsibility,
                Subtotal = request.Subtotal,
                DiscountAmount = request.DiscountAmount,
                VatAmount = request.VatAmount,
                GrandTotal = request.GrandTotal,
                PaidAmount = request.PaidAmount,
                DueAmount = request.DueAmount,
                Status = "COMPLETED",
                IsQuotation = false,
                CreatedAt = DateTime.UtcNow
            });
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
            _logger.LogError(ex, "Error creating quick sale");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while creating the quick sale", error = ex.Message });
        }
    }
}

public class RecordPaymentRequest
{
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public string PaymentMethod { get; set; } = "CASH";
    public string ReferenceNumber { get; set; } = "";
    public Guid? PaymentProviderId { get; set; }
}
