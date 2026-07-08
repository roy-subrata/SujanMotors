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

        var rows = await QueryPagedAsync<SalesByProductRowDto, SalesByProductRow>(
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
            cancellationToken);

        return PagedResult<SalesByProductRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>> GetStockSummaryAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var result = await QueryPagedWithTotalsAsync<StockSummaryRowDto, StockSummaryTotalsDto>(
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
            cancellationToken);

        return new ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>
        {
            Data = result.Rows,
            Pagination = BuildPagination(pageNumber, pageSize, result.Totals.RowCount),
            Totals = result.Totals
        };
    }

    public async Task<IReadOnlyList<SalesByCategoryRowDto>> GetSalesByCategoryAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SalesByCategoryRowDto>(new CommandDefinition(
            "dbo.usp_Report_SalesByCategory",
            new { FromDate = fromDate, ToDate = toDate, query.WarehouseId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<PagedResult<SalesByCustomerRowDto>> GetSalesByCustomerAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<SalesByCustomerRowDto, SalesByCustomerRow>(
            "dbo.usp_Report_SalesByCustomer",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                CustomerType = NormalizeText(query.CustomerType),
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<SalesByCustomerRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<SalesBySalespersonRowDto>> GetSalesBySalespersonAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SalesBySalespersonRowDto>(new CommandDefinition(
            "dbo.usp_Report_SalesBySalesperson",
            new { FromDate = fromDate, ToDate = toDate, query.WarehouseId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<PagedResult<SalesReturnRowDto>> GetSalesReturnsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<SalesReturnRowDto, SalesReturnRow>(
            "dbo.usp_Report_SalesReturns",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                query.WarehouseId,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<SalesReturnRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<PaymentCollectionRowDto>> GetPaymentCollectionsAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);

        var groupBy = (query.GroupBy ?? "day").ToLowerInvariant();
        if (groupBy is not ("day" or "method"))
            throw new ArgumentException("groupBy must be one of: day, method.");

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PaymentCollectionRowDto>(new CommandDefinition(
            "dbo.usp_Report_PaymentCollections",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                GroupBy = groupBy,
                PaymentMethod = NormalizeText(query.PaymentMethod)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<PagedResult<ProfitByProductRowDto>> GetProfitByProductAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<ProfitByProductRowDto, ProfitByProductRow>(
            "dbo.usp_Report_ProfitByProduct",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                query.WarehouseId,
                query.CategoryId,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<ProfitByProductRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<LowStockRowDto>> GetLowStockAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<LowStockRowDto, LowStockRow>(
            "dbo.usp_Report_LowStock",
            new
            {
                query.WarehouseId,
                query.CategoryId,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<LowStockRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<StockMovementRowDto>> GetStockMovementsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query, MaxSummaryRangeDays);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<StockMovementRowDto, StockMovementRow>(
            "dbo.usp_Report_StockMovements",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                query.WarehouseId,
                query.PartId,
                MovementType = NormalizeText(query.MovementType),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<StockMovementRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<ExpiringLotRowDto>> GetExpiringLotsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<ExpiringLotRowDto, ExpiringLotRow>(
            "dbo.usp_Report_ExpiringLots",
            new
            {
                DaysAhead = query.DaysAhead ?? 90,
                query.WarehouseId,
                query.IncludeExpired,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<ExpiringLotRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<SlowMovingStockRowDto>> GetSlowMovingStockAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<SlowMovingStockRowDto, SlowMovingStockRow>(
            "dbo.usp_Report_SlowMovingStock",
            new
            {
                NoSaleDays = query.NoSaleDays ?? 90,
                query.WarehouseId,
                query.CategoryId,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<SlowMovingStockRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<IReadOnlyList<PurchaseSummaryRowDto>> GetPurchaseSummaryAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query, MaxSummaryRangeDays);

        var groupBy = (query.GroupBy ?? "day").ToLowerInvariant();
        if (groupBy is not ("day" or "week" or "month"))
            throw new ArgumentException("groupBy must be one of: day, week, month.");

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PurchaseSummaryRowDto>(new CommandDefinition(
            "dbo.usp_Report_PurchaseSummary",
            new { FromDate = fromDate, ToDate = toDate, GroupBy = groupBy, query.SupplierId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<PagedResult<PurchasesBySupplierRowDto>> GetPurchasesBySupplierAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<PurchasesBySupplierRowDto, PurchasesBySupplierRow>(
            "dbo.usp_Report_PurchasesBySupplier",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<PurchasesBySupplierRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<PagedResult<PurchaseReturnRowDto>> GetPurchaseReturnsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query);
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var rows = await QueryPagedAsync<PurchaseReturnRowDto, PurchaseReturnRow>(
            "dbo.usp_Report_PurchaseReturns",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                query.SupplierId,
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return PagedResult<PurchaseReturnRowDto>.Create(rows.Rows, rows.TotalCount, pageNumber, pageSize);
    }

    public async Task<ReportPage<ReceivablesAgingRowDto, AgingTotalsDto>> GetReceivablesAgingAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var totals = await QueryPagedWithTotalsAsync<ReceivablesAgingRowDto, AgingTotalsDto>(
            "dbo.usp_Report_ReceivablesAging",
            new
            {
                AsOfDate = (query.AsOfDate ?? DateTime.UtcNow).Date,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return new ReportPage<ReceivablesAgingRowDto, AgingTotalsDto>
        {
            Data = totals.Rows,
            Pagination = BuildPagination(pageNumber, pageSize, totals.Totals.RowCount),
            Totals = totals.Totals
        };
    }

    public async Task<ReportPage<PayablesAgingRowDto, AgingTotalsDto>> GetPayablesAgingAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default)
    {
        var (pageNumber, pageSize) = ResolvePaging(query, maxRowsOverride);

        var totals = await QueryPagedWithTotalsAsync<PayablesAgingRowDto, AgingTotalsDto>(
            "dbo.usp_Report_PayablesAging",
            new
            {
                AsOfDate = (query.AsOfDate ?? DateTime.UtcNow).Date,
                Search = NormalizeText(query.Search),
                PageNumber = pageNumber,
                PageSize = pageSize
            },
            cancellationToken);

        return new ReportPage<PayablesAgingRowDto, AgingTotalsDto>
        {
            Data = totals.Rows,
            Pagination = BuildPagination(pageNumber, pageSize, totals.Totals.RowCount),
            Totals = totals.Totals
        };
    }

    public async Task<IReadOnlyList<ExpenseReportRowDto>> GetExpensesAsync(
        ReportQuery query, CancellationToken cancellationToken = default)
    {
        var (fromDate, toDate) = RequireDateRange(query, MaxSummaryRangeDays);

        var groupBy = (query.GroupBy ?? "day").ToLowerInvariant();
        if (groupBy is not ("day" or "category"))
            throw new ArgumentException("groupBy must be one of: day, category.");

        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ExpenseReportRowDto>(new CommandDefinition(
            "dbo.usp_Report_Expenses",
            new
            {
                FromDate = fromDate,
                ToDate = toDate,
                GroupBy = groupBy,
                Category = NormalizeText(query.ExpenseCategory),
                PaymentMethod = NormalizeText(query.PaymentMethod)
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.ToList();
    }

    /// <summary>
    /// Runs a paged stored procedure whose result set carries COUNT(*) OVER() AS TotalCount on
    /// every row, and strips that column back out into a separate total (0 when the page is empty).
    /// </summary>
    private async Task<(IReadOnlyList<TDto> Rows, int TotalCount)> QueryPagedAsync<TDto, TRow>(
        string procedureName, object parameters, CancellationToken cancellationToken)
        where TRow : TDto, IHasTotalCount
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);
        var rows = (await connection.QueryAsync<TRow>(new CommandDefinition(
            procedureName,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken))).ToList();

        return (rows.Cast<TDto>().ToList(), rows.FirstOrDefault()?.TotalCount ?? 0);
    }

    /// <summary>
    /// Runs a paged stored procedure that returns two result sets: the requested page, then a
    /// single totals row (see usp_Report_StockSummary/ReceivablesAging/PayablesAging).
    /// </summary>
    private async Task<(IReadOnlyList<TDto> Rows, TTotals Totals)> QueryPagedWithTotalsAsync<TDto, TTotals>(
        string procedureName, object parameters, CancellationToken cancellationToken)
    {
        var connection = await GetOpenConnectionAsync(cancellationToken);
        await using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            procedureName,
            parameters,
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var rows = (await multi.ReadAsync<TDto>()).ToList();
        var totals = await multi.ReadSingleAsync<TTotals>();
        return (rows, totals);
    }

    private static PaginationMeta BuildPagination(int pageNumber, int pageSize, int totalCount) => new()
    {
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalCount = totalCount,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    };

    /// <summary>
    /// Paged stored procedures window the total with COUNT(*) OVER() on every row; these
    /// Dapper-only subclasses receive that column so the public DTOs stay free of it.
    /// </summary>
    private interface IHasTotalCount
    {
        int TotalCount { get; }
    }

    private sealed class SalesByProductRow : SalesByProductRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class SalesByCustomerRow : SalesByCustomerRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class SalesReturnRow : SalesReturnRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class ProfitByProductRow : ProfitByProductRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class LowStockRow : LowStockRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class StockMovementRow : StockMovementRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class ExpiringLotRow : ExpiringLotRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class SlowMovingStockRow : SlowMovingStockRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class PurchasesBySupplierRow : PurchasesBySupplierRowDto, IHasTotalCount
    {
        public int TotalCount { get; set; }
    }

    private sealed class PurchaseReturnRow : PurchaseReturnRowDto, IHasTotalCount
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
