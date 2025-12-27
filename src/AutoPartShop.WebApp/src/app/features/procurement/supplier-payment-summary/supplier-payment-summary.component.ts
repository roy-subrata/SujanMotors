import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { Subject, takeUntil } from 'rxjs';

import { SupplierPaymentService, SupplierPaymentHistorySummary } from '../services/supplier-payment.service';
import { PaymentMetricsComponent } from './components/payment-metrics.component';
import { PaymentStatusChartComponent } from './components/payment-status-chart.component';
import { PaymentHistoryTableComponent } from './components/payment-history-table.component';
import { CreditInfoComponent } from './components/credit-info.component';
import { PaymentActionsComponent } from './components/payment-actions.component';

@Component({
    selector: 'app-supplier-payment-summary',
    standalone: true,
    imports: [CommonModule, ButtonModule, CardModule, SkeletonModule, ToastModule, ConfirmDialogModule, PaymentMetricsComponent, PaymentStatusChartComponent, PaymentHistoryTableComponent, CreditInfoComponent, PaymentActionsComponent],
    providers: [MessageService],
    templateUrl: './supplier-payment-summary.component.html',
    styleUrls: ['./supplier-payment-summary.component.css']
})
export class SupplierPaymentSummaryComponent implements OnInit, OnDestroy {
    private readonly supplierPaymentService = inject(SupplierPaymentService);
    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly messageService = inject(MessageService);
    private readonly destroy$ = new Subject<void>();

    supplierId: string = '';
    supplierName: string = 'Supplier';
    summary: SupplierPaymentHistorySummary | null = null;
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

        this.supplierPaymentService
            .getSupplierPaymentSummary(this.supplierId)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (data) => {
                    this.summary = data;
                    this.supplierName = data.supplierName;
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

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }
}
