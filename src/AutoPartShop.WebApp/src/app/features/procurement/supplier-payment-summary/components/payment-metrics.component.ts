import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SupplierPaymentHistorySummary } from '../../services/supplier-payment.service';
import { SupplierLedgerSummaryDto } from '../../services/supplier-ledger.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-payment-metrics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <!-- Ledger-based Metrics -->
    <div class="grid grid-cols-12 gap-4" *ngIf="ledgerSummary">
      <!-- Total Purchases Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-red-50 border-l-4 border-red-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierAccountSummary.totalPurchases') }}</p>
              <p class="text-2xl font-bold text-red-600">{{ formatCurrency(ledgerSummary.totalPurchases) }}</p>
            </div>
            <i class="pi pi-shopping-cart text-red-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.confirmedPos') }}</p>
        </div>
      </div>

      <!-- Total Payments Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-green-50 border-l-4 border-green-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierAccountSummary.totalPayments') }}</p>
              <p class="text-2xl font-bold text-green-600">{{ formatCurrency(ledgerSummary.totalPayments) }}</p>
            </div>
            <i class="pi pi-check-circle text-green-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.completedPayments') }}</p>
        </div>
      </div>

      <!-- Total Refunds Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="ledgerSummary.totalRefunds > 0">
        <div class="metric-card bg-purple-50 border-l-4 border-purple-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.metrics.purchaseReturns') }}</p>
              <p class="text-2xl font-bold text-purple-600">{{ formatCurrency(ledgerSummary.totalRefunds) }}</p>
            </div>
            <i class="pi pi-replay text-purple-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.settledRefunds') }}</p>
        </div>
      </div>

      <!-- Available Advance Credit Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="ledgerSummary.availableAdvanceCredit > 0">
        <div class="metric-card bg-blue-50 border-l-4 border-blue-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.advanceCredit') }}</p>
              <p class="text-2xl font-bold text-blue-600">{{ formatCurrency(ledgerSummary.availableAdvanceCredit) }}</p>
            </div>
            <i class="pi pi-wallet text-blue-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.availableToApply') }}</p>
        </div>
      </div>

      <!-- Current Balance Card (Calculated) -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div [ngClass]="getLedgerBalanceCardClass()">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierAccountSummary.currentBalance') }}</p>
              <p [ngClass]="getLedgerBalanceTextColor()" class="text-2xl font-bold">
                {{ formatCurrency(ledgerSummary.currentBalance) }}
              </p>
            </div>
            <i [ngClass]="getLedgerBalanceIcon()" class="text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">
            {{ ledgerSummary.currentBalance > 0 ? i18n.t('supplierPaymentSummary.owedToSupplier') : i18n.t('supplierPaymentSummary.metrics.overpaidCredit') }}
          </p>
        </div>
      </div>
    </div>

    <!-- Legacy Payment-based Metrics (fallback) -->
    <div class="grid grid-cols-12 gap-4" *ngIf="!ledgerSummary && summary">
      <!-- Total Paid Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-green-50 border-l-4 border-green-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.totalPaid') }}</p>
              <p class="text-2xl font-bold text-green-600">{{ formatCurrency(summary.totalPaid) }}</p>
            </div>
            <i class="pi pi-check-circle text-green-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.completedCount', { count: summary.completedPayments }) }}</p>
        </div>
      </div>

      <!-- Total Due Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-orange-50 border-l-4 border-orange-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.totalDue') }}</p>
              <p class="text-2xl font-bold text-orange-600">{{ formatCurrency(summary.totalDue) }}</p>
            </div>
            <i class="pi pi-exclamation-circle text-orange-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.outstandingInvoicesCount', { count: summary.outstandingInvoiceCount }) }}</p>
        </div>
      </div>

      <!-- Total Advance Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-blue-50 border-l-4 border-blue-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.advanceAmount') }}</p>
              <p class="text-2xl font-bold text-blue-600">{{ formatCurrency(summary.totalAdvanceAmount) }}</p>
            </div>
            <i class="pi pi-wallet text-blue-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.prepaymentsOnFile') }}</p>
        </div>
      </div>

      <!-- Total Refunds Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="summary.totalRefunds > 0 || summary.returnedPayments > 0">
        <div class="metric-card bg-purple-50 border-l-4 border-purple-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierPaymentSummary.metrics.purchaseReturns') }}</p>
              <p class="text-2xl font-bold text-purple-600">{{ formatCurrency(summary.totalRefunds) }}</p>
            </div>
            <i class="pi pi-replay text-purple-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ i18n.t('supplierPaymentSummary.metrics.refundsProcessed', { count: summary.returnedPayments }) }}</p>
        </div>
      </div>

      <!-- Payment Balance Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div [ngClass]="getBelanceCardClass()">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">{{ i18n.t('supplierAccountSummary.outstandingBalance') }}</p>
              <p [ngClass]="getBalanceTextColor()" class="text-2xl font-bold">
                {{ formatCurrency(summary.paymentBalance) }}
              </p>
            </div>
            <i [ngClass]="getBalanceIcon()" class="text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">
            {{ summary.paymentBalance > 0 ? i18n.t('supplierPaymentSummary.amountDue') : i18n.t('supplierPaymentSummary.metrics.creditBalance') }}
          </p>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .metric-card {
      padding: 1.25rem;
      border-radius: 0.375rem;
      background: var(--surface-card);
    }

    .metric-card i {
      opacity: 0.6;
    }
  `]
})
export class PaymentMetricsComponent {
  @Input() summary!: SupplierPaymentHistorySummary;
  @Input() ledgerSummary?: SupplierLedgerSummaryDto;

  private readonly currencyService = inject(CurrencyService);
  protected readonly i18n = inject(I18nService);

  formatCurrency(value: number): string {
    return this.currencyService.formatCurrency(value ?? 0, this.currencyService.selectedCurrency());
  }

  // Legacy payment-based methods
  getBelanceCardClass(): string {
    if (this.summary?.paymentBalance > 0) {
      return 'metric-card bg-red-50 border-l-4 border-red-500';
    } else if (this.summary?.paymentBalance < 0) {
      return 'metric-card bg-green-50 border-l-4 border-green-500';
    }
    return 'metric-card bg-gray-50 border-l-4 border-gray-500';
  }

  getBalanceTextColor(): string {
    if (this.summary?.paymentBalance > 0) {
      return 'text-red-600';
    } else if (this.summary?.paymentBalance < 0) {
      return 'text-green-600';
    }
    return 'text-gray-600';
  }

  getBalanceIcon(): string {
    if (this.summary?.paymentBalance > 0) {
      return 'pi pi-exclamation-triangle text-red-500';
    } else if (this.summary?.paymentBalance < 0) {
      return 'pi pi-check-circle text-green-500';
    }
    return 'pi pi-minus-circle text-gray-500';
  }

  // Ledger-based methods
  getLedgerBalanceCardClass(): string {
    const balance = this.ledgerSummary?.currentBalance ?? 0;
    if (balance > 0) {
      return 'metric-card bg-red-50 border-l-4 border-red-500';
    } else if (balance < 0) {
      return 'metric-card bg-green-50 border-l-4 border-green-500';
    }
    return 'metric-card bg-gray-50 border-l-4 border-gray-500';
  }

  getLedgerBalanceTextColor(): string {
    const balance = this.ledgerSummary?.currentBalance ?? 0;
    if (balance > 0) {
      return 'text-red-600';
    } else if (balance < 0) {
      return 'text-green-600';
    }
    return 'text-gray-600';
  }

  getLedgerBalanceIcon(): string {
    const balance = this.ledgerSummary?.currentBalance ?? 0;
    if (balance > 0) {
      return 'pi pi-exclamation-triangle text-red-500';
    } else if (balance < 0) {
      return 'pi pi-check-circle text-green-500';
    }
    return 'pi pi-minus-circle text-gray-500';
  }
}
