import { Component, OnInit, inject, ViewChild, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { InvoiceService, InvoiceResponse } from '../../services/invoice.service';
import { PaymentProviderService } from '../../../procurement/services/payment-provider.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { DialogModule } from 'primeng/dialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { DatePickerModule } from 'primeng/datepicker';
import { TableModule } from 'primeng/table';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { DialogService, DynamicDialogRef } from 'primeng/dynamicdialog';
import { MenuModule } from 'primeng/menu';
import { Menu } from 'primeng/menu';
import { tap } from 'rxjs';
import { ApplyCustomerAdvanceCreditDialogComponent } from '../../sales-orders/apply-advance-credit/apply-advance-credit-dialog.component';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';

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
        PaginatorModule,
        MenuModule
    ],
    providers: [MessageService, ConfirmationService, DialogService],
    templateUrl: './invoices-list.component.html',
    styleUrls: ['./invoices-list.component.css']
})
export class InvoicesListComponent implements OnInit {
    @ViewChild('actionMenu') actionMenu!: Menu;

    private readonly router = inject(Router);
    private readonly invoiceService = inject(InvoiceService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly paymentProviderService = inject(PaymentProviderService);
    private readonly dialogService = inject(DialogService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);
    private activateRoute = inject(ActivatedRoute);

    private dialogRef: DynamicDialogRef | null | undefined;

    invoices: InvoiceResponse[] = [];
    loading = false;
    totalRecords = 0;
    pageNumber = 1;
    pageSize = 25;
    pageSizeOptions = [10, 25, 50, 100];

    actionItems: MenuItem[] = [];

    searchTerm = '';
    filterStatus = '';
    dateRange: Date[] = [];
    customerIdFilter: string | null = null;

    showPaymentDialog = false;
    selectedInvoice: InvoiceResponse | null = null;
    paymentReference = '';
    paymentAmount = 0;
    paymentMethod = 'CASH';
    paymentDate: Date = new Date();
    paymentProviderId: string | null = null;
    paymentProviders: { label: string; value: string; id: string }[] = [];
    paymentMethods: { label: string; value: string }[] = [];

    statusOptions: { label: string; value: string }[] = [];

    get currencyCode(): string {
        return this.currencyService.selectedCurrency();
    }

    get currencyLocale(): string {
        return this.currencyService.getSelectedCurrencyLocale();
    }

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
            if (this.selectedInvoice) this.actionItems = this.buildActionItems(this.selectedInvoice);
        });

        this.loadPaymentProviders();
        this.activateRoute.queryParams
            .pipe(
                tap({
                    next: (params) => {
                        if (params['customerId']) {
                            this.customerIdFilter = params['customerId'];
                        }
                    },
                    error: (err) => { console.error('Error reading query params:', err); }
                })
            )
            .subscribe();
        this.loadInvoices();
    }

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('invoices.statusOptions.allStatuses'), value: '' },
            { label: this.i18n.t('invoices.statusOptions.draft'),        value: 'DRAFT' },
            { label: this.i18n.t('invoices.statusOptions.issued'),       value: 'ISSUED' },
            { label: this.i18n.t('invoices.statusOptions.partiallyPaid'), value: 'PARTIALLY_PAID' },
            { label: this.i18n.t('invoices.statusOptions.paid'),         value: 'PAID' },
            { label: this.i18n.t('invoices.statusOptions.overdue'),      value: 'OVERDUE' },
            { label: this.i18n.t('invoices.statusOptions.cancelled'),    value: 'CANCELLED' }
        ];
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
            fromDate,
            toDate
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
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('invoices.messages.loadFailed')
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
        if (this.invoices.length === 0) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('invoices.messages.exportNoData')
            });
            return;
        }
        if (format === 'csv') {
            this.exportToCSV(this.invoices);
        } else {
            this.exportToJSON(this.invoices);
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

        const csvContent = [headers.join(','), ...csvData.map(row => row.map(cell => `"${cell}"`).join(','))].join('\n');
        const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `invoices_${new Date().toISOString().split('T')[0]}.csv`;
        link.click();
        window.URL.revokeObjectURL(url);

        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('invoices.messages.exportCSVSuccess')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('invoices.messages.exportJSONSuccess')
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
        this.router.navigate(['/sales/invoices/view'], { queryParams: { id: invoice.id } });
    }

    issueInvoice(invoice: InvoiceResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('invoices.messages.issueConfirm', { number: invoice.invoiceNumber }),
            header: this.i18n.t('common.actions.confirm'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.invoiceService.issueInvoice(invoice.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('invoices.messages.issueSuccess')
                        });
                        this.loadInvoices();
                    },
                    error: () => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: this.i18n.t('invoices.messages.issueFailed')
                        });
                    }
                });
            }
        });
    }

    getStatusSeverity(status: string): 'secondary' | 'info' | 'warn' | 'success' | 'danger' {
        const severityMap: Record<string, 'secondary' | 'info' | 'warn' | 'success' | 'danger'> = {
            DRAFT: 'secondary',
            ISSUED: 'info',
            PARTIALLY_PAID: 'warn',
            PAID: 'success',
            OVERDUE: 'danger',
            CANCELLED: 'secondary'
        };
        return severityMap[status] || 'secondary';
    }

    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    formatDate(date: string): string {
        return new Date(date).toLocaleDateString('en-IN');
    }

    openActionMenu(event: MouseEvent, invoice: InvoiceResponse): void {
        this.selectedInvoice = invoice;
        this.actionItems = this.buildActionItems(invoice);
        this.actionMenu.toggle(event);
    }

    private buildActionItems(invoice: InvoiceResponse): MenuItem[] {
        const items: MenuItem[] = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => this.viewInvoice(invoice)
            },
            {
                label: this.i18n.t('common.actions.viewSalesOrder'),
                icon: 'pi pi-shopping-cart',
                command: () => this.viewSalesOrder(invoice)
            },
            { separator: true }
        ];

        if (invoice.status === 'DRAFT') {
            items.push({
                label: this.i18n.t('common.actions.issueInvoice'),
                icon: 'pi pi-check-circle',
                command: () => this.issueInvoice(invoice)
            });
        }

        if (this.canRecordPayment(invoice)) {
            items.push({
                label: this.i18n.t('common.actions.recordPayment'),
                icon: 'pi pi-dollar',
                command: () => this.openPaymentDialog(invoice)
            });
        }

        if (this.canApplyAdvanceCredit(invoice)) {
            items.push({
                label: this.i18n.t('common.actions.applyAdvanceCredit'),
                icon: 'pi pi-credit-card',
                command: () => this.applyAdvanceCredit(invoice)
            });
        }

        items.push({
            label: this.i18n.t('common.actions.viewConfirmPayments'),
            icon: 'pi pi-wallet',
            command: () => this.viewCustomerPayments(invoice)
        });

        return items;
    }

    openPaymentDialog(invoice: InvoiceResponse): void {
        this.selectedInvoice = invoice;
        this.paymentAmount = invoice.outstandingAmount > 0 ? invoice.outstandingAmount : 0;
        this.paymentMethod = 'CASH';
        this.paymentProviderId = null;
        this.paymentReference = '';
        this.paymentDate = new Date();
        this.showPaymentDialog = true;
    }

    closePaymentDialog(): void {
        this.showPaymentDialog = false;
        this.selectedInvoice = null;
        this.paymentAmount = 0;
        this.paymentMethod = 'CASH';
        this.paymentProviderId = null;
        this.paymentReference = '';
        this.paymentDate = new Date();
    }

    recordPayment(): void {
        if (!this.selectedInvoice) return;

        if (this.paymentAmount <= 0) {
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('common.messages.invalidInput')
            });
            return;
        }

        const provider = this.paymentProviders.find(p => p.value === this.paymentMethod);
        const providerId = provider?.id || null;

        if (this.paymentAmount > this.selectedInvoice.outstandingAmount) {
            const creditAmount = this.paymentAmount - this.selectedInvoice.outstandingAmount;
            this.messageService.add({
                severity: 'info',
                summary: this.i18n.t('common.messages.info'),
                detail: `Payment exceeds outstanding by ${this.formatCurrency(creditAmount)}. This will create a credit balance.`,
                life: 5000
            });
        }

        this.invoiceService
            .recordPayment(this.selectedInvoice.id, {
                amount: this.paymentAmount,
                paymentDate: this.paymentDate.toISOString(),
                paymentMethod: this.paymentMethod,
                referenceNumber: this.paymentReference,
                paymentProviderId: providerId || undefined
            })
            .subscribe({
                next: (response: any) => {
                    const message = response.message || `Payment of ${this.formatCurrency(this.paymentAmount)} recorded successfully`;
                    const severity = response.paymentStatus === 'PENDING' ? 'info' : 'success';
                    this.messageService.add({
                        severity,
                        summary: response.paymentStatus === 'PENDING' ? this.i18n.t('common.messages.info') : this.i18n.t('common.messages.success'),
                        detail: message,
                        life: response.paymentStatus === 'PENDING' ? 6000 : 3000
                    });
                    this.closePaymentDialog();
                    this.loadInvoices();
                },
                error: () => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('invoices.messages.recordPaymentFailed')
                    });
                }
            });
    }

    canRecordPayment(invoice: InvoiceResponse): boolean {
        return invoice.status !== 'CANCELLED' && invoice.status !== 'DRAFT';
    }

    viewSalesOrder(invoice: InvoiceResponse): void {
        this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id: invoice.salesOrderId } });
    }

    viewCustomerPayments(invoice: InvoiceResponse): void {
        this.router.navigate(['/sales/customer-payments'], { queryParams: { customerId: invoice.customerId } });
    }

    applyAdvanceCredit(invoice: InvoiceResponse): void {
        this.dialogRef = this.dialogService.open(ApplyCustomerAdvanceCreditDialogComponent, {
            header: `${this.i18n.t('common.actions.applyAdvanceCredit')} - ${invoice.invoiceNumber}`,
            width: '900px',
            data: {
                customerId: invoice.customerId,
                invoiceId: invoice.id,
                invoiceOutstandingAmount: invoice.outstandingAmount
            }
        });

        this.dialogRef?.onClose.subscribe((result) => {
            if (result) {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: result.message || this.i18n.t('common.messages.success')
                });
                this.loadInvoices();
            }
        });
    }

    canApplyAdvanceCredit(invoice: InvoiceResponse): boolean {
        return invoice.status !== 'CANCELLED' && invoice.status !== 'DRAFT' && invoice.outstandingAmount > 0;
    }
}
