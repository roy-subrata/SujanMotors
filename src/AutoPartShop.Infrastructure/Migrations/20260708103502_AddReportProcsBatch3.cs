using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// Reports module, batch 3: Purchase + Financial report procedures. Same shared rules as
    /// batch 1/2 (Isdeleted = 0, status exclusions, base-currency amounts). Additional rules:
    ///   - PurchaseOrders exclude Status IN ('DRAFT','SUBMITTED','CANCELLED') (mirrors FinancialSummaryService).
    ///   - SupplierPayment.PaymentType is stored as a STRING ('REGULAR'/'ADVANCE') — unlike
    ///     CustomerPayment.PaymentType (int) — verified against SupplierPaymentConfiguration.
    ///   - PurchaseReturn carries no Currency column; use the originating PurchaseOrder's currency.
    ///   - GoodsReceiptLine has no AcceptedQuantity column; computed as
    ///     ReceivedQuantity - DamagedQuantity - WrongQuantity.
    /// No SP for Profit &amp; Loss — that report reuses IFinancialSummaryService directly so its
    /// figures never drift from the dashboard (currency conversion, advance-payment de-dup, etc.
    /// can't be replicated in SQL without duplicating that logic).
    /// </summary>
    public partial class AddReportProcsBatch3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_PurchaseSummary
    @FromDate    date,
    @ToDate      date,
    @GroupBy     varchar(10) = 'day',   -- day | week | month
    @SupplierId  uniqueidentifier = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    SELECT
        p.PeriodStart,
        COUNT(*)                              AS PoCount,
        SUM(po.TotalAmount)                   AS TotalAmount,
        SUM(po.PaidAmount)                    AS PaidAmount,
        SUM(po.TotalAmount - po.PaidAmount)   AS Outstanding
    FROM dbo.PurchaseOrders po
    CROSS APPLY (SELECT CASE @GroupBy
                     WHEN 'month' THEN DATEFROMPARTS(YEAR(po.PODate), MONTH(po.PODate), 1)
                     WHEN 'week'  THEN CAST(DATEADD(week, DATEDIFF(week, 0, po.PODate), 0) AS date)
                     ELSE CAST(po.PODate AS date)
                 END AS PeriodStart) p
    WHERE po.Isdeleted = 0
      AND po.PODate >= @FromDt AND po.PODate < @ToExclusive
      AND po.Status NOT IN ('DRAFT', 'SUBMITTED', 'CANCELLED')
      AND (@SupplierId IS NULL OR po.SupplierId = @SupplierId)
    GROUP BY p.PeriodStart
    ORDER BY p.PeriodStart;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_PurchasesBySupplier
    @FromDate    date,
    @ToDate      date,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- Period-scoped supplier summary (PO date within range) — not the all-time balance the
    -- dashboard/supplier-ledger show; this report answers 'what did we buy from whom this period'.
    WITH po AS (
        SELECT s.Id AS SupplierId, s.Code, s.Name,
               COUNT(*) AS PoCount,
               SUM(po.TotalAmount) AS TotalAmount
        FROM dbo.PurchaseOrders po
        JOIN dbo.Suppliers s ON s.Id = po.SupplierId AND s.Isdeleted = 0
        WHERE po.Isdeleted = 0
          AND po.PODate >= @FromDt AND po.PODate < @ToExclusive
          AND po.Status NOT IN ('DRAFT', 'SUBMITTED', 'CANCELLED')
          AND (@Search IS NULL OR s.Name LIKE N'%' + @Search + N'%' OR s.Code LIKE N'%' + @Search + N'%')
        GROUP BY s.Id, s.Code, s.Name
    ),
    received AS (
        SELECT po2.SupplierId,
               SUM((grl.ReceivedQuantity - grl.DamagedQuantity - grl.WrongQuantity) * grl.UnitCost) AS ReceivedValue
        FROM dbo.GoodsReceiptLines grl
        JOIN dbo.GoodsReceipts gr  ON gr.Id = grl.GoodsReceiptId AND gr.Isdeleted = 0
        JOIN dbo.PurchaseOrders po2 ON po2.Id = gr.PurchaseOrderId
        WHERE grl.Isdeleted = 0
          AND gr.Status IN ('ACCEPTED', 'VERIFIED')
          AND gr.ReceiptDate >= @FromDt AND gr.ReceiptDate < @ToExclusive
        GROUP BY po2.SupplierId
    ),
    paid AS (
        SELECT sp.SupplierId, SUM(sp.Amount) AS PaidAmount
        FROM dbo.SupplierPayments sp
        WHERE sp.Isdeleted = 0
          AND sp.Status = 'COMPLETED'
          AND sp.PaymentMethod NOT IN ('REFUND', 'CREDIT_NOTE')
          AND (sp.PaymentType = 'ADVANCE' OR sp.SourceAdvancePaymentId IS NULL)
          AND sp.PaymentDate >= @FromDt AND sp.PaymentDate < @ToExclusive
        GROUP BY sp.SupplierId
    ),
    returned AS (
        SELECT pr.SupplierId, SUM(pr.SettledAmount) AS ReturnedValue
        FROM dbo.PurchaseReturns pr
        WHERE pr.Isdeleted = 0
          AND pr.SettlementStatus = 'SETTLED'
          AND pr.ReturnDate >= @FromDt AND pr.ReturnDate < @ToExclusive
        GROUP BY pr.SupplierId
    ),
    agg AS (
        SELECT
            po.SupplierId, po.Code AS SupplierCode, po.Name AS SupplierName, po.PoCount, po.TotalAmount,
            ISNULL(received.ReceivedValue, 0) AS ReceivedValue,
            ISNULL(paid.PaidAmount, 0)        AS PaidAmount,
            ISNULL(returned.ReturnedValue, 0) AS ReturnedValue,
            po.TotalAmount - ISNULL(paid.PaidAmount, 0) - ISNULL(returned.ReturnedValue, 0) AS Balance
        FROM po
        LEFT JOIN received ON received.SupplierId = po.SupplierId
        LEFT JOIN paid     ON paid.SupplierId = po.SupplierId
        LEFT JOIN returned ON returned.SupplierId = po.SupplierId
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY TotalAmount DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_PurchaseReturns
    @FromDate    date,
    @ToDate      date,
    @SupplierId  uniqueidentifier = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    -- PurchaseReturn carries no Currency column; use the originating PO's currency.
    WITH agg AS (
        SELECT
            pr.ReturnDate, pr.ReturnNumber, po.PONumber, s.Name AS SupplierName,
            pr.Status, pr.SettlementStatus, pr.RefundAmount, po.Currency
        FROM dbo.PurchaseReturns pr
        JOIN dbo.PurchaseOrders po ON po.Id = pr.PurchaseOrderId
        JOIN dbo.Suppliers s       ON s.Id = pr.SupplierId AND s.Isdeleted = 0
        WHERE pr.Isdeleted = 0
          AND pr.ReturnDate >= @FromDt AND pr.ReturnDate < @ToExclusive
          AND (@SupplierId IS NULL OR pr.SupplierId = @SupplierId)
    )
    SELECT agg.*, COUNT(*) OVER() AS TotalCount
    FROM agg
    ORDER BY ReturnDate DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_ReceivablesAging
    @AsOfDate    date = NULL,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfDt date = ISNULL(@AsOfDate, CAST(GETUTCDATE() AS date));

    -- Outstanding per invoice = (SubTotal+TaxAmount-DiscountAmount) - Σ COMPLETED payments,
    -- positive only. Bucketed by DueDate age vs @AsOfDate. Mirrors FinancialSummaryService's
    -- customer-due computation (Invoice.Status<>'CANCELLED', SO not excluded-status).
    WITH inv AS (
        SELECT
            i.Id, i.DueDate, so.CustomerId,
            (i.SubTotal + i.TaxAmount - i.DiscountAmount) - ISNULL(pay.Paid, 0) AS Outstanding
        FROM dbo.Invoices i
        JOIN dbo.SalesOrders so ON so.Id = i.SalesOrderId AND so.Isdeleted = 0
        OUTER APPLY (
            SELECT SUM(cp.Amount) AS Paid
            FROM dbo.CustomerPayments cp
            WHERE cp.InvoiceId = i.Id AND cp.Isdeleted = 0 AND cp.Status = 'COMPLETED'
        ) pay
        WHERE i.Isdeleted = 0
          AND i.Status <> 'CANCELLED'
          AND so.Status NOT IN ('DRAFT', 'CANCELLED', 'RETURNED')
    ),
    positiveInv AS (
        SELECT *, DATEDIFF(day, DueDate, @AsOfDt) AS DaysPastDue
        FROM inv
        WHERE Outstanding > 0
    ),
    perCustomer AS (
        SELECT
            CustomerId,
            SUM(CASE WHEN DaysPastDue <= 0 THEN Outstanding ELSE 0 END)               AS CurrentAmount,
            SUM(CASE WHEN DaysPastDue BETWEEN 1 AND 30 THEN Outstanding ELSE 0 END)    AS Days1To30,
            SUM(CASE WHEN DaysPastDue BETWEEN 31 AND 60 THEN Outstanding ELSE 0 END)   AS Days31To60,
            SUM(CASE WHEN DaysPastDue BETWEEN 61 AND 90 THEN Outstanding ELSE 0 END)   AS Days61To90,
            SUM(CASE WHEN DaysPastDue > 90 THEN Outstanding ELSE 0 END)                AS Days90Plus,
            SUM(Outstanding)                                                           AS Total
        FROM positiveInv
        GROUP BY CustomerId
    ),
    agg AS (
        SELECT pc.CustomerId, c.CustomerCode, c.FirstName + N' ' + c.LastName AS CustomerName,
               pc.CurrentAmount, pc.Days1To30, pc.Days31To60, pc.Days61To90, pc.Days90Plus, pc.Total
        FROM perCustomer pc
        JOIN dbo.Customers c ON c.Id = pc.CustomerId AND c.Isdeleted = 0
        WHERE @Search IS NULL
           OR c.CustomerCode LIKE N'%' + @Search + N'%'
           OR c.FirstName LIKE N'%' + @Search + N'%'
           OR c.LastName LIKE N'%' + @Search + N'%'
    )
    SELECT * INTO #rows FROM agg;

    SELECT *
    FROM #rows
    ORDER BY Total DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        COUNT(*)                      AS [RowCount],
        ISNULL(SUM(CurrentAmount), 0) AS CurrentAmount,
        ISNULL(SUM(Days1To30), 0)     AS Days1To30,
        ISNULL(SUM(Days31To60), 0)    AS Days31To60,
        ISNULL(SUM(Days61To90), 0)    AS Days61To90,
        ISNULL(SUM(Days90Plus), 0)    AS Days90Plus,
        ISNULL(SUM(Total), 0)         AS Total
    FROM #rows;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_PayablesAging
    @AsOfDate    date = NULL,
    @Search      nvarchar(100) = NULL,
    @PageNumber  int = 1,
    @PageSize    int = 50
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @AsOfDt date = ISNULL(@AsOfDate, CAST(GETUTCDATE() AS date));

    -- Balance per PO = TotalAmount - Σ COMPLETED payments applied to that PO - Σ SETTLED
    -- returns for that PO; bucketed by PO-date age vs @AsOfDate (payables have no due-date
    -- column, unlike receivables). Mirrors FinancialSummaryService's supplier-balance rules.
    WITH pos AS (
        SELECT po.Id, po.SupplierId, po.PODate, po.TotalAmount
        FROM dbo.PurchaseOrders po
        WHERE po.Isdeleted = 0
          AND po.Status NOT IN ('DRAFT', 'SUBMITTED', 'CANCELLED')
    ),
    paidByPO AS (
        SELECT sp.PurchaseOrderId, SUM(sp.Amount) AS Paid
        FROM dbo.SupplierPayments sp
        WHERE sp.Isdeleted = 0
          AND sp.Status = 'COMPLETED'
          AND sp.PaymentMethod NOT IN ('REFUND', 'CREDIT_NOTE')
          AND (sp.PaymentType = 'ADVANCE' OR sp.SourceAdvancePaymentId IS NULL)
          AND sp.PurchaseOrderId IS NOT NULL
        GROUP BY sp.PurchaseOrderId
    ),
    returnedByPO AS (
        SELECT pr.PurchaseOrderId, SUM(pr.SettledAmount) AS Returned
        FROM dbo.PurchaseReturns pr
        WHERE pr.Isdeleted = 0 AND pr.SettlementStatus = 'SETTLED'
        GROUP BY pr.PurchaseOrderId
    ),
    balances AS (
        SELECT
            pos.SupplierId,
            pos.TotalAmount - ISNULL(paidByPO.Paid, 0) - ISNULL(returnedByPO.Returned, 0) AS Balance,
            DATEDIFF(day, pos.PODate, @AsOfDt) AS AgeDays
        FROM pos
        LEFT JOIN paidByPO    ON paidByPO.PurchaseOrderId = pos.Id
        LEFT JOIN returnedByPO ON returnedByPO.PurchaseOrderId = pos.Id
    ),
    positive AS (
        SELECT * FROM balances WHERE Balance > 0
    ),
    perSupplier AS (
        SELECT
            SupplierId,
            SUM(CASE WHEN AgeDays <= 0 THEN Balance ELSE 0 END)             AS CurrentAmount,
            SUM(CASE WHEN AgeDays BETWEEN 1 AND 30 THEN Balance ELSE 0 END)  AS Days1To30,
            SUM(CASE WHEN AgeDays BETWEEN 31 AND 60 THEN Balance ELSE 0 END) AS Days31To60,
            SUM(CASE WHEN AgeDays BETWEEN 61 AND 90 THEN Balance ELSE 0 END) AS Days61To90,
            SUM(CASE WHEN AgeDays > 90 THEN Balance ELSE 0 END)             AS Days90Plus,
            SUM(Balance)                                                     AS Total
        FROM positive
        GROUP BY SupplierId
    ),
    agg AS (
        SELECT ps.SupplierId, s.Code AS SupplierCode, s.Name AS SupplierName,
               ps.CurrentAmount, ps.Days1To30, ps.Days31To60, ps.Days61To90, ps.Days90Plus, ps.Total
        FROM perSupplier ps
        JOIN dbo.Suppliers s ON s.Id = ps.SupplierId AND s.Isdeleted = 0
        WHERE @Search IS NULL OR s.Name LIKE N'%' + @Search + N'%' OR s.Code LIKE N'%' + @Search + N'%'
    )
    SELECT * INTO #rows FROM agg;

    SELECT *
    FROM #rows
    ORDER BY Total DESC
    OFFSET (@PageNumber - 1) * @PageSize ROWS FETCH NEXT @PageSize ROWS ONLY;

    SELECT
        COUNT(*)                      AS [RowCount],
        ISNULL(SUM(CurrentAmount), 0) AS CurrentAmount,
        ISNULL(SUM(Days1To30), 0)     AS Days1To30,
        ISNULL(SUM(Days31To60), 0)    AS Days31To60,
        ISNULL(SUM(Days61To90), 0)    AS Days61To90,
        ISNULL(SUM(Days90Plus), 0)    AS Days90Plus,
        ISNULL(SUM(Total), 0)         AS Total
    FROM #rows;
END
");

            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_Expenses
    @FromDate      date,
    @ToDate        date,
    @GroupBy       varchar(10) = 'day',   -- day | category
    @Category      nvarchar(100) = NULL,
    @PaymentMethod nvarchar(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @FromDt      datetime2 = CAST(@FromDate AS datetime2);
    DECLARE @ToExclusive datetime2 = DATEADD(day, 1, CAST(@ToDate AS datetime2));

    SELECT
        CASE WHEN @GroupBy = 'category' THEN de.Category
             ELSE CONVERT(varchar(10), de.ExpenseDate, 120) END AS GroupKey,
        COUNT(*)        AS ExpenseCount,
        SUM(de.Amount)  AS TotalAmount
    FROM dbo.DailyExpenses de
    WHERE de.Isdeleted = 0
      AND de.ExpenseDate >= @FromDt AND de.ExpenseDate < @ToExclusive
      AND (@Category IS NULL OR de.Category = @Category)
      AND (@PaymentMethod IS NULL OR de.PaymentMethod = @PaymentMethod)
    GROUP BY CASE WHEN @GroupBy = 'category' THEN de.Category
                  ELSE CONVERT(varchar(10), de.ExpenseDate, 120) END
    ORDER BY GroupKey;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_PurchaseSummary;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_PurchasesBySupplier;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_PurchaseReturns;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_ReceivablesAging;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_PayablesAging;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_Expenses;");
        }
    }
}
