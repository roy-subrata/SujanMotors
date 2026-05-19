import { Component, Output, EventEmitter, signal, inject, Input, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { BrandResponse, BrandService, CreateBrandRequest, UpdateBrandRequest } from '../../services/brand.service';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';


@Component({
    selector: 'app-brands-form-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule, ToastModule],
    templateUrl: './brands-form-dialog.component.html',
    styleUrls: ['./brands-form-dialog.component.css'],
    providers: [MessageService]
})
export class BrandsFormDialogComponent {
    @Input() displayCreateDialog: boolean = false;
    @Input() displayUpdateDialog: boolean = false;
    @Input() selectedBrand: BrandResponse | null = null;

    @Output() displayCreateDialogChange = new EventEmitter<boolean>();
    @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
    @Output() createSuccess = new EventEmitter<void>();
    @Output() updateSuccess = new EventEmitter<void>();

    private readonly fb = inject(FormBuilder);
    private readonly brandService = inject(BrandService);
    private readonly messageService = inject(MessageService);
    private readonly codeGenerationService = inject(CodeGenerationService);

    generatingCode = false;

    updateForm = this.fb.group({
        id: [''],
        name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description: [''],
        country: [''],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive: [true]
    });

    createForm = this.fb.group({
        name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(100)]],
        description: [''],
        code: [{ value: '', disabled: true }, [Validators.required, Validators.minLength(1), Validators.maxLength(20)]],
        country: [''],
        displayOrder: [0, [Validators.required, Validators.min(0)]],
        isActive: [true]
    });

    isCreating = signal(false);
    isUpdating = signal(false);

    constructor() {
        effect(() => {
            // No parent category effect needed for brands
        });
    }

    /**
     * Called when the update dialog is shown - populate form with selected brand data
     */
    onUpdateDialogShow() {
        if (this.selectedBrand) {
            this.updateForm.patchValue({
                id: this.selectedBrand.id,
                name: this.selectedBrand.name,
                description: this.selectedBrand.description,
                country: this.selectedBrand.country,
                displayOrder: this.selectedBrand.displayOrder,
                isActive: this.selectedBrand.isActive
            });
        }
    }

    onCreateDialogShow() {
        this.createForm.reset({ displayOrder: 0, isActive: true, country: '' });
        this.generateBrandCode();
    }

    onCreateDialogHide() {
        this.displayCreateDialogChange.emit(false);
        this.createForm.reset({ displayOrder: 0, isActive: true, country: '' });
    }

    generateBrandCode() {
        this.generatingCode = true;
        this.codeGenerationService.generateBrandCode().subscribe({
            next: (code) => {
                this.createForm.patchValue({ code });
                this.generatingCode = false;
            },
            error: (err) => {
                console.error('Failed to generate brand code:', err);
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
        const request: CreateBrandRequest = {
            name: formValue.name || '',
            code: formValue.code || '',
            description: formValue.description || '',
            country: formValue.country || ''
        };

        this.brandService.createBrand(request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Brand created successfully' });
                this.isCreating.set(false);
                this.onCreateDialogHide();
                this.createSuccess.emit();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Failed to create brand' });
                this.isCreating.set(false);
                console.error(err);
            }
        });
    }

    onUpdateSubmit() {
        const selectedBrand = this.selectedBrand;
        if (!this.updateForm.valid || !selectedBrand) {
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Please fill all required fields' });
            return;
        }

        this.isUpdating.set(true);
        const formValue = this.updateForm.value as { id?: string | null; name?: string | null; description?: string | null; country?: string | null; displayOrder?: number | null; isActive?: boolean | null };
        const request: UpdateBrandRequest = {
            id: selectedBrand.id,
            name: formValue.name ?? '',
            code: selectedBrand.code,
            description: formValue.description ?? '',
            logoUrl: selectedBrand.logoUrl || '',
            website: selectedBrand.website || '',
            country: formValue.country ?? '',
            contactEmail: selectedBrand.contactEmail || '',
            contactPhone: selectedBrand.contactPhone || '',
            displayOrder: formValue.displayOrder ?? 0,
            isActive: formValue.isActive ?? true
        };

        this.brandService.updateBrand(request.id, request).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Brand updated successfully' });
                this.isUpdating.set(false);
                this.onUpdateDialogHide();
                this.updateSuccess.emit();
            },
            error: (err) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update brand' });
                this.isUpdating.set(false);
                console.error(err);
            }
        });
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
