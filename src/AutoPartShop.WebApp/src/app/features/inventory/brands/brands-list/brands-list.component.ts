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
import { BrandResponse } from '../../services/brand.service';

@Component({
    selector: 'app-brands-list',
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
    templateUrl: './brands-list.component.html',
    styleUrls: ['./brands-list.component.css']
})
export class BrandsListComponent {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    // Input data from parent
    @Input() brands: BrandResponse[] = [];
    @Input() loading = false;
    @Input() totalRecords = 0;
    @Input() rows = 10;
    @Input() currentPage = 1;

    // Output events
    @Output() editBrand = new EventEmitter<BrandResponse>();
    @Output() deleteBrand = new EventEmitter<BrandResponse>();
    @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

    // Context menu
    contextMenuItems: MenuItem[] = [];
    selectedBrand: BrandResponse | null = null;

    // Expose Math for template
    Math = Math;

    /**
     * Build context menu for a brand
     */
    private buildContextMenu(brand: BrandResponse): void {
        this.selectedBrand = brand;
        this.contextMenuItems = [
            {
                label: 'Edit',
                icon: 'pi pi-pencil',
                command: () => this.editBrand.emit(brand)
            },
            { separator: true },
            {
                label: 'Delete',
                icon: 'pi pi-trash',
                command: () => this.deleteBrand.emit(brand),
                styleClass: 'p-menuitem-danger'
            }
        ];
    }

    /**
     * Show context menu
     */
    showContextMenu(event: MouseEvent, brand: BrandResponse): void {
        event.preventDefault();
        event.stopPropagation();
        this.buildContextMenu(brand);
        this.contextMenu?.show(event);
    }

    /**
     * Get status label
     */
    getStatusLabel(isActive: boolean): string {
        return isActive ? 'Active' : 'Inactive';
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
