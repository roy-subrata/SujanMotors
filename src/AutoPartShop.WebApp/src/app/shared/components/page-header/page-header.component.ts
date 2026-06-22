import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Standard page header (the "parts-style" pattern):
 *   [gradient header]  title + subtitle (left)   |   stats badge + actions (right)
 *
 * Action buttons are projected via the `actions` slot and should use the
 * shared `.btn-icon` / `.btn-primary` classes (styled globally for the header).
 *
 * Usage:
 *   <app-page-header
 *     title="Parts"
 *     subtitle="Manage and track all inventory parts"
 *     [count]="totalRecords" countLabel="parts" countIcon="pi pi-box">
 *     <ng-container actions>
 *       <button class="btn-icon" ...><i class="pi pi-refresh"></i></button>
 *       <button class="btn-primary" ...><i class="pi pi-plus"></i><span>New Part</span></button>
 *     </ng-container>
 *   </app-page-header>
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule],
  template: `
    <header class="page-header">
      <div class="header-content">
        <div class="header-left">
          <h1 class="page-title">{{ title }}</h1>
          @if (subtitle) {
            <p class="page-subtitle">{{ subtitle }}</p>
          }
        </div>

        <div class="header-actions">
          @if (count !== null && count !== undefined) {
            <div class="stats-badge">
              <i [class]="countIcon"></i>
              <div class="stats-info">
                <span class="stats-value">{{ count }}</span>
                <span class="stats-label">{{ countLabel }}</span>
              </div>
            </div>
          }
          <div class="action-buttons">
            <ng-content select="[actions]"></ng-content>
          </div>
        </div>
      </div>
    </header>
  `
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() count?: number | null;
  @Input() countLabel = '';
  @Input() countIcon = 'pi pi-list';
}
