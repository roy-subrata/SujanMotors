import { Component, EventEmitter, Input, Output, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService } from 'primeng/api';
import { PartService, PartResponse, CreatePartRequest, UpdatePartRequest } from '../../services/part.service';
import { CategoryService, CategoryResponse } from '../../services/category.service';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { BrandService, BrandResponse } from '../../services/brand.service';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { tap } from 'rxjs';

@Component({
    selector: 'app-parts-form-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule, ReactiveFormsModule, DialogModule, ButtonModule, InputTextModule, InputNumberModule, AutoCompleteModule, TooltipModule],
    templateUrl: './parts-form-dialog.component.html',
    styleUrls: ['./parts-form-dialog.component.css']
})
export class PartsFormDialogComponent implements OnInit {
    @Input() displayCreateDialog = false;
    @Input() displayUpdateDialog = false;
    @Input() selectedPart: PartResponse | null = null;
    @Output() displayCreateDialogChange = new EventEmitter<boolean>();
    @Output() displayUpdateDialogChange = new EventEmitter<boolean>();
    @Output() partCreated = new EventEmitter<PartResponse>();
    @Output() partUpdated = new EventEmitter<PartResponse>();

    private readonly partService = inject(PartService);
    private readonly categoryService = inject(CategoryService);
    private readonly unitService = inject(UnitService);
    private readonly brandService = inject(BrandService);
    private readonly messageService = inject(MessageService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly CodeGenerationService = inject(CodeGenerationService);

    createForm!: FormGroup;
    updateForm!: FormGroup;
    isSubmitting = false;
    categories: CategoryResponse[] = [];
    units: UnitResponse[] = [];
    brands: BrandResponse[] = [];
    warrantyTypes = [
        { label: 'Manufacturer', value: 'MANUFACTURER' },
        { label: 'Seller', value: 'SELLER' },
        { label: 'Extended', value: 'EXTENDED' }
    ];

    // Autocomplete properties for Create dialog
    filteredCategories: CategoryResponse[] = [];
    filteredUnits: UnitResponse[] = [];
    filteredBaseUnits: UnitResponse[] = [];
    filteredBrands: BrandResponse[] = [];
    selectedCreateCategory: CategoryResponse | null = null;
    selectedCreateUnit: UnitResponse | null = null;
    selectedCreateBaseUnit: UnitResponse | null = null;
    selectedCreateBrand: BrandResponse | null = null;

    // Autocomplete properties for Update dialog
    selectedUpdateCategory: CategoryResponse | null = null;
    selectedUpdateUnit: UnitResponse | null = null;
    selectedUpdateBaseUnit: UnitResponse | null = null;
    selectedUpdateBrand: BrandResponse | null = null;
    destroyRef = inject(DestroyRef);

    constructor() {
        this.initializeForms();
    }

    ngOnInit(): void {
        this.loadCategories();
        this.loadUnits();
        this.loadBrands();
        this.setCode();
    }

    private setCode(): void {
        if (!this.selectedPart) {
            this.CodeGenerationService.getCode('Part')
                .pipe(
                    tap({
                        next: (code) => {
                            if (code) {
                                this.createForm.get('partNumber')?.setValue(code);
                                this.createForm.get('sku')?.setValue(code);
                            }
                        },
                        error: (err) => {
                            console.log('Error Generaete code');
                        }
                    })
                )
                .subscribe();
        }
    }

    /**
     * Load all categories
     */
    private loadCategories(): void {
        this.categoryService.getAllCategories().subscribe({
            next: (response) => {
                this.categories = response;
                this.filteredCategories = response;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load categories'
                });
                console.error('Error loading categories:', error);
            }
        });
    }

    /**
     * Load all units
     */
    private loadUnits(): void {
        this.unitService.getAllUnits().subscribe({
            next: (response) => {
                this.units = response;
                this.filteredUnits = response;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load units'
                });
                console.error('Error loading units:', error);
            }
        });
    }

    /**
     * Load all brands
     */
    private loadBrands(): void {
        this.brandService.getActiveBrands().subscribe({
            next: (response) => {
                this.brands = response;
                this.filteredBrands = response;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load brands'
                });
                console.error('Error loading brands:', error);
            }
        });
    }

    /**
     * Initialize forms
     */
    private initializeForms(): void {
        this.createForm = this.formBuilder.group({
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: [''],
            partNumber: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
            sku: ['', [Validators.required, Validators.maxLength(50)]],
            categoryId: ['', [Validators.required]],
            brandId: [null],
            baseUnitId: [null],
            unitId: [null],
            costPrice: [0, [Validators.required, Validators.min(0)]],
            sellingPrice: [0, [Validators.required, Validators.min(0)]],
            minimumStock: [0, [Validators.required, Validators.min(0)]],
            hasWarranty: [false],
            warrantyPeriodMonths: [null],
            warrantyType: [null],
            warrantyTerms: [''],
            warrantyCertificateTemplate: ['']
        });

        this.updateForm = this.formBuilder.group({
            id: [''],
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: [''],
            sku: ['', [Validators.required, Validators.maxLength(50)]],
            categoryId: ['', [Validators.required]],
            brandId: [null],
            baseUnitId: [null],
            unitId: [null],
            costPrice: [0, [Validators.required, Validators.min(0)]],
            sellingPrice: [0, [Validators.required, Validators.min(0)]],
            minimumStock: [0, [Validators.required, Validators.min(0)]],
            isActive: [true],
            hasWarranty: [false],
            warrantyPeriodMonths: [null],
            warrantyType: [null],
            warrantyTerms: [''],
            warrantyCertificateTemplate: ['']
        });

        this.setupWarrantyValidation(this.createForm);
        this.setupWarrantyValidation(this.updateForm);
    }

    private setupWarrantyValidation(form: FormGroup): void {
        const hasWarrantyControl = form.get('hasWarranty');
        const periodControl = form.get('warrantyPeriodMonths');
        const typeControl = form.get('warrantyType');

        const updateValidators = () => {
            const hasWarranty = !!hasWarrantyControl?.value;

            if (hasWarranty) {
                periodControl?.setValidators([Validators.required, Validators.min(1)]);
                typeControl?.setValidators([Validators.required]);
            } else {
                periodControl?.clearValidators();
                typeControl?.clearValidators();
            }

            periodControl?.updateValueAndValidity({ emitEvent: false });
            typeControl?.updateValueAndValidity({ emitEvent: false });
        };

        hasWarrantyControl?.valueChanges.subscribe(updateValidators);
        updateValidators();
    }

    /**
     * Populate update form with selected part data
     */
    onUpdateDialogShow(): void {
        if (this.selectedPart) {
            // Set the selected categories, units and brands for display
            this.selectedUpdateCategory = this.categories.find((c) => c.id === this.selectedPart?.categoryId) || null;
            this.selectedUpdateBaseUnit = this.units.find((u) => u.id === this.selectedPart?.baseUnitId) || null;
            this.selectedUpdateUnit = this.units.find((u) => u.id === this.selectedPart?.unitId) || null;
            this.selectedUpdateBrand = this.brands.find((b) => b.id === this.selectedPart?.brandId) || null;

            this.updateForm.patchValue({
                id: this.selectedPart.id,
                name: this.selectedPart.name,
                description: this.selectedPart.description,
                sku: this.selectedPart.sku,
                categoryId: this.selectedPart.categoryId,
                brandId: this.selectedPart.brandId,
                baseUnitId: this.selectedPart.baseUnitId,
                unitId: this.selectedPart.unitId,
                costPrice: this.selectedPart.costPrice,
                sellingPrice: this.selectedPart.sellingPrice,
                minimumStock: this.selectedPart.minimumStock,
                isActive: this.selectedPart.isActive,
                hasWarranty: this.selectedPart.hasWarranty,
                warrantyPeriodMonths: this.selectedPart.warrantyPeriodMonths,
                warrantyType: this.selectedPart.warrantyType,
                warrantyTerms: this.selectedPart.warrantyTerms ?? '',
                warrantyCertificateTemplate: this.selectedPart.warrantyCertificateTemplate ?? ''
            });
        }
    }

    /**
     * Handle create dialog close
     */
    onCreateDialogHide(): void {
        this.displayCreateDialogChange.emit(false);
        this.createForm.reset();
        this.selectedCreateCategory = null;
        this.selectedCreateUnit = null;
        this.selectedCreateBaseUnit = null;
        this.selectedCreateBrand = null;
    }

    /**
     * Handle update dialog close
     */
    onUpdateDialogHide(): void {
        this.displayUpdateDialogChange.emit(false);
        this.updateForm.reset();
        this.selectedUpdateCategory = null;
        this.selectedUpdateUnit = null;
        this.selectedUpdateBaseUnit = null;
        this.selectedUpdateBrand = null;
    }

    /**
     * Handle category autocomplete event (Create)
     */
    onCategoryEvent(event: any): void {
        const query = event.query || '';
        this.filteredCategories = this.categories.filter((category) => category.name.toLowerCase().includes(query.toLowerCase()) || category.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle unit autocomplete event (Create)
     */
    onUnitEvent(event: any): void {
        const query = event.query || '';
        this.filteredUnits = this.units.filter((unit) => unit.name.toLowerCase().includes(query.toLowerCase()) || unit.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle category selection (Create)
     */
    onCategorySelect(category: CategoryResponse): void {
        this.selectedCreateCategory = category;
        this.createForm.get('categoryId')?.setValue(category.id);
    }

    /**
     * Handle unit selection (Create)
     */
    onUnitSelect(unit: UnitResponse): void {
        this.selectedCreateUnit = unit;
        this.createForm.get('unitId')?.setValue(unit.id);
    }

    /**
     * Handle category cleared (Create)
     */
    onCategoryCleared(): void {
        this.selectedCreateCategory = null;
        this.createForm.get('categoryId')?.setValue(null);
    }

    /**
     * Handle unit cleared (Create)
     */
    onUnitCleared(): void {
        this.selectedCreateUnit = null;
        this.createForm.get('unitId')?.setValue(null);
    }

    /**
     * Handle base unit autocomplete event (Create)
     */
    onBaseUnitEvent(event: any): void {
        const query = event.query || '';
        this.filteredBaseUnits = this.units.filter((unit) => unit.name.toLowerCase().includes(query.toLowerCase()) || unit.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle base unit selection (Create)
     */
    onBaseUnitSelect(unit: UnitResponse): void {
        this.selectedCreateBaseUnit = unit;
        this.createForm.get('baseUnitId')?.setValue(unit.id);
        // If display unit not set, default to base unit
        if (!this.createForm.get('unitId')?.value) {
            this.createForm.get('unitId')?.setValue(unit.id);
            this.selectedCreateUnit = unit;
        }
    }

    /**
     * Handle base unit cleared (Create)
     */
    onBaseUnitCleared(): void {
        this.selectedCreateBaseUnit = null;
        this.createForm.get('baseUnitId')?.setValue(null);
    }

    /**
     * Handle brand autocomplete event (Create)
     */
    onBrandEvent(event: any): void {
        const query = event.query || '';
        this.filteredBrands = this.brands.filter((brand) => brand.name.toLowerCase().includes(query.toLowerCase()) || brand.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle brand selection (Create)
     */
    onBrandSelect(brand: BrandResponse): void {
        this.selectedCreateBrand = brand;
        this.createForm.get('brandId')?.setValue(brand.id);
    }

    /**
     * Handle brand cleared (Create)
     */
    onBrandCleared(): void {
        this.selectedCreateBrand = null;
        this.createForm.get('brandId')?.setValue(null);
    }

    /**
     * Handle category autocomplete event (Update)
     */
    onUpdateCategoryEvent(event: any): void {
        const query = event.query || '';
        this.filteredCategories = this.categories.filter((category) => category.name.toLowerCase().includes(query.toLowerCase()) || category.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle unit autocomplete event (Update)
     */
    onUpdateUnitEvent(event: any): void {
        const query = event.query || '';
        this.filteredUnits = this.units.filter((unit) => unit.name.toLowerCase().includes(query.toLowerCase()) || unit.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle category selection (Update)
     */
    onUpdateCategorySelect(category: CategoryResponse): void {
        this.selectedUpdateCategory = category;
        this.updateForm.get('categoryId')?.setValue(category.id);
    }

    /**
     * Handle unit selection (Update)
     */
    onUpdateUnitSelect(unit: UnitResponse): void {
        this.selectedUpdateUnit = unit;
        this.updateForm.get('unitId')?.setValue(unit.id);
    }

    /**
     * Handle category cleared (Update)
     */
    onUpdateCategoryCleared(): void {
        this.selectedUpdateCategory = null;
        this.updateForm.get('categoryId')?.setValue(null);
    }

    /**
     * Handle unit cleared (Update)
     */
    onUpdateUnitCleared(): void {
        this.selectedUpdateUnit = null;
        this.updateForm.get('unitId')?.setValue(null);
    }

    /**
     * Handle base unit autocomplete event (Update)
     */
    onUpdateBaseUnitEvent(event: any): void {
        const query = event.query || '';
        this.filteredBaseUnits = this.units.filter((unit) => unit.name.toLowerCase().includes(query.toLowerCase()) || unit.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle base unit selection (Update)
     */
    onUpdateBaseUnitSelect(unit: UnitResponse): void {
        this.selectedUpdateBaseUnit = unit;
        this.updateForm.get('baseUnitId')?.setValue(unit.id);
        // If display unit not set, default to base unit
        if (!this.updateForm.get('unitId')?.value) {
            this.updateForm.get('unitId')?.setValue(unit.id);
            this.selectedUpdateUnit = unit;
        }
    }

    /**
     * Handle base unit cleared (Update)
     */
    onUpdateBaseUnitCleared(): void {
        this.selectedUpdateBaseUnit = null;
        this.updateForm.get('baseUnitId')?.setValue(null);
    }

    /**
     * Handle brand autocomplete event (Update)
     */
    onUpdateBrandEvent(event: any): void {
        const query = event.query || '';
        this.filteredBrands = this.brands.filter((brand) => brand.name.toLowerCase().includes(query.toLowerCase()) || brand.code.toLowerCase().includes(query.toLowerCase()));
    }

    /**
     * Handle brand selection (Update)
     */
    onUpdateBrandSelect(brand: BrandResponse): void {
        this.selectedUpdateBrand = brand;
        this.updateForm.get('brandId')?.setValue(brand.id);
    }

    /**
     * Handle brand cleared (Update)
     */
    onUpdateBrandCleared(): void {
        this.selectedUpdateBrand = null;
        this.updateForm.get('brandId')?.setValue(null);
    }

    /**
     * Submit create form
     */
    onCreateSubmit(): void {
        if (this.createForm.invalid) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please fill in all required fields correctly'
            });
            return;
        }

        this.isSubmitting = true;
        const request: CreatePartRequest = {
            name: this.createForm.get('name')?.value ?? '',
            description: this.createForm.get('description')?.value ?? '',
            partNumber: this.createForm.get('partNumber')?.value ?? '',
            sku: this.createForm.get('sku')?.value ?? '',
            categoryId: this.selectedCreateCategory?.id ?? '',
            brandId: this.selectedCreateBrand?.id ?? null,
            baseUnitId: this.selectedCreateBaseUnit?.id ?? null,
            unitId: this.selectedCreateUnit?.id ?? null,
            costPrice: this.createForm.get('costPrice')?.value ?? 0,
            sellingPrice: this.createForm.get('sellingPrice')?.value ?? 0,
            minimumStock: this.createForm.get('minimumStock')?.value ?? 0,
            hasWarranty: this.createForm.get('hasWarranty')?.value ?? false,
            warrantyPeriodMonths: this.createForm.get('hasWarranty')?.value ? this.createForm.get('warrantyPeriodMonths')?.value ?? null : null,
            warrantyType: this.createForm.get('hasWarranty')?.value ? this.createForm.get('warrantyType')?.value ?? null : null,
            warrantyTerms: this.createForm.get('hasWarranty')?.value ? this.createForm.get('warrantyTerms')?.value ?? '' : null,
            warrantyCertificateTemplate: this.createForm.get('hasWarranty')?.value ? this.createForm.get('warrantyCertificateTemplate')?.value ?? '' : null
        };

        this.partService.createPart(request).subscribe({
            next: (response) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Part '${response.name}' created successfully`
                });
                this.partCreated.emit(response);
                this.onCreateDialogHide();
                this.isSubmitting = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to create part'
                });
                console.error('Error creating part:', error);
                this.isSubmitting = false;
            }
        });
    }

    /**
     * Submit update form
     */
    onUpdateSubmit(): void {
        if (this.updateForm.invalid) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please fill in all required fields correctly'
            });
            return;
        }

        this.isSubmitting = true;
        const partId = this.updateForm.get('id')?.value ?? '';
        const request: UpdatePartRequest = {
            id: partId,
            name: this.updateForm.get('name')?.value ?? '',
            description: this.updateForm.get('description')?.value ?? '',
            sku: this.updateForm.get('sku')?.value ?? '',
            categoryId: this.selectedUpdateCategory?.id ?? '',
            brandId: this.selectedUpdateBrand?.id ?? null,
            baseUnitId: this.selectedUpdateBaseUnit?.id ?? null,
            unitId: this.selectedUpdateUnit?.id ?? null,
            costPrice: this.updateForm.get('costPrice')?.value ?? 0,
            sellingPrice: this.updateForm.get('sellingPrice')?.value ?? 0,
            minimumStock: this.updateForm.get('minimumStock')?.value ?? 0,
            isActive: this.updateForm.get('isActive')?.value ?? true,
            hasWarranty: this.updateForm.get('hasWarranty')?.value ?? false,
            warrantyPeriodMonths: this.updateForm.get('hasWarranty')?.value ? this.updateForm.get('warrantyPeriodMonths')?.value ?? null : null,
            warrantyType: this.updateForm.get('hasWarranty')?.value ? this.updateForm.get('warrantyType')?.value ?? null : null,
            warrantyTerms: this.updateForm.get('hasWarranty')?.value ? this.updateForm.get('warrantyTerms')?.value ?? '' : null,
            warrantyCertificateTemplate: this.updateForm.get('hasWarranty')?.value ? this.updateForm.get('warrantyCertificateTemplate')?.value ?? '' : null
        };

        this.partService.updatePart(partId, request).subscribe({
            next: (response) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Part '${response.name}' updated successfully`
                });
                this.partUpdated.emit(response);
                this.onUpdateDialogHide();
                this.isSubmitting = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: error?.error?.message || 'Failed to update part'
                });
                console.error('Error updating part:', error);
                this.isSubmitting = false;
            }
        });
    }

    /**
     * Check if field has error
     */
    hasError(formGroup: FormGroup, fieldName: string): boolean {
        const control = formGroup.get(fieldName);
        return control ? control.invalid && control.touched : false;
    }

    /**
     * Get error message for field
     */
    getErrorMessage(formGroup: FormGroup, fieldName: string): string {
        const control = formGroup.get(fieldName);
        if (control?.hasError('required')) {
            return `${this.getFieldLabel(fieldName)} is required`;
        }
        if (control?.hasError('minlength')) {
            return `${this.getFieldLabel(fieldName)} must be at least ${control.getError('minlength')?.requiredLength} characters`;
        }
        if (control?.hasError('maxlength')) {
            return `${this.getFieldLabel(fieldName)} must not exceed ${control.getError('maxlength')?.requiredLength} characters`;
        }
        if (control?.hasError('min')) {
            return `${this.getFieldLabel(fieldName)} must be at least ${control.getError('min')?.min}`;
        }
        return 'Invalid value';
    }

    /**
     * Get field label for error messages
     */
    private getFieldLabel(fieldName: string): string {
        const labels: { [key: string]: string } = {
            name: 'Part Name',
            partNumber: 'Part Number',
            sku: 'SKU',
            categoryId: 'Category',
            costPrice: 'Cost Price',
            sellingPrice: 'Selling Price',
            minimumStock: 'Minimum Stock',
            warrantyPeriodMonths: 'Warranty Period (months)',
            warrantyType: 'Warranty Type'
        };
        return labels[fieldName] || fieldName;
    }

    /**
     * Check if field is valid
     */
    isFieldValid(formGroup: FormGroup, fieldName: string): boolean {
        const control = formGroup.get(fieldName);
        return control ? control.valid && control.touched : false;
    }
}
