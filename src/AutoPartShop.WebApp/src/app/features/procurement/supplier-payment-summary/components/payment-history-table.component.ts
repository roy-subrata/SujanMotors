import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentHistoryItem } from '../../services/supplier-payment.service';

@Component({
  selector: 'app-payment-history-table',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div>
      <h3 class="text-lg font-bold mb-4">Payment History (Last 10)</h3>

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
              <th class="text-left py-3 px-3 font-semibold text-gray-700">Invoice</th>
              <th class="text-left py-3 px-3 font-semibold text-gray-700">Status</th>
            </tr>
          </thead>
          <tbody>
            <tr *ngFor="let payment of payments" class="border-b border-gray-200 hover:bg-gray-50">
              <td class="py-3 px-3 text-sm">{{ formatDate(payment.paymentDate) }}</td>
              <td class="py-3 px-3 text-sm font-semibold">{{ payment.amount | currency }}</td>
              <td class="py-3 px-3 text-sm">
                <span [ngClass]="getPaymentTypeClass(payment.paymentType)">
                  {{ payment.paymentType }}
                </span>
              </td>
              <td class="py-3 px-3 text-sm">{{ payment.paymentMethod }}</td>
              <td class="py-3 px-3 text-sm">{{ payment.invoiceNumber || 'N/A' }}</td>
              <td class="py-3 px-3 text-sm">
                <span [ngClass]="getStatusBadgeClass(payment.status)">
                  {{ payment.status }}
                </span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
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
  @Input() supplierName: string = '';

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
      default:
        return `${baseClass} bg-gray-100 text-gray-800`;
    }
  }
}
