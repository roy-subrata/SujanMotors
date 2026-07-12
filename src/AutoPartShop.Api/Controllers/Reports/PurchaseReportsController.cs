using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Purchase report group. Viewing requires reports.view; file downloads additionally
/// require reports.export. Data comes from dbo.usp_Report_* stored procedures.
/// </summary>
[ApiController]
[Route("api/reports/purchase")]
[Route("api/v1/reports/purchase")]
[HasPermission(Permissions.ReportsView)]
public class PurchaseReportsController(
    IReportReadRepository reportRepository,
    IReportExportService exportService,
    ILogger<PurchaseReportsController> logger) : ReportsControllerBase(exportService)
{
    /// <summary>Purchase order totals bucketed by day/week/month.</summary>
    [HttpPost("summary")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<PurchaseSummaryRowDto>))]
    public async Task<IActionResult> GetPurchaseSummary([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var rows = await reportRepository.GetPurchaseSummaryAsync(query, cancellationToken);
            return Ok(rows);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running purchase summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("summary/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportPurchaseSummary(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var rows = await reportRepository.GetPurchaseSummaryAsync(query, cancellationToken);
            return ExportFile(format, "Purchase Summary", BuildFilterSummary(query), rows, ReportColumnMaps.PurchaseSummary, "purchase-summary");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting purchase summary report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Purchases aggregated per supplier for the selected period (not an all-time balance).</summary>
    [HttpPost("by-supplier")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<PurchasesBySupplierRowDto>))]
    public async Task<IActionResult> GetPurchasesBySupplier([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetPurchasesBySupplierAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running purchases by supplier report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("by-supplier/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportPurchasesBySupplier(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetPurchasesBySupplierAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Purchases by Supplier", BuildFilterSummary(query), page.Data, ReportColumnMaps.PurchasesBySupplier, "purchases-by-supplier");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting purchases by supplier report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Purchase return documents in the period.</summary>
    [HttpPost("returns")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<PurchaseReturnRowDto>))]
    public async Task<IActionResult> GetPurchaseReturns([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetPurchaseReturnsAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running purchase returns report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("returns/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportPurchaseReturns(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetPurchaseReturnsAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Purchase Returns", BuildFilterSummary(query), page.Data, ReportColumnMaps.PurchaseReturns, "purchase-returns");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting purchase returns report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }
}
