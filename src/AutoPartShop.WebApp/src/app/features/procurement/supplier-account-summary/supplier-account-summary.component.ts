import { Component, OnInit, OnDestroy, inject, signal, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { DatePickerModule } from 'primeng/datepicker';
import { TooltipModule } from 'primeng/tooltip';
import { PaginatorModule, PaginatorState } from 'primeng/paginator';
import { SkeletonModule } from 'primeng/skeleton';
import { Subject, takeUntil, forkJoin } from 'rxjs';
import { map } from 'rxjs/operators';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { SupplierService, SupplierResponse } from '../../inventory/services/supplier.service';
import {
    SupplierLedgerService,
    SupplierLedgerSummaryDto,
    SupplierLedgerEntryDto,
    SupplierLedgerQueryDto,
    PagedLedgerResult,
    SupplierLedgerTransactionType
} from '../services/supplier-ledger.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { InvoicePdfService } from '../../sales/services/invoice-pdf.service';

@Component({
    selector: 'app-supplier-account-summary',
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
        LazyAutocompleteComponent
    ],
    providers: [MessageService],
    templateUrl: './supplier-account-summary.component.html',
    styleUrls: ['./supplier-account-summary.component.css']
})
export class SupplierAccountSummaryComponent implements OnInit, OnDestroy {
    @ViewChild(LazyAutocompleteComponent) supplierAutocomplete!: LazyAutocompleteComponent<SupplierResponse>;

    private readonly supplierService = inject(SupplierService);
    private readonly ledgerService = inject(SupplierLedgerService);
    private readonly invoicePdfService = inject(InvoicePdfService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly destroy$ = new Subject<void>();

    // Filter state
    selectedSupplier: SupplierResponse | null = null;
    fromDate: Date | null = null;
    toDate: Date | null = null;

    // Report state
    summary = signal<SupplierLedgerSummaryDto | null>(null);
    entries = signal<SupplierLedgerEntryDto[]>([]);
    totalEntryCount = signal(0);
    loading = signal(false);
    error = signal<string | null>(null);

    // PDF state
    pdfLoading = signal(false);
    allEntries = signal<SupplierLedgerEntryDto[]>([]);
    allEntriesLoaded = signal(false);

    // Pagination
    pageNumber = 1;
    pageSize = 20;
    first = 0;

    // Getter so the template always reads the latest DB-sourced values.
    get companyConfig() { return this.invoicePdfService.getCompanyConfig(); }
    today = new Date().toISOString();

    fetchSuppliersLazy = (req: LazyRequest) =>
        this.supplierService.getSuppliers({
            search: req.search || '',
            pageNumber: req.pageNumber,
            pageSize: req.pageSize
        }).pipe(
            map(res => ({
                items: res.data ?? [],
                totalCount: res.pagination?.totalCount ?? 0
            }) as LazyResponse<SupplierResponse>)
        );

    ngOnInit(): void {
        const supplierId = this.route.snapshot.queryParamMap.get('supplierId');
        if (supplierId) {
            this.supplierService.getSupplierById(supplierId).pipe(takeUntil(this.destroy$)).subscribe({
                next: (supplier) => {
                    this.selectedSupplier = supplier;
                    // Use setTimeout so the ViewChild is ready after view init
                    setTimeout(() => {
                        this.supplierAutocomplete?.writeValue(supplier);
                        this.generateReport();
                    }, 0);
                },
                error: () => {
                    this.messageService.add({ severity: 'warn', summary: 'Not Found', detail: 'Supplier not found', life: 3000 });
                }
            });
        }
    }

    onSupplierSelected(supplier: SupplierResponse): void {
        this.selectedSupplier = supplier;
    }

    onSupplierCleared(): void {
        this.selectedSupplier = null;
        this.summary.set(null);
        this.entries.set([]);
        this.totalEntryCount.set(0);
        this.allEntries.set([]);
        this.allEntriesLoaded.set(false);
    }

    generateReport(): void {
        if (!this.selectedSupplier) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'Please select a supplier',
                life: 3000
            });
            return;
        }

        this.pageNumber = 1;
        this.first = 0;
        this.allEntriesLoaded.set(false);
        this.loadReport();
    }

    private loadReport(): void {
        if (!this.selectedSupplier) return;

        this.loading.set(true);
        this.error.set(null);

        const supplierId = this.selectedSupplier.id;

        const query: SupplierLedgerQueryDto = {
            supplierId,
            pageNumber: this.pageNumber,
            pageSize: this.pageSize,
            fromDate: this.fromDate ? this.fromDate.toISOString() : undefined,
            toDate: this.toDate ? this.toDate.toISOString() : undefined
        };

        forkJoin({
            summary: this.ledgerService.getLedgerSummary(supplierId),
            pagedEntries: this.ledgerService.getLedgerEntries(query)
        })
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: ({ summary, pagedEntries }) => {
                    this.summary.set(summary);
                    this.entries.set(pagedEntries.entries);
                    this.totalEntryCount.set(pagedEntries.totalCount);
                    this.loading.set(false);
                },
                error: (err) => {
                    console.error('Error loading supplier account summary:', err);
                    this.error.set('Failed to load account summary. Please try again.');
                    this.loading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load supplier account summary',
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

    private loadAllEntriesThen(action: 'pdf' | 'print'): void {
        if (!this.selectedSupplier || !this.summary()) return;

        const total = this.totalEntryCount();

        if (total <= this.entries().length) {
            this.allEntries.set(this.entries());
            this.allEntriesLoaded.set(true);
            setTimeout(() => this.executeAction(action), 100);
            return;
        }

        this.pdfLoading.set(true);
        const query: SupplierLedgerQueryDto = {
            supplierId: this.selectedSupplier.id,
            pageNumber: 1,
            pageSize: total,
            fromDate: this.fromDate ? this.fromDate.toISOString() : undefined,
            toDate: this.toDate ? this.toDate.toISOString() : undefined
        };

        this.ledgerService
            .getLedgerEntries(query)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.allEntries.set(data.entries);
                    this.allEntriesLoaded.set(true);
                    setTimeout(() => this.executeAction(action), 200);
                },
                error: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load all entries for PDF',
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
        this.loadAllEntriesThen('pdf');
    }

    onPrint(): void {
        this.loadAllEntriesThen('print');
    }

    private async downloadPdf(): Promise<void> {
        try {
            const element = document.getElementById('supplier-account-summary-print');
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
            const supplierCode = s.supplierCode || 'supplier';
            const dateStr = new Date().toISOString().split('T')[0];
            pdf.save(`supplier-account-summary-${supplierCode}-${dateStr}.pdf`);

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
        const printContent = document.getElementById('supplier-account-summary-print');
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
                <title>Supplier Account Summary</title>
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
                    .print-supplier-info { display: flex; justify-content: space-between; margin-bottom: 16px; padding: 10px; background: #f9fafb; border: 1px solid #e5e7eb; border-radius: 4px; }
                    .print-supplier-info div { font-size: 12px; }
                    .print-supplier-name { font-size: 14px; font-weight: 600; color: #1f2937; }
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
                    .text-debit { color: #dc2626; }
                    .text-credit { color: #16a34a; }
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

    getTransactionTypeLabel(type: SupplierLedgerTransactionType): string {
        return this.ledgerService.getTransactionTypeLabel(type);
    }

    getTransactionTypeStatus(type: SupplierLedgerTransactionType): string {
        switch (type) {
            case SupplierLedgerTransactionType.PURCHASE:
                return 'purchase';
            case SupplierLedgerTransactionType.PAYMENT:
                return 'payment';
            case SupplierLedgerTransactionType.REFUND:
                return 'refund';
            case SupplierLedgerTransactionType.ADVANCE:
                return 'advance';
            case SupplierLedgerTransactionType.CANCELLATION:
                return 'cancellation';
            default:
                return 'default';
        }
    }

    goBack(): void {
        this.router.navigate(['/inventory/suppliers']);
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

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
