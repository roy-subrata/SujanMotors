using AutoPartShop.Application.DTOs.SalesOrderDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class SalesOrderController : ControllerBase
{
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISalesReturnRepository _salesReturnRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPartRepository _partRepository;
    private readonly IStockLevelRepository _stockLevelRepository;
    private readonly ICustomerPaymentRepository _customerPaymentRepository;
    private readonly ICodeGenerateService _codeGenerateService;
    private readonly ILogger<SalesOrderController> _logger;
    private readonly AutoPartDbContext _dbContext;

    public SalesOrderController(
        ISalesOrderRepository salesOrderRepository,
        IInvoiceRepository invoiceRepository,
        ISalesReturnRepository salesReturnRepository,
        ICustomerRepository customerRepository,
        IPartRepository partRepository,
        IStockLevelRepository stockLevelRepository,
        ICustomerPaymentRepository customerPaymentRepository,
        ICodeGenerateService codeGenerateService,
        ILogger<SalesOrderController> logger,
        AutoPartDbContext dbContext)
    {
        _salesOrderRepository = salesOrderRepository;
        _invoiceRepository = invoiceRepository;
        _salesReturnRepository = salesReturnRepository;
        _customerRepository = customerRepository;
        _partRepository = partRepository;
        _stockLevelRepository = stockLevelRepository;
        _customerPaymentRepository = customerPaymentRepository;
        _codeGenerateService = codeGenerateService;
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

    [HttpGet("list")]
    public async Task<IActionResult> GetList(int pageNumber = 1, int pageSize = 10, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (orders, totalCount) = string.IsNullOrWhiteSpace(searchTerm)
                ? await _salesOrderRepository.GetPagedAsync(pageNumber, pageSize, cancellationToken)
                : await _salesOrderRepository.SearchPagedAsync(searchTerm, pageNumber, pageSize, cancellationToken);

            var response = orders.Select(MapToSalesOrderResponse);
            return Ok(new
            {
                data = response,
                pagination = new { pageNumber, pageSize, totalCount, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales orders list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving sales orders");
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
                request.Notes
            );

            // Add lines
            int lineNumber = 1;
            foreach (var lineRequest in request.Lines)
            {
                var line = SalesOrderLine.Create(
                    order.Id,
                    lineRequest.PartId,
                    lineRequest.Quantity,
                    lineRequest.UnitPrice,
                    lineNumber,
                    lineRequest.Discount
                );
                order.LineItems.Add(line);
                lineNumber++;
            }

            order.CreatedBy = "System";
            order.ModifiedBy = "System";

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

            order.ModifiedBy = "System";
            await _salesOrderRepository.UpdateAsync(order, cancellationToken);

            return Ok(MapToSalesOrderResponse(order));
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
                if (stockLevel == null || stockLevel.QuantityAvailable < line.Quantity)
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
                    stockLevel.ReserveStock(line.Quantity);
                    stockLevel.RemoveStock(line.Quantity, $"Sales Order {order.SONumber}");
                    stockLevel.ModifiedBy = "System";
                    await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                    // Create stock movement record
                    var stockMovement = StockMovement.Create(
                        stockLevel.Id,
                        "OUT",
                        line.Quantity,
                        $"Sales Order {order.SONumber}",
                        order.SONumber
                    );
                    stockMovement.CreatedBy = "System";
                    stockMovement.ModifiedBy = "System";
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

                        int remainingQty = line.Quantity;
                        foreach (var lot in stockLots)
                        {
                            if (remainingQty <= 0) break;

                            int qtyToDeduct = Math.Min(lot.QuantityAvailable, remainingQty);
                            lot.RemoveStock(qtyToDeduct, $"Sales Order {order.SONumber}");
                            lot.ModifiedBy = "System";

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
                            lotMovement.CreatedBy = "System";
                            lotMovement.ModifiedBy = "System";
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
            order.ModifiedBy = "System";
            await _salesOrderRepository.UpdateAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

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
            invoice.CreatedBy = "System";
            invoice.ModifiedBy = "System";

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
            invoice.ModifiedBy = "System";

            // Increase customer balance (invoice increases debt)
            customer.UpdateBalance(invoice.GrandTotal);
            customer.ModifiedBy = "System";

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

            // Mark as completed
            payment.MarkAsCompleted();

            // Save payment
            payment.CreatedBy = "System";
            payment.ModifiedBy = "System";
            await _customerPaymentRepository.AddAsync(payment, cancellationToken);

            // Reload invoice with payments to calculate AmountPaid correctly
            invoice = await _dbContext.Invoices
                .Include(i => i.CustomerPayments)
                .Include(i => i.SalesOrder)
                .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

            // Update invoice status based on payments
            invoice!.UpdatePaymentStatus();
            invoice.ModifiedBy = "System";
            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

            // Update sales order paid amount
            var salesOrder = await _salesOrderRepository.GetByIdAsync(invoice.SalesOrderId, cancellationToken);
            if (salesOrder != null)
            {
                salesOrder.RecordPayment(request.Amount);
                salesOrder.ModifiedBy = "System";
                await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
            }

            return Ok(new
            {
                PaymentId = payment.Id,
                InvoiceStatus = invoice.Status,
                AmountPaid = invoice.AmountPaid,
                OutstandingAmount = invoice.OutstandingAmount,
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

            salesReturn.CreatedBy = "System";
            salesReturn.ModifiedBy = "System";

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
            salesReturn.ModifiedBy = "System";
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

    private SalesOrderResponse MapToSalesOrderResponse(SalesOrder order)
    {
        return new SalesOrderResponse
        {
            Id = order.Id,
            SONumber = order.SONumber,
            CustomerId = order.CustomerId,
            CustomerName = order.CustomerName,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            CustomerCity = order.DeliveryAddress,
            TechnicianId = order.TechnicianId,
            TechnicianName = order.TechnicianName,
            OrderDate = order.SODate,
            DeliveryDate = order.DeliveryDate ?? DateTime.MinValue,
            Status = order.Status,
            SubTotal = order.TotalAmount,
            TaxAmount = order.TaxAmount,
            Discount = 0,  // SalesOrder doesn't track global discount
            GrandTotal = order.GrandTotal,
            AmountPaid = order.PaidAmount,
            OutstandingAmount = order.GrandTotal - order.PaidAmount,
            IsOverdue = order.DeliveryDate.HasValue && DateTime.UtcNow > order.DeliveryDate.Value && order.Status != "DELIVERED" && order.Status != "CANCELLED",
            Notes = order.Notes,
            Lines = order.LineItems.Select(l => new SalesOrderLineResponse
            {
                Id = l.Id,
                PartId = l.PartId,
                Quantity = l.Quantity,
                ShippedQuantity = l.ShippedQuantity,
                UnitPrice = l.UnitPrice,
                Discount = l.Discount,
                LineTotal = l.TotalPrice
            }).ToList(),
            CreatedAt = DateTime.UtcNow
        };
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
                if (totalAvailable < item.Quantity)
                {
                    var part = parts.First(p => p.Id == item.PartId);
                    insufficientStockMessage = $"Insufficient stock for {part.Name}. Available: {totalAvailable}, Required: {item.Quantity}";
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
                var discountPerUnit = (item.UnitPrice * item.Discount) / 100;
                var salesOrderLine = SalesOrderLine.Create(
                    salesOrder.Id,
                    item.PartId,
                    item.Quantity,
                    item.UnitPrice,
                    lineNumber++,
                    discountPerUnit,
                    item.PartName
                );
                salesOrder.LineItems.Add(salesOrderLine);
            }

            // Calculate totals
            salesOrder.CalculateTotal();
            salesOrder.SetTax(request.VatAmount);

            // Confirm the order immediately
            salesOrder.Confirm();
            salesOrder.CreatedBy = "System";
            salesOrder.ModifiedBy = "System";

            // Save sales order
            await _salesOrderRepository.AddAsync(salesOrder, cancellationToken);

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

            invoice.CreatedBy = "System";
            invoice.ModifiedBy = "System";

            // Save invoice
            await _invoiceRepository.AddAsync(invoice, cancellationToken);

            // 6b. Create CustomerPayment records and update customer balance
            decimal totalPaymentAmount = 0;
            if (request.CustomerId.HasValue && request.CustomerId.Value != Guid.Empty)
            {
                // Get customer once for all balance updates
                var customerForBalance = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
                if (customerForBalance != null)
                {
                    // Invoice increases customer balance (debt)
                    customerForBalance.UpdateBalance(invoice.GrandTotal);

                    // Create payment records (SINGLE SOURCE OF TRUTH)
                    if (request.Payments != null && request.Payments.Any())
                    {
                        foreach (var payment in request.Payments.Where(p => p.Amount > 0 && p.Method != "DUE"))
                        {
                            var transactionNumber = $"TXN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                            var customerPayment = CustomerPayment.Create(
                                request.CustomerId.Value,
                                null, // PaymentProviderId - can be enhanced later
                                payment.Amount,
                                payment.Method,
                                transactionNumber,
                                payment.Reference,
                                DateTime.UtcNow
                            );

                            // Link to invoice
                            customerPayment.LinkToInvoice(invoice.Id);

                            // Complete the payment immediately for quick sale
                            customerPayment.MarkAsCompleted();
                            customerPayment.MarkAsSettled("System");

                            customerPayment.CreatedBy = "System";
                            customerPayment.ModifiedBy = "System";

                            await _customerPaymentRepository.AddAsync(customerPayment, cancellationToken);
                            totalPaymentAmount += payment.Amount;

                            _logger.LogInformation("Created CustomerPayment {TransactionNumber} for amount {Amount} via {Method}",
                                transactionNumber, payment.Amount, payment.Method);
                        }

                        // Decrease customer balance for payments
                        if (totalPaymentAmount > 0)
                        {
                            customerForBalance.UpdateBalance(-totalPaymentAmount);
                        }
                    }

                    customerForBalance.ModifiedBy = "System";
                    await _customerRepository.UpdateAsync(customerForBalance, cancellationToken);

                    // Reload invoice with customer payments and update status
                    invoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == invoice.Id, cancellationToken);
                    if (invoice != null)
                    {
                        invoice.UpdatePaymentStatus();
                        invoice.ModifiedBy = "System";
                        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                    }

                    // Update sales order paid amount
                    if (totalPaymentAmount > 0)
                    {
                        salesOrder.RecordPayment(totalPaymentAmount);
                        salesOrder.ModifiedBy = "System";
                        await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                    }
                }
            }

            // 7. Update stock levels with movement tracking
            foreach (var item in request.Items)
            {
                var stockLevels = await _stockLevelRepository.GetByPartAsync(item.PartId, cancellationToken);
                var remainingQty = item.Quantity;

                // Decrease from each warehouse that has stock, starting with the one with most stock
                foreach (var stockLevel in stockLevels.OrderByDescending(sl => sl.QuantityAvailable))
                {
                    if (remainingQty <= 0) break;

                    var qtyToDecrease = Math.Min(remainingQty, stockLevel.QuantityAvailable);
                    if (qtyToDecrease > 0)
                    {
                        stockLevel.RemoveStock(qtyToDecrease, "Quick Sale - " + invoiceNumber);
                        stockLevel.ModifiedBy = "System";
                        await _stockLevelRepository.UpdateAsync(stockLevel, cancellationToken);

                        // Create stock movement record for audit trail
                        var stockMovement = StockMovement.Create(
                            stockLevel.Id,
                            "OUT",
                            qtyToDecrease,
                            $"Quick Sale {invoiceNumber}",
                            invoiceNumber
                        );
                        stockMovement.CreatedBy = "System";
                        stockMovement.ModifiedBy = "System";
                        await _dbContext.StockMovements.AddAsync(stockMovement, cancellationToken);

                        // Create stock lot movement for FIFO/LIFO tracking
                        try
                        {
                            var stockLots = await _dbContext.StockLots
                                .Where(l => l.PartId == item.PartId &&
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
                                lot.ModifiedBy = "System";

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
                                lotMovement.CreatedBy = "System";
                                lotMovement.ModifiedBy = "System";
                                await _dbContext.StockLotMovements.AddAsync(lotMovement, cancellationToken);

                                lotRemainingQty -= qtyFromLot;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Could not create stock lot movements for quick sale {InvoiceNumber}, part {PartId}",
                                invoiceNumber, item.PartId);
                            // Continue - stock movement is more important than lot tracking
                        }

                        remainingQty -= qtyToDecrease;
                    }
                }
            }

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
                CreatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Quick sale created successfully. Invoice: {InvoiceNumber}, SO: {SONumber}",
                invoiceNumber, soNumber);

            return CreatedAtAction(nameof(GetById), new { id = salesOrder.Id }, response);
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
