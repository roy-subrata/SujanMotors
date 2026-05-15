import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Select } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { Subject, takeUntil, forkJoin } from 'rxjs';

import { SupplierPaymentService, SupplierPaymentHistorySummary } from '../services/supplier-payment.service';
import { SupplierLedgerService, SupplierLedgerSummaryDto, SupplierLedgerTransactionType, SupplierLedgerEntryDto, SupplierLedgerQueryDto } from '../services/supplier-ledger.service';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
    selector: 'app-supplier-payment-summary',
    standalone: true,
    imports: [CommonModule, FormsModule, ButtonModule, SkeletonModule, ToastModule, TableModule, TagModule, Select, DatePicker],
    providers: [MessageService],
    templateUrl: './supplier-payment-summary.component.html',
    styleUrls: ['./supplier-payment-summary.component.css']
})
export class SupplierPaymentSummaryComponent implements OnInit, OnDestroy {
    private readonly supplierPaymentService = inject(SupplierPaymentService);
    private readonly supplierLedgerService = inject(SupplierLedgerService);
    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly currencyService = inject(CurrencyService);
    private readonly destroy$ = new Subject<void>();

    supplierId: string = '';
    supplierName: string = 'Supplier';
    summary: SupplierPaymentHistorySummary | null = null;
    ledgerSummary: SupplierLedgerSummaryDto | null = null;
    loading = true;
    error: string | null = null;

    // Ledger filter state
    ledgerEntries: SupplierLedgerEntryDto[] = [];
    ledgerFilterType: SupplierLedgerTransactionType | null = null;
    ledgerDateRange: Date[] = [];
    ledgerLoading = false;
    ledgerPageNumber = 1;
    ledgerPageSize = 10;
    ledgerTotalCount = 0;
    filtersActive = false;

    transactionTypeOptions = [
        { label: 'All Types', value: null },
        { label: 'Purchase', value: SupplierLedgerTransactionType.PURCHASE },
        { label: 'Payment', value: SupplierLedgerTransactionType.PAYMENT },
        { label: 'Refund', value: SupplierLedgerTransactionType.REFUND },
        { label: 'Advance', value: SupplierLedgerTransactionType.ADVANCE },
        { label: 'Cancellation', value: SupplierLedgerTransactionType.CANCELLATION }
    ];

    ngOnInit(): void {
        // Get supplierId from route params
        this.activatedRoute.params.pipe(takeUntil(this.destroy$)).subscribe((params) => {
            this.supplierId = params['supplierId'];
            if (this.supplierId) {
                this.loadSummary();
            } else {
                this.error = 'Supplier ID not provided';
                this.loading = false;
            }
        });
    }

    loadSummary(): void {
        this.loading = true;
        this.error = null;

        // Load both legacy payment summary and new ledger summary
        forkJoin({
            paymentSummary: this.supplierPaymentService.getSupplierPaymentSummary(this.supplierId),
            ledgerSummary: this.supplierLedgerService.getLedgerSummary(this.supplierId, 20)
        })
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: ({ paymentSummary, ledgerSummary }) => {
                    this.summary = paymentSummary;
                    this.ledgerSummary = ledgerSummary;
                    this.ledgerEntries = ledgerSummary.entries || [];
                    this.ledgerTotalCount = ledgerSummary.entries?.length || 0;
                    this.supplierName = ledgerSummary.supplierName || paymentSummary.supplierName;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading payment summary:', err);
                    this.error = typeof err?.error === 'string' ? err.error : (err?.error?.message || 'Failed to load payment summary. Please try again.');
                    this.loading = false;
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: this.error!,
                        life: 5000
                    });
                }
            });
    }

    goBack(): void {
        this.router.navigate(['/procurement/supplier-payments']);
    }

    formatCurrency(value: number | undefined | null): string {
        const numValue = value ?? 0;
        if (isNaN(numValue)) {
            return this.currencyService.formatCurrency(0, this.currencyService.selectedCurrency());
        }
        return this.currencyService.formatCurrency(numValue, this.currencyService.selectedCurrency());
    }

    formatDate(date: string | undefined): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN', {
            year: 'numeric',
            month: 'short',
            day: 'numeric'
        });
    }

    getStatusSeverity(status: string): 'secondary' | 'info' | 'success' | 'danger' | 'warn' {
        switch (status) {
            case 'PENDING': return 'secondary';
            case 'PROCESSING': return 'info';
            case 'COMPLETED': return 'success';
            case 'FAILED': return 'danger';
            case 'CANCELLED': return 'danger';
            case 'REFUNDED': case 'RETURNED': return 'warn';
            default: return 'info';
        }
    }

    getLedgerTypeLabel(type: SupplierLedgerTransactionType | string): string {
        switch (type) {
            case SupplierLedgerTransactionType.PURCHASE: return 'Purchase';
            case SupplierLedgerTransactionType.PAYMENT: return 'Payment';
            case SupplierLedgerTransactionType.REFUND: return 'Refund';
            case SupplierLedgerTransactionType.ADVANCE: return 'Advance';
            case SupplierLedgerTransactionType.CANCELLATION: return 'Cancelled';
            default: return type?.toString() || '';
        }
    }

    getLedgerTypeSeverity(type: SupplierLedgerTransactionType | string): 'secondary' | 'info' | 'success' | 'danger' | 'warn' {
        switch (type) {
            case SupplierLedgerTransactionType.PURCHASE: return 'danger';
            case SupplierLedgerTransactionType.PAYMENT: return 'success';
            case SupplierLedgerTransactionType.REFUND: return 'warn';
            case SupplierLedgerTransactionType.ADVANCE: return 'info';
            case SupplierLedgerTransactionType.CANCELLATION: return 'secondary';
            default: return 'secondary';
        }
    }

    viewAllPayments(): void {
        this.router.navigate(['/procurement/supplier-payments'], {
            queryParams: { supplierId: this.supplierId }
        });
    }

    onLedgerFilterChange(): void {
        if (this.ledgerFilterType || (this.ledgerDateRange?.length === 2 && this.ledgerDateRange[0] && this.ledgerDateRange[1])) {
            this.filtersActive = true;
            this.ledgerPageNumber = 1;
            this.loadFilteredLedger();
        } else {
            this.clearLedgerFilters();
        }
    }

    clearLedgerFilters(): void {
        this.ledgerFilterType = null;
        this.ledgerDateRange = [];
        this.filtersActive = false;
        this.ledgerPageNumber = 1;
        if (this.ledgerSummary) {
            this.ledgerEntries = this.ledgerSummary.entries || [];
            this.ledgerTotalCount = this.ledgerSummary.entries?.length || 0;
        }
    }

    private loadFilteredLedger(): void {
        this.ledgerLoading = true;
        const query: SupplierLedgerQueryDto = {
            supplierId: this.supplierId,
            pageNumber: this.ledgerPageNumber,
            pageSize: this.ledgerPageSize
        };
        if (this.ledgerFilterType) {
            query.transactionType = this.ledgerFilterType;
        }
        if (this.ledgerDateRange?.length === 2 && this.ledgerDateRange[0] && this.ledgerDateRange[1]) {
            query.fromDate = this.formatDateForApi(this.ledgerDateRange[0]);
            query.toDate = this.formatDateForApi(this.ledgerDateRange[1]);
        }
        this.supplierLedgerService.getLedgerEntries(query).pipe(takeUntil(this.destroy$)).subscribe({
            next: (result) => {
                this.ledgerEntries = result.entries;
                this.ledgerTotalCount = result.totalCount;
                this.ledgerLoading = false;
            },
            error: (err) => {
                console.error('Error loading filtered ledger:', err);
                this.ledgerLoading = false;
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || 'Failed to load ledger entries'),
                    life: 5000
                });
            }
        });
    }

    private formatDateForApi(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
