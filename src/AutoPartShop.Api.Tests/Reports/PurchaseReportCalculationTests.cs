using AutoPartShop.Api.Tests.Fixtures;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Tests.Reports;

public class PurchaseReportCalculationTests : IClassFixture<DatabaseFixture>
{
    private readonly AutoPartDbContext _db;
    private readonly IReportReadRepository _reports;

    public PurchaseReportCalculationTests(DatabaseFixture fixture)
    {
        _db = fixture.CreateContext();
        _reports = new ReportReadRepository(_db);
    }

    [Fact]
    public async Task PurchaseSummary_Totals()
    {
        var today = DateTime.UtcNow.Date;
        var query = new ReportQuery
        {
            GroupBy = "day",
            FromDate = today,
            ToDate = today.AddDays(1).AddTicks(-1)
        };

        var rows = await _reports.GetPurchaseSummaryAsync(query);

        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(2, row.PoCount);
        Assert.Equal(946m, row.TotalAmount);
        Assert.Equal(100m, row.PaidAmount);
        Assert.Equal(846m, row.Outstanding);
    }

    [Fact]
    public async Task PurchasesBySupplier_Balance()
    {
        var today = DateTime.UtcNow.Date;
        var query = new ReportQuery
        {
            FromDate = today,
            ToDate = today.AddDays(1).AddTicks(-1)
        };

        var result = await _reports.GetPurchasesBySupplierAsync(query);

        Assert.Equal(2, result.Data.Count);

        var alpha = result.Data.Single(r => r.SupplierName == "Supplier Alpha");
        Assert.Equal(1, alpha.PoCount);
        Assert.Equal(550m, alpha.TotalAmount);
        Assert.Equal(500m, alpha.ReceivedValue);
        Assert.Equal(100m, alpha.PaidAmount);
        Assert.Equal(100m, alpha.ReturnedValue);
        Assert.Equal(350m, alpha.Balance);

        var beta = result.Data.Single(r => r.SupplierName == "Supplier Beta");
        Assert.Equal(1, beta.PoCount);
        Assert.Equal(396m, beta.TotalAmount);
        Assert.Equal(0m, beta.ReceivedValue);
        Assert.Equal(0m, beta.PaidAmount);
        Assert.Equal(0m, beta.ReturnedValue);
        Assert.Equal(396m, beta.Balance);
    }

    [Fact]
    public async Task PurchaseReturns_FilterByDate()
    {
        var today = DateTime.UtcNow.Date;
        var query = new ReportQuery
        {
            FromDate = today,
            ToDate = today.AddDays(1).AddTicks(-1)
        };

        var result = await _reports.GetPurchaseReturnsAsync(query);

        Assert.Single(result.Data);
        var ret = result.Data[0];
        Assert.Equal("PR-001", ret.ReturnNumber);
        Assert.Equal("PO-001", ret.PoNumber);
        Assert.Equal("Supplier Alpha", ret.SupplierName);
        Assert.Equal(100m, ret.RefundAmount);
    }

    [Fact]
    public async Task PurchaseSummary_GroupByWeek()
    {
        var today = DateTime.UtcNow.Date;
        var query = new ReportQuery
        {
            GroupBy = "week",
            FromDate = today,
            ToDate = today.AddDays(1).AddTicks(-1)
        };

        var rows = await _reports.GetPurchaseSummaryAsync(query);

        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(2, row.PoCount);
        Assert.Equal(946m, row.TotalAmount);
    }
}
