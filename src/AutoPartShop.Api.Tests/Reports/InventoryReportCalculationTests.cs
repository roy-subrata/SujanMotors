using AutoPartShop.Api.Tests.Fixtures;
using AutoPartShop.Application.DTOs.ReportDtos;
using AutoPartShop.Application.Reports;
using AutoPartShop.Infrastructure.Data;
using AutoPartShop.Infrastructure.Repositories;

namespace AutoPartShop.Api.Tests.Reports;

public class InventoryReportCalculationTests : IClassFixture<DatabaseFixture>
{
    private readonly AutoPartDbContext _db;
    private readonly IReportReadRepository _reports;

    public InventoryReportCalculationTests(DatabaseFixture fixture)
    {
        _db = fixture.CreateContext();
        _reports = new ReportReadRepository(_db);
    }

    [Fact]
    public async Task StockSummary_Valuation()
    {
        var query = new ReportQuery();
        var result = await _reports.GetStockSummaryAsync(query, maxRowsOverride: 100);

        Assert.Equal(3, result.Data.Count);

        var partA = result.Data.Single(r => r.PartId == TestSeedData.PartAId);
        Assert.Equal(30, partA.QuantityOnHand);
        Assert.Equal(5, partA.QuantityReserved);
        Assert.Equal(25, partA.QuantityAvailable);
        Assert.Equal(50m, (decimal)partA.AverageCost!, 2);
        Assert.Equal(1500m, partA.StockValue, 2);

        var partB = result.Data.Single(r => r.PartId == TestSeedData.PartBId);
        Assert.Equal(50, partB.QuantityOnHand);
        Assert.Equal(0, partB.QuantityReserved);
        Assert.Equal(50, partB.QuantityAvailable);
        Assert.Equal(80m, (decimal)partB.AverageCost!, 2);
        Assert.Equal(4000m, partB.StockValue, 2);

        var partC = result.Data.Single(r => r.PartId == TestSeedData.PartCId);
        Assert.Equal(100, partC.QuantityOnHand);
        Assert.Equal(0, partC.QuantityReserved);
        Assert.Equal(100, partC.QuantityAvailable);
        Assert.Equal(20m, (decimal)partC.AverageCost!, 2);
        Assert.Equal(2000m, partC.StockValue, 2);
    }

    [Fact]
    public async Task StockSummary_Totals()
    {
        var query = new ReportQuery();
        var result = await _reports.GetStockSummaryAsync(query, maxRowsOverride: 100);

        Assert.NotNull(result.Totals);
        Assert.Equal(180, result.Totals.TotalQuantityOnHand);
        Assert.Equal(7500m, result.Totals.TotalStockValue, 2);
        Assert.Equal(3, result.Totals.DistinctPartCount);
    }

    [Fact]
    public async Task LowStock_OnlyBelowMinimum()
    {
        var query = new ReportQuery();
        var result = await _reports.GetLowStockAsync(query, maxRowsOverride: 100);

        Assert.Single(result.Data);

        var partA = result.Data.Single(r => r.PartId == TestSeedData.PartAId);
        Assert.Equal(30, partA.QuantityOnHand);
        Assert.Equal(50, partA.MinimumStock);
        Assert.Equal(20, partA.Shortfall);
    }

    [Fact]
    public async Task ExpiringLots_IncludesExpiredAndSoon()
    {
        var query = new ReportQuery { DaysAhead = 90, IncludeExpired = true };
        var result = await _reports.GetExpiringLotsAsync(query, maxRowsOverride: 100);

        Assert.Equal(2, result.Data.Count);

        var lot1 = result.Data.Single(r => r.LotNumber == "LOT-A1");
        Assert.Equal(-1, lot1.DaysToExpiry);
        Assert.Equal(30, lot1.QuantityAvailable);
        Assert.Equal(1500m, lot1.StockValue, 2);

        var lot2 = result.Data.Single(r => r.LotNumber == "LOT-B1");
        Assert.Equal(1, lot2.DaysToExpiry);
        Assert.Equal(50, lot2.QuantityAvailable);
        Assert.Equal(4000m, lot2.StockValue, 2);
    }

    [Fact]
    public async Task ExpiringLots_ExcludesExpired()
    {
        var query = new ReportQuery { DaysAhead = 90, IncludeExpired = false };
        var result = await _reports.GetExpiringLotsAsync(query, maxRowsOverride: 100);

        Assert.Single(result.Data);

        var lot2 = result.Data.Single(r => r.LotNumber == "LOT-B1");
        Assert.Equal(1, lot2.DaysToExpiry);
        Assert.Equal(50, lot2.QuantityAvailable);
        Assert.Equal(4000m, lot2.StockValue, 2);
    }

    [Fact]
    public async Task SlowMovingStock_FindsSlowMovers()
    {
        var query = new ReportQuery { NoSaleDays = 30 };
        var result = await _reports.GetSlowMovingStockAsync(query, maxRowsOverride: 100);

        Assert.Empty(result.Data);
    }
}
