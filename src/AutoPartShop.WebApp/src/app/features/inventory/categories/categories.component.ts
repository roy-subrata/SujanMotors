import { Component, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CategoryResponse, CategoryService } from '../services/category.service';
import { CategoriesListComponent } from './categories-list/categories-list.component';
import { CategoriesFormDialogComponent } from './categories-form-dialog/categories-form-dialog.component';
import { tap } from 'rxjs';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';
import { FilterBarComponent } from '@/shared/components/filter-bar/filter-bar.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-categories',
    standalone: true,
    imports: [CommonModule, FormsModule, ToastModule, ConfirmDialogModule, ButtonModule, TooltipModule, Select, CategoriesListComponent, CategoriesFormDialogComponent, PageContainerComponent, PageHeaderComponent, FilterBarComponent, TranslatePipe],
    providers: [CategoryService, MessageService, ConfirmationService],
    templateUrl: './categories.component.html',
    styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
    private readonly categoryService = inject(CategoryService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    categories: CategoryResponse[] = [];
    selectedParentCategory: CategoryResponse | null = null;
    selectedCategory: CategoryResponse | null = null;

    displayCreateDialog = false;
    displayUpdateDialog = false;

    loading = false;
    togglingStatusId: string | null = null;
    totalRecords = 0;
    rows = 10;
    currentPage = 1;

    searchTerm = '';
    filterStatus: boolean | null = null;

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
        this.loadCategories();
    }

    loadCategories(page = 1, pageSize = this.rows): void {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        this.loading = true;
        this.categoryService
            .getCategories({
                search: this.searchTerm || undefined,
                isActive: this.filterStatus,
                page,
                pageSize
            })
            .subscribe({
                next: (response) => {
                    this.categories = response.data ?? [];
                    this.totalRecords = response.pagination.totalCount;
                    this.rows = response.pagination.pageSize;
                    this.currentPage = response.pagination.page;
                    this.loading = false;
                },
                error: () => {
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('categories.messages.loadFailed') });
                    this.loading = false;
                }
            });
    }

    onSearch(): void {
        this.loadCategories(1, this.rows);
    }
    onFilterChange(): void {
        this.loadCategories(1, this.rows);
    }
    refreshData(): void {
        this.loadCategories(this.currentPage, this.rows);
    }

    clearSearchInput(): void {
        this.searchTerm = '';
    }

    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.loadCategories(1, this.rows);
    }

    hasActiveFilters(): boolean {
        return !!this.searchTerm || this.filterStatus !== null;
    }

    getStatusLabel(isActive: boolean | null): string {
        if (isActive === true) return this.i18n.t('common.status.active');
        if (isActive === false) return this.i18n.t('common.status.inactive');
        return this.i18n.t('common.status.all');
    }

    onPageChange(event: { page: number; rows: number }): void {
        this.loadCategories(event.page, event.rows);
    }

    // ── Dialogs ────────────────────────────────────────────────────────────────

    onNewCategoryClick(): void {
        this.selectedParentCategory = null;
        this.displayCreateDialog = true;
        this.displayUpdateDialog = false;
    }

    createCategory(): void {
        this.onNewCategoryClick();
    }

    selectAndOpenUpdate(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.displayUpdateDialog = true;
    }

    selectAndAddSubcategory(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.selectedParentCategory = category;
        this.displayCreateDialog = true;
        this.displayUpdateDialog = false;
    }

    onCreateSuccess(): void {
        this.loadCategories(this.currentPage, this.rows);
    }
    onUpdateSuccess(): void {
        this.loadCategories(this.currentPage, this.rows);
    }

    onDisplayCreateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayCreateDialog = false;
    }
    onDisplayUpdateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayUpdateDialog = false;
    }

    // ── Toggle status ──────────────────────────────────────────────────────────

    selectAndToggleStatus(category: CategoryResponse): void {
        if (this.togglingStatusId === category.id) return;
        const isDeactivating = category.isActive;
        const confirmKey = isDeactivating ? 'categories.messages.deactivateConfirm' : 'categories.messages.activateConfirm';
        const header = isDeactivating ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate');

        this.confirmationService.confirm({
            message: this.i18n.t(confirmKey, { name: category.name }),
            header,
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.togglingStatusId = category.id;
                this.categoryService
                    .setStatus(category.id, !category.isActive)
                    .pipe(
                        tap(() => {
                            this.messageService.add({
                                severity: 'success',
                                summary: this.i18n.t('common.messages.success'),
                                detail: this.i18n.t('categories.messages.toggleSuccess')
                            });
                            this.togglingStatusId = null;
                            this.loadCategories(this.currentPage, this.rows);
                        })
                    )
                    .subscribe({
                        error: (err) => {
                            this.togglingStatusId = null;
                            const detail = err.error?.detail ?? err.error?.message ?? this.i18n.t('common.messages.updateFailed');
                            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail });
                        }
                    });
            }
        });
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    selectAndDelete(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.confirmationService.confirm({
            message: this.i18n.t('categories.messages.deleteConfirm'),
            header: this.i18n.t('common.messages.confirmDeletion'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.categoryService
                    .deleteCategory(category.id)
                    .pipe(
                        tap(() => {
                            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('categories.messages.deleteSuccess') });
                            this.selectedCategory = null;
                            const isLastItemOnPage = this.categories.length === 1 && this.currentPage > 1;
                            this.loadCategories(isLastItemOnPage ? this.currentPage - 1 : this.currentPage, this.rows);
                        })
                    )
                    .subscribe({
                        error: (err) => {
                            const detail = err.error?.detail ?? err.error?.message ?? this.i18n.t('categories.messages.deleteFailed');
                            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail });
                        }
                    });
            }
        });
    }
}
