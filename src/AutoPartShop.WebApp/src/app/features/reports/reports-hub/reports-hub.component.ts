import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ChartModule } from 'primeng/chart';
import { of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { REPORT_GROUPS, ReportGroupInfo } from '../report-configs';
import { DashboardService, FinancialSummaryResponse, TopProductDto } from '../../dashboard/services/dashboard.service';
import { CurrencyService } from '../../../shared/services/currency.service';

/**
 * Reports hub: category cards linking to every configured report page,
 * plus pointers to the report-grade pages that live in other modules, plus a
 * KPI/trend/top-movers snapshot reusing the same DashboardService data the
 * Dashboard page itself is built on (no new backend endpoint for this).
 */
@Component({
    selector: 'app-reports-hub',
    standalone: true,
    imports: [CommonModule, RouterModule, ChartModule, PageContainerComponent, PageHeaderComponent],
    templateUrl: './reports-hub.component.html',
    styleUrls: ['./reports-hub.component.scss']
})
export class ReportsHubComponent implements OnInit {
    private readonly dashboardService = inject(DashboardService);
    private readonly currencyService = inject(CurrencyService);

    readonly groups: ReportGroupInfo[] = REPORT_GROUPS;

    /** Existing report-grade pages elsewhere in the app, surfaced here for discoverability. */
    readonly relatedReports = [
        { title: 'Daily Cash Book', icon: 'pi pi-book', link: '/finance/cash-book' },
        { title: 'Customer Statements', icon: 'pi pi-user', link: '/sales/customer-account-summary' },
        { title: 'Supplier Statements', icon: 'pi pi-truck', link: '/procurement/supplier-account-summary' }
    ];

    // KPI strip + top movers (this month, same default period as the Dashboard)
    kpiSummary = signal<FinancialSummaryResponse | null>(null);
    topMovers = signal<TopProductDto[]>([]);
    kpiLoading = signal(true);

    // 14-day sales-vs-purchases trend chart
    trendChartData: any = null;
    trendChartOptions: any = null;
    trendLoading = signal(true);
    hasTrendData = signal(false);

    get totalReports(): number {
        return this.groups.reduce((n, g) => n + g.reports.length, 0);
    }

    ngOnInit(): void {
        this.initChartOptions();
        this.loadKpiSnapshot();
        this.loadTrend();
    }

    private loadKpiSnapshot(): void {
        const today = new Date();
        const start = new Date(today.getFullYear(), today.getMonth(), 1);
        const end = new Date(today.getFullYear(), today.getMonth() + 1, 0);

        this.dashboardService.getDashboardData({ startDate: start, endDate: end, period: 'MONTHLY' })
            .pipe(catchError(() => of(null)))
            .subscribe(data => {
                this.kpiSummary.set(data?.summary ?? null);
                this.topMovers.set((data?.topProducts ?? []).slice(0, 5));
                this.kpiLoading.set(false);
            });
    }

    private loadTrend(): void {
        const end = new Date();
        const start = new Date();
        start.setDate(end.getDate() - 13);

        this.dashboardService.getDashboardData({ startDate: start, endDate: end, period: 'CUSTOM' })
            .pipe(catchError(() => of(null)))
            .subscribe(data => {
                const trend = data?.salesTrend ?? [];
                this.hasTrendData.set(trend.length > 0);
                this.trendChartData = {
                    labels: trend.map(t => new Date(t.date).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })),
                    datasets: [
                        {
                            label: 'Sales',
                            data: trend.map(t => t.sales),
                            backgroundColor: '#3b82f6'
                        },
                        {
                            label: 'Purchases',
                            data: trend.map(t => t.purchases),
                            backgroundColor: '#f59e0b'
                        }
                    ]
                };
                this.trendLoading.set(false);
            });
    }

    private getThemeColor(varName: string, fallback: string): string {
        const value = getComputedStyle(document.documentElement).getPropertyValue(varName).trim();
        return value || fallback;
    }

    private initChartOptions(): void {
        const textColor = this.getThemeColor('--color-text-secondary', '#8a93a2');
        const gridColor = this.getThemeColor('--color-border', '#eef0f3');
        const fmt = (v: number) => this.formatCurrency(v);

        this.trendChartOptions = {
            maintainAspectRatio: false,
            plugins: {
                legend: { labels: { color: textColor, font: { size: 12 } } },
                tooltip: { callbacks: { label: (ctx: any) => ` ${ctx.dataset.label}: ${fmt(ctx.parsed.y)}` } }
            },
            scales: {
                x: { ticks: { color: textColor, font: { size: 11 } }, grid: { color: gridColor } },
                y: { ticks: { color: textColor, font: { size: 11 }, callback: (v: any) => fmt(v) }, grid: { color: gridColor } }
            }
        };
    }

    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(value, currency);
    }

    formatPercent(value: number): string {
        if (value == null || isNaN(value)) return '0.00%';
        return `${value.toFixed(2)}%`;
    }
}
