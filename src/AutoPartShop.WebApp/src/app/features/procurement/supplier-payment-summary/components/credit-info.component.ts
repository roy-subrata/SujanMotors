import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SupplierPaymentHistorySummary } from '../../services/supplier-payment.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-credit-info',
  standalone: true,
  templateUrl: './credit-info.component.html',
  styleUrl: './credit-info.component.scss',
  imports: [CommonModule, TranslatePipe]
})
export class CreditInfoComponent {
  @Input() summary!: SupplierPaymentHistorySummary;

  protected readonly i18n = inject(I18nService);

  getUtilizedCredit(): number {
    if (this.summary.creditLimit === 0) return 0;
    return (this.summary.creditUtilization / 100) * this.summary.creditLimit;
  }

  getAvailableCredit(): number {
    return this.summary.creditLimit - this.getUtilizedCredit();
  }

  getUtilizationBarColor(): string {
    if (this.summary.creditUtilization < 60) {
      return 'bg-green-500';
    } else if (this.summary.creditUtilization < 80) {
      return 'bg-orange-500';
    } else {
      return 'bg-red-500';
    }
  }

  getAvailableTextColor(): string {
    const available = this.getAvailableCredit();
    if (available < 0) {
      return 'text-red-600';
    } else if (available < this.summary.creditLimit * 0.2) {
      return 'text-orange-600';
    }
    return 'text-green-600';
  }

  getCreditStatusClass(): string {
    const percentage = this.summary.creditUtilization;
    if (percentage < 60) {
      return 'bg-green-50 border border-green-200';
    } else if (percentage < 80) {
      return 'bg-orange-50 border border-orange-200';
    } else {
      return 'bg-red-50 border border-red-200';
    }
  }

  getCreditStatusIcon(): string {
    const percentage = this.summary.creditUtilization;
    if (percentage < 60) {
      return 'pi pi-check-circle text-green-600';
    } else if (percentage < 80) {
      return 'pi pi-exclamation-circle text-orange-600';
    } else {
      return 'pi pi-times-circle text-red-600';
    }
  }

  getCreditStatus(): string {
    const percentage = this.summary.creditUtilization;
    if (percentage < 60) {
      return this.i18n.t('supplierPaymentSummary.creditInfo.statusGood');
    } else if (percentage < 80) {
      return this.i18n.t('supplierPaymentSummary.creditInfo.statusWarning');
    } else {
      return this.i18n.t('supplierPaymentSummary.creditInfo.statusRisk');
    }
  }
}
