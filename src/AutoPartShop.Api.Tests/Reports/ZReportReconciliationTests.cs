using AutoPartShop.Api.Tests.Fixtures;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Tests.Reports;

/// <summary>
/// Tests the Z-Report reconciliation identity: Gross - Returns - Discounts = Net.
/// These tests verify the arithmetic consistency between sub-reports used to compose the Z-Report.
/// </summary>
public class ZReportReconciliationTests : IClassFixture<DatabaseFixture>
{
    private readonly AutoPartDbContext _db;
    private readonly IReportReadRepository _reports;

    public ZReportReconciliationTests(DatabaseFixture fixture)
    {
        _db = fixture.CreateContext();
        _reports = new ReportReadRepository(_db);
    }

    /// <summary>
    /// The Z-Report reconciliation identity is:
    ///   Gross - Returns - Discounts = Net
    /// This tests that the sub-reports produce arithmetically consistent totals.
    ///
    /// Seed data:
    ///   Sales Summary (today): GrossAmount=2005, DiscountAmount=20, TaxAmount=200.50
    ///   Sales Returns (today): RefundAmount=150 (1 return on SO1)
    ///
    /// Reconciliation:
    ///   Gross = 2005
    ///   Returns = 150
    ///   Discounts = 20
    ///   Net = Gross - Returns - Discounts = 2005 - 150 - 20 = 1835
    /// </summary>
    [Fact]
    public async Task ZReport_GrossMinusReturnsMinusDiscountsEqualsNet()
    {
        var today = DateTime.UtcNow.Date;
        var summaryRows = await _reports.GetSalesSummaryAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today,
            GroupBy = "day"
        });

        var returns = await _reports.GetSalesReturnsAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today
        }, maxRowsOverride: 10000);

        var gross = summaryRows.Sum(r => r.GrossAmount);
        var discounts = summaryRows.Sum(r => r.DiscountAmount);
        var returnsTotal = returns.Data.Sum(r => r.RefundAmount);
        var expectedNet = gross - returnsTotal - discounts;

        Assert.Equal(2005m, gross, 2);
        Assert.Equal(20m, discounts, 2);
        Assert.Equal(150m, returnsTotal, 2);
        Assert.Equal(1835m, expectedNet, 2);
    }

    /// <summary>
    /// Payment collections by method should sum to the total payments received.
    /// Seed: CASH=432, CREDIT_NOTE=280 → total = 712
    /// </summary>
    [Fact]
    public async Task ZReport_PaymentMethodsSumToTotal()
    {
        var today = DateTime.UtcNow.Date;
        var paymentRows = await _reports.GetPaymentCollectionsAsync(new ReportQuery
        {
            FromDate = today,
            ToDate = today,
            GroupBy = "method"
        });

        var totalAmount = paymentRows.Sum(r => r.TotalAmount);
        var totalPayments = paymentRows.Sum(r => r.PaymentCount);

        Assert.Equal(712m, totalAmount, 2);
        Assert.Equal(2, totalPayments);
    }
}
