import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { SupplierPaymentService } from '../../services/supplier-payment.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-payment-actions',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  templateUrl: './payment-actions.component.html'
})
export class PaymentActionsComponent {
  @Input() supplierId!: string;
  @Input() supplierName!: string;
  @Output() refresh = new EventEmitter<void>();

  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly supplierPaymentService = inject(SupplierPaymentService);
  protected readonly i18n = inject(I18nService);

  createNewPayment(): void {
    this.router.navigate(['/procurement/supplier-payments/create'], {
      queryParams: { supplierId: this.supplierId }
    });
  }

  viewAllPayments(): void {
    this.router.navigate(['/procurement/supplier-payments'], {
      queryParams: { supplierId: this.supplierId }
    });
  }

  downloadReport(): void {
    this.supplierPaymentService.downloadPaymentSummaryReport(this.supplierId).subscribe({
      next: (blob: Blob) => {

        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `payment-summary-${this.supplierName}-${new Date().toISOString().split('T')[0]}.pdf`;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        // Clean up after a brief delay to ensure download completes
        setTimeout(() => {
          window.URL.revokeObjectURL(url);
        }, 100);

        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('supplierPaymentSummary.actions.reportSuccess'),
          life: 5000
        });
      },
      error: (error) => {
        console.error('Error downloading report:', error);
        const errorMessage = typeof error?.error === 'string' ? error.error : (error?.error?.message || this.i18n.t('supplierPaymentSummary.actions.reportFailed'));

        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: errorMessage,
          life: 5000
        });
      }
    });
  }

  refreshSummary(): void {
    this.refresh.emit();
  }
}
