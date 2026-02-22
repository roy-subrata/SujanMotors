import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { Subject, takeUntil } from 'rxjs';
import { SmAutocompleteComponent, AutocompleteDataSource } from '../../../shared/components/sm-autocomplete/sm-autocomplete.component';
import { CustomerService, CustomerResponse } from '../services/customer.service';
import {
    CustomerAccountSummaryService,
    CustomerAccountSummary,
    CustomerAccountSummaryQuery,
    CustomerPurchaseItem
} from '../services/customer-account-summary.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { InvoicePdfService } from '../services/invoice-pdf.service';

@Component({
    selector: 'app-customer-account-summary',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ButtonModule,
        ToastModule,
        DatePickerModule,
        TooltipModule,
        PaginatorModule,
        SkeletonModule,
        SmAutocompleteComponent
    ],
    providers: [MessageService],
    templateUrl: './customer-account-summary.component.html',
    styleUrls: ['./customer-account-summary.component.css']
})
export class CustomerAccountSummaryComponent implements OnInit, OnDestroy {
    private readonly customerService = inject(CustomerService);
    private readonly summaryService = inject(CustomerAccountSummaryService);
    private readonly invoicePdfService = inject(InvoicePdfService);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly destroy$ = new Subject<void>();

    // Filter state
    selectedCustomer: CustomerResponse | null = null;
    fromDate: Date | null = null;
    toDate: Date | null = null;

    // Report state
    summary = signal<CustomerAccountSummary | null>(null);
    loading = signal(false);
    error = signal<string | null>(null);

    // PDF state
    pdfLoading = signal(false);
    allItems = signal<CustomerPurchaseItem[]>([]);
    allItemsLoaded = signal(false);

    // Pagination
    pageNumber = 1;
    pageSize = 20;
    first = 0;

    // Company config
    companyConfig = this.invoicePdfService.getCompanyConfig();

    // Customer autocomplete data source
    customerDataSource: AutocompleteDataSource<CustomerResponse> = {
        fetchData: (search, pageNumber, pageSize) => {
            return this.customerService.getCustomers({
                search,
                pageNumber,
                pageSize
            });
        },
        displayField: (item) => `${item.firstName} ${item.lastName}`,
        subtitleField: (item) => `${item.customerCode} | ${item.phone}`
    };

    ngOnInit(): void {}

    onCustomerSelected(customer: CustomerResponse): void {
        this.selectedCustomer = customer;
    }

    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.summary.set(null);
        this.allItems.set([]);
        this.allItemsLoaded.set(false);
    }

    generateReport(): void {
        if (!this.selectedCustomer) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'Please select a customer',
                life: 3000
            });
            return;
        }

        this.pageNumber = 1;
        this.first = 0;
        this.allItemsLoaded.set(false);
        this.loadReport();
    }

    private loadReport(): void {
        if (!this.selectedCustomer) return;

        this.loading.set(true);
        this.error.set(null);

        const query: CustomerAccountSummaryQuery = {
            customerId: this.selectedCustomer.id,
            fromDate: this.fromDate ? this.fromDate.toISOString() : undefined,
            toDate: this.toDate ? this.toDate.toISOString() : undefined,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize
        };

        this.summaryService
            .getAccountSummary(this.selectedCustomer.id, query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.summary.set(data);
                    this.loading.set(false);
                },
                error: (err) => {
                    console.error('Error loading account summary:', err);
                    this.error.set('Failed to load account summary. Please try again.');
                    this.loading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load customer account summary',
                        life: 5000
                    });
                }
            });
    }

    onPageChange(event: PaginatorState): void {
        this.pageNumber = (event.page ?? 0) + 1;
        this.pageSize = event.rows ?? 20;
        this.first = event.first ?? 0;
        this.loadReport();
    }

    /**
     * Load ALL items (not paginated) for PDF/Print, then trigger the action.
     */
    private loadAllItemsThen(action: 'pdf' | 'print'): void {
        if (!this.selectedCustomer || !this.summary()) return;

        const totalItems = this.summary()!.purchaseItemsTotalCount;

        // If all items already on current page, use them directly
        if (totalItems <= this.summary()!.purchaseItems.length) {
            this.allItems.set(this.summary()!.purchaseItems);
            this.allItemsLoaded.set(true);
            setTimeout(() => this.executeAction(action), 100);
            return;
        }

        // Fetch all items in one call
        this.pdfLoading.set(true);
        const query: CustomerAccountSummaryQuery = {
            customerId: this.selectedCustomer.id,
            fromDate: this.fromDate ? this.fromDate.toISOString() : undefined,
            toDate: this.toDate ? this.toDate.toISOString() : undefined,
            pageNumber: 1,
            pageSize: totalItems
        };

        this.summaryService
            .getAccountSummary(this.selectedCustomer.id, query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.allItems.set(data.purchaseItems);
                    this.allItemsLoaded.set(true);
                    // Wait for DOM to render the print section
                    setTimeout(() => this.executeAction(action), 200);
                },
                error: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load all items for PDF',
                        life: 5000
                    });
                }
            });
    }

    private executeAction(action: 'pdf' | 'print'): void {
        if (action === 'pdf') {
            this.downloadPdf();
        } else {
            this.printReport();
        }
    }

    onDownloadPdf(): void {
        this.loadAllItemsThen('pdf');
    }

    onPrint(): void {
        this.loadAllItemsThen('print');
    }

    private async downloadPdf(): Promise<void> {
        try {
            const element = document.getElementById('account-summary-print');
            if (!element) {
                this.pdfLoading.set(false);
                return;
            }

            const html2canvas = (await import('html2canvas')).default;
            const { jsPDF } = await import('jspdf');

            const canvas = await html2canvas(element, {
                scale: 2,
                useCORS: true,
                logging: false,
                backgroundColor: '#ffffff'
            });

            const imgData = canvas.toDataURL('image/png');
            const pdf = new jsPDF({
                orientation: 'portrait',
                unit: 'mm',
                format: 'a4'
            });

            const pdfWidth = pdf.internal.pageSize.getWidth();
            const pdfHeight = pdf.internal.pageSize.getHeight();
            const imgWidth = pdfWidth;
            const imgHeight = (canvas.height * pdfWidth) / canvas.width;

            // Handle multi-page
            let heightLeft = imgHeight;
            let position = 0;

            pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
            heightLeft -= pdfHeight;

            while (heightLeft > 0) {
                position = position - pdfHeight;
                pdf.addPage();
                pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
                heightLeft -= pdfHeight;
            }

            const s = this.summary()!;
            const customerCode = s.customerCode || 'customer';
            const dateStr = new Date().toISOString().split('T')[0];
            pdf.save(`account-summary-${customerCode}-${dateStr}.pdf`);

            this.pdfLoading.set(false);
            this.messageService.add({
                severity: 'success',
                summary: 'Success',
                detail: 'PDF downloaded successfully',
                life: 3000
            });
        } catch (err) {
            console.error('Error generating PDF:', err);
            this.pdfLoading.set(false);
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Failed to generate PDF',
                life: 5000
            });
        }
    }

    private printReport(): void {
        const printContent = document.getElementById('account-summary-print');
        if (!printContent) {
            this.pdfLoading.set(false);
            return;
        }

        const printWindow = window.open('', '_blank', 'width=800,height=600');
        if (!printWindow) {
            this.pdfLoading.set(false);
            return;
        }

        printWindow.document.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <title>Customer Account Summary</title>
                <style>
                    * { margin: 0; padding: 0; box-sizing: border-box; }
                    body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; padding: 10mm; }
                    @page { size: A4; margin: 10mm; }
                    @media print {
                        body { -webkit-print-color-adjust: exact !important; print-color-adjust: exact !important; }
                    }
                    .print-header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 12px; margin-bottom: 16px; }
                    .print-company-name { font-size: 20px; font-weight: 700; color: #1f2937; }
                    .print-company-info { font-size: 11px; color: #6b7280; margin-top: 4px; }
                    .print-title { font-size: 16px; font-weight: 600; margin-top: 10px; color: #374151; }
                    .print-customer-info { display: flex; justify-content: space-between; margin-bottom: 16px; padding: 10px; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 4px; }
                    .print-customer-info div { font-size: 12px; }
                    .print-customer-name { font-size: 14px; font-weight: 600; color: #1f2937; }
                    .print-metrics { display: flex; justify-content: space-between; margin-bottom: 16px; gap: 12px; }
                    .print-metric { flex: 1; padding: 10px; border: 1px solid #e5e7eb; border-radius: 4px; text-align: center; }
                    .print-metric-label { font-size: 10px; text-transform: uppercase; color: #6b7280; font-weight: 500; }
                    .print-metric-value { font-size: 16px; font-weight: 700; color: #1f2937; margin-top: 4px; }
                    .print-metric.due .print-metric-value { color: #dc2626; }
                    .print-metric.paid .print-metric-value { color: #16a34a; }
                    table { width: 100%; border-collapse: collapse; margin-top: 8px; font-size: 11px; }
                    th { background: #f3f4f6; font-weight: 600; text-align: left; padding: 8px 6px; border: 1px solid #d1d5db; }
                    td { padding: 6px; border: 1px solid #e5e7eb; }
                    tr:nth-child(even) { background: #f9fafb; }
                    .text-right { text-align: right; }
                    .font-bold { font-weight: 700; }
                    .print-table-title { font-size: 13px; font-weight: 600; margin-top: 16px; margin-bottom: 4px; }
                    .print-footer { margin-top: 24px; padding-top: 12px; border-top: 1px solid #e5e7eb; text-align: center; font-size: 10px; color: #9ca3af; }
                    .print-due-box { margin-top: 12px; padding: 10px; border: 2px solid #dc2626; border-radius: 4px; text-align: right; }
                    .print-due-box .label { font-size: 12px; color: #6b7280; }
                    .print-due-box .value { font-size: 18px; font-weight: 700; color: #dc2626; }
                </style>
            </head>
            <body>
                ${printContent.innerHTML}
                <script>
                    window.onload = function() { window.print(); window.close(); }
                </script>
            </body>
            </html>
        `);
        printWindow.document.close();
        this.pdfLoading.set(false);
    }

    goBack(): void {
        this.router.navigate(['/sales/customers']);
    }

    formatCurrency(value: number | undefined | null): string {
        const numValue = value ?? 0;
        if (isNaN(numValue)) {
            const currency = this.currencyService.selectedCurrency();
            return this.currencyService.formatCurrency(0, currency);
        }
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(numValue, currency);
    }

    formatDate(date: string | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    getStatusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast' {
        switch (status) {
            case 'PAID':
                return 'success';
            case 'ISSUED':
                return 'info';
            case 'PARTIALLY_PAID':
                return 'warn';
            case 'OVERDUE':
                return 'danger';
            case 'CANCELLED':
                return 'secondary';
            default:
                return 'info';
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
