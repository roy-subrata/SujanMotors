using AutoPartShop.Application.Common;

namespace AutoPartShop.Application.DTOs.ReportDtos;

/// <summary>
/// Paged report response with an optional grand-totals block computed over the whole
/// filtered set (not just the current page). Mirrors PagedResult's { data, pagination }
/// shape so the frontend handles paged reports with and without totals uniformly.
/// </summary>
public class ReportPage<TRow, TTotals> where TTotals : class
{
    public IReadOnlyList<TRow> Data { get; set; } = [];
    public PaginationMeta Pagination { get; set; } = new();
    public TTotals? Totals { get; set; }
}
