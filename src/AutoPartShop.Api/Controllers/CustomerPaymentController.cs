using AutoPartShop.Application.DTOs.PaymentDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CustomerPaymentController : ControllerBase
{
    private readonly ICustomerPaymentRepository _repository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<CustomerPaymentController> _logger;

    public CustomerPaymentController(
        ICustomerPaymentRepository repository,
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        AutoPartDbContext dbContext,
        ILogger<CustomerPaymentController> logger)
    {
        _repository = repository;
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10, [FromQuery] string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var (payments, totalCount) = string.IsNullOrWhiteSpace(searchTerm)
                ? await _repository.GetPagedAsync(pageNumber, pageSize, cancellationToken)
                : await _repository.SearchPagedAsync(searchTerm, pageNumber, pageSize, cancellationToken);
            var paymentsList = payments.ToList();

            var responses = new List<CustomerPaymentResponse>();
            foreach (var p in paymentsList)
            {
                responses.Add(await MapResponse(p));
            }

            return Ok(new
            {
                data = responses,
                pagination = new
                {
                    pageNumber,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments list");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();
            return Ok(await MapResponse(payment));
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
            var (payments, totalCount) = await _repository.GetByCustomerPagedAsync(customerId, 1, 50, cancellationToken);
            var paymentsList = payments.ToList(); // Materialize the query first

            // Get customer info once
            var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
            var customerName = customer?.GetFullName() ?? "";

            var responses = paymentsList.Select(p => new CustomerPaymentResponse
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
                Notes = p.Notes
            }).ToList();

            return Ok(new { data = responses, totalCount });
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

            // Get all invoices for this customer
            var allInvoices = await _dbContext.Invoices
                .Include(i => i.SalesOrder)
                .Include(i => i.CustomerPayments)
                .Where(i => !i.Isdeleted)
                .ToListAsync(cancellationToken);
            var customerInvoices = allInvoices.Where(i => i.SalesOrder != null && i.SalesOrder.CustomerId == customerId).ToList();

            // Calculate invoice totals
            var totalInvoiceAmount = customerInvoices.Sum(i => i.TotalAmount);
            var totalPaid = completed.Sum(p => p.Amount);
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
                    InvoiceNumber = p.Invoice?.InvoiceNumber ?? string.Empty,
                    TransactionNumber = p.TransactionNumber,
                    ProviderName = p.PaymentProvider?.ProviderName ?? string.Empty
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
                AvailableAdvance = customer.AccountBalance
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
            var payment = CustomerPayment.Create(request.CustomerId, request.PaymentProviderId, request.Amount, request.PaymentMethod, request.TransactionNumber, request.ReferenceNumber, request.PaymentDate);
            if (request.InvoiceId.HasValue)
                payment.LinkToInvoice(request.InvoiceId.Value);
            payment.CreatedBy = "System";
            payment.ModifiedBy = "System";

            // If payment method is CASH, automatically mark as completed and update customer balance
            if (request.PaymentMethod.Trim().ToUpper() == "CASH")
            {
                var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
                if (customer is null)
                    return NotFound(new { message = "Customer not found" });

                payment.MarkAsCompleted();

                // Decrease customer balance (negative because payment reduces debt)
                customer.UpdateBalance(-request.Amount);
                customer.ModifiedBy = "System";

                await _repository.AddAsync(payment, cancellationToken);
                await _customerRepository.UpdateAsync(customer, cancellationToken);

                // Update invoice payment status if payment is linked to an invoice
                if (request.InvoiceId.HasValue)
                {
                    var invoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .FirstOrDefaultAsync(i => i.Id == request.InvoiceId.Value, cancellationToken);

                    if (invoice != null)
                    {
                        invoice.UpdatePaymentStatus();
                        invoice.ModifiedBy = "System";
                        await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                    }
                }
            }
            else
            {
                // For CHECK, BANK_TRANSFER, etc., keep as PENDING until manually marked as complete
                await _repository.AddAsync(payment, cancellationToken);
            }

            return CreatedAtAction(nameof(GetById), new { id = payment.Id }, await MapResponse(payment));
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
            payment.ModifiedBy = "System";

            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            // Get customer and update balance (payment reduces balance)
            var customer = await _customerRepository.GetByIdAsync(payment.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            payment.MarkAsCompleted();
            payment.ModifiedBy = "System";

            // Decrease customer balance (negative because payment reduces debt)
            customer.UpdateBalance(-payment.Amount);
            customer.ModifiedBy = "System";

            await _repository.UpdateAsync(payment, cancellationToken);
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            // Update invoice payment status if payment is linked to an invoice
            if (payment.InvoiceId.HasValue)
            {
                var invoice = await _dbContext.Invoices
                    .Include(i => i.CustomerPayments)
                    .FirstOrDefaultAsync(i => i.Id == payment.InvoiceId.Value, cancellationToken);

                if (invoice != null)
                {
                    invoice.UpdatePaymentStatus();
                    invoice.ModifiedBy = "System";
                    await _invoiceRepository.UpdateAsync(invoice, cancellationToken);
                }
            }

            return Ok(await MapResponse(payment));
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
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
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

            // Get customer and update balance (refund increases balance back)
            var customer = await _customerRepository.GetByIdAsync(payment.CustomerId, cancellationToken);
            if (customer is null) return NotFound(new { message = "Customer not found" });

            payment.MarkAsRefunded(payment.Amount);  // Refund full amount
            payment.ModifiedBy = "System";

            // Increase customer balance (reverting the payment)
            customer.UpdateBalance(payment.Amount);
            customer.ModifiedBy = "System";

            await _repository.UpdateAsync(payment, cancellationToken);
            await _customerRepository.UpdateAsync(customer, cancellationToken);

            return Ok(await MapResponse(payment));
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
            payment.ModifiedBy = "System";
            await _repository.UpdateAsync(payment, cancellationToken);
            return Ok(await MapResponse(payment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payment");
            return StatusCode(500, "An error occurred");
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var payment = await _repository.GetByIdAsync(id, cancellationToken);
            if (payment is null) return NotFound();

            await _repository.DeleteAsync(id, cancellationToken);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment");
            return StatusCode(500, "An error occurred");
        }
    }

    private async Task<CustomerPaymentResponse> MapResponse(CustomerPayment p)
    {
        var customer = await _customerRepository.GetByIdAsync(p.CustomerId);
        return new()
        {
            Id = p.Id,
            CustomerId = p.CustomerId,
            CustomerName = customer?.GetFullName() ?? "",
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
            CreatedAt = DateTime.UtcNow
        };
    }
}
