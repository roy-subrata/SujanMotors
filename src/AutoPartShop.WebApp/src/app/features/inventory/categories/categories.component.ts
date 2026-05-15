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

@Component({
    selector: 'app-categories',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ToastModule,
        ConfirmDialogModule,
        ButtonModule,
        Select,
        CategoriesListComponent,
        CategoriesFormDialogComponent
    ],
    providers: [CategoryService, MessageService, ConfirmationService],
    templateUrl: './categories.component.html',
    styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
    private readonly categoryService = inject(CategoryService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);

    // Data
    categories: CategoryResponse[] = [];
    public selectedParentCategory: CategoryResponse | null = null;
    public selectedCategory: CategoryResponse | null = null;

    // Dialog visibility
    public displayCreateDialog: boolean = false;
    public displayUpdateDialog: boolean = false;

    // Pagination & Loading
    loading = false;
    totalRecords = 0;
    rows = 10;
    currentPage = 1;

    // Filters
    searchTerm = '';
    filterStatus: boolean | null = null;
    sortField = 'name';
    sortDirection: 'asc' | 'desc' = 'asc';

    // Status options for dropdown
    statusOptions = [
        { label: 'All', value: null },
        { label: 'Active', value: true },
        { label: 'Inactive', value: false }
    ];

    ngOnInit(): void {
        this.loadCategories();
    }

    /**
     * Load categories with current filters
     */
    loadCategories(pageNumber: number = 1, pageSize: number = 10): void {
        if (!pageNumber || isNaN(pageNumber) || pageNumber < 1) {
            pageNumber = 1;
        }
        if (!pageSize || isNaN(pageSize) || pageSize < 1) {
            pageSize = 10;
        }

        this.loading = true;
        this.categoryService
            .getPagedCategories({
                search: this.searchTerm,
                pageNumber: pageNumber,
                pageSize: pageSize,
                isActive: this.filterStatus,
                sorts: [{
                    field: this.sortField,
                    direction: this.sortDirection
                }]
            })
            .subscribe({
                next: (response: any) => {
                    // Extract data from response
                    this.categories = response.data || response.items || [];

                    // totalCount is nested in pagination object
                    const pagination = response.pagination || {};
                    this.totalRecords = pagination.totalCount || response.totalCount || this.categories.length;

                    // Update pagination state
                    this.rows = pagination.pageSize || pageSize;
                    this.currentPage = pagination.pageNumber || pageNumber;

                    this.loading = false;
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: 'Failed to load categories'
                    });
                    console.error('Error loading categories:', error);
                    this.loading = false;
                }
            });
    }

    /**
     * Handle search button click - applies all filters
     */
    onSearch(): void {
        this.loadCategories(1, this.rows);
    }

    /**
     * Handle filter changes (status)
     */
    onFilterChange(): void {
        this.loadCategories(1, this.rows);
    }

    clearSearchInput(): void {
        this.searchTerm = '';
        this.loadCategories(1, this.rows);
    }

    /**
     * Clear all filters and reload
     */
    clearFilters(): void {
        this.searchTerm = '';
        this.filterStatus = null;
        this.loadCategories(1, this.rows);
    }

    /**
     * Refresh current page
     */
    refreshData(): void {
        this.loadCategories(this.currentPage, this.rows);
    }

    /**
     * Check if any filters are active
     */
    hasActiveFilters(): boolean {
        return !!this.searchTerm || this.filterStatus !== null;
    }

    /**
     * Status label helper for filter chips
     */
    getStatusLabel(isActive: boolean | null): string {
        if (isActive === true) {
            return 'Active';
        }
        if (isActive === false) {
            return 'Inactive';
        }
        return 'All';
    }

    /**
     * Handle page change from list component
     */
    onPageChange(event: { page: number; rows: number }): void {
        this.loadCategories(event.page, event.rows);
    }

    /**
     * Trigger create dialog
     */
    onNewCategoryClick() {
        this.selectedParentCategory = null;
        this.displayCreateDialog = true;
        this.displayUpdateDialog = false;
    }

    /**
     * Alias for header action button
     */
    createCategory(): void {
        this.onNewCategoryClick();
    }

    /**
     * Handle create success
     */
    onCreateSuccess() {
        this.loadCategories(this.currentPage, this.rows);
    }

    /**
     * Handle update success
     */
    onUpdateSuccess() {
        this.loadCategories(this.currentPage, this.rows);
    }

    /**
     * Handle edit category
     */
    selectAndOpenUpdate(category: CategoryResponse) {
        this.selectedCategory = category;
        this.displayUpdateDialog = true;
    }

    /**
     * Handle delete category
     */
    selectAndDelete(category: CategoryResponse) {
        this.selectedCategory = category;
        this.confirmationService.confirm({
            message: `Are you sure you want to delete "${category.name}"?`,
            header: 'Confirm Delete',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.categoryService.deleteCategory(category.id)
                    .pipe(
                        tap(() => {
                            this.messageService.add({
                                severity: 'success',
                                summary: 'Success',
                                detail: 'Category deleted successfully'
                            });
                            this.selectedCategory = null;
                            this.loadCategories(this.currentPage, this.rows);
                        })
                    )
                    .subscribe({
                        error: (err) => {
                            const msg = err?.error?.message || 'Failed to delete category';
                            this.messageService.add({ severity: 'error', summary: 'Cannot Delete', detail: msg });
                        }
                    });
            }
        });
    }

    /**
     * Handle toggle status
     */
    selectAndToggleStatus(category: CategoryResponse) {
        this.selectedCategory = category;
        const action = category.isActive ? 'deactivate' : 'activate';
        this.confirmationService.confirm({
            message: `Are you sure you want to ${action} "${category.name}"?`,
            header: 'Confirm Status Change',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                const request = category.isActive
                    ? this.categoryService.deactivateCategory(category.id)
                    : this.categoryService.activateCategory(category.id);

                request.pipe(
                    tap(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Category ${action}d successfully`
                        });
                        this.loadCategories(this.currentPage, this.rows);
                    })
                ).subscribe({
                    error: (err) => {
                        console.error(`Failed to ${action} category`, err);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: `Failed to ${action} category`
                        });
                    }
                });
            }
        });
    }

    /**
     * Handle add subcategory
     */
    selectAndAddSubcategory(category: CategoryResponse) {
        this.selectedCategory = category;
        this.selectedParentCategory = category;
        this.displayCreateDialog = true;
        this.displayUpdateDialog = false;
    }

    /**
     * Handle create dialog visibility change
     */
    onDisplayCreateDialogChange(isVisible: boolean) {
        if (!isVisible) {
            this.displayCreateDialog = false;
        }
    }

    /**
     * Handle update dialog visibility change
     */
    onDisplayUpdateDialogChange(isVisible: boolean) {
        if (!isVisible) {
            this.displayUpdateDialog = false;
        }
    }
}
