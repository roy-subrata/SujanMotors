using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.DashboardDtos;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Financial report group. Viewing requires reports.view; file downloads additionally
/// require reports.export. Aging reports come from dbo.usp_Report_* stored procedures;
/// Profit &amp; Loss reuses IFinancialSummaryService directly (no dedicated SP — see
/// AddReportProcsBatch3 migration header for why).
/// </summary>
[ApiController]
[Route("api/v1/reports/financial")]
[HasPermission(Permissions.ReportsView)]
public class FinancialReportsController(
    IReportReadRepository reportRepository,
    IFinancialSummaryService financialSummaryService,
    IReportExportService exportService,
    IShopClock shopClock,
    ILogger<FinancialReportsController> logger) : ReportsControllerBase(exportService, shopClock)
{
    /// <summary>
    /// Aging is an as-of snapshot, not a period report — it buckets what is outstanding on a single
    /// date. fromDate/toDate were accepted and silently discarded, so callers could not tell that
    /// the range they passed had no effect on the numbers they got back.
    /// </summary>
    private IActionResult? RejectDateRange(ReportQuery query)
    {
        if (query.FromDate.HasValue || query.ToDate.HasValue)
        {
            return BadRequest(ApiError.Validation(
                "Aging reports are an as-of snapshot and do not take a date range. Use asOfDate instead of fromDate/toDate."));
        }

        return null;
    }

    /// <summary>Outstanding customer invoices bucketed by age (Current / 1-30 / 31-60 / 61-90 / 90+ days).</summary>
    [HttpPost("receivables-aging")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReportPage<ReceivablesAgingRowDto, AgingTotalsDto>))]
    public async Task<IActionResult> GetReceivablesAging([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        if (RejectDateRange(query) is { } rangeError) return rangeError;

        try
        {
            var page = await reportRepository.GetReceivablesAgingAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running receivables aging report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("receivables-aging/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportReceivablesAging(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        if (RejectDateRange(query) is { } rangeError) return rangeError;

        try
        {
            var lang = this.GetLanguage();
            var page = await reportRepository.GetReceivablesAgingAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, DocStrings.T("report.titles.receivablesAging", lang), BuildFilterSummary(query), page.Data, ReportColumnMaps.ReceivablesAging(lang), "receivables-aging");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting receivables aging report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Outstanding supplier balances bucketed by purchase order age.</summary>
    [HttpPost("payables-aging")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReportPage<PayablesAgingRowDto, AgingTotalsDto>))]
    public async Task<IActionResult> GetPayablesAging([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        if (RejectDateRange(query) is { } rangeError) return rangeError;

        try
        {
            var page = await reportRepository.GetPayablesAgingAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running payables aging report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("payables-aging/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportPayablesAging(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        if (RejectDateRange(query) is { } rangeError) return rangeError;

        try
        {
            var lang = this.GetLanguage();
            var page = await reportRepository.GetPayablesAgingAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, DocStrings.T("report.titles.payablesAging", lang), BuildFilterSummary(query), page.Data, ReportColumnMaps.PayablesAging(lang), "payables-aging");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting payables aging report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Daily expenses grouped by day or category.</summary>
    [HttpPost("expenses")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<ExpenseReportRowDto>))]
    public async Task<IActionResult> GetExpenses([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetExpensesAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running expense report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("expenses/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportExpenses(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var lang = this.GetLanguage();
            var rows = await reportRepository.GetExpensesAsync(query, cancellationToken);
            return ExportFile(format, DocStrings.T("report.titles.expenseReport", lang), BuildFilterSummary(query), rows, ReportColumnMaps.Expenses(lang), "expense-report");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting expense report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Profit &amp; Loss statement — delegates to the dashboard's financial summary so figures never drift.</summary>
    [HttpPost("profit-loss")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialSummaryResponse))]
    public async Task<IActionResult> GetProfitLoss([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var summary = await financialSummaryService.GetFinancialSummaryAsync(ToSummaryRequest(query), cancellationToken);
            return Ok(summary);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running profit & loss report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("profit-loss/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportProfitLoss(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var lang = this.GetLanguage();
            var summary = await financialSummaryService.GetFinancialSummaryAsync(ToSummaryRequest(query), cancellationToken);
            var lines = BuildStatementLines(summary, lang);
            return ExportFile(format, DocStrings.T("report.titles.profitLossStatement", lang), BuildFilterSummary(query), lines, StatementColumns(lang), "profit-loss");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting profit & loss report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    private static IReadOnlyList<ReportColumn<StatementLineDto>> StatementColumns(string lang) =>
    [
        new(DocStrings.T("report.common.lineItem", lang), r => r.Label),
        new(DocStrings.T("report.common.amount", lang), r => r.Value, ReportColumnFormat.Money)
    ];

    private static FinancialSummaryRequest ToSummaryRequest(ReportQuery query)
    {
        if (query.FromDate is null || query.ToDate is null)
            throw new ArgumentException("fromDate and toDate are required for this report.");

        return new FinancialSummaryRequest
        {
            StartDate = query.FromDate.Value.Date,
            EndDate = query.ToDate.Value.Date,
            Period = "CUSTOM"
        };
    }

    /// <summary>
    /// VAT reconciliation for a period: output VAT on sales, less output VAT reversed by credit
    /// notes, less input VAT on purchases.
    /// </summary>
    [HttpPost("vat")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(VatReportDto))]
    public async Task<IActionResult> GetVatReport([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var report = await reportRepository.GetVatReportAsync(query, cancellationToken);
            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running VAT report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Branded VAT Report PDF (the handoff document).</summary>
    [HttpPost("vat/pdf")]
    [HasPermission(Permissions.ReportsExport)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> DownloadVatReportPdf(
        [FromBody] ReportQuery query,
        [FromServices] IShopProfileProvider shopProfiles,
        [FromServices] AutoPartShop.Domain.Repositories.IApplicationSettingsRepository settingsRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await reportRepository.GetVatReportAsync(query, cancellationToken);
            var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);
            var configuredRate = await settingsRepository.GetValueAsync("VAT_RATE", cancellationToken);
            var rate = query.VatRatePercent
                ?? (decimal.TryParse(configuredRate, out var parsedRate) ? parsedRate : 15m);

            var data = new AutoPartShop.Api.Pdf.VatReportDocumentData(
                ReportNumber: $"VAT-{query.FromDate:yyyyMMdd}",
                FromDate: query.FromDate!.Value,
                ToDate: query.ToDate!.Value,
                VatRatePercent: rate,
                SalesTaxableValue: report.SalesTaxableValue,
                SalesVatAmount: report.SalesVatAmount,
                SalesInvoiceCount: report.SalesInvoiceCount,
                CreditTaxableValue: report.CreditTaxableValue,
                CreditVatAmount: report.CreditVatAmount,
                PurchaseTaxableValue: report.PurchaseTaxableValue,
                PurchaseVatAmount: report.PurchaseVatAmount,
                PurchaseOrderCount: report.PurchaseOrderCount,
                NetVatPayable: report.NetVatPayable);

            var pdfBytes = new AutoPartShop.Api.Pdf.VatReportDocument(data, shop, DocTheme.Default with { Lang = this.GetLanguage() }).GeneratePdf();
            return File(pdfBytes, "application/pdf", $"vat-report-{query.FromDate:yyyyMMdd}-{query.ToDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating VAT report PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    private static List<StatementLineDto> BuildStatementLines(FinancialSummaryResponse s, string lang)
    {
        string T(string key) => DocStrings.T($"report.profitLoss.{key}", lang);

        return
        [
            new() { Label = T("totalSales"), Value = s.TotalSales },
            new() { Label = T("cashSales"), Value = s.CashSales },
            new() { Label = T("creditSales"), Value = s.CreditSales },
            new() { Label = T("customerPaymentsReceived"), Value = s.CustomerPaymentsReceived },
            new() { Label = T("totalPurchases"), Value = s.TotalPurchases },
            new() { Label = T("supplierPaymentsMade"), Value = s.SupplierPaymentsMade },
            new() { Label = T("dailyExpenses"), Value = s.DailyExpenses },
            new() { Label = T("totalExpenses"), Value = s.TotalExpenses },
            new() { Label = T("grossProfit"), Value = s.GrossProfit },
            new() { Label = T("netProfit"), Value = s.NetProfit },
            new() { Label = T("profitMarginPercent"), Value = s.ProfitMargin },
            new() { Label = T("customerDueAmount"), Value = s.CustomerDueAmount },
            new() { Label = T("customerOverdueAmount"), Value = s.CustomerOverdueAmount },
            new() { Label = T("supplierDueAmount"), Value = s.SupplierDueAmount },
            new() { Label = T("supplierOverdueAmount"), Value = s.SupplierOverdueAmount },
            new() { Label = T("inventoryValue"), Value = s.InventoryValue },
            new() { Label = T("cashInflow"), Value = s.CashInflow },
            new() { Label = T("cashOutflow"), Value = s.CashOutflow },
            new() { Label = T("closingBalance"), Value = s.ClosingBalance }
        ];
    }
}
