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

    public static readonly IReadOnlyList<ReportColumn<SalesByCategoryRowDto>> SalesByCategory =
    [
        new("Category", r => r.CategoryName),
        new("Orders", r => r.OrderCount, ReportColumnFormat.Integer),
        new("Qty Sold", r => r.QuantitySold, ReportColumnFormat.Integer),
        new("Net Revenue", r => r.NetRevenue, ReportColumnFormat.Money),
        new("% of Total", r => r.PercentOfTotal, ReportColumnFormat.Percent)
    ];

    public static readonly IReadOnlyList<ReportColumn<SalesByCustomerRowDto>> SalesByCustomer =
    [
        new("Customer Code", r => r.CustomerCode),
        new("Customer", r => r.CustomerName),
        new("Type", r => r.CustomerType),
        new("Orders", r => r.OrderCount, ReportColumnFormat.Integer),
        new("Revenue", r => r.Revenue, ReportColumnFormat.Money),
        new("Paid", r => r.PaidAmount, ReportColumnFormat.Money),
        new("Outstanding", r => r.Outstanding, ReportColumnFormat.Money),
        new("Last Purchase", r => r.LastPurchaseDate, ReportColumnFormat.Date)
    ];

    public static readonly IReadOnlyList<ReportColumn<SalesBySalespersonRowDto>> SalesBySalesperson =
    [
        new("Salesperson", r => r.TechnicianName),
        new("Orders", r => r.OrderCount, ReportColumnFormat.Integer),
        new("Qty Sold", r => r.QuantitySold, ReportColumnFormat.Integer),
        new("Revenue", r => r.Revenue, ReportColumnFormat.Money),
        new("Avg Order", r => r.AverageOrderValue, ReportColumnFormat.Money)
    ];

    public static readonly IReadOnlyList<ReportColumn<SalesReturnRowDto>> SalesReturns =
    [
        new("Return Date", r => r.ReturnDate, ReportColumnFormat.Date),
        new("Return No.", r => r.ReturnNumber),
        new("SO Number", r => r.SoNumber),
        new("Customer", r => r.CustomerName),
        new("Status", r => r.Status),
        new("Refund Type", r => r.RefundType),
        new("Refund Amount", r => r.RefundAmount, ReportColumnFormat.Money),
        new("Currency", r => r.Currency),
        new("Reason", r => r.Reason)
    ];

    public static readonly IReadOnlyList<ReportColumn<PaymentCollectionRowDto>> PaymentCollections =
    [
        new("Group", r => r.GroupKey),
        new("Payments", r => r.PaymentCount, ReportColumnFormat.Integer),
        new("Total Amount", r => r.TotalAmount, ReportColumnFormat.Money)
    ];

    public static readonly IReadOnlyList<ReportColumn<ProfitByProductRowDto>> ProfitByProduct =
    [
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("Qty Sold", r => r.QuantitySold, ReportColumnFormat.Integer),
        new("Net Revenue", r => r.NetRevenue, ReportColumnFormat.Money),
        new("COGS", r => r.Cogs, ReportColumnFormat.Money),
        new("Gross Profit", r => r.GrossProfit, ReportColumnFormat.Money),
        new("Margin %", r => r.MarginPercent, ReportColumnFormat.Percent)
    ];

    public static readonly IReadOnlyList<ReportColumn<LowStockRowDto>> LowStock =
    [
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("SKU", r => r.Sku),
        new("Variant", r => r.VariantName),
        new("Category", r => r.CategoryName),
        new("Warehouse", r => r.WarehouseName),
        new("On Hand", r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new("Minimum", r => r.MinimumStock, ReportColumnFormat.Integer),
        new("Reorder Level", r => r.ReorderLevel, ReportColumnFormat.Integer),
        new("Reorder Qty", r => r.ReorderQuantity, ReportColumnFormat.Integer),
        new("Shortfall", r => r.Shortfall, ReportColumnFormat.Integer)
    ];

    public static readonly IReadOnlyList<ReportColumn<StockMovementRowDto>> StockMovements =
    [
        new("Date", r => r.MovementDate, ReportColumnFormat.DateTime),
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("Variant", r => r.VariantName),
        new("Warehouse", r => r.WarehouseName),
        new("Type", r => r.MovementType),
        new("Quantity", r => r.Quantity, ReportColumnFormat.Integer),
        new("Reason", r => r.Reason),
        new("Reference", r => r.ReferenceNumber)
    ];

    public static readonly IReadOnlyList<ReportColumn<ExpiringLotRowDto>> ExpiringLots =
    [
        new("Lot No.", r => r.LotNumber),
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("Warehouse", r => r.WarehouseName),
        new("Supplier", r => r.SupplierName),
        new("Received", r => r.ReceivingDate, ReportColumnFormat.Date),
        new("Expiry", r => r.ExpiryDate, ReportColumnFormat.Date),
        new("Days to Expiry", r => r.DaysToExpiry, ReportColumnFormat.Integer),
        new("Qty Available", r => r.QuantityAvailable, ReportColumnFormat.Integer),
        new("Stock Value", r => r.StockValue, ReportColumnFormat.Money)
    ];

    public static readonly IReadOnlyList<ReportColumn<SlowMovingStockRowDto>> SlowMovingStock =
    [
        new("Part No.", r => r.PartNumber),
        new("Product", r => r.PartName),
        new("Category", r => r.CategoryName),
        new("Warehouse", r => r.WarehouseName),
        new("On Hand", r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new("Stock Value", r => r.StockValue, ReportColumnFormat.Money),
        new("Last Sale", r => r.LastSaleDate, ReportColumnFormat.Date),
        new("Days Since Sale", r => r.DaysSinceLastSale, ReportColumnFormat.Integer)
    ];
}
