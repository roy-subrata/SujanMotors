using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Reconciles the sales reports with the invoices and the customer ledger (QA finding F24).
    ///
    /// Three defects shared one root: the reports treated "not cancelled" as "sold".
    ///   1. PENDING orders were counted. An order that was never confirmed has deducted no stock
    ///      and raised no invoice, so counting it inflated revenue with sales that never happened.
    ///   2. Returns were never deducted, so a returned unit stayed in quantitySold and its refund
    ///      stayed in revenue.
    ///   3. SalesByCustomer.Outstanding derived from SalesOrder alone and ignored returns, which
    ///      is why it reported 1511.65 against a ledger balance of 150.55.
    ///
    /// Only the affected procedures are rewritten; the rest of the report suite is untouched.
    /// Refunds are attributed to the period of the originating order, so a return always nets
    /// against the sale it reverses rather than distorting the period it was processed in.
    /// </summary>
    public partial class ReconcileSalesReportProcs : Migration
    {
        /// <summary>
        /// A sale counts once the order is CONFIRMED. PENDING and DRAFT are not sales yet;
        /// CANCELLED and RETURNED are not sales any more.
        /// </summary>
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

    WITH returns AS (
        SELECT sr.SalesOrderId, SUM(sr.RefundAmount) AS RefundAmount
        FROM dbo.SalesReturns sr
        WHERE sr.Isdeleted = 0 AND sr.Status = 'PROCESSED'
        GROUP BY sr.SalesOrderId
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
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesByProduct
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL,
    @CategoryId  uniqueidentifier = NULL,
    @BrandId     uniqueidentifier = NULL,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    WITH ret AS (
        SELECT srl.PartId,
               SUM(srl.Quantity)                 AS Quantity,
               SUM(srl.Quantity * srl.UnitPrice) AS Amount
        FROM dbo.SalesReturnLines srl
        JOIN dbo.SalesReturns sr ON sr.Id = srl.SalesReturnId AND sr.Isdeleted = 0 AND sr.Status = 'PROCESSED'
        JOIN dbo.SalesOrders so  ON so.Id = sr.SalesOrderId   AND so.Isdeleted = 0
        WHERE srl.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
        GROUP BY srl.PartId
    ),
    sold AS (
        SELECT
            p.Id            AS PartId,
            p.PartNumber,
            p.Name          AS PartName,
            p.SKU           AS Sku,
            c.Name          AS CategoryName,
            b.Name          AS BrandName,
            SUM(l.Quantity)                 AS QuantitySold,
            SUM(l.Quantity * l.UnitPrice)   AS GrossRevenue,
            SUM(l.Quantity * l.Discount)    AS DiscountAmount
        FROM dbo.SalesOrders so
        JOIN dbo.SalesOrderLine l ON l.SalesOrderId = so.Id AND l.Isdeleted = 0
        JOIN dbo.Parts p          ON p.Id = l.PartId        AND p.Isdeleted = 0
        LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId   AND c.Isdeleted = 0
        LEFT JOIN dbo.Brands b     ON b.Id = p.BrandId      AND b.Isdeleted = 0
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status IN ({SoldStatuses})
          AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
          AND (@BrandId IS NULL OR p.BrandId = @BrandId)
          AND (@Search IS NULL
               OR p.Name LIKE N'%' + @Search + N'%'
               OR p.PartNumber LIKE N'%' + @Search + N'%'
               OR p.SKU LIKE N'%' + @Search + N'%')
        GROUP BY p.Id, p.PartNumber, p.Name, p.SKU, c.Name, b.Name
    ),
    agg AS (
        SELECT
            sold.PartId, sold.PartNumber, sold.PartName, sold.Sku,
            sold.CategoryName, sold.BrandName,
            sold.QuantitySold - ISNULL(ret.Quantity, 0)                      AS QuantitySold,
            sold.GrossRevenue - ISNULL(ret.Amount, 0)                        AS GrossRevenue,
            sold.DiscountAmount                                              AS DiscountAmount,
            sold.GrossRevenue - sold.DiscountAmount - ISNULL(ret.Amount, 0)  AS NetRevenue
        FROM sold
        LEFT JOIN ret ON ret.PartId = sold.PartId
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY NetRevenue DESC, PartName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
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
        SELECT sr.SalesOrderId, SUM(sr.RefundAmount) AS RefundAmount
        FROM dbo.SalesReturns sr
        WHERE sr.Isdeleted = 0 AND sr.Status = 'PROCESSED'
        GROUP BY sr.SalesOrderId
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
            // The previous definitions live in AddReportProcsBatch1/Batch2; rolling this back
            // drops the procedures so re-applying those migrations restores them.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesSummary;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByProduct;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByCustomer;");
        }
    }
}
