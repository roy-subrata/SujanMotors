import { Component, Output, EventEmitter, ViewChild, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { ContextMenuModule, ContextMenu } from 'primeng/contextmenu';
import { RippleModule } from 'primeng/ripple';
import { Select } from 'primeng/select';
import { MenuItem } from 'primeng/api';
import { CategoryResponse } from '../../services/category.service';

@Component({
    selector: 'app-categories-list',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        TableModule,
        ButtonModule,
        TooltipModule,
        TagModule,
        ContextMenuModule,
        RippleModule,
        Select
    ],
    templateUrl: './categories-list.component.html',
    styleUrls: ['./categories-list.component.css']
})
export class CategoriesListComponent {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    // Input data from parent
    @Input() categories: CategoryResponse[] = [];
    @Input() loading = false;
    @Input() totalRecords = 0;
    @Input() rows = 10;
    @Input() currentPage = 1;

    // Output events
    @Output() editCategory = new EventEmitter<CategoryResponse>();
    @Output() deleteCategory = new EventEmitter<CategoryResponse>();
    @Output() addSubcategory = new EventEmitter<CategoryResponse>();
    @Output() toggleCategoryStatus = new EventEmitter<CategoryResponse>();
    @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

    // Context menu
    contextMenuItems: MenuItem[] = [];
    selectedCategory: CategoryResponse | null = null;
    pageSizeOptions = [10, 20, 50];

    // Expose Math for template
    Math = Math;

    /**
     * Build context menu for a category
     */
    private buildContextMenu(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.contextMenuItems = [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => this.editCategory.emit(category)
            },
            {
                label: 'Add Subcategory',
                icon: 'pi pi-plus',
                command: () => this.addSubcategory.emit(category)
            },
            { separator: true },
            {
                label: category.isActive ? 'Deactivate' : 'Activate',
                icon: category.isActive ? 'pi pi-times' : 'pi pi-check',
                command: () => this.toggleCategoryStatus.emit(category)
            },
            { separator: true },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => this.deleteCategory.emit(category),
                styleClass: 'p-menuitem-danger'
            }
        ];
    }

    /**
     * Show context menu
     */
    showContextMenu(event: MouseEvent, category: CategoryResponse): void {
        event.preventDefault();
        event.stopPropagation();
        this.buildContextMenu(category);
        this.contextMenu?.show(event);
    }

    /**
     * Handle pagination change
     */
    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') {
            return;
        }
        const pageNumber = Math.floor(event.first / event.rows) + 1;
        this.pageChange.emit({
            page: pageNumber,
            rows: event.rows
        });
    }

    /**
     * Navigate to a specific page
     */
    goToPage(page: number): void {
        if (page < 1 || page > this.totalPages) {
            return;
        }
        this.pageChange.emit({
            page: page,
            rows: this.rows
        });
    }

    /**
     * Handle page size change
     */
    onPageSizeChange(newRows: number): void {
        this.pageChange.emit({
            page: 1,
            rows: newRows
        });
    }

    /**
     * Get status label
     */
    getStatusLabel(isActive: boolean): string {
        return isActive ? 'Active' : 'Inactive';
    }

    /**
     * Get level display
     */
    getLevelDisplay(level: number): string {
        return level === 0 ? 'Root' : `Level ${level}`;
    }

    get first(): number {
        return Math.max(0, (this.currentPage - 1) * this.rows);
    }

    get totalPages(): number {
        if (!this.totalRecords || !this.rows) {
            return 0;
        }
        return Math.ceil(this.totalRecords / this.rows);
    }
}
