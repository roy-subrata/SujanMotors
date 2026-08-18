using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Makes the sales reports deduct the same value for a return that the customer ledger and the
    /// invoice do.
    ///
    /// ReconcileSalesReportProcs started netting returns off revenue, but it used
    /// SalesReturns.RefundAmount — the gross line value. On a discounted sale that is more than the
    /// customer actually got back: returning one unit of a "2 x 100 less 20" order refunds 90, not
    /// 100, because that is what they paid for it. The report therefore understated revenue by the
    /// discount share of every return (a day with two such returns read 160 instead of 180).
    ///
    /// Invoice.ReturnedAmount holds exactly what was credited, so the summary and by-customer
    /// procedures now read that instead. SalesByProduct still nets quantities from
    /// SalesReturnLines, which are exact; only its money column shares the gross-value caveat,
    /// since apportioning an order-level discount across parts is a separate question.
    /// </summary>
    public partial class UseInvoiceReturnCreditInSalesReports : Migration
    {
        private const string SoldStatuses =
            "'CONFIRMED','READY_FOR_DELIVERY','PAID','PACKED','PARTIALLY_SHIPPED','SHIPPED','DELIVERED','COMPLETED'";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesSummary
    @FromDate    date,
    @ToDate      date,
    @GroupBy     varchar(10) = 'day',            -- day | week | month
    @WarehouseId uniqueidentifier = NULL,
    @Channel     nvarchar(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- Credited on the invoice, so this is the discount-adjusted value the customer got back.
    WITH returns AS (
        SELECT i.SalesOrderId, SUM(i.ReturnedAmount) AS RefundAmount
        FROM dbo.Invoices i
        WHERE i.Isdeleted = 0 AND i.Status <> 'CANCELLED' AND i.ReturnedAmount > 0
        GROUP BY i.SalesOrderId
    )
    SELECT
        p.PeriodStart,
        COUNT(*)                                            AS OrderCount,
        SUM(so.SubTotal)                                    AS GrossAmount,
        SUM(so.DiscountAmount)                              AS DiscountAmount,
        SUM(so.TaxAmount)                                   AS TaxAmount,
        SUM(so.TotalAmount - ISNULL(r.RefundAmount, 0))     AS NetAmount,
        SUM(so.TotalAmount + so.TaxAmount - ISNULL(r.RefundAmount, 0)) AS GrandTotal,
        CAST(SUM(so.TotalAmount - ISNULL(r.RefundAmount, 0)) / COUNT(*) AS decimal(18,2)) AS AverageOrderValue
    FROM dbo.SalesOrders so
    LEFT JOIN returns r ON r.SalesOrderId = so.Id
    CROSS APPLY (SELECT CASE @GroupBy
                     WHEN 'month' THEN DATEFROMPARTS(YEAR(so.SODate), MONTH(so.SODate), 1)
                     WHEN 'week'  THEN CAST(DATEADD(week, DATEDIFF(week, 0, so.SODate), 0) AS date)
                     ELSE CAST(so.SODate AS date)
                 END AS PeriodStart) p
    WHERE so.Isdeleted = 0
      AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
      AND so.Status IN ({SoldStatuses})
      AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
      AND (@Channel IS NULL OR so.Channel = @Channel)
    GROUP BY p.PeriodStart
    ORDER BY p.PeriodStart;
END
");

            migrationBuilder.Sql($@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesByCustomer
    @FromDate     date,
    @ToDate       date,
    @CustomerType nvarchar(20) = NULL,
    @Search       nvarchar(100) = NULL,
    @PageNumber   int = 1,
    @PageSize     int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    WITH returns AS (
        SELECT i.SalesOrderId, SUM(i.ReturnedAmount) AS RefundAmount
        FROM dbo.Invoices i
        WHERE i.Isdeleted = 0 AND i.Status <> 'CANCELLED' AND i.ReturnedAmount > 0
        GROUP BY i.SalesOrderId
    ),
    agg AS (
        SELECT
            c.Id                              AS CustomerId,
            c.CustomerCode,
            c.FirstName + N' ' + c.LastName   AS CustomerName,
            c.CustomerType,
            COUNT(*)                          AS OrderCount,
            SUM(so.TotalAmount + so.TaxAmount - ISNULL(r.RefundAmount, 0)) AS Revenue,
            SUM(so.PaidAmount)                AS PaidAmount,
            SUM(CASE WHEN so.TotalAmount + so.TaxAmount - ISNULL(r.RefundAmount, 0) - so.PaidAmount > 0
                     THEN so.TotalAmount + so.TaxAmount - ISNULL(r.RefundAmount, 0) - so.PaidAmount
                     ELSE 0 END)              AS Outstanding,
            MAX(so.SODate)                    AS LastPurchaseDate
        FROM dbo.SalesOrders so
        JOIN dbo.Customers c ON c.Id = so.CustomerId AND c.Isdeleted = 0
        LEFT JOIN returns r ON r.SalesOrderId = so.Id
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status IN ({SoldStatuses})
          AND (@CustomerType IS NULL OR c.CustomerType = @CustomerType)
          AND (@Search IS NULL
               OR c.CustomerCode LIKE N'%' + @Search + N'%'
               OR c.FirstName LIKE N'%' + @Search + N'%'
               OR c.LastName LIKE N'%' + @Search + N'%')
        GROUP BY c.Id, c.CustomerCode, c.FirstName, c.LastName, c.CustomerType
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY Revenue DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverting leaves the ReconcileSalesReportProcs definitions in place; drop these two so
            // re-applying that migration restores them.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesSummary;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByCustomer;");
        }
    }
}
