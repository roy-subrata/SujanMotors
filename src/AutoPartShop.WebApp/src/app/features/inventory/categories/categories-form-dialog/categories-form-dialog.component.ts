import { Component, Output, EventEmitter, signal, inject, Input, OnChanges, SimpleChanges, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { InputNumberModule } from 'primeng/inputnumber';
import { CategoryResponse, CategoryService, CreateCategoryRequest, UpdateCategoryRequest } from '../../services/category.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete/lazy-autocomplete.component';
import { Observable } from 'rxjs';

@Component({
    selector: 'app-categories-form-dialog',
    standalone: true,
    imports: [
        CommonModule, FormsModule, ReactiveFormsModule,
        DialogModule, ButtonModule, InputTextModule, TextareaModule,
        CheckboxModule, InputNumberModule, ToastModule, LazyAutocompleteComponent
    ],
    templateUrl: './categories-form-dialog.component.html',
    styleUrls: ['./categories-form-dialog.component.css'],
    providers: [MessageService]
})
export class CategoriesFormDialogComponent implements OnChanges {
    @Input() displayCreateDialog = false;
    @Input() displayUpdateDialog = false;
    @Input() selectedParentCategory: CategoryResponse | null = null;
    @Input() selectedCategory: CategoryResponse | null = null;

    @Output() displayCreateDialogChange = new EventEmitter<boolean>();
    @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
    @Output() createSuccess = new EventEmitter<void>();
    @Output() updateSuccess = new EventEmitter<void>();

    private readonly fb = inject(FormBuilder);
    private readonly categoryService = inject(CategoryService);
    private readonly messageService = inject(MessageService);

    isCreating = signal(false);
    isUpdating = signal(false);

    // ── Create form ───────────────────────────────────────────────────────────

    createForm = this.fb.group({
        name:             ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description:      [''],
        displayOrder:     [0, [Validators.required, Validators.min(0)]],
        isActive:         [true],
        parentCategoryId: [null as string | null],
        parentCategory:   [null as CategoryResponse | null]
    });

    // ── Update form ───────────────────────────────────────────────────────────

    updateForm = this.fb.group({
        name:         ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description:  [''],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive:     [true]
    });

    constructor() {
        effect(() => {
            const parentCategory = this.selectedParentCategory;
            if (parentCategory && this.displayCreateDialog) {
                this.createForm.patchValue({
                    parentCategory:   parentCategory,
                    parentCategoryId: parentCategory.id ?? null
                });
            }
        });
    }

    ngOnChanges(changes: SimpleChanges): void {
        if (changes['selectedCategory'] && this.selectedCategory) {
            this.updateForm.patchValue({
                name:         this.selectedCategory.name,
                description:  this.selectedCategory.description ?? '',
                displayOrder: this.selectedCategory.displayOrder,
                isActive:     this.selectedCategory.isActive
            });
        }
    }

    // ── Fetch parent categories for lazy autocomplete ─────────────────────────

    private createRootOption(): CategoryResponse {
        return {
            id: null as any,
            name: 'None (Root Category)',
            description: 'Create as top-level category with no parent',
            parentCategoryId: null,
            displayOrder: 0,
            isActive: true,
            depthLevel: 0,
            childCount: 0,
            breadcrumbPath: 'Root',
            createdBy: null,
            modifiedBy: null,
            subCategories: []
        };
    }

    fetchParentCategories(req: LazyRequest): Observable<LazyResponse<CategoryResponse>> {
        return new Observable<LazyResponse<CategoryResponse>>(observer => {
            this.categoryService.getCategories({
                search: req.search,
                page: req.pageNumber,
                pageSize: req.pageSize
            }).subscribe({
                next: (response) => {
                    const addRoot = req.pageNumber === 1 && !req.search;
                    const items = addRoot ? [this.createRootOption(), ...response.data] : response.data;
                    observer.next({
                        items,
                        totalCount: addRoot ? response.pagination.totalCount + 1 : response.pagination.totalCount
                    });
                    observer.complete();
                },
                error: (err) => observer.error(err)
            });
        });
    }

    // ── Create dialog hooks ───────────────────────────────────────────────────

    onCreateDialogShow(): void {
        if (!this.selectedParentCategory) {
            this.createForm.reset({ displayOrder: 0, isActive: true, parentCategoryId: null, parentCategory: null });
        } else {
            this.createForm.reset({
                displayOrder: 0, isActive: true,
                parentCategoryId: this.selectedParentCategory.id ?? null,
                parentCategory: this.selectedParentCategory
            });
        }
    }

    onCreateDialogHide(): void {
        this.displayCreateDialogChange.emit(false);
        this.createForm.reset({ displayOrder: 0, isActive: true, parentCategoryId: null, parentCategory: null });
        this.onParentCategoryCleared();
    }

    // ── Update dialog hooks ───────────────────────────────────────────────────

    onUpdateDialogShow(): void { /* populated via ngOnChanges */ }

    onUpdateDialogHide(): void {
        this.displayUpdateDialogChange.emit(false);
        this.updateForm.reset();
    }

    // ── Submit handlers ───────────────────────────────────────────────────────

    onCreateSubmit(): void {
        if (!this.createForm.valid) {
            this.createForm.markAllAsTouched();
            return;
        }

        this.isCreating.set(true);
        const v = this.createForm.getRawValue();

        const request: CreateCategoryRequest = {
            name:             v.name!,
            description:      v.description || null,
            displayOrder:     v.displayOrder ?? 0,
            parentCategoryId: this.selectedParentCategory?.id ?? null
        };

        this.categoryService.createCategory(request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Created', detail: `Category "${request.name}" created` });
                this.isCreating.set(false);
                this.onCreateDialogHide();
                this.createSuccess.emit();
            },
            error: (err) => {
                const detail = err.error?.detail ?? err.error?.message ?? 'Failed to create category';
                const isConflict = err.status === 409;
                this.messageService.add({
                    severity: 'error',
                    summary: isConflict ? 'Conflict' : 'Error',
                    detail
                });
                this.isCreating.set(false);
            }
        });
    }

    onUpdateSubmit(): void {
        if (!this.updateForm.valid || !this.selectedCategory) {
            this.updateForm.markAllAsTouched();
            return;
        }

        this.isUpdating.set(true);
        const v = this.updateForm.value;

        const request: UpdateCategoryRequest = {
            name:        v.name!,
            description: v.description || null,
            displayOrder: v.displayOrder ?? 0,
            isActive:    v.isActive ?? true
        };

        this.categoryService.updateCategory(this.selectedCategory.id, request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Updated', detail: `Category "${request.name}" updated` });
                this.isUpdating.set(false);
                this.onUpdateDialogHide();
                this.updateSuccess.emit();
            },
            error: (err) => {
                const detail = err.error?.detail ?? err.error?.message ?? 'Failed to update category';
                const isConflict = err.status === 409;
                this.messageService.add({
                    severity: 'error',
                    summary: isConflict ? 'Conflict' : 'Error',
                    detail
                });
                this.isUpdating.set(false);
            }
        });
    }

    // ── Parent category autocomplete handlers ─────────────────────────────────

    onParentCategorySelect(category: CategoryResponse): void {
        const isRoot = !category?.id;
        if (isRoot) {
            this.selectedParentCategory = null;
            this.createForm.patchValue({ parentCategory: null, parentCategoryId: null });
        } else {
            this.selectedParentCategory = category;
            this.createForm.patchValue({ parentCategory: category, parentCategoryId: category.id });
        }
    }

    onParentCategoryCleared(): void {
        this.selectedParentCategory = null;
        this.createForm.patchValue({ parentCategory: null, parentCategoryId: null });
    }
}
