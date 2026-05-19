using AutoPartShop.Api.Common;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/cash-book")]
[ApiController]
[Authorize]
[Produces("application/json")]
public class CashBookController(AutoPartDbContext _db) : ControllerBase
{
    /// <summary>
    /// Daily cash book — aggregates all money movement for a date range.
    /// Cash In : customer payments (COMPLETED | PENDING-COD)
    /// Cash Out: daily expenses + supplier payments (COMPLETED)
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken ct)
    {
        // Resolve range — single date wins over range params; default to today
        var dateFrom = (from ?? date ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToDateTime(TimeOnly.MinValue);
        var dateTo   = (to   ?? date ?? DateOnly.FromDateTime(DateTime.UtcNow)).ToDateTime(TimeOnly.MaxValue);

        if (dateFrom > dateTo) return BadRequest(ApiError.Validation("'from' must not be after 'to'", instance: Request.Path));

        // ── Customer payments (cash in) ─────────────────────────────────
        var customerPayments = await _db.CustomerPayments
            .Where(p => p.PaymentDate >= dateFrom && p.PaymentDate <= dateTo
                     && (p.Status == "COMPLETED" || p.Status == "PENDING"))
            .Include(p => p.Customer)
            .Include(p => p.Invoice)
            .AsNoTracking()
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

        // ── Expenses (cash out) ────────────────────────────────────────
        var expenses = await _db.DailyExpenses
            .Where(e => e.ExpenseDate >= dateFrom && e.ExpenseDate <= dateTo)
            .AsNoTracking()
            .OrderBy(e => e.ExpenseDate)
            .ToListAsync(ct);

        // ── Supplier payments (cash out) ────────────────────────────────
        var supplierPayments = await _db.SupplierPayments
            .Where(p => p.PaymentDate >= dateFrom && p.PaymentDate <= dateTo
                     && p.Status == "COMPLETED")
            .Include(p => p.Supplier)
            .AsNoTracking()
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

        // ── Build cash-in list ─────────────────────────────────────────
        var cashIn = customerPayments.Select(p =>
        {
            var customerName = p.Customer != null
                ? $"{p.Customer.FirstName} {p.Customer.LastName}".Trim()
                : "Customer";
            var invoiceRef = p.Invoice != null ? $" · {p.Invoice.InvoiceNumber}" : string.Empty;
            return new CashBookEntry
            {
                Id            = p.Id,
                Time          = p.PaymentDate,
                Type          = "CUSTOMER_PAYMENT",
                Description   = $"{customerName}{invoiceRef}",
                Reference     = !string.IsNullOrEmpty(p.TransactionNumber) ? p.TransactionNumber : p.ReferenceNumber,
                PaymentMethod = p.PaymentMethod,
                Amount        = p.Amount,
                Currency      = p.Currency,
                Status        = p.Status,
                Notes         = string.IsNullOrWhiteSpace(p.Notes) ? null : p.Notes
            };
        }).ToList();

        // ── Build cash-out list ────────────────────────────────────────
        var cashOut = new List<CashBookEntry>();

        cashOut.AddRange(expenses.Select(e => new CashBookEntry
        {
            Id            = e.Id,
            Time          = e.ExpenseDate,
            Type          = "EXPENSE",
            Description   = e.Description,
            Reference     = e.ReferenceNumber,
            PaymentMethod = e.PaymentMethod,
            Amount        = e.Amount,
            Currency      = e.Currency,
            Status        = "COMPLETED",
            Notes         = string.IsNullOrWhiteSpace(e.Notes) ? null : e.Notes,
            Category      = e.Category,
            Vendor        = string.IsNullOrWhiteSpace(e.VendorName) ? null : e.VendorName
        }));

        cashOut.AddRange(supplierPayments.Select(p => new CashBookEntry
        {
            Id            = p.Id,
            Time          = p.PaymentDate,
            Type          = "SUPPLIER_PAYMENT",
            Description   = p.Supplier != null ? $"Payment to {p.Supplier.Name}" : "Supplier payment",
            Reference     = !string.IsNullOrEmpty(p.TransactionNumber) ? p.TransactionNumber : p.ReferenceNumber,
            PaymentMethod = p.PaymentMethod,
            Amount        = p.NetAmount > 0 ? p.NetAmount : p.Amount,
            Currency      = p.Currency,
            Status        = p.Status,
            Notes         = null
        }));

        cashOut = cashOut.OrderBy(e => e.Time).ToList();

        var totalIn  = cashIn .Sum(e => e.Amount);
        var totalOut = cashOut.Sum(e => e.Amount);

        // ── Running balance (combined chronological ledger) ─────────────
        var allEntries = cashIn.Select(e => new { entry = e, flow = "IN" })
            .Concat(cashOut.Select(e => new { entry = e, flow = "OUT" }))
            .OrderBy(x => x.entry.Time)
            .ToList();

        decimal running = 0;
        var ledger = allEntries.Select(x =>
        {
            running += x.flow == "IN" ? x.entry.Amount : -x.entry.Amount;
            return new LedgerRow
            {
                Id             = x.entry.Id,
                Time           = x.entry.Time,
                Flow           = x.flow,
                Type           = x.entry.Type,
                Description    = x.entry.Description,
                Reference      = x.entry.Reference,
                PaymentMethod  = x.entry.PaymentMethod,
                CashIn         = x.flow == "IN"  ? x.entry.Amount : null,
                CashOut        = x.flow == "OUT" ? x.entry.Amount : null,
                Balance        = running,
                Currency       = x.entry.Currency,
                Status         = x.entry.Status,
                Notes          = x.entry.Notes,
                Category       = x.entry.Category,
                Vendor         = x.entry.Vendor
            };
        }).ToList();

        // ── Payment method breakdown ───────────────────────────────────
        var allForBreakdown = cashIn.Select(e => (e.PaymentMethod, In: e.Amount, Out: 0m))
            .Concat(cashOut.Select(e => (e.PaymentMethod, In: 0m, Out: e.Amount)));

        var breakdown = allForBreakdown
            .GroupBy(x => x.PaymentMethod)
            .Select(g => new
            {
                method = g.Key,
                @in    = g.Sum(x => x.In),
                @out   = g.Sum(x => x.Out),
                net    = g.Sum(x => x.In - x.Out)
            })
            .OrderByDescending(x => x.@in + x.@out)
            .ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            from          = dateFrom.ToString("yyyy-MM-dd"),
            to            = dateTo  .ToString("yyyy-MM-dd"),
            isSingleDay   = dateFrom.Date == dateTo.Date,
            cashIn,
            cashOut,
            ledger,
            totalCashIn   = totalIn,
            totalCashOut  = totalOut,
            netCash        = totalIn - totalOut,
            closingBalance = running,
            entryCount    = cashIn.Count + cashOut.Count,
            paymentMethodBreakdown = breakdown
        }));
    }
}

public sealed class CashBookEntry
{
    public Guid     Id            { get; set; }
    public DateTime Time          { get; set; }
    public string   Type          { get; set; } = string.Empty;
    public string   Description   { get; set; } = string.Empty;
    public string   Reference     { get; set; } = string.Empty;
    public string   PaymentMethod { get; set; } = string.Empty;
    public decimal  Amount        { get; set; }
    public string   Currency      { get; set; } = "BDT";
    public string   Status        { get; set; } = string.Empty;
    public string?  Notes         { get; set; }
    public string?  Category      { get; set; }
    public string?  Vendor        { get; set; }
}

public sealed class LedgerRow
{
    public Guid     Id            { get; set; }
    public DateTime Time          { get; set; }
    public string   Flow          { get; set; } = string.Empty;   // IN | OUT
    public string   Type          { get; set; } = string.Empty;
    public string   Description   { get; set; } = string.Empty;
    public string   Reference     { get; set; } = string.Empty;
    public string   PaymentMethod { get; set; } = string.Empty;
    public decimal? CashIn        { get; set; }
    public decimal? CashOut       { get; set; }
    public decimal  Balance       { get; set; }
    public string   Currency      { get; set; } = "BDT";
    public string   Status        { get; set; } = string.Empty;
    public string?  Notes         { get; set; }
    public string?  Category      { get; set; }
    public string?  Vendor        { get; set; }
}
