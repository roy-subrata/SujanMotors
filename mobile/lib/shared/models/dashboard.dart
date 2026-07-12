class DashboardSummary {
  const DashboardSummary({
    required this.totalSales,
    required this.totalSalesCount,
    required this.cashSales,
    required this.creditSales,
    required this.totalRevenue,
    required this.totalPurchases,
    required this.totalExpenses,
    required this.grossProfit,
    required this.netProfit,
    required this.profitMargin,
    required this.customerDueAmount,
    required this.customerDueCount,
    required this.customerOverdueAmount,
    required this.customerOverdueCount,
    required this.inventoryValue,
    required this.lowStockItemsCount,
    required this.openingBalance,
    required this.cashInflow,
    required this.cashOutflow,
    required this.closingBalance,
    required this.averageSaleValue,
    required this.totalCustomers,
    required this.newCustomers,
  });

  final double totalSales;
  final int totalSalesCount;
  final double cashSales;
  final double creditSales;
  final double totalRevenue;
  final double totalPurchases;
  final double totalExpenses;
  final double grossProfit;
  final double netProfit;
  final double profitMargin;
  final double customerDueAmount;
  final int customerDueCount;
  final double customerOverdueAmount;
  final int customerOverdueCount;
  final double inventoryValue;
  final int lowStockItemsCount;
  final double openingBalance;
  final double cashInflow;
  final double cashOutflow;
  final double closingBalance;
  final double averageSaleValue;
  final int totalCustomers;
  final int newCustomers;

  factory DashboardSummary.fromJson(Map<String, dynamic> j) => DashboardSummary(
        totalSales: (j['totalSales'] as num?)?.toDouble() ?? 0,
        totalSalesCount: (j['totalSalesCount'] as num?)?.toInt() ?? 0,
        cashSales: (j['cashSales'] as num?)?.toDouble() ?? 0,
        creditSales: (j['creditSales'] as num?)?.toDouble() ?? 0,
        totalRevenue: (j['totalRevenue'] as num?)?.toDouble() ?? 0,
        totalPurchases: (j['totalPurchases'] as num?)?.toDouble() ?? 0,
        totalExpenses: (j['totalExpenses'] as num?)?.toDouble() ?? 0,
        grossProfit: (j['grossProfit'] as num?)?.toDouble() ?? 0,
        netProfit: (j['netProfit'] as num?)?.toDouble() ?? 0,
        profitMargin: (j['profitMargin'] as num?)?.toDouble() ?? 0,
        customerDueAmount: (j['customerDueAmount'] as num?)?.toDouble() ?? 0,
        customerDueCount: (j['customerDueCount'] as num?)?.toInt() ?? 0,
        customerOverdueAmount: (j['customerOverdueAmount'] as num?)?.toDouble() ?? 0,
        customerOverdueCount: (j['customerOverdueCount'] as num?)?.toInt() ?? 0,
        inventoryValue: (j['inventoryValue'] as num?)?.toDouble() ?? 0,
        lowStockItemsCount: (j['lowStockItemsCount'] as num?)?.toInt() ?? 0,
        openingBalance: (j['openingBalance'] as num?)?.toDouble() ?? 0,
        cashInflow: (j['cashInflow'] as num?)?.toDouble() ?? 0,
        cashOutflow: (j['cashOutflow'] as num?)?.toDouble() ?? 0,
        closingBalance: (j['closingBalance'] as num?)?.toDouble() ?? 0,
        averageSaleValue: (j['averageSaleValue'] as num?)?.toDouble() ?? 0,
        totalCustomers: (j['totalCustomers'] as num?)?.toInt() ?? 0,
        newCustomers: (j['newCustomers'] as num?)?.toInt() ?? 0,
      );

  static const empty = DashboardSummary(
    totalSales: 0, totalSalesCount: 0, cashSales: 0, creditSales: 0,
    totalRevenue: 0, totalPurchases: 0, totalExpenses: 0, grossProfit: 0,
    netProfit: 0, profitMargin: 0, customerDueAmount: 0, customerDueCount: 0,
    customerOverdueAmount: 0, customerOverdueCount: 0, inventoryValue: 0,
    lowStockItemsCount: 0, openingBalance: 0, cashInflow: 0, cashOutflow: 0,
    closingBalance: 0, averageSaleValue: 0, totalCustomers: 0, newCustomers: 0,
  );
}

class TopProduct {
  const TopProduct({
    required this.partId,
    required this.partName,
    required this.partNumber,
    required this.quantitySold,
    required this.totalRevenue,
    required this.totalProfit,
  });

  final String partId;
  final String partName;
  final String partNumber;
  final int quantitySold;
  final double totalRevenue;
  final double totalProfit;

  factory TopProduct.fromJson(Map<String, dynamic> j) => TopProduct(
        partId: j['partId'] as String? ?? '',
        partName: j['partName'] as String? ?? '',
        partNumber: j['partNumber'] as String? ?? '',
        quantitySold: (j['quantitySold'] as num?)?.toInt() ?? 0,
        totalRevenue: (j['totalRevenue'] as num?)?.toDouble() ?? 0,
        totalProfit: (j['totalProfit'] as num?)?.toDouble() ?? 0,
      );
}

class TopCustomer {
  const TopCustomer({
    required this.customerId,
    required this.customerName,
    required this.phone,
    required this.totalOrders,
    required this.totalRevenue,
    required this.outstandingAmount,
  });

  final String customerId;
  final String customerName;
  final String phone;
  final int totalOrders;
  final double totalRevenue;
  final double outstandingAmount;

  factory TopCustomer.fromJson(Map<String, dynamic> j) => TopCustomer(
        customerId: j['customerId'] as String? ?? '',
        customerName: j['customerName'] as String? ?? '',
        phone: j['phone'] as String? ?? '',
        totalOrders: (j['totalOrders'] as num?)?.toInt() ?? 0,
        totalRevenue: (j['totalRevenue'] as num?)?.toDouble() ?? 0,
        outstandingAmount: (j['outstandingAmount'] as num?)?.toDouble() ?? 0,
      );
}

class DashboardData {
  const DashboardData({
    required this.summary,
    required this.topProducts,
    required this.topCustomers,
  });

  final DashboardSummary summary;
  final List<TopProduct> topProducts;
  final List<TopCustomer> topCustomers;

  factory DashboardData.fromJson(Map<String, dynamic> j) => DashboardData(
        summary: DashboardSummary.fromJson(
          (j['summary'] as Map?)?.cast<String, dynamic>() ?? {},
        ),
        topProducts: (j['topProducts'] as List?)
                ?.whereType<Map>()
                .map((e) => TopProduct.fromJson(e.cast<String, dynamic>()))
                .toList() ??
            [],
        topCustomers: (j['topCustomers'] as List?)
                ?.whereType<Map>()
                .map((e) => TopCustomer.fromJson(e.cast<String, dynamic>()))
                .toList() ??
            [],
      );
}
