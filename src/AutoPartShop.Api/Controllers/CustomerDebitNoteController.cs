using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.CustomerDebitNoteDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
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
    ICurrencyConversionService currencyService,
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

            var username = currentUserService.GetCurrentUsername();
            CustomerDebitNote debitNote = null!;

            // A debit note is a supplementary bill: it increases what the customer owes immediately
            // (mirror image of a credit note reducing it). Settling the note records the payment
            // and reduces the balance; cancelling an un-settled note reverses this.
            //
            // Persist the note and the balance bump in one transaction and convert before saving so
            // a failed FX lookup or save can't leave an issued note that never hit the balance.
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // Allocated inside the transaction so a failed FX lookup or save rolls the
                    // sequence back rather than burning a number; CodeGenerateService enlists in
                    // the ambient transaction.
                    var debitNoteNumber = await codeGenerateService.GenerateAsync("DN", cancellationToken);

                    debitNote = CustomerDebitNote.Create(
                        debitNoteNumber, request.CustomerId, request.InvoiceId, request.Amount,
                        request.Reason, request.Currency, issueDate: null, request.Notes, username);
                    debitNote.CreatedBy = username;
                    debitNote.ModifiedBy = username;

                    var dnFx = await currencyService.ConvertToBaseWithRateAsync(debitNote.TotalAmount, debitNote.Currency, debitNote.IssueDate, cancellationToken);
                    await debitNoteRepository.AddAsync(debitNote, cancellationToken);
                    customer.UpdateBalance(dnFx.BaseAmount);
                    customer.ModifiedBy = username;
                    await customerRepository.UpdateAsync(customer, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

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
    public async Task<IActionResult> Settle(
        Guid id,
        [FromBody] SettleCustomerDebitNoteRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        var debitNote = await debitNoteRepository.GetByIdAsync(id, cancellationToken);
        if (debitNote is null) return NotFound(new { message = "Customer debit note not found" });

        var customer = await customerRepository.GetByIdAsync(debitNote.CustomerId, cancellationToken);
        if (customer is null) return NotFound(new { message = "Customer not found" });

        try
        {
            var username = currentUserService.GetCurrentUsername();
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    debitNote = await debitNoteRepository.GetByIdAsync(id, cancellationToken)
                        ?? throw new InvalidOperationException("Customer debit note not found");
                    debitNote.MarkAsSettled();
                    debitNote.ModifiedBy = username;

                    // Record the customer's payment so the settlement is more than bookkeeping:
                    // a completed CustomerPayment appears in the payments trail and reduces balance.
                    var defaultProvider = await dbContext.PaymentProviders.FirstOrDefaultAsync(cancellationToken);
                    var paymentMethod = string.IsNullOrWhiteSpace(request?.PaymentMethod) ? "CASH" : request.PaymentMethod.Trim().ToUpper();
                    var payment = CustomerPayment.Create(
                        customerId: debitNote.CustomerId,
                        paymentProviderId: defaultProvider?.Id,
                        amount: debitNote.TotalAmount,
                        paymentMethod: paymentMethod,
                        referenceNumber: string.IsNullOrWhiteSpace(request?.ReferenceNumber) ? debitNote.DebitNoteNumber : request.ReferenceNumber,
                        currency: debitNote.Currency);
                    if (debitNote.InvoiceId.HasValue)
                        payment.LinkToInvoice(debitNote.InvoiceId.Value);
                    var settleFx = await currencyService.ConvertToBaseWithRateAsync(payment.Amount, payment.Currency, debitNote.IssueDate, cancellationToken);
                    payment.SetFxBaseAmount(settleFx.BaseAmount, settleFx.RateToBase);
                    payment.MarkAsSettled(username);
                    payment.CreatedBy = username;
                    payment.ModifiedBy = username;
                    payment.UpdateNotes($"Payment for debit note {debitNote.DebitNoteNumber}. Reason: {debitNote.Reason}");
                    dbContext.CustomerPayments.Add(payment);

                    customer.UpdateBalance(-(payment.BaseAmount ?? payment.Amount));
                    customer.ModifiedBy = username;

                    await debitNoteRepository.UpdateAsync(debitNote, cancellationToken);
                    await customerRepository.UpdateAsync(customer, cancellationToken);
                    await dbContext.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

            return Ok(await MapToResponseAsync(debitNote, cancellationToken));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error settling customer debit note: {NoteId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while settling the debit note");
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
            var username = currentUserService.GetCurrentUsername();
            var wasIssued = debitNote.Status == CustomerDebitNoteStatus.ISSUED;
            debitNote.Cancel(reason ?? string.Empty);
            debitNote.ModifiedBy = username;

            // Cancelling an un-settled note removes the charge it added to the customer's balance.
            // Only reverse when the note actually transitions ISSUED → CANCELLED so a repeated
            // cancel call can't reverse the balance twice, and do it in a transaction so a failed
            // save can't persist a cancelled note with the charge still on the balance.
            var strategy = dbContext.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    if (wasIssued)
                    {
                        var customer = await customerRepository.GetByIdAsync(debitNote.CustomerId, cancellationToken);
                        if (customer is not null)
                        {
                            var cancelFx = await currencyService.ConvertToBaseWithRateAsync(debitNote.TotalAmount, debitNote.Currency, debitNote.IssueDate, cancellationToken);
                            customer.UpdateBalance(-cancelFx.BaseAmount);
                            customer.ModifiedBy = username;
                            await customerRepository.UpdateAsync(customer, cancellationToken);
                        }
                    }

                    await debitNoteRepository.UpdateAsync(debitNote, cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });

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
