using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Reports module, batch 4: Sales by Cashier — reports the actual staff user who processed
    /// each sale (SalesOrder.CashierId, added in AddSalesOrderCashier), distinct from the existing
    /// usp_Report_SalesBySalesperson which groups by the optional Technician (mechanic who
    /// recommended the parts). Same shared rules as batch 1-3 (Isdeleted = 0, sales status
    /// exclusions, base-currency amounts). Users (Identity) has no Isdeleted column, so no
    /// soft-delete filter is applied on that join — a deactivated user's historical sales still
    /// attribute to them by name.
    /// </summary>
    public partial class AddReportProcsBatch4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesByCashier
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- Order-level aggregation kept separate from line-level quantity to avoid double-counting
    -- Revenue/OrderCount when a sales order has multiple lines (same pattern as SalesBySalesperson).
    WITH orders AS (
        SELECT so.Id, so.CashierId, so.TotalAmount
        FROM dbo.SalesOrders so
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
          AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
    ),
    qty AS (
        SELECT l.SalesOrderId, SUM(l.Quantity) AS Quantity
        FROM dbo.SalesOrderLine l
        WHERE l.Isdeleted = 0 AND l.SalesOrderId IN (SELECT Id FROM orders)
        GROUP BY l.SalesOrderId
    )
    SELECT
        COALESCE(
            NULLIF(LTRIM(RTRIM(ISNULL(u.FirstName, N'') + N' ' + ISNULL(u.LastName, N''))), N''),
            u.UserName,
            N'Unassigned')                        AS CashierName,
        COUNT(*)                                  AS OrderCount,
        ISNULL(SUM(qty.Quantity), 0)              AS QuantitySold,
        SUM(orders.TotalAmount)                   AS Revenue,
        CAST(SUM(orders.TotalAmount) / COUNT(*) AS decimal(18,2)) AS AverageOrderValue
    FROM orders
    LEFT JOIN dbo.Users u ON u.Id = orders.CashierId
    LEFT JOIN qty ON qty.SalesOrderId = orders.Id
    GROUP BY COALESCE(
        NULLIF(LTRIM(RTRIM(ISNULL(u.FirstName, N'') + N' ' + ISNULL(u.LastName, N''))), N''),
        u.UserName,
        N'Unassigned')
    ORDER BY Revenue DESC;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByCashier;");
        }
    }
}
