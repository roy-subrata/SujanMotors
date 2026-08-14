import { Component, OnInit, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';

// PrimeNG imports
import { CardModule } from 'primeng/card';
import { SelectModule } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { ChartModule } from 'primeng/chart';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { SkeletonModule } from 'primeng/skeleton';
import { TooltipModule } from 'primeng/tooltip';

// Service
import { DashboardService, DashboardResponse, FinancialSummaryRequest } from './services/dashboard.service';
import { CurrencyService } from '../../shared/services/currency.service';
import { MessageService } from 'primeng/api';
import { PurchaseOrderService } from '../procurement/services/purchase-order.service';
import { SalesOrderService } from '../sales/services/sales-order.service';
import { WarrantyService } from '../warranty/services/warranty.service';
import { InvoiceService } from '../sales/services/invoice.service';
import { SalesReturnService } from '../sales/services/sales-return.service';
import { StockTakeService } from '../inventory/services/stock-take.service';
import { StockService, StockLevelResponse } from '../inventory/services/stock.service';
import { I18nService } from '@/shared/services/i18n.service';

interface PeriodOption {
  label: string;
  value: 'DAILY' | 'MONTHLY' | 'YEARLY' | 'CUSTOM';
}

interface WorkQueueItem {
  label: string;
  sub: string;
  count: number;
  icon: string;
  link: string[];
  queryParams?: Record<string, string>;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterModule,
    CardModule,
    SelectModule,
    ButtonModule,
    DatePickerModule,
    ChartModule,
    TableModule,
    TagModule,
    ToastModule,
    SkeletonModule,
    TooltipModule
  ],
  providers: [MessageService],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly purchaseOrderService = inject(PurchaseOrderService);
  private readonly salesOrderService = inject(SalesOrderService);
  private readonly warrantyService = inject(WarrantyService);
  private readonly invoiceService = inject(InvoiceService);
  private readonly salesReturnService = inject(SalesReturnService);
  private readonly stockTakeService = inject(StockTakeService);
  private readonly stockService = inject(StockService);
  readonly i18n = inject(I18nService);

  // Signals for state management
  loading = signal(false);
  dashboardData = signal<DashboardResponse | null>(null);
  hasError = signal(false);

  // Work Queue + Low Stock — point-in-time operational widgets, loaded once,
  // independent of the period selector so they don't jump around as the user
  // changes the KPI date range. Each fails soft/independently (see loadWorkQueue()).
  workQueueItems = signal<WorkQueueItem[]>([]);
  lowStockRows = signal<StockLevelResponse[]>([]);

  // Period selection
  periodOptions = computed<PeriodOption[]>(() => [
    { label: this.i18n.t('dashboard.periods.today'), value: 'DAILY' },
    { label: this.i18n.t('dashboard.periods.thisMonth'), value: 'MONTHLY' },
    { label: this.i18n.t('dashboard.periods.thisYear'), value: 'YEARLY' },
    { label: this.i18n.t('dashboard.periods.customRange'), value: 'CUSTOM' }
  ]);

  selectedPeriod: 'DAILY' | 'MONTHLY' | 'YEARLY' | 'CUSTOM' = 'MONTHLY';
  startDate: Date | null = null;
  endDate: Date | null = null;
  showCustomDateRange = false;

  // Resolved period dates (set on each load for the period info bar)
  resolvedStartDate: Date | null = null;
  resolvedEndDate: Date | null = null;

  // Chart data
  salesTrendChartData: any = null;
  salesTrendChartOptions: any = null;

  ngOnInit(): void {
    this.initializeChartOptions();
    this.loadDashboard();
    this.loadWorkQueue();
    this.loadLowStock();
  }

  /**
   * Six operational counts, each from an existing list endpoint (pageSize:1 where a
   * paginated endpoint exists, reading only the total count — same technique as the
   * Phase 3/4 stat strips). Each call is wrapped in its own catchError so a 403 on
   * one (these are gated by their own domain permission, not the dashboard's
   * reports.view) just drops that item instead of blanking the whole panel.
   */
  private loadWorkQueue(): void {
    forkJoin({
      purchaseOrders: this.purchaseOrderService
        .getPurchaseOrders({ search: '', pageNumber: 1, pageSize: 1, status: 'CONFIRMED' })
        .pipe(catchError(() => of(null))),
      deliveries: this.salesOrderService.getPendingDeliveries().pipe(catchError(() => of(null))),
      warrantyClaims: this.warrantyService.getClaimsByStatus('PENDING').pipe(catchError(() => of(null))),
      invoices: this.invoiceService
        .getAllInvoices(1, 1, { status: 'OVERDUE' })
        .pipe(catchError(() => of(null))),
      salesReturns: this.salesReturnService.getAllSalesReturns().pipe(catchError(() => of(null))),
      stockTakes: this.stockTakeService
        .getStockTakes({ pageNumber: 1, pageSize: 1, status: 'COUNTING' })
        .pipe(catchError(() => of(null)))
    }).subscribe(({ purchaseOrders, deliveries, warrantyClaims, invoices, salesReturns, stockTakes }) => {
      const items: WorkQueueItem[] = [];
      if (purchaseOrders) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.purchaseOrders'), sub: this.i18n.t('dashboard.workQueue.purchaseOrdersSub'), icon: 'pi pi-file',
          count: purchaseOrders.pagination.totalCount, link: ['/procurement/purchase-orders']
        });
      }
      if (deliveries) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.deliveries'), sub: this.i18n.t('dashboard.workQueue.deliveriesSub'), icon: 'pi pi-truck',
          count: deliveries.data.length, link: ['/sales/pending-deliveries']
        });
      }
      if (warrantyClaims) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.warrantyClaims'), sub: this.i18n.t('dashboard.workQueue.warrantyClaimsSub'), icon: 'pi pi-exclamation-triangle',
          count: warrantyClaims.length, link: ['/warranty/claims']
        });
      }
      if (invoices) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.invoicesOverdue'), sub: this.i18n.t('dashboard.workQueue.invoicesOverdueSub'), icon: 'pi pi-file-check',
          count: invoices.pagination.totalCount, link: ['/sales/invoices']
        });
      }
      if (salesReturns) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.salesReturns'), sub: this.i18n.t('dashboard.workQueue.salesReturnsSub'), icon: 'pi pi-replay',
          count: salesReturns.filter(r => r.status === 'PENDING').length, link: ['/sales/sales-returns']
        });
      }
      if (stockTakes) {
        items.push({
          label: this.i18n.t('dashboard.workQueue.stockTakes'), sub: this.i18n.t('dashboard.workQueue.stockTakesSub'), icon: 'pi pi-list-check',
          count: stockTakes.pagination.totalCount, link: ['/inventory/stock-takes']
        });
      }
      this.workQueueItems.set(items);
    });
  }

  private loadLowStock(): void {
    this.stockService
      .getStockLevels({ pageNumber: 1, pageSize: 6, lowStockOnly: true })
      .pipe(catchError(() => of(null)))
      .subscribe(response => {
        this.lowStockRows.set(response?.data ?? []);
      });
  }

  onPeriodChange(): void {
    this.showCustomDateRange = this.selectedPeriod === 'CUSTOM';
    if (this.selectedPeriod !== 'CUSTOM') {
      this.loadDashboard();
    }
  }

  loadDashboard(): void {
    this.loading.set(true);
    this.hasError.set(false);

    let request: FinancialSummaryRequest;

    if (this.selectedPeriod === 'CUSTOM') {
      if (!this.startDate || !this.endDate) {
        this.messageService.add({
          severity: 'warn',
          summary: this.i18n.t('common.messages.warning'),
          detail: this.i18n.t('dashboard.messages.selectDatesWarning'),
          life: 3000
        });
        this.loading.set(false);
        return;
      }
      // Fix #11: validate that endDate is not before startDate
      if (this.endDate < this.startDate) {
        this.messageService.add({
          severity: 'warn',
          summary: this.i18n.t('dashboard.messages.invalidRangeSummary'),
          detail: this.i18n.t('dashboard.messages.invalidRangeDetail'),
          life: 3000
        });
        this.loading.set(false);
        return;
      }
      request = {
        startDate: this.startDate,
        endDate: this.endDate,
        period: 'CUSTOM'
      };
      this.resolvedStartDate = this.startDate;
      this.resolvedEndDate = this.endDate;
    } else {
      const today = new Date();
      let start: Date;
      let end: Date = today;

      switch (this.selectedPeriod) {
        case 'DAILY':
          start = new Date(today.getFullYear(), today.getMonth(), today.getDate());
          end = new Date(today.getFullYear(), today.getMonth(), today.getDate());
          break;
        case 'MONTHLY':
          start = new Date(today.getFullYear(), today.getMonth(), 1);
          end = new Date(today.getFullYear(), today.getMonth() + 1, 0);
          break;
        case 'YEARLY':
          start = new Date(today.getFullYear(), 0, 1);
          end = new Date(today.getFullYear(), 11, 31);
          break;
        default:
          start = today;
      }

      this.resolvedStartDate = start;
      this.resolvedEndDate = end;

      request = {
        startDate: start,
        endDate: end,
        period: this.selectedPeriod
      };
    }

    this.dashboardService.getDashboardData(request).subscribe({
      next: (data) => {
        this.dashboardData.set(data);
        this.updateChartData(data);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Error loading dashboard:', error);
        this.hasError.set(true);
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('dashboard.messages.loadFailed'),
          life: 5000
        });
        this.loading.set(false);
      }
    });
  }

  // ─── Period Label (#3) ────────────────────────────────────────────────────
  getPeriodLabel(): string {
    if (!this.resolvedStartDate || !this.resolvedEndDate) return '';
    const fmt = (d: Date) => d.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
    if (this.selectedPeriod === 'DAILY') return fmt(this.resolvedStartDate);
    return `${fmt(this.resolvedStartDate)} – ${fmt(this.resolvedEndDate)}`;
  }

  // ─── Alert helpers ───────────────────────────────────────────────────────
  get hasAlerts(): boolean {
    const s = this.dashboardData()?.summary;
    if (!s) return false;
    return s.lowStockItemsCount > 0 || s.customerOverdueAmount > 0 || s.supplierOverdueAmount > 0;
  }

  get hasChartData(): boolean {
    return (this.dashboardData()?.salesTrend?.length ?? 0) > 0;
  }

  // ─── Chart ────────────────────────────────────────────────────────────────
  private updateChartData(data: DashboardResponse): void {
    // Backend always groups by day, so use a date label for all periods.
    // (Previously DAILY used a time format which showed "12:00 AM" for the single daily data point.)
    const labels = data.salesTrend.map(t =>
      new Date(t.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    );
    const salesData = data.salesTrend.map(t => t.sales);
    const purchasesData = data.salesTrend.map(t => t.purchases);
    const profitData = data.salesTrend.map(t => t.profit);

    // Design-token chart colors (design_handoff_pos_dashboard/README.md) — same in both themes.
    this.salesTrendChartData = {
      labels,
      datasets: [
        {
          label: this.i18n.t('dashboard.chart.sales'),
          data: salesData,
          borderColor: '#3b82f6',
          backgroundColor: 'rgba(59, 130, 246, 0.09)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        },
        {
          label: this.i18n.t('dashboard.chart.purchases'),
          data: purchasesData,
          borderColor: '#f59e0b',
          backgroundColor: 'rgba(245, 158, 11, 0.07)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        },
        {
          label: this.i18n.t('dashboard.chart.profit'),
          data: profitData,
          borderColor: '#10b981',
          backgroundColor: 'rgba(16, 185, 129, 0.07)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        }
      ]
    };
  }

  // Read theme colors from CSS custom properties so the chart follows light/dark mode
  private getThemeColor(varName: string, fallback: string): string {
    const value = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
    return value || fallback;
  }

  private initializeChartOptions(): void {
    // Fix #7 and #8: currency formatting on Y-axis and tooltips
    const fmt = (v: number) => this.formatCurrency(v);

    // Theme-aware colors from the redesign's own tokens (--text3/--border2), not the
    // old PrimeNG surface vars, so the chart matches the rest of the shell/dashboard.
    const textColor = this.getThemeColor('--text3', '#8a93a2');
    const gridColor = this.getThemeColor('--border2', '#eef0f3');

    this.salesTrendChartOptions = {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: textColor, font: { size: 12 } }
        },
        tooltip: {
          callbacks: {
            label: (ctx: any) => ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`
          }
        }
      },
      scales: {
        x: {
          ticks: { color: textColor, font: { size: 11 } },
          grid: { color: gridColor }
        },
        y: {
          ticks: {
            color: textColor,
            font: { size: 11 },
            callback: (value: any) => fmt(value)
          },
          grid: { color: gridColor }
        }
      }
    };
  }

  // ─── Formatting helpers ───────────────────────────────────────────────────
  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }

  formatPercent(value: number): string {
    if (value == null || isNaN(value)) return '0.00%';
    return `${value.toFixed(2)}%`;
  }

  formatDate(date: Date | string | null | undefined): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
  }

  /** Compact "Jul 2" form used in the Top Customers "Last" column. */
  formatShortDate(date: Date | string | null | undefined): string {
    if (!date) return '—';
    return new Date(date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  getInitials(name: string): string {
    const parts = name.trim().split(/\s+/).filter(Boolean);
    if (parts.length === 0) return '?';
    if (parts.length === 1) return parts[0].charAt(0).toUpperCase();
    return (parts[0].charAt(0) + parts[parts.length - 1].charAt(0)).toUpperCase();
  }

  getChangeColor(value: number): string {
    if (value > 0) return 'success';
    if (value < 0) return 'danger';
    return 'secondary';
  }

  getProfitMarginClass(margin: number): string {
    if (margin >= 20) return 'text-success';
    if (margin >= 5) return 'text-warning';
    return 'text-danger';
  }
}
