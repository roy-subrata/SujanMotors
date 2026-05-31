import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';

/**
 * Standard pagination footer for list pages. Renders the desktop pager
 * (range text + page-size selector + nav) and a compact sticky mobile pager.
 * Place once at the end of the page; visibility is handled by the shared
 * .desktop-only / .mobile-only classes.
 *
 * Usage:
 *   <app-data-pagination
 *     [first]="first" [pageSize]="pageSize" [totalRecords]="totalRecords"
 *     itemLabel="parts"
 *     (pageChange)="goToPage($event)" (pageSizeChange)="onPageSizeChange($event)">
 *   </app-data-pagination>
 */
@Component({
  selector: 'app-data-pagination',
  standalone: true,
  imports: [CommonModule, TooltipModule],
  template: `
    @if (totalRecords > 0) {
      <!-- Desktop -->
      <div class="pagination-section desktop-only">
        <div class="pagination-info">
          Showing <strong>{{ rangeFrom() }}</strong> – <strong>{{ rangeTo() }}</strong>
          of <strong>{{ totalRecords }}</strong> {{ itemLabel }}
        </div>
        <div class="pagination-controls">
          <span class="page-size-label">Rows per page:</span>
          <select class="page-size-native" [value]="pageSize" (change)="onSizeChange($event)">
            @for (opt of pageSizeOptions; track opt) {
              <option [value]="opt">{{ opt }}</option>
            }
          </select>
          <div class="page-nav">
            <button class="page-btn" [disabled]="currentPage() <= 1" (click)="emit(1)" pTooltip="First" tooltipPosition="top">
              <i class="pi pi-angle-double-left"></i>
            </button>
            <button class="page-btn" [disabled]="currentPage() <= 1" (click)="emit(currentPage() - 1)" pTooltip="Previous" tooltipPosition="top">
              <i class="pi pi-angle-left"></i>
            </button>
            <span class="page-indicator">{{ currentPage() }} / {{ totalPages() }}</span>
            <button class="page-btn" [disabled]="currentPage() >= totalPages()" (click)="emit(currentPage() + 1)" pTooltip="Next" tooltipPosition="top">
              <i class="pi pi-angle-right"></i>
            </button>
            <button class="page-btn" [disabled]="currentPage() >= totalPages()" (click)="emit(totalPages())" pTooltip="Last" tooltipPosition="top">
              <i class="pi pi-angle-double-right"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Mobile -->
      <div class="mobile-pagination mobile-only">
        <div class="pagination-info-mobile">{{ rangeFrom() }}–{{ rangeTo() }} of {{ totalRecords }}</div>
        <div class="pagination-nav-mobile">
          <button class="page-btn" [disabled]="currentPage() <= 1" (click)="emit(currentPage() - 1)">
            <i class="pi pi-chevron-left"></i>
          </button>
          <button class="page-btn" [disabled]="currentPage() >= totalPages()" (click)="emit(currentPage() + 1)">
            <i class="pi pi-chevron-right"></i>
          </button>
        </div>
      </div>
    }
  `,
  styles: [`
    .page-size-native {
      width: 64px;
      padding: 6px 8px;
      font-size: 14px;
      color: var(--color-text-primary);
      background-color: var(--color-bg-primary);
      border: 1px solid var(--color-border);
      border-radius: var(--radius-sm);
      cursor: pointer;
      outline: none;
    }
    .page-size-native:focus {
      border-color: var(--color-primary);
      box-shadow: 0 0 0 3px var(--color-primary-light);
    }
  `]
})
export class DataPaginationComponent {
  private firstSig = signal(0);
  private pageSizeSig = signal(10);

  @Input() set first(v: number) { this.firstSig.set(v ?? 0); }
  get first() { return this.firstSig(); }

  @Input() set pageSize(v: number) { this.pageSizeSig.set(v || 10); }
  get pageSize() { return this.pageSizeSig(); }

  @Input() totalRecords = 0;
  @Input() pageSizeOptions: number[] = [10, 20, 50];
  @Input() itemLabel = 'records';

  /** Emits the new 1-based page number. */
  @Output() pageChange = new EventEmitter<number>();
  /** Emits the new page size. */
  @Output() pageSizeChange = new EventEmitter<number>();

  currentPage = computed(() => Math.floor(this.firstSig() / this.pageSizeSig()) + 1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecords / this.pageSizeSig())));
  rangeFrom = computed(() => this.totalRecords === 0 ? 0 : this.firstSig() + 1);
  rangeTo = computed(() => Math.min(this.firstSig() + this.pageSizeSig(), this.totalRecords));

  emit(page: number): void {
    const clamped = Math.min(Math.max(1, page), this.totalPages());
    if (clamped !== this.currentPage()) this.pageChange.emit(clamped);
  }

  onSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
