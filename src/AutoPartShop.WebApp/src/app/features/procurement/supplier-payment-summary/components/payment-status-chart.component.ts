import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentStatusBreakdown } from '../../services/supplier-payment.service';

@Component({
  selector: 'app-payment-status-chart',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div *ngIf="breakdown">
      <h3 class="text-lg font-bold mb-4">Payment Status Breakdown</h3>

      <div class="space-y-3">
        <!-- Completed -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Completed</span>
            <span class="text-sm font-bold text-green-600">{{ breakdown.completed }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-green-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.completed)"></div>
          </div>
        </div>

        <!-- Pending -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Pending</span>
            <span class="text-sm font-bold text-orange-600">{{ breakdown.pending }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-orange-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.pending)"></div>
          </div>
        </div>

        <!-- Processing -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Processing</span>
            <span class="text-sm font-bold text-blue-600">{{ breakdown.processing }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-blue-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.processing)"></div>
          </div>
        </div>

        <!-- Reconciled -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Reconciled</span>
            <span class="text-sm font-bold text-purple-600">{{ breakdown.reconciled }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-purple-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.reconciled)"></div>
          </div>
        </div>

        <!-- Failed -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Failed</span>
            <span class="text-sm font-bold text-red-600">{{ breakdown.failed }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-red-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.failed)"></div>
          </div>
        </div>

        <!-- Cancelled -->
        <div class="status-item">
          <div class="flex justify-between mb-1">
            <span class="text-sm font-medium text-gray-700">Cancelled</span>
            <span class="text-sm font-bold text-gray-600">{{ breakdown.cancelled }}</span>
          </div>
          <div class="w-full bg-gray-200 rounded-full h-2">
            <div class="bg-gray-500 h-2 rounded-full" [style.width.%]="getPercentage(breakdown.cancelled)"></div>
          </div>
        </div>
      </div>

      <div class="mt-4 pt-4 border-t border-gray-200">
        <p class="text-xs text-gray-500">
          Total: {{ getTotalPayments() }} payments
        </p>
      </div>
    </div>
  `,
  styles: [`
    .status-item {
      padding: 0.75rem;
      background: var(--surface-ground);
      border-radius: 0.375rem;
    }
  `]
})
export class PaymentStatusChartComponent {
  @Input() statusBreakdown!: PaymentStatusBreakdown | undefined;

  get breakdown(): PaymentStatusBreakdown | undefined {
    return this.statusBreakdown;
  }

  getTotalPayments(): number {
    if (!this.breakdown) return 0;
    return (this.breakdown.completed || 0) +
           (this.breakdown.pending || 0) +
           (this.breakdown.processing || 0) +
           (this.breakdown.reconciled || 0) +
           (this.breakdown.failed || 0) +
           (this.breakdown.cancelled || 0);
  }

  getPercentage(count: number): number {
    const total = this.getTotalPayments();
    if (total === 0) return 0;
    return Math.min((count / total) * 100, 100);
  }
}
