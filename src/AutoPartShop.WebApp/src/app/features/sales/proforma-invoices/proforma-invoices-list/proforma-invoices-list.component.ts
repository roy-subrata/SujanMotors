import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

import { ProformaInvoiceService, ProformaInvoiceResponse } from '../../services/proforma-invoice.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { GenerateProformaDialogComponent } from '../generate-proforma-dialog/generate-proforma-dialog.component';

@Component({
    selector: 'app-proforma-invoices-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        PageContainerComponent,
        PageHeaderComponent,
        DataPaginationComponent,
        GenerateProformaDialogComponent
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './proforma-invoices-list.component.html',
    styleUrls: ['./proforma-invoices-list.component.scss']
})
export class ProformaInvoicesListComponent implements OnInit {
    private readonly proformaInvoiceService = inject(ProformaInvoiceService);
    private readonly currencyService = inject(CurrencyService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);

    @ViewChild('actionMenu') actionMenu!: Menu;

    proformas: ProformaInvoiceResponse[] = [];
    selectedProforma: ProformaInvoiceResponse | null = null;
    loading = false;

    totalRecords = 0;
    pageNumber = 1;
    pageSize = 10;
    first = 0;

    actionMenuItems: MenuItem[] = [];

    // "Generate Proforma" dialog — no pre-selected sales order when opened from this page.
    generateDialogVisible = false;

    Math = Math;

    ngOnInit(): void {
        this.loadData();
    }

    private buildActionMenuItems(proforma: ProformaInvoiceResponse): void {
        this.actionMenuItems = [
            {
                label: 'Download PDF',
                icon: 'pi pi-file-pdf',
                command: () => this.downloadPdf(proforma)
            },
            {
                label: 'View Sales Order',
                icon: 'pi pi-external-link',
                command: () => this.viewSalesOrder(proforma)
            }
        ];
    }

    loadData(): void {
        this.loading = true;

        this.proformaInvoiceService.list(this.pageNumber, this.pageSize).subscribe({
            next: (response) => {
                this.proformas = response.data;
                this.totalRecords = response.totalCount;
                this.loading = false;
            },
            error: (err) => {
                console.error('Error loading proforma invoices:', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load proforma invoices.'
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

    showActionMenu(event: Event, proforma: ProformaInvoiceResponse): void {
        this.selectedProforma = proforma;
        this.buildActionMenuItems(proforma);
        this.actionMenu.toggle(event);
    }

    openGenerateDialog(): void {
        this.generateDialogVisible = true;
    }

    onProformaGenerated(_proforma: ProformaInvoiceResponse): void {
        this.resetPagination();
        this.loadData();
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    downloadPdf(proforma: ProformaInvoiceResponse): void {
        this.proformaInvoiceService.downloadPdf(proforma.id, proforma.proformaNumber).subscribe({
            error: () => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to download the proforma invoice PDF'
                });
            }
        });
    }

    viewSalesOrder(proforma: ProformaInvoiceResponse): void {
        this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id: proforma.salesOrderId } });
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

    formatCurrency(amount: number): string {
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        const map: Record<string, 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'> = {
            ISSUED: 'success',
            EXPIRED: 'warn',
            SUPERSEDED: 'secondary'
        };
        return map[status] ?? 'secondary';
    }

    formatStatus(status: string): string {
        return (status ?? '-').split('_')
            .map(w => w.charAt(0) + w.slice(1).toLowerCase()).join(' ');
    }
}
