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
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PartService, PartResponse, CreatePartRequest, UpdatePartRequest, VehicleCompatibilityResponse } from '../../services/part.service';
import { CategoryService, CategoryResponse } from '../../services/category.service';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { BrandService, BrandResponse } from '../../services/brand.service';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CheckboxModule } from 'primeng/checkbox';
import { CodeGenerationService } from '@/shared/services/CodeGenerationService';
import { VehicleService, VehicleResponse, CreatePartCompatibilityRequest } from '../../services/vehicle.service';
import { forkJoin, of, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

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
        ToastModule,
        TableModule,
        TagModule,
        ConfirmDialogModule
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './part-form.component.html',
    styleUrls: ['./part-form.component.css']
})
export class PartFormComponent implements OnInit {
    private readonly partService = inject(PartService);
    private readonly categoryService = inject(CategoryService);
    private readonly unitService = inject(UnitService);
    private readonly brandService = inject(BrandService);
    private readonly vehicleService = inject(VehicleService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly codeGenerationService = inject(CodeGenerationService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    partForm!: FormGroup;
    compatibilityForm!: FormGroup;
    isEditMode = false;
    isViewMode = false;
    partId: string | null = null;
    isSubmitting = false;
    isLoading = false;
    isCompatibilitySubmitting = false;
    loadingCompatibilities = false;

    categories: CategoryResponse[] = [];
    units: UnitResponse[] = [];
    brands: BrandResponse[] = [];
    vehicles: VehicleResponse[] = [];

    filteredCategories: CategoryResponse[] = [];
    filteredUnits: UnitResponse[] = [];
    filteredBrands: BrandResponse[] = [];
    filteredVehicles: VehicleResponse[] = [];

    selectedCategory: CategoryResponse | null = null;
    selectedUnit: UnitResponse | null = null;
    selectedBrand: BrandResponse | null = null;
    selectedVehicle: VehicleResponse | null = null;

    compatibleVehicles: VehicleCompatibilityResponse[] = [];
    pendingCompatibilities: Array<{ vehicle: VehicleResponse; isCompatible: boolean; notes: string }> = [];

    warrantyTypes = [
        { label: 'Manufacturer', value: 'MANUFACTURER' },
        { label: 'Seller', value: 'SELLER' },
        { label: 'Extended', value: 'EXTENDED' }
    ];

    constructor() {
        this.initializeForm();
        this.initializeCompatibilityForm();
    }

    ngOnInit(): void {
        this.loadCategories();
        this.loadUnits();
        this.loadBrands();
        this.loadVehicles();
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
                this.loadCompatibleVehicles();
            } else {
                // Create mode - generate code
                this.generateCode();
            }

            // Disable form if in view mode
            if (this.isViewMode) {
                this.partForm.disable();
                this.compatibilityForm.disable();
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

        this.syncSelectedLookups();
    }

    private initializeForm(): void {
        this.partForm = this.formBuilder.group({
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: [''],
            partNumber: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(20), Validators.pattern(/^[A-Za-z]/)]],
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

    private initializeCompatibilityForm(): void {
        this.compatibilityForm = this.formBuilder.group({
            vehicle: [null, Validators.required],
            isCompatible: [true],
            notes: ['', [Validators.maxLength(500)]]
        });
    }

    private loadCategories(): void {
        this.categoryService.getAllCategories().subscribe({
            next: (response) => {
                this.categories = response;
                this.filteredCategories = response;
                this.syncSelectedLookups();
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
                this.syncSelectedLookups();
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
                this.syncSelectedLookups();
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

    private loadVehicles(): void {
        this.vehicleService.getActiveVehicles().subscribe({
            next: (response) => {
                this.vehicles = response;
                this.filteredVehicles = response;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load vehicles'
                });
                console.error('Error loading vehicles:', error);
            }
        });
    }

    private loadCompatibleVehicles(): void {
        if (!this.partId) return;

        this.loadingCompatibilities = true;
        this.partService.getPartCompatibleVehicles(this.partId).subscribe({
            next: (vehicles) => {
                this.compatibleVehicles = vehicles;
                this.loadingCompatibilities = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load compatible vehicles'
                });
                console.error('Error loading compatible vehicles:', error);
                this.loadingCompatibilities = false;
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

    onVehicleSearch(event: any): void {
        const query = (event.query || '').toLowerCase();
        this.filteredVehicles = this.vehicles.filter(vehicle =>
            `${vehicle.make} ${vehicle.model}`.toLowerCase().includes(query) ||
            String(vehicle.year).includes(query) ||
            vehicle.engineType.toLowerCase().includes(query)
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

    onVehicleSelect(event: any): void {
        const vehicle = event.value as VehicleResponse;
        this.selectedVehicle = vehicle;
        this.compatibilityForm.patchValue({ vehicle: vehicle });
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

    onVehicleClear(): void {
        this.selectedVehicle = null;
        this.compatibilityForm.patchValue({ vehicle: null });
    }

    addCompatibility(): void {
        if (this.isViewMode) return;

        if (this.compatibilityForm.invalid) {
            Object.keys(this.compatibilityForm.controls).forEach(key => {
                this.compatibilityForm.get(key)?.markAsTouched();
            });
            this.messageService.add({
                severity: 'warn',
                summary: 'Validation Error',
                detail: 'Please select a vehicle'
            });
            return;
        }

        const vehicle = this.compatibilityForm.value.vehicle as VehicleResponse;
        const request: CreatePartCompatibilityRequest = {
            isCompatible: this.compatibilityForm.value.isCompatible,
            notes: this.compatibilityForm.value.notes || ''
        };

        if (this.partId) {
            this.isCompatibilitySubmitting = true;
            this.vehicleService.addPartCompatibility(vehicle.id, this.partId, request).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Vehicle '${vehicle.make} ${vehicle.model}' added`
                    });
                    this.resetCompatibilityForm();
                    this.loadCompatibleVehicles();
                    this.isCompatibilitySubmitting = false;
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.error?.message || 'Failed to add compatibility'
                    });
                    console.error('Error adding compatibility:', error);
                    this.isCompatibilitySubmitting = false;
                }
            });
            return;
        }

        const exists = this.pendingCompatibilities.some(item => item.vehicle.id === vehicle.id);
        if (exists) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Duplicate',
                detail: 'This vehicle is already added'
            });
            return;
        }

        this.pendingCompatibilities = [
            ...this.pendingCompatibilities,
            {
                vehicle,
                isCompatible: request.isCompatible,
                notes: request.notes || ''
            }
        ];
        this.resetCompatibilityForm();
    }

    removeCompatibility(item: { id?: string; vehicleId: string; isPending?: boolean }): void {
        if (this.isViewMode) return;

        this.confirmationService.confirm({
            message: 'Are you sure you want to remove this compatibility?',
            header: 'Confirm Removal',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-danger',
            accept: () => {
                if (item.isPending) {
                    this.pendingCompatibilities = this.pendingCompatibilities.filter(p => p.vehicle.id !== item.vehicleId);
                    return;
                }

                if (!item.id) return;
                this.vehicleService.removeCompatibility(item.id).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: 'Compatibility removed successfully'
                        });
                        this.loadCompatibleVehicles();
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to remove compatibility'
                        });
                        console.error('Error removing compatibility:', error);
                    }
                });
            }
        });
    }

    private resetCompatibilityForm(): void {
        this.compatibilityForm.reset({
            vehicle: null,
            isCompatible: true,
            notes: ''
        });
        this.selectedVehicle = null;
    }

    getCompatibilityRows(): Array<{
        id?: string;
        vehicleId: string;
        vehicleInfo: string;
        isCompatible: boolean;
        notes: string;
        isPending?: boolean;
    }> {
        const apiRows = this.compatibleVehicles.map(vehicle => ({
            id: vehicle.id,
            vehicleId: vehicle.vehicleId,
            vehicleInfo: `${vehicle.vehicleMake} ${vehicle.vehicleModel} ${vehicle.vehicleYear} • ${vehicle.vehicleEngineType}`,
            isCompatible: vehicle.isCompatible,
            notes: vehicle.notes || ''
        }));

        const pendingRows = this.pendingCompatibilities.map(item => ({
            vehicleId: item.vehicle.id,
            vehicleInfo: `${item.vehicle.make} ${item.vehicle.model} ${item.vehicle.year} • ${item.vehicle.engineType}`,
            isCompatible: item.isCompatible,
            notes: item.notes || '',
            isPending: true
        }));

        return [...pendingRows, ...apiRows];
    }

    onSubmit(): void {
        if (this.partForm.invalid) {
            this.partForm.markAllAsTouched();
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
            name: (this.partForm.value.name || '').trim(),
            description: this.partForm.value.description || '',
            partNumber: (this.partForm.value.partNumber || '').trim(),
            sku: (this.partForm.value.sku || '').trim(),
            categoryId: this.partForm.value.categoryId,
            brandId: this.partForm.value.brandId || null,
            unitId: this.partForm.value.unitId || null,
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
                const complete = () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: `Part '${response.name}' created successfully`
                    });
                    this.isSubmitting = false;
                    this.router.navigate(['/inventory/parts']);
                };

                if (this.pendingCompatibilities.length === 0) {
                    complete();
                    return;
                }

                this.savePendingCompatibilities(response.id).subscribe({
                    next: () => complete(),
                    error: () => complete()
                });
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
            name: (this.partForm.value.name || '').trim(),
            description: this.partForm.value.description || '',
            sku: (this.partForm.value.sku || '').trim(),
            categoryId: this.partForm.value.categoryId,
            brandId: this.partForm.value.brandId || null,
            unitId: this.partForm.value.unitId || null,
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

    private savePendingCompatibilities(partId: string) {
        const requests = this.pendingCompatibilities.map(item =>
            this.vehicleService
                .addPartCompatibility(item.vehicle.id, partId, {
                    isCompatible: item.isCompatible,
                    notes: item.notes || ''
                })
                .pipe(catchError(() => of(null)))
        );

        if (requests.length === 0) {
            return of(void 0);
        }

        return forkJoin(requests).pipe(
            tap((results) => {
                const failedCount = results.filter(result => !result).length;
                if (failedCount > 0) {
                    this.messageService.add({
                        severity: 'warn',
                        summary: 'Partial Success',
                        detail: `${failedCount} vehicle compatibility item(s) failed to save`
                    });
                }
            }),
            // normalize to a single observable type for subscribe
            map(() => void 0)
        );
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
        if (control?.hasError('pattern') && fieldName === 'partNumber') {
            return 'Part Number must start with a letter';
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

    getCompatibilitySeverity(isCompatible: boolean): string {
        return isCompatible ? 'success' : 'warning';
    }

    getCompatibilityLabel(isCompatible: boolean): string {
        return isCompatible ? 'Compatible' : 'Not Compatible';
    }

    private syncSelectedLookups(): void {
        const categoryId = this.partForm?.value?.categoryId;
        const unitId = this.partForm?.value?.unitId;
        const brandId = this.partForm?.value?.brandId;

        if (categoryId && !this.selectedCategory) {
            this.selectedCategory = this.categories.find(c => c.id === categoryId) || null;
        }
        if (unitId && !this.selectedUnit) {
            this.selectedUnit = this.units.find(u => u.id === unitId) || null;
        }
        if (brandId && !this.selectedBrand) {
            this.selectedBrand = this.brands.find(b => b.id === brandId) || null;
        }
    }
}
