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
        PaginatorModule
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

    // Computed page number for API (1-based)
    get pageNumber(): number {
        return Math.floor(this.first / this.pageSize) + 1;
    }
    loading: boolean = false;
    contextMenuItems: MenuItem[] = [];
    selectedPayment: CustomerPaymentResponse | null = null;
  

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Processing', value: 'PROCESSING' },
        { label: 'Completed', value: 'COMPLETED' },
        { label: 'Failed', value: 'FAILED' },
        { label: 'Cancelled', value: 'CANCELLED' },
        { label: 'Refunded', value: 'REFUNDED' }
    ];

    reconciledOptions = [
        { label: 'Reconciled', value: true },
        { label: 'Not Reconciled', value: false }
    ];

    reconciledFilter: boolean | null = null;

    ngOnInit(): void {
        this.initializeContextMenu();
        this.setupSearchDebounce();
        this.loadCustomerPayments();
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    /**
     * Setup debounced search
     */
    private setupSearchDebounce(): void {
        this.searchSubject$.pipe(debounceTime(400), distinctUntilChanged(), takeUntil(this.destroy$)).subscribe(() => {
            this.first = 0;
            this.loadCustomerPayments();
        });
    }

    /**
     * Handle search input change
     */
    onSearchInput(): void {
        this.searchSubject$.next(this.searchTerm);
    }

    /**
     * Initialize context menu items
     */
    private initializeContextMenu(): void {
        this.contextMenuItems = [
            {
                label: 'View Details',
                icon: 'pi pi-eye',
                command: () => {
                    if (this.selectedPayment) {
                        this.viewDetails(this.selectedPayment);
                    }
                }
            },
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => {
                    if (this.selectedPayment) {
                        this.edit(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status !== 'COMPLETED' && this.selectedPayment.status !== 'REFUNDED' : false
            },
            { separator: true },
            {
                label: 'Mark as Advance',
                icon: 'pi pi-arrow-up',
                command: () => {
                    if (this.selectedPayment) {
                        this.markAsAdvance(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.paymentType !== 'ADVANCE' : false
            },
            {
                label: 'Mark as Regular',
                icon: 'pi pi-arrow-down',
                command: () => {
                    if (this.selectedPayment) {
                        this.markAsRegular(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.paymentType === 'ADVANCE' : false
            },
            { separator: true },
            {
                label: 'Confirm',
                icon: 'pi pi-check',
                command: () => {
                    if (this.selectedPayment) {
                        this.confirmPayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status === 'PENDING' : false
            },
            {
                label: 'Reconcile',
                icon: 'pi pi-check-square',
                command: () => {
                    if (this.selectedPayment) {
                        this.reconcilePayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status === 'COMPLETED' && !this.selectedPayment.isReconciled : false
            },
            {
                label: 'Refund',
                icon: 'pi pi-replay',
                command: () => {
                    if (this.selectedPayment) {
                        this.refundPayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status === 'COMPLETED' && !this.selectedPayment.isReconciled : false
            },
            { separator: true },
            {
                label: 'Cancel',
                icon: 'pi pi-times',
                command: () => {
                    if (this.selectedPayment) {
                        this.cancelPayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status !== 'COMPLETED' && this.selectedPayment.status !== 'REFUNDED' : false
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => {
                    if (this.selectedPayment) {
                        this.deletePayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status !== 'COMPLETED' && this.selectedPayment.status !== 'REFUNDED' : false
            }
        ];
    }

    /**
     * Show context menu
     */
    showContextMenu(event: MouseEvent, payment: CustomerPaymentResponse): void {
        this.selectedPayment = payment;
        this.initializeContextMenu();
        this.contextMenu?.show(event);
    }

    /**
     * Load customer payments
     */
    loadCustomerPayments(): void {
        this.loading = true;

        // Prepare date range for API - format as local date strings to avoid timezone issues
        let fromDateStr: string | undefined;
        let toDateStr: string | undefined;

        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            // Format as YYYY-MM-DD to preserve local date
            fromDateStr = this.formatDateForApi(this.dateRange[0]);
            toDateStr = this.formatDateForApi(this.dateRange[1]);
        }

        // Load all payments with pagination
        this.customerPaymentService
            .getCustomerPayments({
                search: this.searchTerm,
                status: this.statusFilter || undefined,
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
                        summary: 'Error',
                        detail: 'Failed to load customer payments'
                    });
                    console.error('Error loading customer payments:', error);
                    this.loading = false;
                }
            });
    }

    /**
     * Filter change handler - triggers on status or date range change
     */
    onFilterChange(): void {
        this.first = 0;
        this.loadCustomerPayments();
    }

    /**
     * Handle date range selection
     */
    onDateRangeSelect(): void {
        // Only filter when both dates are selected
        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            this.first = 0;
            this.loadCustomerPayments();
        }
    }

    /**
     * Handle date range change (including clear)
     */
    onDateRangeChange(value: Date[] | null): void {
        // If cleared (null or empty), reload without date filter
        if (!value || value.length === 0) {
            this.dateRange = [];
            this.first = 0;
            this.loadCustomerPayments();
        }
    }
    /**
     * Clear all filters
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.statusFilter = null;
        this.dateRange = [];
        this.reconciledFilter = null;
        this.first = 0;
        this.router.navigate(['/sales/customer-payments']); // Clear query params
        this.loadCustomerPayments();
    }

    /**
     * Get filtered payments based on status
     */
    get filteredPayments(): CustomerPaymentResponse[] {
        if (!this.statusFilter) {
            return this.customerPayments;
        }
        return this.customerPayments.filter((p) => p.status === this.statusFilter);
    }

    /**
     * Create new customer payment
     */
    createNew(): void {
        this.router.navigate(['/sales/customer-payments/new']);
    }

    /**
     * Edit customer payment
     */
    edit(payment: CustomerPaymentResponse): void {
        this.router.navigate(['/sales/customer-payments/edit'], { queryParams: { id: payment.id } });
    }

    /**
     * View customer payment details
     */
    viewDetails(payment: CustomerPaymentResponse): void {
        this.router.navigate(['/sales/customer-payments/view'], { queryParams: { id: payment.id } });
    }

    /**
     * Confirm payment
     */
    confirmPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: 'Are you sure you want to confirm payment of ' + this.formatCurrency(payment.amount) + ' from ' + payment.customerName + '?',
            header: 'Confirm Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.confirmPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment confirmed successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to confirm payment'
                        });
                        console.error('Error confirming payment:', error);
                    }
                });
            }
        });
    }

    /**
     * Mark payment as advance
     */
    markAsAdvance(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Mark this payment of ${this.formatCurrency(payment.amount)} as an advance payment?`,
            header: 'Mark as Advance',
            icon: 'pi pi-info-circle',
            accept: () => {
                this.customerPaymentService.markPaymentAsAdvance(payment.id, 'Advance Payment').subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment marked as advance successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to mark payment as advance'
                        });
                        console.error('Error marking payment as advance:', error);
                    }
                });
            }
        });
    }

    /**
     * Mark payment as regular
     */
    markAsRegular(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Mark this payment of ${this.formatCurrency(payment.amount)} as a regular payment?`,
            header: 'Mark as Regular',
            icon: 'pi pi-info-circle',
            accept: () => {
                this.customerPaymentService.markPaymentAsRegular(payment.id, 'Regular Payment').subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment marked as regular successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to mark payment as regular'
                        });
                        console.error('Error marking payment as regular:', error);
                    }
                });
            }
        });
    }

    /**
     * Reconcile payment
     */
    reconcilePayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: 'Are you sure you want to reconcile payment of ' + this.formatCurrency(payment.amount) + ' from ' + payment.customerName + '?',
            header: 'Reconcile Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.reconcilePayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment reconciled successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to reconcile payment'
                        });
                        console.error('Error reconciling payment:', error);
                    }
                });
            }
        });
    }

    /**
     * Refund payment
     */
    refundPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: 'Are you sure you want to refund payment of ' + this.formatCurrency(payment.amount) + ' to ' + payment.customerName + '?',
            header: 'Refund Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.refundPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment refunded successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to refund payment'
                        });
                        console.error('Error refunding payment:', error);
                    }
                });
            }
        });
    }

    /**
     * Cancel payment
     */
    cancelPayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: 'Are you sure you want to cancel this payment?',
            header: 'Cancel Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.cancelPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment cancelled successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to cancel payment'
                        });
                        console.error('Error cancelling payment:', error);
                    }
                });
            }
        });
    }

    /**
     * Delete payment
     */
    deletePayment(payment: CustomerPaymentResponse): void {
        this.confirmationService.confirm({
            message: 'Are you sure you want to delete this payment?',
            header: 'Delete Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.customerPaymentService.deleteCustomerPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment deleted successfully'
                        });
                        this.loadCustomerPayments();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to delete payment'
                        });
                        console.error('Error deleting payment:', error);
                    }
                });
            }
        });
    }

    /**
     * Handle pagination
     */
    onPageChange(event: PaginatorState): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadCustomerPayments();
    }

    /**
     * Export payments to CSV or JSON
     */
    exportPayments(format: 'csv' | 'json'): void {
        const dataToExport = this.filteredPayments;

        if (dataToExport.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Data',
                detail: 'No payments available to export'
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV(dataToExport);
        } else {
            this.exportToJSON(dataToExport);
        }
    }

    /**
     * Export data to CSV
     */
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
            summary: 'Export Complete',
            detail: 'Payments exported as CSV'
        });
    }

    /**
     * Export data to JSON
     */
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
            summary: 'Export Complete',
            detail: 'Payments exported as JSON'
        });
    }

    /**
     * Format currency
     */
    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency() || 'BDT';
        return this.currencyService.formatCurrency(value, currency);
    }

    /**
     * Clear search input and reload
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.first = 0;
        this.loadCustomerPayments();
    }

    /**
     * Format date for API - returns YYYY-MM-DD string in local timezone
     */
    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    /**
     * Get severity for status tag
     */
    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (status?.toUpperCase()) {
            case 'COMPLETED':
                return 'success';
            case 'PENDING':
                return 'warn';
            case 'FAILED':
            case 'CANCELLED':
                return 'danger';
            case 'REFUNDED':
                return 'secondary';
            case 'PROCESSING':
                return 'info';
            default:
                return 'secondary';
        }
    }

    /**
     * Get severity for payment method tag
     */
    getMethodSeverity(method: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (method?.toUpperCase()) {
            case 'CASH':
                return 'success';
            case 'CARD':
            case 'CREDIT_CARD':
            case 'DEBIT_CARD':
                return 'info';
            case 'UPI':
                return 'contrast';
            case 'BANK_TRANSFER':
            case 'NEFT':
            case 'RTGS':
                return 'warn';
            case 'CHEQUE':
                return 'secondary';
            case 'REFUND':
                return 'danger';
            default:
                return 'secondary';
        }
    }

    /**
     * Handle context menu select event
     */
    onContextMenuSelect(event: TableContextMenuSelectEvent): void {
        this.selectedPayment = event.data;
        this.initializeContextMenu();
    }
}
