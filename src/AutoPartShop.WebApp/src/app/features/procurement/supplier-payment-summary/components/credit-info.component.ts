import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SupplierPaymentHistorySummary } from '../../services/supplier-payment.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-credit-info',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="summary">
      <h3 class="text-lg font-bold mb-4">{{ i18n.t('supplierPaymentSummary.creditInfo.title') }}</h3>

      <div class="space-y-4">
        <!-- Credit Limit -->
        <div class="info-row">
          <div class="flex justify-between items-center mb-2">
            <span class="text-sm font-medium text-gray-700">{{ i18n.t('supplierPaymentSummary.creditInfo.creditLimit') }}</span>
            <span class="text-sm font-bold text-gray-900">{{ summary.creditLimit | currency }}</span>
          </div>
        </div>

        <!-- Credit Utilized -->
        <div class="info-row">
          <div class="flex justify-between items-center mb-2">
            <span class="text-sm font-medium text-gray-700">{{ i18n.t('supplierPaymentSummary.creditInfo.creditUtilized') }}</span>
            <span class="text-sm font-bold text-gray-900">{{ getUtilizedCredit() | currency }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-3">
            <div
              class="h-3 rounded-full transition-all duration-300"
              [ngClass]="getUtilizationBarColor()"
              [style.width.%]="summary.creditUtilization">
            </div>
          </div>
          <p class="text-xs text-gray-500 mt-1">{{ i18n.t('supplierPaymentSummary.creditInfo.utilizedPercent', { percent: summary.creditUtilization.toFixed(1) }) }}</p>
        </div>

        <!-- Credit Available -->
        <div class="info-row">
          <div class="flex justify-between items-center mb-2">
            <span class="text-sm font-medium text-gray-700">{{ i18n.t('supplierPaymentSummary.creditInfo.creditAvailable') }}</span>
            <span class="text-sm font-bold" [ngClass]="getAvailableTextColor()">
              {{ getAvailableCredit() | currency }}
            </span>
          </div>
        </div>

        <!-- Status Indicator -->
        <div class="mt-4 p-3 rounded-lg" [ngClass]="getCreditStatusClass()">
          <div class="flex items-center">
            <i [ngClass]="getCreditStatusIcon()" class="mr-2"></i>
            <span class="text-sm font-medium">{{ getCreditStatus() }}</span>
          </div>
        </div>

        <!-- Additional Info -->
        <div class="mt-4 pt-4 border-t border-gray-200">
          <div>
            <p class="text-xs text-gray-600">{{ 'common.labels.supplierCode' | translate }}</p>
            <p class="text-sm font-semibold text-gray-900">{{ summary.supplierCode }}</p>
          </div>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .info-row {
      padding: 0.75rem;
      background: var(--surface-ground);
      border-radius: 0.375rem;
    }
  `  ],
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
