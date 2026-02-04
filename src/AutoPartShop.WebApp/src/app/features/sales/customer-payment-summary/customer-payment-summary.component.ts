import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { Subject, takeUntil } from 'rxjs';
import { CustomerPaymentService, CustomerPaymentHistorySummary } from '../services/customer-payment.service';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
  selector: 'app-customer-payment-summary',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    CardModule,
    SkeletonModule,
    ToastModule,
    ConfirmDialogModule,
    TableModule,
    TagModule
  ],
  providers: [MessageService],
  templateUrl: './customer-payment-summary.component.html',
  styleUrls: ['./customer-payment-summary.component.css']
})
export class CustomerPaymentSummaryComponent implements OnInit, OnDestroy {
  private readonly customerPaymentService = inject(CustomerPaymentService);
  private readonly activatedRoute = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly currencyService = inject(CurrencyService);
  private readonly destroy$ = new Subject<void>();

  customerId: string = '';
  customerName: string = 'Customer';
  summary: CustomerPaymentHistorySummary | null = null;
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    // Get customerId from route params
    this.activatedRoute.params.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      this.customerId = params['customerId'];
      if (this.customerId) {
        this.loadSummary();
      } else {
        this.error = 'Customer ID not provided';
        this.loading = false;
      }
    });
  }

  private loadSummary(): void {
    this.loading = true;
    this.error = null;

    this.customerPaymentService
      .getCustomerPaymentSummary(this.customerId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (data) => {
          this.summary = data;
          this.customerName = data.customerName;
          this.loading = false;
        },
        error: (err) => {
          console.error('Error loading payment summary:', err);
          this.error = 'Failed to load payment summary. Please try again.';
          this.loading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load customer payment summary',
            life: 5000
          });
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/sales/customer-payments']);
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

  getStatusSeverity(status: string): string {
    switch (status) {
      case 'PENDING':
        return 'secondary';
      case 'PROCESSING':
        return 'info';
      case 'COMPLETED':
        return 'success';
      case 'FAILED':
        return 'danger';
      case 'CANCELLED':
        return 'danger';
      case 'REFUNDED':
        return 'warning';
      default:
        return 'info';
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
