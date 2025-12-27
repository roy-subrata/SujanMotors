import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { TableModule } from 'primeng/table';
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
  rows = 10;
  activeTab = 0;
  viewMode: 'table' | 'grid' = 'table';
  selectedWarehouse: string | null = null;
  selectedStatus: string | null = null;
  showLowStockDialog = false;
  showHistoryDialog = false;

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
    setTimeout(() => {
      this.loadLowStock();
    }, 100);

    // Auto-refresh stock data every 30 seconds
    setInterval(() => {
      this.refreshStockData();
    }, 30000);
  }

  private refreshStockData(): void {
    // Only refresh if not currently loading
    if (!this.loading) {
      this.loadAllStock();
      this.loadLowStock();
    }
  }

  loadAllStock(): void {
    this.loading = true;
    this.stockService.getAllStockLevels().subscribe({
      next: (levels) => {
        this.allStockLevels = levels;
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
    this.stockService.getLowStock().subscribe({
      next: (levels) => {
        this.lowStockLevels = levels;
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

  onSearch(): void {
    // Trigger table filter refresh
  }

  onSearchInput(): void {
    // Real-time search as user types
  }

  onSearchClear(): void {
    this.searchTerm = '';
    this.onSearch();
  }

  onRefresh(): void {
    this.loadAllStock();
    this.loadLowStock();
  }

  onAdjustStock(stock: StockLevelResponse): void {
    const dialogRef = this.dialogService.open(StockAdjustmentDialogComponent, {
      header: 'Stock Adjustment',
      width: '600px',
      modal: true,
      data: { stock }
    });

    dialogRef.onClose.subscribe((result: any) => {
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

  /**
   * Load all parts
   */
  loadParts(): void {
    this.partService.getAllParts().subscribe({
      next: (parts) => {
        this.parts = Array.isArray(parts) ? parts : [];
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
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses) => {
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

  /**
   * Filter stock by search term, warehouse, and status
   */
  getFilteredStock(stockLevels: StockLevelResponse[]): StockLevelResponse[] {
    let filtered = stockLevels;

    // Filter by warehouse
    if (this.selectedWarehouse) {
      filtered = filtered.filter(s => s.warehouseId === this.selectedWarehouse);
    }

    // Filter by status
    if (this.selectedStatus) {
      filtered = filtered.filter(s => {
        const status = this.getStatusDotClass(s);
        return status === this.selectedStatus;
      });
    }

    // Filter by search term
    if (this.searchTerm) {
      const term = this.searchTerm.toLowerCase();
      filtered = filtered.filter(s => {
        const partName = this.getPartName(s.partId).toLowerCase();
        const partSku = this.getPartSku(s.partId).toLowerCase();
        const warehouseName = this.getWarehouseName(s.warehouseId).toLowerCase();
        return partName.includes(term) || partSku.includes(term) || warehouseName.includes(term);
      });
    }

    return filtered;
  }

  onWarehouseFilter(): void {
    // Filtering is done reactively via getFilteredStock
  }

  onStatusFilter(): void {
    // Filtering is done reactively via getFilteredStock
  }

  showAllLowStock(): void {
    this.showLowStockDialog = true;
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
    return this.allStockLevels.reduce((sum, s) => sum + (s.quantity || 0), 0);
  }

  /**
   * Calculate total reserved quantity
   */
  getTotalReserved(): number {
    return this.allStockLevels.reduce((sum, s) => sum + (s.reservedQuantity || 0), 0);
  }

  /**
   * Check if stock is below reorder level
   */
  isLowStock(stock: StockLevelResponse): boolean {
    return stock.availableQuantity <= stock.reorderLevel;
  }

  /**
   * Get stock status text
   */
  getStockStatus(stock: StockLevelResponse): string {
    if (stock.availableQuantity === 0) return 'Out of Stock';
    if (stock.availableQuantity <= stock.reorderLevel * 0.5) return 'Critical';
    if (stock.availableQuantity <= stock.reorderLevel) return 'Low Stock';
    return 'In Stock';
  }

  /**
   * Get status CSS class
   */
  getStatusClass(stock: StockLevelResponse): string {
    if (stock.availableQuantity === 0) return 'status-out';
    if (stock.availableQuantity <= stock.reorderLevel * 0.5) return 'status-critical';
    if (stock.availableQuantity <= stock.reorderLevel) return 'status-low';
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
