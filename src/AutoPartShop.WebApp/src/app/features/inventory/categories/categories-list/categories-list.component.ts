import { Component, Output, EventEmitter, ViewChild, Input, OnInit, inject, DestroyRef } from '@angular/core';
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
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { I18nService } from '@/shared/services/i18n.service';

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
export class CategoriesListComponent implements OnInit {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    @Input() categories: CategoryResponse[] = [];
    @Input() loading = false;
    @Input() totalRecords = 0;
    @Input() rows = 10;
    @Input() currentPage = 1;

    @Output() editCategory = new EventEmitter<CategoryResponse>();
    @Output() deleteCategory = new EventEmitter<CategoryResponse>();
    @Output() addSubcategory = new EventEmitter<CategoryResponse>();
    @Output() toggleCategoryStatus = new EventEmitter<CategoryResponse>();
    @Output() pageChange = new EventEmitter<{ page: number; rows: number }>();

    contextMenuItems: MenuItem[] = [];
    selectedCategory: CategoryResponse | null = null;
    pageSizeOptions = [10, 20, 50];

    Math = Math;

    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    ngOnInit(): void {
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            if (this.selectedCategory) this.buildContextMenu(this.selectedCategory);
        });
    }

    private buildContextMenu(category: CategoryResponse): void {
        this.selectedCategory = category;
        this.contextMenuItems = [
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => this.editCategory.emit(category)
            },
            {
                label: this.i18n.t('common.actions.addSubcategory'),
                icon: 'pi pi-plus',
                command: () => this.addSubcategory.emit(category)
            },
            { separator: true },
            {
                label: category.isActive ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate'),
                icon: category.isActive ? 'pi pi-times' : 'pi pi-check',
                command: () => this.toggleCategoryStatus.emit(category)
            },
            { separator: true },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => this.deleteCategory.emit(category),
                styleClass: 'p-menuitem-danger'
            }
        ];
    }

    showContextMenu(event: MouseEvent, category: CategoryResponse): void {
        event.preventDefault();
        event.stopPropagation();
        this.buildContextMenu(category);
        this.contextMenu?.show(event);
    }

    onPageChange(event: any): void {
        if (!event || typeof event.first !== 'number' || typeof event.rows !== 'number') return;
        const pageNumber = Math.floor(event.first / event.rows) + 1;
        this.pageChange.emit({ page: pageNumber, rows: event.rows });
    }

    goToPage(page: number): void {
        if (page < 1 || page > this.totalPages) return;
        this.pageChange.emit({ page, rows: this.rows });
    }

    onPageSizeChange(newRows: number): void {
        this.pageChange.emit({ page: 1, rows: newRows });
    }

    getStatusLabel(isActive: boolean): string {
        return isActive ? this.i18n.t('common.status.active') : this.i18n.t('common.status.inactive');
    }

    getLevelDisplay(level: number): string {
        return level === 0 ? this.i18n.t('common.labels.root') : `${this.i18n.t('common.labels.level')} ${level}`;
    }

    get first(): number {
        return Math.max(0, (this.currentPage - 1) * this.rows);
    }

    get totalPages(): number {
        if (!this.totalRecords || !this.rows) return 0;
        return Math.ceil(this.totalRecords / this.rows);
    }
}
