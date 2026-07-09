using AutoPartShop.Application.Common;
using AutoPartShop.Application.DTOs.ReportDtos;

namespace AutoPartShop.Application.Reports;

/// <summary>
/// Read side of the Reports module. Every method executes a dbo.usp_Report_* stored procedure
/// via Dapper — reports are wide multi-table aggregations that are clearer and faster in SQL.
/// Paged methods accept <c>maxRowsOverride</c> so export endpoints can pull the full filtered
/// set (capped by the caller) past BaseQuery's page-size clamp.
/// </summary>
public interface IReportReadRepository
{
    Task<IReadOnlyList<SalesSummaryRowDto>> GetSalesSummaryAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<SalesByProductRowDto>> GetSalesByProductAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<ReportPage<StockSummaryRowDto, StockSummaryTotalsDto>> GetStockSummaryAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesByCategoryRowDto>> GetSalesByCategoryAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<SalesByCustomerRowDto>> GetSalesByCustomerAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesBySalespersonRowDto>> GetSalesBySalespersonAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SalesByCashierRowDto>> GetSalesByCashierAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<SalesReturnRowDto>> GetSalesReturnsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentCollectionRowDto>> GetPaymentCollectionsAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<ProfitByProductRowDto>> GetProfitByProductAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<PagedResult<LowStockRowDto>> GetLowStockAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<PagedResult<StockMovementRowDto>> GetStockMovementsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<PagedResult<ExpiringLotRowDto>> GetExpiringLotsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<PagedResult<SlowMovingStockRowDto>> GetSlowMovingStockAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseSummaryRowDto>> GetPurchaseSummaryAsync(
        ReportQuery query, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchasesBySupplierRowDto>> GetPurchasesBySupplierAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<PagedResult<PurchaseReturnRowDto>> GetPurchaseReturnsAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<ReportPage<ReceivablesAgingRowDto, AgingTotalsDto>> GetReceivablesAgingAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<ReportPage<PayablesAgingRowDto, AgingTotalsDto>> GetPayablesAgingAsync(
        ReportQuery query, int? maxRowsOverride = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ExpenseReportRowDto>> GetExpensesAsync(
        ReportQuery query, CancellationToken cancellationToken = default);
}
