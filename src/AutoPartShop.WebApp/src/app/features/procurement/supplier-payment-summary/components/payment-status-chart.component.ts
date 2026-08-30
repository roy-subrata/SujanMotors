import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PaymentStatusBreakdown } from '../../services/supplier-payment.service';
import { I18nService } from '@/shared/services/i18n.service';

@Component({
  selector: 'app-payment-status-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './payment-status-chart.component.html',
  styleUrl: './payment-status-chart.component.scss'
})
export class PaymentStatusChartComponent {
  @Input() statusBreakdown!: PaymentStatusBreakdown | undefined;

  protected readonly i18n = inject(I18nService);

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
