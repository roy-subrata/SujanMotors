import { Component, OnInit, inject } from '@angular/core';
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
import { StockTakeService, StockTakeResponse } from '../services/stock-take.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { CategoryService, CategoryResponse } from '../services/category.service';
import { CurrencyService } from '../../../shared/services/currency.service';

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
    DataPaginationComponent
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
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);

  stockTakes: StockTakeResponse[] = [];
  loading = false;
  totalRecords = 0;
  rows = 10;
  currentPage = 1;
  get first(): number { return (this.currentPage - 1) * this.rows; }

  // Filters
  searchTerm = '';
  filterStatus: string | null = null;
  filterWarehouseId: string | null = null;

  statusOptions = [
    { label: 'Counting', value: 'COUNTING' },
    { label: 'Review', value: 'REVIEW' },
    { label: 'Completed', value: 'COMPLETED' },
    { label: 'Cancelled', value: 'CANCELLED' }
  ];
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
    this.loadStockTakes();
    this.warehouseService.getAllWarehouses().subscribe(w => (this.warehouses = w));
    this.categoryService.getActiveCategories().subscribe(c => (this.categories = c));
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
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load stock takes' });
        this.loading = false;
      }
    });
  }

  onSearch(): void { this.loadStockTakes(1, this.rows); }
  onFilterChange(): void { this.loadStockTakes(1, this.rows); }
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
    switch (status) {
      case 'COUNTING': return 'pending';
      case 'REVIEW': return 'info';
      case 'COMPLETED': return 'completed';
      case 'CANCELLED': return 'cancelled';
      default: return 'draft';
    }
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
        this.messageService.add({ severity: 'success', summary: 'Stock Take Started', detail: `${st.stockTakeNumber} — ${st.totalLines} items to count` });
        this.router.navigate(['/inventory/stock-takes', st.id]);
      },
      error: (err) => {
        this.creating = false;
        this.messageService.add({ severity: 'error', summary: 'Could Not Start', detail: err?.error?.message ?? 'Failed to create stock take' });
      }
    });
  }
}
