import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';
import { StatusDisplayService, StatusDomain } from '@/shared/services/status-display.service';

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
  private readonly statusDisplay = inject(StatusDisplayService);

  @Input() status: string = '';
  @Input() type: StatusType = 'purchase-order';

  getSeverity(): 'secondary' | 'info' | 'success' | 'warn' | 'danger' | 'contrast' {
    return this.statusDisplay.getSeverity(this.status, this.type as StatusDomain);
  }
}
