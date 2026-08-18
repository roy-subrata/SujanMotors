using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Enums;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Service for calculating and retrieving customer ledger data.
/// Combines Invoices, CustomerPayments, and processed SalesReturns into a unified ledger view.
/// Queries AutoPartDbContext directly (same approach as the neighbouring
/// CustomerAccountSummaryService) rather than via repositories, since the customer-scoped
/// totals here are computed properties (Invoice.GrandTotal) that need in-memory summation
/// the same way that service already does.
/// </summary>
public class CustomerLedgerService : ICustomerLedgerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly AutoPartDbContext _dbContext;
    private readonly ILogger<CustomerLedgerService> _logger;

    public CustomerLedgerService(
        ICustomerRepository customerRepository,
        AutoPartDbContext dbContext,
        ILogger<CustomerLedgerService> logger)
    {
        _customerRepository = customerRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CustomerLedgerSummaryDto> GetLedgerSummaryAsync(
        Guid customerId, int entryLimit = 20, CancellationToken ct = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, ct);
        if (customer == null)
            throw new InvalidOperationException($"Customer with ID {customerId} not found");

        var totalInvoiced = await GetTotalInvoicedAsync(customerId, ct);
        var totalPayments = await GetTotalPaymentsAsync(customerId, ct);
        var totalRefunds = await GetTotalRefundsAsync(customerId, ct);
        var totalDebitNotes = await GetTotalDebitNotesAsync(customerId, ct);
        var totalCreditNotes = await GetTotalCreditNotesAppliedAsync(customerId, ct);
        var advanceCredit = await GetAvailableAdvanceCreditAsync(customerId, ct);

        // A processed return is recognised exactly once, on the invoice: SalesReturnController
        // credits it via Invoice.ApplyReturnCredit, so GrandTotal (and therefore totalInvoiced)
        // is already net of returned goods. The cash that went back out is a negative payment.
        // Subtracting totalRefunds on top counted every return twice — it is reported below for
        // information, not used in the balance.
        var currentBalance = totalInvoiced - totalPayments + totalDebitNotes - totalCreditNotes;

        var entries = await GetLedgerEntriesAsync(customerId, null, null, ct);

        // Calculate running balances over the FULL history first so each entry's balance
        // carries the prior balance forward, then take the most recent N for display.
        CalculateRunningBalances(entries);
        var recentEntries = entries.OrderByDescending(e => e.TransactionDate).Take(entryLimit).ToList();

        return new CustomerLedgerSummaryDto
        {
            CustomerId = customerId,
            CustomerName = customer.GetFullName(),
            CustomerCode = customer.CustomerCode,
            TotalInvoiced = totalInvoiced,
            TotalPayments = totalPayments,
            TotalRefunds = totalRefunds,
            TotalDebitNotes = totalDebitNotes,
            TotalCreditNotesApplied = totalCreditNotes,
            AvailableAdvanceCredit = advanceCredit,
            CurrentBalance = currentBalance,
            TransactionCount = entries.Count,
            LastTransactionDate = entries.MaxBy(e => e.TransactionDate)?.TransactionDate,
            Entries = recentEntries
        };
    }

    public async Task<decimal> CalculateCurrentBalanceAsync(Guid customerId, CancellationToken ct = default)
    {
        var totalInvoiced = await GetTotalInvoicedAsync(customerId, ct);
        var totalPayments = await GetTotalPaymentsAsync(customerId, ct);
        var totalDebitNotes = await GetTotalDebitNotesAsync(customerId, ct);
        var totalCreditNotes = await GetTotalCreditNotesAppliedAsync(customerId, ct);

        // See GetSummaryAsync: returns are netted on the invoice, refunds are negative payments.
        return totalInvoiced - totalPayments + totalDebitNotes - totalCreditNotes;
    }

    public async Task<PagedCustomerLedgerResult> GetLedgerEntriesAsync(
        CustomerLedgerQueryDto query, CancellationToken ct = default)
    {
        var allEntries = await GetLedgerEntriesAsync(
            query.CustomerId, query.FromDate, query.ToDate, ct);

        if (query.TransactionType.HasValue)
        {
            allEntries = allEntries
                .Where(e => e.TransactionType == query.TransactionType.Value)
                .ToList();
        }

        allEntries = allEntries.OrderByDescending(e => e.TransactionDate).ToList();

        CalculateRunningBalances(allEntries);

        var totalCount = allEntries.Count;
        var pagedEntries = allEntries
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new PagedCustomerLedgerResult
        {
            Entries = pagedEntries,
            TotalCount = totalCount,
            PageNumber = query.PageNumber,
            PageSize = query.PageSize
        };
    }

    public async Task<List<CustomerLedgerEntryDto>> GetLedgerEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct = default)
    {
        var entries = new List<CustomerLedgerEntryDto>();

        entries.AddRange(await GetInvoiceEntriesAsync(customerId, fromDate, toDate, ct));
        entries.AddRange(await GetDebitNoteEntriesAsync(customerId, fromDate, toDate, ct));
        entries.AddRange(await GetPaymentEntriesAsync(customerId, fromDate, toDate, ct));
        entries.AddRange(await GetRefundEntriesAsync(customerId, fromDate, toDate, ct));
        entries.AddRange(await GetCreditNoteEntriesAsync(customerId, fromDate, toDate, ct));

        CalculateRunningBalances(entries);
        return entries;
    }

    public async Task<decimal> GetTotalInvoicedAsync(Guid customerId, CancellationToken ct = default)
    {
        var invoices = await _dbContext.Invoices
            .AsNoTracking()
            .Where(i => !i.Isdeleted
                && i.Status != InvoiceStatus.CANCELLED
                && i.SalesOrder != null
                && i.SalesOrder.CustomerId == customerId)
            .ToListAsync(ct);

        return invoices.Sum(i => i.GrandTotal);
    }

    public async Task<decimal> GetTotalPaymentsAsync(Guid customerId, CancellationToken ct = default)
    {
        // Excludes re-applications of an existing advance to avoid double-counting — same
        // logic CustomerAccountSummaryService and SupplierLedgerService.GetTotalPaymentsAsync use.
        // CREDIT_NOTE settlements are excluded because GetTotalCreditNotesAppliedAsync already
        // books those from the note's UsedAmount.
        //
        // Negative REFUND rows ARE included: they are the exact cash that went back out, already
        // adjusted for any order discount. The balance nets them here rather than through a
        // separate refunds term — see GetCurrentBalanceAsync for why that has to be one or the
        // other, never both.
        return await _dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => !p.Isdeleted
                && p.CustomerId == customerId
                && p.Status == CustomerPaymentStatus.COMPLETED
                && (p.PaymentType == CustomerPaymentType.ADVANCE || p.SourceAdvancePaymentId == null)
                && p.PaymentMethod != "CREDIT_NOTE")
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0;
    }

    public async Task<decimal> GetTotalRefundsAsync(Guid customerId, CancellationToken ct = default)
    {
        var returns = await _dbContext.Set<SalesReturn>()
            .AsNoTracking()
            .Where(r => !r.Isdeleted
                && r.Status == SalesReturnStatus.PROCESSED
                && r.SalesOrder != null
                && r.SalesOrder.CustomerId == customerId)
            .ToListAsync(ct);

        return returns.Sum(r => r.RefundAmount);
    }

    public async Task<decimal> GetAvailableAdvanceCreditAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => !p.Isdeleted
                && p.CustomerId == customerId
                && p.Status == CustomerPaymentStatus.COMPLETED
                && p.PaymentType == CustomerPaymentType.ADVANCE
                && p.RemainingAmount > 0)
            .SumAsync(p => (decimal?)p.RemainingAmount, ct) ?? 0;
    }

    public async Task<decimal> GetTotalDebitNotesAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _dbContext.CustomerDebitNotes
            .AsNoTracking()
            .Where(dn => !dn.Isdeleted
                && dn.CustomerId == customerId
                && dn.Status == CustomerDebitNoteStatus.ISSUED)
            .SumAsync(dn => (decimal?)dn.TotalAmount, ct) ?? 0;
    }

    public async Task<decimal> GetTotalCreditNotesAppliedAsync(Guid customerId, CancellationToken ct = default)
    {
        return await _dbContext.CustomerCreditNotes
            .AsNoTracking()
            .Where(cn => !cn.Isdeleted
                && cn.CustomerId == customerId
                && cn.Status != CustomerCreditNoteStatus.CANCELLED)
            .SumAsync(cn => (decimal?)cn.UsedAmount, ct) ?? 0;
    }

    #region Private Helper Methods

    private async Task<List<CustomerLedgerEntryDto>> GetInvoiceEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.Invoices
            .AsNoTracking()
            .Where(i => !i.Isdeleted
                && i.Status != InvoiceStatus.CANCELLED
                && i.SalesOrder != null
                && i.SalesOrder.CustomerId == customerId);

        if (fromDate.HasValue)
            query = query.Where(i => i.InvoiceDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(i => i.InvoiceDate <= toDate.Value);

        var invoices = await query.ToListAsync(ct);

        return invoices.Select(i => new CustomerLedgerEntryDto
        {
            Id = i.Id,
            TransactionDate = i.InvoiceDate,
            TransactionType = CustomerLedgerTransactionType.INVOICE,
            ReferenceNumber = i.InvoiceNumber,
            ReferenceId = i.Id,
            DebitAmount = i.GrandTotal,
            CreditAmount = 0,
            Description = "Invoice",
            Status = i.Status.ToString()
        }).ToList();
    }

    private async Task<List<CustomerLedgerEntryDto>> GetPaymentEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => !p.Isdeleted
                && p.CustomerId == customerId
                && p.Status == CustomerPaymentStatus.COMPLETED
                && (p.PaymentType == CustomerPaymentType.ADVANCE || p.SourceAdvancePaymentId == null)
                && p.PaymentMethod != "CREDIT_NOTE");

        if (fromDate.HasValue)
            query = query.Where(p => p.PaymentDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(p => p.PaymentDate <= toDate.Value);

        var payments = await query.ToListAsync(ct);

        return payments.Select(p => new CustomerLedgerEntryDto
        {
            Id = p.Id,
            TransactionDate = p.PaymentDate,
            TransactionType = p.PaymentType == CustomerPaymentType.ADVANCE
                ? CustomerLedgerTransactionType.ADVANCE
                : CustomerLedgerTransactionType.PAYMENT,
            ReferenceNumber = p.TransactionNumber,
            ReferenceId = p.Id,
            DebitAmount = 0,
            CreditAmount = p.Amount,
            Description = GetPaymentDescription(p),
            Status = p.Status.ToString()
        }).ToList();
    }

    private async Task<List<CustomerLedgerEntryDto>> GetRefundEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.Set<SalesReturn>()
            .AsNoTracking()
            .Where(r => !r.Isdeleted
                && r.Status == SalesReturnStatus.PROCESSED
                && r.SalesOrder != null
                && r.SalesOrder.CustomerId == customerId);

        if (fromDate.HasValue)
            query = query.Where(r => r.ReturnDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(r => r.ReturnDate <= toDate.Value);

        var returns = await query.ToListAsync(ct);

        return returns.Select(r => new CustomerLedgerEntryDto
        {
            Id = r.Id,
            TransactionDate = r.ApprovedDate ?? r.ReturnDate,
            TransactionType = CustomerLedgerTransactionType.REFUND,
            ReferenceNumber = r.ReturnNumber,
            ReferenceId = r.Id,
            DebitAmount = 0,
            CreditAmount = r.RefundAmount,
            Description = $"Sales Return - {r.Reason}",
            Status = r.Status.ToString()
        }).ToList();
    }

    private async Task<List<CustomerLedgerEntryDto>> GetDebitNoteEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.CustomerDebitNotes
            .AsNoTracking()
            .Where(dn => !dn.Isdeleted
                && dn.CustomerId == customerId
                && dn.Status == CustomerDebitNoteStatus.ISSUED);

        if (fromDate.HasValue)
            query = query.Where(dn => dn.IssueDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(dn => dn.IssueDate <= toDate.Value);

        var debitNotes = await query.ToListAsync(ct);

        return debitNotes.Select(dn => new CustomerLedgerEntryDto
        {
            Id = dn.Id,
            TransactionDate = dn.IssueDate,
            TransactionType = CustomerLedgerTransactionType.DEBIT_NOTE,
            ReferenceNumber = dn.DebitNoteNumber,
            ReferenceId = dn.Id,
            DebitAmount = dn.TotalAmount,
            CreditAmount = 0,
            Description = $"Debit Note - {dn.Reason}",
            Status = dn.Status.ToString()
        }).ToList();
    }

    private async Task<List<CustomerLedgerEntryDto>> GetCreditNoteEntriesAsync(
        Guid customerId, DateTime? fromDate, DateTime? toDate, CancellationToken ct)
    {
        var query = _dbContext.CustomerCreditNotes
            .AsNoTracking()
            .Where(cn => !cn.Isdeleted
                && cn.CustomerId == customerId
                && cn.Status != CustomerCreditNoteStatus.CANCELLED
                && cn.UsedAmount > 0);

        if (fromDate.HasValue)
            query = query.Where(cn => cn.IssueDate >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(cn => cn.IssueDate <= toDate.Value);

        var creditNotes = await query.ToListAsync(ct);

        return creditNotes.Select(cn => new CustomerLedgerEntryDto
        {
            Id = cn.Id,
            TransactionDate = cn.IssueDate,
            TransactionType = CustomerLedgerTransactionType.CREDIT_NOTE,
            ReferenceNumber = cn.CreditNoteNumber,
            ReferenceId = cn.Id,
            DebitAmount = 0,
            CreditAmount = cn.UsedAmount,
            Description = $"Credit Note Applied - {cn.Notes}",
            Status = cn.Status.ToString()
        }).ToList();
    }

    private static string GetPaymentDescription(CustomerPayment payment)
    {
        var description = payment.PaymentType == CustomerPaymentType.ADVANCE
            ? "Advance Payment"
            : "Payment";

        if (!string.IsNullOrEmpty(payment.PaymentMethod))
            description += $" - {payment.PaymentMethod}";

        if (payment.SourceAdvancePaymentId.HasValue)
            description = "Applied from Advance Credit";

        return description;
    }

    private static void CalculateRunningBalances(List<CustomerLedgerEntryDto> entries)
    {
        if (!entries.Any()) return;

        var sortedEntries = entries.OrderBy(e => e.TransactionDate).ToList();

        decimal runningBalance = 0;
        foreach (var entry in sortedEntries)
        {
            runningBalance += entry.DebitAmount - entry.CreditAmount;
            entry.RunningBalance = runningBalance;
        }

        var finalOrder = entries.OrderByDescending(e => e.TransactionDate).ToList();
        entries.Clear();
        entries.AddRange(finalOrder);
    }

    #endregion
}
