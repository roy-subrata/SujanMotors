using AutoPartShop.Api.Tests.Fixtures;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Tests.Reports;

public class SalesReportCalculationTests : IClassFixture<DatabaseFixture>
{
    private readonly AutoPartDbContext _db;
    private readonly IReportReadRepository _reports;

    public SalesReportCalculationTests(DatabaseFixture fixture)
    {
        _db = fixture.CreateContext();
        _reports = new ReportReadRepository(_db);
    }

    private static ReportQuery TodayQuery() => new()
    {
        FromDate = DateTime.UtcNow.Date,
        ToDate = DateTime.UtcNow.Date
    };

    [Fact]
    public async Task SalesSummary_TotalsAcrossAllOrders()
    {
        var rows = await _reports.GetSalesSummaryAsync(TodayQuery());

        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(3, row.OrderCount);
        Assert.Equal(2005m, row.GrossAmount, 2);
        Assert.Equal(20m, row.DiscountAmount, 2);
        Assert.Equal(200.50m, row.TaxAmount, 2);
        Assert.Equal(1804.50m, row.NetAmount, 2);
        Assert.Equal(2005m, row.GrandTotal, 2);
        Assert.Equal(601.50m, row.AverageOrderValue, 2);
    }

    [Fact]
    public async Task SalesSummary_PeriodStartIsToday()
    {
        var rows = await _reports.GetSalesSummaryAsync(TodayQuery());

        Assert.Single(rows);
        Assert.Equal(DateTime.UtcNow.Date, rows[0].PeriodStart.Date);
    }

    [Fact]
    public async Task SalesByProduct_ReturnDeductsQuantityAndRevenue()
    {
        var result = await _reports.GetSalesByProductAsync(TodayQuery());

        Assert.Equal(3, result.Data.Count);

        var partA = result.Data.Single(r => r.PartId == TestSeedData.PartAId);
        Assert.Equal(1, partA.QuantitySold);
        Assert.Equal(150m, partA.GrossRevenue, 2);
        Assert.Equal(20m, partA.DiscountAmount, 2);
        Assert.Equal(130m, partA.NetRevenue, 2);

        var partB = result.Data.Single(r => r.PartId == TestSeedData.PartBId);
        Assert.Equal(6, partB.QuantitySold);
        Assert.Equal(1200m, partB.GrossRevenue, 2);
        Assert.Equal(75m, partB.DiscountAmount, 2);
        Assert.Equal(1125m, partB.NetRevenue, 2);

        var partC = result.Data.Single(r => r.PartId == TestSeedData.PartCId);
        Assert.Equal(10, partC.QuantitySold);
        Assert.Equal(600m, partC.GrossRevenue, 2);
        Assert.Equal(0m, partC.DiscountAmount, 2);
        Assert.Equal(600m, partC.NetRevenue, 2);
    }

    [Fact]
    public async Task SalesByProduct_OrderedByNetRevenueDescending()
    {
        var result = await _reports.GetSalesByProductAsync(TodayQuery());

        Assert.Equal(3, result.Data.Count);
        Assert.Equal(TestSeedData.PartBId, result.Data[0].PartId);
        Assert.Equal(TestSeedData.PartCId, result.Data[1].PartId);
        Assert.Equal(TestSeedData.PartAId, result.Data[2].PartId);
    }

    [Fact]
    public async Task SalesByCategory_BrakesAndFilters()
    {
        var rows = await _reports.GetSalesByCategoryAsync(TodayQuery());

        Assert.Equal(2, rows.Count);

        var brakes = rows.Single(r => r.CategoryName == "Brakes");
        Assert.Equal(2, brakes.OrderCount);
        Assert.Equal(8, brakes.QuantitySold);
        Assert.Equal(1405m, brakes.NetRevenue, 2);
        Assert.Equal(70.07m, brakes.PercentOfTotal, 2);

        var filters = rows.Single(r => r.CategoryName == "Filters");
        Assert.Equal(1, filters.OrderCount);
        Assert.Equal(10, filters.QuantitySold);
        Assert.Equal(600m, filters.NetRevenue, 2);
        Assert.Equal(29.93m, filters.PercentOfTotal, 2);
    }

    [Fact]
    public async Task SalesByCustomer_RevenueAndOutstanding()
    {
        var result = await _reports.GetSalesByCustomerAsync(TodayQuery());

        Assert.Equal(2, result.Data.Count);

        var rahim = result.Data.Single(r => r.CustomerName == "Rahim Khan");
        Assert.Equal("RETAIL", rahim.CustomerType);
        Assert.Equal(2, rahim.OrderCount);
        Assert.Equal(1080m, rahim.Revenue, 2);
        Assert.Equal(432m, rahim.PaidAmount, 2);
        Assert.Equal(648m, rahim.Outstanding, 2);

        var karim = result.Data.Single(r => r.CustomerName == "Karim Ahmed");
        Assert.Equal("WHOLESALE", karim.CustomerType);
        Assert.Equal(1, karim.OrderCount);
        Assert.Equal(925m, karim.Revenue, 2);
        Assert.Equal(0m, karim.PaidAmount, 2);
        Assert.Equal(925m, karim.Outstanding, 2);
    }

    [Fact]
    public async Task SalesBySalesperson_TechnicianAndUnassigned()
    {
        var rows = await _reports.GetSalesBySalespersonAsync(TodayQuery());

        Assert.Equal(2, rows.Count);

        var rafiq = rows.Single(r => r.TechnicianName == "Rafiq Uddin");
        Assert.Equal(1, rafiq.OrderCount);
        Assert.Equal(3, rafiq.QuantitySold);
        Assert.Equal(432m, rafiq.Revenue, 2);
        Assert.Equal(432m, rafiq.AverageOrderValue, 2);

        var unassigned = rows.Single(r => r.TechnicianName == "Unassigned");
        Assert.Equal(2, unassigned.OrderCount);
        Assert.Equal(15, unassigned.QuantitySold);
        Assert.Equal(1372.50m, unassigned.Revenue, 2);
        Assert.Equal(686.25m, unassigned.AverageOrderValue, 2);
    }

    [Fact]
    public async Task SalesByCashier_CashierAndUnassigned()
    {
        var rows = await _reports.GetSalesByCashierAsync(TodayQuery());

        Assert.Equal(2, rows.Count);

        var cashier = rows.Single(r => r.CashierName == "Cashier One");
        Assert.Equal(1, cashier.OrderCount);
        Assert.Equal(3, cashier.QuantitySold);
        Assert.Equal(432m, cashier.Revenue, 2);
        Assert.Equal(432m, cashier.AverageOrderValue, 2);

        var unassigned = rows.Single(r => r.CashierName == "Unassigned");
        Assert.Equal(2, unassigned.OrderCount);
        Assert.Equal(15, unassigned.QuantitySold);
        Assert.Equal(1372.50m, unassigned.Revenue, 2);
        Assert.Equal(686.25m, unassigned.AverageOrderValue, 2);
    }

    [Fact]
    public async Task SalesReturns_ListedReturnOnSO1()
    {
        var result = await _reports.GetSalesReturnsAsync(TodayQuery());

        Assert.Single(result.Data);
        var ret = result.Data[0];
        Assert.Equal("RET-001", ret.ReturnNumber);
        Assert.Equal("SO-001", ret.SoNumber);
        Assert.Equal("Rahim Khan", ret.CustomerName);
        Assert.Equal("PROCESSED", ret.Status);
        Assert.Equal("FULL", ret.RefundType);
        Assert.Equal(150m, ret.RefundAmount, 2);
        Assert.Equal("BDT", ret.Currency);
    }

    [Fact]
    public async Task PaymentCollections_GroupByDay()
    {
        var query = new ReportQuery
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date,
            GroupBy = "day"
        };

        var rows = await _reports.GetPaymentCollectionsAsync(query);

        Assert.Single(rows);
        var row = rows[0];
        Assert.Equal(DateTime.UtcNow.Date.ToString("yyyy-MM-dd"), row.GroupKey);
        Assert.Equal(2, row.PaymentCount);
        Assert.Equal(712m, row.TotalAmount, 2);
    }

    [Fact]
    public async Task PaymentCollections_GroupByMethod()
    {
        var query = new ReportQuery
        {
            FromDate = DateTime.UtcNow.Date,
            ToDate = DateTime.UtcNow.Date,
            GroupBy = "method"
        };

        var rows = await _reports.GetPaymentCollectionsAsync(query);

        Assert.Equal(2, rows.Count);

        var cash = rows.Single(r => r.GroupKey == "CASH");
        Assert.Equal(1, cash.PaymentCount);
        Assert.Equal(432m, cash.TotalAmount, 2);

        var creditNote = rows.Single(r => r.GroupKey == "CREDIT_NOTE");
        Assert.Equal(1, creditNote.PaymentCount);
        Assert.Equal(280m, creditNote.TotalAmount, 2);
    }

    [Fact]
    public async Task ProfitByProduct_MarginAndCogs()
    {
        var result = await _reports.GetProfitByProductAsync(TodayQuery());

        Assert.Equal(3, result.Data.Count);

        var partA = result.Data.Single(r => r.PartId == TestSeedData.PartAId);
        Assert.Equal(2, partA.QuantitySold);
        Assert.Equal(280m, partA.NetRevenue, 2);
        Assert.Equal(50m, partA.Cogs, 2);
        Assert.Equal(230m, partA.GrossProfit, 2);
        Assert.Equal(82.14m, (decimal)partA.MarginPercent!, 2);

        var partB = result.Data.Single(r => r.PartId == TestSeedData.PartBId);
        Assert.Equal(6, partB.QuantitySold);
        Assert.Equal(1125m, partB.NetRevenue, 2);
        Assert.Equal(80m, partB.Cogs, 2);
        Assert.Equal(1045m, partB.GrossProfit, 2);
        Assert.Equal(92.89m, (decimal)partB.MarginPercent!, 2);

        var partC = result.Data.Single(r => r.PartId == TestSeedData.PartCId);
        Assert.Equal(10, partC.QuantitySold);
        Assert.Equal(600m, partC.NetRevenue, 2);
        Assert.Equal(20m, partC.Cogs, 2);
        Assert.Equal(580m, partC.GrossProfit, 2);
        Assert.Equal(96.67m, (decimal)partC.MarginPercent!, 2);
    }

    [Fact]
    public async Task ProfitByProduct_OrderedByGrossProfitDescending()
    {
        var result = await _reports.GetProfitByProductAsync(TodayQuery());

        Assert.Equal(3, result.Data.Count);
        Assert.Equal(TestSeedData.PartBId, result.Data[0].PartId);
        Assert.Equal(TestSeedData.PartCId, result.Data[1].PartId);
        Assert.Equal(TestSeedData.PartAId, result.Data[2].PartId);
    }
}
