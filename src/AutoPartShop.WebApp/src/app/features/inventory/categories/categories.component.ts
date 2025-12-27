import { Component, inject, ViewChild, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { CategoryResponse, CategoryService } from '../services/category.service';
import { CategoriesHeaderComponent } from './categories-header/categories-header.component';
import { CategoriesListComponent } from './categories-list/categories-list.component';
import { CategoriesFormDialogComponent } from './categories-form-dialog/categories-form-dialog.component';
import { tap } from 'rxjs';

@Component({
    selector: 'app-categories',
    standalone: true,
    imports: [CommonModule, ToastModule, ConfirmDialogModule, CategoriesHeaderComponent, CategoriesListComponent, CategoriesFormDialogComponent],
    providers: [CategoryService, MessageService, ConfirmationService],
    templateUrl: './categories.component.html',
    styleUrls: ['./categories.component.css']
})
export class CategoriesComponent implements OnInit {
    @ViewChild(CategoriesListComponent) listComponent!: CategoriesListComponent;
    @ViewChild(CategoriesFormDialogComponent) formDialogComponent!: CategoriesFormDialogComponent;

    private readonly categoryService = inject(CategoryService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly messageService = inject(MessageService);

    public selectedParentCategory: CategoryResponse | null = null;
    public selectedCategory: CategoryResponse | null = null;
    public filteredParentCategories: CategoryResponse[] = [];
    public displayCreateDialog: boolean = false;
    public displayUpdateDialog: boolean = false;

    ngOnInit(): void {
        this.loadAllParentCategories();
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
     * Handle search
     */
    onSearch(query: string) {
        this.listComponent.search(query);
    }

    /**
     * Handle list node selection
     */
    onNodeSelect() {
        if (this.listComponent && this.listComponent.selectedRows && this.listComponent.selectedRows.length > 0) {
            const node = this.listComponent.selectedRows[0];
            const category = node.data?.category as CategoryResponse;
            this.selectedCategory = category;
        }
    }

    /**
     * Handle create success
     */
    onCreateSuccess() {
        this.listComponent.reload();
    }

    /**
     * Handle update success
     */
    onUpdateSuccess() {
        this.listComponent.reload();
    }

    /**
     * Handle edit category
     */
    selectAndOpenUpdate(category: CategoryResponse) {
        this.selectedCategory = category;
        this.formDialogComponent.updateForm.patchValue({
            id: category.id,
            name: category.name,
            description: category.description,
            displayOrder: category.displayOrder,
            isActive: category.isActive
        });
        if (!this.selectedCategory) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Warning',
                detail: 'Please select a category first'
            });
        }
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
                        this.listComponent.reload();
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
                const action = category.isActive ? 'deactivate' : 'activate';
                const request = category.isActive ? this.categoryService.deactivateCategory(category.id) : this.categoryService.activateCategory(category.id);

                request.pipe(
                    tap(() => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Category ${action}d successfully`
                        });
                        this.listComponent.reload();
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
        // Ensure the selected parent is in the filtered list so autocomplete can display it
        this.filteredParentCategories = [category];
    }

    parentCategorySearchQuery: string = '';
    parentCategoryOptions: CategoryResponse[] = [];

    // Lazy loading variables
    private readonly PARENT_CATEGORY_PAGE_SIZE = 20;
    private isLoadingMoreParentCategories = false;

    /**
     * Handle parent category autocomplete events (search and lazy load)
     */
    onParentCategoryEvent(event: any) {
        // Check if it's a search/filter event (has 'query' property from completeMethod)
        if ('query' in event) {
            const query = event.query || '';
            const lowerQuery = query.toLowerCase();
            this.parentCategorySearchQuery = lowerQuery;
            this.parentCategoryOptions = [];
            this.filteredParentCategories = [];

            // Reset and load first page with search query
            this.loadParentCategories(0, this.PARENT_CATEGORY_PAGE_SIZE);
        } else {
            // It's a lazy load event (has 'first' and 'rows' properties from onLazyLoad)
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

        // Only load if we haven't loaded this page yet
        const expectedItemsCount = pageNumber * rows;
        if (this.parentCategoryOptions.length >= expectedItemsCount) {
            return;
        }

        this.isLoadingMoreParentCategories = true;
        const searchQuery = this.parentCategorySearchQuery;

        this.categoryService.getPagedCategories(pageNumber, rows, searchQuery).subscribe({
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
