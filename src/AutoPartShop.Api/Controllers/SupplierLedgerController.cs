using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.LedgerDtos;
using AutoPartShop.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers;

/// <summary>
/// Controller for supplier ledger operations.
/// Provides unified view of all supplier transactions (purchases, payments, refunds).
/// </summary>
[Route("api/supplier-ledger")]
[Route("api/v1/supplier-ledger")]
[ApiController]
[HasPermission(Permissions.ReportsView)]
[Produces("application/json")]
public class SupplierLedgerController : ControllerBase
{
    private readonly ISupplierLedgerService _ledgerService;
    private readonly ILogger<SupplierLedgerController> _logger;

    public SupplierLedgerController(
        ISupplierLedgerService ledgerService,
        ILogger<SupplierLedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// Get complete ledger summary for a supplier including calculated balance and recent entries
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="entryLimit">Maximum number of entries to return (default: 20)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{supplierId:guid}/summary")]
    [ProducesResponseType(typeof(SupplierLedgerSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLedgerSummary(
        Guid supplierId,
        [FromQuery] int entryLimit = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var summary = await _ledgerService.GetLedgerSummaryAsync(supplierId, entryLimit, cancellationToken);
            return Ok(summary);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger summary for supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving the ledger summary" });
        }
    }

    /// <summary>
    /// Get paginated ledger entries with optional filtering
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="query">Query parameters for filtering and pagination</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpPost("{supplierId:guid}/entries")]
    [ProducesResponseType(typeof(PagedLedgerResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntries(
        Guid supplierId,
        [FromBody] SupplierLedgerQueryDto query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            query.SupplierId = supplierId;
            var result = await _ledgerService.GetLedgerEntriesAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }

    /// <summary>
    /// Download the supplier ledger as a PDF — the server-rendered equivalent of the Supplier
    /// Account Summary page. Totals are always all-time (matching GetLedgerSummaryAsync); the
    /// entry list respects fromDate/toDate the same way the on-screen date filter does.
    /// </summary>
    [HttpGet("{supplierId:guid}/statement-pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadStatementPdf(
        Guid supplierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromServices] AutoPartShop.Api.Services.IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // entryLimit here only bounds the summary DTO's own Entries list, which this document
            // doesn't use — the real entry list below is fetched separately so it can honour the
            // date filter, same as the Angular page's "load all entries then export" flow.
            var summary = await _ledgerService.GetLedgerSummaryAsync(supplierId, entryLimit: 1, cancellationToken);

            const int allEntriesCap = 50_000;
            var paged = await _ledgerService.GetLedgerEntriesAsync(
                new AutoPartShop.Application.DTOs.LedgerDtos.SupplierLedgerQueryDto
                {
                    SupplierId = supplierId,
                    PageNumber = 1,
                    PageSize = allEntriesCap,
                    FromDate = fromDate,
                    ToDate = toDate
                },
                cancellationToken);

            var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);

            var periodLabel = fromDate is null && toDate is null
                ? "All time"
                : $"{fromDate?.ToString("dd MMM yyyy") ?? "…"} – {toDate?.ToString("dd MMM yyyy") ?? "…"}";

            var data = new AutoPartShop.Api.Pdf.SupplierLedgerStatementData(
                SupplierName: summary.SupplierName,
                SupplierCode: summary.SupplierCode,
                PeriodLabel: periodLabel,
                TotalPurchases: summary.TotalPurchases,
                TotalPayments: summary.TotalPayments,
                TotalRefunds: summary.TotalRefunds,
                AvailableAdvanceCredit: summary.AvailableAdvanceCredit,
                CurrentBalance: summary.CurrentBalance,
                Entries: paged.Entries
                    .Select(e => new AutoPartShop.Api.Pdf.SupplierLedgerStatementLine(
                        TransactionDate: e.TransactionDate,
                        TransactionType: e.TransactionType.ToString(),
                        ReferenceNumber: e.ReferenceNumber,
                        Description: e.Description,
                        DebitAmount: e.DebitAmount,
                        CreditAmount: e.CreditAmount,
                        RunningBalance: e.RunningBalance))
                    .ToList());

            var pdfBytes = new AutoPartShop.Api.Pdf.SupplierLedgerStatementDocument(data, shop)
                .GeneratePdf();

            var dateStr = DateTime.UtcNow.ToString("yyyyMMdd");
            return File(pdfBytes, "application/pdf", $"supplier-ledger-{summary.SupplierCode}-{dateStr}.pdf");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("not found"))
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ledger statement PDF for supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while generating the ledger statement" });
        }
    }

    /// <summary>
    /// Get current calculated balance for a supplier
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{supplierId:guid}/balance")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentBalance(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var balance = await _ledgerService.CalculateCurrentBalanceAsync(supplierId, cancellationToken);
            var totalPurchases = await _ledgerService.GetTotalPurchasesAsync(supplierId, cancellationToken);
            var totalPayments = await _ledgerService.GetTotalPaymentsAsync(supplierId, cancellationToken);
            var totalRefunds = await _ledgerService.GetTotalRefundsAsync(supplierId, cancellationToken);
            var advanceCredit = await _ledgerService.GetAvailableAdvanceCreditAsync(supplierId, cancellationToken);

            return Ok(new
            {
                supplierId,
                currentBalance = balance,
                totalPurchases,
                totalPayments,
                totalRefunds,
                availableAdvanceCredit = advanceCredit,
                calculatedAt = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating balance for supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while calculating the balance" });
        }
    }

    /// <summary>
    /// Get ledger entries within a date range
    /// </summary>
    /// <param name="supplierId">The supplier ID</param>
    /// <param name="fromDate">Start date (optional)</param>
    /// <param name="toDate">End date (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    [HttpGet("{supplierId:guid}/entries")]
    [ProducesResponseType(typeof(List<SupplierLedgerEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerEntriesByDateRange(
        Guid supplierId,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entries = await _ledgerService.GetLedgerEntriesAsync(supplierId, fromDate, toDate, cancellationToken);
            return Ok(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting ledger entries for supplier: {SupplierId}", supplierId);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "An error occurred while retrieving ledger entries" });
        }
    }
}
