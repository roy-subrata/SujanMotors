using AutoPartShop.Api.Authorization;
using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;

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

    /// <summary>
    /// Branded Stock Report PDF (the handoff document), as opposed to the generic xlsx/pdf export.
    /// Fetches the whole filtered set (capped) and shows a LOW/OK status per line.
    /// </summary>
    [HttpPost("stock-summary/report-pdf")]
    [HasPermission(Permissions.ReportsExport)]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> DownloadStockReport(
        [FromBody] ReportQuery query,
        [FromServices] IShopProfileProvider shopProfiles,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetStockSummaryAsync(query, ExportRowCap, cancellationToken);
            var shop = await shopProfiles.GetAsync(cancellationToken: cancellationToken);

            // Header label from the data itself: one warehouse → its name, otherwise "All Warehouses".
            var warehouses = page.Data.Select(r => r.WarehouseName).Distinct().ToList();
            var warehouseLabel = warehouses.Count == 1 ? warehouses[0] : "All Warehouses";

            var asOf = query.AsOfDate ?? DateTime.Now;
            var data = new AutoPartShop.Api.Pdf.StockReportDocumentData(
                ReportNumber: $"STK-{asOf:yyyyMMdd}",
                AsOf: asOf,
                WarehouseLabel: warehouseLabel,
                Rows: page.Data,
                TotalStockValue: page.Totals?.TotalStockValue ?? page.Data.Sum(r => r.StockValue));

            var pdfBytes = new AutoPartShop.Api.Pdf.StockReportDocument(data, shop).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"stock-report-{asOf:yyyyMMdd}.pdf");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating stock report PDF");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Parts at or below their configured minimum stock threshold (MinimumStock &gt; 0, opt-in).</summary>
    [HttpPost("low-stock")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<LowStockRowDto>))]
    public async Task<IActionResult> GetLowStock([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetLowStockAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running low stock report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("low-stock/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportLowStock(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetLowStockAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Low Stock", BuildFilterSummary(query), page.Data, ReportColumnMaps.LowStock, "low-stock");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting low stock report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Stock movement audit ledger (IN/OUT/RETURN/ADJUST/TRANSFER).</summary>
    [HttpPost("stock-movements")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<StockMovementRowDto>))]
    public async Task<IActionResult> GetStockMovements([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetStockMovementsAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running stock movements report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("stock-movements/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportStockMovements(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetStockMovementsAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Stock Movements", BuildFilterSummary(query), page.Data, ReportColumnMaps.StockMovements, "stock-movements");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting stock movements report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>AVAILABLE lots expiring within the given horizon (default 90 days).</summary>
    [HttpPost("expiring-lots")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<ExpiringLotRowDto>))]
    public async Task<IActionResult> GetExpiringLots([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetExpiringLotsAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running expiring lots report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("expiring-lots/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportExpiringLots(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetExpiringLotsAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Expiring Lots", BuildFilterSummary(query), page.Data, ReportColumnMaps.ExpiringLots, "expiring-lots");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting expiring lots report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    /// <summary>Stock with no sale in the given window (default 90 days) — dead/slow-moving stock.</summary>
    [HttpPost("slow-moving")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<SlowMovingStockRowDto>))]
    public async Task<IActionResult> GetSlowMovingStock([FromBody] ReportQuery query, CancellationToken cancellationToken)
    {
        try
        {
            var page = await reportRepository.GetSlowMovingStockAsync(query, cancellationToken: cancellationToken);
            return Ok(page);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error running slow-moving stock report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }

    [HttpPost("slow-moving/export")]
    [HasPermission(Permissions.ReportsExport)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileResult))]
    public async Task<IActionResult> ExportSlowMovingStock(
        [FromBody] ReportQuery query, [FromQuery] string format = "xlsx", CancellationToken cancellationToken = default)
    {
        try
        {
            var page = await reportRepository.GetSlowMovingStockAsync(query, ExportRowCap, cancellationToken);
            return ExportFile(format, "Slow-Moving Stock", BuildFilterSummary(query), page.Data, ReportColumnMaps.SlowMovingStock, "slow-moving-stock");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiError.Validation(ex.Message));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting slow-moving stock report");
            return StatusCode(StatusCodes.Status500InternalServerError, ApiError.Internal(HttpContext.TraceIdentifier));
        }
    }
}
