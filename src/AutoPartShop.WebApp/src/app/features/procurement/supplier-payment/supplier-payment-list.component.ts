import { Component, OnInit, inject, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { CardModule } from 'primeng/card';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { PaginatorModule } from 'primeng/paginator';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { SupplierPaymentService, SupplierPaymentResponse, PaginatedSupplierPaymentResponse } from '../services/supplier-payment.service';
import { StatusBadgeComponent } from '../components/status-badge.component';

@Component({
    selector: 'app-supplier-payment-list',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, TableModule, InputTextModule, ToastModule, ConfirmDialogModule, DialogModule, TagModule, ContextMenuModule, RippleModule, CardModule, Select, DatePicker, PaginatorModule, StatusBadgeComponent],
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

    supplierPayments: SupplierPaymentResponse[] = [];
    searchTerm: string = '';
    pageNumber: number = 1;
    pageSize: number = 10;
    totalCount: number = 0;
    loading: boolean = false;
    contextMenuItems: MenuItem[] = [];
    selectedPayment: SupplierPaymentResponse | null = null;
    filterStatus: string | null = null;
    dateRange: Date[] | null = null;
    pageSizeOptions = [10, 25, 50, 100];

    statusOptions = [
        { label: 'All', value: null },
        { label: 'Pending', value: 'PENDING' },
        { label: 'Confirmed', value: 'CONFIRMED' },
        { label: 'Reconciled', value: 'RECONCILED' },
        { label: 'Cancelled', value: 'CANCELLED' }
    ];

    ngOnInit(): void {
        this.initializeContextMenu();
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
                label: 'View Summary',
                icon: 'pi pi-chart-bar',
                command: () => {
                    if (this.selectedPayment) {
                        this.viewPaymentSummary(this.selectedPayment);
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
        this.supplierPaymentService.getSupplierPayments(this.pageNumber, this.pageSize, this.searchTerm).subscribe({
            next: (response: PaginatedSupplierPaymentResponse) => {
                this.supplierPayments = response.items;
                this.totalCount = response.totalCount;
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
     * Search supplier payments
     */
    onSearch(): void {
        this.pageNumber = 1;
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
     * View supplier payment summary
     */
    viewPaymentSummary(payment: SupplierPaymentResponse): void {
        this.router.navigate(['/procurement/supplier-payments/summary', payment.supplierId]);
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
        this.pageNumber = event.first / event.rows + 1;
        this.pageSize = event.rows;
        this.loadSupplierPayments();
    }

    /**
     * Clear search
     */
    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadSupplierPayments();
    }

    /**
     * Handle filter change
     */
    onFilterChange(): void {
        // TODO: Implement filtering logic with status and date range
        this.pageNumber = 1;
        this.loadSupplierPayments();
    }

    /**
     * Clear all filters
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.dateRange = null;
        this.pageNumber = 1;
        this.loadSupplierPayments();
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
     * Format currency
     */
    formatCurrency(value: number): string {
        return new Intl.NumberFormat('en-IN', {
            style: 'currency',
            currency: 'INR'
        }).format(value);
    }
}
