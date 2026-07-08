using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Inventory report group. Viewing requires reports.view; file downloads additionally
/// require reports.export. Data comes from dbo.usp_Report_* stored procedures.
/// </summary>
[ApiController]
[Route("api/reports/inventory")]
[Route("api/v1/reports/inventory")]
[HasPermission(Permissions.ReportsView)]
public class InventoryReportsController(
    IReportReadRepository reportRepository,
    IReportExportService exportService,
    ILogger<InventoryReportsController> logger) : ReportsControllerBase(exportService)
{
    /// <summary>Current stock per part/variant/warehouse with lot-based valuation and grand totals.</summary>
    [HttpPost("stock-summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>))]
    public async Task<IActionResult> GetStockSummary([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetStockSummaryAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running stock summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("stock-summary/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportStockSummary(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetStockSummaryAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Stock Summary & Valuation", BuildFilterSummary(query), page.Data, ReportColumnMaps.StockSummary, "stock-summary");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting stock summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }
}
