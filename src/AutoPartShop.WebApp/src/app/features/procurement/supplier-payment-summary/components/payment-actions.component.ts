import { Component, Input, Output, EventEmitter, inject } from '@angular/core';
import { Router } from '@angular/router';
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { MessageService } from 'primeng/api';
import { SupplierPaymentService } from '../../services/supplier-payment.service';

@Component({
  selector: 'app-payment-actions',
  standalone: true,
  imports: [CommonModule, ButtonModule],
  template: `
    <div>
      <h3 class="text-lg font-bold mb-4">Actions</h3>

      <div class="grid grid-cols-12 gap-3">
        <!-- New Payment Button -->
        <div class="col-span-12 sm:col-span-6 md:col-span-3">
          <button pButton
            type="button"
            icon="pi pi-plus"
            label="New Payment"
            class="w-full"
            (click)="createNewPayment()">
          </button>
        </div>

        <!-- View All Payments Button -->
        <div class="col-span-12 sm:col-span-6 md:col-span-3">
          <button pButton
            type="button"
            icon="pi pi-list"
            label="All Payments"
            class="w-full"
            [outlined]="true"
            (click)="viewAllPayments()">
          </button>
        </div>

        <!-- Download Report Button -->
        <div class="col-span-12 sm:col-span-6 md:col-span-3">
          <button pButton
            type="button"
            icon="pi pi-download"
            label="Download Report"
            class="w-full"
            [outlined]="true"
            (click)="downloadReport()">
          </button>
        </div>

        <!-- Refresh Summary Button -->
        <div class="col-span-12 sm:col-span-6 md:col-span-3">
          <button pButton
            type="button"
            icon="pi pi-refresh"
            label="Refresh"
            class="w-full"
            [text]="true"
            (click)="refreshSummary()">
          </button>
        </div>
      </div>

      <div class="mt-4 p-3 bg-blue-50 border border-blue-200 rounded-lg">
        <p class="text-xs text-blue-700">
          <i class="pi pi-info-circle mr-2"></i>
          Actions allow you to manage supplier payments, create new payments, and view detailed payment history.
        </p>
      </div>
    </div>
  `,
  styles: []
})
export class PaymentActionsComponent {
  @Input() supplierId!: string;
  @Input() supplierName!: string;
  @Output() refresh = new EventEmitter<void>();

  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);
  private readonly supplierPaymentService = inject(SupplierPaymentService);

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
          summary: 'Success',
          detail: 'Report downloaded successfully as PDF',
          life: 5000
        });
      },
      error: (error) => {
        console.error('Error downloading report:', error);
        const errorMessage = typeof error?.error === 'string' ? error.error : (error?.error?.message || 'Failed to download report');

        this.messageService.add({
          severity: 'error',
          summary: 'Error',
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
