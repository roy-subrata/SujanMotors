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

        // ── Customer outstanding / overdue ───────────────────────────────────────────
        // All-time snapshot — not filtered to the selected period.
        // Each order's outstanding is converted to base currency individually so mixed-currency
        // invoices are summed correctly.
        // Overdue proxy: SalesOrder has no PaymentDueDate field; net-30 is used as the implicit term.
        const int CreditTermDays = 30;
        var today = DateTime.UtcNow.Date;

        var openSalesOrders = await _dbContext.SalesOrders
            .Where(o => o.Status != "CANCELLED" && o.Status != "RETURNED" && !o.Isdeleted)
            .Select(o => new { o.CustomerId, o.TotalAmount, o.TaxAmount, o.PaidAmount, o.Currency, o.SODate })
            .ToListAsync(cancellationToken);

        var customerDueByCustomer = new Dictionary<Guid, decimal>();
        var customerOverdueByCustomer = new Dictionary<Guid, decimal>();

        foreach (var order in openSalesOrders)
        {
            var outstanding = order.TotalAmount + order.TaxAmount - order.PaidAmount;
            if (outstanding <= 0) continue;

            var converted = await _currencyService.ConvertToBaseAsync(outstanding, order.Currency, order.SODate, cancellationToken);
            customerDueByCustomer.TryAdd(order.CustomerId, 0);
            customerDueByCustomer[order.CustomerId] += converted;

            // Past the implicit credit term → overdue
            if (order.SODate.Date <= today.AddDays(-CreditTermDays))
            {
                customerOverdueByCustomer.TryAdd(order.CustomerId, 0);
                customerOverdueByCustomer[order.CustomerId] += converted;
            }
        }

        var customerDuesPositive = customerDueByCustomer.Values.Where(v => v > 0).ToList();
        var customerOverduePositive = customerOverdueByCustomer.Values.Where(v => v > 0).ToList();

        // ── Supplier outstanding / overdue ──────────────────────────────────────────
        // Balance = delivered purchases − completed payments − settled returns, per supplier.
        // Each row is converted to base currency so multi-currency supplier accounts are correct.
        var activePOs = await _dbContext.PurchaseOrders
            .Where(x => x.Status != "DRAFT" && x.Status != "SUBMITTED" && x.Status != "CANCELLED" && !x.Isdeleted)
            .Select(x => new { x.SupplierId, x.TotalAmount, x.Currency, x.PODate, x.ExpectedDeliveryDate, x.Status })
            .ToListAsync(cancellationToken);

        var allSupplierPaymentsData = await _dbContext.SupplierPayments
            .Where(x => x.Status == "COMPLETED" && x.PaymentMethod != "REFUND" &&
                        (x.PaymentType == AutoPartShop.Domain.Entities.PaymentType.ADVANCE || x.SourceAdvancePaymentId == null) &&
                        !x.Isdeleted)
            .Select(x => new { x.SupplierId, x.Amount, x.Currency, x.PaymentDate })
            .ToListAsync(cancellationToken);

        // PurchaseReturn has no Currency field; use the originating PO's currency.
        var allPurchaseReturns = await _dbContext.PurchaseReturns
            .Where(x => x.SettlementStatus == "SETTLED" && !x.Isdeleted && x.PurchaseOrder != null)
            .Select(x => new { x.SupplierId, x.SettledAmount, Currency = x.PurchaseOrder!.Currency, x.SettledDate })
            .ToListAsync(cancellationToken);

        var supplierPOBySupplier = new Dictionary<Guid, decimal>();
        foreach (var po in activePOs)
        {
            var converted = await _currencyService.ConvertToBaseAsync(po.TotalAmount, po.Currency, po.PODate, cancellationToken);
            supplierPOBySupplier.TryAdd(po.SupplierId, 0);
            supplierPOBySupplier[po.SupplierId] += converted;
        }

        var supplierPaymentBySupplier = new Dictionary<Guid, decimal>();
        foreach (var payment in allSupplierPaymentsData)
        {
            var converted = await _currencyService.ConvertToBaseAsync(payment.Amount, payment.Currency, payment.PaymentDate, cancellationToken);
            supplierPaymentBySupplier.TryAdd(payment.SupplierId, 0);
            supplierPaymentBySupplier[payment.SupplierId] += converted;
        }

        var supplierRefundBySupplier = new Dictionary<Guid, decimal>();
        foreach (var refund in allPurchaseReturns)
        {
            var converted = await _currencyService.ConvertToBaseAsync(
                refund.SettledAmount, refund.Currency, refund.SettledDate ?? DateTime.UtcNow, cancellationToken);
            supplierRefundBySupplier.TryAdd(refund.SupplierId, 0);
            supplierRefundBySupplier[refund.SupplierId] += converted;
        }

        var allSupplierIds = supplierPOBySupplier.Keys
            .Union(supplierPaymentBySupplier.Keys)
            .Union(supplierRefundBySupplier.Keys);

        var supplierBalancesPositive = allSupplierIds
            .Select(id => supplierPOBySupplier.GetValueOrDefault(id)
                          - supplierPaymentBySupplier.GetValueOrDefault(id)
                          - supplierRefundBySupplier.GetValueOrDefault(id))
            .Where(b => b > 0)
            .ToList();

        // Overdue: positive-balance suppliers that have at least one delivered PO past its expected delivery date.
        var overdueSupplierIds = activePOs
            .Where(po => po.ExpectedDeliveryDate.Date < today && (po.Status == "DELIVERED" || po.Status == "PARTIAL"))
            .Select(po => po.SupplierId)
            .ToHashSet();

        var supplierOverdueBalancesPositive = allSupplierIds
            .Where(id => overdueSupplierIds.Contains(id))
            .Select(id => supplierPOBySupplier.GetValueOrDefault(id)
                          - supplierPaymentBySupplier.GetValueOrDefault(id)
                          - supplierRefundBySupplier.GetValueOrDefault(id))
            .Where(b => b > 0)
            .ToList();

        // Get inventory value at ACTUAL purchase cost: sum each on-hand lot's qty × its lot cost
        // (not a typed standard cost). Lots carry the real cost paid at goods-receipt time.
        var inventoryValue = await _dbContext.StockLots
            .Where(l => !l.Isdeleted && l.QuantityAvailable > 0)
            .SumAsync(l => l.QuantityAvailable * l.CostPrice, cancellationToken);

        // Get low stock items
        var lowStockItems = await _dbContext.StockLevels
            .Include(sl => sl.Part)
            .Where(sl => !sl.Isdeleted && sl.Part != null && !sl.Part.Isdeleted && sl.QuantityOnHand <= sl.Part.MinimumStock)
            .ToListAsync(cancellationToken);

        // Value the low-stock parts at actual lot cost too (same basis as total inventory value).
        var lowStockPartIds = lowStockItems.Select(sl => sl.PartId).Distinct().ToList();
        var lowStockValue = await _dbContext.StockLots
            .Where(l => !l.Isdeleted && l.QuantityAvailable > 0 && lowStockPartIds.Contains(l.PartId))
            .SumAsync(l => l.QuantityAvailable * l.CostPrice, cancellationToken);

        // Get customer counts
        var totalCustomers = await _dbContext.Customers.CountAsync(c => !c.Isdeleted, cancellationToken);
        var newCustomers = await _dbContext.Customers
            .CountAsync(c => c.CreatedDate >= startDate && c.CreatedDate <= endDate && !c.Isdeleted, cancellationToken);

        // Calculate profit
        var grossProfit = totalSales - totalPurchases;
        var netProfit = grossProfit - totalDailyExpenses;
        // Net margin — matches the Net Profit KPI card so the % is consistent with the value shown
        var profitMargin = totalSales > 0 ? (netProfit / totalSales) * 100 : 0;

        // Cash flow
        var cashInflow = cashSales + customerPayments;   // POS receipts + credit-order collections
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
            // "Revenue Collected" = cash received: POS sales + credit-order collections (not totalSales
            // which includes unpaid credit invoices and would otherwise double-count with customerPayments)
            TotalRevenue = cashSales + customerPayments,

            // Expenses
            TotalPurchases = totalPurchases,
            TotalPurchasesCount = purchaseOrders.Count,
            SupplierPaymentsMade = supplierPayments,
            DailyExpenses = totalDailyExpenses,
            DailyExpensesCount = dailyExpensesCount,
            OtherExpenses = 0,
            // Accrual-basis total: purchase cost + operational expenses.
            // supplierPayments is a cash settlement of PO liabilities — adding it here would double-count
            // any PO received and paid in the same period.
            TotalExpenses = totalPurchases + totalDailyExpenses,

            // Profitability
            GrossProfit = grossProfit,
            NetProfit = netProfit,
            ProfitMargin = profitMargin,

            // Outstanding
            CustomerDueAmount = customerDuesPositive.Sum(),
            CustomerDueCount = customerDuesPositive.Count,
            SupplierDueAmount = supplierBalancesPositive.Sum(),
            SupplierDueCount = supplierBalancesPositive.Count,

            // Overdue (genuinely past due: customer net-30, supplier past ExpectedDeliveryDate)
            CustomerOverdueAmount = customerOverduePositive.Sum(),
            CustomerOverdueCount = customerOverduePositive.Count,
            SupplierOverdueAmount = supplierOverdueBalancesPositive.Sum(),
            SupplierOverdueCount = supplierOverdueBalancesPositive.Count,

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
        // Exclusive upper bound matches the inclusive-end pattern used in GetFinancialSummaryAsync
        // so the chart and KPI cards always cover the same date range.
        var filterEnd = endDate.AddDays(1);

        var salesData = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate < filterEnd && !so.Isdeleted)
            .GroupBy(so => so.SODate.Date)
            .Select(g => new
            {
                Date = g.Key,
                Sales = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var purchaseData = await _dbContext.PurchaseOrders
            .Where(po => po.PODate >= startDate && po.PODate < filterEnd && !po.Isdeleted)
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

        // Load line items with the parent order's currency so revenue can be converted per-row.
        var lineItems = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted)
            .SelectMany(so => so.LineItems
                .Where(li => li.Part != null)
                .Select(li => new
                {
                    li.PartId,
                    PartName = li.Part!.Name,
                    PartNumber = li.Part.PartNumber.Value,
                    Sku = li.Part.SKU,
                    li.Quantity,
                    Revenue = (li.Quantity * li.UnitPrice) - (li.Quantity * li.Discount),
                    so.Currency,
                    so.SODate
                }))
            .ToListAsync(cancellationToken);

        var productAgg = new Dictionary<Guid, (string Name, string PartNumber, string Sku, int Qty, decimal Revenue)>();
        foreach (var li in lineItems)
        {
            var convertedRevenue = await _currencyService.ConvertToBaseAsync(li.Revenue, li.Currency, li.SODate, cancellationToken);
            if (productAgg.TryGetValue(li.PartId, out var agg))
                productAgg[li.PartId] = (agg.Name, agg.PartNumber, agg.Sku, agg.Qty + li.Quantity, agg.Revenue + convertedRevenue);
            else
                productAgg[li.PartId] = (li.PartName, li.PartNumber, li.Sku, li.Quantity, convertedRevenue);
        }

        // COGS per product — SALE lot-movements already recorded in base currency at movement time.
        var cogsByPart = await _dbContext.StockLotMovements
            .Where(m => m.MovementType == "SALE" && m.MovementDate >= startDate && m.MovementDate <= endDate && m.StockLot != null)
            .GroupBy(m => m.StockLot!.PartId)
            .Select(g => new { PartId = g.Key, Cogs = g.Sum(m => m.QuantityInBaseUnit * m.CostAtMovementInBaseUnit) })
            .ToDictionaryAsync(x => x.PartId, x => x.Cogs, cancellationToken);

        return productAgg
            .Select(kvp => new TopProductDto
            {
                PartId = kvp.Key.ToString(),
                PartName = kvp.Value.Name,
                PartNumber = kvp.Value.PartNumber,
                Sku = kvp.Value.Sku,
                QuantitySold = kvp.Value.Qty,
                TotalRevenue = kvp.Value.Revenue,
                TotalProfit = kvp.Value.Revenue - (cogsByPart.TryGetValue(kvp.Key, out var c) ? c : 0)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToList();
    }

    private async Task<List<TopCustomerDto>> GetTopCustomersAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate = request.EndDate.Date.AddDays(1).AddSeconds(-1);

        // Load orders in memory so we can apply per-row currency conversion before grouping.
        var orders = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate <= endDate && !so.Isdeleted && so.Customer != null)
            .Select(so => new
            {
                so.CustomerId,
                CustomerName = so.Customer!.FirstName + " " + so.Customer.LastName,
                Phone = so.Customer.Phone,
                so.TotalAmount, so.TaxAmount, so.PaidAmount,
                so.Currency, so.SODate, so.Status
            })
            .ToListAsync(cancellationToken);

        var customerAgg = new Dictionary<Guid, (string Name, string Phone, int Orders, decimal Revenue, decimal Outstanding, DateTime LastDate)>();

        foreach (var so in orders)
        {
            var revenue = await _currencyService.ConvertToBaseAsync(so.TotalAmount, so.Currency, so.SODate, cancellationToken);

            var rawOutstanding = so.Status != "CANCELLED" && so.Status != "RETURNED"
                ? so.TotalAmount + so.TaxAmount - so.PaidAmount
                : 0;
            var outstanding = rawOutstanding > 0
                ? await _currencyService.ConvertToBaseAsync(rawOutstanding, so.Currency, so.SODate, cancellationToken)
                : 0;

            if (customerAgg.TryGetValue(so.CustomerId, out var agg))
            {
                customerAgg[so.CustomerId] = (agg.Name, agg.Phone,
                    agg.Orders + 1, agg.Revenue + revenue, agg.Outstanding + outstanding,
                    so.SODate > agg.LastDate ? so.SODate : agg.LastDate);
            }
            else
            {
                customerAgg[so.CustomerId] = (so.CustomerName, so.Phone, 1, revenue, outstanding, so.SODate);
            }
        }

        return customerAgg
            .Select(kvp => new TopCustomerDto
            {
                CustomerId = kvp.Key.ToString(),
                CustomerName = kvp.Value.Name,
                Phone = kvp.Value.Phone,
                TotalOrders = kvp.Value.Orders,
                TotalRevenue = kvp.Value.Revenue,
                OutstandingAmount = kvp.Value.Outstanding,
                LastPurchaseDate = kvp.Value.LastDate
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(10)
            .ToList();
    }
}
