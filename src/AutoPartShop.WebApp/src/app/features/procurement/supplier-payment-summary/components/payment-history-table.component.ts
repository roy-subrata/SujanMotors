import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentHistoryItem } from '../../services/supplier-payment.service';
import { SupplierLedgerEntryDto, SupplierLedgerTransactionType } from '../../services/supplier-ledger.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

@Component({
  selector: 'app-payment-history-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <h3 class="text-lg font-bold mb-4">{{ useLedger ? 'Transaction Ledger' : 'Payment History' }} (Last {{ entryLimit }})</h3>

      <!-- Ledger View -->
      <ng-container *ngIf="useLedger">
        <div *ngIf="!ledgerEntries || ledgerEntries.length === 0" class="text-center py-8 text-gray-500">
          No transactions available
        </div>

        <div *ngIf="ledgerEntries && ledgerEntries.length > 0" class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b-2 border-gray-300">
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Date</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Type</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Reference</th>
                <th class="text-right py-3 px-3 font-semibold text-gray-700">Debit</th>
                <th class="text-right py-3 px-3 font-semibold text-gray-700">Credit</th>
                <th class="text-right py-3 px-3 font-semibold text-gray-700">Balance</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Status</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let entry of ledgerEntries" class="border-b border-gray-200 hover:bg-gray-50">
                <td class="py-3 px-3 text-sm">{{ formatDate(entry.transactionDate) }}</td>
                <td class="py-3 px-3 text-sm">
                  <span [ngClass]="getLedgerTypeClass(entry.transactionType)">
                    {{ getLedgerTypeLabel(entry.transactionType) }}
                  </span>
                </td>
                <td class="py-3 px-3 text-sm">
                  <div [ngClass]="getReferenceClass(entry.transactionType)" class="font-medium">
                    {{ entry.referenceNumber }}
                  </div>
                  <div class="text-xs text-gray-500 mt-1">{{ entry.description }}</div>
                </td>
                <td class="py-3 px-3 text-sm text-right font-semibold text-red-600">
                  {{ entry.debitAmount > 0 ? formatCurrency(entry.debitAmount) : '' }}
                </td>
                <td class="py-3 px-3 text-sm text-right font-semibold text-green-600">
                  {{ entry.creditAmount > 0 ? formatCurrency(entry.creditAmount) : '' }}
                </td>
                <td class="py-3 px-3 text-sm text-right font-semibold" [ngClass]="entry.runningBalance > 0 ? 'text-red-600' : 'text-green-600'">
                  {{ formatCurrency(entry.runningBalance) }}
                </td>
                <td class="py-3 px-3 text-sm">
                  <span [ngClass]="getStatusBadgeClass(entry.status)">
                    {{ entry.status }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </ng-container>

      <!-- Payment History View (legacy) -->
      <ng-container *ngIf="!useLedger">
        <div *ngIf="!payments || payments.length === 0" class="text-center py-8 text-gray-500">
          No payment history available
        </div>

        <div *ngIf="payments && payments.length > 0" class="overflow-x-auto">
          <table class="w-full">
            <thead>
              <tr class="border-b-2 border-gray-300">
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Date</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Amount</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Type</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Method</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Reference</th>
                <th class="text-left py-3 px-3 font-semibold text-gray-700">Status</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let payment of payments" class="border-b border-gray-200 hover:bg-gray-50">
                <td class="py-3 px-3 text-sm">{{ formatDate(payment.paymentDate) }}</td>
                <td class="py-3 px-3 text-sm font-semibold" [class.text-purple-600]="isRefundPayment(payment)">
                  {{ isRefundPayment(payment) ? '-' : '' }}{{ formatCurrency(payment.amount) }}
                </td>
                <td class="py-3 px-3 text-sm">
                  <span [ngClass]="getPaymentTypeClass(isRefundPayment(payment) ? 'REFUND' : payment.paymentType)">
                    {{ isRefundPayment(payment) ? 'REFUND' : payment.paymentType }}
                  </span>
                </td>
                <td class="py-3 px-3 text-sm">
                  <div [ngClass]="getPaymentMethodClass(payment.paymentMethod)">{{ payment.paymentMethod }}</div>
                  <div *ngIf="payment.sourceAdvanceTransactionNumber" class="text-xs text-gray-500 mt-1">
                    From: {{ payment.sourceAdvanceTransactionNumber }}
                  </div>
                </td>
                <td class="py-3 px-3 text-sm">
                  <div *ngIf="isRefundPayment(payment)" class="text-purple-600 font-medium">
                    {{ payment.transactionNumber }}
                  </div>
                  <div *ngIf="!isRefundPayment(payment) && payment.purchaseOrderNumber" class="text-blue-600 font-medium">
                    PO: {{ payment.purchaseOrderNumber }}
                  </div>
                  <div *ngIf="!isRefundPayment(payment) && payment.goodsReceiptNumber && !payment.purchaseOrderNumber" class="text-green-600 font-medium">
                    GR: {{ payment.goodsReceiptNumber }}
                  </div>
                  <div *ngIf="!isRefundPayment(payment) && !payment.purchaseOrderNumber && !payment.goodsReceiptNumber && payment.invoiceNumber" class="text-gray-600">
                    {{ payment.invoiceNumber }}
                  </div>
                  <div *ngIf="!isRefundPayment(payment) && !payment.purchaseOrderNumber && !payment.goodsReceiptNumber && !payment.invoiceNumber" class="text-gray-400">
                    N/A
                  </div>
                </td>
                <td class="py-3 px-3 text-sm">
                  <span [ngClass]="getStatusBadgeClass(payment.status)">
                    {{ payment.status }}
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </ng-container>
    </div>
  `,
  styles: [`
    table {
      border-collapse: collapse;
    }

    tr:last-child td {
      border-bottom: none;
    }
  `]
})
export class PaymentHistoryTableComponent {
  @Input() payments: PaymentHistoryItem[] = [];
  @Input() ledgerEntries: SupplierLedgerEntryDto[] = [];
  @Input() supplierName: string = '';
  @Input() useLedger: boolean = false;  // Set to true to use ledger view
  @Input() entryLimit: number = 10;

  private readonly currencyService = inject(CurrencyService);

  formatCurrency(value: number): string {
    return this.currencyService.formatCurrency(value ?? 0, this.currencyService.selectedCurrency());
  }

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    return date.toLocaleDateString('en-US', {
      year: 'numeric',
      month: 'short',
      day: 'numeric'
    });
  }

  getPaymentTypeClass(type: string): string {
    const baseClass = 'px-2 py-1 rounded text-xs font-medium';
    switch (type) {
      case 'ADVANCE':
        return `${baseClass} bg-blue-100 text-blue-800`;
      case 'REGULAR':
        return `${baseClass} bg-gray-100 text-gray-800`;
      case 'REFUND':
        return `${baseClass} bg-purple-100 text-purple-800`;
      default:
        return `${baseClass} bg-gray-100 text-gray-800`;
    }
  }

  getPaymentMethodClass(method: string): string {
    const baseClass = 'px-2 py-1 rounded text-xs font-medium';
    switch (method?.toUpperCase()) {
      case 'REFUND':
        return `${baseClass} bg-purple-100 text-purple-800`;
      case 'ADVANCE_CREDIT':
        return `${baseClass} bg-blue-100 text-blue-800`;
      case 'CASH':
        return `${baseClass} bg-green-100 text-green-800`;
      case 'BANK_TRANSFER':
        return `${baseClass} bg-cyan-100 text-cyan-800`;
      default:
        return `${baseClass} bg-gray-100 text-gray-800`;
    }
  }

  getStatusBadgeClass(status: string): string {
    const baseClass = 'px-2 py-1 rounded text-xs font-medium';
    switch (status) {
      case 'COMPLETED':
        return `${baseClass} bg-green-100 text-green-800`;
      case 'PENDING':
        return `${baseClass} bg-orange-100 text-orange-800`;
      case 'PROCESSING':
        return `${baseClass} bg-blue-100 text-blue-800`;
      case 'FAILED':
        return `${baseClass} bg-red-100 text-red-800`;
      case 'CANCELLED':
        return `${baseClass} bg-gray-100 text-gray-800`;
      case 'RETURNED':
        return `${baseClass} bg-purple-100 text-purple-800`;
      default:
        return `${baseClass} bg-gray-100 text-gray-800`;
    }
  }

  /**
   * Check if this is a refund payment (from purchase return)
   */
  isRefundPayment(payment: PaymentHistoryItem): boolean {
    return payment.paymentMethod?.toUpperCase() === 'REFUND' ||
           payment.transactionNumber?.startsWith('REFUND-');
  }

  /**
   * Get ledger transaction type label
   */
  getLedgerTypeLabel(type: SupplierLedgerTransactionType | string): string {
    switch (type) {
      case 'PURCHASE':
      case SupplierLedgerTransactionType.PURCHASE:
        return 'Purchase';
      case 'PAYMENT':
      case SupplierLedgerTransactionType.PAYMENT:
        return 'Payment';
      case 'REFUND':
      case SupplierLedgerTransactionType.REFUND:
        return 'Refund';
      case 'ADVANCE':
      case SupplierLedgerTransactionType.ADVANCE:
        return 'Advance';
      case 'CANCELLATION':
      case SupplierLedgerTransactionType.CANCELLATION:
        return 'Cancelled';
      default:
        return type?.toString() || '';
    }
  }

  /**
   * Get ledger transaction type CSS class
   */
  getLedgerTypeClass(type: SupplierLedgerTransactionType | string): string {
    const baseClass = 'px-2 py-1 rounded text-xs font-medium';
    switch (type) {
      case 'PURCHASE':
      case SupplierLedgerTransactionType.PURCHASE:
        return `${baseClass} bg-red-100 text-red-800`;
      case 'PAYMENT':
      case SupplierLedgerTransactionType.PAYMENT:
        return `${baseClass} bg-green-100 text-green-800`;
      case 'REFUND':
      case SupplierLedgerTransactionType.REFUND:
        return `${baseClass} bg-purple-100 text-purple-800`;
      case 'ADVANCE':
      case SupplierLedgerTransactionType.ADVANCE:
        return `${baseClass} bg-blue-100 text-blue-800`;
      case 'CANCELLATION':
      case SupplierLedgerTransactionType.CANCELLATION:
        return `${baseClass} bg-gray-100 text-gray-800`;
      default:
        return `${baseClass} bg-gray-100 text-gray-800`;
    }
  }

  /**
   * Get reference number CSS class based on transaction type
   */
  getReferenceClass(type: SupplierLedgerTransactionType | string): string {
    switch (type) {
      case 'PURCHASE':
      case SupplierLedgerTransactionType.PURCHASE:
        return 'text-blue-600';
      case 'PAYMENT':
      case SupplierLedgerTransactionType.PAYMENT:
        return 'text-green-600';
      case 'REFUND':
      case SupplierLedgerTransactionType.REFUND:
        return 'text-purple-600';
      case 'ADVANCE':
      case SupplierLedgerTransactionType.ADVANCE:
        return 'text-blue-600';
      default:
        return 'text-gray-600';
    }
  }
}
