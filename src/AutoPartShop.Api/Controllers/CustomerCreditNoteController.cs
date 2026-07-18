using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerCreditNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using CustomerCreditNoteListQuery = AutoPartShop.Application.DTOs.CustomerCreditNoteDtos.CustomerCreditNoteListQuery;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using System.Data;

namespace AutoPartShop.Api.Controllers;

[Route("api/customer-credit-notes")]
[Route("api/v1/customer-credit-notes")]
[ApiController]
[HasPermission(Permissions.SalesView)]
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

    /// <summary>Download the customer Credit Note as a PDF.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken)
    {
        var creditNote = await _dbContext.Set<CustomerCreditNote>()
            .AsNoTracking()
            .Include(cn => cn.Customer)
            .Include(cn => cn.SalesReturn!).ThenInclude(sr => sr.LineItems).ThenInclude(l => l.Part)
            .Include(cn => cn.SalesReturn!).ThenInclude(sr => sr.LineItems).ThenInclude(l => l.Unit)
            .Include(cn => cn.Invoice)
            .FirstOrDefaultAsync(cn => cn.Id == id && !cn.Isdeleted, cancellationToken);

        if (creditNote is null)
            return NotFound(new { message = "Customer credit note not found" });

        var currency = await _dbContext.Set<Currency>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == creditNote.Currency && !c.Isdeleted, cancellationToken);
        var shop = await shopProfiles.GetAsync(currency?.Symbol, cancellationToken);

        var customer = creditNote.Customer;
        var customerName = customer is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(customer.CompanyName)
                ? customer.CompanyName
                : $"{customer.FirstName} {customer.LastName}".Trim();
        var address = customer is null
            ? string.Empty
            : string.Join(", ", new[] { customer.BillingAddress, customer.City, customer.PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        // Part numbers aren't on the return line, so fetch them for the parts in play.
        var returnLines = creditNote.SalesReturn?.LineItems.ToList() ?? [];
        var partIds = returnLines.Select(l => l.PartId).Distinct().ToList();
        var partNumbers = await _dbContext.Set<Product>()
            .AsNoTracking()
            .Where(p => partIds.Contains(p.Id))
            .Select(p => new { p.Id, PartNumber = p.PartNumber!.Value })
            .ToDictionaryAsync(p => p.Id, p => p.PartNumber, cancellationToken);

        List<CreditNoteLine> lines;
        if (returnLines.Count > 0)
        {
            lines = returnLines
                .Select((l, i) => new CreditNoteLine(
                    SlNo: i + 1,
                    PartNumber: partNumbers.TryGetValue(l.PartId, out var pn) ? pn : (l.Part?.SKU ?? string.Empty),
                    DisplayName: l.Part?.Name ?? string.Empty,
                    LocalName: l.Part?.LocalName,
                    Quantity: l.Quantity,
                    UnitSymbol: l.Unit?.Symbol ?? string.Empty,
                    UnitPrice: l.UnitPrice,
                    LineTotal: l.RefundAmount))
                .ToList();
        }
        else
        {
            // Warranty refunds and standalone credits have no return lines — show one summary line
            // so the total is still itemised rather than appearing from nowhere.
            lines =
            [
                new CreditNoteLine(1, "—", "Credit adjustment", null, 1, "", creditNote.TotalAmount, creditNote.TotalAmount)
            ];
        }

        var data = new CreditNoteDocumentData(
            CreditNoteNumber: creditNote.CreditNoteNumber,
            IssueDate: creditNote.IssueDate,
            RefInvoiceNumber: creditNote.Invoice?.InvoiceNumber ?? string.Empty,
            CustomerName: customerName,
            CustomerAddress: address,
            CustomerPhone: customer?.Phone ?? string.Empty,
            Reason: FormatReason(creditNote.SalesReturn?.Reason),
            Lines: lines,
            TotalCredit: creditNote.TotalAmount,
            Notes: creditNote.Notes);

        var pdfBytes = new CreditNoteDocument(data, shop).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"credit-note-{creditNote.CreditNoteNumber}.pdf");
    }

    private static string FormatReason(string? reason) => reason switch
    {
        null or "" => "",
        "DAMAGED" => "Goods returned — damaged.",
        "DEFECTIVE" => "Goods returned — manufacturing defect.",
        "WRONG_ITEM" => "Goods returned — wrong item supplied.",
        "EXCESS_STOCK" => "Goods returned — excess stock.",
        _ => reason.Replace('_', ' ')
    };

    [HttpPost("apply")]
    [HasPermission(Permissions.SalesProcessPayment)]
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
    [HasPermission(Permissions.SalesProcessPayment)]
    public async Task<IActionResult> Cancel(Guid id, [FromQuery] string reason = "", CancellationToken cancellationToken = default)
    {
        try
        {
            var creditNote = await _creditNoteRepository.GetByIdAsync(id, cancellationToken);
            if (creditNote is null) return NotFound(new { message = "Customer credit note not found" });

            if (creditNote.UsedAmount > 0)
                return BadRequest(new { message = "Cannot cancel a credit note that has been partially used" });

            creditNote.ModifiedBy = _currentUserService.GetCurrentUsername();
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
