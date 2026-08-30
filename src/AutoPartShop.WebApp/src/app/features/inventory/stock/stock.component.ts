import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
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
import { StockTransferDialogComponent } from './stock-transfer-dialog.component';
import { StockMovementHistoryComponent } from './stock-movement-history.component';
import { StockLotsByWarehouseComponent } from './stock-lots-by-warehouse.component';
import { StockPriceHistoryComponent } from './stock-price-history.component';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { StatStripComponent, StatStripItem } from '@/shared/components/stat-strip/stat-strip.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
        StockPriceHistoryComponent,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent,
        StatStripComponent,
        TranslatePipe
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
    private readonly route = inject(ActivatedRoute);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    allStockLevels: StockLevelResponse[] = [];
    lowStockLevels: StockLevelResponse[] = [];
    warehouses: WarehouseResponse[] = [];
    // Stock rows already carry their own partName/partSku/displayName from the API, so this is
    // only a defensive fallback for a missing name — resolved on demand per partId, not a
    // capped catalog preload (which could never cover a large parts catalog anyway).
    private partCache = new Map<string, PartResponse>();
    private partLoading = new Set<string>();

    loading = false;
    searchTerm = '';
    activeTab = 0;

    // Part scope for the movement-history tab, set via a row's "view history" action.
    movementPartFilter: { partId: string; label: string } | null = null;

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
    stockStatusOptions: { label: string; value: string }[] = [];

    // Adds an "All" pill to the front — stockStatusOptions itself stays as-is since
    // other tabs/consumers of that exact list expect only the 4 real values.
    stockStatusPillOptions: { label: string; value: string }[] = [];

    stats: StatStripItem[] = [];

    /** Raw counts behind `stats` — kept around so the labels can be re-translated on language switch without a refetch. */
    private statCounts: { inStock: number; low: number; critical: number; outOfStock: number } | null = null;

    ngOnInit(): void {
        this.buildStockStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStockStatusOptions();
            this.buildStats();
        });

        // Deep-link support (e.g. the topbar reorder alert links to /inventory/stock?tab=low).
        // Subscribed (not snapshot) so the link also works when the page is already open.
        this.route.queryParamMap.subscribe((params) => {
            if (params.get('tab') === 'low') this.activeTab = 1;
        });

        this.loadWarehouses();
        this.loadAllStock();
        this.loadLowStock();
        this.loadStats();
    }

    private buildStockStatusOptions(): void {
        this.stockStatusOptions = [
            { label: this.i18n.t('stock.status.inStock'), value: 'in-stock' },
            { label: this.i18n.t('stock.lowStock'), value: 'low' },
            { label: this.i18n.t('stock.status.critical'), value: 'critical' },
            { label: this.i18n.t('stock.outOfStock'), value: 'out-of-stock' }
        ];
        this.stockStatusPillOptions = [{ label: this.i18n.t('common.status.all'), value: '' }, ...this.stockStatusOptions];
    }

    /**
     * Grand-total counts per stock-health status for the stat strip — independent of
     * the All Stock tab's live filters/search, so the strip doesn't jump around as the
     * user filters the table below it. Reuses the existing list endpoint with
     * pageSize:1 per status bucket, reading only response.pagination.totalCount.
     */
    private loadStats(): void {
        forkJoin({
            inStock: this.stockService.getStockLevels({ pageNumber: 1, pageSize: 1, status: 'in-stock' }),
            low: this.stockService.getStockLevels({ pageNumber: 1, pageSize: 1, status: 'low' }),
            critical: this.stockService.getStockLevels({ pageNumber: 1, pageSize: 1, status: 'critical' }),
            outOfStock: this.stockService.getStockLevels({ pageNumber: 1, pageSize: 1, status: 'out-of-stock' })
        }).subscribe({
            next: ({ inStock, low, critical, outOfStock }) => {
                this.statCounts = {
                    inStock: inStock.pagination.totalCount,
                    low: low.pagination.totalCount,
                    critical: critical.pagination.totalCount,
                    outOfStock: outOfStock.pagination.totalCount
                };
                this.buildStats();
            },
            error: () => {
                /* strip just stays empty — not worth a toast */
            }
        });
    }

    private buildStats(): void {
        if (!this.statCounts) return;
        this.stats = [
            { label: this.i18n.t('stock.status.inStock'), value: String(this.statCounts.inStock) },
            { label: this.i18n.t('stock.lowStock'), value: String(this.statCounts.low) },
            { label: this.i18n.t('stock.status.critical'), value: String(this.statCounts.critical) },
            { label: this.i18n.t('stock.outOfStock'), value: String(this.statCounts.outOfStock) }
        ];
    }

    loadAllStock(): void {
        this.loading = true;
        this.stockService
            .getStockLevels({
                search: this.allStockFilters.search,
                pageNumber: this.allPageNumber,
                pageSize: this.allPageSize,
                warehouseId: this.allStockFilters.warehouseId || undefined,
                status: this.allStockFilters.status || undefined
            })
            .subscribe({
                next: (response) => {
                    this.allStockLevels = response.data;
                    this.allTotalRecords = response.pagination.totalCount;
                    this.loading = false;
                },
                error: (_error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('stock.messages.loadLevelsFailed')
                    });
                    this.loading = false;
                }
            });
    }

    loadLowStock(): void {
        this.stockService
            .getStockLevels({
                search: this.lowStockFilters.search,
                pageNumber: this.lowPageNumber,
                pageSize: this.lowPageSize,
                warehouseId: this.lowStockFilters.warehouseId || undefined,
                lowStockOnly: true
            })
            .subscribe({
                next: (response) => {
                    this.lowStockLevels = response.data;
                    this.lowTotalRecords = response.pagination.totalCount;
                },
                error: (_error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: this.i18n.t('stock.messages.loadLowFailed')
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

    onAllStockStatusFilterChange(value: string): void {
        this.allStockFilters.status = value || null;
        this.onAllStockFilterChange();
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
            header: this.i18n.t('stock.adjustmentDialog.title'),
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
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('stock.adjustmentDialog.messages.success')
                });
                // Refresh stock levels
                this.loadAllStock();
                this.loadLowStock();
            }
        });
    }

    onNewStockEntry(): void {
        const dialogRef = this.dialogService.open(StockAdjustmentDialogComponent, {
            header: this.i18n.t('stock.newStockEntry'),
            width: '720px',
            breakpoints: {
                '960px': '95vw',
                '640px': '100vw'
            },
            styleClass: 'stock-adjustment-dialog',
            modal: true,
            data: { mode: 'create' }
        });

        dialogRef!.onClose.subscribe((result: any) => {
            if (result?.success) {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('stock.adjustmentDialog.messages.createSuccess')
                });
                this.loadAllStock();
                this.loadLowStock();
            }
        });
    }

    /** Resolve a part on demand (cache + single lookup) instead of preloading the catalog. */
    private resolvePart(partId: string): PartResponse | undefined {
        const cached = this.partCache.get(partId);
        if (cached) return cached;
        this.fetchPart(partId);
        return undefined;
    }

    private fetchPart(partId: string): void {
        if (!partId || this.partLoading.has(partId) || this.partCache.has(partId)) return;
        this.partLoading.add(partId);
        this.partService.getPartById(partId).subscribe({
            next: (part) => {
                this.partCache.set(partId, part);
                this.partLoading.delete(partId);
            },
            error: (_error) => {
                this.partLoading.delete(partId);
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
                this.warehouseOptions = this.warehouses.map((w) => ({
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

    goToAllPage(page: number): void {
        this.onAllLazyLoad({ first: (page - 1) * this.allPageSize, rows: this.allPageSize } as TableLazyLoadEvent);
    }

    onAllPageSizeChange(size: number): void {
        this.onAllLazyLoad({ first: 0, rows: size } as TableLazyLoadEvent);
    }

    goToLowPage(page: number): void {
        this.onLowLazyLoad({ first: (page - 1) * this.lowPageSize, rows: this.lowPageSize } as TableLazyLoadEvent);
    }

    onLowPageSizeChange(size: number): void {
        this.onLowLazyLoad({ first: 0, rows: size } as TableLazyLoadEvent);
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
     * Get part name for a given partId
     */
    getPartName(partId: string): string {
        const part = this.resolvePart(partId);
        return part?.name || partId;
    }

    /**
     * Get part SKU for a given partId
     */
    getPartSku(partId: string): string {
        const part = this.resolvePart(partId);
        return part?.sku || '';
    }

    /**
     * Get part name and code for a given partId
     */
    getPartInfo(partId: string): string {
        const part = this.resolvePart(partId);
        if (part) {
            return `${part.name} (${part.sku})`;
        }
        return partId;
    }

    /**
     * Get warehouse name for a given warehouseId
     */
    getWarehouseName(warehouseId: string): string {
        const warehouse = this.warehouses.find((w) => w.id === warehouseId);
        return warehouse?.name || warehouseId;
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
        if (availBase === 0) return this.i18n.t('stock.outOfStock');
        if (availBase <= stock.reorderLevel * 0.5) return this.i18n.t('stock.status.critical');
        if (availBase <= stock.reorderLevel) return this.i18n.t('stock.lowStock');
        return this.i18n.t('stock.status.inStock');
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
        if (ratio === 0 || stock.availableQuantity === 0) return this.i18n.t('stock.status.critical');
        if (ratio <= 0.25) return this.i18n.t('stock.status.high');
        return this.i18n.t('stock.status.medium');
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
        // Scope the movement-history tab to this part, then switch to it.
        this.movementPartFilter = {
            partId: stock.partId,
            label: stock.displayName || stock.partName || stock.partSku || stock.partId
        };
        this.activeTab = 2;
    }

    /**
     * Open the transfer dialog for a stock row (move on-hand quantity between warehouses).
     */
    onTransferStock(stock: StockLevelResponse): void {
        const dialogRef = this.dialogService.open(StockTransferDialogComponent, {
            header: this.i18n.t('stockTransfer.title'),
            width: '640px',
            breakpoints: {
                '960px': '95vw',
                '640px': '100vw'
            },
            modal: true,
            data: { stock }
        });

        dialogRef!.onClose.subscribe((result: any) => {
            if (result?.success) {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('stockTransfer.messages.success')
                });
                this.loadAllStock();
                this.loadLowStock();
            }
        });
    }

    /**
     * Create reorder for low stock item
     */
    createReorder(stock: StockLevelResponse): void {
        this.messageService.add({
            severity: 'info',
            summary: this.i18n.t('stock.messages.comingSoonTitle'),
            detail: this.i18n.t('stock.messages.poComingSoon')
        });
    }
}
