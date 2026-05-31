import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

export interface BreadcrumbItem {
  label: string;
  /** Router link; omit for the active (last) crumb. */
  link?: string | any[];
}

/**
 * Standard page header (the "parts-style" pattern):
 *   [optional breadcrumb bar]
 *   [gradient header]  title + subtitle (left)   |   stats badge + actions (right)
 *
 * Action buttons are projected via the `actions` slot and should use the
 * shared `.btn-icon` / `.btn-primary` classes (styled globally for the header).
 *
 * Usage:
 *   <app-page-header
 *     title="Parts"
 *     subtitle="Manage and track all inventory parts"
 *     [breadcrumb]="[{label:'Products', link:'/inventory/categories'}, {label:'Parts'}]"
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
  imports: [CommonModule, RouterModule],
  template: `
    @if (breadcrumb?.length) {
      <div class="breadcrumb-bar">
        <div class="breadcrumb-content">
          <a routerLink="/" class="breadcrumb-link"><i class="pi pi-home"></i></a>
          @for (crumb of breadcrumb; track crumb.label) {
            <i class="pi pi-chevron-right breadcrumb-separator"></i>
            @if (crumb.link) {
              <span class="breadcrumb-item"><a [routerLink]="crumb.link">{{ crumb.label }}</a></span>
            } @else {
              <span class="breadcrumb-active">{{ crumb.label }}</span>
            }
          }
        </div>
      </div>
    }

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
  @Input() breadcrumb?: BreadcrumbItem[];
  @Input() count?: number | null;
  @Input() countLabel = '';
  @Input() countIcon = 'pi pi-list';
}
