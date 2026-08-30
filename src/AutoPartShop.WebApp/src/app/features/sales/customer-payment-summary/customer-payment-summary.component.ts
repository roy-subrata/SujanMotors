import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { SkeletonModule } from 'primeng/skeleton';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { Subject, takeUntil } from 'rxjs';
import { CustomerPaymentService, CustomerPaymentHistorySummary } from '../services/customer-payment.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
  selector: 'app-customer-payment-summary',
  standalone: true,
  imports: [
    CommonModule,
    ButtonModule,
    SkeletonModule,
    ToastModule,
    TooltipModule,
    TranslatePipe,
    PageContainerComponent,
    PageHeaderComponent
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
  private readonly i18n = inject(I18nService);
  private readonly destroy$ = new Subject<void>();

  customerId: string = '';
  customerName: string = '';
  summary: CustomerPaymentHistorySummary | null = null;
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.activatedRoute.params.pipe(takeUntil(this.destroy$)).subscribe((params) => {
      this.customerId = params['customerId'];
      if (this.customerId) {
        this.loadSummary();
      } else {
        this.error = this.i18n.t('customerPaymentSummary.messages.noCustomerId');
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
          this.error = this.i18n.t('customerPaymentSummary.messages.loadFailedInline');
          this.loading = false;
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('common.messages.error'),
            detail: this.i18n.t('customerPaymentSummary.messages.loadFailed'),
            life: 5000
          });
        }
      });
  }

  goBack(): void {
    if (this.customerId) {
      this.router.navigate(['/sales/customers/detail'], {
        queryParams: { id: this.customerId }
      });
    } else {
      this.router.navigate(['/sales/customers']);
    }
  }

  viewAllPayments(): void {
    this.router.navigate(['/sales/customer-payments'], {
      queryParams: { customerId: this.customerId }
    });
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
