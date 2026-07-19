using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoPartShop.Infrastructure.Migrations
{
    /// <summary>
    /// VAT reconciliation report for a period: output VAT on sales, less output VAT reversed by
    /// customer credit notes, less input VAT on purchases.
    ///
    /// Sales and Purchases use the actual stored Invoice.TaxAmount / PurchaseOrder.TaxAmount —
    /// these are real figures, not derived. CustomerCreditNotes has no per-transaction tax
    /// breakdown (TotalAmount mirrors the tax-exclusive SalesReturn.RefundAmount — see
    /// CustomerCreditNote.Create callers), so its VAT is derived as TotalAmount × @VatRatePercent.
    /// The caller passes the shop's configured rate (ReportQuery.VatRatePercent, default 15); this
    /// proc does not read settings.
    ///
    /// Status filters mirror the existing reports module: Invoice excludes CANCELLED only (matches
    /// the AddReportProcsBatch4 comment on FinancialSummaryService's own convention);
    /// PurchaseOrder excludes DRAFT/SUBMITTED/CANCELLED (matches usp_Report_PurchaseSummary,
    /// AddReportProcsBatch3); CustomerCreditNotes excludes CANCELLED only.
    /// </summary>
    public partial class AddVatReportProc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
CREATE OR ALTER PROCEDURE dbo.usp_Report_Vat
    @FromDate       date,
    @ToDate         date,
    @VatRatePercent decimal(5,2) = 15.00
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SalesTaxableValue    decimal(18,2), @SalesVatAmount    decimal(18,2), @SalesInvoiceCount int;
    DECLARE @CreditTaxableValue   decimal(18,2), @CreditVatAmount   decimal(18,2);
    DECLARE @PurchaseTaxableValue decimal(18,2), @PurchaseVatAmount decimal(18,2), @PurchaseOrderCount int;

    SELECT
        @SalesTaxableValue = ISNULL(SUM(i.SubTotal - i.DiscountAmount), 0),
        @SalesVatAmount    = ISNULL(SUM(i.TaxAmount), 0),
        @SalesInvoiceCount = COUNT(*)
    FROM dbo.Invoices i
    WHERE i.Isdeleted = 0
      AND i.Status <> 'CANCELLED'
      AND i.InvoiceDate >= @FromDate
      AND i.InvoiceDate < DATEADD(day, 1, @ToDate);

    SELECT
        @CreditTaxableValue = ISNULL(SUM(cn.TotalAmount), 0)
    FROM dbo.CustomerCreditNotes cn
    WHERE cn.Isdeleted = 0
      AND cn.Status <> 'CANCELLED'
      AND cn.IssueDate >= @FromDate
      AND cn.IssueDate < DATEADD(day, 1, @ToDate);

    SET @CreditVatAmount = ROUND(ISNULL(@CreditTaxableValue, 0) * @VatRatePercent / 100.0, 2);

    SELECT
        @PurchaseTaxableValue = ISNULL(SUM(po.SubTotal - po.DiscountAmount), 0),
        @PurchaseVatAmount    = ISNULL(SUM(po.TaxAmount), 0),
        @PurchaseOrderCount   = COUNT(*)
    FROM dbo.PurchaseOrders po
    WHERE po.Isdeleted = 0
      AND po.Status NOT IN ('DRAFT', 'SUBMITTED', 'CANCELLED')
      AND po.PODate >= @FromDate
      AND po.PODate < DATEADD(day, 1, @ToDate);

    SELECT
        @SalesTaxableValue                                             AS SalesTaxableValue,
        @SalesVatAmount                                                AS SalesVatAmount,
        @SalesInvoiceCount                                             AS SalesInvoiceCount,
        @CreditTaxableValue                                            AS CreditTaxableValue,
        @CreditVatAmount                                               AS CreditVatAmount,
        @PurchaseTaxableValue                                          AS PurchaseTaxableValue,
        @PurchaseVatAmount                                             AS PurchaseVatAmount,
        @PurchaseOrderCount                                            AS PurchaseOrderCount,
        (@SalesVatAmount - @CreditVatAmount - @PurchaseVatAmount)      AS NetVatPayable;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.usp_Report_Vat;");
        }
    }
}
