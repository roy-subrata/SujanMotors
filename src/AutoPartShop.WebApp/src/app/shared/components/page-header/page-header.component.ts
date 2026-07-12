import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';

/**
 * Standard page header (data-page design system):
 *   [back] title + subtitle (left)   |   actions (right)
 *
 * The record count is folded into the subtitle line (e.g. "Manage and
 * track all inventory parts · 49 parts") rather than shown as a separate
 * banner badge. `countIcon` is kept as an input purely for backward
 * compatibility with existing page templates that still pass it — it's
 * no longer rendered.
 *
 * Action buttons are projected via the `actions` slot and should use the
 * shared `.btn-icon` / `.btn-primary` / `.btn-secondary` classes.
 *
 * Pass `backLink` (a routerLink target) to render a back arrow to the left
 * of the title — e.g. a report detail page navigating back to its hub.
 *
 * Usage:
 *   <app-page-header
 *     title="Parts"
 *     subtitle="Manage and track all inventory parts"
 *     [count]="totalRecords" countLabel="parts"
 *     [backLink]="['/inventory']">
 *     <ng-container actions>
 *       <button class="btn-icon" ...><i class="pi pi-refresh"></i></button>
 *       <button class="btn-primary" ...><i class="pi pi-plus"></i><span>New Part</span></button>
 *     </ng-container>
 *   </app-page-header>
 */
@Component({
  selector: 'app-page-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <header class="page-header">
      <div class="header-content">
        <div class="header-left-group">
          @if (backLink) {
            <a class="btn-icon back-button" [routerLink]="backLink" title="Back">
              <i class="pi pi-arrow-left"></i>
            </a>
          }
          <div class="header-left">
            <h1 class="page-title">{{ title }}</h1>
            @if (subtitleLine) {
              <p class="page-subtitle">{{ subtitleLine }}</p>
            }
          </div>
        </div>

        <div class="header-actions">
          <div class="action-buttons">
            <ng-content select="[actions]"></ng-content>
          </div>
        </div>
      </div>
    </header>
  `,
  styles: [`
    .header-left-group {
      display: flex;
      align-items: center;
      gap: 12px;
      flex: 1;
      min-width: 0;
    }
    .back-button {
      flex-shrink: 0;
      text-decoration: none;
    }
  `]
})
export class PageHeaderComponent {
  @Input() title = '';
  @Input() subtitle?: string;
  @Input() count?: number | null;
  @Input() countLabel = '';
  /** Optional routerLink target for a back arrow rendered to the left of the title. */
  @Input() backLink?: string | readonly unknown[];
  /** @deprecated no longer rendered — kept so existing page templates still bind cleanly. */
  @Input() countIcon = 'pi pi-list';

  get subtitleLine(): string {
    const parts: string[] = [];
    if (this.subtitle) parts.push(this.subtitle);
    if (this.count !== null && this.count !== undefined) {
      parts.push(`${this.count} ${this.countLabel}`.trim());
    }
    return parts.join(' · ');
  }
}
