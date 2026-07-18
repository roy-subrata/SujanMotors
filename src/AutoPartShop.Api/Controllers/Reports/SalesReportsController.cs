using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Api.Pdf;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Sales report group. Viewing requires reports.view; file downloads additionally
/// require reports.export. Data comes from dbo.usp_Report_* stored procedures.
/// </summary>
[ApiController]
[Route("api/reports/sales")]
[Route("api/v1/reports/sales")]
[HasPermission(Permissions.ReportsView)]
public class SalesReportsController(
    IReportReadRepository reportRepository,
    IReportExportService exportService,
    ILogger<SalesReportsController> logger) : ReportsControllerBase(exportService)
{
    /// <summary>Sales summary bucketed by day/week/month.</summary>
    [HttpPost("summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SalesSummaryRowDto>))]
    public async Task<IActionResult> GetSalesSummary([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetSalesSummaryAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("summary/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesSummary(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetSalesSummaryAsync(query, cancellationToken);
            return ExportFile(format, "Sales Summary", BuildFilterSummary(query), rows, ReportColumnMaps.SalesSummary, "sales-summary");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales aggregated per product, ordered by net revenue.</summary>
    [HttpPost("by-product")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<SalesByProductRowDto>))]
    public async Task<IActionResult> GetSalesByProduct([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetSalesByProductAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales by product report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-product/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesByProduct(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetSalesByProductAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Sales by Product", BuildFilterSummary(query), page.Data, ReportColumnMaps.SalesByProduct, "sales-by-product");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales by product report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales aggregated per category, with each category's share of total net revenue.</summary>
    [HttpPost("by-category")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SalesByCategoryRowDto>))]
    public async Task<IActionResult> GetSalesByCategory([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetSalesByCategoryAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales by category report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-category/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesByCategory(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetSalesByCategoryAsync(query, cancellationToken);
            return ExportFile(format, "Sales by Category", BuildFilterSummary(query), rows, ReportColumnMaps.SalesByCategory, "sales-by-category");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales by category report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales aggregated per customer: revenue, paid amount, outstanding balance.</summary>
    [HttpPost("by-customer")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<SalesByCustomerRowDto>))]
    public async Task<IActionResult> GetSalesByCustomer([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetSalesByCustomerAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales by customer report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-customer/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesByCustomer(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetSalesByCustomerAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Sales by Customer", BuildFilterSummary(query), page.Data, ReportColumnMaps.SalesByCustomer, "sales-by-customer");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales by customer report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales aggregated per salesperson (technician); orders with no technician group as "Unassigned".</summary>
    [HttpPost("by-salesperson")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SalesBySalespersonRowDto>))]
    public async Task<IActionResult> GetSalesBySalesperson([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetSalesBySalespersonAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales by salesperson report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-salesperson/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesBySalesperson(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetSalesBySalespersonAsync(query, cancellationToken);
            return ExportFile(format, "Sales by Salesperson", BuildFilterSummary(query), rows, ReportColumnMaps.SalesBySalesperson, "sales-by-salesperson");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales by salesperson report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales aggregated per cashier — the staff user who actually processed the sale (distinct from the assigned Technician).</summary>
    [HttpPost("by-cashier")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<SalesByCashierRowDto>))]
    public async Task<IActionResult> GetSalesByCashier([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetSalesByCashierAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales by cashier report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-cashier/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesByCashier(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetSalesByCashierAsync(query, cancellationToken);
            return ExportFile(format, "Sales by Cashier", BuildFilterSummary(query), rows, ReportColumnMaps.SalesByCashier, "sales-by-cashier");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales by cashier report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Sales return documents in the period.</summary>
    [HttpPost("returns")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<SalesReturnRowDto>))]
    public async Task<IActionResult> GetSalesReturns([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetSalesReturnsAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running sales returns report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("returns/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSalesReturns(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetSalesReturnsAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Sales Returns", BuildFilterSummary(query), page.Data, ReportColumnMaps.SalesReturns, "sales-returns");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting sales returns report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Customer payment collections grouped by day or payment method (cash-basis, COMPLETED payments only).</summary>
    [HttpPost("payment-collections")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PaymentCollectionRowDto>))]
    public async Task<IActionResult> GetPaymentCollections([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetPaymentCollectionsAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running payment collections report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("payment-collections/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportPaymentCollections(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetPaymentCollectionsAsync(query, cancellationToken);
            return ExportFile(format, "Payment Collections", BuildFilterSummary(query), rows, ReportColumnMaps.PaymentCollections, "payment-collections");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting payment collections report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Per-product profit: net revenue vs. actual lot cost (COGS) and margin.</summary>
    [HttpPost("profit-by-product")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ProfitByProductRowDto>))]
    public async Task<IActionResult> GetProfitByProduct([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetProfitByProductAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running profit by product report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("profit-by-product/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportProfitByProduct(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetProfitByProductAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Profit by Product", BuildFilterSummary(query), page.Data, ReportColumnMaps.ProfitByProduct, "profit-by-product");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting profit by product report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>
    /// Daily Sales (Z) Report PDF — the handoff document. Composed from several report procs for a
    /// single business day (FromDate/ToDate should bound one day): sales summary, sales returns,
    /// payment collections by method, and sales by category.
    /// </summary>
    [HttpPost("daily-z-report/pdf")]
    [HasPermission(Permissions.ReportsExport)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> DownloadDailyZReport(
        [FromBody] ReportQuery query,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Each sub-report needs its own GroupBy; clone the query so filters don't leak between them.
            ReportQuery With(string? groupBy) => new()
            {
                FromDate = query.FromDate,
                ToDate = query.ToDate,
                WarehouseId = query.WarehouseId,
                GroupBy = groupBy,
            };

            var summaryRows = await reportRepository.GetSalesSummaryAsync(With("day"), cancellationToken);
            var categoryRows = await reportRepository.GetSalesByCategoryAsync(With(null), cancellationToken);
            var paymentRows = await reportRepository.GetPaymentCollectionsAsync(With("method"), cancellationToken);
            var returns = await reportRepository.GetSalesReturnsAsync(With(null), ExportRowCap, cancellationToken);

            var gross = summaryRows.Sum(r => r.GrossAmount);
            var discounts = summaryRows.Sum(r => r.DiscountAmount);
            var vat = summaryRows.Sum(r => r.TaxAmount);
            var receipts = summaryRows.Sum(r => r.OrderCount);
            var returnsTotal = returns.Data.Sum(r => r.RefundAmount);
            // Reconciliation identity shown on the report: Gross − Returns − Discounts = Net.
            var net = gross - returnsTotal - discounts;

            var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);
            var businessDate = query.FromDate ?? DateTime.Now;

            var data = new DailySalesReportData(
                ReportNumber: $"Z-{businessDate:yyyyMMdd}",
                BusinessDate: businessDate,
                BusinessDayLabel: query.FromDate is { } f && query.ToDate is { } t && f.Date == t.Date
                    ? f.ToString("dd MMM yyyy")
                    : $"{query.FromDate:dd MMM} – {query.ToDate:dd MMM yyyy}",
                // Deliberately shop-wide, not per-terminal — this is the whole-day summary across
                // every cashier/counter. TillSession (cashier-shifts feature) now exists and gives
                // the per-terminal, per-shift equivalent via the Shift Report instead.
                TerminalLabel: "All Tills",
                GrossSales: gross,
                ReturnsAmount: returnsTotal,
                DiscountsAmount: discounts,
                NetSales: net,
                VatCollected: vat,
                ReceiptCount: receipts,
                Payments: paymentRows
                    .Select(p => new ZReportPaymentRow(FormatMethod(p.GroupKey), p.PaymentCount, p.TotalAmount))
                    .ToList(),
                Categories: categoryRows
                    .OrderByDescending(c => c.NetRevenue)
                    .Select(c => new ZReportCategoryRow(c.CategoryName, c.NetRevenue))
                    .ToList(),
                Note: "");

            var pdfBytes = new DailySalesReportDocument(data, shop).GeneratePdf();
            return File(pdfBytes, "application/pdf", $"daily-sales-report-{businessDate:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating daily Z report PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    private static string FormatMethod(string method) => method switch
    {
        "CASH" => "Cash",
        "CARD" => "Card",
        "MOBILE_BANKING" => "Mobile Banking",
        "BANK_TRANSFER" => "Bank Transfer",
        "ADVANCE_CREDIT" => "Credit Applied",
        _ => string.IsNullOrWhiteSpace(method) ? "Other" : method.Replace('_', ' ')
    };
}
