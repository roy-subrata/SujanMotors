using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Pdf;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.TillSessionDtos;
using AutoPartShop.Domain.Entities;
using AutoPartShop.Domain.Repositories;
using AutoPartShop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

[Route("api/till-sessions")]
[Route("api/v1/till-sessions")]
[ApiController]
[Produces("application/json")]
[HasPermission(Permissions.SalesView)]
public class TillSessionController(
    ITillSessionRepository tillSessionRepository,
    ICurrentUserService currentUserService,
    AutoPartDbContext dbContext,
    ILogger<TillSessionController> logger) : ControllerBase
{
    [HttpPost("open")]
    [HasPermission(Permissions.SalesCreate)]
    public async Task<IActionResult> Open(OpenTillSessionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var cashierId = currentUserService.GetCurrentUserGuid();
            if (cashierId is null || cashierId == Guid.Empty)
                return Unauthorized(new { message = "Could not resolve the current user" });

            var existing = await tillSessionRepository.GetOpenSessionForCashierAsync(cashierId.Value, cancellationToken);
            if (existing is not null)
                return BadRequest(new { message = $"You already have an open till session ({existing.TerminalLabel}, opened {existing.OpenedAt:g}). Close it before opening a new one." });

            var username = currentUserService.GetCurrentUsername();
            var session = TillSession.Create(
                cashierId.Value, username, request.TerminalLabel, request.OpeningFloat,
                request.ShiftLabel, request.Notes);
            session.CreatedBy = username;
            session.ModifiedBy = username;

            await tillSessionRepository.AddAsync(session, cancellationToken);

            return CreatedAtAction(nameof(GetById), new { id = session.Id }, await MapToResponseAsync(session, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error opening till session");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while opening the till session");
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var session = await tillSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Till session not found" });

        return Ok(await MapToResponseAsync(session, cancellationToken));
    }

    /// <summary>The calling cashier's currently open session, if any.</summary>
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var cashierId = currentUserService.GetCurrentUserGuid();
        if (cashierId is null || cashierId == Guid.Empty)
            return Unauthorized(new { message = "Could not resolve the current user" });

        var session = await tillSessionRepository.GetOpenSessionForCashierAsync(cashierId.Value, cancellationToken);
        return session is null ? Ok(null) : Ok(await MapToResponseAsync(session, cancellationToken));
    }

    [HttpPost("list")]
    public async Task<IActionResult> Search(TillSessionQuery query, CancellationToken cancellationToken)
    {
        var (sessions, totalCount) = await tillSessionRepository.SearchPagedAsync(query, cancellationToken);
        var responses = new List<TillSessionResponse>();
        foreach (var s in sessions)
            responses.Add(await MapToResponseAsync(s, cancellationToken));

        return Ok(new { data = responses, totalCount, query.PageNumber, query.PageSize });
    }

    [HttpPost("{id:guid}/cash-drops")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> RecordCashDrop(Guid id, RecordCashDropRequest request, CancellationToken cancellationToken)
    {
        var session = await tillSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Till session not found" });

        try
        {
            var drop = TillCashDrop.Create(session.Id, request.Amount, request.Notes);
            drop.CreatedBy = currentUserService.GetCurrentUsername();
            drop.ModifiedBy = drop.CreatedBy;

            session.RecordCashDrop(drop);
            session.ModifiedBy = currentUserService.GetCurrentUsername();

            await tillSessionRepository.UpdateAsync(session, cancellationToken);
            return Ok(await MapToResponseAsync(session, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Closes the session and freezes its reconciliation. Cash sales/refunds are computed here from
    /// CustomerPayment — see TillSession's class remarks for why that's a derived read rather than a
    /// stored link.
    /// </summary>
    [HttpPost("{id:guid}/close")]
    [HasPermission(Permissions.SalesEdit)]
    public async Task<IActionResult> Close(Guid id, CloseTillSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await tillSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Till session not found" });

        try
        {
            var windowEnd = DateTime.UtcNow;

            var cashSales = await dbContext.CustomerPayments
                .Where(p => p.CreatedBy == session.CashierUsername
                         && p.PaymentMethod == "CASH"
                         && p.Status == "COMPLETED"
                         && p.PaymentDate >= session.OpenedAt
                         && p.PaymentDate <= windowEnd
                         && !p.Isdeleted)
                .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

            // Refunds are stored as negative CustomerPayment amounts with PaymentMethod="REFUND"
            // (see SalesReturnController) — this sums the absolute cash paid back out.
            var cashRefunds = await dbContext.CustomerPayments
                .Where(p => p.CreatedBy == session.CashierUsername
                         && p.PaymentMethod == "REFUND"
                         && p.Status == "COMPLETED"
                         && p.PaymentDate >= session.OpenedAt
                         && p.PaymentDate <= windowEnd
                         && !p.Isdeleted)
                .SumAsync(p => (decimal?)-p.Amount, cancellationToken) ?? 0m;

            session.Close(request.CountedAmount, cashSales, cashRefunds, request.Notes);
            session.ModifiedBy = currentUserService.GetCurrentUsername();

            await tillSessionRepository.UpdateAsync(session, cancellationToken);
            return Ok(await MapToResponseAsync(session, cancellationToken));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Download the Shift Report as a PDF. Only meaningful once the session is CLOSED.</summary>
    [HttpGet("{id:guid}/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(
        Guid id,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken)
    {
        var session = await tillSessionRepository.GetByIdAsync(id, cancellationToken);
        if (session is null) return NotFound(new { message = "Till session not found" });

        if (session.Status != "CLOSED")
            return BadRequest(new { message = "The shift report is only available once the session is closed." });

        var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);

        var cashierDisplay = await ResolveCashierDisplayAsync(session, cancellationToken);
        var (receiptCount, returnCount, voidCount, methodCounts) = await GetTransactionSummaryAsync(session, cancellationToken);

        var data = new ShiftReportDocumentData(
            ReportNumber: $"SHF-{session.OpenedAt:yyyyMMdd}-{session.TerminalLabel.Replace(" ", "")}",
            ReportDate: session.OpenedAt,
            ShiftLabel: session.ShiftLabel ?? "",
            ShiftHours: $"{session.OpenedAt:HH:mm} – {(session.ClosedAt ?? DateTime.UtcNow):HH:mm}",
            TerminalLabel: session.TerminalLabel,
            CashierName: cashierDisplay,
            SignedIn: session.OpenedAt,
            SignedOut: session.ClosedAt,
            ReceiptCount: receiptCount,
            ReturnCount: returnCount,
            VoidCount: voidCount,
            MethodCounts: methodCounts,
            OpeningFloat: session.OpeningFloat,
            CashSales: session.CashSalesTotal,
            CashRefunds: session.CashRefundsTotal,
            CashDrops: session.CashDropsTotal,
            ExpectedInDrawer: session.ExpectedAmount,
            CountedAtClose: session.ClosingCountedAmount ?? 0,
            OverShort: session.OverShortAmount,
            Note: session.Notes);

        var pdfBytes = new ShiftReportDocument(data, shop).GeneratePdf();
        return File(pdfBytes, "application/pdf", $"shift-report-{session.OpenedAt:yyyyMMdd}-{session.TerminalLabel.Replace(" ", "")}.pdf");
    }

    // ── Helpers ────────────────────────────────────────────────────────────────
    private async Task<string> ResolveCashierDisplayAsync(TillSession session, CancellationToken cancellationToken)
    {
        var employee = await dbContext.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == session.CashierId && !e.Isdeleted, cancellationToken);

        if (employee is not null)
            return $"{employee.Name} · Staff ID {employee.EmployeeCode}";

        var user = session.Cashier ?? await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == session.CashierId, cancellationToken);

        if (user is not null)
        {
            var name = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? session.CashierUsername : name;
        }

        return session.CashierUsername;
    }

    private async Task<(int Receipts, int Returns, int Voids, List<ShiftMethodCount> Methods)> GetTransactionSummaryAsync(
        TillSession session, CancellationToken cancellationToken)
    {
        var windowEnd = session.ClosedAt ?? DateTime.UtcNow;

        var payments = await dbContext.CustomerPayments
            .AsNoTracking()
            .Where(p => p.CreatedBy == session.CashierUsername
                     && p.Status == "COMPLETED"
                     && p.PaymentDate >= session.OpenedAt
                     && p.PaymentDate <= windowEnd
                     && !p.Isdeleted)
            .Select(p => new { p.PaymentMethod, p.Amount })
            .ToListAsync(cancellationToken);

        var sales = payments.Where(p => p.PaymentMethod != "REFUND").ToList();
        var returns = payments.Count(p => p.PaymentMethod == "REFUND");

        var methodCounts = sales
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new ShiftMethodCount(FormatMethod(g.Key), g.Count()))
            .OrderByDescending(m => m.Count)
            .ToList();

        // No void/cancellation tracking exists on CustomerPayment today, so this is always 0 —
        // the field is kept on the document because the handoff shows it, ready for whenever a
        // void concept is added.
        return (sales.Count, returns, 0, methodCounts);
    }

    private static string FormatMethod(string method) => method switch
    {
        "CASH" => "Cash",
        "CARD" => "Card",
        "MOBILE_BANKING" => "Mobile",
        "BANK_TRANSFER" => "Bank Transfer",
        "ADVANCE_CREDIT" => "Credit Applied",
        _ => string.IsNullOrWhiteSpace(method) ? "Other" : method.Replace('_', ' ')
    };

    private async Task<TillSessionResponse> MapToResponseAsync(TillSession s, CancellationToken cancellationToken)
    {
        var cashierName = await ResolveCashierDisplayAsync(s, cancellationToken);

        return new TillSessionResponse
        {
            Id = s.Id,
            CashierId = s.CashierId,
            CashierName = cashierName,
            TerminalLabel = s.TerminalLabel,
            ShiftLabel = s.ShiftLabel,
            OpenedAt = s.OpenedAt,
            ClosedAt = s.ClosedAt,
            OpeningFloat = s.OpeningFloat,
            ClosingCountedAmount = s.ClosingCountedAmount,
            Status = s.Status,
            CashSalesTotal = s.CashSalesTotal,
            CashRefundsTotal = s.CashRefundsTotal,
            CashDropsTotal = s.CashDropsTotal,
            ExpectedAmount = s.ExpectedAmount,
            OverShortAmount = s.OverShortAmount,
            Notes = s.Notes,
            CashDrops = s.CashDrops.OrderBy(d => d.DroppedAt).Select(d => new TillCashDropResponse
            {
                Id = d.Id,
                Amount = d.Amount,
                DroppedAt = d.DroppedAt,
                Notes = d.Notes
            }).ToList()
        };
    }
}
