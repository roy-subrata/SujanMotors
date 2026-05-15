import { Component, Output, EventEmitter, signal, inject, Input, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { CategoryResponse, CategoryService, CreateCategoryRequest, UpdateCategoryRequest } from '../../services/category.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete/lazy-autocomplete.component';
import { Observable } from 'rxjs';


@Component({
    selector: 'app-categories-form-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, ToastModule, LazyAutocompleteComponent],
    templateUrl: './categories-form-dialog.component.html',
    styleUrls: ['./categories-form-dialog.component.css'],
    providers: [MessageService]
})
export class CategoriesFormDialogComponent {
    @Input() displayCreateDialog: boolean = false;
    @Input() displayUpdateDialog: boolean = false;
    @Input() selectedParentCategory: CategoryResponse | null = null;
    @Input() selectedCategory: CategoryResponse | null = null;

    @Output() displayCreateDialogChange = new EventEmitter<boolean>();
    @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
    @Output() createSuccess = new EventEmitter<void>();
    @Output() updateSuccess = new EventEmitter<void>();

    private readonly fb = inject(FormBuilder);
    private readonly categoryService = inject(CategoryService);
    private readonly messageService = inject(MessageService);
    private readonly codeGenerationService = inject(CodeGenerationService);

    generatingCode = false;

    updateForm = this.fb.group({
        id: [''],
        name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description: [''],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive: [true]
    });

    createForm = this.fb.group({
        name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description: [''],
        code: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(1), Validators.maxLength(20)]],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive: [true],
        parentCategoryId: [''],
        parentCategory: [null as CategoryResponse | null]
    });

    isCreating = signal(false);
    isUpdating = signal(false);

    /**
     * Create a root category option for the autocomplete
     */
    private createRootCategoryOption(): CategoryResponse {
        return {
            id: null as any,
            name: 'None (Root Category)',
            code: 'ROOT',
            description: 'Create as top-level category with no parent',
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

    /**
     * Fetch parent categories for lazy autocomplete
     */
    fetchParentCategories(req: LazyRequest): Observable<LazyResponse<CategoryResponse>> {
        return new Observable<LazyResponse<CategoryResponse>>(observer => {
            this.categoryService.getPagedCategories({
                search: req.search,
                pageNumber: req.pageNumber,
                pageSize: req.pageSize
            }).subscribe({
                next: (response: any) => {
                    const items = response.items || response.data || [];
                    const realTotal = response.pagination?.totalCount || response.totalCount || response.total || 0;

                    // Prepend "None (Root)" only on page 1 with no active search
                    const addRoot = req.pageNumber === 1 && !req.search;
                    const itemsWithRoot = addRoot ? [this.createRootCategoryOption(), ...items] : items;

                    observer.next({
                        items: itemsWithRoot,
                        totalCount: addRoot ? realTotal + 1 : realTotal
                    });
                    observer.complete();
                },
                error: (err) => {
                    observer.error(err);
                }
            });
        });
    }

    constructor() {
        effect(() => {
            const parentCategory = this.selectedParentCategory;
            if (parentCategory && this.displayCreateDialog) {
                this.createForm.patchValue({
                    parentCategory: parentCategory,
                    parentCategoryId: parentCategory.id || ''
                });
            }
        });
    }

    /**
     * Called when the update dialog is shown - populate form with selected category data
     */
    onUpdateDialogShow() {
        if (this.selectedCategory) {
            this.updateForm.patchValue({
                id: this.selectedCategory.id,
                name: this.selectedCategory.name,
                description: this.selectedCategory.description,
                displayOrder: this.selectedCategory.displayOrder,
                isActive: this.selectedCategory.isActive
            });
        }
    }

    onCreateDialogShow() {
        // Only reset if there's no selected parent category (i.e., creating a root category)
        if (!this.selectedParentCategory) {
            this.createForm.reset({ displayOrder: 0, isActive: true, parentCategoryId: '', parentCategory: null });
        } else {
            // Reset but keep parent category
            this.createForm.reset({
                displayOrder: 0,
                isActive: true,
                parentCategoryId: this.selectedParentCategory.id || '',
                parentCategory: this.selectedParentCategory
            });
        }
        this.generateCategoryCode();
    }

    onCreateDialogHide() {
        this.displayCreateDialogChange.emit(false);
        this.createForm.reset({ displayOrder: 0, isActive: true, parentCategoryId: '', parentCategory: null });
        this.onParentCategoryCleared();
    }

    generateCategoryCode() {
        this.generatingCode = true;
        this.codeGenerationService.generateCategoryCode().subscribe({
            next: (code) => {
                this.createForm.patchValue({ code });
                this.generatingCode = false;
            },
            error: (err) => {
                console.error('Failed to generate category code:', err);
                this.messageService.add({ severity: 'warn', summary: 'Warning', detail: 'Could not auto-generate code. Please enter manually.' });
                this.generatingCode = false;
            }
        });
    }

    onUpdateDialogHide() {
        this.displayUpdateDialogChange.emit(false);
        this.updateForm.reset();
    }

    onCreateSubmit() {
        if (!this.createForm.valid) {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Please fill all required fields' });
            return;
        }

        this.isCreating.set(true);
        // Use getRawValue() to include disabled fields (like 'code')
        const formValue = this.createForm.getRawValue();
        const request: CreateCategoryRequest = {
            name: formValue.name || '',
            code: formValue.code || '',
            description: formValue.description || '',
            displayOrder: formValue.displayOrder || 0,
            parentCategoryId: this.selectedParentCategory?.id || null
        };

        this.categoryService.createCategory(request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Category created successfully' });
                this.isCreating.set(false);
                this.onCreateDialogHide();
                this.createSuccess.emit();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to create category' });
                this.isCreating.set(false);
                console.error(err);
            }
        });
    }

    onUpdateSubmit() {
        const selectedCat = this.selectedCategory;
        if (!this.updateForm.valid || !selectedCat) {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Please fill all required fields' });
            return;
        }

        this.isUpdating.set(true);
        const formValue = this.updateForm.value as { id?: string | null; name?: string | null; description?: string | null; displayOrder?: number | null; isActive?: boolean | null };
        const request: UpdateCategoryRequest = {
            id: selectedCat.id,
            name: formValue.name ?? '',
            description: formValue.description ?? '',
            displayOrder: formValue.displayOrder ?? 0,
            isActive: formValue.isActive ?? true
        };

        this.categoryService.updateCategory(request.id, request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Category updated successfully' });
                this.isUpdating.set(false);
                this.onUpdateDialogHide();
                this.updateSuccess.emit();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update category' });
                this.isUpdating.set(false);
                console.error(err);
            }
        });
    }

    /**
     * Handle parent category selection from lazy autocomplete
     */
    onParentCategorySelect(category: CategoryResponse) {
        // Check if it's the root category option (id is null or empty)
        const isRootCategory = !category?.id || category.code === 'ROOT';
        
        if (isRootCategory) {
            this.selectedParentCategory = null;
            this.createForm.get('parentCategory')?.setValue(null);
            this.createForm.get('parentCategoryId')?.setValue(null);
        } else {
            this.selectedParentCategory = category;
            this.createForm.get('parentCategory')?.setValue(category);
            this.createForm.get('parentCategoryId')?.setValue(category.id || '');
        }
    }

    /**
     * Handle parent category cleared
     */
    onParentCategoryCleared() {
        this.selectedParentCategory = null;
        this.createForm.get('parentCategory')?.setValue(null);
        this.createForm.get('parentCategoryId')?.setValue('');
    }
}
