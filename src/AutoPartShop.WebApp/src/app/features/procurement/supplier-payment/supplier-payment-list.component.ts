import { Component, OnInit, inject, ViewChild, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { SupplierPaymentService, SupplierPaymentResponse, PaginatedSupplierPaymentResponse, SupplierPaymentQuery } from '../services/supplier-payment.service';
import { StatusBadgeComponent } from '../components/status-badge.component';
import { CurrencyService } from '../../../shared/services/currency.service';
import { SupplierService } from '../../inventory/services/supplier.service';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
    selector: 'app-supplier-payment-list',
    standalone: true,
    imports: [CommonModule, FormsModule, RouterModule, ButtonModule, TableModule, InputTextModule,
              ToastModule, ConfirmDialogModule, TagModule, ContextMenuModule, Select, DatePicker,
              TooltipModule, StatusBadgeComponent],
    providers: [MessageService, ConfirmationService],
    templateUrl: './supplier-payment-list.component.html',
    styleUrls: ['./supplier-payment-list.component.css']
})
export class SupplierPaymentListComponent implements OnInit {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    private readonly supplierPaymentService = inject(SupplierPaymentService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly currencyService = inject(CurrencyService);
    private readonly supplierService = inject(SupplierService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    supplierPayments: SupplierPaymentResponse[] = [];
    searchTerm: string = '';
    pageNumber: number = 1;
    pageSize: number = 10;
    first: number = 0;
    totalCount: number = 0;
    loading: boolean = false;
    contextMenuItems: MenuItem[] = [];
    selectedPayment: SupplierPaymentResponse | null = null;
    filterStatus: string | null = null;
    dateRange: Date[] = [];
    sortField: string | null = null;
    sortOrder: number | null = null;
    pageSizeOptions = [10, 25, 50, 100];
    pageSizeSelectOptions = this.pageSizeOptions.map((size) => ({ label: size.toString(), value: size }));
    Math = Math;
    supplierFilter: string | null = null;
    supplierFilterName: string = '';

    statusOptions: { label: string; value: string | null }[] = [];

    ngOnInit(): void {
        this.buildStatusOptions();
        this.rebuildContextMenu();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
            if (this.selectedPayment) this.rebuildContextMenu();
        });

        const supplierId = this.route.snapshot.queryParamMap.get('supplierId');
        if (supplierId) {
            this.supplierFilter = supplierId;
            this.loadSupplierName(supplierId);
        }

        this.loadSupplierPayments();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('supplierPayments.statusOptions.all'),        value: null },
            { label: this.i18n.t('supplierPayments.statusOptions.pending'),     value: 'PENDING' },
            { label: this.i18n.t('supplierPayments.statusOptions.completed'),   value: 'COMPLETED' },
            { label: this.i18n.t('supplierPayments.statusOptions.processing'),  value: 'PROCESSING' },
            { label: this.i18n.t('supplierPayments.statusOptions.failed'),      value: 'FAILED' },
            { label: this.i18n.t('supplierPayments.statusOptions.cancelled'),   value: 'CANCELLED' }
        ];
    }

    private rebuildContextMenu(): void {
        const payment = this.selectedPayment;
        this.contextMenuItems = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => { if (payment) this.viewDetails(payment); }
            },
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => { if (payment) this.edit(payment); },
                visible: payment ? payment.status !== 'CONFIRMED' && payment.status !== 'RECONCILED' : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.markAsAdvance'),
                icon: 'pi pi-arrow-up',
                command: () => { if (payment) this.markAsAdvance(payment); },
                visible: payment ? payment.paymentType !== 'ADVANCE' : false
            },
            {
                label: this.i18n.t('common.actions.markAsRegular'),
                icon: 'pi pi-arrow-down',
                command: () => { if (payment) this.markAsRegular(payment); },
                visible: payment ? payment.paymentType === 'ADVANCE' : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.confirmPayment'),
                icon: 'pi pi-check-circle',
                command: () => { if (payment) this.confirmPayment(payment); },
                visible: payment ? (payment.status === 'PENDING' || payment.status === 'PROCESSING') : false
            },
            {
                label: this.i18n.t('common.actions.reconcile'),
                icon: 'pi pi-sync',
                command: () => { if (payment) this.reconcilePayment(payment); },
                visible: payment ? payment.status === 'CONFIRMED' && !payment.isReconciled : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.cancel'),
                icon: 'pi pi-times',
                command: () => { if (payment) this.cancelPayment(payment); },
                visible: payment ? payment.status !== 'CONFIRMED' && payment.status !== 'RECONCILED' : false
            },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => { if (payment) this.deletePayment(payment); },
                visible: payment ? payment.status !== 'CONFIRMED' && payment.status !== 'RECONCILED' : false
            }
        ];
    }

    showContextMenu(event: MouseEvent, payment: SupplierPaymentResponse): void {
        this.selectedPayment = payment;
        this.rebuildContextMenu();
        this.contextMenu?.show(event);
    }

    loadSupplierPayments(): void {
        this.loading = true;

        const query: SupplierPaymentQuery = {
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            search: this.searchTerm || undefined,
            status: this.filterStatus || undefined,
            supplierId: this.supplierFilter || undefined
        };

        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            query.fromDate = this.formatDateForApi(this.dateRange[0]);
            query.toDate = this.formatDateForApi(this.dateRange[1]);
        }

        if (this.sortField && this.sortOrder) {
            query.sorts = [{ field: this.sortField, direction: this.sortOrder === 1 ? 'ASC' : 'DESC' }];
        }

        this.supplierPaymentService.getSupplierPayments(query).subscribe({
            next: (response: PaginatedSupplierPaymentResponse) => {
                this.supplierPayments = response.data;
                this.totalCount = response.pagination.totalCount;
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('supplierPayments.messages.loadFailed')
                });
                console.error('Error loading supplier payments:', error);
                this.loading = false;
            }
        });
    }

    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    onSearch(): void {
        this.resetPagination();
        this.loadSupplierPayments();
    }

    createNew(): void {
        this.router.navigate(['/procurement/supplier-payments/new']);
    }

    edit(payment: SupplierPaymentResponse): void {
        this.router.navigate(['/procurement/supplier-payments/edit'], { queryParams: { id: payment.id } });
    }

    viewDetails(payment: SupplierPaymentResponse): void {
        this.router.navigate(['/procurement/supplier-payments/view'], { queryParams: { id: payment.id } });
    }

    confirmPayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('supplierPayments.messages.confirmConfirm'),
            header: this.i18n.t('common.actions.confirmPayment'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.confirmPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('supplierPayments.messages.confirmSuccess')
                        });
                        this.loadSupplierPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('supplierPayments.messages.confirmFailed')
                        });
                        console.error('Error confirming payment:', error);
                    }
                });
            }
        });
    }

    reconcilePayment(payment: SupplierPaymentResponse): void {
        this.supplierPaymentService.reconcilePayment(payment.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('supplierPayments.messages.reconcileSuccess')
                });
                this.loadSupplierPayments();
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('supplierPayments.messages.reconcileFailed')
                });
                console.error('Error reconciling payment:', error);
            }
        });
    }

    cancelPayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('supplierPayments.messages.cancelConfirm'),
            header: this.i18n.t('common.actions.cancel'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.cancelPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('supplierPayments.messages.cancelSuccess')
                        });
                        this.loadSupplierPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('supplierPayments.messages.cancelFailed')
                        });
                        console.error('Error cancelling payment:', error);
                    }
                });
            }
        });
    }

    deletePayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('supplierPayments.messages.deleteConfirm'),
            header: this.i18n.t('common.messages.confirmDeletion'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.deleteSupplierPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('supplierPayments.messages.deleteSuccess')
                        });
                        this.loadSupplierPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('supplierPayments.messages.deleteFailed')
                        });
                        console.error('Error deleting payment:', error);
                    }
                });
            }
        });
    }

    markAsAdvance(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('common.actions.markAsAdvance') + '?',
            header: this.i18n.t('common.actions.markAsAdvance'),
            icon: 'pi pi-info-circle',
            accept: () => {
                this.supplierPaymentService.markPaymentAsAdvance(payment.id, { description: 'Advance Payment' }).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('supplierPayments.messages.markAdvanceSuccess')
                        });
                        this.loadSupplierPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('supplierPayments.messages.markFailed')
                        });
                        console.error('Error marking payment as advance:', error);
                    }
                });
            }
        });
    }

    markAsRegular(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('common.actions.markAsRegular') + '?',
            header: this.i18n.t('common.actions.markAsRegular'),
            icon: 'pi pi-info-circle',
            accept: () => {
                this.supplierPaymentService.markPaymentAsRegular(payment.id, { description: 'Regular Payment' }).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('supplierPayments.messages.markRegularSuccess')
                        });
                        this.loadSupplierPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('supplierPayments.messages.markFailed')
                        });
                        console.error('Error marking payment as regular:', error);
                    }
                });
            }
        });
    }

    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') return;
        this.first = event.first;
        this.pageNumber = event.first / event.rows + 1;
        this.pageSize = event.rows;
        this.loadSupplierPayments();
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.sortField = (event.sortField as string) || null;
        this.sortOrder = event.sortOrder ?? null;
        this.loadSupplierPayments();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.resetPagination();
        this.loadSupplierPayments();
    }

    onFilterChange(): void {
        this.resetPagination();
        this.loadSupplierPayments();
    }

    onDateRangeSelect(): void {
        if (this.dateRange?.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            this.resetPagination();
            this.loadSupplierPayments();
        }
    }

    onDateClear(): void {
        this.dateRange = [];
        this.resetPagination();
        this.loadSupplierPayments();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.dateRange = [];
        this.supplierFilter = null;
        this.supplierFilterName = '';
        this.router.navigate(['/procurement/supplier-payments']);
        this.resetPagination();
        this.loadSupplierPayments();
    }

    clearSupplierFilter(): void {
        this.supplierFilter = null;
        this.supplierFilterName = '';
        this.router.navigate(['/procurement/supplier-payments']);
        this.resetPagination();
        this.loadSupplierPayments();
    }

    private loadSupplierName(supplierId: string): void {
        this.supplierService.getSupplierById(supplierId).subscribe({
            next: (supplier) => {
                this.supplierFilterName = supplier.name || 'Selected Supplier';
            },
            error: () => {
                this.supplierFilterName = 'Selected Supplier';
            }
        });
    }

    private resetPagination(): void {
        this.pageNumber = 1;
        this.first = 0;
    }

    exportPayments(format: 'csv' | 'json'): void {
        if (this.supplierPayments.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('supplierPayments.messages.noDataExport')
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV();
        } else {
            this.exportToJSON();
        }
    }

    private exportToCSV(): void {
        const headers = ['Supplier', 'Payment Type', 'Gross Amount', 'Payment Fee', 'Net Amount', 'Payment Date', 'Payment Method', 'Status', 'Provider', 'Invoice #', 'Reconciled'];
        const rows = this.supplierPayments.map(payment => [
            payment.supplierName,
            payment.paymentType || 'N/A',
            payment.amount.toString(),
            (payment.paymentFee || 0).toString(),
            (payment.netAmount || payment.amount).toString(),
            payment.paymentDate ? new Date(payment.paymentDate).toLocaleDateString() : '',
            payment.paymentMethod,
            payment.status,
            payment.providerName || '',
            payment.invoiceNumber || '-',
            payment.isReconciled ? 'YES' : 'NO'
        ]);

        const csvContent = [headers.join(','), ...rows.map(row => row.join(','))].join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `supplier-payments-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('supplierPayments.messages.exportCSVSuccess')
        });
    }

    private exportToJSON(): void {
        const jsonContent = JSON.stringify(this.supplierPayments, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `supplier-payments-${new Date().toISOString().split('T')[0]}.json`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('supplierPayments.messages.exportJSONSuccess')
        });
    }

    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(value, currency);
    }
}
