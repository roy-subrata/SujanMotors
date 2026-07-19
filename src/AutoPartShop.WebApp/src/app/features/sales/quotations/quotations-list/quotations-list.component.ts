import { Component, OnInit, ViewChild, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { PanelModule } from 'primeng/panel';
import { CardModule } from 'primeng/card';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputGroupModule } from 'primeng/inputgroup';
import { InputGroupAddonModule } from 'primeng/inputgroupaddon';

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { QuotationService, QuotationResponse } from '../../services/quotation.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';

@Component({
    selector: 'app-quotations-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        PanelModule,
        CardModule,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        InputGroupModule,
        InputGroupAddonModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './quotations-list.component.html',
    styleUrls: ['./quotations-list.component.css']
})
export class QuotationsListComponent implements OnInit {
    private readonly quotationService = inject(QuotationService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('actionMenu') actionMenu!: Menu;

    quotations: QuotationResponse[] = [];
    selectedQuotation: QuotationResponse | null = null;
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;

    searchTerm = '';
    filterStatus = '';

    statusOptions: { label: string; value: string }[] = [];

    actionMenuItems: MenuItem[] = [];

    Math = Math;

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
            if (this.selectedQuotation) this.buildActionMenuItems(this.selectedQuotation);
        });

        this.route.queryParams.subscribe((params) => {
            if (params['status']) {
                this.filterStatus = params['status'];
            }
        });

        this.loadData();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('common.status.allStatuses'), value: '' },
            { label: this.i18n.t('common.status.draft'), value: 'DRAFT' },
            { label: 'Sent', value: 'SENT' },
            { label: this.i18n.t('common.status.approved'), value: 'ACCEPTED' },
            { label: this.i18n.t('common.status.rejected'), value: 'REJECTED' },
            { label: 'Converted', value: 'CONVERTED' },
            { label: 'Expired', value: 'EXPIRED' }
        ];
    }

    private buildActionMenuItems(quotation: QuotationResponse): void {
        this.actionMenuItems = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => this.viewQuotation(quotation)
            },
            {
                label: 'Download PDF',
                icon: 'pi pi-file-pdf',
                command: () => this.downloadPdf(quotation)
            },
            { separator: true },
            {
                label: 'Send to Customer',
                icon: 'pi pi-send',
                command: () => this.sendQuotation(quotation),
                visible: quotation.status === 'DRAFT'
            },
            {
                label: this.i18n.t('common.actions.approve'),
                icon: 'pi pi-check-circle',
                command: () => this.acceptQuotation(quotation),
                visible: quotation.status === 'SENT'
            },
            {
                label: this.i18n.t('common.actions.reject'),
                icon: 'pi pi-times-circle',
                command: () => this.rejectQuotation(quotation),
                visible: quotation.status === 'SENT',
                styleClass: 'text-orange-600'
            },
            {
                label: 'Convert to Sales Order',
                icon: 'pi pi-arrow-right-arrow-left',
                command: () => this.convertQuotation(quotation),
                visible: quotation.status === 'ACCEPTED'
            }
        ];
    }

    loadData(): void {
        this.loading = true;

        this.quotationService
            .search({
                pageNumber: this.pageNumber,
                pageSize: this.pageSize,
                search: this.searchTerm || undefined,
                status: this.filterStatus || undefined
            })
            .subscribe({
                next: (response) => {
                    this.quotations = response.data;
                    this.totalRecords = response.totalCount;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading quotations:', err);
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('common.messages.loadFailed')
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
        return !!(this.searchTerm || this.filterStatus);
    }

    onSearch(): void {
        this.resetPagination();
        this.loadData();
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadData();
    }

    clearFilters(): void {
        this.searchTerm = '';
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

    showActionMenu(event: Event, quotation: QuotationResponse): void {
        this.selectedQuotation = quotation;
        this.buildActionMenuItems(quotation);
        this.actionMenu.toggle(event);
    }

    createQuotation(): void {
        this.router.navigate(['/sales/quotations/create']);
    }

    viewQuotation(quotation: QuotationResponse): void {
        this.router.navigate(['/sales/quotations/view'], { queryParams: { id: quotation.id } });
    }

    downloadPdf(quotation: QuotationResponse): void {
        this.quotationService.downloadPdf(quotation.id, quotation.quotationNumber).subscribe({
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: 'Failed to download the quotation PDF'
                });
            }
        });
    }

    sendQuotation(quotation: QuotationResponse): void {
        this.confirmationService.confirm({
            message: `Send quotation ${quotation.quotationNumber} to the customer?`,
            header: 'Send Quotation',
            icon: 'pi pi-send',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.quotationService.send(quotation.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: `Quotation ${quotation.quotationNumber} marked as Sent.`
                        });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: err?.error?.message ?? 'Failed to send quotation.'
                        });
                    }
                });
            }
        });
    }

    acceptQuotation(quotation: QuotationResponse): void {
        this.confirmationService.confirm({
            message: `Mark quotation ${quotation.quotationNumber} as Accepted?`,
            header: 'Accept Quotation',
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.quotationService.accept(quotation.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: `Quotation ${quotation.quotationNumber} accepted.`
                        });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: err?.error?.message ?? 'Failed to accept quotation.'
                        });
                    }
                });
            }
        });
    }

    rejectQuotation(quotation: QuotationResponse): void {
        const reason = prompt(`Reason for rejecting quotation ${quotation.quotationNumber}:`);
        if (reason === null) return;

        this.quotationService.reject(quotation.id, reason).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: `Quotation ${quotation.quotationNumber} rejected.`
                });
                this.loadData();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: err?.error?.message ?? 'Failed to reject quotation.'
                });
            }
        });
    }

    convertQuotation(quotation: QuotationResponse): void {
        this.confirmationService.confirm({
            message: `Convert quotation ${quotation.quotationNumber} into a new Sales Order?`,
            header: 'Convert to Sales Order',
            icon: 'pi pi-arrow-right-arrow-left',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.quotationService.convertToSalesOrder(quotation.id).subscribe({
                    next: (result) => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: `Converted to Sales Order ${result.soNumber}.`
                        });
                        this.loadData();
                    },
                    error: (err) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: err?.error?.message ?? 'Failed to convert quotation.'
                        });
                    }
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

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            DRAFT: 'secondary',
            SENT: 'info',
            ACCEPTED: 'success',
            REJECTED: 'danger',
            CONVERTED: 'contrast',
            EXPIRED: 'warn'
        };
        return map[status] ?? 'secondary';
    }

    formatStatus(status: string): string {
        return (status ?? '-').split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
