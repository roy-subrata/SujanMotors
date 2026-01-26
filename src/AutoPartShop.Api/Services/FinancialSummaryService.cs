using AutoPartShop.Application.DTOs.DashboardDtos;
using AutoPartShop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AutoPartShop.Api.Services;

public interface IFinancialSummaryService
{
    Task<DashboardResponse> GetDashboardDataAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default);
    Task<FinancialSummaryResponse> GetFinancialSummaryAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default);
    Task<List<SalesTrendDto>> GetSalesTrendAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default);
}

public class FinancialSummaryService : IFinancialSummaryService
{
    private readonly AutoPartDbContext _dbContext;
    private readonly ICurrencyConversionService _currencyService;

    public FinancialSummaryService(AutoPartDbContext dbContext, ICurrencyConversionService currencyService)
    {
        _dbContext = dbContext;
        _currencyService = currencyService;
    }
    public async Task<DashboardResponse> GetDashboardDataAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var summary = await GetFinancialSummaryAsync(request, cancellationToken);
        var topProducts = await GetTopProductsAsync(request, cancellationToken);
        var topCustomers = await GetTopCustomersAsync(request, cancellationToken);
        var salesTrend = await GetSalesTrendAsync(request, cancellationToken);

        return new DashboardResponse
        {
            Summary = summary,
            TopProducts = topProducts,
            TopCustomers = topCustomers,
            SalesTrend = salesTrend
        };
    }

    public async Task<FinancialSummaryResponse> GetFinancialSummaryAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1).AddSeconds(-1);

        // Get sales data
        var salesOrders = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted)
            .ToListAsync(cancellationToken);

        // Convert all sales amounts to base currency
        var totalSales = 0m;
        var cashSales = 0m;
        var creditSales = 0m;

        foreach (var so in salesOrders)
        {
            var converted = await _currencyService.ConvertToBaseAsync(so.TotalAmount, so.Currency, so.SODate, cancellationToken);
            totalSales += converted;

            if (so.PaymentStatus == "PAID")
                cashSales += converted;
            else
                creditSales += converted;
        }

        // Get customer payments and convert to base currency
        var customerPaymentsList = await _dbContext.CustomerPayments
            .Where(cp => cp.PaymentDate >= startDate && cp.PaymentDate <= endDate && !cp.Isdeleted)
            .ToListAsync(cancellationToken);

        var customerPayments = 0m;
        foreach (var cp in customerPaymentsList)
        {
            customerPayments += await _currencyService.ConvertToBaseAsync(cp.Amount, cp.Currency, cp.PaymentDate, cancellationToken);
        }

        // Get purchase orders and convert to base currency
        var purchaseOrders = await _dbContext.PurchaseOrders
            .Where(po => po.PODate >= startDate && po.PODate <= endDate && !po.Isdeleted)
            .ToListAsync(cancellationToken);

        var totalPurchases = 0m;
        foreach (var po in purchaseOrders)
        {
            totalPurchases += await _currencyService.ConvertToBaseAsync(po.TotalAmount, po.Currency, po.PODate, cancellationToken);
        }

        // Get supplier payments and convert to base currency
        var supplierPaymentsList = await _dbContext.SupplierPayments
            .Where(sp => sp.PaymentDate >= startDate && sp.PaymentDate <= endDate && !sp.Isdeleted)
            .ToListAsync(cancellationToken);

        var supplierPayments = 0m;
        foreach (var sp in supplierPaymentsList)
        {
            supplierPayments += await _currencyService.ConvertToBaseAsync(sp.Amount, sp.Currency, sp.PaymentDate, cancellationToken);
        }

        // Get daily expenses and convert to base currency
        var dailyExpenses = await _dbContext.DailyExpenses
            .Where(de => de.ExpenseDate >= startDate && de.ExpenseDate <= endDate && !de.Isdeleted)
            .ToListAsync(cancellationToken);

        var totalDailyExpenses = 0m;
        foreach (var de in dailyExpenses)
        {
            totalDailyExpenses += await _currencyService.ConvertToBaseAsync(de.Amount, de.Currency, de.ExpenseDate, cancellationToken);
        }
        var dailyExpensesCount = dailyExpenses.Count;

        // Get outstanding balances
        var customersWithDue = await _dbContext.Customers
            .Where(c => c.CurrentBalance > 0 && !c.Isdeleted)
            .ToListAsync(cancellationToken);

        var suppliersWithDue = await _dbContext.Suppliers
            .Where(s => s.CurrentBalance < 0 && !s.Isdeleted)
            .ToListAsync(cancellationToken);

        // Calculate overdue (simplified - you may want to add due date logic)
        var customerOverdue = customersWithDue.Sum(c => c.CurrentBalance);
        var supplierOverdue = suppliersWithDue.Sum(s => Math.Abs(s.CurrentBalance));

        // Get inventory value
        var inventoryValue = await _dbContext.StockLevels
            .Where(sl => !sl.Isdeleted)
            .SumAsync(sl => sl.QuantityOnHand * (sl.Part != null ? sl.Part.CostPrice : 0), cancellationToken);

        // Get low stock items
        var lowStockItems = await _dbContext.StockLevels
            .Include(sl => sl.Part)
            .Where(sl => !sl.Isdeleted && sl.Part != null && !sl.Part.Isdeleted && sl.QuantityOnHand <= sl.Part.MinimumStock)
            .ToListAsync(cancellationToken);

        var lowStockValue = lowStockItems
            .Sum(sl => sl.QuantityOnHand * (sl.Part?.CostPrice ?? 0));

        // Get customer counts
        var totalCustomers = await _dbContext.Customers.CountAsync(c => !c.Isdeleted, cancellationToken);
        var newCustomers = await _dbContext.Customers
            .CountAsync(c => c.CreatedDate >= startDate && c.CreatedDate <= endDate && !c.Isdeleted, cancellationToken);

        // Calculate profit
        var grossProfit = totalSales - totalPurchases;
        var netProfit = grossProfit - totalDailyExpenses; // Subtract daily operational expenses
        var profitMargin = totalSales > 0 ? (grossProfit / totalSales) * 100 : 0;

        // Cash flow
        var cashInflow = cashSales + customerPayments;
        var cashOutflow = supplierPayments + totalDailyExpenses;

        return new FinancialSummaryResponse
        {
            StartDate = startDate,
            EndDate = endDate,
            Period = request.Period,

            // Revenue
            TotalSales = totalSales,
            TotalSalesCount = salesOrders.Count,
            CashSales = cashSales,
            CreditSales = creditSales,
            CustomerPaymentsReceived = customerPayments,
            TotalRevenue = totalSales + customerPayments,

            // Expenses
            TotalPurchases = totalPurchases,
            TotalPurchasesCount = purchaseOrders.Count,
            SupplierPaymentsMade = supplierPayments,
            DailyExpenses = totalDailyExpenses,
            DailyExpensesCount = dailyExpensesCount,
            OtherExpenses = 0,
            TotalExpenses = totalPurchases + supplierPayments + totalDailyExpenses,

            // Profitability
            GrossProfit = grossProfit,
            NetProfit = netProfit,
            ProfitMargin = profitMargin,

            // Outstanding
            CustomerDueAmount = customersWithDue.Sum(c => c.CurrentBalance),
            CustomerDueCount = customersWithDue.Count(),
            SupplierDueAmount = Math.Abs(suppliersWithDue.Sum(s => s.CurrentBalance)),
            SupplierDueCount = suppliersWithDue.Count(),

            // Overdue
            CustomerOverdueAmount = customerOverdue,
            CustomerOverdueCount = customersWithDue.Count(c => c.CurrentBalance > 0),
            SupplierOverdueAmount = supplierOverdue,
            SupplierOverdueCount = suppliersWithDue.Count(),

            // Inventory
            InventoryValue = inventoryValue,
            LowStockValue = lowStockValue,
            LowStockItemsCount = lowStockItems.Count(),

            // Cash Flow
            OpeningBalance = 0, // You may want to track this separately
            CashInflow = cashInflow,
            CashOutflow = cashOutflow,
            ClosingBalance = cashInflow - cashOutflow,

            // Additional
            AverageSaleValue = salesOrders.Count() > 0 ? totalSales / salesOrders.Count() : 0,
            AveragePurchaseValue = purchaseOrders.Count() > 0 ? totalPurchases / purchaseOrders.Count() : 0,
            TotalCustomers = totalCustomers,
            NewCustomers = newCustomers,
            TotalSuppliers = await _dbContext.Suppliers.CountAsync(s => !s.Isdeleted, cancellationToken),
            ActiveSuppliers = await _dbContext.Suppliers.CountAsync(s => s.IsActive && !s.Isdeleted, cancellationToken)
        };
    }

    public async Task<List<SalesTrendDto>> GetSalesTrendAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;

        var salesData = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted)
            .GroupBy(so => so.SODate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Sales = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var purchaseData = await _dbContext.PurchaseOrders
            .Where(po => po.PODate >= startDate && po.PODate <= endDate && !po.Isdeleted)
            .GroupBy(po => po.PODate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Purchases = g.Sum(po => po.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        var allDates = Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset => startDate.AddDays(offset))
            .ToList();

        var trend = allDates.Select(date =>
        {
            var sale = salesData.FirstOrDefault(s => s.Date == date);
            var purchase = purchaseData.FirstOrDefault(p => p.Date == date);

            var salesAmount = sale?.Sales ?? 0;
            var purchasesAmount = purchase?.Purchases ?? 0;

            return new SalesTrendDto
            {
                Date = date,
                Sales = salesAmount,
                Purchases = purchasesAmount,
                Profit = salesAmount - purchasesAmount,
                OrderCount = sale?.OrderCount ?? 0
            };
        }).ToList();

        return trend;
    }

    private async Task<List<TopProductDto>> GetTopProductsAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1).AddSeconds(-1);

        var topProducts = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted)
            .SelectMany(so => so.LineItems)
            .Include(sol => sol.Part)
            .GroupBy(sol => new { sol.PartId, sol.Part!.Name, PartNumber = sol.Part.PartNumber.Value, sol.Part.SKU })
            .Select(g => new TopProductDto
            {
                PartId = g.Key.PartId.ToString(),
                PartName = g.Key.Name,
                PartNumber = g.Key.PartNumber,
                Sku = g.Key.SKU,
                QuantitySold = g.Sum(sol => sol.Quantity),
                TotalRevenue = g.Sum(sol => (sol.Quantity * sol.UnitPrice) - (sol.Quantity * sol.Discount)),
                TotalProfit = g.Sum(sol => ((sol.Quantity * sol.UnitPrice) - (sol.Quantity * sol.Discount)) - (sol.Quantity * sol.Part!.CostPrice))
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        return topProducts;
    }

    private async Task<List<TopCustomerDto>> GetTopCustomersAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1).AddSeconds(-1);

        var topCustomers = await _dbContext.SalesOrders
            .Include(so => so.Customer)
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted)
            .GroupBy(so => new
            {
                so.CustomerId,
                CustomerName = so.Customer!.FirstName + " " + so.Customer.LastName,
                so.Customer.Phone,
                so.Customer.CurrentBalance
            })
            .Select(g => new TopCustomerDto
            {
                CustomerId = g.Key.CustomerId.ToString(),
                CustomerName = g.Key.CustomerName,
                Phone = g.Key.Phone,
                TotalOrders = g.Count(),
                TotalRevenue = g.Sum(so => so.TotalAmount),
                OutstandingAmount = g.Key.CurrentBalance,
                LastPurchaseDate = g.Max(so => so.SODate)
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        return topCustomers;
    }
}
