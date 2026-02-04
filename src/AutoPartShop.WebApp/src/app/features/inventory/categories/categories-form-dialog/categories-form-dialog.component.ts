import { Component, Output, EventEmitter, signal, inject, Input, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CategoryResponse, CategoryService, CreateCategoryRequest, UpdateCategoryRequest } from '../../services/category.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';


@Component({
    selector: 'app-categories-form-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, AutoCompleteModule, ToastModule],
    templateUrl: './categories-form-dialog.component.html',
    styleUrls: ['./categories-form-dialog.component.css'],
    providers: [MessageService]
})
export class CategoriesFormDialogComponent {
    @Input() displayCreateDialog: boolean = false;
    @Input() displayUpdateDialog: boolean = false;
    @Input() selectedParentCategory: CategoryResponse | null = null;
    @Input() selectedCategory: CategoryResponse | null = null;
    @Input() filteredParentCategories: CategoryResponse[] = [];

    @Output() displayCreateDialogChange = new EventEmitter<boolean>();
    @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
    @Output() createSuccess = new EventEmitter<void>();
    @Output() updateSuccess = new EventEmitter<void>();
    @Output() parentCategoryEvent = new EventEmitter<any>();
    @Output() parentCategorySelect = new EventEmitter<CategoryResponse>();
    @Output() parentCategoryCleared = new EventEmitter<void>();

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
        code: ['', [Validators.required, Validators.minLength(1), Validators.maxLength(20)]],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive: [true],
        parentCategoryId: [''],
        parentCategory: [null as CategoryResponse | null]
    });

    isCreating = signal(false);
    isUpdating = signal(false);

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
        this.parentCategoryCleared.emit();
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
        const formValue = this.createForm.value as { name?: string | null; code?: string | null; description?: string | null; displayOrder?: number | null; isActive?: boolean | null; parentCategoryId?: string | null };
        const request: CreateCategoryRequest = {
            name: formValue.name ?? '',
            code: formValue.code ?? '',
            description: formValue.description ?? '',
            displayOrder: formValue.displayOrder ?? 0,
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

    onParentCategoryEvent(event: any) {
        this.parentCategoryEvent.emit(event);
    }

    onParentCategorySelect(event: any) {
        const selectedCategory = event as CategoryResponse;
        this.createForm.get('parentCategory')?.setValue(selectedCategory);
        this.createForm.get('parentCategoryId')?.setValue(selectedCategory.id || '');
        this.parentCategorySelect.emit(selectedCategory);
    }

    onParentCategoryCleared() {
        this.createForm.get('parentCategory')?.setValue(null);
        this.createForm.get('parentCategoryId')?.setValue('');
        this.parentCategoryCleared.emit();
    }
}
