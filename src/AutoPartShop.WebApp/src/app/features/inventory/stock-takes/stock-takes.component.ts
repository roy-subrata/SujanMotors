import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { MessageService } from 'primeng/api';

import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { StatusPillFilterComponent } from '@/shared/components/status-pill-filter/status-pill-filter.component';
import { MoreFiltersDialogComponent } from '@/shared/components/more-filters-dialog/more-filters-dialog.component';
import { StockTakeService, StockTakeResponse } from '../services/stock-take.service';
import { StockTakeStatus } from '@/shared/models/status.types';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { CategoryService, CategoryResponse } from '../services/category.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { StatusDisplayService } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-stock-takes',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    TableModule,
    ToastModule,
    TooltipModule,
    DialogModule,
    SelectModule,
    TextareaModule,
    PageContainerComponent,
    PageHeaderComponent,
    FilterBarComponent,
    DataPaginationComponent,
    StatusPillFilterComponent,
    MoreFiltersDialogComponent,
    TranslatePipe
  ],
  providers: [MessageService],
  templateUrl: './stock-takes.component.html',
  styleUrls: ['./stock-takes.component.css']
})
export class StockTakesComponent implements OnInit {
  private readonly stockTakeService = inject(StockTakeService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly categoryService = inject(CategoryService);
  private readonly currencyService = inject(CurrencyService);
  private readonly statusDisplay = inject(StatusDisplayService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  stockTakes: StockTakeResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  get first(): number { return (this.currentPage - 1) * this.rows; }

  // Filters
  searchTerm = '';
  filterStatus: StockTakeStatus | null = null;
  filterWarehouseId: string | null = null;
  moreFiltersVisible = false;

  statusOptions: { label: string; value: string }[] = [];
  warehouses: WarehouseResponse[] = [];
  categories: CategoryResponse[] = [];

  // New stock take dialog
  showCreateDialog = false;
  creating = false;
  createForm: { warehouseId: string | null; categoryId: string | null; notes: string } = {
    warehouseId: null,
    categoryId: null,
    notes: ''
  };

  ngOnInit(): void {
    this.buildStatusOptions();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildStatusOptions();
    });
    this.loadStockTakes();
    this.warehouseService.getAllWarehouses().subscribe(w => (this.warehouses = w));
    this.categoryService.getActiveCategories().subscribe(c => (this.categories = c));
  }

  private buildStatusOptions(): void {
    this.statusOptions = [
      { label: this.i18n.t('common.status.all'), value: '' },
      { label: this.i18n.t('stockTakes.status.counting'), value: 'COUNTING' },
      { label: this.i18n.t('stockTakes.status.review'), value: 'REVIEW' },
      { label: this.i18n.t('common.status.completed'), value: 'COMPLETED' },
      { label: this.i18n.t('common.status.cancelled'), value: 'CANCELLED' }
    ];
  }

  loadStockTakes(page = 1, pageSize = this.rows): void {
    this.loading = true;
    this.stockTakeService.getStockTakes({
      pageNumber: page,
      pageSize,
      status: this.filterStatus,
      warehouseId: this.filterWarehouseId,
      search: this.searchTerm || undefined
    }).subscribe({
      next: (response) => {
        this.stockTakes = response.data ?? [];
        this.totalRecords = response.pagination.totalCount;
        this.rows = response.pagination.pageSize;
        this.currentPage = response.pagination.pageNumber;
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('stockTakes.messages.loadFailed') });
        this.loading = false;
      }
    });
  }

  onSearch(): void { this.loadStockTakes(1, this.rows); }
  onFilterChange(): void { this.loadStockTakes(1, this.rows); }

  onStatusFilterChange(value: string): void {
    this.filterStatus = (value || null) as StockTakeStatus | null;
    this.onFilterChange();
  }
  refreshData(): void { this.loadStockTakes(this.currentPage, this.rows); }
  clearSearchInput(): void { this.searchTerm = ''; }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterStatus = null;
    this.filterWarehouseId = null;
    this.loadStockTakes(1, this.rows);
  }

  hasActiveFilters(): boolean {
    return !!this.searchTerm || this.filterStatus !== null || this.filterWarehouseId !== null;
  }

  goToPage(page: number): void { this.loadStockTakes(page, this.rows); }
  onPageSizeChange(size: number): void { this.loadStockTakes(1, size); }

  openDetail(st: StockTakeResponse): void {
    this.router.navigate(['/inventory/stock-takes', st.id]);
  }

  // ── Status helpers ──────────────────────────────────────────────────────────

  /** Maps stock-take statuses onto the shared status-pill palette. */
  pillStatus(status: string): string {
    return this.statusDisplay.getPillAttr(status, 'stock-take');
  }

  statusLabel(status: string): string {
    return this.statusOptions.find(o => o.value === status)?.label ?? status;
  }

  warehouseLabel(id: string | null): string {
    return this.warehouses.find(w => w.id === id)?.name ?? '';
  }

  formatCurrency(amount: number): string {
    return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
  }

  // ── Create ──────────────────────────────────────────────────────────────────

  openCreateDialog(): void {
    this.createForm = { warehouseId: null, categoryId: null, notes: '' };
    this.showCreateDialog = true;
  }

  createStockTake(): void {
    if (!this.createForm.warehouseId || this.creating) return;
    this.creating = true;
    this.stockTakeService.create({
      warehouseId: this.createForm.warehouseId,
      categoryId: this.createForm.categoryId,
      notes: this.createForm.notes
    }).subscribe({
      next: (st) => {
        this.creating = false;
        this.showCreateDialog = false;
        this.messageService.add({
          severity: 'success',
          summary: this.i18n.t('stockTakes.messages.createdTitle'),
          detail: this.i18n.t('stockTakes.messages.createdDetail', { number: st.stockTakeNumber, count: String(st.totalLines) })
        });
        this.router.navigate(['/inventory/stock-takes', st.id]);
      },
      error: (err) => {
        this.creating = false;
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('stockTakes.messages.createFailedTitle'),
          detail: err?.error?.message ?? this.i18n.t('stockTakes.messages.createFailed')
        });
      }
    });
  }
}
