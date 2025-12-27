import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoiceService, InvoiceResponse } from '../../services/invoice.service';
import { PaymentProviderService } from '../../../procurement/services/payment-provider.service';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService } from 'primeng/api';
import { DatePickerModule } from 'primeng/datepicker';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { tap } from 'rxjs';

@Component({
    selector: 'app-invoices-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        DatePickerModule,
        DialogModule,
        InputNumberModule,
        ToastModule,
        TagModule,
        TooltipModule,
        ConfirmDialogModule,
        CardModule,
        PaginatorModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './invoices-list.component.html',
    styleUrls: ['./invoices-list.component.css']
})
export class InvoicesListComponent implements OnInit {
    private readonly router = inject(Router);
    private readonly invoiceService = inject(InvoiceService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly paymentProviderService = inject(PaymentProviderService);
    private activateRoute = inject(ActivatedRoute);

    invoices: InvoiceResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    // Filters
    searchTerm = '';
    filterStatus = '';
    dateRange: Date[] = [];
    customerIdFilter: string | null = null;

    // Payment Dialog
    showPaymentDialog = false;
    selectedInvoice: InvoiceResponse | null = null;
    paymentReference = '';
    paymentAmount = 0;
    paymentMethod = 'CASH';
    paymentProviderId: string | null = null;
    paymentProviders: { label: string; value: string; id: string }[] = [];
    paymentMethods: { label: string; value: string }[] = [];

    statusOptions = [
        { label: 'All Statuses', value: '' },
        { label: 'Draft', value: 'DRAFT' },
        { label: 'Issued', value: 'ISSUED' },
        { label: 'Partially Paid', value: 'PARTIALLY_PAID' },
        { label: 'Paid', value: 'PAID' },
        { label: 'Overdue', value: 'OVERDUE' },
        { label: 'Cancelled', value: 'CANCELLED' }
    ];

    ngOnInit(): void {
        this.loadPaymentProviders();
        this.activateRoute.queryParams
            .pipe(
                tap({
                    next: (params) => {
                        if (params['customerId']) {
                            this.customerIdFilter = params['customerId'];
                        }
                    },
                    error: (err) => {
                        console.error('Error reading query params:', err);
                    }
                })
            )
            .subscribe();
        this.loadInvoices();
    }

    private loadPaymentProviders(): void {
        this.paymentProviderService.getAllPaymentProviders().subscribe({
            next: (providers) => {
                this.paymentProviders = (Array.isArray(providers) ? providers : []).map((p) => ({
                    label: p.providerName,
                    value: p.providerType || 'CASH',
                    id: p.id
                }));
                this.paymentMethods = this.paymentProviders.map(p => ({ label: p.label, value: p.value }));
                const hasCash = this.paymentMethods.some((m) => m.value === 'CASH');
                if (!hasCash) {
                    this.paymentMethods.unshift({ label: 'Cash', value: 'CASH' });
                }
            },
            error: () => {
                this.paymentMethods = [{ label: 'Cash', value: 'CASH' }];
                this.paymentProviders = [];
            }
        });
    }

    loadInvoices(): void {
        this.loading = true;

        const fromDate = this.dateRange && this.dateRange.length > 0 ? this.formatDateForApi(this.dateRange[0]) : undefined;
        const toDate = this.dateRange && this.dateRange.length > 1 ? this.formatDateForApi(this.dateRange[1]) : undefined;

        const filter: any = {
            searchTerm: this.searchTerm,
            status: this.filterStatus,
            customerId: this.customerIdFilter,
            fromDate: fromDate,
            toDate: toDate
        };

        this.invoiceService.getAllInvoices(this.pageNumber, this.pageSize, filter).subscribe({
            next: (response) => {
                this.invoices = response.data;
                this.totalRecords = response.pagination.totalCount;
                this.loading = false;
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load invoices'
                });
                console.error('Error loading invoices:', err);
                this.loading = false;
            }
        });
    }

    onSearch(): void {
        this.pageNumber = 1;
        this.loadInvoices();
    }

    onFilterChange(): void {
        this.pageNumber = 1;
        this.loadInvoices();
    }

    clearSearch(): void {
        this.searchTerm = '';
        this.pageNumber = 1;
        this.loadInvoices();
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.dateRange = [];
        this.pageNumber = 1;
        this.loadInvoices();
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? this.pageSize;
        this.loadInvoices();
    }

    exportInvoices(format: 'csv' | 'json'): void {
        const dataToExport = this.invoices;

        if (dataToExport.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: 'No Data',
                detail: 'No invoices available to export'
            });
            return;
        }

        if (format === 'csv') {
            this.exportToCSV(dataToExport);
        } else {
            this.exportToJSON(dataToExport);
        }
    }

    private exportToCSV(data: InvoiceResponse[]): void {
        const headers = ['Invoice #', 'Sales Order', 'Customer', 'Invoice Date', 'Due Date', 'Status', 'Total', 'Outstanding'];
        const csvData = data.map(invoice => [
            invoice.invoiceNumber,
            invoice.salesOrderNumber || '',
            invoice.customerName || '',
            this.formatDate(invoice.invoiceDate),
            this.formatDate(invoice.dueDate),
            invoice.status,
            invoice.grandTotal.toString(),
            invoice.outstandingAmount.toString()
        ]);

        const csvContent = [
            headers.join(','),
            ...csvData.map(row => row.map(cell => `"${cell}"`).join(','))
        ].join('\n');

        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `invoices_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Export Complete',
            detail: 'Invoices exported as CSV'
        });
    }

    private exportToJSON(data: InvoiceResponse[]): void {
        const jsonContent = JSON.stringify(data, null, 2);
        const blob = new Blob([jsonContent], { type: 'application/json' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `invoices_${new Date().toISOString().split('T')[0]}.json`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: 'Export Complete',
            detail: 'Invoices exported as JSON'
        });
    }

    private formatDateForApi(date: Date): string {
        const y = date.getFullYear();
        const m = String(date.getMonth() + 1).padStart(2, '0');
        const d = String(date.getDate()).padStart(2, '0');
        return `${y}-${m}-${d}`;
    }

    createInvoice(): void {
        this.router.navigate(['/sales/invoices/create']);
    }

    viewInvoice(invoice: InvoiceResponse): void {
        this.router.navigate(['/sales/invoices/view'], {
            queryParams: { id: invoice.id }
        });
    }

    issueInvoice(invoice: InvoiceResponse): void {
        this.confirmationService.confirm({
            message: `Are you sure you want to issue Invoice ${invoice.invoiceNumber}?`,
            header: 'Confirm',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.invoiceService.issueInvoice(invoice.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Invoice issued successfully'
                        });
                        this.loadInvoices();
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Failed to issue invoice'
                        });
                    }
                });
            }
        });
    }

    getStatusSeverity(status: string): string {
        const severityMap: Record<string, string> = {
            DRAFT: 'secondary',
            ISSUED: 'info',
            PARTIALLY_PAID: 'warning',
            PAID: 'success',
            OVERDUE: 'danger',
            CANCELLED: 'secondary'
        };
        return severityMap[status] || 'secondary';
    }

    formatCurrency(amount: number): string {
        return new Intl.NumberFormat('en-IN', {
            style: 'currency',
            currency: 'INR'
        }).format(amount);
    }

    formatDate(date: string): string {
        return new Date(date).toLocaleDateString('en-IN');
    }

    openPaymentDialog(invoice: InvoiceResponse): void {
        this.selectedInvoice = invoice;
        this.paymentAmount = invoice.outstandingAmount;
        this.paymentMethod = 'CASH';
        this.paymentProviderId = null;
        this.paymentReference = '';
        this.showPaymentDialog = true;
    }

    closePaymentDialog(): void {
        this.showPaymentDialog = false;
        this.selectedInvoice = null;
        this.paymentAmount = 0;
        this.paymentMethod = 'CASH';
        this.paymentProviderId = null;
        this.paymentReference = '';
    }

    recordPayment(): void {
        if (!this.selectedInvoice) return;

        if (this.paymentAmount <= 0) {
            this.messageService.add({
                severity: 'error',
                summary: 'Invalid Amount',
                detail: 'Payment amount must be greater than 0'
            });
            return;
        }

        // Find payment provider ID from selected method
        const provider = this.paymentProviders.find(p => p.value === this.paymentMethod);
        const providerId = provider?.id || null;

        // Show warning for overpayments but allow them (creates credit balance)
        if (this.paymentAmount > this.selectedInvoice.outstandingAmount) {
            const creditAmount = this.paymentAmount - this.selectedInvoice.outstandingAmount;
            this.messageService.add({
                severity: 'info',
                summary: 'Overpayment',
                detail: `Payment exceeds outstanding by ${this.formatCurrency(creditAmount)}. This will create a credit balance.`,
                life: 5000
            });
        }

        this.invoiceService
            .recordPayment(this.selectedInvoice.id, {
                amount: this.paymentAmount,
                paymentDate: new Date().toISOString(),
                paymentMethod: this.paymentMethod,
                referenceNumber: this.paymentReference,
                paymentProviderId: providerId || undefined
            })
            .subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Payment Recorded',
                        detail: `Payment of ${this.formatCurrency(this.paymentAmount)} recorded successfully`
                    });
                    this.closePaymentDialog();
                    this.loadInvoices();
                },
                error: () => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Payment Failed',
                        detail: 'Failed to record payment'
                    });
                }
            });
    }

    canRecordPayment(invoice: InvoiceResponse): boolean {
        // Allow payments for issued invoices (including overpayments that create credit)
        return invoice.status !== 'CANCELLED' && invoice.status !== 'DRAFT';
    }

    viewSalesOrder(invoice: InvoiceResponse): void {
        this.router.navigate(['/sales/sales-orders/view'], {
            queryParams: { id: invoice.salesOrderId }
        });
    }
}
