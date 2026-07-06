using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.CustomerPayment;
using AutoPartShop.Application.CustomerPayment.Dtos;
using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/customer-payments")]
[Route("api/v1/customer-payments")]
[ApiController]
[Authorize]
public class CustomerPaymentController : ControllerBase
{
    private readonly ICustomerPaymentRepository _repository;
    private readonly ICustomerPaymentReadRepository _customerPaymentReadRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IApplicationSettingsRepository _settingsRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomerPaymentController> _logger;

    public CustomerPaymentController(
        ICustomerPaymentRepository repository,
        ICustomerPaymentReadRepository customerPaymentReadRepository,
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        ISalesOrderRepository salesOrderRepository,
        IApplicationSettingsRepository settingsRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CustomerPaymentController> logger)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _customerPaymentReadRepository = customerPaymentReadRepository;
        _invoiceRepository = invoiceRepository;
        _salesOrderRepository = salesOrderRepository;
        _settingsRepository = settingsRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }


    [HttpPost("list")]
    public async Task<IActionResult> FindAll([FromBody] CustomerPaymentQuery query, CancellationToken cancellation)
    {
        if (query == null)
        {
            return BadRequest("Query parameters are required.");
        }

        if (query.PageNumber <= 0 || query.PageSize <= 0)
        {
            return BadRequest("Invalid pagination parameters.");
        }

        var (payments, totalCount) = await _customerPaymentReadRepository.FindAllAsync(query, cancellation);

        return Ok(PagedResult<Application.CustomerPayment.Dtos.CustomerPaymentResponse>.Create(
            payments.ToList(),
            totalCount,
            query
        ));
    }



    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var payments = await _repository.GetByCustomerAsync(customerId, cancellationToken);
            var paymentsList = payments.ToList(); // Materialize the query first

            // Get customer info once
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            var customerName = customer?.GetFullName() ?? "";

            var responses = paymentsList.Select(p => new Application.CustomerPayment.Dtos.CustomerPaymentResponse
            {
                Id = p.Id,
                CustomerId = p.CustomerId,
                CustomerName = customerName,
                InvoiceId = p.InvoiceId,
                PaymentProviderId = p.PaymentProviderId,
                ProviderName = p.PaymentProvider?.ProviderName ?? string.Empty,
                InvoiceNumber = p.Invoice?.InvoiceNumber ?? string.Empty,
                TransactionNumber = p.TransactionNumber,
                Amount = p.Amount,
                PaymentFee = p.PaymentFee,
                NetAmount = p.NetAmount,
                PaymentMethod = p.PaymentMethod,
                PaymentDate = p.PaymentDate,
                ReferenceNumber = p.ReferenceNumber,
                Status = p.Status,
                Notes = p.Notes,
                PaymentType = p.PaymentType.ToString(),
                RemainingAmount = p.RemainingAmount,
                SourceAdvancePaymentId = p.SourceAdvancePaymentId,
                CreatedAt = p.CreatedDate
            }).ToList();

            return Ok(new { data = responses, totalCount = paymentsList.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer payments");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("customer/{customerId:guid}/summary")]
    public async Task<IActionResult> GetSummary(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            if (customer is null) return NotFound();

            var payments = await _repository.GetByCustomerAsync(customerId, cancellationToken);
            var completed = payments.Where(p => p.Status == "COMPLETED").ToList();
            var pending = payments.Where(p => p.Status == "PENDING").ToList();
            var failed = payments.Where(p => p.Status == "FAILED").ToList();

            // Get invoices for this customer directly via sales order relationship
            var customerInvoices = await _dbContext.Invoices
                .Include(i => i.SalesOrder)
                .Include(i => i.CustomerPayments)
                .Where(i => !i.Isdeleted && i.SalesOrder != null && i.SalesOrder.CustomerId == customerId
                            && i.SalesOrder.Status != "CANCELLED"
                            && i.SalesOrder.Status != "RETURNED"
                            && i.SalesOrder.Status != "DRAFT")
                .ToListAsync(cancellationToken);

            // Calculate invoice totals
            var totalInvoiceAmount = customerInvoices.Sum(i => i.TotalAmount);

            // Total Paid: Sum of completed payments that represent NEW money received
            // Includes ADVANCE payments (original advance amount)
            // Excludes REGULAR payments created from advance (to prevent double-counting)
            // Includes negative amounts for refunds, which correctly reduces the total
            var totalPaid = completed
                .Where(p => p.PaymentType == CustomerPaymentType.ADVANCE || p.SourceAdvancePaymentId == null)
                .Sum(p => p.Amount);

            var totalOutstanding = customerInvoices.Sum(i => i.OutstandingAmount);
            var unpaidInvoices = customerInvoices.Count(i => i.OutstandingAmount > 0);

            // Calculate overdue invoices (invoices past due date with outstanding balance)
            var today = DateTime.UtcNow.Date;
            var overdueInvoices = customerInvoices.Count(i =>
                i.OutstandingAmount > 0 &&
                i.DueDate.Date < today);

            // Build payment history from all payments
            var paymentHistory = payments
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new PaymentHistoryItem
                {
                    Id = p.Id,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate,
                    Status = p.Status,
                    PaymentMethod = p.PaymentMethod,
                    PaymentType = (PaymentType)(int)p.PaymentType, // Convert CustomerPaymentType to PaymentType enum
                    InvoiceNumber = p.Invoice?.InvoiceNumber ?? string.Empty,
                    TransactionNumber = p.TransactionNumber,
                    ProviderName = p.PaymentMethod == "ADVANCE_CREDIT"
                        ? "Advance Credit"
                        : (p.PaymentProvider?.ProviderName ?? string.Empty),
                    SourceAdvancePaymentId = p.SourceAdvancePaymentId,
                    SourceAdvanceTransactionNumber = p.SourceAdvancePayment?.TransactionNumber
                })
                .ToList();

            return Ok(new CustomerPaymentHistorySummary
            {
                CustomerId = customerId,
                CustomerName = customer.GetFullName(),
                TotalPaid = totalPaid,
                TotalFees = completed.Sum(p => p.PaymentFee),
                CompletedPayments = completed.Count,
                PendingPayments = pending.Count,
                FailedPayments = failed.Count,
                LastPaymentDate = completed.OrderByDescending(p => p.PaymentDate).FirstOrDefault()?.PaymentDate,
                LastPaymentAmount = completed.OrderByDescending(p => p.PaymentDate).FirstOrDefault()?.Amount ?? 0,

                // Invoice and Outstanding Information
                TotalInvoiceAmount = totalInvoiceAmount,
                TotalOutstanding = totalOutstanding,
                AmountDue = totalOutstanding, // Same as total outstanding
                TotalInvoices = customerInvoices.Count,
                UnpaidInvoices = unpaidInvoices,
                OverdueInvoices = overdueInvoices,

                PaymentHistory = paymentHistory,
                AvailableAdvance = customer.AdvanceAmount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment summary");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCustomerPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate payment method
            if (string.IsNullOrWhiteSpace(request.PaymentMethod))
                return BadRequest(new { message = "Payment method is required" });

            var payment = CustomerPayment.Create(request.CustomerId, request.PaymentProviderId, request.Amount, request.PaymentMethod, request.TransactionNumber, request.ReferenceNumber, request.PaymentDate, request.Currency);
            if (request.InvoiceId.HasValue)
                payment.LinkToInvoice(request.InvoiceId.Value);
            payment.CreatedBy = _currentUserService.GetCurrentUsername();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            // If payment method is CASH, automatically mark as completed and update customer balance
            if (request.PaymentMethod.Trim().ToUpper() == "CASH")
            {
                payment.MarkAsCompleted();

                var strategy = _dbContext.Database.CreateExecutionStrategy();
                await strategy.ExecuteAsync(async () =>
                {
                    await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                    try
                    {
                        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
                        if (customer is null)
                            throw new InvalidOperationException("Customer not found");

                        // Decrease customer balance (negative because payment reduces debt)
                        customer.UpdateBalance(-request.Amount);
                        customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                        await _repository.AddAsync(payment, cancellationToken);
                        await _customerRepository.UpdateAsync(customer, cancellationToken);

                        // Update invoice payment status and sales order if payment is linked to an invoice
                        if (request.InvoiceId.HasValue)
                        {
                            var invoice = await _dbContext.Invoices
                                .Include(i => i.CustomerPayments)
                                .Include(i => i.SalesOrder)
                                .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value, cancellationToken);

                            if (invoice != null)
                            {
                                invoice.UpdatePaymentStatus();
                                invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                                // Update sales order paid amount
                                if (invoice.SalesOrder != null)
                                {
                                    invoice.SalesOrder.RecordPayment(request.Amount);
                                    invoice.SalesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                                    await _salesOrderRepository.UpdateAsync(invoice.SalesOrder, cancellationToken);
                                }
                            }
                        }

                        await transaction.CommitAsync(cancellationToken);
                    }
                    catch
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        throw;
                    }
                });
            }
            else
            {
                // For CHECK, BANK_TRANSFER, etc., keep as PENDING until manually marked as complete
                await _repository.AddAsync(payment, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, MapResponse(payment));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerPaymentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            payment.UpdateReferenceNumber(request.ReferenceNumber);
            payment.SetAuthorizationCode(request.AuthorizationCode);
            payment.UpdateNotes(request.Notes);
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/mark-completed")]
    public async Task<IActionResult> MarkCompleted(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            payment.MarkAsCompleted();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var customer = await _customerRepository.GetByIdAsync(payment.CustomerId, cancellationToken);
                    if (customer is null)
                        throw new InvalidOperationException("Customer not found");

                    // Decrease customer balance (negative because payment reduces debt)
                    customer.UpdateBalance(-payment.Amount);
                    customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                    await _repository.UpdateAsync(payment, cancellationToken);
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    // Update invoice payment status if payment is linked to an invoice
                    if (payment.InvoiceId.HasValue)
                    {
                        var invoice = await _dbContext.Invoices
                            .Include(i => i.CustomerPayments)
                            .Include(i => i.SalesOrder)
                            .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, cancellationToken);

                        if (invoice != null)
                        {
                            invoice.UpdatePaymentStatus();
                            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                            // Update sales order paid amount
                            if (invoice.SalesOrder != null)
                            {
                                invoice.SalesOrder.RecordPayment(payment.Amount);
                                invoice.SalesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _salesOrderRepository.UpdateAsync(invoice.SalesOrder, cancellationToken);
                            }
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking payment completed");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/reconcile")]
    public async Task<IActionResult> Reconcile(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            payment.Reconcile();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reconciling payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/refund")]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            if (payment.Status == "REFUNDED")
                return BadRequest(new { message = "Payment has already been refunded" });

            if (payment.Status != "COMPLETED")
                return BadRequest(new { message = $"Only completed payments can be refunded. Current status: {payment.Status}" });

            if (payment.Amount <= 0 || payment.PaymentMethod == "REFUND")
                return BadRequest(new { message = "This payment is not eligible for refund" });

            if (payment.PaymentType == CustomerPaymentType.ADVANCE)
            {
                var derivedPayments = await _dbContext.CustomerPayments
                    .Where(p => p.SourceAdvancePaymentId == payment.Id && p.Status == "COMPLETED" && !p.Isdeleted)
                    .AnyAsync(cancellationToken);
                if (derivedPayments)
                    return BadRequest(new { message = "Cannot refund an advance payment that has already been applied to invoices. Reverse the invoice applications first." });
            }

            payment.MarkAsRefunded(payment.Amount);  // Refund full amount
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var customer = await _customerRepository.GetByIdAsync(payment.CustomerId, cancellationToken);
                    if (customer is null)
                        throw new InvalidOperationException("Customer not found");

                    // Increase customer balance (reverting the payment)
                    customer.UpdateBalance(payment.Amount);
                    customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                    await _repository.UpdateAsync(payment, cancellationToken);
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    // Update invoice and sales order if payment was linked
                    if (payment.InvoiceId.HasValue)
                    {
                        var invoice = await _dbContext.Invoices
                            .Include(i => i.CustomerPayments)
                            .Include(i => i.SalesOrder)
                            .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, cancellationToken);

                        if (invoice != null)
                        {
                            invoice.UpdatePaymentStatus();
                            invoice.ModifiedBy = _currentUserService.GetCurrentUsername();
                            await _invoiceRepository.UpdateAsync(invoice, cancellationToken);

                            // Reverse payment on sales order
                            if (invoice.SalesOrder != null)
                            {
                                invoice.SalesOrder.ProcessRefund(payment.Amount);
                                invoice.SalesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();
                                await _salesOrderRepository.UpdateAsync(invoice.SalesOrder, cancellationToken);
                            }
                        }
                    }

                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refunding payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            payment.Cancel();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpPatch("{id:guid}/mark-advance")]
    public async Task<IActionResult> MarkAsAdvance(Guid id, [FromBody] MarkAsCustomerPaymentAdvanceRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Customer payment not found" });

            payment.MarkAsAdvance();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking customer payment as advance");
            return StatusCode(500, new { message = "An error occurred while marking payment as advance" });
        }
    }

    [HttpPatch("{id:guid}/mark-regular")]
    public async Task<IActionResult> MarkAsRegular(Guid id, [FromBody] MarkAsCustomerPaymentRegularRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound(new { message = "Customer payment not found" });

            payment.MarkAsRegular();
            payment.ModifiedBy = _currentUserService.GetCurrentUsername();
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(MapResponse(payment));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking customer payment as regular");
            return StatusCode(500, new { message = "An error occurred while marking payment as regular" });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            // Prevent deleting payments that have already affected balances
            if (payment.Status == "COMPLETED" || payment.Status == "REFUNDED")
                return BadRequest(new { message = $"Cannot delete a {payment.Status} payment. Cancel or refund it instead." });

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment");
            return StatusCode(500, "An error occurred");
        }
    }



    /// <summary>
    /// Download a Payment Receipt PDF for a single payment.
    /// </summary>
    [HttpGet("{id:guid}/receipt")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPaymentReceipt(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            var mapped = MapResponse(payment);

            var businessSettings = await _settingsRepository.GetByCategoryAsync("BUSINESS", cancellationToken);

            string Get(string key, string fallback = "")
            {
                var v = businessSettings.FirstOrDefault(s => s.Key == key && !s.Isdeleted)?.Value;
                return string.IsNullOrWhiteSpace(v) ? fallback : v;
            }

            var currencyEntity = await _dbContext.Set<Currency>()
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Code == mapped.Currency && !c.Isdeleted, cancellationToken);
            string currencySymbol = currencyEntity?.Symbol ?? mapped.Currency;

            var shopProfile = new ShopProfile(
                Name: Get("SHOP_NAME"),
                Address: Get("SHOP_ADDRESS"),
                Phone: Get("SHOP_PHONE"),
                Email: Get("SHOP_EMAIL"),
                TaxNo: Get("SHOP_TAX_NUMBER"),
                Tagline: Get("SHOP_TAGLINE"),
                FooterText: Get("INVOICE_FOOTER_TEXT", "Thank you for your payment."),
                CurrencySymbol: currencySymbol);

            var document = new PaymentReceiptDocument(mapped, shopProfile);
            var pdfBytes = document.GeneratePdf();

            var filename = $"receipt-{mapped.TransactionNumber}-{DateTime.UtcNow:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment receipt for payment: {PaymentId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while generating the payment receipt" });
        }
    }

    private Application.CustomerPayment.Dtos.CustomerPaymentResponse MapResponse(CustomerPayment p)
    {
        return new()
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            CustomerName = p.Customer?.GetFullName() ?? "",
            InvoiceId = p.InvoiceId,
            InvoiceNumber = p.Invoice?.InvoiceNumber ?? string.Empty,
            PaymentProviderId = p.PaymentProviderId,
            ProviderName = p.PaymentProvider?.ProviderName ?? string.Empty,
            TransactionNumber = p.TransactionNumber,
            Amount = p.Amount,
            PaymentFee = p.PaymentFee,
            NetAmount = p.NetAmount,
            Currency = p.Currency,
            PaymentDate = p.PaymentDate,
            PaymentMethod = p.PaymentMethod,
            Status = p.Status,
            ReferenceNumber = p.ReferenceNumber,
            AuthorizationCode = p.AuthorizationCode,
            Notes = p.Notes,
            SettledDate = p.SettledDate,
            SettledBy = p.SettledBy,
            IsReconciled = p.IsReconciled,
            ReconciledDate = p.ReconciledDate,
            PaymentType = p.PaymentType.ToString(),
            RemainingAmount = p.RemainingAmount,
            SourceAdvancePaymentId = p.SourceAdvancePaymentId,
            CreatedAt = p.CreatedDate
        };
    }

    /// <summary>
    /// Get available advance payments for a customer
    /// </summary>
    [HttpGet("customer/{customerId:guid}/available-advances")]
    public async Task<IActionResult> GetAvailableAdvances(Guid customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _repository.GetByCustomerAsync(customerId, cancellationToken);

            var availableAdvances = payments
                .Where(p => p.PaymentType == CustomerPaymentType.ADVANCE &&
                           p.Status == "COMPLETED" &&
                           p.RemainingAmount > 0)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new AvailableCustomerAdvancePayment
                {
                    Id = p.Id,
                    TransactionNumber = p.TransactionNumber,
                    Amount = p.Amount,
                    RemainingAmount = p.RemainingAmount,
                    PaymentDate = p.PaymentDate,
                    Description = p.Notes
                })
                .ToList();

            return Ok(availableAdvances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available advances for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "An error occurred while retrieving available advances" });
        }
    }

    /// <summary>
    /// Apply advance credit to an invoice
    /// </summary>
    [HttpPost("apply-advance-credit")]
    public async Task<IActionResult> ApplyAdvanceCredit([FromBody] ApplyCustomerAdvanceCreditRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate request
            if (request.InvoiceId == Guid.Empty)
                return BadRequest(new { message = "Invoice ID is required" });

            if (request.SourceAdvancePaymentId == Guid.Empty)
                return BadRequest(new { message = "Source advance payment ID is required" });

            if (request.Amount <= 0)
                return BadRequest(new { message = "Amount must be greater than zero" });

            // Get the advance payment
            var advancePayment = await _repository.GetByIdAsync(request.SourceAdvancePaymentId, cancellationToken);
            if (advancePayment == null)
                return NotFound(new { message = "Advance payment not found" });

            if (advancePayment.PaymentType != CustomerPaymentType.ADVANCE)
                return BadRequest(new { message = "Source payment is not an advance payment" });

            if (advancePayment.RemainingAmount < request.Amount)
                return BadRequest(new { message = $"Insufficient advance balance. Available: {advancePayment.RemainingAmount}, Requested: {request.Amount}" });

            // Get the invoice
            var invoice = await _invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken);
            if (invoice == null)
                return NotFound(new { message = "Invoice not found" });

            // Get the sales order to verify customer
            var salesOrder = await _salesOrderRepository.GetByIdAsync(invoice.SalesOrderId, cancellationToken);
            if (salesOrder == null)
                return NotFound(new { message = "Sales order not found" });

            if (salesOrder.CustomerId != advancePayment.CustomerId)
                return BadRequest(new { message = "Invoice customer does not match advance payment customer" });

            if (request.Amount > invoice.OutstandingAmount)
                return BadRequest(new { message = $"Amount exceeds invoice outstanding amount. Outstanding: {invoice.OutstandingAmount}" });

            // Get the customer
            var customer = await _customerRepository.GetByIdAsync(advancePayment.CustomerId, cancellationToken);
            if (customer == null)
                return NotFound(new { message = "Customer not found" });

            // Create new payment from advance
            var newPayment = CustomerPayment.CreateFromAdvance(
                advancePayment.CustomerId,
                request.InvoiceId,
                request.SourceAdvancePaymentId,
                advancePayment.PaymentProviderId,
                request.Amount,
                request.Description ?? $"Applied from advance payment {advancePayment.TransactionNumber}"
            );

            newPayment.CreatedBy = _currentUserService.GetCurrentUsername();

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            ApplyCustomerAdvanceCreditResponse? creditResponse = null;
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Reduce the remaining amount on the advance payment
                    advancePayment.ReduceRemainingAmount(request.Amount);
                    advancePayment.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Update sales order paid amount (CRITICAL - matches supplier implementation)
                    salesOrder.RecordPayment(request.Amount);
                    salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Reflect the new payment in the in-memory collection before recalculating invoice status —
                    // newPayment is not yet in the DB so UpdatePaymentStatus would miss it otherwise.
                    invoice.CustomerPayments.Add(newPayment);
                    invoice.UpdatePaymentStatus();
                    invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Update customer balance (reduce the amount owed)
                    customer.UpdateBalance(-request.Amount);
                    customer.ModifiedBy = _currentUserService.GetCurrentUsername();

                    // Save all changes
                    await _repository.AddAsync(newPayment, cancellationToken);
                    await _repository.UpdateAsync(advancePayment, cancellationToken);
                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                    await _salesOrderRepository.UpdateAsync(salesOrder, cancellationToken);
                    await _customerRepository.UpdateAsync(customer, cancellationToken);

                    await transaction.CommitAsync(cancellationToken);

                    creditResponse = new ApplyCustomerAdvanceCreditResponse
                    {
                        PaymentId = newPayment.Id,
                        TransactionNumber = newPayment.TransactionNumber,
                        AmountApplied = request.Amount,
                        RemainingAdvanceBalance = advancePayment.RemainingAmount,
                        Message = $"Successfully applied {request.Amount:C} from advance to invoice {invoice.InvoiceNumber}"
                    };
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(creditResponse);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when applying advance credit");
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "The advance payment was modified by another transaction. Please retry." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying advance credit");
            return StatusCode(500, new { message = "An error occurred while applying advance credit" });
        }
    }
}
