import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface FinancialSummaryRequest {
  startDate: Date;
  endDate: Date;
  period: 'DAILY' | 'MONTHLY' | 'YEARLY' | 'CUSTOM';
}

export interface FinancialSummaryResponse {
  startDate: Date;
  endDate: Date;
  period: string;

  // Revenue Metrics
  totalSales: number;
  totalSalesCount: number;
  cashSales: number;
  creditSales: number;
  customerPaymentsReceived: number;
  totalRevenue: number;

  // Expense Metrics
  totalPurchases: number;
  totalPurchasesCount: number;
  supplierPaymentsMade: number;
  dailyExpenses: number;
  dailyExpensesCount: number;
  otherExpenses: number;
  totalExpenses: number;

  // Profitability
  grossProfit: number;
  netProfit: number;
  profitMargin: number;

  // Outstanding Balances
  customerDueAmount: number;
  customerDueCount: number;
  supplierDueAmount: number;
  supplierDueCount: number;

  // Overdue Amounts
  customerOverdueAmount: number;
  customerOverdueCount: number;
  supplierOverdueAmount: number;
  supplierOverdueCount: number;

  // Inventory Metrics
  inventoryValue: number;
  lowStockValue: number;
  lowStockItemsCount: number;

  // Cash Flow
  openingBalance: number;
  cashInflow: number;
  cashOutflow: number;
  closingBalance: number;

  // Additional Metrics
  averageSaleValue: number;
  averagePurchaseValue: number;
  totalCustomers: number;
  newCustomers: number;
  totalSuppliers: number;
  activeSuppliers: number;
}

export interface TopProductDto {
  partId: string;
  partName: string;
  partNumber: string;
  sku: string;
  quantitySold: number;
  totalRevenue: number;
  totalProfit: number;
}

export interface TopCustomerDto {
  customerId: string;
  customerName: string;
  phone: string;
  totalOrders: number;
  totalRevenue: number;
  outstandingAmount: number;
  lastPurchaseDate?: Date;
}

export interface SalesTrendDto {
  date: Date;
  sales: number;
  purchases: number;
  profit: number;
  orderCount: number;
}

export interface DashboardResponse {
  summary: FinancialSummaryResponse;
  topProducts: TopProductDto[];
  topCustomers: TopCustomerDto[];
  salesTrend: SalesTrendDto[];
}

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/dashboard';

  /**
   * Get complete dashboard data including summary, trends, and top items
   */
  getDashboardData(request: FinancialSummaryRequest): Observable<DashboardResponse> {
    return this.http.post<DashboardResponse>(`${this.apiUrl}/financial-summary`, request);
  }

  /**
   * Get financial summary only
   */
  getFinancialSummary(request: FinancialSummaryRequest): Observable<FinancialSummaryResponse> {
    return this.http.post<FinancialSummaryResponse>(`${this.apiUrl}/summary`, request);
  }

  /**
   * Get sales trend data for charts
   */
  getSalesTrend(request: FinancialSummaryRequest): Observable<SalesTrendDto[]> {
    return this.http.post<SalesTrendDto[]>(`${this.apiUrl}/sales-trend`, request);
  }

  /**
   * Get today's stats
   */
  getTodayStats(): Observable<FinancialSummaryResponse> {
    return this.http.get<FinancialSummaryResponse>(`${this.apiUrl}/today`);
  }

  /**
   * Get current month stats
   */
  getMonthStats(): Observable<FinancialSummaryResponse> {
    return this.http.get<FinancialSummaryResponse>(`${this.apiUrl}/month`);
  }

  /**
   * Get current year stats
   */
  getYearStats(): Observable<FinancialSummaryResponse> {
    return this.http.get<FinancialSummaryResponse>(`${this.apiUrl}/year`);
  }
}
