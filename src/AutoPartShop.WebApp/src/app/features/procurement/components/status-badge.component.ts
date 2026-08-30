import { Component, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TagModule } from 'primeng/tag';
import { StatusDisplayService, StatusDomain } from '@/shared/services/status-display.service';

export type StatusType = 'purchase-order' | 'goods-receipt' | 'payment';

@Component({
  selector: 'app-status-badge',
  standalone: true,
  imports: [CommonModule, TagModule],
  templateUrl: './status-badge.component.html',
  styleUrl: './status-badge.component.scss'
})
export class StatusBadgeComponent {
  private readonly statusDisplay = inject(StatusDisplayService);

  @Input() status: string = '';
  @Input() type: StatusType = 'purchase-order';

  /** Getter, not a field: re-resolves per change-detection pass so it follows a language switch. */
  get label(): string {
    return this.statusDisplay.getLabel(this.status);
  }

  getSeverity(): 'secondary' | 'info' | 'success' | 'warn' | 'danger' | 'contrast' {
    return this.statusDisplay.getSeverity(this.status, this.type as StatusDomain);
  }
}
