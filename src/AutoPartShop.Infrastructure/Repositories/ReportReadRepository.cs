using System.Data;
using System.Data.Common;
using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using Dapper;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Infrastructure.Repositories;

/// <summary>
/// Dapper-based reader for the dbo.usp_Report_* stored procedures.
/// Reuses the DbContext's connection (EF owns and disposes it) so reports share the
/// request scope's connection string and pooling. Note: Dapper calls bypass EF's
/// EnableRetryOnFailure execution strategy — acceptable for read-only report queries,
/// where a transient failure surfaces as a 500 and the user simply re-runs the report.
/// </summary>
public class ReportReadRepository : IReportReadRepository
{
    /// <summary>
    /// Widest date range a period-bucketed report accepts; guards against a
    /// row-per-day result set exploding on an unbounded range.
    /// </summary>
    private const int MaxSummaryRangeDays = 731;

    private readonly AutoPartDbContext _dbContext;

    public ReportReadRepository(AutoPartDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SalesSummaryRowDto>> GetSalesSummaryAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query, MaxSummaryRangeDays);

        var groupBy = (query.GroupBy ?? "day").ToLowerInvariant();
        if (groupBy is not ("day" or "week" or "month"))
            throw new ArgumentException("groupBy must be one of: day, week, month.");

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SalesSummaryRowDto>(new CommandDefinition(
            "dbo.usp_Report_SalesSummary",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                GroupBy = groupBy,
                query.WarehouseId,
                Channel = NormalizeText(query.Channel)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<PagedResult<SalesByProductRowDto>> GetSalesByProductAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<SalesByProductRow>(new CommandDefinition(
            "dbo.usp_Report_SalesByProduct",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                query.WarehouseId,
                query.CategoryId,
                query.BrandId,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken))).ToList();

        return PagedResult<SalesByProductRowDto>.Create(rows, rows.FirstOrDefault()?.TotalCount ?? 0, pageNumber, pageSize);
    }

    public async Task<ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>> GetStockSummaryAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var connection = await GetOpenConnectionAsync(cancellationToken);
        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            "dbo.usp_Report_StockSummary",
            new
            {
                query.WarehouseId,
                query.CategoryId,
                query.BrandId,
                Search = NormalizeText(query.Search),
                query.IncludeZeroStock,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var rows = (await multi.ReadAsync<StockSummaryRowDto>()).ToList();
        var totals = await multi.ReadSingleAsync<StockSummaryTotalsDto>();

        return new ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>
        {
            Data = rows,
            Pagination = new PaginationMeta
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = totals.RowCount,
                TotalPages = (int)Math.Ceiling(totals.RowCount / (double)pageSize)
            },
            Totals = totals
        };
    }

    /// <summary>
    /// Paged stored procedures window the total with COUNT(*) OVER() on every row; these
    /// Dapper-only subclasses receive that column so the public DTOs stay free of it.
    /// </summary>
    private sealed class SalesByProductRow : SalesByProductRowDto
    {
        public int TotalCount { get; set; }
    }

    private async Task<DbConnection> GetOpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = _dbContext.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static (DateTime FromDate, DateTime ToDate) RequireDateRange(ReportQuery query, int? maxRangeDays = null)
    {
        if (query.FromDate is null || query.ToDate is null)
            throw new ArgumentException("fromDate and toDate are required for this report.");

        var from = query.FromDate.Value.Date;
        var to = query.ToDate.Value.Date;

        if (from > to)
            throw new ArgumentException("fromDate must not be after toDate.");
        if (maxRangeDays is not null && (to - from).TotalDays > maxRangeDays.Value)
            throw new ArgumentException($"Date range too large; maximum is {maxRangeDays} days for this report.");

        return (from, to);
    }

    private static (int PageNumber, int PageSize) ResolvePaging(ReportQuery query, int? maxRowsOverride)
        => maxRowsOverride is int max ? (1, max) : (query.PageNumber, query.PageSize);

    private static string? NormalizeText(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
