using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerDebitNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/customer-debit-notes")]
[Route("api/v1/customer-debit-notes")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class CustomerDebitNoteController(
    ICustomerDebitNoteRepository debitNoteRepository,
    ICustomerRepository customerRepository,
    ICodeGenerateService codeGenerateService,
    ICurrentUserService currentUserService,
    AutoPartDbContext dbContext,
    ILogger<CustomerDebitNoteController> logger) : ControllerBase
{
    [HttpPost]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Create(CreateCustomerDebitNoteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.CustomerId == Guid.Empty)
                return BadRequest(new { message = "CustomerId is required" });

            var customer = await customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
            if (customer is null)
                return BadRequest(new { message = "Customer not found" });

            var debitNoteNumber = await codeGenerateService.GenerateAsync("DN", cancellationToken);
            var username = currentUserService.GetCurrentUsername();

            var debitNote = CustomerDebitNote.Create(
                debitNoteNumber, request.CustomerId, request.InvoiceId, request.Amount,
                request.Reason, request.Currency, issueDate: null, request.Notes, username);
            debitNote.CreatedBy = username;
            debitNote.ModifiedBy = username;

            await debitNoteRepository.AddAsync(debitNote, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = debitNote.Id }, await MapToResponseAsync(debitNote, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating customer debit note");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the debit note");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var debitNote = await debitNoteRepository.GetByIdAsync(id, cancellationToken);
        if (debitNote is null) return NotFound(new { message = "Customer debit note not found" });

        return Ok(await MapToResponseAsync(debitNote, cancellationToken));
    }

    [HttpGet("customer/{customerId:guid}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId, CancellationToken cancellationToken)
    {
        var debitNotes = await debitNoteRepository.GetByCustomerIdAsync(customerId, cancellationToken);
        var responses = new List<CustomerDebitNoteResponse>();
        foreach (var dn in debitNotes)
            responses.Add(await MapToResponseAsync(dn, cancellationToken));

        return Ok(responses);
    }

    [HttpPost("list")]
    public async Task<IActionResult> Search(CustomerDebitNoteQuery query, CancellationToken cancellationToken)
    {
        var (debitNotes, totalCount) = await debitNoteRepository.SearchPagedAsync(query, cancellationToken);
        var responses = new List<CustomerDebitNoteResponse>();
        foreach (var dn in debitNotes)
            responses.Add(await MapToResponseAsync(dn, cancellationToken));

        return Ok(new { data = responses, totalCount, query.PageNumber, query.PageSize });
    }

    [HttpPatch("{id:guid}/settle")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Settle(Guid id, CancellationToken cancellationToken)
    {
        var debitNote = await debitNoteRepository.GetByIdAsync(id, cancellationToken);
        if (debitNote is null) return NotFound(new { message = "Customer debit note not found" });

        try
        {
            debitNote.MarkAsSettled();
            debitNote.ModifiedBy = currentUserService.GetCurrentUsername();
            await debitNoteRepository.UpdateAsync(debitNote, cancellationToken);
            return Ok(await MapToResponseAsync(debitNote, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPatch("{id:guid}/cancel")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] string? reason, CancellationToken cancellationToken)
    {
        var debitNote = await debitNoteRepository.GetByIdAsync(id, cancellationToken);
        if (debitNote is null) return NotFound(new { message = "Customer debit note not found" });

        try
        {
            debitNote.Cancel(reason ?? string.Empty);
            debitNote.ModifiedBy = currentUserService.GetCurrentUsername();
            await debitNoteRepository.UpdateAsync(debitNote, cancellationToken);
            return Ok(await MapToResponseAsync(debitNote, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Download the customer Debit Note as a PDF, rendered through the same document class as the
    /// Credit Note — the handoff specifies both directions share one layout.
    /// </summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken)
    {
        var debitNote = await dbContext.Set<CustomerDebitNote>()
            .AsNoTracking()
            .Include(dn => dn.Customer)
            .Include(dn => dn.Invoice)
            .FirstOrDefaultAsync(dn => dn.Id == id && !dn.Isdeleted, cancellationToken);

        if (debitNote is null)
            return NotFound(new { message = "Customer debit note not found" });

        var currency = await dbContext.Set<Currency>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == debitNote.Currency && !c.Isdeleted, cancellationToken);
        var shop = await shopProfiles.GetAsync(currency?.Symbol, cancellationToken);

        var customer = debitNote.Customer;
        var customerName = customer is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(customer.CompanyName)
                ? customer.CompanyName
                : $"{customer.FirstName} {customer.LastName}".Trim();
        var address = customer is null
            ? string.Empty
            : string.Join(", ", new[] { customer.BillingAddress, customer.City, customer.PostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        var data = new CreditNoteDocumentData(
            CreditNoteNumber: debitNote.DebitNoteNumber,
            IssueDate: debitNote.IssueDate,
            RefInvoiceNumber: debitNote.Invoice?.InvoiceNumber ?? string.Empty,
            CustomerName: customerName,
            CustomerAddress: address,
            CustomerPhone: customer?.Phone ?? string.Empty,
            Reason: debitNote.Reason,
            // A debit note is a flat correction, not an itemized return — one summary line, same
            // as a credit note with no linked return (warranty refunds, standalone adjustments).
            Lines: [new CreditNoteLine(1, "—", "Debit adjustment", null, 1, "", debitNote.TotalAmount, debitNote.TotalAmount)],
            TotalCredit: debitNote.TotalAmount,
            Notes: debitNote.Notes,
            IsDebit: true);

        var pdfBytes = new CreditNoteDocument(data, shop).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"debit-note-{debitNote.DebitNoteNumber}.pdf");
    }

    private async Task<CustomerDebitNoteResponse> MapToResponseAsync(CustomerDebitNote dn, CancellationToken cancellationToken)
    {
        var customer = dn.Customer ?? await customerRepository.GetByIdAsync(dn.CustomerId, cancellationToken);
        var customerName = customer is null
            ? string.Empty
            : !string.IsNullOrWhiteSpace(customer.CompanyName)
                ? customer.CompanyName
                : $"{customer.FirstName} {customer.LastName}".Trim();

        return new CustomerDebitNoteResponse
        {
            Id = dn.Id,
            DebitNoteNumber = dn.DebitNoteNumber,
            CustomerId = dn.CustomerId,
            CustomerName = customerName,
            InvoiceId = dn.InvoiceId,
            InvoiceNumber = dn.Invoice?.InvoiceNumber,
            TotalAmount = dn.TotalAmount,
            Currency = dn.Currency,
            IssueDate = dn.IssueDate,
            Reason = dn.Reason,
            Status = dn.Status,
            Notes = dn.Notes,
            IssuedBy = dn.IssuedBy,
            CreatedAt = dn.CreatedDate
        };
    }
}
