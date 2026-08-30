using AutoPartShop.Api.Tests.Fixtures;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Tests.Reports;

/// <summary>
/// Tests financial report calculations: Receivables Aging, Payables Aging, Expenses, VAT.
/// </summary>
public class FinancialReportCalculationTests : IClassFixture<DatabaseFixture>
{
    private readonly AutoPartDbContext _db;
    private readonly IReportReadRepository _reports;

    public FinancialReportCalculationTests(DatabaseFixture fixture)
    {
        _db = fixture.CreateContext();
        _reports = new ReportReadRepository(_db);
    }

    /// <summary>
    /// Receivables Aging buckets outstanding invoices by days past due.
    /// Invoice1: outstanding = (480+48-0)-432 = 96, dueDate = today+30 → Current bucket (not yet due)
    /// Invoice2: outstanding = (925+92.50-0)-0 = 1017.50, dueDate = today-15 → Days1To30 bucket (15 days overdue)
    /// Invoice3: outstanding = (600+60-0)-0 = 660, dueDate = today+45 → Current bucket (not yet due)
    /// </summary>
    [Fact]
    public async Task ReceivablesAging_BucketsByDaysPastDue()
    {
        var result = await _reports.GetReceivablesAgingAsync(new ReportQuery
        {
            AsOfDate = DateTime.UtcNow.Date,
            PageSize = 100
        }, maxRowsOverride: 100);

        var totals = result.Totals;
        Assert.NotNull(totals);

        // Invoice1: (480+48-0) - 432 = 96, due today+30 → Current
        // Invoice3: (600+60-0) - 0 = 660, due today+45 → Current
        // Invoice2: (925+92.50-0) - 0 = 1017.50, due today-15 → Days1To30
        Assert.Equal(756m, totals.CurrentAmount, 2);  // 96 + 660
        Assert.Equal(737.50m, totals.Days1To30, 2);  // 1017.50 - 280 (CPayment2 COMPLETED)
        Assert.Equal(0m, totals.Days31To60, 2);
        Assert.Equal(0m, totals.Days61To90, 2);
        Assert.Equal(0m, totals.Days90Plus, 2);
        Assert.Equal(1493.50m, totals.Total, 2);  // 756 + 737.50
    }

    /// <summary>
    /// Payables Aging buckets outstanding PO balances by age.
    /// PO1: balance = 550 - 100 (paid) - 100 (returned) = 350, PODate = today → Current
    /// PO2: balance = 396 - 0 - 0 = 396, PODate = today → Current
    /// </summary>
    [Fact]
    public async Task PayablesAging_BucketsByPOAge()
    {
        var result = await _reports.GetPayablesAgingAsync(new ReportQuery
        {
            AsOfDate = DateTime.UtcNow.Date,
            PageSize = 100
        }, maxRowsOverride: 100);

        var totals = result.Totals;
        Assert.NotNull(totals);

        // Both POs have PODate = today, AgeDays = 0 → Current
        Assert.Equal(746m, totals.CurrentAmount, 2);  // 350 + 396
        Assert.Equal(0m, totals.Days1To30, 2);
        Assert.Equal(0m, totals.Days31To60, 2);
        Assert.Equal(0m, totals.Days61To90, 2);
        Assert.Equal(0m, totals.Days90Plus, 2);
        Assert.Equal(746m, totals.Total, 2);
    }

    /// <summary>
    /// Expense Report groups expenses. We have 3 expenses today:
    /// RENT: 200, UTILITIES: 150+100 = 250.
    /// GroupBy=day: 1 row for today, TotalAmount=450
    /// GroupBy=category: 2 rows (RENT=200, UTILITIES=250)
    /// </summary>
    [Fact]
    public async Task Expenses_GroupByDay_SumsDailyTotal()
    {
        var today = DateTime.UtcNow.Date;
        var rows = await _reports.GetExpensesAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today,
            GroupBy = "day"
        });

        Assert.Single(rows);
        Assert.Equal(today.ToString("yyyy-MM-dd"), rows[0].GroupKey);
        Assert.Equal(3, rows[0].ExpenseCount);
        Assert.Equal(450m, rows[0].TotalAmount, 2);
    }

    [Fact]
    public async Task Expenses_GroupByCategory_SplitsCorrectly()
    {
        var today = DateTime.UtcNow.Date;
        var rows = await _reports.GetExpensesAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today,
            GroupBy = "category"
        });

        Assert.Equal(2, rows.Count);

        var rent = rows.Single(r => r.GroupKey == "RENT");
        Assert.Equal(1, rent.ExpenseCount);
        Assert.Equal(200m, rent.TotalAmount, 2);

        var utilities = rows.Single(r => r.GroupKey == "UTILITIES");
        Assert.Equal(2, utilities.ExpenseCount);
        Assert.Equal(250m, utilities.TotalAmount, 2);
    }

    /// <summary>
    /// VAT Report:
    /// Sales: Invoice1 (Sub=480,Tax=48) + Invoice2 (Sub=925,Tax=92.50) + Invoice3 (Sub=600,Tax=60)
    ///   SalesTaxableValue = (480-0)+(925-0)+(600-0) = 2005
    ///   SalesVatAmount = 48+92.50+60 = 200.50
    ///   SalesInvoiceCount = 3
    /// Credit Notes: CN-001 TotalAmount=100, CreditVatAmount = 100 * 15/100 = 15
    /// Purchases: PO1 (Sub=500, Disc=0, Tax=50) + PO2 (Sub=360, Disc=0, Tax=36)
    ///   PurchaseTaxableValue = (500-0)+(360-0) = 860
    ///   PurchaseVatAmount = 50+36 = 86
    ///   PurchaseOrderCount = 2
    /// NetVatPayable = 200.50 - 15 - 86 = 99.50
    /// </summary>
    [Fact]
    public async Task VatReport_CalculatesNetVatPayable()
    {
        var today = DateTime.UtcNow.Date;
        var vat = await _reports.GetVatReportAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today,
            VatRatePercent = 15m
        });

        Assert.Equal(2005m, vat.SalesTaxableValue, 2);
        Assert.Equal(200.50m, vat.SalesVatAmount, 2);
        Assert.Equal(3, vat.SalesInvoiceCount);
        Assert.Equal(100m, vat.CreditTaxableValue, 2);
        Assert.Equal(15m, vat.CreditVatAmount, 2);
        Assert.Equal(860m, vat.PurchaseTaxableValue, 2);
        Assert.Equal(86m, vat.PurchaseVatAmount, 2);
        Assert.Equal(2, vat.PurchaseOrderCount);
        Assert.Equal(99.50m, vat.NetVatPayable, 2);
    }
}
