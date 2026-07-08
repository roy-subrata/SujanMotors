using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;

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
}
