import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { Select } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CategoryResponse, CategoryService } from '../services/category.service';
import { CategoriesListComponent } from './categories-list/categories-list.component';
import { CategoriesFormDialogComponent } from './categories-form-dialog/categories-form-dialog.component';
import { tap } from 'rxjs';
import { PageContainerComponent } from '@/shared/components/page-container/page-container.component';
import { PageHeaderComponent } from '@/shared/components/page-header/page-header.component';

@Component({
    selector: 'app-categories',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ToastModule, ConfirmDialogModule,
        ButtonModule, Select, CategoriesListComponent, CategoriesFormDialogComponent,
        PageContainerComponent, PageHeaderComponent
    ],
    providers: [CategoryService, MessageService, ConfirmationService],
    templateUrl: './categories.component.html',
    styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
    private readonly categoryService = inject(CategoryService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);

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

    statusOptions = [
        { label: 'All',      value: null  },
        { label: 'Active',   value: true  },
        { label: 'Inactive', value: false }
    ];

    ngOnInit(): void { this.loadCategories(); }

    loadCategories(page = 1, pageSize = this.rows): void {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;

        this.loading = true;
        this.categoryService.getCategories({
            search: this.searchTerm || undefined,
            isActive: this.filterStatus,
            page,
            pageSize
        }).subscribe({
            next: (response) => {
                this.categories   = response.data ?? [];
                this.totalRecords = response.pagination.totalCount;
                this.rows         = response.pagination.pageSize;
                this.currentPage  = response.pagination.page;
                this.loading      = false;
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories' });
                this.loading = false;
            }
        });
    }

    onSearch(): void       { this.loadCategories(1, this.rows); }
    onFilterChange(): void { this.loadCategories(1, this.rows); }
    refreshData(): void    { this.loadCategories(this.currentPage, this.rows); }

    clearSearchInput(): void { this.searchTerm = ''; }

    clearFilters(): void {
        this.searchTerm   = '';
        this.filterStatus = null;
        this.loadCategories(1, this.rows);
    }

    hasActiveFilters(): boolean {
        return !!this.searchTerm || this.filterStatus !== null;
    }

    getStatusLabel(isActive: boolean | null): string {
        if (isActive === true)  return 'Active';
        if (isActive === false) return 'Inactive';
        return 'All';
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

    createCategory(): void { this.onNewCategoryClick(); }

    selectAndOpenUpdate(category: CategoryResponse): void {
        this.selectedCategory    = category;
        this.displayUpdateDialog = true;
    }

    selectAndAddSubcategory(category: CategoryResponse): void {
        this.selectedCategory       = category;
        this.selectedParentCategory = category;
        this.displayCreateDialog    = true;
        this.displayUpdateDialog    = false;
    }

    onCreateSuccess(): void { this.loadCategories(this.currentPage, this.rows); }
    onUpdateSuccess(): void { this.loadCategories(this.currentPage, this.rows); }

    onDisplayCreateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayCreateDialog = false;
    }
    onDisplayUpdateDialogChange(isVisible: boolean): void {
        if (!isVisible) this.displayUpdateDialog = false;
    }

    // ── Toggle status ──────────────────────────────────────────────────────────

    selectAndToggleStatus(category: CategoryResponse): void {
        if (this.togglingStatusId === category.id) return;
        const action = category.isActive ? 'deactivate' : 'activate';

        this.confirmationService.confirm({
            message: `Are you sure you want to ${action} "${category.name}"?`,
            header: 'Confirm Status Change',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.togglingStatusId = category.id;
                this.categoryService.setStatus(category.id, !category.isActive)
                    .pipe(tap(() => {
                        this.messageService.add({
                            severity: 'success', summary: 'Updated',
                            detail: `"${category.name}" ${action}d`
                        });
                        this.togglingStatusId = null;
                        this.loadCategories(this.currentPage, this.rows);
                    }))
                    .subscribe({
                        error: (err) => {
                            this.togglingStatusId = null;
                            const detail = err.error?.detail ?? err.error?.message ?? `Failed to ${action} category`;
                            this.messageService.add({ severity: 'error', summary: 'Error', detail });
                        }
                    });
            }
        });
    }

    // ── Delete ─────────────────────────────────────────────────────────────────

    selectAndDelete(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete "${category.name}"?`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.categoryService.deleteCategory(category.id)
                    .pipe(tap(() => {
                        this.messageService.add({ severity: 'success', summary: 'Deleted', detail: `"${category.name}" deleted` });
                        this.selectedCategory = null;
                        const isLastItemOnPage = this.categories.length === 1 && this.currentPage > 1;
                        this.loadCategories(isLastItemOnPage ? this.currentPage - 1 : this.currentPage, this.rows);
                    }))
                    .subscribe({
                        error: (err) => {
                            const detail = err.error?.detail ?? err.error?.message ?? 'Failed to delete category';
                            this.messageService.add({ severity: 'error', summary: 'Cannot Delete', detail });
                        }
                    });
            }
        });
    }
}
