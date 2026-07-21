import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { DatePicker } from 'primeng/datepicker';
import { MessageService } from 'primeng/api';
import { map } from 'rxjs/operators';
import { StockService, StockMovementResponse } from '../services/stock.service';
import { PartService } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';

@Component({
  selector: 'app-stock-movement-history',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    TableModule,
    TagModule,
    TooltipModule,
    CardModule,
    ToastModule,
    InputTextModule,
    SelectModule,
    DatePicker
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>

    <p-card>
      <div class="filters-row">
        <div class="filter-group search-group">
          <div class="search-input-wrapper">
            <i class="pi pi-search"></i>
            <input
              type="text"
              [(ngModel)]="searchTerm"
              placeholder="Search by part, warehouse, reference..."
              (keyup.enter)="onSearch()"
              class="search-input" />
            <button *ngIf="searchTerm" class="search-clear" (click)="searchTerm = ''; onSearch()">
              <i class="pi pi-times"></i>
            </button>
          </div>
        </div>

        <div class="filter-group">
          <p-select
            [options]="movementTypeOptions"
            [(ngModel)]="filterType"
            placeholder="Type"
            [showClear]="true"
            (onChange)="onFilterChange()"
            appendTo="body"
            styleClass="filter-dropdown">
          </p-select>
        </div>

        <div class="filter-group">
          <p-select
            [options]="statusOptions"
            [(ngModel)]="filterStatus"
            placeholder="Status"
            [showClear]="true"
            (onChange)="onFilterChange()"
            appendTo="body"
            styleClass="filter-dropdown">
          </p-select>
        </div>

        <div class="filter-group date-range-group">
          <p-datePicker
            [(ngModel)]="dateRange"
            selectionMode="range"
            [showIcon]="true"
            [showButtonBar]="true"
            dateFormat="dd-M-yy"
            placeholder="Date range"
            [maxDate]="today"
            (onSelect)="onDateChange()"
            (onClearButtonClick)="onClearDateRange()"
            styleClass="date-picker-range">
          </p-datePicker>
        </div>

        <div class="filter-group filter-actions-group">
          <div class="filter-actions">
            <button class="btn-search" (click)="onSearch()">
              <i class="pi pi-search"></i>
            </button>
            <button *ngIf="hasActiveFilters()" class="btn-clear" (click)="clearFilters()">
              <i class="pi pi-filter-slash"></i>
            </button>
          </div>
        </div>
      </div>

      <div *ngIf="hasActiveFilters()" class="active-filters">
        <span class="active-filters-label">Active filters:</span>
        <span *ngIf="searchTerm" class="filter-chip">
          Search: "{{ searchTerm }}"
          <i class="pi pi-times" (click)="searchTerm = ''; onSearch()"></i>
        </span>
        <span *ngIf="filterType" class="filter-chip">
          Type: {{ filterType }}
          <i class="pi pi-times" (click)="filterType = ''; onFilterChange()"></i>
        </span>
        <span *ngIf="filterStatus" class="filter-chip">
          Status: {{ filterStatus }}
          <i class="pi pi-times" (click)="filterStatus = ''; onFilterChange()"></i>
        </span>
        <span *ngIf="dateRange && dateRange[0]" class="filter-chip">
          From: {{ dateRange[0] | date:'dd-MMM-yyyy' }}
          <i class="pi pi-times" (click)="onClearDateRange()"></i>
        </span>
        <span *ngIf="dateRange && dateRange[1]" class="filter-chip">
          To: {{ dateRange[1] | date:'dd-MMM-yyyy' }}
          <i class="pi pi-times" (click)="onClearDateRange()"></i>
        </span>
      </div>

      <p-table
        [value]="movements"
        [rows]="pageSize"
        [paginator]="true"
        [lazy]="true"
        [loading]="loading"
        [totalRecords]="totalRecords"
        [first]="first"
        (onLazyLoad)="onLazyLoad($event)"
        responsiveLayout="scroll"
        styleClass="p-datatable-striped">

        <!-- Header Template -->
        <ng-template pTemplate="header">
          <tr>
            <th style="width: 15%">Date & Time</th>
            <th style="width: 15%">Part</th>
            <th style="width: 15%">Warehouse</th>
            <th style="width: 10%" class="text-right">Quantity</th>
            <th style="width: 12%">Type</th>
            <th style="width: 12%">Status</th>
            <th style="width: 20%">Notes / Reference</th>
          </tr>
        </ng-template>

        <!-- Data Rows -->
        <ng-template pTemplate="body" let-movement>
          <tr>
            <td>
              <span class="text-sm">{{ movement.createdAt | date:'dd-MMM-yyyy HH:mm' }}</span>
            </td>
            <td>
              <span class="font-semibold">{{ getPartDisplay(movement) }}</span>
            </td>
            <td>
              <span class="text-gray-600">{{ getWarehouseDisplay(movement) }}</span>
            </td>
            <td class="text-right">
              <div class="dual-unit-cell">
                <span
                  class="display-qty"
                  [ngClass]="getQuantityClass(movement.type)">
                  {{ getQuantityDisplay(movement.type, movement.quantity) }}
                </span>
                <span class="display-unit" *ngIf="movement.unitSymbol">{{ movement.unitSymbol }}</span>
                <span class="base-qty" *ngIf="movement.quantityInBaseUnit && movement.unitSymbol">
                  ({{ formatBaseQuantity(movement) }} {{ movement.baseUnitSymbol || movement.unitSymbol }})
                </span>
              </div>
            </td>
            <td>
              <p-tag
                [value]="getMovementTypeLabel(movement.type)"
                [severity]="getMovementTypeSeverity(movement.type)">
              </p-tag>
            </td>
            <td>
              <p-tag
                [value]="movement.status"
                [severity]="getStatusSeverity(movement.status)">
              </p-tag>
            </td>
            <td>
              <div class="flex flex-col gap-1">
                <span class="text-sm font-semibold">{{ movement.reference }}</span>
                <span class="text-xs text-gray-500" *ngIf="movement.notes" [pTooltip]="movement.notes" tooltipPosition="left">
                  {{ getMovementDescription(movement) }}
                </span>
              </div>
            </td>
          </tr>
        </ng-template>

        <!-- Empty Message -->
        <ng-template pTemplate="emptymessage">
          <tr>
            <td colspan="7" class="text-center py-4">
              <i class="pi pi-inbox mr-2"></i>
              No stock movements found
            </td>
          </tr>
        </ng-template>
      </p-table>
    </p-card>
  `,
  styles: [`
    .filters-row {
      display: flex;
      flex-wrap: nowrap;
      gap: 0.75rem;
      align-items: flex-end;
      margin-bottom: 1rem;
      overflow-x: auto;
      padding-bottom: 0.5rem;
    }

    .filter-group {
      display: flex;
      flex-direction: column;
      gap: 0;
      min-width: 140px;
      flex-shrink: 0;
    }

    .filter-group.search-group {
      min-width: 220px;
      flex: 1;
    }

    .filter-group.date-range-group {
      min-width: 180px;
    }

    .filter-group.filter-actions-group {
      min-width: auto;
    }

    .filter-actions {
      flex-direction: row;
      gap: 0.5rem;
      align-items: center;
    }

    :host ::ng-deep .date-picker-range {
      .p-inputtext {
        min-height: 34px;
        border-radius: 8px;
        border-color: var(--surface-border);
        font-size: 0.8125rem;
      }

      .p-inputtext:focus {
        box-shadow: 0 0 0 3px var(--color-primary-light);
        border-color: var(--color-primary);
      }

      .p-datepicker-trigger {
        background: var(--surface-ground);
        border-color: var(--surface-border);
      }

      .p-datepicker-trigger:hover {
        background: var(--surface-hover);
      }
    }

    :host ::ng-deep .filter-dropdown .p-dropdown {
      min-height: 34px;
    }
    .search-input-wrapper {
      position: relative;
      display: flex;
      align-items: center;
      background: var(--surface-card);
      border: 1px solid var(--surface-border);
      border-radius: 8px;
      padding: 0 0.75rem;
      min-height: 34px;
    }

    .search-input-wrapper i {
      color: var(--text-color-secondary);
      margin-right: 0.5rem;
      font-size: 0.875rem;
    }

    .search-input {
      border: none;
      outline: none;
      font-size: 0.8125rem;
      width: 100%;
      background: transparent;
      color: var(--text-color);
    }

    .search-clear {
      background: none;
      border: none;
      color: var(--text-color-secondary);
      cursor: pointer;
      padding: 0;
      margin-left: 0.5rem;
      font-size: 0.875rem;
    }

    .search-clear:hover {
      color: var(--text-color);
    }

    .btn-search,
    .btn-clear {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 34px;
      height: 34px;
      border-radius: 8px;
      border: 1px solid var(--surface-border);
      cursor: pointer;
      transition: all 0.2s ease;
      font-size: 0.875rem;
    }

    .btn-search {
      background: var(--color-info);
      border-color: var(--color-info);
      color: #fff;
    }

    .btn-search:hover {
      opacity: 0.88;
    }

    .btn-clear {
      background: var(--surface-card);
      color: var(--text-color-secondary);
    }

    .btn-clear:hover {
      background: var(--surface-hover);
      border-color: var(--surface-border);
    }

    .search-clear {
      border: none;
      background: transparent;
      cursor: pointer;
      color: var(--text-color-secondary);
    }

    .active-filters {
      display: flex;
      flex-wrap: wrap;
      gap: 0.5rem;
      align-items: center;
      margin-bottom: 1rem;
    }

    .active-filters-label {
      font-size: 0.75rem;
      color: var(--text-color-secondary);
      font-weight: 600;
    }

    .filter-chip {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding: 0.25rem 0.5rem;
      border-radius: 9999px;
      background: var(--color-info-light);
      color: var(--color-info);
      font-size: 0.75rem;
      font-weight: 600;
    }

    .filter-chip i {
      cursor: pointer;
      color: var(--color-info);
    }

    @media (max-width: 768px) {
      .filters-row {
        flex-direction: column;
        align-items: stretch;
      }

      .filter-group {
        width: 100%;
        min-width: 0;
      }

      .filter-actions {
        width: 100%;
        justify-content: stretch;
      }

      .btn-search,
      .btn-clear {
        width: 100%;
        justify-content: center;
      }
    }

    .history-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;
      padding: 0;
    }

    .history-header h3 {
      margin: 0;
      font-size: 1.125rem;
      font-weight: 600;
    }

    .text-sm {
      font-size: 0.875rem;
    }

    .text-gray-600 {
      color: var(--text-color-secondary);
    }

    .font-semibold {
      font-weight: 600;
    }

    .text-right {
      text-align: right;
    }

    .text-green {
      color: var(--color-success);
    }

    .text-red {
      color: var(--color-danger);
    }

    .text-orange {
      color: var(--color-warning);
    }

    /* Dual Unit Display Styles */
    .dual-unit-cell {
      display: flex;
      flex-direction: column;
      gap: 2px;
      align-items: flex-end;
    }

    .display-qty {
      font-weight: 600;
      font-size: 14px;
    }

    .display-unit {
      font-size: 11px;
      color: var(--text-color-secondary);
      font-weight: 500;
    }

    .base-qty {
      font-size: 11px;
      color: var(--text-color-secondary);
      font-weight: 400;
    }

    @media (max-width: 768px) {
      .dual-unit-cell {
        align-items: flex-start;
      }
    }
  `]
})
export class StockMovementHistoryComponent implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);

  movements: StockMovementResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  // Movement rows already carry their own partName/partCode/displayName from the API, so this is
  // only a defensive fallback for a missing name — resolved on demand per partId, not a
  // capped catalog preload (which could never cover a large parts catalog anyway).
  private partNameCache = new Map<string, string>();
  private partNameLoading = new Set<string>();
  loading = false;
  totalRecords = 0;
  pageNumber = 1;
  pageSize = 10;
  first = 0;

  searchTerm = '';
  filterType = '';
  filterStatus = '';
  dateRange: Date[] = [];
  today = new Date();

  movementTypeOptions = [
    { label: 'All Types', value: '' },
    { label: 'Stock In', value: 'IN' },
    { label: 'Stock Out', value: 'OUT' },
    { label: 'Return', value: 'RETURN' },
    { label: 'Adjustment', value: 'ADJUST' },
    { label: 'Transfer', value: 'TRANSFER' }
  ];

  statusOptions = [
    { label: 'All Statuses', value: '' },
    { label: 'Pending', value: 'PENDING' },
    { label: 'Approved', value: 'APPROVED' }
  ];

  ngOnInit(): void {
    this.loadAllData();
  }

  private loadAllData(): void {
    this.loading = true;
    this.warehouseService
      .getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] })
      .pipe(map(res => res.data ?? []))
      .subscribe({
        next: (warehouses) => {
          this.warehouses = warehouses;
          this.loadMovements();
        },
        error: (error) => {
          console.error('Error loading reference data:', error);
          this.loading = false;
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load reference data'
          });
        }
      });
  }

  /** Resolve a part's display label on demand (cache + single lookup) instead of preloading the catalog. */
  private resolvePartLabel(partId: string): string {
    const cached = this.partNameCache.get(partId);
    if (cached) return cached;
    this.fetchPartName(partId);
    return partId;
  }

  private fetchPartName(partId: string): void {
    if (!partId || this.partNameLoading.has(partId) || this.partNameCache.has(partId)) return;
    this.partNameLoading.add(partId);
    this.partService.getPartById(partId).subscribe({
      next: (part) => {
        const code = part.partNumber || part.sku || '';
        const label = code ? `${part.name} (${code})` : part.name;
        this.partNameCache.set(partId, label);
        this.partNameLoading.delete(partId);
      },
      error: (_error) => {
        this.partNameLoading.delete(partId);
      }
    });
  }

  loadMovements(): void {
    this.loading = true;
    const fromDate = this.dateRange && this.dateRange.length > 0 ? this.dateRange[0] : null;
    const toDate = this.dateRange && this.dateRange.length > 1 ? this.dateRange[1] : null;
    
    this.stockService.getStockMovements({
      search: this.searchTerm,
      pageNumber: this.pageNumber,
      pageSize: this.pageSize,
      type: this.filterType || undefined,
      status: this.filterStatus || undefined,
      fromDate: fromDate ? fromDate.toISOString() : undefined,
      toDate: toDate ? toDate.toISOString() : undefined
    }).subscribe({
      next: (response) => {
        this.movements = response.data;
        this.totalRecords = response.pagination.totalCount;
        this.loading = false;
      },
      error: (_error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load stock movements'
        });
        this.loading = false;
      }
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    this.first = event.first ?? 0;
    this.pageSize = event.rows ?? 10;
    this.pageNumber = Math.floor(this.first / this.pageSize) + 1;
    this.loadMovements();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.filterType || this.filterStatus || (this.dateRange && this.dateRange.length > 0));
  }

  onSearch(): void {
    this.resetPagination();
    this.loadMovements();
  }

  onFilterChange(): void {
    this.resetPagination();
    this.loadMovements();
  }

  onDateChange(): void {
    this.resetPagination();
    this.loadMovements();
  }

  onClearDateRange(): void {
    this.dateRange = [];
    this.resetPagination();
    this.loadMovements();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.filterType = '';
    this.filterStatus = '';
    this.dateRange = [];
    this.resetPagination();
    this.loadMovements();
  }

  private resetPagination(): void {
    this.first = 0;
    this.pageNumber = 1;
  }

  getPartDisplay(movement: StockMovementResponse): string {
    // Prefer the composed "Base - Variant" name so variant rows are distinguishable.
    const name = movement.displayName || movement.partName;
    // Variant rows use their own SKU; non-variant rows use the base part code.
    const code = movement.variantSku || movement.partCode;
    if (name) {
      return code ? `${name} (${code})` : name;
    }
    // Fallback: resolve on demand (movement rows normally already carry partName/displayName).
    return movement.partId ? this.resolvePartLabel(movement.partId) : 'Unknown Part';
  }

  getWarehouseDisplay(movement: StockMovementResponse): string {
    // Use warehouseName and warehouseCode directly from the API response
    if (movement.warehouseName) {
      return movement.warehouseCode ? `${movement.warehouseName} (${movement.warehouseCode})` : movement.warehouseName;
    }
    // Fallback: try to find in local warehouses array
    const warehouse = this.warehouses.find(w => w.id === movement.warehouseId);
    if (warehouse) {
      return warehouse.code ? `${warehouse.name} (${warehouse.code})` : warehouse.name;
    }
    return movement.warehouseId || 'Unknown Warehouse';
  }

  // Keep old methods for backward compatibility
  getPartInfo(partId: string): string {
    return this.resolvePartLabel(partId);
  }

  getWarehouseName(warehouseId: string): string {
    const warehouse = this.warehouses.find(w => w.id === warehouseId);
    if (warehouse) {
      // Show name with code
      return warehouse.code ? `${warehouse.name} (${warehouse.code})` : warehouse.name;
    }
    return warehouseId; // Fallback to ID if warehouse not found
  }

  getMovementTypeLabel(type: string): string {
    const types: { [key: string]: string } = {
      'IN': 'Stock In',
      'OUT': 'Stock Out',
      'RETURN': 'Return',
      'ADJUST': 'Adjustment',
      'ADJUSTMENT': 'Adjustment',
      'TRANSFER': 'Transfer',
      'DAMAGE': 'Damage',
      'SHRINKAGE': 'Shrinkage',
      'COUNT_CORRECTION': 'Count Correction'
    };
    return types[type] || type;
  }

  getMovementTypeSeverity(type: string): 'success' | 'info' | 'danger' | 'warn' {
    switch (type) {
      case 'IN':
      case 'COUNT_CORRECTION':
        return 'success';
      case 'OUT':
      case 'DAMAGE':
      case 'SHRINKAGE':
        return 'danger';
      case 'RETURN':
      case 'TRANSFER':
        return 'info';
      case 'ADJUST':
      case 'ADJUSTMENT':
        return 'warn';
      default:
        return 'info';
    }
  }

  getStatusSeverity(status: string): 'success' | 'info' | 'danger' | 'warn' {
    switch (status) {
      case 'PENDING':
        return 'warn';
      case 'APPROVED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'info';
    }
  }

  getQuantityDisplay(type: string, quantity: number): string {
    if (type === 'IN' || type === 'COUNT_CORRECTION') {
      return `+${quantity}`;
    } else if (type === 'OUT' || type === 'DAMAGE' || type === 'SHRINKAGE') {
      return `-${quantity}`;
    } else if (type === 'RETURN' || type === 'ADJUST' || type === 'ADJUSTMENT') {
      // RETURN can be IN or OUT depending on context, show neutral
      return `${quantity}`;
    } else if (type === 'TRANSFER') {
      return `${quantity}`;
    } else {
      return `-${quantity}`;
    }
  }

  formatBaseQuantity(movement: StockMovementResponse): string {
    const qty = movement.quantityInBaseUnit || 0;
    if (movement.type === 'IN' || movement.type === 'COUNT_CORRECTION') {
      return `+${qty}`;
    } else if (movement.type === 'OUT' || movement.type === 'DAMAGE' || movement.type === 'SHRINKAGE') {
      return `-${qty}`;
    }
    return `${qty}`;
  }

  getQuantityClass(type: string): string {
    if (type === 'IN' || type === 'COUNT_CORRECTION') {
      return 'text-green';
    } else if (type === 'OUT' || type === 'DAMAGE' || type === 'SHRINKAGE') {
      return 'text-red';
    } else if (type === 'RETURN' || type === 'TRANSFER' || type === 'ADJUST' || type === 'ADJUSTMENT') {
      return 'text-orange';
    }
    return 'text-orange';
  }

  getMovementDescription(movement: StockMovementResponse): string {
    // Provide context based on reference number prefix
    const ref = movement.reference || '';
    const reason = movement.reason || '';

    if (ref.startsWith('PR-')) {
      return 'Returned to supplier';
    } else if (ref.startsWith('INV-') || reason.includes('Quick Sale')) {
      return 'Quick sale to customer';
    } else if (ref.startsWith('SO-') || ref.startsWith('Sales Order')) {
      return 'Sold to customer';
    } else if (ref.startsWith('SR-') || ref.includes('Sales Return')) {
      return 'Returned by customer';
    } else if (ref.startsWith('GRN-') || ref.includes('GRN')) {
      return 'Received from supplier';
    } else if (ref.startsWith('ADJ-')) {
      return 'Stock adjustment';
    } else if (ref.startsWith('TRF-')) {
      return 'Warehouse transfer';
    }

    return movement.notes || '';
  }
}
