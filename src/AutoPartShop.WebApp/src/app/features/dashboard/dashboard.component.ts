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
    SkeletonModule
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
      request = {
        startDate: this.startDate,
        endDate: this.endDate,
        period: 'CUSTOM'
      };
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

  private updateChartData(data: DashboardResponse): void {
    const labels = data.salesTrend.map(t => new Date(t.date).toLocaleDateString());
    const salesData = data.salesTrend.map(t => t.sales);
    const purchasesData = data.salesTrend.map(t => t.purchases);
    const profitData = data.salesTrend.map(t => t.profit);

    this.salesTrendChartData = {
      labels: labels,
      datasets: [
        {
          label: 'Sales',
          data: salesData,
          borderColor: '#42A5F5',
          backgroundColor: 'rgba(66, 165, 245, 0.2)',
          fill: true,
          tension: 0.4
        },
        {
          label: 'Purchases',
          data: purchasesData,
          borderColor: '#FFA726',
          backgroundColor: 'rgba(255, 167, 38, 0.2)',
          fill: true,
          tension: 0.4
        },
        {
          label: 'Profit',
          data: profitData,
          borderColor: '#66BB6A',
          backgroundColor: 'rgba(102, 187, 106, 0.2)',
          fill: true,
          tension: 0.4
        }
      ]
    };
  }

  private initializeChartOptions(): void {
    this.salesTrendChartOptions = {
      maintainAspectRatio: false,
      aspectRatio: 0.8,
      plugins: {
        legend: {
          labels: {
            color: '#495057'
          }
        }
      },
      scales: {
        x: {
          ticks: {
            color: '#495057'
          },
          grid: {
            color: '#ebedef'
          }
        },
        y: {
          ticks: {
            color: '#495057'
          },
          grid: {
            color: '#ebedef'
          }
        }
      }
    };
  }

  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency() || 'BDT';
    return this.currencyService.formatCurrency(value, currency);
  }

  formatNumber(value: number): string {
    return new Intl.NumberFormat('en-US').format(value);
  }

  formatPercent(value: number): string {
    return `${value.toFixed(2)}%`;
  }

  getChangeColor(value: number): string {
    if (value > 0) return 'success';
    if (value < 0) return 'danger';
    return 'info';
  }
}
