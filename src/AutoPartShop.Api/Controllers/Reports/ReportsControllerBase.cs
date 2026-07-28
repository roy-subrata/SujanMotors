using AutoPartShop.Api.Common;
using AutoPartShop.Api.Services;
using AutoPartShop.Application.DTOs.ReportDtos;
using Microsoft.AspNetCore.Mvc;

namespace AutoPartShop.Api.Controllers.Reports;

/// <summary>
/// Shared plumbing for the report group controllers: export file negotiation and the
/// human-readable filter line stamped on exported files.
/// </summary>
public abstract class ReportsControllerBase(IReportExportService exportService, IShopClock shopClock) : ControllerBase
{
    /// <summary>
    /// The shop's business clock. Report defaults and generated-on stamps use the shop's
    /// calendar day rather than the server's, which on a UTC host is a different date for
    /// the first hours of every local morning.
    /// </summary>
    protected IShopClock ShopClock { get; } = shopClock;

    /// <summary>
    /// Hard cap on rows in an exported file. Exports re-run the report unpaged; this keeps a
    /// runaway filter from producing a 100 MB workbook. UI shows page data only, so the cap
    /// only affects downloads.
    /// </summary>
    protected const int ExportRowCap = 10_000;

    protected IActionResult ExportFile<T>(
        string format,
        string title,
        string? filterSummary,
        IReadOnlyList<T> rows,
        IReadOnlyList<ReportColumn<T>> columns,
        string fileSlug)
    {
        var meta = new ReportExportMeta(title, filterSummary);
        // Filename stamp is the shop's calendar day, so a file downloaded at 09:00 local is
        // named for today rather than yesterday.
        var stamp = ShopClock.Now.ToString("yyyyMMdd");

        return format.ToLowerInvariant() switch
        {
            "xlsx" => File(
                exportService.ToXlsx(meta, rows, columns),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{fileSlug}-{stamp}.xlsx"),
            "pdf" => File(
                exportService.ToPdf(meta, rows, columns),
                "application/pdf",
                $"{fileSlug}-{stamp}.pdf"),
            _ => BadRequest(ApiError.Validation("format must be 'xlsx' or 'pdf'."))
        };
    }

    /// <summary>Filter line for export headers, e.g. "01 Jun 2026 – 30 Jun 2026 · Search: bolt".</summary>
    protected string BuildFilterSummary(ReportQuery query)
    {
        var parts = new List<string>();
        if (query.FromDate is not null || query.ToDate is not null)
            parts.Add($"{query.FromDate:dd MMM yyyy} – {query.ToDate:dd MMM yyyy}");
        if (!string.IsNullOrWhiteSpace(query.Search))
            parts.Add($"Search: {query.Search}");
        if (!string.IsNullOrWhiteSpace(query.Channel))
            parts.Add($"Channel: {query.Channel}");
        parts.Add($"Generated {ShopClock.Now:dd MMM yyyy HH:mm}");
        return string.Join("  ·  ", parts);
    }
}
