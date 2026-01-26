namespace AutoPartShop.Application.DTOs.DashboardDtos;

/// <summary>
/// Financial summary for a specific period
/// </summary>
public class FinancialSummaryResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = string.Empty; // DAILY, MONTHLY, YEARLY, CUSTOM

    // Revenue Metrics
    public decimal TotalSales { get; set; }
    public int TotalSalesCount { get; set; }
    public decimal CashSales { get; set; }
    public decimal CreditSales { get; set; }
    public decimal CustomerPaymentsReceived { get; set; }
    public decimal TotalRevenue { get; set; }

    // Expense Metrics
    public decimal TotalPurchases { get; set; }
    public int TotalPurchasesCount { get; set; }
    public decimal SupplierPaymentsMade { get; set; }
    public decimal DailyExpenses { get; set; }
    public int DailyExpensesCount { get; set; }
    public decimal OtherExpenses { get; set; }
    public decimal TotalExpenses { get; set; }

    // Profitability
    public decimal GrossProfit { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }

    // Outstanding Balances
    public decimal CustomerDueAmount { get; set; }
    public int CustomerDueCount { get; set; }
    public decimal SupplierDueAmount { get; set; }
    public int SupplierDueCount { get; set; }

    // Overdue Amounts
    public decimal CustomerOverdueAmount { get; set; }
    public int CustomerOverdueCount { get; set; }
    public decimal SupplierOverdueAmount { get; set; }
    public int SupplierOverdueCount { get; set; }

    // Inventory Metrics
    public decimal InventoryValue { get; set; }
    public decimal LowStockValue { get; set; }
    public int LowStockItemsCount { get; set; }

    // Cash Flow
    public decimal OpeningBalance { get; set; }
    public decimal CashInflow { get; set; }
    public decimal CashOutflow { get; set; }
    public decimal ClosingBalance { get; set; }

    // Additional Metrics
    public decimal AverageSaleValue { get; set; }
    public decimal AveragePurchaseValue { get; set; }
    public int TotalCustomers { get; set; }
    public int NewCustomers { get; set; }
    public int TotalSuppliers { get; set; }
    public int ActiveSuppliers { get; set; }
}

/// <summary>
/// Top selling products
/// </summary>
public class TopProductDto
{
    public string PartId { get; set; } = string.Empty;
    public string PartName { get; set; } = string.Empty;
    public string PartNumber { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal TotalProfit { get; set; }
}

/// <summary>
/// Top customers by revenue
/// </summary>
public class TopCustomerDto
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int TotalOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal OutstandingAmount { get; set; }
    public DateTime? LastPurchaseDate { get; set; }
}

/// <summary>
/// Sales trend data for charts
/// </summary>
public class SalesTrendDto
{
    public DateTime Date { get; set; }
    public decimal Sales { get; set; }
    public decimal Purchases { get; set; }
    public decimal Profit { get; set; }
    public int OrderCount { get; set; }
}

/// <summary>
/// Request parameters for financial summary
/// </summary>
public class FinancialSummaryRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Period { get; set; } = "DAILY"; // DAILY, MONTHLY, YEARLY, CUSTOM
}

/// <summary>
/// Complete dashboard response
/// </summary>
public class DashboardResponse
{
    public FinancialSummaryResponse Summary { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<TopCustomerDto> TopCustomers { get; set; } = new();
    public List<SalesTrendDto> SalesTrend { get; set; } = new();
}
