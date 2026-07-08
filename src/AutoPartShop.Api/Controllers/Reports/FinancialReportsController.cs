using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.DashboardDtos;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Financial report group. Viewing requires reports.view; file downloads additionally
/// require reports.export. Aging reports come from dbo.usp_Report_* stored procedures;
/// Profit &amp; Loss reuses IFinancialSummaryService directly (no dedicated SP — see
/// AddReportProcsBatch3 migration header for why).
/// </summary>
[ApiController]
[Route("api/reports/financial")]
[Route("api/v1/reports/financial")]
[HasPermission(Permissions.ReportsView)]
public class FinancialReportsController(
    IReportReadRepository reportRepository,
    IFinancialSummaryService financialSummaryService,
    IReportExportService exportService,
    ILogger<FinancialReportsController> logger) : ReportsControllerBase(exportService)
{
    /// <summary>Outstanding customer invoices bucketed by age (Current / 1-30 / 31-60 / 61-90 / 90+ days).</summary>
    [HttpPost("receivables-aging")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReportPage<ReceivablesAgingRowDto, AgingTotalsDto>))]
    public async Task<IActionResult> GetReceivablesAging([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
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
        try
        {
            var page = await reportRepository.GetReceivablesAgingAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Receivables Aging", BuildFilterSummary(query), page.Data, ReportColumnMaps.ReceivablesAging, "receivables-aging");
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
        try
        {
            var page = await reportRepository.GetPayablesAgingAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Payables Aging", BuildFilterSummary(query), page.Data, ReportColumnMaps.PayablesAging, "payables-aging");
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
            var rows = await reportRepository.GetExpensesAsync(query, cancellationToken);
            return ExportFile(format, "Expense Report", BuildFilterSummary(query), rows, ReportColumnMaps.Expenses, "expense-report");
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
            var summary = await financialSummaryService.GetFinancialSummaryAsync(ToSummaryRequest(query), cancellationToken);
            var lines = BuildStatementLines(summary);
            return ExportFile(format, "Profit & Loss Statement", BuildFilterSummary(query), lines, StatementColumns, "profit-loss");
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

    private static readonly IReadOnlyList<ReportColumn<StatementLineDto>> StatementColumns =
    [
        new("Line Item", r => r.Label),
        new("Amount", r => r.Value, ReportColumnFormat.Money)
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

    private static List<StatementLineDto> BuildStatementLines(FinancialSummaryResponse s) =>
    [
        new() { Label = "Total Sales", Value = s.TotalSales },
        new() { Label = "Cash Sales", Value = s.CashSales },
        new() { Label = "Credit Sales", Value = s.CreditSales },
        new() { Label = "Customer Payments Received", Value = s.CustomerPaymentsReceived },
        new() { Label = "Total Purchases", Value = s.TotalPurchases },
        new() { Label = "Supplier Payments Made", Value = s.SupplierPaymentsMade },
        new() { Label = "Daily Expenses", Value = s.DailyExpenses },
        new() { Label = "Total Expenses", Value = s.TotalExpenses },
        new() { Label = "Gross Profit", Value = s.GrossProfit },
        new() { Label = "Net Profit", Value = s.NetProfit },
        new() { Label = "Profit Margin %", Value = s.ProfitMargin },
        new() { Label = "Customer Due Amount", Value = s.CustomerDueAmount },
        new() { Label = "Customer Overdue Amount", Value = s.CustomerOverdueAmount },
        new() { Label = "Supplier Due Amount", Value = s.SupplierDueAmount },
        new() { Label = "Supplier Overdue Amount", Value = s.SupplierOverdueAmount },
        new() { Label = "Inventory Value", Value = s.InventoryValue },
        new() { Label = "Cash Inflow", Value = s.CashInflow },
        new() { Label = "Cash Outflow", Value = s.CashOutflow },
        new() { Label = "Closing Balance", Value = s.ClosingBalance }
    ];
}
