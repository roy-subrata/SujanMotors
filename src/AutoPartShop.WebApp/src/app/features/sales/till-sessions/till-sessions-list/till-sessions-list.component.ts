import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';

import { MessageService } from 'primeng/api';

import { TillSessionService, TillSessionResponse } from '../../services/till-session.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

/**
 * Admin history view of ALL till sessions (every cashier, not just the current user's) — the
 * `current`-session route is where a cashier manages their own open/close lifecycle; this page
 * is a read-only paginated audit trail with a Shift Report PDF download once a session is CLOSED.
 */
@Component({
    selector: 'app-till-sessions-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        Select,
        DatePicker,
        TooltipModule,
        ToastModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent
    ],
    providers: [MessageService],
    templateUrl: './till-sessions-list.component.html',
    styleUrls: ['./till-sessions-list.component.scss']
})
export class TillSessionsListComponent implements OnInit {
    private readonly tillSessionService = inject(TillSessionService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);

    sessions: TillSessionResponse[] = [];
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;

    filterStatus = '';
    dateRange: Date[] | null = null;

    statusOptions: { label: string; value: string }[] = [
        { label: 'All Statuses', value: '' },
        { label: 'Open', value: 'OPEN' },
        { label: 'Closed', value: 'CLOSED' }
    ];

    Math = Math;

    ngOnInit(): void {
        this.loadData();
    }

    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    loadData(): void {
        this.loading = true;

        let fromDate: string | undefined;
        let toDate: string | undefined;
        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            fromDate = this.formatDateForApi(this.dateRange[0]);
            toDate = this.formatDateForApi(this.dateRange[1]);
        }

        this.tillSessionService
            .search({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                status: this.filterStatus || undefined,
                fromDate,
                toDate
            })
            .subscribe({
                next: (response) => {
                    this.sessions = response.data;
                    this.totalRecords = response.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading till sessions:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load till sessions.'
                    });
                    this.loading = false;
                }
            });
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? 10;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.loadData();
    }

    hasActiveFilters(): boolean {
        return !!(this.filterStatus || (this.dateRange && this.dateRange.length > 0));
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    clearFilters(): void {
        this.filterStatus = '';
        this.dateRange = null;
        this.resetPagination();
        this.loadData();
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    goToPage(page: number): void {
        this.first = (page - 1) * this.pageSize;
        this.pageNumber = page;
        this.loadData();
    }

    onPageSizeChange(size: number): void {
        this.pageSize = size;
        this.first = 0;
        this.pageNumber = 1;
        this.loadData();
    }

    refreshData(): void {
        this.loadData();
    }

    downloadPdf(session: TillSessionResponse): void {
        if (session.status !== 'CLOSED') return;
        this.tillSessionService.downloadPdf(session.id, session.terminalLabel, session.openedAt).subscribe({
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to download the shift report PDF.'
                });
            }
        });
    }

    formatDate(date: string | null | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    formatDateTime(date: string | null | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric',
            hour: '2-digit',
            minute: '2-digit'
        });
    }

    formatCurrency(amount: number | null | undefined): string {
        if (amount == null || isNaN(amount)) return '—';
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    overShortClass(session: TillSessionResponse): string {
        if (session.status !== 'CLOSED') return '';
        if (Math.abs(session.overShortAmount) < 0.005) return 'amount-exact';
        return session.overShortAmount > 0 ? 'amount-over' : 'amount-short';
    }

    formatStatus(status: string): string {
        return (status ?? '-').split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
