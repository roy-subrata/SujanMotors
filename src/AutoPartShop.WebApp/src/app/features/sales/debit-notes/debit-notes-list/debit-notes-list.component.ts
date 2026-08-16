import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { CustomerDebitNoteService, CustomerDebitNoteResponse } from '../../services/customer-debit-note.service';
import { CustomerDebitNoteStatus } from '@/shared/models/status.types';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { StatusPillFilterComponent } from '@/shared/components/status-pill-filter/status-pill-filter.component';
import { MoreFiltersDialogComponent } from '@/shared/components/more-filters-dialog/more-filters-dialog.component';
import { StatusDisplayService, StatusSeverity } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-debit-notes-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent,
        StatusPillFilterComponent,
        MoreFiltersDialogComponent,
        TranslatePipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './debit-notes-list.component.html',
    styleUrls: ['./debit-notes-list.component.scss']
})
export class DebitNotesListComponent implements OnInit {
    private readonly debitNoteService = inject(CustomerDebitNoteService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly statusDisplay = inject(StatusDisplayService);
    private readonly i18n = inject(I18nService);

    @ViewChild('actionMenu') actionMenu!: Menu;

    debitNotes: CustomerDebitNoteResponse[] = [];
    selectedDebitNote: CustomerDebitNoteResponse | null = null;
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;

    filterStatus: CustomerDebitNoteStatus | '' = '';
    /** Getter, not a field: t() resolved at construction would freeze these labels in
     *  whichever language was active then, instead of following the language switcher. */
    get statusOptions(): { label: string; value: string }[] {
        return [
            { label: this.i18n.t('debitNotes.statusOptions.allStatuses'), value: '' },
            { label: this.i18n.t('debitNotes.statusOptions.issued'), value: 'ISSUED' },
            { label: this.i18n.t('debitNotes.statusOptions.settled'), value: 'SETTLED' },
            { label: this.i18n.t('debitNotes.statusOptions.cancelled'), value: 'CANCELLED' }
        ];
    }

    actionMenuItems: MenuItem[] = [];
    moreFiltersVisible = false;

    Math = Math;

    ngOnInit(): void {
        this.loadData();
    }

    onStatusFilterChange(value: string): void {
        this.filterStatus = value as CustomerDebitNoteStatus | '';
        this.onFilterChange();
    }

    private buildActionMenuItems(debitNote: CustomerDebitNoteResponse): void {
        const open = debitNote.status === 'ISSUED';
        this.actionMenuItems = [
            {
                label: this.i18n.t('debitNotes.actions.downloadPdf'),
                icon: 'pi pi-file-pdf',
                command: () => this.downloadPdf(debitNote)
            },
            { separator: true },
            {
                label: this.i18n.t('debitNotes.actions.markSettled'),
                icon: 'pi pi-check-circle',
                command: () => this.settleDebitNote(debitNote),
                visible: open
            },
            {
                label: this.i18n.t('debitNotes.actions.cancel'),
                icon: 'pi pi-times-circle',
                command: () => this.cancelDebitNote(debitNote),
                visible: open,
                styleClass: 'text-orange-600'
            }
        ];
    }

    loadData(): void {
        this.loading = true;

        this.debitNoteService
            .search({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                status: this.filterStatus || undefined
            })
            .subscribe({
                next: (response) => {
                    this.debitNotes = response.data;
                    this.totalRecords = response.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading debit notes:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('debitNotes.messages.loadFailed')
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
        return !!this.filterStatus;
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    clearFilters(): void {
        this.filterStatus = '';
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

    showActionMenu(event: Event, debitNote: CustomerDebitNoteResponse): void {
        this.selectedDebitNote = debitNote;
        this.buildActionMenuItems(debitNote);
        this.actionMenu.toggle(event);
    }

    createDebitNote(): void {
        this.router.navigate(['/sales/debit-notes/create']);
    }

    downloadPdf(debitNote: CustomerDebitNoteResponse): void {
        this.debitNoteService.downloadPdf(debitNote.id, debitNote.debitNoteNumber).subscribe({
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('debitNotes.messages.pdfFailed')
                });
            }
        });
    }

    settleDebitNote(debitNote: CustomerDebitNoteResponse): void {
        this.debitNoteService.settle(debitNote.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('debitNotes.messages.settleSuccess', { number: debitNote.debitNoteNumber })
                });
                this.loadData();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: err?.error?.message ?? this.i18n.t('debitNotes.messages.settleFailed')
                });
            }
        });
    }

    cancelDebitNote(debitNote: CustomerDebitNoteResponse): void {
        const reason = prompt(this.i18n.t('debitNotes.messages.cancelPrompt', { number: debitNote.debitNoteNumber }));
        if (reason === null) return;

        this.debitNoteService.cancel(debitNote.id, reason).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('debitNotes.messages.cancelSuccess', { number: debitNote.debitNoteNumber })
                });
                this.loadData();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: err?.error?.message ?? this.i18n.t('debitNotes.messages.cancelFailed')
                });
            }
        });
    }

    refreshData(): void {
        this.loadData();
    }

    formatDate(date: string): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            day: '2-digit',
            month: 'short',
            year: 'numeric'
        });
    }

    formatCurrency(amount: number, currency?: string): string {
        return this.currencyService.formatCurrency(amount, currency || this.currencyService.selectedCurrency());
    }

    getStatusSeverity(status: string): StatusSeverity {
        return this.statusDisplay.getSeverity(status, 'customer-debit-note');
    }

    formatStatus(status: string): string {
        if (!status) return '-';
        const key = 'debitNotes.statusOptions.' + status.toLowerCase()
            .replace(/_(.)/g, (_m, c: string) => c.toUpperCase());
        const label = this.i18n.t(key);
        if (label !== key) return label;
        return status.split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
