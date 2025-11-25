import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TreeTableModule } from 'primeng/treetable';
import { TableModule } from 'primeng/table';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TreeNode } from 'primeng/api';
import { CategoryService } from '../../../../core/services';
import { Category } from '../../../../core/models';

@Component({
  selector: 'app-categories',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    DialogModule,
    ToastModule,
    ConfirmDialogModule,
    TooltipModule,
    TreeTableModule,
    TableModule,
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './categories.component.html',
  styleUrl: './categories.component.scss',
})
export class CategoriesComponent implements OnInit {
  private categoryService = inject(CategoryService);
  private messageService = inject(MessageService);
  private confirmationService = inject(ConfirmationService);

  categories: Category[] = [];
  treeTableNodes: TreeNode[] = [];
  displayDialog = false;
  isEditMode = false;
  selectedCategory: Category | null = null;
  loading = false;
  searchTerm = '';

  categoryForm = {
    code: '',
    name: '',
    description: '',
    parentId: null as string | null,
  };

  ngOnInit() {
    this.loadCategories();
  }

  loadCategories() {
    this.loading = true;
    this.categoryService.getAll().subscribe({
      next: (data) => {
        this.categories = data;
        // Filter only top-level categories and build tree from API's nested structure
        const parentCategories = data.filter((c) => !c.parentCategoryId);
        this.treeTableNodes = parentCategories.map((parent) => this.buildTreeNodeFromAPI(parent));
        this.loading = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load categories',
        });
        this.loading = false;
      },
    });
  }

  private buildTreeNodeFromAPI(category: Category): TreeNode {
    const hasChildren = category.SubCategories && category.SubCategories.length > 0;

    return {
      data: category,
      key: category.id,
      label: category.name,
      expandedIcon: 'pi pi-chevron-down',
      collapsedIcon: 'pi pi-chevron-right',
      leaf: !hasChildren,
      children: hasChildren ? category.SubCategories.map((child) => this.buildTreeNodeFromAPI(child)) : undefined,
    };
  }

  get activeCategoryCount(): number {
    return this.categories.filter((c) => c.isActive).length;
  }

  openCreateDialog() {
    this.isEditMode = false;
    this.resetForm();
    this.displayDialog = true;
  }

  openEditDialog(category: Category) {
    this.isEditMode = true;
    this.selectedCategory = category;
    this.categoryForm = {
      code: category.code,
      name: category.name,
      description: category.description,
      parentId: category.parentCategoryId,
    };
    this.displayDialog = true;
  }

  saveCategory() {
    if (!this.categoryForm.name.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Validation',
        detail: 'Category name is required',
      });
      return;
    }

    if (this.isEditMode && this.selectedCategory) {
      this.categoryService
        .update(this.selectedCategory.id, this.categoryForm)
        .subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Category updated successfully',
            });
            this.displayDialog = false;
            this.loadCategories();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to update category',
            });
          },
        });
    } else {
      this.categoryService.create(this.categoryForm).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Category created successfully',
          });
          this.displayDialog = false;
          this.loadCategories();
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to create category',
          });
        },
      });
    }
  }

  deleteCategory(category: Category) {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete "${category.name}"?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.categoryService.delete(category.id).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: 'Category deleted successfully',
            });
            this.loadCategories();
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: 'Failed to delete category',
            });
          },
        });
      },
    });
  }

  toggleCategoryStatus(category: Category) {
    const action = category.isActive
      ? this.categoryService.deactivate(category.id)
      : this.categoryService.activate(category.id);

    action.subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Category ${category.isActive ? 'deactivated' : 'activated'} successfully`,
        });
        this.loadCategories();
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to update category status',
        });
      },
    });
  }

  private resetForm() {
    this.categoryForm = {
      code: '',
      name: '',
      description: '',
      parentId: null,
    };
  }

  onSearchChanged(value: string) {
    this.searchTerm = value;
    if (this.searchTerm) {
      this.filterCategories();
    } else {
      this.loadCategories();
    }
  }

  clearSearch() {
    this.searchTerm = '';
    this.loadCategories();
  }

  private filterCategories() {
    const searchLower = this.searchTerm.toLowerCase();
    const filtered = this.categories.filter((c) => !c.parentCategoryId);
    this.treeTableNodes = filtered
      .map((parent) => this.buildFilteredTreeNode(parent, searchLower))
      .filter((node) => node !== null) as TreeNode[];
  }

  private buildFilteredTreeNode(category: Category, searchLower: string): TreeNode | null {
    const categoryMatches =
      category.name.toLowerCase().includes(searchLower) ||
      (category.description && category.description.toLowerCase().includes(searchLower));

    const hasSubcategories = category.SubCategories && category.SubCategories.length > 0;

    let children: TreeNode[] | undefined;
    if (hasSubcategories) {
      const filteredChildren = category.SubCategories
        .map((child) => this.buildFilteredTreeNode(child, searchLower))
        .filter((node) => node !== null) as TreeNode[];
      children = filteredChildren.length > 0 ? filteredChildren : undefined;
    }

    // Include category if it matches or has matching children
    if (categoryMatches || children) {
      return {
        data: category,
        key: category.id,
        label: category.name,
        expandedIcon: 'pi pi-chevron-down',
        collapsedIcon: 'pi pi-chevron-right',
        leaf: !children || children.length === 0,
        children: children,
      };
    }

    return null;
  }
}
