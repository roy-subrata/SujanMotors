import { Component, OnInit, ViewChild, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { TableModule, TableLazyLoadEvent } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { MenuModule, Menu } from 'primeng/menu';
import { TooltipModule } from 'primeng/tooltip';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { SkeletonModule } from 'primeng/skeleton';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';
import { PartsImportDialogComponent } from './parts-import-dialog/parts-import-dialog.component';
import { PartService, PartResponse } from '../services/part.service';
import { CategoryService, CategoryResponse } from '../services/category.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { PriceCodeService } from '@/shared/services/price-code.service';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { DataPaginationComponent } from '@/shared/components/data-pagination/data-pagination.component';
import { StatStripComponent, StatStripItem } from '@/shared/components/stat-strip/stat-strip.component';
import { StatusPillFilterComponent } from '@/shared/components/status-pill-filter/status-pill-filter.component';
import { MoreFiltersDialogComponent } from '@/shared/components/more-filters-dialog/more-filters-dialog.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-parts',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        InputTextModule,
        Select,
        TagModule,
        MenuModule,
        TooltipModule,
        ToastModule,
        ConfirmDialogModule,
        SkeletonModule,
        PageContainerComponent,
        PageHeaderComponent,
        FilterBarComponent,
        DataPaginationComponent,
        StatStripComponent,
        StatusPillFilterComponent,
        MoreFiltersDialogComponent,
        PartsImportDialogComponent,
        TranslatePipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './parts.component.html',
    styleUrls: ['./parts.component.css']
})
export class PartsComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly categoryService = inject(CategoryService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly currencyService = inject(CurrencyService);
    readonly priceCodeService = inject(PriceCodeService);
    private readonly router = inject(Router);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    @ViewChild('actionMenu') actionMenu!: Menu;

    parts: PartResponse[] = [];
    selectedPart: PartResponse | null = null;
    loading = false;
    totalRecords = 0;
    currentPage = 1;
    pageSize = 10;
    searchTerm = '';
    filterStatus = '';
    filterCategoryId = '';
    sortValue = '';

    actionMenuItems: MenuItem[] = [];
    importDialogVisible = false;
    moreFiltersVisible = false;
    stats: StatStripItem[] = [];

    statusOptions: { label: string; value: string }[] = [];

    /** Category names come from the API (not translatable); only the "All Categories" label is re-built on language switch. */
    categoryOptions: { label: string; value: string }[] = [];
    private loadedCategories: { label: string; value: string }[] = [];

    /** value encodes "field_direction" — matches SortOption.Field on the backend Part entity. */
    sortOptions: { label: string; value: string }[] = [];

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('common.status.allStatuses'), value: '' },
            { label: this.i18n.t('common.status.active'), value: 'ACTIVE' },
            { label: this.i18n.t('common.status.inactive'), value: 'INACTIVE' }
        ];
    }

    private buildCategoryOptions(): void {
        this.categoryOptions = [
            { label: this.i18n.t('parts.allCategories'), value: '' },
            ...this.loadedCategories
        ];
    }

    private buildSortOptions(): void {
        this.sortOptions = [
            { label: this.i18n.t('parts.sort'), value: '' },
            { label: this.i18n.t('parts.sortNameAsc'), value: 'Name_asc' },
            { label: this.i18n.t('parts.sortNameDesc'), value: 'Name_desc' },
            { label: this.i18n.t('parts.sortPriceAsc'), value: 'SellingPrice_asc' },
            { label: this.i18n.t('parts.sortPriceDesc'), value: 'SellingPrice_desc' }
        ];
    }

    Math = Math;

    get totalPages(): number { return Math.max(1, Math.ceil(this.totalRecords / this.pageSize)); }
    get first(): number { return (this.currentPage - 1) * this.pageSize; }

    constructor() {}

    ngOnInit(): void {
        this.buildStatusOptions();
        this.buildCategoryOptions();
        this.buildSortOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
            this.buildCategoryOptions();
            this.buildSortOptions();
            this.buildStats();
        });

        this.loadData();
        this.loadStats();
        this.categoryService.getActiveCategories().subscribe({
            next: (categories: CategoryResponse[]) => {
                this.loadedCategories = categories.map(c => ({ label: c.name, value: c.id }));
                this.buildCategoryOptions();
            },
            error: () => { /* dropdown just stays "All Categories" — not worth a toast */ }
        });
    }

    /**
     * Grand-total counts for the stat strip — always reflect the whole
     * catalog, not the currently-filtered table, so the strip doesn't
     * jump around as the user filters below it. Reuses the same list
     * endpoint with pageSize:1, reading only response.pagination.totalCount
     * — no dedicated summary endpoint needed.
     */
    private statCounts: { total: number; active: number; inactive: number } | null = null;

    private loadStats(): void {
        forkJoin({
            total: this.partService.getParts({ search: '', pageNumber: 1, pageSize: 1 }),
            active: this.partService.getParts({ search: '', pageNumber: 1, pageSize: 1, isActive: true }),
            inactive: this.partService.getParts({ search: '', pageNumber: 1, pageSize: 1, isActive: false })
        }).subscribe({
            next: ({ total, active, inactive }) => {
                this.statCounts = {
                    total: total.pagination.totalCount,
                    active: active.pagination.totalCount,
                    inactive: inactive.pagination.totalCount
                };
                this.buildStats();
            },
            error: () => { /* strip just stays empty — not worth a toast */ }
        });
    }

    /** Re-labels the stat strip from cached counts — called on load and on language switch. */
    private buildStats(): void {
        if (!this.statCounts) return;
        this.stats = [
            { label: this.i18n.t('parts.statTotalParts'), value: String(this.statCounts.total) },
            { label: this.i18n.t('common.status.active'), value: String(this.statCounts.active) },
            { label: this.i18n.t('common.status.inactive'), value: String(this.statCounts.inactive) }
        ];
    }

    onStatusFilterChange(value: string): void {
        this.filterStatus = value;
        this.onFilterChange();
    }

    loadData(page = this.currentPage, rows = this.pageSize): void {
        this.loading = true;
        this.currentPage = page;
        this.pageSize = rows;
        const isActive = this.filterStatus === 'ACTIVE' ? true : this.filterStatus === 'INACTIVE' ? false : undefined;
        const [sortBy, sortDirection] = this.sortValue ? this.sortValue.split('_') : [undefined, undefined];
        this.partService.getParts({
            search: this.searchTerm,
            pageNumber: page,
            pageSize: rows,
            isActive,
            categoryId: this.filterCategoryId || undefined,
            sortBy,
            sortDirection: sortDirection as 'asc' | 'desc' | undefined
        }).subscribe({
            next: (response) => {
                this.parts = response.data;
                this.totalRecords = response.pagination.totalCount;
                this.loading = false;
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.messages.loadFailed') });
                this.loading = false;
            }
        });
    }

    goToPage(page: number): void {
        if (page < 1 || page > this.totalPages) return;
        this.loadData(page, this.pageSize);
    }

    onPageSizeChange(rows: number): void {
        this.loadData(1, rows);
    }

    /**
     * Handle create button click
     */
    createPart(): void {
        this.router.navigate(['/inventory/parts/create']);
    }

    /**
     * Open the bulk import dialog
     */
    openImportDialog(): void {
        this.importDialogVisible = true;
    }

    /**
     * Handle a completed import: notify and refresh the list
     */
    onPartsImported(createdCount: number): void {
        if (createdCount > 0) {
            this.messageService.add({
                severity: 'success',
                summary: this.i18n.t('parts.messages.importComplete'),
                detail: this.i18n.t('parts.messages.importedCount', { count: String(createdCount) })
            });
            this.loadData(1, this.pageSize);
        }
    }

    /**
     * Handle search
     */
    onSearch(): void { this.loadData(1, this.pageSize); }
    onFilterChange(): void { this.loadData(1, this.pageSize); }
    refreshData(): void { this.loadData(this.currentPage, this.pageSize); }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = '';
        this.filterCategoryId = '';
        this.sortValue = '';
        this.loadData(1, this.pageSize);
    }

    /**
     * Check if filters are active
     */
    hasActiveFilters(): boolean {
        return !!(this.searchTerm || this.filterStatus || this.filterCategoryId || this.sortValue);
    }

    get filterCategoryLabel(): string {
        return this.categoryOptions.find(c => c.value === this.filterCategoryId)?.label ?? '';
    }

    onLazyLoad(event: TableLazyLoadEvent): void {
        const page = Math.floor((event.first ?? 0) / (event.rows ?? this.pageSize)) + 1;
        this.loadData(page, event.rows ?? this.pageSize);
    }

    /**
     * Show action menu for a part
     */
    showActionMenu(event: Event, part: PartResponse): void {
        this.selectedPart = part;
        this.actionMenuItems = [
            {
                label: this.i18n.t('common.actions.viewDetails'),
                icon: 'pi pi-eye',
                command: () => this.viewPart(part)
            },
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => this.editPart(part)
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.activate'),
                icon: 'pi pi-check',
                command: () => this.activatePart(part),
                visible: !part.isActive
            },
            {
                label: this.i18n.t('common.actions.deactivate'),
                icon: 'pi pi-times',
                command: () => this.deactivatePart(part),
                visible: part.isActive
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => this.deletePart(part),
                styleClass: 'text-red-600'
            }
        ];

        this.actionMenu.toggle(event);
    }

    /**
     * View part details
     */
    viewPart(part: PartResponse): void {
        this.router.navigate(['/inventory/parts', part.id]);
    }

    /**
     * Edit part
     */
    editPart(part: PartResponse): void {
        this.router.navigate(['/inventory/parts/edit'], {
            queryParams: { id: part.id, mode: 'edit' }
        });
    }

    /**
     * Activate part
     */
    private activatePart(part: PartResponse): void {
        this.partService.activatePart(part.id).subscribe({
            next: (updatedPart) => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('parts.messages.activateSuccess')
                });
                const index = this.parts.findIndex(p => p.id === part.id);
                if (index !== -1) {
                    this.parts[index] = updatedPart;
                    this.parts = [...this.parts];
                }
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('parts.messages.activateFailed')
                });
            }
        });
    }

    /**
     * Deactivate part
     */
    private deactivatePart(part: PartResponse): void {
        this.partService.deactivatePart(part.id).subscribe({
            next: (updatedPart) => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('parts.messages.deactivateSuccess')
                });
                const index = this.parts.findIndex(p => p.id === part.id);
                if (index !== -1) {
                    this.parts[index] = updatedPart;
                    this.parts = [...this.parts];
                }
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('parts.messages.deactivateFailed')
                });
            }
        });
    }

    /**
     * Delete part
     */
    private deletePart(part: PartResponse): void {
        this.confirmationService.confirm({
            message: this.i18n.t('parts.messages.deleteConfirm', { name: part.name }),
            header: this.i18n.t('parts.deletePart'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                this.partService.deletePart(part.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('parts.messages.deleteSuccess')
                        });
                        this.loadData();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('parts.messages.deleteFailed')
                        });
                    }
                });
            }
        });
    }

    /**
     * Format currency
     */
    formatCurrency(amount: number): string {
        const currency = this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currency);
    }

    formatCostPrice(amount: number): string {
        const coded = this.priceCodeService.getDisplayPrice(amount);
        if (coded !== null) return coded;
        return this.formatCurrency(amount);
    }

    /**
     * Format status label
     */
    formatStatus(isActive: boolean): string {
        return isActive ? this.i18n.t('common.status.active') : this.i18n.t('common.status.inactive');
    }
}
