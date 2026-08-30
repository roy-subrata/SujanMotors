import { Component, EventEmitter, Input, Output, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TooltipModule } from 'primeng/tooltip';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
  imports: [CommonModule, TooltipModule, TranslatePipe],
  templateUrl: './data-pagination.component.html',
  styleUrl: './data-pagination.component.scss',
})
export class DataPaginationComponent {
  private firstSig = signal(0);
  private pageSizeSig = signal(10);
  private totalRecordsSig = signal(0);

  @Input() set first(v: number) { this.firstSig.set(v ?? 0); }
  get first() { return this.firstSig(); }

  @Input() set pageSize(v: number) { this.pageSizeSig.set(v || 10); }
  get pageSize() { return this.pageSizeSig(); }

  @Input() set totalRecords(v: number) { this.totalRecordsSig.set(v ?? 0); }
  get totalRecords() { return this.totalRecordsSig(); }

  @Input() pageSizeOptions: number[] = [10, 20, 50];
  @Input() itemLabel = 'records';

  /** Emits the new 1-based page number. */
  @Output() pageChange = new EventEmitter<number>();
  /** Emits the new page size. */
  @Output() pageSizeChange = new EventEmitter<number>();

  currentPage = computed(() => Math.floor(this.firstSig() / this.pageSizeSig()) + 1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalRecordsSig() / this.pageSizeSig())));
  rangeFrom = computed(() => this.totalRecordsSig() === 0 ? 0 : this.firstSig() + 1);
  rangeTo = computed(() => Math.min(this.firstSig() + this.pageSizeSig(), this.totalRecordsSig()));

  emit(page: number): void {
    const clamped = Math.min(Math.max(1, page), this.totalPages());
    if (clamped !== this.currentPage()) this.pageChange.emit(clamped);
  }

  onSizeChange(event: Event): void {
    const value = Number((event.target as HTMLSelectElement).value);
    this.pageSizeChange.emit(value);
  }
}
