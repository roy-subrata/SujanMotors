import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';

export type StatusType = 'purchase-order' | 'goods-receipt' | 'payment';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, TagModule],
  template: `
    <p-tag
      [value]="status"
      [severity]="getSeverity()"
      [styleClass]="'text-sm'">
    </p-tag>
  `,
  styles: [`
    :host ::ng-deep {
      .p-tag {
        font-weight: 500;
        letter-spacing: 0.5px;
      }
    }
  `]
})
export class StatusBadgeComponent {
  @Input() status: string = '';
  @Input() type: StatusType = 'purchase-order';

  getSeverity(): string {
    if (this.type === 'purchase-order') {
      return this.getPOSeverity(this.status);
    } else if (this.type === 'goods-receipt') {
      return this.getGRNSeverity(this.status);
    } else if (this.type === 'payment') {
      return this.getPaymentSeverity(this.status);
    }
    return 'info';
  }

  private getPOSeverity(status: string): string {
    switch (status) {
      case 'DRAFT':
        return 'secondary';
      case 'SUBMITTED':
        return 'info';
      case 'CONFIRMED':
        return 'warning';
      case 'PARTIAL':
        return 'warning';
      case 'DELIVERED':
        return 'success';
      case 'CANCELLED':
        return 'danger';
      default:
        return 'info';
    }
  }

  private getGRNSeverity(status: string): string {
    switch (status) {
      case 'PENDING':
        return 'secondary';
      case 'VERIFIED':
        return 'info';
      case 'ACCEPTED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'info';
    }
  }

  private getPaymentSeverity(status: string): string {
    switch (status) {
      case 'PENDING':
        return 'secondary';
      case 'PROCESSING':
        return 'info';
      case 'CONFIRMED':
        return 'warning';
      case 'RECONCILED':
        return 'success';
      case 'FAILED':
        return 'danger';
      case 'CANCELLED':
        return 'danger';
      default:
        return 'info';
    }
  }
}
