import { Component, Output, EventEmitter, ViewChild, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TreeTableModule } from 'primeng/treetable';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TooltipModule } from 'primeng/tooltip';
import { ContextMenuModule } from 'primeng/contextmenu';
import { ContextMenu } from 'primeng/contextmenu';
import { MessageModule } from 'primeng/message';
import { TreeNode } from 'primeng/api';
import { MenuItem } from 'primeng/api';
import { CategoryService, CategoryResponse } from '../../services/category.service';
import { MessageService } from 'primeng/api';
import { I18nService } from '../../../../shared/services/i18n.service';

@Component({
    selector: 'app-categories-list',
    standalone: true,
    imports: [CommonModule, TreeTableModule, TableModule, ButtonModule, TooltipModule, ContextMenuModule, MessageModule],
    providers: [MessageService],
    templateUrl: './categories-list.component.html',
    styleUrls: ['./categories-list.component.css'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class CategoriesListComponent implements OnInit {
    @ViewChild('contextMenu') contextMenu: ContextMenu | undefined;

    treeData: TreeNode[] = [];
    loading = false;
    loadingMore = false;
    selectedRows: TreeNode[] = [];
    hasMore = true;
    totalRecords = 0;
    rows = 20;
    contextMenuItems: MenuItem[] = [];

    @Output() nodeSelect = new EventEmitter<void>();
    @Output() editCategory = new EventEmitter<CategoryResponse>();
    @Output() deleteCategory = new EventEmitter<CategoryResponse>();
    @Output() addSubcategory = new EventEmitter<CategoryResponse>();
    @Output() toggleCategoryStatus = new EventEmitter<CategoryResponse>();
    @Output() itemsLoaded = new EventEmitter<CategoryResponse[]>();

    private selectedContextCategory: CategoryResponse | null = null;

    private currentPage = 1;
    private categories: CategoryResponse[] = [];
    private searchQuery = '';

    private i18n = inject(I18nService);

    constructor(
        private categoryService: CategoryService,
        private messageService: MessageService,
        private cdr: ChangeDetectorRef
    ) {}

    ngOnInit() {
        this.loadCategories();
    }

    /**
     * Load categories with pagination
     */
    loadCategories() {
        this.loading = true;
        this.currentPage = 1;
        this.categories = [];

        this.categoryService.getPagedCategories(this.currentPage, this.rows).subscribe({
            next: (response: any) => {
                this.categories = response.items || response.data || [];
                this.totalRecords = response.totalCount || response.total || this.categories.length;
                this.hasMore = response.hasNextPage !== false && this.currentPage * this.rows < this.totalRecords;
                this.buildTree();
                this.itemsLoaded.emit(this.categories);
                this.loading = false;
                this.cdr.markForCheck();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('categories.messages.loadFailed')
                });
                this.loading = false;
                this.cdr.markForCheck();
                console.error(err);
            }
        });
    }

    /**
     * Search categories
     */
    search(query: string) {
        this.searchQuery = query.trim();
        this.currentPage = 1;
        this.categories = [];

        if (this.searchQuery === '') {
            this.loadCategories();
        } else {
            this.loadSearchResults();
        }
    }

    /**
     * Load search results with pagination
     */
    private loadSearchResults() {
        this.loading = true;
        this.categoryService.getPagedCategories(this.currentPage, this.rows, this.searchQuery).subscribe({
            next: (response: any) => {
                this.categories = response.items || response.data || [];
                this.totalRecords = response.totalCount || response.total || this.categories.length;
                this.hasMore = response.hasNextPage !== false && this.currentPage * this.rows < this.totalRecords;
                this.buildTree();
                this.itemsLoaded.emit(this.categories);
                this.loading = false;
                this.cdr.markForCheck();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('categories.messages.searchFailed')
                });
                this.loading = false;
                this.cdr.markForCheck();
                console.error(err);
            }
        });
    }

    /**
     * Handle lazy loading for pagination
     */
    onLazyLoad(event: any) {
        if (this.loadingMore || this.loading) {
            return;
        }

        this.loadingMore = true;
        const first = event.first || 0;
        const pageNumber = Math.floor(first / event.rows) + 1;
        this.currentPage = pageNumber;

        this.categoryService.getPagedCategories(pageNumber, event.rows, this.searchQuery).subscribe({
            next: (response: any) => {
                const newItems = response.items || response.data || [];
                this.categories = newItems;
                this.totalRecords = response.totalCount || response.total || this.categories.length;
                this.hasMore = response.hasNextPage !== false && pageNumber * event.rows < this.totalRecords;
                this.buildTree();
                this.itemsLoaded.emit(this.categories);
                this.loadingMore = false;
                this.cdr.markForCheck();
            },
            error: (err) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('categories.messages.loadMoreFailed')
                });
                this.loadingMore = false;
                this.cdr.markForCheck();
                console.error(err);
            }
        });
    }

    /**
     * Build tree structure from categories
     */
    private buildTree() {
        const categoryMap = new Map<string, TreeNode>();

        this.categories.forEach((category) => {
            const depthLevel = category.depthLevel || 0;
            const node: TreeNode = {
                key: category.id,
                data: {
                    id: category.id,
                    name: category.name,
                    code: category.code,
                    description: category.description,
                    level: depthLevel,
                    levelDisplay: this.getLevelDisplay(depthLevel),
                    childCount: category.childCount,
                    isActive: category.isActive,
                    displayOrder: category.displayOrder,
                    breadcrumbPath: category.breadcrumbPath,
                    category: category
                },
                children: [],
                expanded: false
            };
            categoryMap.set(category.id, node);
        });

        const rootNodes: TreeNode[] = [];
        this.categories.forEach((category) => {
            const node = categoryMap.get(category.id);
            if (node) {
                if (category.parentCategoryId) {
                    const parentNode = categoryMap.get(category.parentCategoryId);
                    if (parentNode) {
                        parentNode.children?.push(node);
                    } else {
                        rootNodes.push(node);
                    }
                } else {
                    rootNodes.push(node);
                }
            }
        });

        rootNodes.sort((a, b) => (a.data.displayOrder || 0) - (b.data.displayOrder || 0));
        this.treeData = rootNodes;
    }

    onNodeSelect() {
        this.nodeSelect.emit();
    }

    /**
     * Build context menu for a category
     */
    buildContextMenu(category: CategoryResponse) {
        this.selectedContextCategory = category;
        this.contextMenuItems = [
            {
                label: this.i18n.t('common.actions.edit'),
                icon: 'pi pi-pencil',
                command: () => {
                    this.editCategory.emit(category);
                }
            },
            {
                label: this.i18n.t('common.actions.addSubcategory'),
                icon: 'pi pi-plus',
                command: () => {
                    this.addSubcategory.emit(category);
                }
            },
            { separator: true },
            {
                label: category.isActive ? this.i18n.t('common.actions.deactivate') : this.i18n.t('common.actions.activate'),
                icon: category.isActive ? 'pi pi-times' : 'pi pi-check',
                command: () => {
                    this.toggleCategoryStatus.emit(category);
                }
            },
            {
                label: this.i18n.t('common.actions.delete'),
                icon: 'pi pi-trash',
                command: () => {
                    this.deleteCategory.emit(category);
                }
            }
        ];
        this.cdr.markForCheck();
    }

    /**
     * Show context menu
     */
    showContextMenu(event: MouseEvent, category: CategoryResponse) {
        this.buildContextMenu(category);
        if (this.contextMenu) {
            this.contextMenu.show(event);
        }
    }

    private getLevelDisplay(level: number): string {
        return level === 0 ? this.i18n.t('common.labels.root') : this.i18n.t('common.labels.level') + ' ' + level;
    }

    /**
     * Reload the list (called after create/update/delete)
     */
    reload() {
        if (this.searchQuery) {
            this.loadSearchResults();
        } else {
            this.loadCategories();
        }
    }
}
