using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Application.SaleOrders;
using AutoPartShop.Application.SaleOrders.Dtos;
using AutoPartShop.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly ISaleOrderReadRepository _saleOrderReadRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISalesReturnRepository _salesReturnRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _partRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly ICustomerPaymentRepository _customerPaymentRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly IUnitConversionService _unitConversionService;
    private readonly IWarrantyService _warrantyService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPricingValidationService _pricingValidationService;
    private readonly ILogger<SalesOrderController> _logger;
    private readonly AutoPartDbContext _dbContext;

    public SalesOrderController(
        ISalesOrderRepository salesOrderRepository,
        ISaleOrderReadRepository saleOrderReadRepository,
        IInvoiceRepository invoiceRepository,
        ISalesReturnRepository salesReturnRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository,
        IStockLevelRepository stockLevelRepository,
        ICustomerPaymentRepository customerPaymentRepository,
        ICodeGenerateService codeGenerateService,
        IUnitConversionService unitConversionService,
        IWarrantyService warrantyService,
        ICurrentUserService currentUserService,
        IPricingValidationService pricingValidationService,
        ILogger<SalesOrderController> logger,
        AutoPartDbContext dbContext)
    {
        _salesOrderRepository = salesOrderRepository;
        _saleOrderReadRepository = saleOrderReadRepository;
        _invoiceRepository = invoiceRepository;
        _salesReturnRepository = salesReturnRepository;
        _customerRepository = customerRepository;
        _partRepository = partRepository;
        _stockLevelRepository = stockLevelRepository;
        _customerPaymentRepository = customerPaymentRepository;
        _codeGenerateService = codeGenerateService;
        _unitConversionService = unitConversionService;
        _warrantyService = warrantyService;
        _currentUserService = currentUserService;
        _pricingValidationService = pricingValidationService;
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
            if (request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerName) || request.DeliveryDate == default)
                return BadRequest(new { message = "CustomerId, CustomerName and DeliveryDate are required" });

            var order = SalesOrder.Create(
                $"SO-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.CustomerId,
                request.CustomerName,
                request.CustomerEmail,
                request.CustomerPhone,
                null,  // WarehouseId - optional
                request.TechnicianId,
                request.TechnicianName,
                request.CustomerCity,  // DeliveryAddress
                request.Notes,
                request.Currency
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

            if (request.CustomerId == Guid.Empty || string.IsNullOrWhiteSpace(request.CustomerName) || request.DeliveryDate == default)
                return BadRequest(new { message = "CustomerId, CustomerName and DeliveryDate are required" });

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

                var subtotal = newLines.Sum(l => l.TotalPrice);
                var discountPercentage = request.Discount;
                var discountAmount = subtotal * (discountPercentage / 100);
                var totalAmount = subtotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

                await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                var updated = await _dbContext.Set<SalesOrder>()
                    .Where(so => so.Id == order.Id && !so.Isdeleted)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(so => so.CustomerId, request.CustomerId)
                        .SetProperty(so => so.CustomerName, request.CustomerName)
                        .SetProperty(so => so.CustomerEmail, request.CustomerEmail)
                        .SetProperty(so => so.CustomerPhone, request.CustomerPhone)
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
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var order = await _salesOrderRepository.GetByIdAsync(id, cancellationToken);
            if (order is null) return NotFound(new { message = "Sales order not found" });

            // Get default warehouse if not specified
            var warehouseId = order.WarehouseId ?? await GetDefaultWarehouseId(cancellationToken);
            if (warehouseId == Guid.Empty)
                return BadRequest(new { message = "Warehouse is required for stock deduction" });

            // Check stock availability for all line items
            foreach (var line in order.LineItems)
            {
                var stockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(line.PartId, warehouseId, cancellationToken);
                if (stockLevel == null || stockLevel.QuantityAvailable < line.QuantityInBaseUnit)
                {
                    var part = await _partRepository.GetByIdAsync(line.PartId, cancellationToken);
                    return BadRequest(new { message = $"Insufficient stock for part: {part?.Name ?? line.PartId.ToString()}" });
                }
            }

            // Deduct stock and create movement history
            foreach (var line in order.LineItems)
            {
                var stockLevel = await _stockLevelRepository.GetByPartAndWarehouseAsync(line.PartId, warehouseId, cancellationToken);
                if (stockLevel != null)
                {
                    // Only call RemoveStock for confirmed sales - no need to reserve first
                    stockLevel.RemoveStock(line.QuantityInBaseUnit, $"Sales Order {order.SONumber}");
                    stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    // Create stock movement record
                    var stockMovement = StockMovement.Create(
                        stockLevel.Id,
                        "OUT",
                        line.QuantityInBaseUnit,
                        $"Sales Order {order.SONumber}",
                        order.SONumber
                    );
                    stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                    stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

                    // Create stock lot movement if using lot tracking
                    try
                    {
                        var stockLots = await _dbContext.StockLots
                            .Where(sl => sl.PartId == line.PartId &&
                                        sl.WarehouseId == warehouseId &&
                                        sl.QuantityAvailable > 0)
                            .OrderBy(sl => sl.ExpiryDate) // FIFO by expiry
                            .ThenBy(sl => sl.CreatedDate)   // Then by creation date
                            .ToListAsync(cancellationToken);

                        int remainingQty = line.QuantityInBaseUnit;
                        foreach (var lot in stockLots)
                        {
                            if (remainingQty <= 0) break;

                            int qtyToDeduct = Math.Min(lot.QuantityAvailable, remainingQty);
                            lot.RemoveStock(qtyToDeduct, $"Sales Order {order.SONumber}");
                            lot.ModifiedBy = _currentUserService.GetCurrentUsername();

                            // Create lot movement
                            var lotMovement = StockLotMovement.Create(
                                lot.Id,
                                qtyToDeduct,
                                "SALE",
                                order.Id,
                                "SalesOrder",
                                null,
                                lot.CostPrice,
                                $"Sales Order {order.SONumber}"
                            );
                            lotMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                            lotMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);

                            remainingQty -= qtyToDeduct;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not create stock lot movements for SO {SOId}", id);
                        // Continue execution - lot tracking is optional
                    }
                }
            }

            order.Confirm();
            order.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _salesOrderRepository.UpdateAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Auto-create warranty registrations for parts with warranty
            try
            {
                foreach (var line in order.LineItems)
                {
                    var part = await _partRepository.GetByIdAsync(line.PartId, cancellationToken);
                    if (part != null && part.HasWarranty && part.WarrantyPeriodMonths.HasValue)
                    {
                        await _warrantyService.CreateWarrantyForSalesOrderLineAsync(
                            line,
                            order.Id,
                            order.CustomerId,
                            order.SODate,
                            cancellationToken);

                        _logger.LogInformation("Created warranty registration for part {PartName} in order {SONumber}",
                            part.Name, order.SONumber);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not create warranty registrations for SO {SOId}. Warranties can be created manually.", id);
                // Continue execution - warranty creation failure should not block order confirmation
            }

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

    private async Task<Guid> GetDefaultWarehouseId(CancellationToken cancellationToken)
    {
        var defaultWarehouse = await _dbContext.Warehouses
            .OrderBy(w => w.CreatedDate)
            .FirstOrDefaultAsync(cancellationToken);
        return defaultWarehouse?.Id ?? Guid.Empty;
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            if (!await _salesOrderRepository.ExistsAsync(id, cancellationToken))
                return NotFound(new { message = "Sales order not found" });

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
            // Get all sales orders for this customer
            var salesOrders = await _salesOrderRepository.GetByCustomerAsync(customerId, cancellationToken);
            var salesOrderIds = salesOrders.Select(so => so.Id).ToList();

            // Get all invoices for these sales orders
            var allInvoices = new List<Invoice>();
            foreach (var salesOrderId in salesOrderIds)
            {
                var invoices = await _invoiceRepository.GetBySalesOrderAsync(salesOrderId, cancellationToken);
                allInvoices.AddRange(invoices);
            }

            var response = allInvoices.Select(MapToInvoiceResponse).OrderByDescending(i => i.InvoiceDate);
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

            var invoice = Invoice.Create(
                $"INV-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.SalesOrderId,
                request.SubTotal,
                request.TaxAmount,
                request.DueDate,
                request.Notes
            );
            invoice.CreatedBy = _currentUserService.GetCurrentUsername();
            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _invoiceRepository.AddAsync(invoice, cancellationToken);

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

            // Get sales order to find customer
            var salesOrder = await _salesOrderRepository.GetByIdAsync(invoice.SalesOrderId, cancellationToken);
            if (salesOrder is null) return NotFound(new { message = "Sales order not found" });

            // Get customer and update balance (invoice increases balance)
            var customer = await _customerRepository.GetByIdAsync(salesOrder.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            invoice.Issue();
            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Increase customer balance (invoice increases debt)
            customer.UpdateBalance(invoice.GrandTotal);
            customer.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
            await _customerRepository.UpdateAsync(customer, cancellationToken);

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
            // Get invoice with sales order to access customer ID
            var invoice = await _dbContext.Invoices
                .Include(i => i.SalesOrder)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            if (invoice is null) return NotFound(new { message = "Invoice not found" });
            if (invoice.SalesOrder is null) return BadRequest(new { message = "Sales order not found for invoice" });

            // Create CustomerPayment (SINGLE SOURCE OF TRUTH)
            var payment = CustomerPayment.Create(
                customerId: invoice.SalesOrder.CustomerId,
                paymentProviderId: request.PaymentProviderId,
                amount: request.Amount,
                paymentMethod: request.PaymentMethod ?? "CASH",
                transactionNumber: $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}",
                referenceNumber: request.ReferenceNumber ?? "",
                paymentDate: request.PaymentDate
            );

            // Link to invoice
            payment.LinkToInvoice(invoice.Id);

            payment.CreatedBy = _currentUserService.GetCurrentUsername();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Get customer to update balance
            var customer = await _customerRepository.GetByIdAsync(invoice.SalesOrder.CustomerId, cancellationToken);
            if (customer is null)
                return NotFound(new { message = "Customer not found" });

            // If payment method is CASH, automatically mark as completed and update customer balance
            if (request.PaymentMethod?.Trim().ToUpper() == "CASH")
            {
                // Mark as completed
                payment.MarkAsCompleted();

                // Decrease customer balance (negative because payment reduces debt)
                customer.UpdateBalance(-request.Amount);
                customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                await _customerPaymentRepository.AddAsync(payment, cancellationToken);
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                // Reload invoice with payments to calculate AmountPaid correctly
                invoice = await _dbContext.Invoices
                    .Include(i => i.CustomerPayments)
                    .Include(i => i.SalesOrder)
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

                // Update invoice status based on payments
                invoice!.UpdatePaymentStatus();
                invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                // Update sales order paid amount
                var salesOrder = await _salesOrderRepository.GetByIdAsync(invoice.SalesOrderId, cancellationToken);
                if (salesOrder != null)
                {
                    salesOrder.RecordPayment(request.Amount);
                    salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                }
            }
            else
            {
                // For CHECK, BANK_TRANSFER, etc., keep as PENDING until manually marked as complete
                // Customer balance will be updated when payment is marked as completed via the MarkCompleted endpoint
                await _customerPaymentRepository.AddAsync(payment, cancellationToken);
            }

            // Reload invoice if not already reloaded (for non-cash payments)
            if (request.PaymentMethod?.Trim().ToUpper() != "CASH")
            {
                invoice = await _dbContext.Invoices
                    .Include(i => i.CustomerPayments)
                    .Include(i => i.SalesOrder)
                    .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
            }

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

    // Sales Return endpoints
    [HttpPost("returns")]
    public async Task<IActionResult> CreateReturn(CreateSalesReturnRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.SalesOrderId == Guid.Empty || request.WarehouseId == Guid.Empty)
                return BadRequest(new { message = "SalesOrderId and WarehouseId are required" });

            var salesReturn = SalesReturn.Create(
                $"SR-{DateTime.UtcNow:yyyyMMddHHmmss}",
                request.SalesOrderId,
                null,  // InvoiceId - optional
                request.Reason,
                request.WarehouseId,
                null,  // ReturnDate - will default to now
                request.Notes
            );

            // Add lines
            foreach (var lineRequest in request.Lines)
            {
                var line = SalesReturnLine.Create(
                    salesReturn.Id,
                    lineRequest.SalesOrderLineId,
                    lineRequest.PartId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    lineRequest.Condition
                );
                line.AddNotes(lineRequest.Notes);
                salesReturn.LineItems.Add(line);
            }

            salesReturn.CreatedBy = _currentUserService.GetCurrentUsername();
            salesReturn.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _salesReturnRepository.AddAsync(salesReturn, cancellationToken);

            return CreatedAtAction(nameof(GetReturnById), new { id = salesReturn.Id }, MapToSalesReturnResponse(salesReturn));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sales return");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the sales return");
        }
    }

    [HttpGet("returns/{id:guid}")]
    public async Task<IActionResult> GetReturnById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id, cancellationToken);
            if (salesReturn is null) return NotFound(new { message = "Sales return not found" });

            return Ok(MapToSalesReturnResponse(salesReturn));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales return by ID: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the sales return");
        }
    }

    [HttpPatch("returns/{id:guid}/approve")]
    public async Task<IActionResult> ApproveReturn(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var salesReturn = await _salesReturnRepository.GetByIdAsync(id, cancellationToken);
            if (salesReturn is null) return NotFound(new { message = "Sales return not found" });

            salesReturn.Approve("Manager");
            salesReturn.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _salesReturnRepository.UpdateAsync(salesReturn, cancellationToken);

            return Ok(MapToSalesReturnResponse(salesReturn));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving sales return: {ReturnId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while approving the sales return");
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
            SubTotal = order.SubTotal,
            TaxAmount = order.TaxAmount,
            Discount = order.DiscountPercentage,
            GrandTotal = order.GrandTotal,
            Currency = order.Currency,
            AmountPaid = order.PaidAmount,
            OutstandingAmount = order.GrandTotal - order.PaidAmount,
            IsOverdue = order.DeliveryDate.HasValue && DateTime.UtcNow > order.DeliveryDate.Value && order.Status != "DELIVERED" && order.Status != "CANCELLED",
            Notes = order.Notes,
            Lines = order.LineItems.Select(l => new SalesOrderLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                PartName = l.Part?.Name ?? string.Empty,
                PartSku = l.Part?.SKU ?? string.Empty,
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
            CreatedAt = DateTime.UtcNow
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

        var (quantityInBaseUnit, unitId, baseUnitPrice) = await ResolveUnitPricingAsync(
            part,
            lineRequest.Quantity,
            lineRequest.UnitId,
            lineRequest.UnitPrice,
            cancellationToken);

        _pricingValidationService.ValidateLinePricing(part, baseUnitPrice, lineRequest.Discount);

        var discountPerUnit = lineRequest.UnitPrice * (lineRequest.Discount / 100);

        return SalesOrderLine.Create(
            order.Id,
            lineRequest.PartId,
            lineRequest.Quantity,
            lineRequest.UnitPrice,
            lineNumber,
            unitId: unitId,
            quantityInBaseUnit: quantityInBaseUnit,
            discount: discountPerUnit
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
            var conversionFactor = await _unitConversionService.GetConversionFactorAsync(unitId.Value, part.UnitId.Value, cancellationToken);
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
            CreatedAt = DateTime.UtcNow
        };
    }

    private SalesReturnResponse MapToSalesReturnResponse(SalesReturn salesReturn)
    {
        return new SalesReturnResponse
        {
            Id = salesReturn.Id,
            ReturnNumber = salesReturn.ReturnNumber,
            SalesOrderId = salesReturn.SalesOrderId,
            WarehouseId = Guid.Empty,  // SalesReturn doesn't track warehouse directly
            Reason = salesReturn.Reason,
            Status = salesReturn.Status,
            TotalRefundAmount = salesReturn.RefundAmount,
            Notes = salesReturn.Notes,
            Lines = salesReturn.LineItems.Select(l => new SalesReturnLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice,
                RefundAmount = l.RefundAmount,
                Condition = l.Condition,
                Notes = l.Notes
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Quick Sale endpoint for POS-style sales
    /// Creates SalesOrder + Invoice + Payments in a single transaction
    /// </summary>
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
                var totalAvailable = stockLevels.Sum(sl => sl.QuantityAvailable);

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

                var (quantityInBaseUnit, unitId, baseUnitPrice) = await ResolveUnitPricingAsync(
                    part,
                    item.Quantity,
                    item.UnitId,
                    item.UnitPrice,
                    cancellationToken);

                try
                {
                    _pricingValidationService.ValidateLinePricing(part, baseUnitPrice, item.Discount);
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }

                var discountPerUnit = (item.UnitPrice * item.Discount) / 100;
                var salesOrderLine = SalesOrderLine.Create(
                    salesOrder.Id,
                    item.PartId,
                    item.Quantity,
                    item.UnitPrice,
                    lineNumber++,
                    unitId: unitId,
                    quantityInBaseUnit: quantityInBaseUnit,
                    discount: discountPerUnit,
                    description: item.PartName
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

            // Save sales order
            await _salesOrderRepository.AddAsync(salesOrder, cancellationToken);
            await _codeGenerateService.SaveGenerateCodeAsync("SO", cancellationToken);

            // If quotation, skip invoice creation, stock updates, and payments - just return the quotation
            if (request.SaveAsQuotation)
            {
                return Ok(new QuickSaleResponse
                {
                    Id = Guid.NewGuid(), // No invoice for quotations
                    InvoiceNumber = "", // No invoice number
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

            // 6. Create Invoice
            var invoice = Invoice.Create(
                invoiceNumber,
                salesOrder.Id,
                request.Subtotal,
                request.VatAmount,
                DateTime.UtcNow.AddDays(30),
                request.Notes
            );

            // Issue invoice immediately
            invoice.Issue();

            invoice.CreatedBy = _currentUserService.GetCurrentUsername();
            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

            // Save invoice
            await _invoiceRepository.AddAsync(invoice, cancellationToken);
            await _codeGenerateService.SaveGenerateCodeAsync("INV", cancellationToken);

            // 6a. Apply Advance Credit if requested
            decimal advancePaymentAmount = 0;
            if (request.UseAdvanceBalance && request.AdvanceAmountToApply > 0 && request.CustomerId.HasValue)
            {
                try
                {
                    // Find the advance payment to use
                    var advancePayments = await _customerPaymentRepository.GetByCustomerAsync(request.CustomerId.Value, cancellationToken);
                    var availableAdvance = advancePayments
                        .Where(p => p.PaymentType == Domain.Entities.CustomerPaymentType.ADVANCE &&
                                   p.Status == "COMPLETED" &&
                                   p.RemainingAmount > 0)
                        .OrderBy(p => p.PaymentDate)
                        .FirstOrDefault();

                    if (availableAdvance != null && availableAdvance.RemainingAmount >= request.AdvanceAmountToApply)
                    {
                        // Create a new payment from the advance
                        var advancePayment = CustomerPayment.CreateFromAdvance(
                            request.CustomerId.Value,
                            invoice.Id,
                            availableAdvance.Id,
                            null, // PaymentProviderId
                            request.AdvanceAmountToApply,
                            $"Applied from advance {availableAdvance.TransactionNumber} - Quick Sale"
                        );

                        // Mark as completed since it's from advance
                        advancePayment.MarkAsCompleted();
                        advancePayment.MarkAsSettled("System");
                        advancePayment.CreatedBy = _currentUserService.GetCurrentUsername();
                        advancePayment.ModifiedBy = _currentUserService.GetCurrentUsername();

                        // Reduce the advance payment's remaining amount
                        availableAdvance.ReduceRemainingAmount(request.AdvanceAmountToApply);
                        availableAdvance.ModifiedBy = _currentUserService.GetCurrentUsername();

                        // Save both payments
                        await _customerPaymentRepository.AddAsync(advancePayment, cancellationToken);
                        await _customerPaymentRepository.UpdateAsync(availableAdvance, cancellationToken);

                        // Track advance payment amount for later use
                        advancePaymentAmount = request.AdvanceAmountToApply;

                        _logger.LogInformation("Applied advance credit of {Amount} from payment {TransactionNumber} to invoice {InvoiceNumber}",
                            request.AdvanceAmountToApply, availableAdvance.TransactionNumber, invoiceNumber);
                    }
                    else
                    {
                        _logger.LogWarning("Could not apply advance credit: insufficient advance balance or no advance payment found for customer {CustomerId}",
                            request.CustomerId.Value);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error applying advance credit in quick sale for customer {CustomerId}", request.CustomerId.Value);
                    // Continue with regular payments - don't fail the entire sale
                }
            }

            // 6b. Create CustomerPayment records and update customer balance
            decimal manualPaymentAmount = 0;
            if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
            {
                // Get customer once for all balance updates
                var customerForBalance = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                if (customerForBalance != null)
                {
                    // Invoice increases customer balance (debt)
                    customerForBalance.UpdateBalance(invoice.GrandTotal);

                    // Decrease customer balance by advance payment (if any)
                    if (advancePaymentAmount > 0)
                    {
                        customerForBalance.UpdateBalance(-advancePaymentAmount);
                    }

                    // Create manual payment records (SINGLE SOURCE OF TRUTH)
                    if (request.Payments != null && request.Payments.Any())
                    {
                        foreach (var payment in request.Payments.Where(p => p.Amount > 0 && p.Method != "DUE"))
                        {
                            var transactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                            var customerPayment = CustomerPayment.Create(
                                request.CustomerId.Value,
                                null, // PaymentProviderId - can be enhanced later
                                payment.Amount,
                                payment.Method ?? "CASH",
                                transactionNumber,
                                payment.Reference ?? "",
                                DateTime.UtcNow
                            );

                            // Link to invoice
                            customerPayment.LinkToInvoice(invoice.Id);

                            customerPayment.CreatedBy = _currentUserService.GetCurrentUsername();
                            customerPayment.ModifiedBy = _currentUserService.GetCurrentUsername();

                            // CASH payments: Auto-complete and include in balance update
                            // Other methods: Keep PENDING until manually confirmed
                            if (payment.Method?.Trim().ToUpper() == "CASH")
                            {
                                // Complete the payment immediately for cash
                                customerPayment.MarkAsCompleted();
                                customerPayment.MarkAsSettled("System");
                                manualPaymentAmount += payment.Amount;

                                _logger.LogInformation("Created and completed CASH CustomerPayment {TransactionNumber} for amount {Amount}",
                                    transactionNumber, payment.Amount);
                            }
                            else
                            {
                                // Keep as PENDING for CHECK, BANK_TRANSFER, etc.
                                _logger.LogInformation("Created PENDING CustomerPayment {TransactionNumber} for amount {Amount} via {Method}",
                                    transactionNumber, payment.Amount, payment.Method);
                            }

                            await _customerPaymentRepository.AddAsync(customerPayment, cancellationToken);
                        }

                        // Decrease customer balance only for completed (CASH) manual payments
                        if (manualPaymentAmount > 0)
                        {
                            customerForBalance.UpdateBalance(-manualPaymentAmount);
                        }
                    }

                    customerForBalance.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _customerRepository.UpdateAsync(customerForBalance, cancellationToken);
                }
            }

            // 6c. Update invoice payment status and sales order (after ALL payments processed)
            decimal totalPaymentAmount = advancePaymentAmount + manualPaymentAmount;

            // Always reload invoice and update status (even if no payments, to set correct initial status)
            if (request.CustomerId.HasValue)
            {
                // Detach current invoice from tracking to force fresh reload
                _dbContext.Entry(invoice).State = EntityState.Detached;

                // Reload invoice with ALL customer payments and update status
                var freshInvoice = await _dbContext.Invoices
                    .Include(i => i.CustomerPayments)
                    .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);

                if (freshInvoice != null)
                {
                    freshInvoice.UpdatePaymentStatus();
                    freshInvoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _invoiceRepository.UpdateAsync(freshInvoice, cancellationToken);
                    invoice = freshInvoice; // Update reference for response
                }

                // Update sales order paid amount with TOTAL (advance + manual)
                if (totalPaymentAmount > 0)
                {
                    salesOrder.RecordPayment(totalPaymentAmount);
                    salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                    await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                }
            }

            // 7. Update stock levels with movement tracking
            foreach (var line in salesOrder.LineItems)
            {
                var stockLevels = await _stockLevelRepository.GetByPartAsync(line.PartId, cancellationToken);
                var remainingQty = line.QuantityInBaseUnit;

                // Decrease from each warehouse that has stock, starting with the one with most stock
                foreach (var stockLevel in stockLevels.OrderByDescending(sl => sl.QuantityAvailable))
                {
                    if (remainingQty <= 0) break;

                    var qtyToDecrease = Math.Min(remainingQty, stockLevel.QuantityAvailable);
                    if (qtyToDecrease > 0)
                    {
                        stockLevel.RemoveStock(qtyToDecrease, "Quick Sale - " + invoiceNumber);
                        stockLevel.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                        // Create stock movement record for audit trail
                        var stockMovement = StockMovement.Create(
                            stockLevel.Id,
                            "OUT",
                            qtyToDecrease,
                            $"Quick Sale {invoiceNumber}",
                            invoiceNumber
                        );
                        stockMovement.CreatedBy = _currentUserService.GetCurrentUsername();
                        stockMovement.ModifiedBy = _currentUserService.GetCurrentUsername();
                        await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

                        // Create stock lot movement for FIFO/LIFO tracking
                        try
                        {
                            var stockLots = await _dbContext.StockLots
                                .Where(l => l.PartId == line.PartId &&
                                           l.WarehouseId == stockLevel.WarehouseId &&
                                           l.QuantityAvailable > 0)
                                .OrderBy(l => l.ReceivingDate) // FIFO
                                .ToListAsync(cancellationToken);

                            var lotRemainingQty = qtyToDecrease;
                            foreach (var lot in stockLots)
                            {
                                if (lotRemainingQty <= 0) break;

                                int qtyFromLot = Math.Min(lot.QuantityAvailable, lotRemainingQty);
                                lot.RemoveStock(qtyFromLot, $"Quick Sale {invoiceNumber}");
                                lot.ModifiedBy = _currentUserService.GetCurrentUsername();

                                // Create lot movement
                                var lotMovement = StockLotMovement.Create(
                                    lot.Id,
                                    qtyFromLot,
                                    "SALE",
                                    salesOrder.Id,
                                    "QuickSale",
                                    DateTime.UtcNow,
                                    lot.CostPrice,
                                    $"Quick Sale {invoiceNumber}"
                                );
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
                            // Continue - stock movement is more important than lot tracking
                        }

                        remainingQty -= qtyToDecrease;
                    }
                }
            }

            // Save all stock movements and lot movements
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 8. Return response
            var response = new QuickSaleResponse
            {
                Id = invoice.Id,
                InvoiceNumber = invoice.InvoiceNumber,
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
            };

            _logger.LogInformation("Quick sale created successfully. Invoice: {InvoiceNumber}, SO: {SONumber}",
                invoiceNumber, soNumber);

            return CreatedAtAction(nameof(GetById), new { id = salesOrder.Id }, response);
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
