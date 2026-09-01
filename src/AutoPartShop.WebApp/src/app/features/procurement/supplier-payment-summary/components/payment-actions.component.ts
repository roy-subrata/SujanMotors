import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { SupplierPaymentService } from '../../services/supplier-payment.service';
import { I18nService } from '@/shared/services/i18n.service';
import { PdfPreviewService } from '@/shared/services/pdf-preview.service';

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
  private readonly pdfPreview = inject(PdfPreviewService);
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
        const filename = `payment-summary-${this.supplierName}-${new Date().toISOString().split('T')[0]}.pdf`;
        this.pdfPreview.open(blob, filename);
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
