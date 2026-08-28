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
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { I18nService } from '@/shared/services/i18n.service';

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
        LazyAutocompleteComponent,
        PageHeaderComponent,
        PageContainerComponent,
        DataPaginationComponent,
        TranslatePipe
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
    private readonly i18n = inject(I18nService);
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
                    this.messageService.add({ severity: 'warn', summary: this.i18n.t('supplierAccountSummary.messages.notFound'), detail: this.i18n.t('supplierAccountSummary.messages.supplierNotFound'), life: 3000 });
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
    }

    generateReport(): void {
        if (!this.selectedSupplier) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('supplierAccountSummary.messages.selectSupplier'),
                life: 3000
            });
            return;
        }

        this.pageNumber = 1;
        this.first = 0;
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
                    this.error.set(this.i18n.t('supplierAccountSummary.messages.loadErrorHint'));
                    this.loading.set(false);
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('supplierAccountSummary.messages.loadFailed'),
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

    goToPage(page: number): void {
        this.onPageChange({ page: page - 1, rows: this.pageSize, first: (page - 1) * this.pageSize } as PaginatorState);
    }

    onPageSizeChange(size: number): void {
        this.pageSize = size;
        this.onPageChange({ page: 0, rows: size, first: 0 } as PaginatorState);
    }

    /** Server-rendered QuestPDF statement — the full ledger, not just the on-screen page of entries. */
    onDownloadPdf(): void {
        if (!this.selectedSupplier) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.warning'), detail: this.i18n.t('supplierAccountSummary.messages.selectSupplier'), life: 3000 });
            return;
        }

        this.pdfLoading.set(true);
        this.ledgerService
            .downloadStatementPdf(
                this.selectedSupplier.id,
                this.selectedSupplier.code,
                this.fromDate ? this.fromDate.toISOString() : undefined,
                this.toDate ? this.toDate.toISOString() : undefined
            )
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('supplierAccountSummary.messages.pdfSuccess'), life: 3000 });
                },
                error: () => {
                    this.pdfLoading.set(false);
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('supplierAccountSummary.messages.pdfFailed'), life: 5000 });
                }
            });
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
