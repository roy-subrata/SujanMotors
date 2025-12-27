import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SupplierPaymentHistorySummary } from '../../services/supplier-payment.service';

@Component({
  selector: 'app-payment-metrics',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="grid grid-cols-12 gap-4" *ngIf="summary">
      <!-- Total Paid Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div class="metric-card bg-green-50 border-l-4 border-green-500">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Total Paid</p>
              <p class="text-2xl font-bold text-green-600">{{ summary.totalPaid | currency }}</p>
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
              <p class="text-2xl font-bold text-orange-600">{{ summary.totalDue | currency }}</p>
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
              <p class="text-2xl font-bold text-blue-600">{{ summary.totalAdvanceAmount | currency }}</p>
            </div>
            <i class="pi pi-wallet text-blue-500 text-3xl"></i>
          </div>
          <p class="text-xs text-gray-500 mt-2">Prepayments on file</p>
        </div>
      </div>

      <!-- Payment Balance Card -->
      <div class="col-span-12 sm:col-span-6 md:col-span-3">
        <div [ngClass]="getBelanceCardClass()">
          <div class="flex items-center justify-between">
            <div>
              <p class="text-gray-600 text-sm font-medium">Balance</p>
              <p [ngClass]="getBalanceTextColor()" class="text-2xl font-bold">
                {{ summary.paymentBalance | currency }}
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
      background: white;
    }

    .metric-card i {
      opacity: 0.6;
    }
  `]
})
export class PaymentMetricsComponent {
  @Input() summary!: SupplierPaymentHistorySummary;

  getBelanceCardClass(): string {
    if (this.summary.paymentBalance > 0) {
      return 'metric-card bg-red-50 border-l-4 border-red-500';
    } else if (this.summary.paymentBalance < 0) {
      return 'metric-card bg-green-50 border-l-4 border-green-500';
    }
    return 'metric-card bg-gray-50 border-l-4 border-gray-500';
  }

  getBalanceTextColor(): string {
    if (this.summary.paymentBalance > 0) {
      return 'text-red-600';
    } else if (this.summary.paymentBalance < 0) {
      return 'text-green-600';
    }
    return 'text-gray-600';
  }

  getBalanceIcon(): string {
    if (this.summary.paymentBalance > 0) {
      return 'pi pi-exclamation-triangle text-red-500';
    } else if (this.summary.paymentBalance < 0) {
      return 'pi pi-check-circle text-green-500';
    }
    return 'pi pi-minus-circle text-gray-500';
  }
}
