import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { Subject, takeUntil, forkJoin } from 'rxjs';

import { SupplierPaymentService, SupplierPaymentHistorySummary } from '../services/supplier-payment.service';
import { SupplierLedgerService, SupplierLedgerSummaryDto } from '../services/supplier-ledger.service';
import { PaymentMetricsComponent } from './components/payment-metrics.component';
import { PaymentHistoryTableComponent } from './components/payment-history-table.component';
import { PaymentActionsComponent } from './components/payment-actions.component';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
    selector: 'app-supplier-payment-summary',
    standalone: true,
    imports: [CommonModule, ButtonModule, CardModule, SkeletonModule, ToastModule, ConfirmDialogModule, PaymentMetricsComponent, PaymentHistoryTableComponent, PaymentActionsComponent],
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
    useLedgerView = true;  // Use the new unified ledger view
    loading = true;
    error: string | null = null;

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

    private loadSummary(): void {
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
                                    debugger;
                    this.summary = paymentSummary;
                    this.ledgerSummary = ledgerSummary;
                    this.supplierName = ledgerSummary.supplierName || paymentSummary.supplierName;
                    this.loading = false;
                },
                error: (err) => {
                    console.error('Error loading payment summary:', err);
                    this.error = 'Failed to load payment summary. Please try again.';
                    this.loading = false;
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load supplier payment summary',
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
            const currency = this.currencyService.selectedCurrency();
            return this.currencyService.formatCurrency(0, currency);
        }
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(numValue, currency);
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
