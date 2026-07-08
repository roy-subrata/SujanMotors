using AutoPartShop.Application.DTOs.ReportDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Export column definitions, one list per report — the single source of truth for what an
/// exported file contains. Keep in sync with the frontend report-page column configs.
/// </summary>
public static class ReportColumnMaps
{
    public static readonly IReadOnlyList<ReportColumn<SalesSummaryRowDto>> SalesSummary =
    [
        new("Period", r => r.PeriodStart, ReportColumnFormat.Date),
        new("Orders", r => r.OrderCount, ReportColumnFormat.Integer),
        new("Gross", r => r.GrossAmount, ReportColumnFormat.Money),
        new("Discount", r => r.DiscountAmount, ReportColumnFormat.Money),
        new("Tax", r => r.TaxAmount, ReportColumnFormat.Money),
        new("Net", r => r.NetAmount, ReportColumnFormat.Money),
        new("Grand Total", r => r.GrandTotal, ReportColumnFormat.Money),
        new("Avg Order", r => r.AverageOrderValue, ReportColumnFormat.Money)
    ];

    public static readonly IReadOnlyList<ReportColumn<SalesByProductRowDto>> SalesByProduct =
    [
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("SKU", r => r.Sku),
        new("Category", r => r.CategoryName),
        new("Brand", r => r.BrandName),
        new("Qty Sold", r => r.QuantitySold, ReportColumnFormat.Integer),
        new("Gross Revenue", r => r.GrossRevenue, ReportColumnFormat.Money),
        new("Discount", r => r.DiscountAmount, ReportColumnFormat.Money),
        new("Net Revenue", r => r.NetRevenue, ReportColumnFormat.Money)
    ];

    public static readonly IReadOnlyList<ReportColumn<StockSummaryRowDto>> StockSummary =
    [
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("Variant", r => r.VariantName),
        new("SKU", r => r.Sku),
        new("Category", r => r.CategoryName),
        new("Warehouse", r => r.WarehouseName),
        new("On Hand", r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new("Reserved", r => r.QuantityReserved, ReportColumnFormat.Integer),
        new("Damaged", r => r.QuantityDamaged, ReportColumnFormat.Integer),
        new("Available", r => r.QuantityAvailable, ReportColumnFormat.Integer),
        new("Avg Cost", r => r.AverageCost, ReportColumnFormat.Money),
        new("Stock Value", r => r.StockValue, ReportColumnFormat.Money)
    ];
}
