using AutoPartShop.Application.DTOs.DashboardDtos;
using AutoPartShop.Domain.Entities;
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

    // Statuses that represent no real economic activity and must be excluded from every metric.
    private static readonly string[] ExcludedSalesStatuses = ["CANCELLED", "RETURNED", "DRAFT"];
    private static readonly string[] ExcludedPOStatuses    = ["DRAFT", "SUBMITTED", "CANCELLED"];

    public FinancialSummaryService(AutoPartDbContext dbContext, ICurrencyConversionService currencyService)
    {
        _dbContext = dbContext;
        _currencyService = currencyService;
    }

    public async Task<DashboardResponse> GetDashboardDataAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var summary     = await GetFinancialSummaryAsync(request, cancellationToken);
        var topProducts = await GetTopProductsAsync(request, cancellationToken);
        var topCustomers = await GetTopCustomersAsync(request, cancellationToken);
        var salesTrend  = await GetSalesTrendAsync(request, cancellationToken);

        return new DashboardResponse
        {
            Summary      = summary,
            TopProducts  = topProducts,
            TopCustomers = topCustomers,
            SalesTrend   = salesTrend
        };
    }

    public async Task<FinancialSummaryResponse> GetFinancialSummaryAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate   = request.EndDate.Date.AddDays(1); // exclusive upper bound — all queries use < endDate

        // ── Sales ─────────────────────────────────────────────────────────────────────
        // Exclude CANCELLED, RETURNED (reversed sales) and DRAFT (uncommitted legacy orders).
        // Uses SO.TotalAmount (pre-tax, post-discount) as the accrual invoice value.
        var salesOrders = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate < endDate
                         && !so.Isdeleted
                         && !ExcludedSalesStatuses.Contains(so.Status))
            .ToListAsync(cancellationToken);

        var totalSales = 0m;
        var cashSales  = 0m;   // accrual: invoiced amount where payment is already PAID
        var creditSales = 0m;  // accrual: invoiced amount still outstanding (PENDING / PARTIAL)

        foreach (var so in salesOrders)
        {
            var converted = await _currencyService.ConvertToBaseAsync(so.TotalAmount, so.Currency, so.SODate, cancellationToken);
            totalSales += converted;

            if (so.PaymentStatus == "PAID")
                cashSales += converted;
            else
                creditSales += converted;
        }

        // ── Customer payments (cash-basis revenue) ────────────────────────────────────
        // Only COMPLETED payments represent cash actually received.
        // Advance-credit re-applications (SourceAdvancePaymentId != null, REGULAR type) are
        // excluded: the original advance deposit is the cash event; its later application to
        // an invoice is an internal ledger transfer, not a new inflow.
        var customerPaymentsList = await _dbContext.CustomerPayments
            .Where(cp => cp.PaymentDate >= startDate && cp.PaymentDate < endDate
                         && !cp.Isdeleted
                         && cp.Status == "COMPLETED"
                         && (cp.PaymentType == CustomerPaymentType.ADVANCE || cp.SourceAdvancePaymentId == null))
            .ToListAsync(cancellationToken);

        var customerPayments = 0m;
        foreach (var cp in customerPaymentsList)
            customerPayments += await _currencyService.ConvertToBaseAsync(cp.Amount, cp.Currency, cp.PaymentDate, cancellationToken);

        // ── Purchase orders ───────────────────────────────────────────────────────────
        // Exclude DRAFT/SUBMITTED (uncommitted) and CANCELLED. Consistent with the
        // supplier ledger so TotalPurchases reflects only committed/received stock cost.
        var purchaseOrders = await _dbContext.PurchaseOrders
            .Where(po => po.PODate >= startDate && po.PODate < endDate
                         && !po.Isdeleted
                         && !ExcludedPOStatuses.Contains(po.Status))
            .ToListAsync(cancellationToken);

        var totalPurchases = 0m;
        foreach (var po in purchaseOrders)
            totalPurchases += await _currencyService.ConvertToBaseAsync(po.TotalAmount, po.Currency, po.PODate, cancellationToken);

        // ── Supplier payments (cash outflow) ─────────────────────────────────────────
        // Only COMPLETED, non-REFUND payments are real cash outflows.
        // Advance re-applications excluded for the same reason as customer payments above.
        var supplierPaymentsList = await _dbContext.SupplierPayments
            .Where(sp => sp.PaymentDate >= startDate && sp.PaymentDate < endDate
                         && !sp.Isdeleted
                         && sp.Status == "COMPLETED"
                         && sp.PaymentMethod != "REFUND"
                         && sp.PaymentMethod != "CREDIT_NOTE" // credit notes are returns, not cash outflows
                         && (sp.PaymentType == PaymentType.ADVANCE || sp.SourceAdvancePaymentId == null))
            .ToListAsync(cancellationToken);

        var supplierPayments = 0m;
        foreach (var sp in supplierPaymentsList)
            supplierPayments += await _currencyService.ConvertToBaseAsync(sp.Amount, sp.Currency, sp.PaymentDate, cancellationToken);

        // ── Daily expenses ────────────────────────────────────────────────────────────
        var dailyExpenses = await _dbContext.DailyExpenses
            .Where(de => de.ExpenseDate >= startDate && de.ExpenseDate < endDate && !de.Isdeleted)
            .ToListAsync(cancellationToken);

        var totalDailyExpenses = 0m;
        foreach (var de in dailyExpenses)
            totalDailyExpenses += await _currencyService.ConvertToBaseAsync(de.Amount, de.Currency, de.ExpenseDate, cancellationToken);

        var dailyExpensesCount = dailyExpenses.Count;

        // ── Customer outstanding / overdue (all-time snapshot) ────────────────────────
        // Not filtered to the selected period — always shows current real exposure.
        // outstanding = GrandTotal − PaidAmount  (GrandTotal = TotalAmount + TaxAmount).
        // Overdue proxy: net-30 from order date (no PaymentDueDate field on SalesOrder).
        const int CreditTermDays = 30;
        var today = DateTime.UtcNow.Date;

        var openSalesOrders = await _dbContext.SalesOrders
            .Where(o => !o.Isdeleted
                        && o.Status != "CANCELLED"
                        && o.Status != "RETURNED"
                        && o.Status != "DRAFT")
            .Select(o => new { o.CustomerId, o.TotalAmount, o.TaxAmount, o.PaidAmount, o.Currency, o.SODate })
            .ToListAsync(cancellationToken);

        var customerDueByCustomer      = new Dictionary<Guid, decimal>();
        var customerOverdueByCustomer  = new Dictionary<Guid, decimal>();

        foreach (var order in openSalesOrders)
        {
            var outstanding = order.TotalAmount + order.TaxAmount - order.PaidAmount;
            if (outstanding <= 0) continue;

            var converted = await _currencyService.ConvertToBaseAsync(outstanding, order.Currency, order.SODate, cancellationToken);

            customerDueByCustomer.TryAdd(order.CustomerId, 0);
            customerDueByCustomer[order.CustomerId] += converted;

            if (order.SODate.Date <= today.AddDays(-CreditTermDays))
            {
                customerOverdueByCustomer.TryAdd(order.CustomerId, 0);
                customerOverdueByCustomer[order.CustomerId] += converted;
            }
        }

        var customerDuesPositive     = customerDueByCustomer.Values.Where(v => v > 0).ToList();
        var customerOverduePositive  = customerOverdueByCustomer.Values.Where(v => v > 0).ToList();

        // ── Supplier outstanding / overdue (all-time snapshot) ────────────────────────
        // Balance per supplier = Σ active PO amounts − Σ completed payments − Σ settled returns.
        // All amounts converted to base currency individually for correct multi-currency handling.
        var activePOs = await _dbContext.PurchaseOrders
            .Where(x => !x.Isdeleted && !ExcludedPOStatuses.Contains(x.Status))
            .Select(x => new { x.SupplierId, x.TotalAmount, x.Currency, x.PODate, x.ExpectedDeliveryDate, x.Status })
            .ToListAsync(cancellationToken);

        var allSupplierPaymentsData = await _dbContext.SupplierPayments
            .Where(x => !x.Isdeleted
                        && x.Status == "COMPLETED"
                        && x.PaymentMethod != "REFUND"
                        && x.PaymentMethod != "CREDIT_NOTE" // already counted in allPurchaseReturns; including here would double-reduce the balance
                        && (x.PaymentType == PaymentType.ADVANCE || x.SourceAdvancePaymentId == null))
            .Select(x => new { x.SupplierId, x.Amount, x.Currency, x.PaymentDate })
            .ToListAsync(cancellationToken);

        // PurchaseReturn carries no Currency; use the originating PO's currency.
        var allPurchaseReturns = await _dbContext.PurchaseReturns
            .Where(x => !x.Isdeleted && x.SettlementStatus == "SETTLED" && x.PurchaseOrder != null)
            .Select(x => new { x.SupplierId, x.SettledAmount, Currency = x.PurchaseOrder!.Currency, x.SettledDate, PODate = x.PurchaseOrder.PODate })
            .ToListAsync(cancellationToken);

        var supplierPOBySupplier      = new Dictionary<Guid, decimal>();
        var supplierPaymentBySupplier = new Dictionary<Guid, decimal>();
        var supplierRefundBySupplier  = new Dictionary<Guid, decimal>();

        foreach (var po in activePOs)
        {
            var converted = await _currencyService.ConvertToBaseAsync(po.TotalAmount, po.Currency, po.PODate, cancellationToken);
            supplierPOBySupplier.TryAdd(po.SupplierId, 0);
            supplierPOBySupplier[po.SupplierId] += converted;
        }

        foreach (var payment in allSupplierPaymentsData)
        {
            var converted = await _currencyService.ConvertToBaseAsync(payment.Amount, payment.Currency, payment.PaymentDate, cancellationToken);
            supplierPaymentBySupplier.TryAdd(payment.SupplierId, 0);
            supplierPaymentBySupplier[payment.SupplierId] += converted;
        }

        foreach (var refund in allPurchaseReturns)
        {
            // Use SettledDate for conversion rate accuracy; fall back to PO date (not UtcNow)
            // so a null SettledDate uses the original purchase rate rather than today's rate.
            var rateDate = refund.SettledDate ?? refund.PODate;
            var converted = await _currencyService.ConvertToBaseAsync(refund.SettledAmount, refund.Currency, rateDate, cancellationToken);
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

        // Overdue: supplier has a positive balance AND at least one DELIVERED/PARTIAL PO that is
        // past its expected delivery date. Guard DateTime.MinValue — non-nullable DateTime default
        // when a PO was saved without an expected delivery date.
        var overdueSupplierIds = activePOs
            .Where(po => po.ExpectedDeliveryDate != DateTime.MinValue
                         && po.ExpectedDeliveryDate.Date < today
                         && (po.Status == "DELIVERED" || po.Status == "PARTIAL"))
            .Select(po => po.SupplierId)
            .ToHashSet();

        var supplierOverdueBalancesPositive = allSupplierIds
            .Where(id => overdueSupplierIds.Contains(id))
            .Select(id => supplierPOBySupplier.GetValueOrDefault(id)
                          - supplierPaymentBySupplier.GetValueOrDefault(id)
                          - supplierRefundBySupplier.GetValueOrDefault(id))
            .Where(b => b > 0)
            .ToList();

        // ── Inventory ─────────────────────────────────────────────────────────────────
        // CostPrice on StockLot = actual purchase cost stored in base currency at goods-receipt time.
        // Only AVAILABLE lots are sellable; DAMAGED and QUARANTINE are held for return and excluded.
        var inventoryValue = await _dbContext.StockLots
            .Where(l => !l.Isdeleted && l.QuantityAvailable > 0 && l.Status == "AVAILABLE")
            .SumAsync(l => l.QuantityAvailable * l.CostPrice, cancellationToken);

        // Low-stock: quantity at or below the configured minimum threshold.
        // MinimumStock = 0 means "no threshold set" — skip those parts to avoid noise.
        var lowStockItems = await _dbContext.StockLevels
            .Include(sl => sl.Part)
            .Where(sl => !sl.Isdeleted
                         && sl.Part != null
                         && !sl.Part.Isdeleted
                         && sl.Part.MinimumStock > 0
                         && sl.QuantityOnHand <= sl.Part.MinimumStock)
            .ToListAsync(cancellationToken);

        var lowStockPartIds = lowStockItems.Select(sl => sl.PartId).Distinct().ToList();

        var lowStockValue = lowStockPartIds.Count > 0
            ? await _dbContext.StockLots
                .Where(l => !l.Isdeleted && l.QuantityAvailable > 0 && l.Status == "AVAILABLE" && lowStockPartIds.Contains(l.PartId))
                .SumAsync(l => l.QuantityAvailable * l.CostPrice, cancellationToken)
            : 0m;

        // ── Customers ────────────────────────────────────────────────────────────────
        var totalCustomers = await _dbContext.Customers.CountAsync(c => !c.Isdeleted, cancellationToken);
        var newCustomers   = await _dbContext.Customers
            .CountAsync(c => !c.Isdeleted && c.CreatedDate >= startDate && c.CreatedDate < endDate, cancellationToken);

        // ── Profitability (accrual basis) ─────────────────────────────────────────────
        var grossProfit  = totalSales - totalPurchases;
        var netProfit    = grossProfit - totalDailyExpenses;
        var profitMargin = totalSales > 0 ? (netProfit / totalSales) * 100 : 0m;

        // ── Cash flow (cash basis) ────────────────────────────────────────────────────
        // Inflow = all COMPLETED CustomerPayments. Every receipt — POS cash, card, credit
        // collection — creates a CustomerPayment record, so this is the single source of
        // truth. Do NOT add cashSales here: CASH POS payments are already COMPLETED
        // CustomerPayments, so cashSales + customerPayments would double-count them.
        // cashSales / creditSales are accrual-basis breakdowns of TotalSales, not cash.
        var cashInflow  = customerPayments;
        var cashOutflow = supplierPayments + totalDailyExpenses;

        return new FinancialSummaryResponse
        {
            StartDate = startDate,
            EndDate   = request.EndDate.Date, // inclusive end — NOT the exclusive upper-bound variable
            Period    = request.Period,

            // Revenue (accrual)
            TotalSales              = totalSales,
            TotalSalesCount         = salesOrders.Count,
            CashSales               = cashSales,
            CreditSales             = creditSales,
            CustomerPaymentsReceived = customerPayments,
            TotalRevenue            = customerPayments, // cash-basis: all COMPLETED CustomerPayments

            // Expenses (accrual)
            TotalPurchases      = totalPurchases,
            TotalPurchasesCount = purchaseOrders.Count,
            SupplierPaymentsMade = supplierPayments,
            DailyExpenses       = totalDailyExpenses,
            DailyExpensesCount  = dailyExpensesCount,
            OtherExpenses       = 0,
            TotalExpenses       = totalPurchases + totalDailyExpenses, // accrual; supplierPayments excluded to avoid double-count

            // Profitability
            GrossProfit  = grossProfit,
            NetProfit    = netProfit,
            ProfitMargin = profitMargin,

            // Outstanding (all-time)
            CustomerDueAmount  = customerDuesPositive.Sum(),
            CustomerDueCount   = customerDuesPositive.Count,
            SupplierDueAmount  = supplierBalancesPositive.Sum(),
            SupplierDueCount   = supplierBalancesPositive.Count,

            // Overdue (all-time)
            CustomerOverdueAmount  = customerOverduePositive.Sum(),
            CustomerOverdueCount   = customerOverduePositive.Count,
            SupplierOverdueAmount  = supplierOverdueBalancesPositive.Sum(),
            SupplierOverdueCount   = supplierOverdueBalancesPositive.Count,

            // Inventory (current snapshot)
            InventoryValue    = inventoryValue,
            LowStockValue     = lowStockValue,
            LowStockItemsCount = lowStockPartIds.Count, // distinct parts, not warehouse rows

            // Cash flow
            OpeningBalance  = 0, // requires a separate balance-ledger; tracked in CashBookController
            CashInflow      = cashInflow,
            CashOutflow     = cashOutflow,
            ClosingBalance  = cashInflow - cashOutflow,

            // Averages / counts
            AverageSaleValue     = salesOrders.Count > 0 ? totalSales / salesOrders.Count : 0m,
            AveragePurchaseValue = purchaseOrders.Count > 0 ? totalPurchases / purchaseOrders.Count : 0m,
            TotalCustomers       = totalCustomers,
            NewCustomers         = newCustomers,
            TotalSuppliers       = await _dbContext.Suppliers.CountAsync(s => !s.Isdeleted, cancellationToken),
            ActiveSuppliers      = await _dbContext.Suppliers.CountAsync(s => !s.Isdeleted && s.IsActive, cancellationToken)
        };
    }

    public async Task<List<SalesTrendDto>> GetSalesTrendAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate   = request.EndDate.Date;
        var filterEnd = endDate.AddDays(1); // exclusive upper bound

        // Groups by calendar date in SQL so the result is already aggregated.
        // Currency conversion is intentionally omitted here (SQL-side grouping prevents
        // per-row conversion); the chart is informational and assumes base currency.
        var salesData = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate < filterEnd
                         && !so.Isdeleted
                         && !ExcludedSalesStatuses.Contains(so.Status))
            .GroupBy(so => so.SODate.Date)
            .Select(g => new
            {
                Date       = g.Key,
                Sales      = g.Sum(so => so.TotalAmount),
                OrderCount = g.Count()
            })
            .ToListAsync(cancellationToken);

        var purchaseData = await _dbContext.PurchaseOrders
            .Where(po => po.PODate >= startDate && po.PODate < filterEnd
                         && !po.Isdeleted
                         && !ExcludedPOStatuses.Contains(po.Status))
            .GroupBy(po => po.PODate.Date)
            .Select(g => new
            {
                Date      = g.Key,
                Purchases = g.Sum(po => po.TotalAmount)
            })
            .ToListAsync(cancellationToken);

        // Produce a zero-filled entry for every day in the range so the chart has no gaps.
        return Enumerable.Range(0, (endDate - startDate).Days + 1)
            .Select(offset =>
            {
                var date      = startDate.AddDays(offset);
                var sale      = salesData.FirstOrDefault(s => s.Date == date);
                var purchase  = purchaseData.FirstOrDefault(p => p.Date == date);
                var salesAmt  = sale?.Sales ?? 0m;
                var purchAmt  = purchase?.Purchases ?? 0m;

                return new SalesTrendDto
                {
                    Date       = date,
                    Sales      = salesAmt,
                    Purchases  = purchAmt,
                    Profit     = salesAmt - purchAmt,
                    OrderCount = sale?.OrderCount ?? 0
                };
            })
            .ToList();
    }

    private async Task<List<TopProductDto>> GetTopProductsAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate   = request.EndDate.Date.AddDays(1); // exclusive upper bound

        // Revenue = (Quantity × UnitPrice) − (Quantity × Discount), where Discount is per-unit flat amount.
        var lineItems = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate < endDate
                         && !so.Isdeleted
                         && !ExcludedSalesStatuses.Contains(so.Status))
            .SelectMany(so => so.LineItems
                .Where(li => li.Part != null)
                .Select(li => new
                {
                    li.PartId,
                    PartName   = li.Part!.Name,
                    PartNumber = li.Part.PartNumber.Value,
                    Sku        = li.Part.SKU,
                    li.Quantity,
                    Revenue    = (li.Quantity * li.UnitPrice) - (li.Quantity * li.Discount),
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

        // COGS from lot movements — already in base currency at movement time.
        var cogsByPart = await _dbContext.StockLotMovements
            .Where(m => m.MovementType == "SALE"
                        && m.MovementDate >= startDate && m.MovementDate < endDate
                        && m.StockLot != null)
            .GroupBy(m => m.StockLot!.PartId)
            .Select(g => new { PartId = g.Key, Cogs = g.Sum(m => m.QuantityInBaseUnit * m.CostAtMovementInBaseUnit) })
            .ToDictionaryAsync(x => x.PartId, x => x.Cogs, cancellationToken);

        return productAgg
            .Select(kvp => new TopProductDto
            {
                PartId       = kvp.Key.ToString(),
                PartName     = kvp.Value.Name,
                PartNumber   = kvp.Value.PartNumber,
                Sku          = kvp.Value.Sku,
                QuantitySold = kvp.Value.Qty,
                TotalRevenue = kvp.Value.Revenue,
                TotalProfit  = kvp.Value.Revenue - (cogsByPart.TryGetValue(kvp.Key, out var c) ? c : 0)
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(10)
            .ToList();
    }

    private async Task<List<TopCustomerDto>> GetTopCustomersAsync(FinancialSummaryRequest request, CancellationToken cancellationToken = default)
    {
        var startDate = request.StartDate.Date;
        var endDate   = request.EndDate.Date.AddDays(1); // exclusive upper bound

        var orders = await _dbContext.SalesOrders
            .Where(so => so.SODate >= startDate && so.SODate < endDate
                         && !so.Isdeleted
                         && so.Customer != null
                         && !ExcludedSalesStatuses.Contains(so.Status))
            .Select(so => new
            {
                so.CustomerId,
                CustomerName = so.Customer!.FirstName + " " + so.Customer.LastName,
                Phone        = so.Customer.Phone,
                so.TotalAmount, so.TaxAmount, so.PaidAmount,
                so.Currency, so.SODate
            })
            .ToListAsync(cancellationToken);

        var customerAgg = new Dictionary<Guid, (string Name, string Phone, int Orders, decimal Revenue, decimal Outstanding, DateTime LastDate)>();

        foreach (var so in orders)
        {
            var grandTotal  = so.TotalAmount + so.TaxAmount;
            var revenue     = await _currencyService.ConvertToBaseAsync(grandTotal, so.Currency, so.SODate, cancellationToken);
            var rawDue      = grandTotal - so.PaidAmount;
            var outstanding = rawDue > 0
                ? await _currencyService.ConvertToBaseAsync(rawDue, so.Currency, so.SODate, cancellationToken)
                : 0m;

            if (customerAgg.TryGetValue(so.CustomerId, out var agg))
            {
                customerAgg[so.CustomerId] = (agg.Name, agg.Phone,
                    agg.Orders + 1,
                    agg.Revenue + revenue,
                    agg.Outstanding + outstanding,
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
                CustomerId       = kvp.Key.ToString(),
                CustomerName     = kvp.Value.Name,
                Phone            = kvp.Value.Phone,
                TotalOrders      = kvp.Value.Orders,
                TotalRevenue     = kvp.Value.Revenue,
                OutstandingAmount = kvp.Value.Outstanding,
                LastPurchaseDate = kvp.Value.LastDate
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(10)
            .ToList();
    }
}
