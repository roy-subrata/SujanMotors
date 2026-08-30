using AutoPartShop.Api.Pdf.Design;
using AutoPartShop.Application.DTOs.ReportDtos;

namespace AutoPartShop.Api.Services;

/// <summary>
/// Export column definitions, one method per report — the single source of truth for what an
/// exported file contains. Headers are resolved per-language via <see cref="DocStrings"/> under the
/// report.common.* namespace. Keep in sync with the frontend report-page column configs.
/// </summary>
public static class ReportColumnMaps
{
    private static string T(string key, string lang) => DocStrings.T($"report.common.{key}", lang);

    public static IReadOnlyList<ReportColumn<SalesSummaryRowDto>> SalesSummary(string lang) =>
    [
        new(T("period", lang), r => r.PeriodStart, ReportColumnFormat.Date),
        new(T("orders", lang), r => r.OrderCount, ReportColumnFormat.Integer),
        new(T("gross", lang), r => r.GrossAmount, ReportColumnFormat.Money),
        new(T("discount", lang), r => r.DiscountAmount, ReportColumnFormat.Money),
        new(T("tax", lang), r => r.TaxAmount, ReportColumnFormat.Money),
        new(T("net", lang), r => r.NetAmount, ReportColumnFormat.Money),
        new(T("grandTotal", lang), r => r.GrandTotal, ReportColumnFormat.Money),
        new(T("avgOrder", lang), r => r.AverageOrderValue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<SalesByProductRowDto>> SalesByProduct(string lang) =>
    [
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("sku", lang), r => r.Sku),
        new(T("category", lang), r => r.CategoryName),
        new(T("brand", lang), r => r.BrandName),
        new(T("qtySold", lang), r => r.QuantitySold, ReportColumnFormat.Integer),
        new(T("grossRevenue", lang), r => r.GrossRevenue, ReportColumnFormat.Money),
        new(T("discount", lang), r => r.DiscountAmount, ReportColumnFormat.Money),
        new(T("netRevenue", lang), r => r.NetRevenue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<StockSummaryRowDto>> StockSummary(string lang) =>
    [
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("variant", lang), r => r.VariantName),
        new(T("sku", lang), r => r.Sku),
        new(T("category", lang), r => r.CategoryName),
        new(T("warehouse", lang), r => r.WarehouseName),
        new(T("onHand", lang), r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new(T("reserved", lang), r => r.QuantityReserved, ReportColumnFormat.Integer),
        new(T("damaged", lang), r => r.QuantityDamaged, ReportColumnFormat.Integer),
        new(T("available", lang), r => r.QuantityAvailable, ReportColumnFormat.Integer),
        new(T("avgCost", lang), r => r.AverageCost, ReportColumnFormat.Money),
        new(T("stockValue", lang), r => r.StockValue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<SalesByCategoryRowDto>> SalesByCategory(string lang) =>
    [
        new(T("category", lang), r => r.CategoryName),
        new(T("orders", lang), r => r.OrderCount, ReportColumnFormat.Integer),
        new(T("qtySold", lang), r => r.QuantitySold, ReportColumnFormat.Integer),
        new(T("netRevenue", lang), r => r.NetRevenue, ReportColumnFormat.Money),
        new(T("percentOfTotal", lang), r => r.PercentOfTotal, ReportColumnFormat.Percent)
    ];

    public static IReadOnlyList<ReportColumn<SalesByCustomerRowDto>> SalesByCustomer(string lang) =>
    [
        new(T("customerCode", lang), r => r.CustomerCode),
        new(T("customer", lang), r => r.CustomerName),
        new(T("type", lang), r => r.CustomerType),
        new(T("orders", lang), r => r.OrderCount, ReportColumnFormat.Integer),
        new(T("revenue", lang), r => r.Revenue, ReportColumnFormat.Money),
        new(T("paid", lang), r => r.PaidAmount, ReportColumnFormat.Money),
        new(T("outstanding", lang), r => r.Outstanding, ReportColumnFormat.Money),
        new(T("lastPurchase", lang), r => r.LastPurchaseDate, ReportColumnFormat.Date)
    ];

    public static IReadOnlyList<ReportColumn<SalesBySalespersonRowDto>> SalesBySalesperson(string lang) =>
    [
        new(T("salesperson", lang), r => r.TechnicianName),
        new(T("orders", lang), r => r.OrderCount, ReportColumnFormat.Integer),
        new(T("qtySold", lang), r => r.QuantitySold, ReportColumnFormat.Integer),
        new(T("revenue", lang), r => r.Revenue, ReportColumnFormat.Money),
        new(T("avgOrder", lang), r => r.AverageOrderValue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<SalesByCashierRowDto>> SalesByCashier(string lang) =>
    [
        new(T("cashier", lang), r => r.CashierName),
        new(T("orders", lang), r => r.OrderCount, ReportColumnFormat.Integer),
        new(T("qtySold", lang), r => r.QuantitySold, ReportColumnFormat.Integer),
        new(T("revenue", lang), r => r.Revenue, ReportColumnFormat.Money),
        new(T("avgOrder", lang), r => r.AverageOrderValue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<SalesReturnRowDto>> SalesReturns(string lang) =>
    [
        new(T("returnDate", lang), r => r.ReturnDate, ReportColumnFormat.Date),
        new(T("returnNo", lang), r => r.ReturnNumber),
        new(T("soNumber", lang), r => r.SoNumber),
        new(T("customer", lang), r => r.CustomerName),
        new(T("status", lang), r => r.Status),
        new(T("refundType", lang), r => r.RefundType),
        new(T("refundAmount", lang), r => r.RefundAmount, ReportColumnFormat.Money),
        new(T("currency", lang), r => r.Currency),
        new(T("reason", lang), r => r.Reason)
    ];

    public static IReadOnlyList<ReportColumn<PaymentCollectionRowDto>> PaymentCollections(string lang) =>
    [
        new(T("group", lang), r => r.GroupKey),
        new(T("payments", lang), r => r.PaymentCount, ReportColumnFormat.Integer),
        new(T("totalAmount", lang), r => r.TotalAmount, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<ProfitByProductRowDto>> ProfitByProduct(string lang) =>
    [
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("qtySold", lang), r => r.QuantitySold, ReportColumnFormat.Integer),
        new(T("netRevenue", lang), r => r.NetRevenue, ReportColumnFormat.Money),
        new(T("cogs", lang), r => r.Cogs, ReportColumnFormat.Money),
        new(T("grossProfit", lang), r => r.GrossProfit, ReportColumnFormat.Money),
        new(T("marginPercent", lang), r => r.MarginPercent, ReportColumnFormat.Percent)
    ];

    public static IReadOnlyList<ReportColumn<LowStockRowDto>> LowStock(string lang) =>
    [
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("sku", lang), r => r.Sku),
        new(T("variant", lang), r => r.VariantName),
        new(T("category", lang), r => r.CategoryName),
        new(T("warehouse", lang), r => r.WarehouseName),
        new(T("onHand", lang), r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new(T("minimum", lang), r => r.MinimumStock, ReportColumnFormat.Integer),
        new(T("reorderLevel", lang), r => r.ReorderLevel, ReportColumnFormat.Integer),
        new(T("reorderQty", lang), r => r.ReorderQuantity, ReportColumnFormat.Integer),
        new(T("shortfall", lang), r => r.Shortfall, ReportColumnFormat.Integer)
    ];

    public static IReadOnlyList<ReportColumn<StockMovementRowDto>> StockMovements(string lang) =>
    [
        new(T("date", lang), r => r.MovementDate, ReportColumnFormat.DateTime),
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("variant", lang), r => r.VariantName),
        new(T("warehouse", lang), r => r.WarehouseName),
        new(T("type", lang), r => r.MovementType),
        new(T("quantity", lang), r => r.Quantity, ReportColumnFormat.Integer),
        new(T("reason", lang), r => r.Reason),
        new(T("reference", lang), r => r.ReferenceNumber)
    ];

    public static IReadOnlyList<ReportColumn<ExpiringLotRowDto>> ExpiringLots(string lang) =>
    [
        new(T("lotNo", lang), r => r.LotNumber),
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("warehouse", lang), r => r.WarehouseName),
        new(T("supplier", lang), r => r.SupplierName),
        new(T("received", lang), r => r.ReceivingDate, ReportColumnFormat.Date),
        new(T("expiry", lang), r => r.ExpiryDate, ReportColumnFormat.Date),
        new(T("daysToExpiry", lang), r => r.DaysToExpiry, ReportColumnFormat.Integer),
        new(T("qtyAvailable", lang), r => r.QuantityAvailable, ReportColumnFormat.Integer),
        new(T("stockValue", lang), r => r.StockValue, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<SlowMovingStockRowDto>> SlowMovingStock(string lang) =>
    [
        new(T("partNo", lang), r => r.PartNumber),
        new(T("product", lang), r => r.PartName),
        new(T("category", lang), r => r.CategoryName),
        new(T("warehouse", lang), r => r.WarehouseName),
        new(T("onHand", lang), r => r.QuantityOnHand, ReportColumnFormat.Integer),
        new(T("stockValue", lang), r => r.StockValue, ReportColumnFormat.Money),
        new(T("lastSale", lang), r => r.LastSaleDate, ReportColumnFormat.Date),
        new(T("daysSinceSale", lang), r => r.DaysSinceLastSale, ReportColumnFormat.Integer)
    ];

    public static IReadOnlyList<ReportColumn<PurchaseSummaryRowDto>> PurchaseSummary(string lang) =>
    [
        new(T("period", lang), r => r.PeriodStart, ReportColumnFormat.Date),
        new(T("poCount", lang), r => r.PoCount, ReportColumnFormat.Integer),
        new(T("totalAmount", lang), r => r.TotalAmount, ReportColumnFormat.Money),
        new(T("paid", lang), r => r.PaidAmount, ReportColumnFormat.Money),
        new(T("outstanding", lang), r => r.Outstanding, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<PurchasesBySupplierRowDto>> PurchasesBySupplier(string lang) =>
    [
        new(T("supplierCode", lang), r => r.SupplierCode),
        new(T("supplier", lang), r => r.SupplierName),
        new(T("poCount", lang), r => r.PoCount, ReportColumnFormat.Integer),
        new(T("totalAmount", lang), r => r.TotalAmount, ReportColumnFormat.Money),
        new(T("receivedValue", lang), r => r.ReceivedValue, ReportColumnFormat.Money),
        new(T("paid", lang), r => r.PaidAmount, ReportColumnFormat.Money),
        new(T("returnedValue", lang), r => r.ReturnedValue, ReportColumnFormat.Money),
        new(T("balance", lang), r => r.Balance, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<PurchaseReturnRowDto>> PurchaseReturns(string lang) =>
    [
        new(T("returnDate", lang), r => r.ReturnDate, ReportColumnFormat.Date),
        new(T("returnNo", lang), r => r.ReturnNumber),
        new(T("poNumber", lang), r => r.PoNumber),
        new(T("supplier", lang), r => r.SupplierName),
        new(T("status", lang), r => r.Status),
        new(T("settlement", lang), r => r.SettlementStatus),
        new(T("refundAmount", lang), r => r.RefundAmount, ReportColumnFormat.Money),
        new(T("currency", lang), r => r.Currency)
    ];

    public static IReadOnlyList<ReportColumn<ReceivablesAgingRowDto>> ReceivablesAging(string lang) =>
    [
        new(T("customerCode", lang), r => r.CustomerCode),
        new(T("customer", lang), r => r.CustomerName),
        new(T("current", lang), r => r.CurrentAmount, ReportColumnFormat.Money),
        new(T("days1to30", lang), r => r.Days1To30, ReportColumnFormat.Money),
        new(T("days31to60", lang), r => r.Days31To60, ReportColumnFormat.Money),
        new(T("days61to90", lang), r => r.Days61To90, ReportColumnFormat.Money),
        new(T("days90plus", lang), r => r.Days90Plus, ReportColumnFormat.Money),
        new(T("total", lang), r => r.Total, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<PayablesAgingRowDto>> PayablesAging(string lang) =>
    [
        new(T("supplierCode", lang), r => r.SupplierCode),
        new(T("supplier", lang), r => r.SupplierName),
        new(T("current", lang), r => r.CurrentAmount, ReportColumnFormat.Money),
        new(T("days1to30", lang), r => r.Days1To30, ReportColumnFormat.Money),
        new(T("days31to60", lang), r => r.Days31To60, ReportColumnFormat.Money),
        new(T("days61to90", lang), r => r.Days61To90, ReportColumnFormat.Money),
        new(T("days90plus", lang), r => r.Days90Plus, ReportColumnFormat.Money),
        new(T("total", lang), r => r.Total, ReportColumnFormat.Money)
    ];

    public static IReadOnlyList<ReportColumn<ExpenseReportRowDto>> Expenses(string lang) =>
    [
        new(T("group", lang), r => r.GroupKey),
        new(T("count", lang), r => r.ExpenseCount, ReportColumnFormat.Integer),
        new(T("totalAmount", lang), r => r.TotalAmount, ReportColumnFormat.Money)
    ];
}
