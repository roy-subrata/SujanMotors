using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerCreditNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using CustomerCreditNoteListQuery = AutoPartShop.Application.DTOs.CustomerCreditNoteDtos.CustomerCreditNoteListQuery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace AutoPartShop.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces("application/json")]
public class CustomerCreditNoteController : ControllerBase
{
    private readonly ICustomerCreditNoteRepository _creditNoteRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ISalesOrderRepository _salesOrderRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICustomerPaymentRepository _customerPaymentRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomerCreditNoteController> _logger;

    public CustomerCreditNoteController(
        ICustomerCreditNoteRepository creditNoteRepository,
        ICustomerRepository customerRepository,
        ISalesOrderRepository salesOrderRepository,
        IInvoiceRepository invoiceRepository,
        ICustomerPaymentRepository customerPaymentRepository,
        AutoPartDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<CustomerCreditNoteController> logger)
    {
        _creditNoteRepository = creditNoteRepository;
        _customerRepository = customerRepository;
        _salesOrderRepository = salesOrderRepository;
        _invoiceRepository = invoiceRepository;
        _customerPaymentRepository = customerPaymentRepository;
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var creditNotes = await _creditNoteRepository.GetByCustomerIdAsync(customerId, cancellationToken);
            var response = creditNotes.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting credit notes for customer {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving credit notes");
        }
    }

    [HttpGet("customer/{customerId:guid}/available")]
    public async Task<IActionResult> GetAvailableCredits(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var creditNotes = await _creditNoteRepository.GetAvailableCreditsAsync(customerId, cancellationToken);
            var response = creditNotes.Select(MapToResponse);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available credits for customer {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving available credits");
        }
    }

    [HttpGet("customer/{customerId:guid}/total-available")]
    public async Task<IActionResult> GetTotalAvailableCredit(Guid customerId, CancellationToken cancellationToken)
    {
        try
        {
            var total = await _creditNoteRepository.GetTotalAvailableCreditAsync(customerId, cancellationToken);
            return Ok(new { totalAvailableCredit = total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting total available credit for customer {CustomerId}", customerId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving total available credit");
        }
    }

    [HttpGet("list")]
    public async Task<IActionResult> GetList([FromQuery] CustomerCreditNoteListQuery query, CancellationToken cancellationToken)
    {
        try
        {
            if (query.PageNumber < 1) query.PageNumber = 1;
            if (query.PageSize < 1) query.PageSize = 10;
            if (query.PageSize > 100) query.PageSize = 100;

            var (creditNotes, totalCount) = await _creditNoteRepository.SearchPagedAsync(
                new Domain.Repositories.CustomerCreditNoteQuery
                {
                    CustomerId = query.CustomerId,
                    Status = query.Status,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize
                }, cancellationToken);

            var response = creditNotes.Select(MapToResponse);
            return Ok(new
            {
                data = response,
                pagination = new
                {
                    query.PageNumber,
                    query.PageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer credit notes list");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving customer credit notes");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(id, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Customer credit note not found" });

            return Ok(MapToResponse(creditNote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer credit note {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the customer credit note");
        }
    }

    [HttpPost("apply")]
    public async Task<IActionResult> ApplyCredit([FromBody] ApplyCustomerCreditNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.CreditNoteId == Guid.Empty)
                return BadRequest(new { message = "CreditNoteId is required" });

            if (request.InvoiceId == Guid.Empty)
                return BadRequest(new { message = "InvoiceId is required" });

            if (request.SalesOrderId == Guid.Empty)
                return BadRequest(new { message = "SalesOrderId is required" });

            if (request.AmountToApply <= 0)
                return BadRequest(new { message = "Amount to apply must be greater than 0" });

            CustomerCreditNote? appliedCreditNote = null;
            string salesOrderNumber = string.Empty;
            decimal remainingAvailableBeforeApply = 0;

            var strategy = _dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
                try
                {
                    var creditNote = await _dbContext.CustomerCreditNotes
                        .Include(cn => cn.Customer)
                        .Include(cn => cn.SalesReturn)
                        .FirstOrDefaultAsync(cn => cn.Id == request.CreditNoteId, cancellationToken);

                    if (creditNote is null)
                        throw new ArgumentException("Customer credit note not found");

                    if (!creditNote.IsAvailable())
                        throw new InvalidOperationException("This credit note is not available for use");

                    if (request.AmountToApply > creditNote.AvailableAmount)
                        throw new InvalidOperationException($"Insufficient credit available. Available: {creditNote.AvailableAmount}");

                    var salesOrder = await _dbContext.SalesOrders
                        .FirstOrDefaultAsync(so => so.Id == request.SalesOrderId && !so.Isdeleted, cancellationToken);
                    if (salesOrder is null)
                        throw new ArgumentException("Sales order not found");

                    if (salesOrder.CustomerId != creditNote.CustomerId)
                        throw new InvalidOperationException("Credit note customer does not match sales order customer");

                    var invoice = await _dbContext.Invoices
                        .Include(i => i.CustomerPayments)
                        .Include(i => i.SalesOrder)
                        .FirstOrDefaultAsync(i => i.Id == request.InvoiceId && !i.Isdeleted, cancellationToken);
                    if (invoice is null)
                        throw new ArgumentException("Invoice not found");

                    if (request.AmountToApply > invoice.OutstandingAmount)
                        throw new InvalidOperationException($"Amount exceeds invoice outstanding amount. Outstanding: {invoice.OutstandingAmount}");

                    remainingAvailableBeforeApply = creditNote.AvailableAmount;
                    creditNote.ApplyToInvoice(request.InvoiceId, request.SalesOrderId, request.AmountToApply);

                    var defaultProvider = await _dbContext.PaymentProviders.FirstOrDefaultAsync(cancellationToken);
                    if (defaultProvider != null)
                    {
                        var customerPayment = CustomerPayment.CreateFromAdvance(
                            customerId: creditNote.CustomerId,
                            invoiceId: request.InvoiceId,
                            sourceAdvancePaymentId: creditNote.Id,
                            paymentProviderId: defaultProvider.Id,
                            amount: request.AmountToApply,
                            description: $"Applied credit note {creditNote.CreditNoteNumber} to invoice"
                        );
                        customerPayment.CreatedBy = _currentUserService.GetCurrentUsername();
                        customerPayment.ModifiedBy = _currentUserService.GetCurrentUsername();
                        _dbContext.CustomerPayments.Add(customerPayment);
                    }

                    salesOrder.RecordPayment(request.AmountToApply);
                    salesOrder.ModifiedBy = _currentUserService.GetCurrentUsername();

                    invoice.UpdatePaymentStatus();
                    invoice.ModifiedBy = _currentUserService.GetCurrentUsername();

                    await _dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);

                    appliedCreditNote = creditNote;
                    salesOrderNumber = salesOrder.SONumber;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            _logger.LogInformation(
                "Customer credit note {CreditNoteNumber} applied to SO {SONumber}, amount: {Amount}, remaining before apply: {Remaining}",
                appliedCreditNote!.CreditNoteNumber, salesOrderNumber, request.AmountToApply, remainingAvailableBeforeApply);

            return Ok(MapToResponse(appliedCreditNote));
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
            _logger.LogError(ex, "Error applying customer credit note");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while applying customer credit note");
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(id, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Customer credit note not found" });

            if (creditNote.UsedAmount > 0)
                return BadRequest(new { message = "Cannot cancel a credit note that has been partially used" });

            creditNote.Cancel(reason);
            await _creditNoteRepository.UpdateAsync(creditNote, cancellationToken);

            return Ok(MapToResponse(creditNote));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling customer credit note {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while cancelling customer credit note");
        }
    }

    private CustomerCreditNoteResponse MapToResponse(CustomerCreditNote cn)
    {
        return new CustomerCreditNoteResponse
        {
            Id = cn.Id,
            CreditNoteNumber = cn.CreditNoteNumber,
            CustomerId = cn.CustomerId,
            CustomerName = cn.Customer?.GetFullName() ?? string.Empty,
            SalesReturnId = cn.SalesReturnId,
            ReturnNumber = cn.SalesReturn?.ReturnNumber,
            InvoiceId = cn.InvoiceId,
            InvoiceNumber = cn.Invoice?.InvoiceNumber,
            SalesOrderId = cn.SalesOrderId,
            SalesOrderNumber = cn.SalesOrder?.SONumber,
            TotalAmount = cn.TotalAmount,
            UsedAmount = cn.UsedAmount,
            AvailableAmount = cn.AvailableAmount,
            Currency = cn.Currency,
            IssueDate = cn.IssueDate,
            ExpiryDate = cn.ExpiryDate,
            Status = cn.Status,
            Notes = cn.Notes,
            IssuedBy = cn.IssuedBy,
            CreatedBy = cn.CreatedBy,
            CreatedAt = cn.CreatedDate
        };
    }
}
