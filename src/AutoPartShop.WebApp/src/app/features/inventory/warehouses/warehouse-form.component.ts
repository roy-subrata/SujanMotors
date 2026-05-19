import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';

@Component({
    selector: 'app-warehouse-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, CardModule, InputTextModule, InputNumberModule, TextareaModule, ToastModule],
    providers: [MessageService],
    templateUrl: './warehouse-form.component.html',
    styleUrls: ['./warehouse-form.component.css']
})
export class WarehouseFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly warehouseService = inject(WarehouseService);
    private readonly messageService = inject(MessageService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly codeGenerationService = inject(CodeGenerationService);

    form: FormGroup;
    isEditMode = false;
    isViewMode = false;
    isSubmitting = false;
    warehouseId: string | null = null;
    pageTitle = 'Create Warehouse';
    private loadedWarehouse: WarehouseResponse | null = null;
    generatingCode = false;

    constructor() {
        this.form = this.createForm();
    }

    ngOnInit(): void {
        // Check if we're in edit or view mode
        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.warehouseId = params['id'];
                this.isEditMode = this.router.url.includes('/edit');
                this.isViewMode = this.router.url.includes('/view');
                this.pageTitle = this.isViewMode ? 'View Warehouse' : 'Edit Warehouse';

                if (this.warehouseId) {
                    this.loadWarehouse(this.warehouseId);
                }

                if (this.isViewMode) {
                    this.form.disable();
                } else {
                    this.form.get('code')?.disable();
                }
            } else {
                this.form.get('code')?.disable();
                this.generateWarehouseCode();
            }
        });
    }
    

    private generateWarehouseCode(): void {
        this.generatingCode = true;
        this.form.patchValue({ code: '' });

        this.codeGenerationService.generateWarehouseCode().subscribe({
            next: (code) => {
                if (code) {
                    this.form.get('code')?.patchValue(code);
                }
                this.generatingCode = false;
            },
            error: (error) => {
                console.error('Error generating warehouse code:', error);
                this.messageService.add({
                    severity: 'warn',
                    summary: 'Warning',
                    detail: 'Failed to generate warehouse code. Please enter manually.'
                });
                this.form.get('code')?.enable();
                this.generatingCode = false;
            }
        });
    }
    /**
     * Create form group
     */
    private createForm(): FormGroup {
        return this.fb.group({
            name: ['', [Validators.required, Validators.minLength(2)]],
            code: ['', [Validators.required, Validators.minLength(3)]],
            location: ['', [Validators.required, Validators.minLength(3)]],
            capacity: [0, [Validators.min(0)]],
            capacityUnit: ['SQM'],
            currentStock: [{ value: 0, disabled: true }],
            description: ['']
        });
    }

    /**
     * Load warehouse data for editing
     */
    private loadWarehouse(id: string): void {
        this.warehouseService.getWarehouseById(id).subscribe({
            next: (warehouse: WarehouseResponse) => {
                this.loadedWarehouse = warehouse;
                this.form.patchValue({
                    name: warehouse.name,
                    code: warehouse.code,
                    location: warehouse.location,
                    capacity: warehouse.storageCapacity ?? warehouse.capacity ?? 0,
                    capacityUnit: warehouse.capacityUnit ?? 'SQM',
                    currentStock: warehouse.currentStock,
                    description: warehouse.description
                });
            },
            error: (error: any) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load warehouse details'
                });
                console.error('Error loading warehouse:', error);
            }
        });
    }

    /**
     * Submit form
     */
    onSubmit(): void {
        if (this.generatingCode) {
            this.messageService.add({
                severity: 'info',
                summary: 'Please wait',
                detail: 'Generating warehouse code...'
            });
            return;
        }

        if (!this.form.valid) {
            this.messageService.add({
                severity: 'error',
                summary: 'Error',
                detail: 'Please fill all required fields'
            });
            return;
        }

        this.isSubmitting = true;

        const capacity = Number(this.form.getRawValue().capacity) || 0;
        const capacityUnit = (this.form.getRawValue().capacityUnit ?? 'SQM').toString();

        if (this.isEditMode && this.warehouseId) {
            const warehouseData = {
                name: this.form.getRawValue().name,
                location: this.form.getRawValue().location,
                city: this.loadedWarehouse?.city ?? '',
                state: this.loadedWarehouse?.state ?? '',
                country: this.loadedWarehouse?.country ?? '',
                postalCode: this.loadedWarehouse?.postalCode ?? '',
                manager: this.loadedWarehouse?.manager ?? '',
                managerEmail: this.loadedWarehouse?.managerEmail ?? '',
                managerPhone: this.loadedWarehouse?.managerPhone ?? '',
                storageCapacity: capacity,
                capacityUnit,
                description: this.form.getRawValue().description ?? '',
                isActive: this.loadedWarehouse?.isActive ?? true
            };

            // Update existing warehouse
            this.warehouseService.updateWarehouse(this.warehouseId, warehouseData).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Warehouse '${warehouseData.name}' updated successfully`
                    });
                    this.router.navigate(['/inventory/warehouses']);
                },
                error: (error: any) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.error?.message || 'Failed to update warehouse'
                    });
                    console.error('Error updating warehouse:', error);
                    this.isSubmitting = false;
                }
            });
        } else {
            const code = (this.form.getRawValue().code ?? '').toString().trim();
            if (!code) {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Warehouse code is required'
                });
                this.isSubmitting = false;
                return;
            }

            const warehouseData = {
                name: this.form.getRawValue().name,
                code,
                location: this.form.getRawValue().location,
                city: '',
                state: '',
                country: '',
                postalCode: '',
                manager: '',
                managerEmail: '',
                managerPhone: '',
                storageCapacity: capacity,
                capacityUnit,
                description: this.form.getRawValue().description ?? ''
            };

            // Create new warehouse
            this.warehouseService.createWarehouse(warehouseData).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Warehouse '${warehouseData.name}' created successfully`
                    });
                    this.router.navigate(['/inventory/warehouses']);
                },
                error: (error: any) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.error?.message || 'Failed to create warehouse'
                    });
                    console.error('Error creating warehouse:', error);
                    this.isSubmitting = false;
                }
            });
        }
    }

    /**
     * Go back to list
     */
    goBack(): void {
        this.router.navigate(['/inventory/warehouses']);
    }

    /**
     * Check if field has error
     */
    hasError(fieldName: string): boolean {
        const field = this.form.get(fieldName);
        return !!(field && field.invalid && field.touched);
    }

    /**
     * Get error message
     */
    getErrorMessage(fieldName: string): string {
        const field = this.form.get(fieldName);
        if (field?.hasError('required')) {
            return `${this.formatFieldName(fieldName)} is required`;
        }
        if (field?.hasError('minlength')) {
            const minLength = field.errors?.['minlength'].requiredLength;
            return `${this.formatFieldName(fieldName)} must be at least ${minLength} characters`;
        }
        if (field?.hasError('min')) {
            const min = field.errors?.['min'].min;
            return `${this.formatFieldName(fieldName)} must be at least ${min}`;
        }
        return '';
    }

    /**
     * Format field name for display
     */
    private formatFieldName(fieldName: string): string {
        return fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
    }
}
