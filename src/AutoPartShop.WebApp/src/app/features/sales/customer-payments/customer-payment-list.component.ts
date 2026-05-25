import { Component, OnInit, OnDestroy, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule, TableContextMenuSelectEvent } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { SelectModule } from 'primeng/select';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { DatePickerModule } from 'primeng/datepicker';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, takeUntil } from 'rxjs/operators';
import { CustomerPaymentService, CustomerPaymentResponse } from '../services/customer-payment.service';
import { CustomerService, PaginatedResponse } from '../services/customer.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { AppCurrencyPipe } from '../../../shared/pipes/app-currency.pipe';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
    selector: 'app-customer-payment-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        TableModule,
        InputTextModule,
        ToastModule,
        ConfirmDialogModule,
        DialogModule,
        TagModule,
        SelectModule,
        ContextMenuModule,
        RippleModule,
        TooltipModule,
        CardModule,
        DatePickerModule,
        PaginatorModule,
        AppCurrencyPipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './customer-payment-list.component.html',
    styleUrls: ['./customer-payment-list.component.css']
})
export class CustomerPaymentListComponent implements OnInit, OnDestroy {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    private readonly customerPaymentService = inject(CustomerPaymentService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly customerService = inject(CustomerService);
    private readonly currencyService = inject(CurrencyService);
    private readonly i18n = inject(I18nService);

    private readonly destroy$ = new Subject<void>();
    private readonly searchSubject$ = new Subject<string>();

    customerPayments: CustomerPaymentResponse[] = [];
    searchTerm: string = '';
    statusFilter: string | null = null;
    dateRange: Date[] = [];
    pageSize: number = 25;
    pageSizeOptions = [10, 25, 50, 100];
    totalCount: number = 0;
    first: number = 0;

    customerFilter: string | null = null;
    customerFilterName: string = '';

    get pageNumber(): number {
        return Math.floor(this.first / this.pageSize) + 1;
    }
    loading: boolean = false;
    contextMenuItems: MenuItem[] = [];
    selectedPayment: CustomerPaymentResponse | null = null;

    statusOptions: { label: string; value: string }[] = [];
    reconciledOptions: { label: string; value: boolean }[] = [];
    reconciledFilter: boolean | null = null;

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntil(this.destroy$)).subscribe(() => {
            this.buildStatusOptions();
            if (this.selectedPayment) this.rebuildContextMenu();
        });
        this.rebuildContextMenu();
        this.setupSearchDebounce();

        const customerId = this.route.snapshot.queryParamMap.get('customerId');
        if (customerId) {
            this.customerFilter = customerId;
            this.loadCustomerName(customerId);
        }

        this.loadCustomerPayments();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('customerPayments.statusOptions.allStatuses'), value: '' },
            { label: this.i18n.t('customerPayments.statusOptions.pending'),     value: 'PENDING' },
            { label: this.i18n.t('customerPayments.statusOptions.processing'),  value: 'PROCESSING' },
            { label: this.i18n.t('customerPayments.statusOptions.completed'),   value: 'COMPLETED' },
            { label: this.i18n.t('customerPayments.statusOptions.failed'),      value: 'FAILED' },
            { label: this.i18n.t('customerPayments.statusOptions.cancelled'),   value: 'CANCELLED' },
            { label: this.i18n.t('customerPayments.statusOptions.refunded'),    value: 'REFUNDED' }
        ];
        this.reconciledOptions = [
            { label: this.i18n.t('customerPayments.reconciledOptions.reconciled'),    value: true },
            { label: this.i18n.t('customerPayments.reconciledOptions.notReconciled'), value: false }
        ];
    }

    private setupSearchDebounce(): void {
        this.searchSubject$.pipe(debounceTime(400), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(() => {
            this.first = 0;
            this.loadCustomerPayments();
        });
    }

    onSearchInput(): void {
        this.searchSubject$.next(this.searchTerm);
    }

    private rebuildContextMenu(): void {
        const p = this.selectedPayment;
        this.contextMenuItems = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => { if (p) this.viewDetails(p); }
            },
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => { if (p) this.edit(p); },
                visible: p ? p.status !== 'COMPLETED' && p.status !== 'REFUNDED' : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.markAsAdvance'),
                icon: 'pi pi-arrow-up',
                command: () => { if (p) this.markAsAdvance(p); },
                visible: p ? p.paymentType !== 'ADVANCE' : false
            },
            {
                label: this.i18n.t('common.actions.markAsRegular'),
                icon: 'pi pi-arrow-down',
                command: () => { if (p) this.markAsRegular(p); },
                visible: p ? p.paymentType === 'ADVANCE' : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.confirm'),
                icon: 'pi pi-check',
                command: () => { if (p) this.confirmPayment(p); },
                visible: p ? p.status === 'PENDING' : false
            },
            {
                label: this.i18n.t('common.actions.reconcile'),
                icon: 'pi pi-check-square',
                command: () => { if (p) this.reconcilePayment(p); },
                visible: p ? p.status === 'COMPLETED' && !p.isReconciled : false
            },
            {
                label: this.i18n.t('common.actions.refund'),
                icon: 'pi pi-replay',
                command: () => { if (p) this.refundPayment(p); },
                visible: p ? p.status === 'COMPLETED' && !p.isReconciled : false
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.cancel'),
                icon: 'pi pi-times',
                command: () => { if (p) this.cancelPayment(p); },
                visible: p ? p.status !== 'COMPLETED' && p.status !== 'REFUNDED' : false
            },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => { if (p) this.deletePayment(p); },
                visible: p ? p.status !== 'COMPLETED' && p.status !== 'REFUNDED' : false
            }
        ];
    }

    showContextMenu(event: MouseEvent, payment: CustomerPaymentResponse): void {
        this.selectedPayment = payment;
        this.rebuildContextMenu();
        this.contextMenu?.show(event);
    }

    loadCustomerPayments(): void {
        this.loading = true;

        let fromDateStr: string | undefined;
        let toDateStr: string | undefined;

        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            fromDateStr = this.formatDateForApi(this.dateRange[0]);
            toDateStr = this.formatDateForApi(this.dateRange[1]);
        }

        this.customerPaymentService
            .getCustomerPayments({
                search: this.searchTerm,
                status: this.statusFilter || undefined,
                customerId: this.customerFilter || undefined,
                fromDate: fromDateStr,
                toDate: toDateStr,
                isReconciled: this.reconciledFilter ?? undefined,
                pageNumber: this.pageNumber,
                pageSize: this.pageSize
            })
            .subscribe({
                next: (response: PaginatedResponse<CustomerPaymentResponse>) => {
                    this.customerPayments = response.data;
                    this.totalCount = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('customerPayments.messages.loadFailed')
                    });
                    console.error('Error loading customer payments:', error);
                    this.loading = false;
                }
            });
    }

    onFilterChange(): void {
        this.first = 0;
        this.loadCustomerPayments();
    }

    onDateRangeSelect(): void {
        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            this.first = 0;
            this.loadCustomerPayments();
        }
    }

    onDateRangeChange(value: Date[] | null): void {
        if (!value || value.length === 0) {
            this.dateRange = [];
            this.first = 0;
            this.loadCustomerPayments();
        }
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.statusFilter = null;
        this.dateRange = [];
        this.reconciledFilter = null;
        this.customerFilter = null;
        this.customerFilterName = '';
        this.first = 0;
        this.router.navigate(['/sales/customer-payments']);
        this.loadCustomerPayments();
    }

    clearCustomerFilter(): void {
        this.customerFilter = null;
        this.customerFilterName = '';
        this.first = 0;
        this.router.navigate(['/sales/customer-payments']);
        this.loadCustomerPayments();
    }

    private loadCustomerName(customerId: string): void {
        this.customerService.getCustomerById(customerId).subscribe({
            next: (customer) => {
                this.customerFilterName = `${customer.firstName} ${customer.lastName}`;
            },
            error: () => {
                this.customerFilterName = 'Selected Customer';
            }
        });
    }

    get filteredPayments(): CustomerPaymentResponse[] {
        return this.customerPayments;
    }

    createNew(): void {
        this.router.navigate(['/sales/customer-payments/new']);
    }

    edit(payment: CustomerPaymentResponse): void {
        this.router.navigate(['/sales/customer-payments/edit'], { queryParams: { id: payment.id } });
    }

    viewDetails(payment: CustomerPaymentResponse): void {
        this.router.navigate(['/sales/customer-payments/view'], { queryParams: { id: payment.id } });
    }

    confirmPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.confirmConfirm'),
            header: this.i18n.t('common.actions.confirmPayment'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.confirmPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.confirmSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.confirmFailed'))
                        });
                        console.error('Error confirming payment:', error);
                    }
                });
            }
        });
    }

    markAsAdvance(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.markAsAdvanceConfirm'),
            header: this.i18n.t('common.actions.markAsAdvance'),
            icon: 'pi pi-info-circle',
            accept: () => {
                this.customerPaymentService.markPaymentAsAdvance(payment.id, 'Advance Payment').subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.markAsAdvanceSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.markAsAdvanceFailed'))
                        });
                        console.error('Error marking payment as advance:', error);
                    }
                });
            }
        });
    }

    markAsRegular(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.markAsRegularConfirm'),
            header: this.i18n.t('common.actions.markAsRegular'),
            icon: 'pi pi-info-circle',
            accept: () => {
                this.customerPaymentService.markPaymentAsRegular(payment.id, 'Regular Payment').subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.markAsRegularSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.markAsRegularFailed'))
                        });
                        console.error('Error marking payment as regular:', error);
                    }
                });
            }
        });
    }

    reconcilePayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.reconcileConfirm'),
            header: this.i18n.t('common.actions.reconcile'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.reconcilePayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.reconcileSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.reconcileFailed'))
                        });
                        console.error('Error reconciling payment:', error);
                    }
                });
            }
        });
    }

    refundPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.refundConfirm'),
            header: this.i18n.t('common.actions.refund'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.refundPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.refundSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.refundFailed'))
                        });
                        console.error('Error refunding payment:', error);
                    }
                });
            }
        });
    }

    cancelPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.cancelConfirm'),
            header: this.i18n.t('common.actions.cancel'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.cancelPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.cancelSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.cancelFailed'))
                        });
                        console.error('Error cancelling payment:', error);
                    }
                });
            }
        });
    }

    deletePayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('customerPayments.messages.deleteConfirm'),
            header: this.i18n.t('common.messages.confirmDeletion'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.deleteCustomerPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('customerPayments.messages.deleteSuccess')
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('customerPayments.messages.deleteFailed'))
                        });
                        console.error('Error deleting payment:', error);
                    }
                });
            }
        });
    }

    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadCustomerPayments();
    }

    exportPayments(format: 'csv' | 'json'): void {
        const dataToExport = this.filteredPayments;

        if (dataToExport.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('customerPayments.messages.exportNoData')
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV(dataToExport);
        } else {
            this.exportToJSON(dataToExport);
        }
    }

    private exportToCSV(data: CustomerPaymentResponse[]): void {
        const headers = ['Customer', 'Amount', 'Date', 'Method', 'Status', 'Provider', 'Invoice', 'Reconciled', 'Transaction', 'Reference'];
        const csvData = data.map((payment) => [
            payment.customerName,
            payment.amount.toString(),
            new Date(payment.paymentDate).toLocaleDateString(),
            payment.paymentMethod,
            payment.status,
            payment.providerName || '',
            payment.invoiceNumber || '',
            payment.isReconciled ? 'Yes' : 'No',
            payment.transactionNumber || '',
            payment.referenceNumber || ''
        ]);

        const csvContent = [headers.join(','), ...csvData.map((row) => row.map((cell) => `"${cell}"`).join(','))].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `customer_payments_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('customerPayments.messages.exportCSVSuccess')
        });
    }

    private exportToJSON(data: CustomerPaymentResponse[]): void {
        const jsonContent = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `customer_payments_${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('customerPayments.messages.exportJSONSuccess')
        });
    }

    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(value, currency);
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.first = 0;
        this.loadCustomerPayments();
    }

    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (status?.toUpperCase()) {
            case 'COMPLETED': return 'success';
            case 'PENDING': return 'warn';
            case 'FAILED':
            case 'CANCELLED': return 'danger';
            case 'REFUNDED': return 'secondary';
            case 'PROCESSING': return 'info';
            default: return 'secondary';
        }
    }

    getMethodSeverity(method: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (method?.toUpperCase()) {
            case 'CASH': return 'success';
            case 'CARD':
            case 'CREDIT_CARD':
            case 'DEBIT_CARD': return 'info';
            case 'UPI': return 'contrast';
            case 'BANK_TRANSFER':
            case 'NEFT':
            case 'RTGS': return 'warn';
            case 'CHEQUE': return 'secondary';
            case 'REFUND': return 'danger';
            default: return 'secondary';
        }
    }

    onContextMenuSelect(event: TableContextMenuSelectEvent): void {
        this.selectedPayment = event.data;
        this.rebuildContextMenu();
    }
}
