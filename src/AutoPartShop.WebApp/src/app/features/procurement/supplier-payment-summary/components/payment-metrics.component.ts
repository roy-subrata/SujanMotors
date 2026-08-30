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
  templateUrl: './payment-metrics.component.html',
  styleUrl: './payment-metrics.component.scss'
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
