import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SupplierPaymentHistorySummary } from '../../services/supplier-payment.service';
import { SupplierLedgerSummaryDto } from '../../services/supplier-ledger.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

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
              <p class="text-gray-600 text-sm font-medium">Total Purchases</p>
              <p class="text-2xl font-bold text-red-600">{{ formatCurrency(ledgerSummary.totalPurchases) }}</p>
            </div>
            <i class="pi pi-shopping-cart text-red-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Confirmed purchase orders</p>
        </div>
      </div>

      <!-- Total Payments Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-green-50 border-l-4 border-green-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Total Payments</p>
              <p class="text-2xl font-bold text-green-600">{{ formatCurrency(ledgerSummary.totalPayments) }}</p>
            </div>
            <i class="pi pi-check-circle text-green-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Completed payments</p>
        </div>
      </div>

      <!-- Total Refunds Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="ledgerSummary.totalRefunds > 0">
        <div class="metric-card bg-purple-50 border-l-4 border-purple-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Purchase Returns</p>
              <p class="text-2xl font-bold text-purple-600">{{ formatCurrency(ledgerSummary.totalRefunds) }}</p>
            </div>
            <i class="pi pi-replay text-purple-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Settled refunds</p>
        </div>
      </div>

      <!-- Available Advance Credit Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="ledgerSummary.availableAdvanceCredit > 0">
        <div class="metric-card bg-blue-50 border-l-4 border-blue-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Advance Credit</p>
              <p class="text-2xl font-bold text-blue-600">{{ formatCurrency(ledgerSummary.availableAdvanceCredit) }}</p>
            </div>
            <i class="pi pi-wallet text-blue-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Available to apply</p>
        </div>
      </div>

      <!-- Current Balance Card (Calculated) -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div [ngClass]="getLedgerBalanceCardClass()">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Current Balance</p>
              <p [ngClass]="getLedgerBalanceTextColor()" class="text-2xl font-bold">
                {{ formatCurrency(ledgerSummary.currentBalance) }}
              </p>
            </div>
            <i [ngClass]="getLedgerBalanceIcon()" class="text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">
            {{ ledgerSummary.currentBalance > 0 ? 'Amount owed to supplier' : 'Overpaid / Credit' }}
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
              <p class="text-gray-600 text-sm font-medium">Total Paid</p>
              <p class="text-2xl font-bold text-green-600">{{ formatCurrency(summary.totalPaid) }}</p>
            </div>
            <i class="pi pi-check-circle text-green-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ summary.completedPayments }} completed payments</p>
        </div>
      </div>

      <!-- Total Due Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-orange-50 border-l-4 border-orange-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Total Due</p>
              <p class="text-2xl font-bold text-orange-600">{{ formatCurrency(summary.totalDue) }}</p>
            </div>
            <i class="pi pi-exclamation-circle text-orange-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ summary.outstandingInvoiceCount }} outstanding invoices</p>
        </div>
      </div>

      <!-- Total Advance Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-blue-50 border-l-4 border-blue-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Advance Amount</p>
              <p class="text-2xl font-bold text-blue-600">{{ formatCurrency(summary.totalAdvanceAmount) }}</p>
            </div>
            <i class="pi pi-wallet text-blue-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Prepayments on file</p>
        </div>
      </div>

      <!-- Total Refunds Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3" *ngIf="summary.totalRefunds > 0 || summary.returnedPayments > 0">
        <div class="metric-card bg-purple-50 border-l-4 border-purple-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Purchase Returns</p>
              <p class="text-2xl font-bold text-purple-600">{{ formatCurrency(summary.totalRefunds) }}</p>
            </div>
            <i class="pi pi-replay text-purple-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">{{ summary.returnedPayments }} refunds processed</p>
        </div>
      </div>

      <!-- Payment Balance Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div [ngClass]="getBelanceCardClass()">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Outstanding Balance</p>
              <p [ngClass]="getBalanceTextColor()" class="text-2xl font-bold">
                {{ formatCurrency(summary.paymentBalance) }}
              </p>
            </div>
            <i [ngClass]="getBalanceIcon()" class="text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">
            {{ summary.paymentBalance > 0 ? 'Amount due' : 'Credit balance' }}
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
