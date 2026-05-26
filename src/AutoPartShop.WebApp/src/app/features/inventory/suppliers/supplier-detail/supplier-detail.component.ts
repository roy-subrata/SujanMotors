import { Component, OnInit, inject, signal, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { AvatarModule } from 'primeng/avatar';
import { MessageService } from 'primeng/api';
import { SupplierService, SupplierResponse } from '../../services/supplier.service';
import { SupplierPaymentService, SupplierPaymentHistorySummary } from '../../../procurement/services/supplier-payment.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

@Component({
    selector: 'app-supplier-detail',
    standalone: true,
    imports: [CommonModule, ButtonModule, TagModule, ToastModule, TooltipModule, AvatarModule],
    providers: [MessageService],
    templateUrl: './supplier-detail.component.html',
    styleUrls: ['./supplier-detail.component.css']
})
export class SupplierDetailComponent implements OnInit {
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);
    private readonly supplierService = inject(SupplierService);
    private readonly paymentService = inject(SupplierPaymentService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly destroyRef = inject(DestroyRef);

    supplierId = signal<string>('');
    supplier = signal<SupplierResponse | null>(null);
    paymentSummary = signal<SupplierPaymentHistorySummary | null>(null);
    loading = signal(true);

    ngOnInit(): void {
        this.route.queryParams.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
            const id = params['id'];
            if (id) {
                this.supplierId.set(id);
                this.loadSupplierData();
            } else {
                this.router.navigate(['/inventory/suppliers']);
            }
        });
    }

    loadSupplierData(): void {
        this.loading.set(true);

        this.supplierService.getSupplierById(this.supplierId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: (supplier) => {
                this.supplier.set(supplier);
                this.loading.set(false);
            },
            error: (error) => {
                this.loading.set(false);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load supplier details'
                });
                console.error('Error loading supplier:', error);
            }
        });

        this.paymentService.getSupplierPaymentSummary(this.supplierId()).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: (summary) => {
                this.paymentSummary.set(summary);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load payment summary'
                });
                console.error('Error loading payment summary:', error);
            }
        });
    }

    getInitials(supplier: SupplierResponse): string {
        const name = supplier.name || '';
        const parts = name.split(' ');
        if (parts.length >= 2) {
            return `${parts[0].charAt(0)}${parts[1].charAt(0)}`.toUpperCase();
        }
        return name.charAt(0).toUpperCase() || 'S';
    }

    getStatusSeverity(isActive: boolean): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        return isActive ? 'success' : 'danger';
    }

    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount ?? 0, currency);
    }

    editSupplier(): void {
        this.router.navigate(['/inventory/suppliers/edit'], {
            queryParams: { id: this.supplierId(), mode: 'edit' }
        });
    }

    recordPayment(): void {
        this.router.navigate(['/procurement/supplier-payments/new'], {
            queryParams: { supplierId: this.supplierId() }
        });
    }

    viewPayments(): void {
        this.router.navigate(['/procurement/supplier-payments'], {
            queryParams: { supplierId: this.supplierId() }
        });
    }

    viewPaymentAccounts(): void {
        this.router.navigate(['/inventory/suppliers/payment-accounts'], {
            queryParams: { supplierId: this.supplierId() }
        });
    }

    viewAccountSummary(): void {
        this.router.navigate(['/procurement/supplier-account-summary'], {
            queryParams: { supplierId: this.supplierId() }
        });
    }

    goBack(): void {
        this.router.navigate(['/inventory/suppliers']);
    }
}
