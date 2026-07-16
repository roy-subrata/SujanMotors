using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Reports module, batch 1: pilot stored procedures read by ReportReadRepository (Dapper).
    ///
    /// Rules shared by every usp_Report_* procedure — kept identical to FinancialSummaryService
    /// so report figures reconcile with the dashboard:
    ///   - Isdeleted = 0 on every table.
    ///   - Date windows: >= @FromDate AND < DATEADD(day, 1, @ToDate)  (@ToDate inclusive).
    ///   - Sales exclude Status IN ('DRAFT','CANCELLED','RETURNED').
    ///   - Lot valuation: Status = 'AVAILABLE' AND QuantityAvailable > 0, value = qty * CostPrice.
    ///   - Amounts are raw sums assumed to be base currency (BDT); SQL-side grouping cannot apply
    ///     per-row exchange-rate conversion (same simplification as the dashboard sales trend).
    /// Each CREATE OR ALTER runs as its own migration command, so it is the first statement
    /// in its batch and needs no EXEC(N'...') wrapper.
    /// </summary>
    public partial class AddReportProcsBatch1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
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

    SELECT
        p.PeriodStart,
        COUNT(*)                                            AS OrderCount,
        SUM(so.SubTotal)                                    AS GrossAmount,
        SUM(so.DiscountAmount)                              AS DiscountAmount,
        SUM(so.TaxAmount)                                   AS TaxAmount,
        SUM(so.TotalAmount)                                 AS NetAmount,
        SUM(so.TotalAmount + so.TaxAmount)                  AS GrandTotal,
        CAST(SUM(so.TotalAmount) / COUNT(*) AS decimal(18,2)) AS AverageOrderValue
    FROM dbo.SalesOrders so
    CROSS APPLY (SELECT CASE @GroupBy
                     WHEN 'month' THEN DATEFROMPARTS(YEAR(so.SODate), MONTH(so.SODate), 1)
                     WHEN 'week'  THEN CAST(DATEADD(week, DATEDIFF(week, 0, so.SODate), 0) AS date)
                     ELSE CAST(so.SODate AS date)
                 END AS PeriodStart) p
    WHERE so.Isdeleted = 0
      AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
      AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
      AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
      AND (@Channel IS NULL OR so.Channel = @Channel)
    GROUP BY p.PeriodStart
    ORDER BY p.PeriodStart;
END
");

            migrationBuilder.Sql(@"
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

    -- Line revenue = Quantity * UnitPrice - Quantity * Discount (Discount is per-unit flat),
    -- matching the dashboard top-products computation.
    WITH agg AS (
        SELECT
            p.Id            AS PartId,
            p.PartNumber,
            p.Name          AS PartName,
            p.SKU           AS Sku,
            c.Name          AS CategoryName,
            b.Name          AS BrandName,
            SUM(l.Quantity)                 AS QuantitySold,
            SUM(l.Quantity * l.UnitPrice)   AS GrossRevenue,
            SUM(l.Quantity * l.Discount)    AS DiscountAmount,
            SUM(l.Quantity * l.UnitPrice) - SUM(l.Quantity * l.Discount) AS NetRevenue
        FROM dbo.SalesOrders so
        JOIN dbo.SalesOrderLine l ON l.SalesOrderId = so.Id AND l.Isdeleted = 0
        JOIN dbo.Parts p          ON p.Id = l.PartId        AND p.Isdeleted = 0
        LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId   AND c.Isdeleted = 0
        LEFT JOIN dbo.Brands b     ON b.Id = p.BrandId      AND b.Isdeleted = 0
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
          AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
          AND (@BrandId IS NULL OR p.BrandId = @BrandId)
          AND (@Search IS NULL
               OR p.Name LIKE N'%' + @Search + N'%'
               OR p.PartNumber LIKE N'%' + @Search + N'%'
               OR p.SKU LIKE N'%' + @Search + N'%')
        GROUP BY p.Id, p.PartNumber, p.Name, p.SKU, c.Name, b.Name
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY NetRevenue DESC, PartName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_StockSummary
    @WarehouseId      uniqueidentifier = NULL,
    @CategoryId       uniqueidentifier = NULL,
    @BrandId          uniqueidentifier = NULL,
    @Search           nvarchar(100) = NULL,
    @IncludeZeroStock bit = 0,
    @PageNumber       int = 1,
    @PageSize         int = 50
AS
BEGIN
    SET NOCOUNT ON;

    -- One row per StockLevel (part/variant/warehouse); valuation from AVAILABLE lots only.
    SELECT
        p.Id   AS PartId,
        p.PartNumber,
        p.Name AS PartName,
        p.SKU  AS Sku,
        v.Name AS VariantName,
        c.Name AS CategoryName,
        w.Name AS WarehouseName,
        sl.QuantityOnHand,
        sl.QuantityReserved,
        sl.QuantityDamaged,
        sl.QuantityOnHand - sl.QuantityReserved AS QuantityAvailable,
        CASE WHEN ISNULL(lv.Qty, 0) = 0 THEN NULL
             ELSE CAST(lv.Value / lv.Qty AS decimal(18,2)) END AS AverageCost,
        ISNULL(lv.Value, 0) AS StockValue
    INTO #rows
    FROM dbo.StockLevels sl
    JOIN dbo.Parts p       ON p.Id = sl.PartId      AND p.Isdeleted = 0
    JOIN dbo.Warehouses w  ON w.Id = sl.WarehouseId AND w.Isdeleted = 0
    LEFT JOIN dbo.ProductVariants v ON v.Id = sl.VariantId AND v.Isdeleted = 0
    LEFT JOIN dbo.Categories c      ON c.Id = p.CategoryId AND c.Isdeleted = 0
    OUTER APPLY (
        SELECT SUM(lot.QuantityAvailable * lot.CostPrice) AS Value,
               SUM(lot.QuantityAvailable)                 AS Qty
        FROM dbo.StockLots lot
        WHERE lot.Isdeleted = 0
          AND lot.Status = 'AVAILABLE'
          AND lot.QuantityAvailable > 0
          AND lot.PartId = sl.PartId
          AND lot.WarehouseId = sl.WarehouseId
          AND ((sl.VariantId IS NULL AND lot.VariantId IS NULL) OR lot.VariantId = sl.VariantId)
    ) lv
    WHERE sl.Isdeleted = 0
      AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
      AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
      AND (@BrandId IS NULL OR p.BrandId = @BrandId)
      AND (@IncludeZeroStock = 1 OR sl.QuantityOnHand <> 0)
      AND (@Search IS NULL
           OR p.Name LIKE N'%' + @Search + N'%'
           OR p.PartNumber LIKE N'%' + @Search + N'%'
           OR p.SKU LIKE N'%' + @Search + N'%');

    -- Result set 1: the requested page.
    SELECT *
    FROM #rows
    ORDER BY PartName, VariantName, WarehouseName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    -- Result set 2: grand totals over the whole filtered set.
    SELECT
        COUNT(*)                        AS [RowCount],
        COUNT(DISTINCT PartId)          AS DistinctPartCount,
        ISNULL(SUM(QuantityOnHand), 0)  AS TotalQuantityOnHand,
        ISNULL(SUM(StockValue), 0)      AS TotalStockValue
    FROM #rows;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesSummary;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByProduct;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_StockSummary;");
        }
    }
}
