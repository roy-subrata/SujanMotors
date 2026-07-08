using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Reports module, batch 2: remaining Sales + Inventory report procedures.
    /// Same shared rules as batch 1 (see AddReportProcsBatch1) — Isdeleted = 0, sales status
    /// exclusions, AVAILABLE-lot valuation, base-currency amounts. Additional rule specific to
    /// this batch: CustomerPayment.PaymentType is stored as an int (REGULAR=0, ADVANCE=1) —
    /// verified against CustomerPaymentConfiguration — so usp_Report_PaymentCollections compares
    /// cp.PaymentType = 1 rather than a string literal.
    /// </summary>
    public partial class AddReportProcsBatch2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesByCategory
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    WITH agg AS (
        SELECT
            ISNULL(c.Name, N'Uncategorized') AS CategoryName,
            COUNT(DISTINCT so.Id)            AS OrderCount,
            SUM(l.Quantity)                  AS QuantitySold,
            SUM(l.Quantity * l.UnitPrice) - SUM(l.Quantity * l.Discount) AS NetRevenue
        FROM dbo.SalesOrders so
        JOIN dbo.SalesOrderLine l ON l.SalesOrderId = so.Id AND l.Isdeleted = 0
        JOIN dbo.Parts p          ON p.Id = l.PartId        AND p.Isdeleted = 0
        LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId   AND c.Isdeleted = 0
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
          AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
        GROUP BY ISNULL(c.Name, N'Uncategorized')
    )
    SELECT
        CategoryName, OrderCount, QuantitySold, NetRevenue,
        CASE WHEN SUM(NetRevenue) OVER() = 0 THEN 0
             ELSE CAST(NetRevenue * 100.0 / SUM(NetRevenue) OVER() AS decimal(18,2)) END AS PercentOfTotal
    FROM agg
    ORDER BY NetRevenue DESC;
END
");

            migrationBuilder.Sql(@"
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

    -- Revenue/Paid/Outstanding taken directly from SalesOrder (accrual figures), matching the
    -- dashboard's top-customers computation rather than re-deriving from Invoices/Payments.
    WITH agg AS (
        SELECT
            c.Id                              AS CustomerId,
            c.CustomerCode,
            c.FirstName + N' ' + c.LastName   AS CustomerName,
            c.CustomerType,
            COUNT(*)                          AS OrderCount,
            SUM(so.TotalAmount + so.TaxAmount) AS Revenue,
            SUM(so.PaidAmount)                 AS PaidAmount,
            SUM(so.TotalAmount + so.TaxAmount - so.PaidAmount) AS Outstanding,
            MAX(so.SODate)                     AS LastPurchaseDate
        FROM dbo.SalesOrders so
        JOIN dbo.Customers c ON c.Id = so.CustomerId AND c.Isdeleted = 0
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
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

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesBySalesperson
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- Order-level aggregation kept separate from line-level quantity to avoid double-counting
    -- Revenue/OrderCount when a sales order has multiple lines.
    WITH orders AS (
        SELECT so.Id, so.TechnicianId, so.TotalAmount
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
        ISNULL(t.Name, N'Unassigned')            AS TechnicianName,
        COUNT(*)                                 AS OrderCount,
        ISNULL(SUM(qty.Quantity), 0)             AS QuantitySold,
        SUM(orders.TotalAmount)                  AS Revenue,
        CAST(SUM(orders.TotalAmount) / COUNT(*) AS decimal(18,2)) AS AverageOrderValue
    FROM orders
    LEFT JOIN dbo.Technicians t ON t.Id = orders.TechnicianId AND t.Isdeleted = 0
    LEFT JOIN qty ON qty.SalesOrderId = orders.Id
    GROUP BY ISNULL(t.Name, N'Unassigned')
    ORDER BY Revenue DESC;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SalesReturns
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- SalesReturn carries no Currency column; use the originating sales order's currency.
    WITH agg AS (
        SELECT
            sr.ReturnDate, sr.ReturnNumber, so.SONumber, so.CustomerName,
            sr.Status, sr.RefundType, sr.RefundAmount, so.Currency, sr.Reason
        FROM dbo.SalesReturns sr
        JOIN dbo.SalesOrders so ON so.Id = sr.SalesOrderId AND so.Isdeleted = 0
        WHERE sr.Isdeleted = 0
          AND sr.ReturnDate >= @FromDt AND sr.ReturnDate < @ToExclusive
          AND (@WarehouseId IS NULL OR sr.WarehouseId = @WarehouseId)
          AND (@Search IS NULL
               OR sr.ReturnNumber LIKE N'%' + @Search + N'%'
               OR so.SONumber LIKE N'%' + @Search + N'%'
               OR so.CustomerName LIKE N'%' + @Search + N'%')
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY ReturnDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_PaymentCollections
    @FromDate      date,
    @ToDate        date,
    @GroupBy       varchar(10) = 'day',   -- day | method
    @PaymentMethod nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- Cash-basis: only COMPLETED payments, excluding advance re-applications
    -- (PaymentType = 1 is ADVANCE — stored as int, see CustomerPaymentConfiguration).
    SELECT
        CASE WHEN @GroupBy = 'method' THEN cp.PaymentMethod
             ELSE CONVERT(varchar(10), cp.PaymentDate, 120) END AS GroupKey,
        COUNT(*)       AS PaymentCount,
        SUM(cp.Amount) AS TotalAmount
    FROM dbo.CustomerPayments cp
    WHERE cp.Isdeleted = 0
      AND cp.PaymentDate >= @FromDt AND cp.PaymentDate < @ToExclusive
      AND cp.Status = 'COMPLETED'
      AND (cp.PaymentType = 1 OR cp.SourceAdvancePaymentId IS NULL)
      AND (@PaymentMethod IS NULL OR cp.PaymentMethod = @PaymentMethod)
    GROUP BY CASE WHEN @GroupBy = 'method' THEN cp.PaymentMethod
                  ELSE CONVERT(varchar(10), cp.PaymentDate, 120) END
    ORDER BY GroupKey;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_ProfitByProduct
    @FromDate    date,
    @ToDate      date,
    @WarehouseId uniqueidentifier = NULL,
    @CategoryId  uniqueidentifier = NULL,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    WITH rev AS (
        SELECT
            p.Id AS PartId, p.PartNumber, p.Name AS PartName,
            SUM(l.Quantity) AS QuantitySold,
            SUM(l.Quantity * l.UnitPrice) - SUM(l.Quantity * l.Discount) AS NetRevenue
        FROM dbo.SalesOrders so
        JOIN dbo.SalesOrderLine l ON l.SalesOrderId = so.Id AND l.Isdeleted = 0
        JOIN dbo.Parts p          ON p.Id = l.PartId        AND p.Isdeleted = 0
        WHERE so.Isdeleted = 0
          AND so.SODate >= @FromDt AND so.SODate < @ToExclusive
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
          AND (@WarehouseId IS NULL OR so.WarehouseId = @WarehouseId)
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
          AND (@Search IS NULL
               OR p.Name LIKE N'%' + @Search + N'%'
               OR p.PartNumber LIKE N'%' + @Search + N'%'
               OR p.SKU LIKE N'%' + @Search + N'%')
        GROUP BY p.Id, p.PartNumber, p.Name
    ),
    -- COGS from stock lot movements, actual cost at movement time (already base currency).
    cogs AS (
        SELECT
            sl.PartId,
            SUM(CASE WHEN slm.MovementType = 'SALE'   THEN slm.QuantityInBaseUnit * slm.CostAtMovementInBaseUnit
                     WHEN slm.MovementType = 'RETURN' THEN -slm.QuantityInBaseUnit * slm.CostAtMovementInBaseUnit
                     ELSE 0 END) AS Cogs
        FROM dbo.StockLotMovements slm
        JOIN dbo.StockLots sl ON sl.Id = slm.StockLotId
        WHERE slm.Isdeleted = 0
          AND slm.MovementDate >= @FromDt AND slm.MovementDate < @ToExclusive
          AND slm.MovementType IN ('SALE', 'RETURN')
        GROUP BY sl.PartId
    )
    SELECT
        rev.PartId, rev.PartNumber, rev.PartName, rev.QuantitySold, rev.NetRevenue,
        ISNULL(cogs.Cogs, 0) AS Cogs,
        rev.NetRevenue - ISNULL(cogs.Cogs, 0) AS GrossProfit,
        CASE WHEN rev.NetRevenue = 0 THEN NULL
             ELSE CAST((rev.NetRevenue - ISNULL(cogs.Cogs, 0)) * 100.0 / rev.NetRevenue AS decimal(18,2)) END AS MarginPercent,
        COUNT(*) OVER() AS TotalCount
    FROM rev
    LEFT JOIN cogs ON cogs.PartId = rev.PartId
    ORDER BY GrossProfit DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_LowStock
    @WarehouseId uniqueidentifier = NULL,
    @CategoryId  uniqueidentifier = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    -- Same rule as the dashboard: MinimumStock = 0 means 'no threshold set', skip those parts.
    WITH agg AS (
        SELECT
            p.Id AS PartId, p.PartNumber, p.Name AS PartName, p.SKU,
            v.Name AS VariantName, c.Name AS CategoryName, w.Name AS WarehouseName,
            sl.QuantityOnHand, p.MinimumStock, sl.ReorderLevel, sl.ReorderQuantity,
            p.MinimumStock - sl.QuantityOnHand AS Shortfall
        FROM dbo.StockLevels sl
        JOIN dbo.Parts p      ON p.Id = sl.PartId      AND p.Isdeleted = 0
        JOIN dbo.Warehouses w ON w.Id = sl.WarehouseId AND w.Isdeleted = 0
        LEFT JOIN dbo.ProductVariants v ON v.Id = sl.VariantId AND v.Isdeleted = 0
        LEFT JOIN dbo.Categories c      ON c.Id = p.CategoryId AND c.Isdeleted = 0
        WHERE sl.Isdeleted = 0
          AND p.MinimumStock > 0
          AND sl.QuantityOnHand <= p.MinimumStock
          AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY Shortfall DESC, PartName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_StockMovements
    @FromDate     date,
    @ToDate       date,
    @WarehouseId  uniqueidentifier = NULL,
    @PartId       uniqueidentifier = NULL,
    @MovementType nvarchar(20) = NULL,
    @PageNumber   int = 1,
    @PageSize     int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    WITH agg AS (
        SELECT
            sm.MovementDate, p.PartNumber, p.Name AS PartName, v.Name AS VariantName,
            w.Name AS WarehouseName, sm.MovementType, sm.Quantity, sm.Reason, sm.ReferenceNumber
        FROM dbo.StockMovements sm
        JOIN dbo.StockLevels sl ON sl.Id = sm.StockLevelId AND sl.Isdeleted = 0
        JOIN dbo.Parts p        ON p.Id = sl.PartId        AND p.Isdeleted = 0
        JOIN dbo.Warehouses w   ON w.Id = sl.WarehouseId   AND w.Isdeleted = 0
        LEFT JOIN dbo.ProductVariants v ON v.Id = sl.VariantId AND v.Isdeleted = 0
        WHERE sm.Isdeleted = 0
          AND sm.MovementDate >= @FromDt AND sm.MovementDate < @ToExclusive
          AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
          AND (@PartId IS NULL OR sl.PartId = @PartId)
          AND (@MovementType IS NULL OR sm.MovementType = @MovementType)
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY MovementDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_ExpiringLots
    @DaysAhead      int = 90,
    @WarehouseId    uniqueidentifier = NULL,
    @IncludeExpired bit = 0,
    @PageNumber     int = 1,
    @PageSize       int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today   date = CAST(GETUTCDATE() AS date);
    DECLARE @Horizon date = DATEADD(day, @DaysAhead, @Today);

    WITH agg AS (
        SELECT
            lot.LotNumber, p.PartNumber, p.Name AS PartName, w.Name AS WarehouseName,
            s.Name AS SupplierName, lot.ReceivingDate, lot.ExpiryDate,
            DATEDIFF(day, @Today, lot.ExpiryDate) AS DaysToExpiry,
            lot.QuantityAvailable, lot.QuantityAvailable * lot.CostPrice AS StockValue
        FROM dbo.StockLots lot
        JOIN dbo.Parts p      ON p.Id = lot.PartId      AND p.Isdeleted = 0
        JOIN dbo.Warehouses w ON w.Id = lot.WarehouseId AND w.Isdeleted = 0
        LEFT JOIN dbo.Suppliers s ON s.Id = lot.SupplierId AND s.Isdeleted = 0
        WHERE lot.Isdeleted = 0
          AND lot.Status = 'AVAILABLE'
          AND lot.QuantityAvailable > 0
          AND lot.ExpiryDate IS NOT NULL
          AND lot.ExpiryDate <= @Horizon
          AND (@IncludeExpired = 1 OR lot.ExpiryDate >= @Today)
          AND (@WarehouseId IS NULL OR lot.WarehouseId = @WarehouseId)
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY ExpiryDate
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_SlowMovingStock
    @NoSaleDays  int = 90,
    @WarehouseId uniqueidentifier = NULL,
    @CategoryId  uniqueidentifier = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Today  date = CAST(GETUTCDATE() AS date);
    DECLARE @Cutoff date = DATEADD(day, -@NoSaleDays, @Today);

    WITH lastSale AS (
        SELECT lot.PartId, lot.WarehouseId, MAX(slm.MovementDate) AS LastSaleDate
        FROM dbo.StockLotMovements slm
        JOIN dbo.StockLots lot ON lot.Id = slm.StockLotId
        WHERE slm.Isdeleted = 0 AND slm.MovementType = 'SALE'
        GROUP BY lot.PartId, lot.WarehouseId
    ),
    valuation AS (
        SELECT lot.PartId, lot.WarehouseId, SUM(lot.QuantityAvailable * lot.CostPrice) AS StockValue
        FROM dbo.StockLots lot
        WHERE lot.Isdeleted = 0 AND lot.Status = 'AVAILABLE' AND lot.QuantityAvailable > 0
        GROUP BY lot.PartId, lot.WarehouseId
    ),
    agg AS (
        SELECT
            p.Id AS PartId, p.PartNumber, p.Name AS PartName, c.Name AS CategoryName,
            w.Name AS WarehouseName, sl.QuantityOnHand,
            ISNULL(val.StockValue, 0) AS StockValue,
            ls.LastSaleDate,
            CASE WHEN ls.LastSaleDate IS NULL THEN NULL ELSE DATEDIFF(day, ls.LastSaleDate, @Today) END AS DaysSinceLastSale
        FROM dbo.StockLevels sl
        JOIN dbo.Parts p      ON p.Id = sl.PartId      AND p.Isdeleted = 0
        JOIN dbo.Warehouses w ON w.Id = sl.WarehouseId AND w.Isdeleted = 0
        LEFT JOIN dbo.Categories c ON c.Id = p.CategoryId AND c.Isdeleted = 0
        LEFT JOIN lastSale ls ON ls.PartId = sl.PartId AND ls.WarehouseId = sl.WarehouseId
        LEFT JOIN valuation val ON val.PartId = sl.PartId AND val.WarehouseId = sl.WarehouseId
        WHERE sl.Isdeleted = 0
          AND sl.QuantityOnHand > 0
          AND (ls.LastSaleDate IS NULL OR ls.LastSaleDate < @Cutoff)
          AND (@WarehouseId IS NULL OR sl.WarehouseId = @WarehouseId)
          AND (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY DaysSinceLastSale DESC, PartName
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByCategory;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesByCustomer;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesBySalesperson;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SalesReturns;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_PaymentCollections;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_ProfitByProduct;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_LowStock;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_StockMovements;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_ExpiringLots;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_SlowMovingStock;");
        }
    }
}
