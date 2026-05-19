import { Component, OnInit, signal, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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

interface PeriodOption {
  label: string;
  value: 'DAILY' | 'MONTHLY' | 'YEARLY' | 'CUSTOM';
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
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

  // Signals for state management
  loading = signal(false);
  dashboardData = signal<DashboardResponse | null>(null);
  hasError = signal(false);

  // Period selection
  periodOptions: PeriodOption[] = [
    { label: 'Today', value: 'DAILY' },
    { label: 'This Month', value: 'MONTHLY' },
    { label: 'This Year', value: 'YEARLY' },
    { label: 'Custom Range', value: 'CUSTOM' }
  ];

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
          summary: 'Warning',
          detail: 'Please select both start and end dates',
          life: 3000
        });
        this.loading.set(false);
        return;
      }
      // Fix #11: validate that endDate is not before startDate
      if (this.endDate < this.startDate) {
        this.messageService.add({
          severity: 'warn',
          summary: 'Invalid Range',
          detail: 'End date must be on or after start date',
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
          summary: 'Error',
          detail: 'Failed to load dashboard data',
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

  // ─── Net Cash Flow (#12) — moved out of template ─────────────────────────
  get netCashFlow(): number {
    const s = this.dashboardData()?.summary;
    if (!s) return 0;
    return s.cashInflow - s.cashOutflow;
  }

  // ─── Alert helpers (#13) ─────────────────────────────────────────────────
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
    const labels = data.salesTrend.map(t =>
      this.selectedPeriod === 'DAILY'
        ? new Date(t.date).toLocaleTimeString('en-US', { hour: '2-digit', minute: '2-digit' })
        : new Date(t.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })
    );
    const salesData = data.salesTrend.map(t => t.sales);
    const purchasesData = data.salesTrend.map(t => t.purchases);
    const profitData = data.salesTrend.map(t => t.profit);

    this.salesTrendChartData = {
      labels,
      datasets: [
        {
          label: 'Sales',
          data: salesData,
          borderColor: '#42A5F5',
          backgroundColor: 'rgba(66, 165, 245, 0.15)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        },
        {
          label: 'Purchases',
          data: purchasesData,
          borderColor: '#FFA726',
          backgroundColor: 'rgba(255, 167, 38, 0.15)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        },
        {
          label: 'Profit',
          data: profitData,
          borderColor: '#66BB6A',
          backgroundColor: 'rgba(102, 187, 106, 0.15)',
          fill: true,
          tension: 0.4,
          pointRadius: 3
        }
      ]
    };
  }

  private initializeChartOptions(): void {
    // Fix #7 and #8: currency formatting on Y-axis and tooltips
    const fmt = (v: number) => this.formatCurrency(v);

    this.salesTrendChartOptions = {
      maintainAspectRatio: false,
      plugins: {
        legend: {
          labels: { color: '#495057', font: { size: 12 } }
        },
        tooltip: {
          callbacks: {
            label: (ctx: any) => ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}`
          }
        }
      },
      scales: {
        x: {
          ticks: { color: '#64748b', font: { size: 11 } },
          grid: { color: '#f1f5f9' }
        },
        y: {
          ticks: {
            color: '#64748b',
            font: { size: 11 },
            callback: (value: any) => fmt(value)
          },
          grid: { color: '#f1f5f9' }
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
