import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
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
        CardModule,
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
    public filteredParentCategories: CategoryResponse[] = [];

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

    // Status options for dropdown
    statusOptions = [
        { label: 'All', value: null },
        { label: 'Active', value: true },
        { label: 'Inactive', value: false }
    ];

    // Parent category autocomplete
    parentCategorySearchQuery: string = '';
    parentCategoryOptions: CategoryResponse[] = [];
    private readonly PARENT_CATEGORY_PAGE_SIZE = 20;
    private isLoadingMoreParentCategories = false;

    ngOnInit(): void {
        this.loadCategories();
        this.loadAllParentCategories();
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
                isActive: this.filterStatus
            })
            .subscribe({
                next: (response: any) => {
                    this.categories = response.items || response.data || [];
                    this.totalRecords = response.totalCount || response.total || this.categories.length;
                    this.rows = pageSize;
                    this.currentPage = pageNumber;
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

    /**
     * Clear search input only (does not trigger search)
     */
    clearSearchInput(): void {
        this.searchTerm = '';
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
        this.filteredParentCategories = [];
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
                            console.error('Failed to delete category', err);
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Error',
                                detail: 'Failed to delete category'
                            });
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
        this.filteredParentCategories = [category];
    }

    /**
     * Helper method to create root category
     */
    private createRootCategory(): CategoryResponse {
        return {
            id: null as any,
            name: 'Root (No Parent)',
            code: 'ROOT',
            description: 'Top level category with no parent',
            parentCategoryId: null,
            displayOrder: 0,
            isActive: true,
            depthLevel: 0,
            childCount: 0,
            breadcrumbPath: 'Root',
            createdBy: '',
            modifiedBy: '',
            subCategories: []
        };
    }

    private addRootToCategories(categories: CategoryResponse[]): CategoryResponse[] {
        const root = this.createRootCategory();
        return [root, ...categories];
    }

    private loadAllParentCategories() {
        this.categoryService.getAllCategories().subscribe({
            next: (data) => {
                this.parentCategoryOptions = data;
                this.filteredParentCategories = [];
            },
            error: (err) => {
                console.error('Failed to load parent categories', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load parent categories'
                });
            }
        });
    }

    /**
     * Handle parent category autocomplete events
     */
    onParentCategoryEvent(event: any) {
        if ('query' in event) {
            const query = event.query || '';
            const lowerQuery = query.toLowerCase();
            this.parentCategorySearchQuery = lowerQuery;
            this.parentCategoryOptions = [];
            this.filteredParentCategories = [];
            this.loadParentCategories(0, this.PARENT_CATEGORY_PAGE_SIZE);
        } else {
            this.loadParentCategories(event.first || 0, event.rows || this.PARENT_CATEGORY_PAGE_SIZE);
        }
    }

    /**
     * Load parent categories with pagination
     */
    private loadParentCategories(first: number, rows: number) {
        if (this.isLoadingMoreParentCategories) {
            return;
        }

        const pageNumber = Math.floor(first / rows) + 1;
        const expectedItemsCount = pageNumber * rows;
        if (this.parentCategoryOptions.length >= expectedItemsCount) {
            return;
        }

        this.isLoadingMoreParentCategories = true;
        const searchQuery = this.parentCategorySearchQuery;

        this.categoryService.getPagedCategories({
            search: searchQuery,
            pageNumber: pageNumber,
            pageSize: rows
        }).subscribe({
            next: (response: any) => {
                const newItems = response.items || response.data || [];

                if (pageNumber === 1) {
                    this.parentCategoryOptions = this.addRootToCategories(newItems);
                } else {
                    this.parentCategoryOptions = [...this.parentCategoryOptions, ...newItems];
                }

                this.filteredParentCategories = this.parentCategoryOptions;
                this.isLoadingMoreParentCategories = false;
            },
            error: (err) => {
                console.error('Failed to load parent categories for autocomplete', err);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load parent categories'
                });
                this.isLoadingMoreParentCategories = false;
            }
        });
    }

    /**
     * Handle parent category selection
     */
    onParentCategorySelect(category: CategoryResponse) {
        this.selectedParentCategory = category;
    }

    /**
     * Handle parent category cleared
     */
    onParentCategoryCleared() {
        this.selectedParentCategory = null;
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
