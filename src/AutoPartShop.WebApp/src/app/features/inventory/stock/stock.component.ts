import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TooltipModule } from 'primeng/tooltip';
import { SelectModule } from 'primeng/select';
import { DialogModule } from 'primeng/dialog';
import { MessageService } from 'primeng/api';
import { StockService, StockLevelResponse } from '../services/stock.service';
import { PartService, PartResponse } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { DialogService } from 'primeng/dynamicdialog';
import { StockAdjustmentDialogComponent } from './stock-adjustment-dialog.component';
import { StockMovementHistoryComponent } from './stock-movement-history.component';
import { StockLotsByWarehouseComponent } from './stock-lots-by-warehouse.component';
import { StockPriceHistoryComponent } from './stock-price-history.component';

@Component({
  selector: 'app-stock',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    TableModule,
    TooltipModule,
    SelectModule,
    ToastModule,
    DialogModule,
    StockMovementHistoryComponent,
    StockLotsByWarehouseComponent,
    StockPriceHistoryComponent
  ],
  providers: [MessageService, DialogService],
  templateUrl: './stock.component.html',
  styleUrls: ['./stock.component.css']
})
export class StockComponent implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly messageService = inject(MessageService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly dialogService = inject(DialogService);

  allStockLevels: StockLevelResponse[] = [];
  lowStockLevels: StockLevelResponse[] = [];
  parts: PartResponse[] = [];
  warehouses: WarehouseResponse[] = [];

  loading = false;
  searchTerm = '';
  activeTab = 0;

  // Per-tab filter states
  allStockFilters = {
    search: '',
    warehouseId: null as string | null,
    status: null as string | null
  };

  lowStockFilters = {
    search: '',
    warehouseId: null as string | null
  };

  // Pagination state - All Stock
  allTotalRecords = 0;
  allPageNumber = 1;
  allPageSize = 10;
  allFirst = 0;

  // Pagination state - Low Stock
  lowTotalRecords = 0;
  lowPageNumber = 1;
  lowPageSize = 10;
  lowFirst = 0;

  pageSizeOptions = [10, 25, 50];

  // Warehouse filter options
  warehouseOptions: { label: string; value: string }[] = [];

  // Stock status options
  stockStatusOptions = [
    { label: 'In Stock', value: 'in-stock' },
    { label: 'Low Stock', value: 'low' },
    { label: 'Critical', value: 'critical' },
    { label: 'Out of Stock', value: 'out-of-stock' }
  ];

  ngOnInit(): void {
    this.loadParts();
    this.loadWarehouses();
    this.loadAllStock();
    this.loadLowStock();
  }

  loadAllStock(): void {
    this.loading = true;
    this.stockService.getStockLevels({
      search: this.allStockFilters.search,
      pageNumber: this.allPageNumber,
      pageSize: this.allPageSize,
      warehouseId: this.allStockFilters.warehouseId || undefined,
      status: this.allStockFilters.status || undefined
    }).subscribe({
      next: (response) => {
        this.allStockLevels = response.data;
        this.allTotalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load stock levels'
        });
        this.loading = false;
      }
    });
  }

  loadLowStock(): void {
    this.stockService.getStockLevels({
      search: this.lowStockFilters.search,
      pageNumber: this.lowPageNumber,
      pageSize: this.lowPageSize,
      warehouseId: this.lowStockFilters.warehouseId || undefined,
      lowStockOnly: true
    }).subscribe({
      next: (response) => {
        this.lowStockLevels = response.data;
        this.lowTotalRecords = response.pagination.totalCount;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load low stock items'
        });
      }
    });
  }

  setActiveTab(tab: number): void {
    this.activeTab = tab;
  }

  // All Stock Tab Filters
  onAllStockSearch(): void {
    this.resetAllPagination();
    this.loadAllStock();
  }

  onAllStockSearchInput(): void {
    // Debounced search will be triggered on input
  }

  onAllStockSearchClear(): void {
    this.allStockFilters.search = '';
    this.onAllStockFilterChange();
  }

  onAllStockFilterChange(): void {
    this.resetAllPagination();
    this.loadAllStock();
  }

  // Low Stock Tab Filters
  onLowStockSearch(): void {
    this.resetLowPagination();
    this.loadLowStock();
  }

  onLowStockSearchInput(): void {
    // Debounced search will be triggered on input
  }

  onLowStockSearchClear(): void {
    this.lowStockFilters.search = '';
    this.onLowStockFilterChange();
  }

  onLowStockFilterChange(): void {
    this.resetLowPagination();
    this.loadLowStock();
  }

  onRefresh(): void {
    this.loadAllStock();
    this.loadLowStock();
  }

  onAdjustStock(stock: StockLevelResponse): void {
    const dialogRef = this.dialogService.open(StockAdjustmentDialogComponent, {
      header: 'Stock Adjustment',
      width: '720px',
      breakpoints: {
        '960px': '95vw',
        '640px': '100vw'
      },
      styleClass: 'stock-adjustment-dialog',
      modal: true,
      data: { stock }
    });

    dialogRef!.onClose.subscribe((result: any) => {
      if (result?.success) {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Stock adjustment recorded successfully'
        });
        // Refresh stock levels
        this.loadAllStock();
        this.loadLowStock();
      }
    });
  }

  loadParts(): void {
    this.partService.getParts({ search: '', pageNumber: 1, pageSize: 500, isActive: true }).subscribe({
      next: (res) => {
        this.parts = res.data ?? [];
      },
      error: (_error) => {
        console.error('Error loading parts:', _error);
      }
    });
  }

  /**
   * Load all warehouses
   */
  loadWarehouses(): void {
    this.warehouseService.getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] }).subscribe({
      next: (res) => {
        const warehouses = res.data ?? [];
        this.warehouses = Array.isArray(warehouses) ? warehouses : [];
        this.warehouseOptions = this.warehouses.map(w => ({
          label: w.name,
          value: w.id
        }));
      },
      error: (_error) => {
        console.error('Error loading warehouses:', _error);
      }
    });
  }

  onAllLazyLoad(event: TableLazyLoadEvent): void {
    this.allFirst = event.first ?? 0;
    this.allPageSize = event.rows ?? this.allPageSize;
    this.allPageNumber = Math.floor(this.allFirst / this.allPageSize) + 1;
    this.loadAllStock();
  }

  onLowLazyLoad(event: TableLazyLoadEvent): void {
    this.lowFirst = event.first ?? 0;
    this.lowPageSize = event.rows ?? this.lowPageSize;
    this.lowPageNumber = Math.floor(this.lowFirst / this.lowPageSize) + 1;
    this.loadLowStock();
  }

  goAllPrevPage(): void {
    if (this.allFirst === 0) return;
    this.onAllLazyLoad({ first: this.allFirst - this.allPageSize, rows: this.allPageSize } as TableLazyLoadEvent);
  }

  goAllNextPage(): void {
    if (this.allFirst + this.allPageSize >= this.allTotalRecords) return;
    this.onAllLazyLoad({ first: this.allFirst + this.allPageSize, rows: this.allPageSize } as TableLazyLoadEvent);
  }

  goLowPrevPage(): void {
    if (this.lowFirst === 0) return;
    this.onLowLazyLoad({ first: this.lowFirst - this.lowPageSize, rows: this.lowPageSize } as TableLazyLoadEvent);
  }

  goLowNextPage(): void {
    if (this.lowFirst + this.lowPageSize >= this.lowTotalRecords) return;
    this.onLowLazyLoad({ first: this.lowFirst + this.lowPageSize, rows: this.lowPageSize } as TableLazyLoadEvent);
  }

  private resetAllPagination(): void {
    this.allFirst = 0;
    this.allPageNumber = 1;
  }

  private resetLowPagination(): void {
    this.lowFirst = 0;
    this.lowPageNumber = 1;
  }

  /**
   * Check if stock is critically low
   */
  isCriticalStock(stock: StockLevelResponse): boolean {
    return stock.availableQuantity <= stock.reorderLevel * 0.5;
  }

  /**
   * Get status dot CSS class
   */
  getStatusDotClass(stock: StockLevelResponse): string {
    if (stock.availableQuantity === 0) return 'out-of-stock';
    if (stock.availableQuantity <= stock.reorderLevel * 0.5) return 'critical';
    if (stock.availableQuantity <= stock.reorderLevel) return 'low';
    return 'in-stock';
  }

  /**
   * Get warehouse summary for sidebar
   */
  getWarehouseSummary(): { name: string; itemCount: number; percentage: number }[] {
    const maxItems = Math.max(...this.warehouses.map(w => 
      this.allStockLevels.filter(s => s.warehouseId === w.id).length
    ), 1);

    return this.warehouses.slice(0, 4).map(w => {
      const itemCount = this.allStockLevels.filter(s => s.warehouseId === w.id).length;
      return {
        name: w.name,
        itemCount,
        percentage: (itemCount / maxItems) * 100
      };
    });
  }

  /**
   * Get part name for a given partId
   */
  getPartName(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part?.name || partId;
  }

  /**
   * Get part SKU for a given partId
   */
  getPartSku(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part?.sku || '';
  }

  /**
   * Get part name and code for a given partId
   */
  getPartInfo(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    if (part) {
      return `${part.name} (${part.sku})`;
    }
    return partId;
  }

  /**
   * Get warehouse name for a given warehouseId
   */
  getWarehouseName(warehouseId: string): string {
    const warehouse = this.warehouses.find(w => w.id === warehouseId);
    return warehouse?.name || warehouseId;
  }

  /**
   * Calculate total on-hand quantity
   */
  getTotalOnHand(): number {
    // Sum base unit quantities for accurate totals across different units
    return this.allStockLevels.reduce((sum, s) => sum + (s.quantityInBaseUnit || 0), 0);
  }

  /**
   * Calculate total reserved quantity
   */
  getTotalReserved(): number {
    // Sum base unit quantities for accurate totals across different units
    return this.allStockLevels.reduce((sum, s) => sum + (s.reservedQuantityInBaseUnit || 0), 0);
  }

  /**
   * Check if stock is below reorder level
   */
  isLowStock(stock: StockLevelResponse): boolean {
    // Compare base unit quantity with reorder level
    return stock.availableQuantityInBaseUnit <= stock.reorderLevel;
  }

  /**
   * Get stock status text
   */
  getStockStatus(stock: StockLevelResponse): string {
    const availBase = stock.availableQuantityInBaseUnit;
    if (availBase === 0) return 'Out of Stock';
    if (availBase <= stock.reorderLevel * 0.5) return 'Critical';
    if (availBase <= stock.reorderLevel) return 'Low Stock';
    return 'In Stock';
  }

  /**
   * Get status CSS class
   */
  getStatusClass(stock: StockLevelResponse): string {
    const availBase = stock.availableQuantityInBaseUnit;
    if (availBase === 0) return 'status-out';
    if (availBase <= stock.reorderLevel * 0.5) return 'status-critical';
    if (availBase <= stock.reorderLevel) return 'status-low';
    return 'status-in-stock';
  }

  /**
   * Get status icon class
   */
  getStatusIcon(stock: StockLevelResponse): string {
    if (stock.availableQuantity === 0) return 'pi pi-times-circle';
    if (stock.availableQuantity <= stock.reorderLevel * 0.5) return 'pi pi-exclamation-circle';
    if (stock.availableQuantity <= stock.reorderLevel) return 'pi pi-exclamation-triangle';
    return 'pi pi-check-circle';
  }

  /**
   * Get shortage amount
   */
  getShortage(stock: StockLevelResponse): number {
    return Math.max(0, stock.reorderLevel - stock.availableQuantity);
  }

  /**
   * Get urgency level for low stock
   */
  getUrgencyLevel(stock: StockLevelResponse): string {
    const ratio = stock.availableQuantity / stock.reorderLevel;
    if (ratio === 0 || stock.availableQuantity === 0) return 'Critical';
    if (ratio <= 0.25) return 'High';
    return 'Medium';
  }

  /**
   * Get urgency CSS class
   */
  getUrgencyClass(stock: StockLevelResponse): string {
    const ratio = stock.availableQuantity / stock.reorderLevel;
    if (ratio === 0 || stock.availableQuantity === 0) return 'urgency-critical';
    if (ratio <= 0.25) return 'urgency-high';
    return 'urgency-medium';
  }

  /**
   * View stock history for an item
   */
  viewStockHistory(stock: StockLevelResponse): void {
    // Switch to movement history tab with filter
    this.activeTab = 2;
  }

  /**
   * Create reorder for low stock item
   */
  createReorder(stock: StockLevelResponse): void {
    this.messageService.add({
      severity: 'info',
      summary: 'Coming Soon',
      detail: 'Purchase order creation will be available soon'
    });
  }
}
