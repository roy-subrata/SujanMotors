import { Component, OnInit, inject, DestroyRef } from '@angular/core';
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
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
    selector: 'app-warehouse-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, CardModule, InputTextModule, InputNumberModule, TextareaModule, ToastModule, TranslatePipe],
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
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    form: FormGroup;
    isEditMode = false;
    isViewMode = false;
    isSubmitting = false;
    warehouseId: string | null = null;
    pageTitle = '';
    private loadedWarehouse: WarehouseResponse | null = null;
    generatingCode = false;

    constructor() {
        this.form = this.createForm();
    }

    ngOnInit(): void {
        this.updatePageTitle();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.updatePageTitle();
        });

        // Check if we're in edit or view mode
        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.warehouseId = params['id'];
                this.isEditMode = this.router.url.includes('/edit');
                this.isViewMode = this.router.url.includes('/view');
                this.updatePageTitle();

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

    private updatePageTitle(): void {
        this.pageTitle = this.isViewMode
            ? this.i18n.t('warehouses.viewWarehouse')
            : this.isEditMode
                ? this.i18n.t('warehouses.editWarehouse')
                : this.i18n.t('warehouses.createWarehouse');
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
                    summary: this.i18n.t('common.messages.warning'),
                    detail: this.i18n.t('warehouses.messages.codeGenerationFailed')
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
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('warehouses.messages.loadDetailsFailed')
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
                summary: this.i18n.t('common.messages.pleaseWait'),
                detail: this.i18n.t('warehouses.messages.generatingCode')
            });
            return;
        }

        if (!this.form.valid) {
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('common.messages.error'),
                detail: this.i18n.t('common.messages.fillRequiredFields')
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
                        summary: this.i18n.t('common.messages.success'),
                        detail: this.i18n.t('warehouses.messages.updateSuccess')
                    });
                    this.router.navigate(['/inventory/warehouses']);
                },
                error: (error: any) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: error?.error?.message || this.i18n.t('warehouses.messages.updateFailed')
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
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('warehouses.messages.codeRequired')
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
                        summary: this.i18n.t('common.messages.success'),
                        detail: this.i18n.t('warehouses.messages.createSuccess')
                    });
                    this.router.navigate(['/inventory/warehouses']);
                },
                error: (error: any) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: this.i18n.t('common.messages.error'),
                        detail: error?.error?.message || this.i18n.t('warehouses.messages.createFailed')
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
            return this.i18n.t('common.messages.fieldRequired', { field: this.formatFieldName(fieldName) });
        }
        if (field?.hasError('minlength')) {
            const minLength = field.errors?.['minlength'].requiredLength;
            return this.i18n.t('common.messages.fieldMinLength', { field: this.formatFieldName(fieldName), min: String(minLength) });
        }
        if (field?.hasError('min')) {
            const min = field.errors?.['min'].min;
            return this.i18n.t('common.messages.fieldMinValue', { field: this.formatFieldName(fieldName), min: String(min) });
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
