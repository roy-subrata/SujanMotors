import { Component, OnInit, inject, ViewChild } from '@angular/core';
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

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Completed', value: 'COMPLETED' },
        { label: 'Processing', value: 'PROCESSING' },
        { label: 'Failed', value: 'FAILED' },
        { label: 'Cancelled', value: 'CANCELLED' }
    ];

    ngOnInit(): void {
        this.initializeContextMenu();

        // Check for supplier filter from query params
        const supplierId = this.route.snapshot.queryParamMap.get('supplierId');
        if (supplierId) {
            this.supplierFilter = supplierId;
            this.loadSupplierName(supplierId);
        }

        this.loadSupplierPayments();
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
                visible: this.selectedPayment ? this.selectedPayment.status !== 'CONFIRMED' && this.selectedPayment.status !== 'RECONCILED' : false
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
                label: 'Confirm Payment',
                icon: 'pi pi-check-circle',
                command: () => {
                    if (this.selectedPayment) {
                        this.confirmPayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? (this.selectedPayment.status === 'PENDING' || this.selectedPayment.status === 'PROCESSING') : false
            },
            {
                label: 'Reconcile',
                icon: 'pi pi-sync',
                command: () => {
                    if (this.selectedPayment) {
                        this.reconcilePayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status === 'CONFIRMED' && !this.selectedPayment.isReconciled : false
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
                visible: this.selectedPayment ? this.selectedPayment.status !== 'CONFIRMED' && this.selectedPayment.status !== 'RECONCILED' : false
            },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => {
                    if (this.selectedPayment) {
                        this.deletePayment(this.selectedPayment);
                    }
                },
                visible: this.selectedPayment ? this.selectedPayment.status !== 'CONFIRMED' && this.selectedPayment.status !== 'RECONCILED' : false
            }
        ];
    }

    /**
     * Show context menu
     */
    showContextMenu(event: MouseEvent, payment: SupplierPaymentResponse): void {
        this.selectedPayment = payment;
        this.initializeContextMenu();
        this.contextMenu?.show(event);
    }

    /**
     * Load supplier payments
     */
    loadSupplierPayments(): void {
        this.loading = true;

        // Build query object
        const query: SupplierPaymentQuery = {
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            search: this.searchTerm || undefined,
            status: this.filterStatus || undefined,
            supplierId: this.supplierFilter || undefined
        };

        // Add date range if selected
        if (this.dateRange && this.dateRange.length === 2 && this.dateRange[0] && this.dateRange[1]) {
            query.fromDate = this.formatDateForApi(this.dateRange[0]);
            query.toDate = this.formatDateForApi(this.dateRange[1]);
        }

        if (this.sortField && this.sortOrder) {
            query.sorts = [
                {
                    field: this.sortField,
                    direction: this.sortOrder === 1 ? 'ASC' : 'DESC'
                }
            ];
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
                    summary: 'Error',
                    detail: 'Failed to load supplier payments'
                });
                console.error('Error loading supplier payments:', error);
                this.loading = false;
            }
        });
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
     * Search supplier payments
     */
    onSearch(): void {
        this.resetPagination();
        this.loadSupplierPayments();
    }

    /**
     * Create new supplier payment
     */
    createNew(): void {
        this.router.navigate(['/procurement/supplier-payments/new']);
    }

    /**
     * Edit supplier payment
     */
    edit(payment: SupplierPaymentResponse): void {
        this.router.navigate(['/procurement/supplier-payments/edit'], { queryParams: { id: payment.id } });
    }

    /**
     * View supplier payment details
     */
    viewDetails(payment: SupplierPaymentResponse): void {
        this.router.navigate(['/procurement/supplier-payments/view'], { queryParams: { id: payment.id } });
    }

    /**
     * Confirm payment
     */
    confirmPayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to confirm payment of ${this.formatCurrency(payment.amount)} to ${payment.supplierName}?`,
            header: 'Confirm Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.confirmPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment confirmed successfully'
                        });
                        this.loadSupplierPayments();
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
     * Reconcile payment
     */
    reconcilePayment(payment: SupplierPaymentResponse): void {
        this.supplierPaymentService.reconcilePayment(payment.id).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: 'Payment reconciled successfully'
                });
                this.loadSupplierPayments();
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

    /**
     * Cancel payment
     */
    cancelPayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to cancel this payment?`,
            header: 'Cancel Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.cancelPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment cancelled successfully'
                        });
                        this.loadSupplierPayments();
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
    deletePayment(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to delete this payment?`,
            header: 'Delete Payment',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.supplierPaymentService.deleteSupplierPayment(payment.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment deleted successfully'
                        });
                        this.loadSupplierPayments();
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
     * Mark payment as advance
     */
    markAsAdvance(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Mark this payment of ${this.formatCurrency(payment.amount)} as an advance payment?`,
            header: 'Mark as Advance',
            icon: 'pi pi-info-circle',
            accept: () => {
                this.supplierPaymentService.markPaymentAsAdvance(payment.id, { description: 'Advance Payment' }).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment marked as advance successfully'
                        });
                        this.loadSupplierPayments();
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
     * Mark payment as regularreconcilePayment
     */
    markAsRegular(payment: SupplierPaymentResponse): void {
        this.confirmationService.confirm({
            message: `Mark this payment of ${this.formatCurrency(payment.amount)} as a regular payment?`,
            header: 'Mark as Regular',
            icon: 'pi pi-info-circle',
            accept: () => {
                this.supplierPaymentService.markPaymentAsRegular(payment.id, {description: 'Regular Payment'}).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Payment marked as regular successfully'
                        });
                        this.loadSupplierPayments();
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
     * Handle pagination
     */
    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
            return;
        }
        this.first = event.first;
        this.pageNumber = event.first / event.rows + 1;
        this.pageSize = event.rows;
        this.loadSupplierPayments();
    }

    /**
     * Handle PrimeNG table lazy load event (pagination + sorting)
     */
    onLazyLoad(event: TableLazyLoadEvent): void {
        this.first = event.first ?? 0;
        this.pageSize = event.rows ?? this.pageSize;
        this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
        this.sortField = (event.sortField as string) || null;
        this.sortOrder = event.sortOrder ?? null;
        this.loadSupplierPayments();
    }

    /**
     * Clear search
     */
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

    /**
     * Clear all filters
     */
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

    /**
     * Export payments to CSV or JSON
     */
    exportPayments(format: 'csv' | 'json'): void {
        if (this.supplierPayments.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Data',
                detail: 'No supplier payments to export'
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV();
        } else {
            this.exportToJSON();
        }
    }

    /**
     * Export to CSV
     */
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

        const csvContent = [
            headers.join(','),
            ...rows.map(row => row.join(','))
        ].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv' });
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `supplier-payments-${new Date().toISOString().split('T')[0]}.csv`;
        a.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Supplier payments exported to CSV'
        });
    }

    /**
     * Export to JSON
     */
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
            summary: 'Success',
            detail: 'Supplier payments exported to JSON'
        });
    }

    /**
     * Format currency - uses default currency from settings
     */
    formatCurrency(value: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(value, currency);
    }
}
