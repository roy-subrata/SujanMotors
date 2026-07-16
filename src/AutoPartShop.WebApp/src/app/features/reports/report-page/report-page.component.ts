import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { CommonModule, formatDate } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { TableModule } from 'primeng/table';
import { SelectModule } from 'primeng/select';
import { DatePickerModule } from 'primeng/datepicker';
import { CheckboxModule } from 'primeng/checkbox';
import { ChartModule } from 'primeng/chart';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';

import { PageContainerComponent } from '../../../shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '../../../shared/components/page-header/page-header.component';
import { FilterBarComponent } from '../../../shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '../../../shared/components/data-pagination/data-pagination.component';
import { HasPermissionDirective } from '../../../shared/directives/has-permission.directive';
import { extractApiError } from '../../../shared/utils/api-error.util';

import { WarehouseService } from '../../inventory/services/warehouse.service';
import { CategoryService } from '../../inventory/services/category.service';
import { BrandService } from '../../inventory/services/brand.service';

import { REPORT_REGISTRY, ReportFilterDef, ReportPageConfig, ReportSelectOption } from '../report-configs';
import { ReportExportFormat, ReportQuery, ReportsService } from '../services/reports.service';

// Rows are shaped by each report's SP; the template indexes them dynamically by
// column config, so they stay `any` (strictTemplates would reject `unknown` in pipes).
type ReportRow = Record<string, any>;

/**
 * Generic report page driven by a ReportPageConfig (resolved from the :reportKey route param):
 * filter bar → optional chart → table (desktop) / cards (mobile) → pagination → xlsx/pdf export.
 * Reports needing a bespoke layout (e.g. P&L statement) get their own component instead.
 */
@Component({
    selector: 'app-report-page',
    standalone: true,
    imports: [
        CommonModule, FormsModule, TableModule, SelectModule, DatePickerModule,
        CheckboxModule, ChartModule, ToastModule, TooltipModule,
        PageContainerComponent, PageHeaderComponent, FilterBarComponent,
        DataPaginationComponent, HasPermissionDirective
    ],
    providers: [MessageService],
    templateUrl: './report-page.component.html',
    styleUrls: ['./report-page.component.scss']
})
export class ReportPageComponent implements OnInit, OnDestroy {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly reportsService = inject(ReportsService);
    private readonly messageService = inject(MessageService);
    private readonly warehouseService = inject(WarehouseService);
    private readonly categoryService = inject(CategoryService);
    private readonly brandService = inject(BrandService);

    config!: ReportPageConfig;

    rows: ReportRow[] = [];
    totals: ReportRow | null = null;
    loading = false;
    exporting: ReportExportFormat | null = null;

    // Filter state
    searchTerm = '';
    dateRange: Date[] | null = null;
    filterValues: Record<string, any> = {};
    lookupOptions: Record<string, ReportSelectOption[]> = {};

    // Pagination (1-based page number; `first` derived for the shared pagination component)
    pageNumber = 1;
    pageSize = 20;
    totalRecords = 0;
    get first(): number { return (this.pageNumber - 1) * this.pageSize; }

    chartData: any = null;
    chartOptions: any = null;

    private routeSub?: Subscription;

    ngOnInit(): void {
        this.routeSub = this.route.paramMap.subscribe(params => {
            const key = params.get('reportKey') ?? '';
            const config = REPORT_REGISTRY.get(key);
            if (!config) {
                this.router.navigate(['/reports']);
                return;
            }
            this.config = config;
            this.resetState();
            this.loadLookups();
            this.loadData();
        });
    }

    ngOnDestroy(): void {
        this.routeSub?.unsubscribe();
    }

    // ── Data loading ────────────────────────────────────────────────────────────

    loadData(): void {
        if (this.config.requiresDateRange && !this.hasCompleteDateRange()) {
            return; // wait until the user completes the range
        }

        this.loading = true;
        const query = this.buildQuery();

        if (this.config.paged) {
            this.reportsService.runPaged(this.config, query).subscribe({
                next: res => {
                    this.rows = res.data ?? [];
                    this.totalRecords = res.pagination?.totalCount ?? this.rows.length;
                    this.totals = (res.totals as ReportRow | undefined) ?? null;
                    this.loading = false;
                },
                error: err => this.onLoadError(err)
            });
        } else {
            this.reportsService.runList(this.config, query).subscribe({
                next: rows => {
                    this.rows = rows ?? [];
                    this.totalRecords = this.rows.length;
                    this.buildChart();
                    this.loading = false;
                },
                error: err => this.onLoadError(err)
            });
        }
    }

    private onLoadError(err: unknown): void {
        this.loading = false;
        this.rows = [];
        this.totalRecords = 0;
        this.messageService.add({
            severity: 'error',
            summary: 'Report failed',
            detail: extractApiError(err, 'Could not load the report. Please try again.')
        });
    }

    private buildQuery(): ReportQuery {
        const query: ReportQuery = {
            search: this.searchTerm?.trim() || undefined,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            ...this.cleanFilterValues()
        };

        if (this.hasCompleteDateRange()) {
            query.fromDate = formatDate(this.dateRange![0], 'yyyy-MM-dd', 'en-US');
            query.toDate = formatDate(this.dateRange![1], 'yyyy-MM-dd', 'en-US');
        }
        return query;
    }

    private cleanFilterValues(): Record<string, unknown> {
        const cleaned: Record<string, unknown> = {};
        for (const [key, value] of Object.entries(this.filterValues)) {
            if (value !== null && value !== undefined && value !== '') cleaned[key] = value;
        }
        return cleaned;
    }

    private hasCompleteDateRange(): boolean {
        return !!this.dateRange && this.dateRange.length === 2 && !!this.dateRange[0] && !!this.dateRange[1];
    }

    // ── Filter events ───────────────────────────────────────────────────────────

    onSearch(): void {
        this.pageNumber = 1;
        this.loadData();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadData();
    }

    onDateRangeSelect(): void {
        // p-datepicker range fires onSelect for each end; only re-query once complete.
        if (this.hasCompleteDateRange()) this.onFilterChange();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.initFilterDefaults();
        this.applyDefaultRange();
        this.onFilterChange();
    }

    hasActiveFilters(): boolean {
        return !!this.searchTerm || Object.values(this.cleanFilterValues()).some(v => v !== false);
    }

    // ── Pagination ──────────────────────────────────────────────────────────────

    goToPage(page: number): void {
        this.pageNumber = page;
        this.loadData();
    }

    onPageSizeChange(size: number): void {
        this.pageSize = size;
        this.pageNumber = 1;
        this.loadData();
    }

    // ── Export ──────────────────────────────────────────────────────────────────

    export(format: ReportExportFormat): void {
        if (this.exporting) return;
        this.exporting = format;

        this.reportsService.export(this.config, this.buildQuery(), format).subscribe({
            next: blob => {
                const url = URL.createObjectURL(blob);
                const anchor = document.createElement('a');
                anchor.href = url;
                anchor.download = `${this.config.key}-${formatDate(new Date(), 'yyyyMMdd', 'en-US')}.${format}`;
                anchor.click();
                URL.revokeObjectURL(url);
                this.exporting = null;
            },
            error: () => {
                this.exporting = null;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Export failed',
                    detail: 'Could not export the report. Please try again.'
                });
            }
        });
    }

    // ── Setup helpers ───────────────────────────────────────────────────────────

    get selectFilters(): ReportFilterDef[] {
        return this.config.filters.filter(f => f.kind === 'select' || f.kind === 'lookup');
    }

    get checkboxFilters(): ReportFilterDef[] {
        return this.config.filters.filter(f => f.kind === 'checkbox');
    }

    get hasDateRange(): boolean {
        return this.config.filters.some(f => f.kind === 'dateRange');
    }

    get hasSearch(): boolean {
        return this.config.filters.some(f => f.kind === 'search');
    }

    get searchPlaceholder(): string {
        return this.config.filters.find(f => f.kind === 'search')?.placeholder ?? 'Search...';
    }

    get mobilePrimaryField(): string {
        return (this.config.columns.find(c => c.mobilePrimary) ?? this.config.columns[0]).field;
    }

    optionsFor(filter: ReportFilterDef): ReportSelectOption[] {
        return filter.kind === 'lookup' ? (this.lookupOptions[filter.key] ?? []) : (filter.options ?? []);
    }

    private resetState(): void {
        this.rows = [];
        this.totals = null;
        this.totalRecords = 0;
        this.pageNumber = 1;
        this.pageSize = 20;
        this.searchTerm = '';
        this.chartData = null;
        this.initFilterDefaults();
        this.applyDefaultRange();
    }

    private initFilterDefaults(): void {
        this.filterValues = {};
        for (const filter of this.config.filters) {
            if (filter.kind === 'dateRange' || filter.kind === 'search') continue;
            this.filterValues[filter.key] = filter.default ?? (filter.kind === 'checkbox' ? false : null);
        }
    }

    private applyDefaultRange(): void {
        const today = new Date();
        switch (this.config.defaultRange) {
            case 'today':
                this.dateRange = [today, today];
                break;
            case 'last7':
                this.dateRange = [new Date(today.getFullYear(), today.getMonth(), today.getDate() - 6), today];
                break;
            case 'thisMonth':
                this.dateRange = [new Date(today.getFullYear(), today.getMonth(), 1), today];
                break;
            case 'lastMonth':
                this.dateRange = [
                    new Date(today.getFullYear(), today.getMonth() - 1, 1),
                    new Date(today.getFullYear(), today.getMonth(), 0)
                ];
                break;
            default:
                this.dateRange = null;
        }
    }

    private loadLookups(): void {
        const lookups = new Set(this.config.filters.filter(f => f.kind === 'lookup').map(f => f.lookup));

        if (lookups.has('warehouse')) {
            this.warehouseService.getAllWarehouses().subscribe(list => {
                this.setLookup('warehouse', list.map(w => ({ label: w.name, value: w.id })));
            });
        }
        if (lookups.has('category')) {
            this.categoryService.getAllCategories().subscribe(list => {
                this.setLookup('category', list.map(c => ({ label: c.name, value: c.id })));
            });
        }
        if (lookups.has('brand')) {
            this.brandService.getBrands({ page: 1, pageSize: 1000 }).subscribe(res => {
                this.setLookup('brand', (res.data ?? []).map(b => ({ label: b.name, value: b.id })));
            });
        }
    }

    private setLookup(lookup: 'warehouse' | 'category' | 'brand', options: ReportSelectOption[]): void {
        for (const filter of this.config.filters) {
            if (filter.kind === 'lookup' && filter.lookup === lookup) {
                this.lookupOptions[filter.key] = options;
            }
        }
    }

    // ── Chart ───────────────────────────────────────────────────────────────────

    private buildChart(): void {
        const chart = this.config.chart;
        if (!chart || this.rows.length === 0) {
            this.chartData = null;
            return;
        }

        const palette = ['#3b82f6', '#22c55e', '#f59e0b', '#8b5cf6', '#ef4444', '#14b8a6'];
        const labels = this.rows.map(row => {
            const raw = row[chart.labelField];
            return chart.labelType === 'date' && raw
                ? formatDate(raw as string, 'dd MMM', 'en-US')
                : String(raw ?? '');
        });

        if (chart.type === 'pie' || chart.type === 'doughnut') {
            const series = chart.series[0];
            this.chartData = {
                labels,
                datasets: [{
                    data: this.rows.map(row => Number(row[series.field] ?? 0)),
                    backgroundColor: this.rows.map((_, i) => palette[i % palette.length])
                }]
            };
        } else {
            this.chartData = {
                labels,
                datasets: chart.series.map((series, i) => ({
                    label: series.label,
                    data: this.rows.map(row => Number(row[series.field] ?? 0)),
                    borderColor: palette[i % palette.length],
                    backgroundColor: chart.type === 'bar' ? palette[i % palette.length] : 'transparent',
                    tension: 0.3
                }))
            };
        }

        this.chartOptions = {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { position: 'bottom' } }
        };
    }
}
