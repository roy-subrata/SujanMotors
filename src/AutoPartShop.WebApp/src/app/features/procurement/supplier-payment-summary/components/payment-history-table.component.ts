import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentHistoryItem } from '../../services/supplier-payment.service';
import { SupplierLedgerEntryDto, SupplierLedgerTransactionType } from '../../services/supplier-ledger.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-payment-history-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-history-table.component.html',
  styleUrl: './payment-history-table.component.scss'
})
export class PaymentHistoryTableComponent {
  @Input() payments: PaymentHistoryItem[] = [];
  @Input() ledgerEntries: SupplierLedgerEntryDto[] = [];
  @Input() supplierName: string = '';
  @Input() useLedger: boolean = false;  // Set to true to use ledger view
  @Input() entryLimit: number = 10;

  private readonly currencyService = inject(CurrencyService);
  protected readonly i18n = inject(I18nService);

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
        return this.i18n.t('supplierPaymentSummary.type.purchase');
      case 'PAYMENT':
      case SupplierLedgerTransactionType.PAYMENT:
        return this.i18n.t('supplierPaymentSummary.type.payment');
      case 'REFUND':
      case SupplierLedgerTransactionType.REFUND:
        return this.i18n.t('supplierPaymentSummary.type.refund');
      case 'ADVANCE':
      case SupplierLedgerTransactionType.ADVANCE:
        return this.i18n.t('supplierPaymentSummary.type.advance');
      case 'CANCELLATION':
      case SupplierLedgerTransactionType.CANCELLATION:
        return this.i18n.t('supplierPaymentSummary.type.cancelled');
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
