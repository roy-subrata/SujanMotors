using AutoPartShop.Api.Common;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Controllers;

[Route("api/cash-book")]
[Route("api/v1/cash-book")]
[ApiController]
[HasPermission(Permissions.ReportsView)]
[Produces("application/json")]
public class CashBookController(AutoPartDbContext _db) : ControllerBase
{
    // Payment methods that represent deferred credit, not immediate cash receipt.
    private static readonly HashSet<string> CreditMethods =
        new(StringComparer.OrdinalIgnoreCase) { "DUE", "PART_PAY" };

    /// <summary>
    /// Daily cash book â€” aggregates all money movement for a date range.
    /// Cash In : COMPLETED customer payments (positive amounts only)
    /// Cash Out: daily expenses + COMPLETED supplier payments + refund payments (negative customer payments)
    /// Opening balance: cumulative net of all prior COMPLETED transactions before dateFrom
    /// tzOffsetMinutes: browser's -getTimezoneOffset() value (e.g. 360 for UTC+6).
    ///   Shifts the UTC datetime window so "today" matches the user's local calendar date.
    /// </summary>
    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily(
        [FromQuery] DateOnly? date,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] int tzOffsetMinutes = 0,
        CancellationToken ct = default)
    {
        // Clamp offset to a sane range (UTC-14 to UTC+14).
        tzOffsetMinutes = Math.Clamp(tzOffsetMinutes, -840, 840);
        var tzShift = TimeSpan.FromMinutes(tzOffsetMinutes);

        // Resolve local-calendar range, then shift to UTC so DB comparisons are correct.
        var localFrom = from ?? date ?? DateOnly.FromDateTime(DateTime.UtcNow.Add(tzShift));
        var localTo = to ?? date ?? DateOnly.FromDateTime(DateTime.UtcNow.Add(tzShift));
        var dateFrom = localFrom.ToDateTime(TimeOnly.MinValue) - tzShift;
        var dateTo = localTo.ToDateTime(TimeOnly.MaxValue) - tzShift;

        if (dateFrom > dateTo) return BadRequest(ApiError.Validation("'from' must not be after 'to'", instance: Request.Path));

        // Guard against huge ranges that would load the entire ledger into memory.
        var daySpan = (localTo.DayNumber - localFrom.DayNumber) + 1;
        if (daySpan > 366)
            return BadRequest(ApiError.Validation("Date range cannot exceed 366 days. Split into smaller periods.", instance: Request.Path));

        // â”€â”€ Opening balance (all COMPLETED transactions before dateFrom) â”€
        var priorCustomerNet = await _db.CustomerPayments
            .Where(p => p.PaymentDate < dateFrom && p.Status == "COMPLETED")
            .SumAsync(p => (decimal?)p.Amount, ct) ?? 0m;

        var priorExpenseTotal = await _db.DailyExpenses
            .Where(e => e.ExpenseDate < dateFrom)
            .SumAsync(e => (decimal?)e.Amount, ct) ?? 0m;

        var priorSupplierTotal = await _db.SupplierPayments
            .Where(p => p.PaymentDate < dateFrom && p.Status == "COMPLETED")
            .SumAsync(p => (decimal?)(p.NetAmount > 0 ? p.NetAmount : p.Amount), ct) ?? 0m;

        var priorDepositTotal = await _db.CashDeposits
            .Where(d => d.DepositDate < dateFrom && !d.Isdeleted)
            .SumAsync(d => (decimal?)d.Amount, ct) ?? 0m;

        var openingBalance = priorCustomerNet + priorDepositTotal - priorExpenseTotal - priorSupplierTotal;

        // â”€â”€ Customer payments â€” COMPLETED only â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var customerPayments = await _db.CustomerPayments
            .Where(p => p.PaymentDate >= dateFrom && p.PaymentDate <= dateTo
                     && p.Status == "COMPLETED")
            .Include(p => p.Customer)
            .Include(p => p.Invoice)
            .AsNoTracking()
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

        // â”€â”€ Expenses (cash out) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var expenses = await _db.DailyExpenses
            .Where(e => e.ExpenseDate >= dateFrom && e.ExpenseDate <= dateTo)
            .AsNoTracking()
            .OrderBy(e => e.ExpenseDate)
            .ToListAsync(ct);

        // â”€â”€ Supplier payments (cash out) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var supplierPayments = await _db.SupplierPayments
            .Where(p => p.PaymentDate >= dateFrom && p.PaymentDate <= dateTo
                     && p.Status == "COMPLETED")
            .Include(p => p.Supplier)
            .AsNoTracking()
            .OrderBy(p => p.PaymentDate)
            .ToListAsync(ct);

        // â”€â”€ Manual deposits (cash in) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var deposits = await _db.CashDeposits
            .Where(d => d.DepositDate >= dateFrom && d.DepositDate <= dateTo
                     && !d.Isdeleted)
            .AsNoTracking()
            .OrderBy(d => d.DepositDate)
            .ToListAsync(ct);

        // â”€â”€ Build cash-in / cash-out from customer payments â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        // Negative-amount payments (refunds from warranty claims / sales returns)
        // are reclassified as cash-out with their absolute value.
        var cashIn = new List<CashBookEntry>();
        var cashOut = new List<CashBookEntry>();

        foreach (var p in customerPayments)
        {
            var customerName = p.Customer != null
                ? $"{p.Customer.FirstName} {p.Customer.LastName}".Trim()
                : "Customer";
            var invoiceRef = p.Invoice != null ? $" Â· {p.Invoice.InvoiceNumber}" : string.Empty;
            var isRefund = p.Amount < 0;

            var entry = new CashBookEntry
            {
                Id = p.Id,
                Time = p.PaymentDate,
                Type = isRefund ? "REFUND" : "CUSTOMER_PAYMENT",
                Description = isRefund
                    ? $"Refund to {customerName}{invoiceRef}"
                    : $"{customerName}{invoiceRef}",
                Reference = !string.IsNullOrEmpty(p.TransactionNumber) ? p.TransactionNumber : p.ReferenceNumber,
                PaymentMethod = p.PaymentMethod,
                Amount = Math.Abs(p.Amount),
                Currency = p.Currency,
                Status = p.Status,
                Notes = string.IsNullOrWhiteSpace(p.Notes) ? null : p.Notes,
                IsCreditSale = !isRefund && CreditMethods.Contains(p.PaymentMethod)
            };

            if (isRefund) cashOut.Add(entry);
            else cashIn.Add(entry);
        }

        cashIn.AddRange(deposits.Select(d => new CashBookEntry
        {
            Id = d.Id,
            Time = d.DepositDate,
            Type = "DEPOSIT",
            Description = d.Description,
            Reference = d.ReferenceNumber,
            PaymentMethod = d.PaymentMethod,
            Amount = d.Amount,
            Currency = d.Currency,
            Status = "COMPLETED",
            Notes = string.IsNullOrWhiteSpace(d.Notes) ? null : d.Notes,
            Category = d.Category
        }));

        cashIn = cashIn.OrderBy(e => e.Time).ToList();

        cashOut.AddRange(expenses.Select(e => new CashBookEntry
        {
            Id = e.Id,
            Time = e.ExpenseDate,
            Type = "EXPENSE",
            Description = e.Description,
            Reference = e.ReferenceNumber,
            PaymentMethod = e.PaymentMethod,
            Amount = e.Amount,
            Currency = e.Currency,
            Status = "COMPLETED",
            Notes = string.IsNullOrWhiteSpace(e.Notes) ? null : e.Notes,
            Category = e.Category,
            Vendor = string.IsNullOrWhiteSpace(e.VendorName) ? null : e.VendorName
        }));

        cashOut.AddRange(supplierPayments.Select(p => new CashBookEntry
        {
            Id = p.Id,
            Time = p.PaymentDate,
            Type = "SUPPLIER_PAYMENT",
            Description = p.Supplier != null ? $"Payment to {p.Supplier.Name}" : "Supplier payment",
            Reference = !string.IsNullOrEmpty(p.TransactionNumber) ? p.TransactionNumber : p.ReferenceNumber,
            PaymentMethod = p.PaymentMethod,
            Amount = p.NetAmount > 0 ? p.NetAmount : p.Amount,
            Currency = p.Currency,
            Status = p.Status,
            Notes = null
        }));

        cashOut = cashOut.OrderBy(e => e.Time).ToList();

        var totalIn = cashIn.Sum(e => e.Amount);
        var totalOut = cashOut.Sum(e => e.Amount);
        var totalCreditIn = cashIn.Where(e => e.IsCreditSale).Sum(e => e.Amount);
        var totalActualCashIn = totalIn - totalCreditIn;

        // â”€â”€ Running balance starting from opening balance â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var allEntries = cashIn.Select(e => new { entry = e, flow = "IN" })
            .Concat(cashOut.Select(e => new { entry = e, flow = "OUT" }))
            .OrderBy(x => x.entry.Time)
            .ToList();

        decimal running = openingBalance;
        var ledger = allEntries.Select(x =>
        {
            running += x.flow == "IN" ? x.entry.Amount : -x.entry.Amount;
            return new LedgerRow
            {
                Id = x.entry.Id,
                Time = x.entry.Time,
                Flow = x.flow,
                Type = x.entry.Type,
                Description = x.entry.Description,
                Reference = x.entry.Reference,
                PaymentMethod = x.entry.PaymentMethod,
                CashIn = x.flow == "IN" ? x.entry.Amount : null,
                CashOut = x.flow == "OUT" ? x.entry.Amount : null,
                Balance = running,
                Currency = x.entry.Currency,
                Status = x.entry.Status,
                Notes = x.entry.Notes,
                Category = x.entry.Category,
                Vendor = x.entry.Vendor
            };
        }).ToList();

        // â”€â”€ Payment method breakdown â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        var allForBreakdown = cashIn.Select(e => (e.PaymentMethod, In: e.Amount, Out: 0m))
            .Concat(cashOut.Select(e => (e.PaymentMethod, In: 0m, Out: e.Amount)));

        var breakdown = allForBreakdown
            .GroupBy(x => x.PaymentMethod)
            .Select(g => new
            {
                method = g.Key,
                @in = g.Sum(x => x.In),
                @out = g.Sum(x => x.Out),
                net = g.Sum(x => x.In - x.Out)
            })
            .OrderByDescending(x => x.@in + x.@out)
            .ToList();

        return Ok(ApiResponse<object>.Ok(new
        {
            from = localFrom.ToString("yyyy-MM-dd"),
            to = localTo.ToString("yyyy-MM-dd"),
            isSingleDay = localFrom == localTo,
            openingBalance,
            cashIn,
            cashOut,
            ledger,
            totalCashIn = totalIn,
            totalActualCashIn,
            totalCreditIn,
            totalCashOut = totalOut,
            netCash = totalIn - totalOut,
            netActualCash = totalActualCashIn - totalOut,
            closingBalance = running,
            entryCount = cashIn.Count + cashOut.Count,
            paymentMethodBreakdown = breakdown
        }));
    }

    /// <summary>
    /// Records a manual cash-in entry (owner deposit, misc income). Cash-out
    /// entries go through POST /daily-expense — this is only the IN side.
    /// </summary>
    [HttpPost("deposits")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateDeposit(
        [FromBody] CreateCashDepositRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var deposit = CashDeposit.Create(
                request.DepositDate ?? DateTime.UtcNow,
                request.Category,
                request.Amount,
                request.Description,
                request.PaymentMethod,
                request.ReferenceNumber ?? string.Empty,
                request.Notes ?? string.Empty,
                request.Currency ?? "BDT");

            _db.CashDeposits.Add(deposit);
            await _db.SaveChangesAsync(ct);

            return Ok(ApiResponse<object>.Ok(new
            {
                deposit.Id,
                deposit.DepositDate,
                deposit.Category,
                deposit.Amount,
                deposit.Description,
                deposit.PaymentMethod
            }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message, instance: Request.Path));
        }
    }
}

public sealed class CreateCashDepositRequest
{
    public DateTime? DepositDate { get; set; }
    public string Category { get; set; } = "OWNER_DEPOSIT"; // OWNER_DEPOSIT | OTHER
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = "CASH";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? Currency { get; set; }
}

public sealed class CashBookEntry
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "BDT";
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }
    /// <summary>
    /// True for DUE / PART_PAY entries â€” cash has not yet been physically received.
    /// Excluded from totalActualCashIn; included in totalCreditIn.
    /// </summary>
    public bool IsCreditSale { get; set; }
}

public sealed class LedgerRow
{
    public Guid Id { get; set; }
    public DateTime Time { get; set; }
    public string Flow { get; set; } = string.Empty;   // IN | OUT
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal? CashIn { get; set; }
    public decimal? CashOut { get; set; }
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "BDT";
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Category { get; set; }
    public string? Vendor { get; set; }
}
