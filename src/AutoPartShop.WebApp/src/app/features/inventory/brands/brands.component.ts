import { Component, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { BrandResponse, BrandService } from '../services/brand.service';
import { BrandsListComponent } from './brands-list/brands-list.component';
import { BrandsFormDialogComponent } from './brands-form-dialog/brands-form-dialog.component';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { TooltipModule } from 'primeng/tooltip';
import { tap, Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-brands',
    standalone: true,
    imports: [CommonModule, FormsModule, ToastModule, ConfirmDialogModule, ButtonModule, TooltipModule, Select, BrandsListComponent, BrandsFormDialogComponent, PageContainerComponent, PageHeaderComponent, FilterBarComponent, TranslatePipe],
    providers: [BrandService, MessageService, ConfirmationService],
    templateUrl: './brands.component.html',
    styleUrls: ['./brands.component.css']
})
export class BrandsComponent implements OnInit {
    private readonly brandService = inject(BrandService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);
    private readonly searchSubject$ = new Subject<string>();

    // Data
    brands: BrandResponse[] = [];
    selectedBrand: BrandResponse | null = null;

    // Dialog visibility
    displayCreateDialog = false;
    displayUpdateDialog = false;

    // Pagination & loading
    loading = false;
    togglingStatusId: string | null = null; // prevents double-click on toggle
    totalRecords = 0;
    rows = 10;
    currentPage = 1;

    // Filters
    searchTerm = '';
    filterStatus: boolean | null = null;
    filterCountry = '';

    statusOptions: { label: string; value: boolean | null }[] = [];

    private buildStatusOptions(): void {
        this.statusOptions = [
            { label: this.i18n.t('common.status.all'), value: null },
            { label: this.i18n.t('common.status.active'), value: true },
            { label: this.i18n.t('common.status.inactive'), value: false }
        ];
    }

    ngOnInit(): void {
        this.buildStatusOptions();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildStatusOptions();
        });
        this.setupSearchDebounce();
        this.loadBrands();
    }

    private setupSearchDebounce(): void {
        this.searchSubject$.pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.onSearch();
        });
    }

    onSearchInput(): void {
        this.searchSubject$.next(this.searchTerm);
    }

    loadBrands(page = 1, pageSize = this.rows): void {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        this.loading = true;
        this.brandService
            .getBrands({
                search: this.searchTerm || undefined,
                isActive: this.filterStatus,
                country: this.filterCountry || undefined,
                page,
                pageSize
            })
            .subscribe({
                next: (response) => {
                    this.brands = response.data ?? [];
                    this.totalRecords = response.pagination.totalCount;
                    this.rows = response.pagination.pageSize;
                    this.currentPage = response.pagination.page;
                    this.loading = false;
                },
                error: () => {
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('brands.messages.loadFailed') });
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.loadBrands(1, this.rows);
    }
    onFilterChange(): void {
        this.loadBrands(1, this.rows);
    }
    refreshData(): void {
        this.loadBrands(this.currentPage, this.rows);
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.filterCountry = '';
        this.loadBrands(1, this.rows);
    }

    hasActiveFilters(): boolean {
        return !!this.searchTerm || this.filterStatus !== null || !!this.filterCountry;
    }

    onPageChange(event: { page: number; rows: number }): void {
        this.loadBrands(event.page, event.rows);
    }

    // ── Dialogs ────────────────────────────────────────────────────────────────

    onNewBrandClick(): void {
        this.displayCreateDialog = true;
        this.displayUpdateDialog = false;
    }

    /** Alias used by the template header button. */
    createBrand(): void {
        this.onNewBrandClick();
    }

    /** Clear search text only — does not reload. */
    clearSearchInput(): void {
        this.searchTerm = '';
    }

    /** Label for status filter chip. */
    getStatusLabel(isActive: boolean | null): string {
        if (isActive === true) return this.i18n.t('common.status.active');
        if (isActive === false) return this.i18n.t('common.status.inactive');
        return this.i18n.t('common.status.all');
    }

    selectAndOpenUpdate(brand: BrandResponse): void {
        this.selectedBrand = brand;
        this.displayUpdateDialog = true;
    }

    toggleBrandStatus(brand: BrandResponse): void {
        if (this.togglingStatusId === brand.id) return; // guard against double-click
        this.togglingStatusId = brand.id;

        this.brandService
            .updateBrand(brand.id, {
                name: brand.name,
                description: brand.description,
                logoUrl: brand.logoUrl,
                website: brand.website,
                country: brand.country,
                contactEmail: brand.contactEmail,
                contactPhone: brand.contactPhone,
                displayOrder: brand.displayOrder,
                isActive: !brand.isActive
            })
            .subscribe({
                next: () => {
                    const detailKey = brand.isActive ? 'brands.messages.deactivateSuccess' : 'brands.messages.activateSuccess';
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t(detailKey) });
                    this.togglingStatusId = null;
                    this.loadBrands(this.currentPage, this.rows);
                },
                error: () => {
                    this.togglingStatusId = null;
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('common.messages.updateFailed') });
                }
            });
    }

    onCreateSuccess(): void {
        this.loadBrands(this.currentPage, this.rows);
    }
    onUpdateSuccess(): void {
        this.loadBrands(this.currentPage, this.rows);
    }

    onDisplayCreateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayCreateDialog = false;
    }
    onDisplayUpdateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayUpdateDialog = false;
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    selectAndDelete(brand: BrandResponse): void {
        this.selectedBrand = brand;
        this.confirmationService.confirm({
            message: this.i18n.t('brands.messages.deleteConfirm', { name: brand.name }),
            header: this.i18n.t('common.messages.confirmDeletion'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.brandService
                    .deleteBrand(brand.id)
                    .pipe(
                        tap(() => {
                            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('brands.messages.deleteSuccess') });
                            this.selectedBrand = null;
                            // If we deleted the last item on a page beyond page 1, go back a page
                            const isLastItemOnPage = this.brands.length === 1 && this.currentPage > 1;
                            this.loadBrands(isLastItemOnPage ? this.currentPage - 1 : this.currentPage, this.rows);
                        })
                    )
                    .subscribe({
                        error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('brands.messages.deleteFailed') })
                    });
            }
        });
    }
}
