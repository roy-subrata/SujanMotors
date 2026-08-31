import { Component, OnInit, Input, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ButtonModule } from 'primeng/button';
import { TableModule } from 'primeng/table';
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
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
    DatePicker,
    DataPaginationComponent,
    TranslatePipe
  ],
  providers: [MessageService],
  templateUrl: './stock-movement-history.component.html',
  styleUrl: './stock-movement-history.component.scss',
})
export class StockMovementHistoryComponent implements OnInit {
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchSubject$ = new Subject<string>();

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

  /** Set by the parent stock page ("view history" on a stock row) to scope the list to one part. */
  @Input() partFilter: { partId: string; label: string } | null = null;

  /** Getters, not fields: resolving t() once at construction would freeze the labels in whichever
   *  language happened to be active then, instead of following the language switcher. */
  get movementTypeOptions() {
    return [
      { label: this.i18n.t('stockMovements.typeOptions.all'), value: '' },
      { label: this.i18n.t('stockMovements.types.IN'), value: 'IN' },
      { label: this.i18n.t('stockMovements.types.OUT'), value: 'OUT' },
      { label: this.i18n.t('stockMovements.types.RETURN'), value: 'RETURN' },
      { label: this.i18n.t('stockMovements.types.ADJUST'), value: 'ADJUST' },
      { label: this.i18n.t('stockMovements.types.TRANSFER'), value: 'TRANSFER' }
    ];
  }

  get statusOptions() {
    return [
      { label: this.i18n.t('stockMovements.statusOptions.all'), value: '' },
      { label: this.i18n.t('stockMovements.statuses.PENDING'), value: 'PENDING' },
      { label: this.i18n.t('stockMovements.statuses.APPROVED'), value: 'APPROVED' }
    ];
  }

  ngOnInit(): void {
    this.setupSearchDebounce();
    this.loadAllData();
  }

  private setupSearchDebounce(): void {
    this.searchSubject$.pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.onSearch();
    });
  }

  onSearchInput(): void {
    this.searchSubject$.next(this.searchTerm);
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
            summary: this.i18n.t('common.messages.error'),
            detail: this.i18n.t('stockMovements.messages.loadReferenceFailed')
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
      partId: this.partFilter?.partId,
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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('stockMovements.messages.loadFailed')
        });
        this.loading = false;
      }
    });
  }

  goToPage(page: number): void {
    this.pageNumber = page;
    this.first = (page - 1) * this.pageSize;
    this.loadMovements();
  }

  onPageSizeChange(size: number): void {
    this.pageSize = size;
    this.resetPagination();
    this.loadMovements();
  }

  hasActiveFilters(): boolean {
    return !!(this.searchTerm || this.filterType || this.filterStatus || this.partFilter || (this.dateRange && this.dateRange.length > 0));
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
    this.partFilter = null;
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
    return movement.partId ? this.resolvePartLabel(movement.partId) : this.i18n.t('stockMovements.unknownPart');
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
    return movement.warehouseId || this.i18n.t('stockMovements.unknownWarehouse');
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
    if (!type) return '';
    const key = 'stockMovements.types.' + type;
    const label = this.i18n.t(key);
    return label === key ? type : label;
  }

  getStatusLabel(status: string): string {
    if (!status) return '';
    const key = 'stockMovements.statuses.' + status;
    const label = this.i18n.t(key);
    return label === key ? status : label;
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
      return this.i18n.t('stockMovements.descriptions.returnedToSupplier');
    } else if (ref.startsWith('INV-') || reason.includes('Quick Sale')) {
      return this.i18n.t('stockMovements.descriptions.quickSale');
    } else if (ref.startsWith('SO-') || ref.startsWith('Sales Order')) {
      return this.i18n.t('stockMovements.descriptions.soldToCustomer');
    } else if (ref.startsWith('SR-') || ref.includes('Sales Return')) {
      return this.i18n.t('stockMovements.descriptions.returnedByCustomer');
    } else if (ref.startsWith('GRN-') || ref.includes('GRN')) {
      return this.i18n.t('stockMovements.descriptions.receivedFromSupplier');
    } else if (ref.startsWith('ADJ-')) {
      return this.i18n.t('stockMovements.descriptions.stockAdjustment');
    } else if (ref.startsWith('TRF-')) {
      return this.i18n.t('stockMovements.descriptions.warehouseTransfer');
    }

    return movement.notes || '';
  }
}
