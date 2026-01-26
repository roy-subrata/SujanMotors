import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PartService, PartResponse, CreatePartRequest, UpdatePartRequest } from '../../services/part.service';
import { CategoryService, CategoryResponse } from '../../services/category.service';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { BrandService, BrandResponse } from '../../services/brand.service';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { tap } from 'rxjs';

@Component({
    selector: 'app-part-form',
    standalone: true,
    imports: [
        CommonModule,
        FormsModule,
        ReactiveFormsModule,
        ButtonModule,
        InputTextModule,
        InputNumberModule,
        TextareaModule,
        AutoCompleteModule,
        CheckboxModule,
        TooltipModule,
        CardModule,
        ToastModule
    ],
    providers: [MessageService],
    templateUrl: './part-form.component.html',
    styleUrls: ['./part-form.component.css']
})
export class PartFormComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly categoryService = inject(CategoryService);
    private readonly unitService = inject(UnitService);
    private readonly brandService = inject(BrandService);
    private readonly messageService = inject(MessageService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly codeGenerationService = inject(CodeGenerationService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    partForm!: FormGroup;
    isEditMode = false;
    isViewMode = false;
    partId: string | null = null;
    isSubmitting = false;
    isLoading = false;

    categories: CategoryResponse[] = [];
    units: UnitResponse[] = [];
    brands: BrandResponse[] = [];

    filteredCategories: CategoryResponse[] = [];
    filteredUnits: UnitResponse[] = [];
    filteredBrands: BrandResponse[] = [];

    selectedCategory: CategoryResponse | null = null;
    selectedUnit: UnitResponse | null = null;
    selectedBrand: BrandResponse | null = null;

    warrantyTypes = [
        { label: 'Manufacturer', value: 'MANUFACTURER' },
        { label: 'Seller', value: 'SELLER' },
        { label: 'Extended', value: 'EXTENDED' }
    ];

    constructor() {
        this.initializeForm();
    }

    ngOnInit(): void {
        this.loadCategories();
        this.loadUnits();
        this.loadBrands();
        this.checkRouteParams();
    }

    private checkRouteParams(): void {
        // Check if we're in view/edit mode
        this.route.queryParams.subscribe(params => {
            this.partId = params['id'];
            this.isViewMode = params['mode'] === 'view';
            this.isEditMode = params['mode'] === 'edit';

            if (this.partId) {
                this.loadPart(this.partId);
            } else {
                // Create mode - generate code
                this.generateCode();
            }

            // Disable form if in view mode
            if (this.isViewMode) {
                this.partForm.disable();
            }
        });
    }

    private generateCode(): void {
        this.codeGenerationService.getCode('Part')
            .pipe(
                tap({
                    next: (code) => {
                        if (code) {
                            this.partForm.patchValue({
                                partNumber: code,
                                sku: code
                            });
                        }
                    },
                    error: (err) => {
                        console.error('Error generating code:', err);
                    }
                })
            )
            .subscribe();
    }

    private loadPart(id: string): void {
        this.isLoading = true;
        this.partService.getPartById(id).subscribe({
            next: (part) => {
                this.populateForm(part);
                this.isLoading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load part'
                });
                console.error('Error loading part:', error);
                this.isLoading = false;
            }
        });
    }

    private populateForm(part: PartResponse): void {
        this.selectedCategory = this.categories.find(c => c.id === part.categoryId) || null;
        this.selectedUnit = this.units.find(u => u.id === part.unitId) || null;
        this.selectedBrand = this.brands.find(b => b.id === part.brandId) || null;

        this.partForm.patchValue({
            name: part.name,
            description: part.description,
            partNumber: part.partNumber,
            sku: part.sku,
            categoryId: part.categoryId,
            brandId: part.brandId,
            unitId: part.unitId,
            costPrice: part.costPrice,
            sellingPrice: part.sellingPrice,
            minimumStock: part.minimumStock,
            isActive: part.isActive,
            // Warranty fields
            hasWarranty: part.hasWarranty || false,
            warrantyPeriodMonths: part.warrantyPeriodMonths || null,
            warrantyType: part.warrantyType || '',
            warrantyTerms: part.warrantyTerms || '',
            warrantyCertificateTemplate: part.warrantyCertificateTemplate || ''
        });
    }

    private initializeForm(): void {
        this.partForm = this.formBuilder.group({
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: [''],
            partNumber: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20)]],
            sku: ['', [Validators.required, Validators.maxLength(50)]],
            categoryId: ['', [Validators.required]],
            brandId: [null],
            unitId: [null],
            costPrice: [0, [Validators.required, Validators.min(0)]],
            sellingPrice: [0, [Validators.required, Validators.min(0)]],
            minimumStock: [0, [Validators.required, Validators.min(0)]],
            isActive: [true],
            // Warranty fields
            hasWarranty: [false],
            warrantyPeriodMonths: [null],
            warrantyType: [''],
            warrantyTerms: [''],
            warrantyCertificateTemplate: ['']
        });

        // Add conditional validators for warranty fields
        this.partForm.get('hasWarranty')?.valueChanges.subscribe(hasWarranty => {
            const warrantyPeriodControl = this.partForm.get('warrantyPeriodMonths');
            const warrantyTypeControl = this.partForm.get('warrantyType');

            if (hasWarranty) {
                warrantyPeriodControl?.setValidators([Validators.required, Validators.min(1)]);
                warrantyTypeControl?.setValidators([Validators.required]);
            } else {
                warrantyPeriodControl?.clearValidators();
                warrantyTypeControl?.clearValidators();
            }

            warrantyPeriodControl?.updateValueAndValidity();
            warrantyTypeControl?.updateValueAndValidity();
        });
    }

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

    // Autocomplete handlers
    onCategorySearch(event: any): void {
        const query = event.query || '';
        this.filteredCategories = this.categories.filter(category =>
            category.name.toLowerCase().includes(query.toLowerCase()) ||
            category.code.toLowerCase().includes(query.toLowerCase())
        );
    }

    onUnitSearch(event: any): void {
        const query = event.query || '';
        this.filteredUnits = this.units.filter(unit =>
            unit.name.toLowerCase().includes(query.toLowerCase()) ||
            unit.code.toLowerCase().includes(query.toLowerCase())
        );
    }

    onBrandSearch(event: any): void {
        const query = event.query || '';
        this.filteredBrands = this.brands.filter(brand =>
            brand.name.toLowerCase().includes(query.toLowerCase()) ||
            brand.code.toLowerCase().includes(query.toLowerCase())
        );
    }

    onCategorySelect(event: any): void {
        const category = event.value as CategoryResponse;
        this.selectedCategory = category;
        this.partForm.patchValue({ categoryId: category.id });
    }

    onUnitSelect(event: any): void {
        const unit = event.value as UnitResponse;
        this.selectedUnit = unit;
        this.partForm.patchValue({ unitId: unit.id });
    }

    onBrandSelect(event: any): void {
        const brand = event.value as BrandResponse;
        this.selectedBrand = brand;
        this.partForm.patchValue({ brandId: brand.id });
    }

    onCategoryClear(): void {
        this.selectedCategory = null;
        this.partForm.patchValue({ categoryId: null });
    }

    onUnitClear(): void {
        this.selectedUnit = null;
        this.partForm.patchValue({ unitId: null });
    }

    onBrandClear(): void {
        this.selectedBrand = null;
        this.partForm.patchValue({ brandId: null });
    }

    onSubmit(): void {
        if (this.partForm.invalid) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please fill in all required fields correctly'
            });
            return;
        }

        this.isSubmitting = true;

        if (this.isEditMode && this.partId) {
            this.updatePart();
        } else {
            this.createPart();
        }
    }

    private createPart(): void {
        const request: CreatePartRequest = {
            name: this.partForm.value.name,
            description: this.partForm.value.description || '',
            partNumber: this.partForm.value.partNumber,
            sku: this.partForm.value.sku,
            categoryId: this.selectedCategory?.id || '',
            brandId: this.selectedBrand?.id || null,
            unitId: this.selectedUnit?.id || null,
            costPrice: this.partForm.value.costPrice || 0,
            sellingPrice: this.partForm.value.sellingPrice || 0,
            minimumStock: this.partForm.value.minimumStock || 0,
            // Warranty fields
            hasWarranty: this.partForm.value.hasWarranty || false,
            warrantyPeriodMonths: this.partForm.value.hasWarranty ? this.partForm.value.warrantyPeriodMonths : null,
            warrantyType: this.partForm.value.hasWarranty ? this.partForm.value.warrantyType : null,
            warrantyTerms: this.partForm.value.hasWarranty ? this.partForm.value.warrantyTerms : null,
            warrantyCertificateTemplate: this.partForm.value.hasWarranty ? this.partForm.value.warrantyCertificateTemplate : null
        };

        this.partService.createPart(request).subscribe({
            next: (response) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Part '${response.name}' created successfully`
                });
                this.isSubmitting = false;
                this.router.navigate(['/inventory/parts']);
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

    private updatePart(): void {
        const request: UpdatePartRequest = {
            id: this.partId!,
            name: this.partForm.value.name,
            description: this.partForm.value.description || '',
            sku: this.partForm.value.sku,
            categoryId: this.selectedCategory?.id || '',
            brandId: this.selectedBrand?.id || null,
            unitId: this.selectedUnit?.id || null,
            costPrice: this.partForm.value.costPrice || 0,
            sellingPrice: this.partForm.value.sellingPrice || 0,
            minimumStock: this.partForm.value.minimumStock || 0,
            isActive: this.partForm.value.isActive,
            // Warranty fields
            hasWarranty: this.partForm.value.hasWarranty || false,
            warrantyPeriodMonths: this.partForm.value.hasWarranty ? this.partForm.value.warrantyPeriodMonths : null,
            warrantyType: this.partForm.value.hasWarranty ? this.partForm.value.warrantyType : null,
            warrantyTerms: this.partForm.value.hasWarranty ? this.partForm.value.warrantyTerms : null,
            warrantyCertificateTemplate: this.partForm.value.hasWarranty ? this.partForm.value.warrantyCertificateTemplate : null
        };

        this.partService.updatePart(this.partId!, request).subscribe({
            next: (response) => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Part '${response.name}' updated successfully`
                });
                this.isSubmitting = false;
                this.router.navigate(['/inventory/parts']);
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

    onCancel(): void {
        this.router.navigate(['/inventory/parts']);
    }

    hasError(fieldName: string): boolean {
        const control = this.partForm.get(fieldName);
        return control ? control.invalid && control.touched : false;
    }

    getErrorMessage(fieldName: string): string {
        const control = this.partForm.get(fieldName);
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

    private getFieldLabel(fieldName: string): string {
        const labels: { [key: string]: string } = {
            name: 'Part Name',
            partNumber: 'Part Number',
            sku: 'SKU',
            categoryId: 'Category',
            costPrice: 'Cost Price',
            sellingPrice: 'Selling Price',
            minimumStock: 'Minimum Stock',
            warrantyPeriodMonths: 'Warranty Period',
            warrantyType: 'Warranty Type'
        };
        return labels[fieldName] || fieldName;
    }

    isFieldValid(fieldName: string): boolean {
        const control = this.partForm.get(fieldName);
        return control ? control.valid && control.touched : false;
    }

    get pageTitle(): string {
        if (this.isViewMode) return 'View Part';
        if (this.isEditMode) return 'Edit Part';
        return 'Create New Part';
    }
}
