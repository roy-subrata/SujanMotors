import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormGroup, FormBuilder, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
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
import { CheckboxModule } from 'primeng/checkbox';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { MessageService, ConfirmationService } from 'primeng/api';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { Select } from 'primeng/select';

import { PartService, PartResponse, CreatePartRequest, UpdatePartRequest, VehicleCompatibilityResponse } from '../../services/part.service';
import { CategoryService, CategoryResponse } from '../../services/category.service';
import { UnitService, UnitResponse } from '../../services/unit.service';
import { BrandService, BrandResponse } from '../../services/brand.service';
import { VehicleService, VehicleResponse, CreatePartCompatibilityRequest } from '../../services/vehicle.service';
import { CatalogEntryService, CatalogEntryResponse, UpsertCatalogEntryRequest } from '../../services/catalog-entry.service';
import { ProductVariantManagerComponent } from '../product-variant-manager/product-variant-manager.component';

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
        ToggleSwitchModule,
        Select,
        TooltipModule,
        ProductVariantManagerComponent,
        RouterModule,
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
    private readonly catalogEntryService = inject(CatalogEntryService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);

    partForm!: FormGroup;
    catalogEntryForm!: FormGroup;
    compatibilityForm!: FormGroup;

    isEditMode = false;
    isViewMode = false;
    partId: string | null = null;
    /** Backend still has a CostPrice column (see Cost Model design), but this form has no UI for it — preserve the existing value on update instead of clobbering it. */
    private existingCostPrice = 0;
    isSubmitting = false;
    isLoading = false;
    isCompatibilitySubmitting = false;
    loadingCompatibilities = false;

    categories: CategoryResponse[] = [];
    units: UnitResponse[] = [];
    baseUnits: UnitResponse[] = [];
    brands: BrandResponse[] = [];
    vehicles: VehicleResponse[] = [];

    filteredCategories: CategoryResponse[] = [];
    filteredUnits: UnitResponse[] = [];
    filteredBaseUnits: UnitResponse[] = [];
    filteredBrands: BrandResponse[] = [];
    filteredVehicles: VehicleResponse[] = [];

    selectedCategory: CategoryResponse | null = null;
    selectedBaseUnit: UnitResponse | null = null;
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

    productTypes = [
        { label: 'Physical', value: 'PHYSICAL' },
        { label: 'Digital', value: 'DIGITAL' },
        { label: 'Service', value: 'SERVICE' }
    ];

    taxCodes = [
        { label: 'Standard', value: 'STANDARD' },
        { label: 'Food', value: 'FOOD' },
        { label: 'Medicine', value: 'MEDICINE' },
        { label: 'Exempt', value: 'EXEMPT' }
    ];

    constructor() {
        this.initializeForm();
        this.initializeCatalogEntryForm();
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
        this.route.queryParams.subscribe(params => {
            this.partId = params['id'];
            this.isViewMode = params['mode'] === 'view';
            this.isEditMode = params['mode'] === 'edit';

            if (this.partId) {
                this.loadPart(this.partId);
                this.loadCompatibleVehicles();
                this.loadCatalogEntry(this.partId);
            }

            if (this.isViewMode) {
                this.partForm.disable();
                this.catalogEntryForm.disable();
                this.compatibilityForm.disable();
            }
        });
    }

    private loadPart(id: string): void {
        this.isLoading = true;
        this.partService.getPartById(id).subscribe({
            next: (part) => { this.populateForm(part); this.isLoading = false; },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load part' });
                this.isLoading = false;
            }
        });
    }

    private loadCatalogEntry(partId: string): void {
        this.catalogEntryService.get(partId).subscribe({
            next: (entry) => {
                if (entry) {
                    this.catalogEntryForm.patchValue({
                        slug: entry.slug,
                        shortDescription: entry.shortDescription,
                        isPublished: entry.isPublished,
                        isFeatured: entry.isFeatured,
                        featuredRank: entry.featuredRank,
                        metaTitle: entry.metaTitle ?? '',
                        metaDescription: entry.metaDescription ?? ''
                    });
                }
            },
            error: () => { /* catalog entry is optional, silently ignore */ }
        });
    }

    private populateForm(part: PartResponse): void {
        this.existingCostPrice = part.costPrice ?? 0;
        this.selectedCategory = this.categories.find(c => c.id === part.categoryId) || null;
        this.selectedBaseUnit = this.units.find(u => u.id === part.baseUnitId) || null;
        this.selectedUnit = this.units.find(u => u.id === part.unitId) || null;
        this.selectedBrand = this.brands.find(b => b.id === part.brandId) || null;

        this.partForm.patchValue({
            name: part.name,
            description: part.description,
            richDescription: part.richDescription || '',
            partNumber: part.partNumber,
            oemNumber: part.oemNumber || null,
            localName: part.localName || null,
            barcode: part.barcode || '',
            categoryId: part.categoryId,
            brandId: part.brandId,
            baseUnitId: part.baseUnitId,
            unitId: part.unitId,
            costPrice: part.costPrice ?? 0,
            sellingPrice: part.sellingPrice ?? 0,
            minimumStock: part.minimumStock,
            isActive: part.isActive,
            hasWarranty: part.hasWarranty || false,
            warrantyPeriodMonths: part.warrantyPeriodMonths || null,
            warrantyType: part.warrantyType || '',
            warrantyTerms: part.warrantyTerms || '',
            warrantyCertificateTemplate: part.warrantyCertificateTemplate || '',
            tags: part.tags || '',
            productType: part.productType || 'PHYSICAL',
            isPerishable: part.isPerishable || false,
            weightKg: part.weightKg ?? null,
            taxCode: part.taxCode || ''
        });

        this.syncSelectedLookups();
    }

    private initializeForm(): void {
        this.partForm = this.formBuilder.group({
            name: ['', [Validators.required, Validators.maxLength(200)]],
            description: [''],
            richDescription: [''],
            partNumber: ['', [Validators.required, Validators.maxLength(30)]],
            oemNumber: [null, [Validators.maxLength(100)]],
            localName: [null, [Validators.maxLength(200)]],
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
            warrantyType: [''],
            warrantyTerms: [''],
            warrantyCertificateTemplate: [''],
            barcode: [''],
            tags: [''],
            productType: ['PHYSICAL'],
            isPerishable: [false],
            weightKg: [null],
            taxCode: ['']
        });

        this.partForm.get('hasWarranty')?.valueChanges.subscribe(hasWarranty => {
            const periodCtrl = this.partForm.get('warrantyPeriodMonths');
            const typeCtrl = this.partForm.get('warrantyType');
            if (hasWarranty) {
                periodCtrl?.setValidators([Validators.required, Validators.min(1)]);
                typeCtrl?.setValidators([Validators.required]);
            } else {
                periodCtrl?.clearValidators();
                typeCtrl?.clearValidators();
            }
            periodCtrl?.updateValueAndValidity();
            typeCtrl?.updateValueAndValidity();
        });

        // Auto-generate slug from part name
        this.partForm.get('name')?.valueChanges.subscribe(name => {
            if (!this.isEditMode) {
                this.autoUpdateSlug(name);
            }
        });

        // Auto-update slug when category changes (for context)
        this.partForm.get('categoryId')?.valueChanges.subscribe(() => {
            if (!this.isEditMode) {
                const name = this.partForm.get('name')?.value;
                if (name) this.autoUpdateSlug(name);
            }
        });
    }

    private initializeCatalogEntryForm(): void {
        this.catalogEntryForm = this.formBuilder.group({
            slug: ['', [Validators.maxLength(200), Validators.pattern(/^[a-z0-9-]*$/)]],
            shortDescription: ['', [Validators.maxLength(300)]],
            isPublished: [true],
            isFeatured: [false],
            featuredRank: [0, [Validators.min(0)]],
            metaTitle: ['', [Validators.maxLength(70)]],
            metaDescription: ['', [Validators.maxLength(160)]]
        });
    }

    private autoUpdateSlug(name: string): void {
        if (!name) return;
        const slug = name.trim()
            .toLowerCase()
            .replace(/[^a-z0-9\s-]/g, '')
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .replace(/^-|-$/g, '');
        this.catalogEntryForm.patchValue({ slug }, { emitEvent: false });
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
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories' })
        });
    }

    private loadUnits(): void {
        this.unitService.getAllUnits().subscribe({
            next: (response) => {
                this.units = response;
                this.baseUnits = response;
                this.filteredUnits = response;
                this.filteredBaseUnits = response;
                this.syncSelectedLookups();
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load units' })
        });
    }

    private loadBrands(): void {
        this.brandService.getActiveBrands().subscribe({
            next: (response) => {
                this.brands = response;
                this.filteredBrands = response;
                this.syncSelectedLookups();
            },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load brands' })
        });
    }

    private loadVehicles(): void {
        this.vehicleService.getActiveVehicles().subscribe({
            next: (response) => { this.vehicles = response; this.filteredVehicles = response; },
            error: () => this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load vehicles' })
        });
    }

    private loadCompatibleVehicles(): void {
        if (!this.partId) return;
        this.loadingCompatibilities = true;
        this.partService.getPartCompatibleVehicles(this.partId).subscribe({
            next: (vehicles) => { this.compatibleVehicles = vehicles; this.loadingCompatibilities = false; },
            error: () => { this.loadingCompatibilities = false; }
        });
    }

    // ── Autocomplete handlers ──────────────────────────────────────────────

    onCategorySearch(event: any): void {
        const q = (event.query || '').toLowerCase();
        this.filteredCategories = this.categories.filter(c =>
            c.name.toLowerCase().includes(q));
    }

    onUnitSearch(event: any): void {
        const q = (event.query || '').toLowerCase();
        this.filteredUnits = this.units.filter(u =>
            u.name.toLowerCase().includes(q) || u.symbol.toLowerCase().includes(q));
    }

    onBaseUnitSearch(event: any): void {
        const q = (event.query || '').toLowerCase();
        this.filteredBaseUnits = this.baseUnits.filter(u =>
            u.name.toLowerCase().includes(q) || u.symbol.toLowerCase().includes(q));
    }

    onBrandSearch(event: any): void {
        const q = (event.query || '').toLowerCase();
        this.filteredBrands = this.brands.filter(b =>
            b.name.toLowerCase().includes(q));
    }

    onVehicleSearch(event: any): void {
        const q = (event.query || '').toLowerCase();
        this.filteredVehicles = this.vehicles.filter(v =>
            `${v.make} ${v.model}`.toLowerCase().includes(q) ||
            String(v.year).includes(q) ||
            v.engineType.toLowerCase().includes(q));
    }

    onCategorySelect(event: any): void {
        this.selectedCategory = event.value as CategoryResponse;
        this.partForm.patchValue({ categoryId: this.selectedCategory.id });
    }

    onUnitSelect(event: any): void {
        this.selectedUnit = event.value as UnitResponse;
        this.partForm.patchValue({ unitId: this.selectedUnit.id });
    }

    onBaseUnitSelect(event: any): void {
        this.selectedBaseUnit = event.value as UnitResponse;
        this.partForm.patchValue({ baseUnitId: this.selectedBaseUnit.id });
        if (!this.partForm.value.unitId) {
            this.partForm.patchValue({ unitId: this.selectedBaseUnit.id });
            this.selectedUnit = this.selectedBaseUnit;
        }
    }

    onBrandSelect(event: any): void {
        this.selectedBrand = event.value as BrandResponse;
        this.partForm.patchValue({ brandId: this.selectedBrand.id });
    }

    onVehicleSelect(event: any): void {
        this.selectedVehicle = event.value as VehicleResponse;
        this.compatibilityForm.patchValue({ vehicle: this.selectedVehicle });
    }

    onCategoryClear(): void { this.selectedCategory = null; this.partForm.patchValue({ categoryId: null }); }
    onUnitClear(): void { this.selectedUnit = null; this.partForm.patchValue({ unitId: null }); }
    onBaseUnitClear(): void {
        this.selectedBaseUnit = null;
        this.selectedUnit = null;
        this.partForm.patchValue({ baseUnitId: null, unitId: null });
    }
    onBrandClear(): void { this.selectedBrand = null; this.partForm.patchValue({ brandId: null }); }
    onVehicleClear(): void { this.selectedVehicle = null; this.compatibilityForm.patchValue({ vehicle: null }); }

    // ── Vehicle Compatibility ──────────────────────────────────────────────

    addCompatibility(): void {
        if (this.isViewMode) return;

        if (this.compatibilityForm.invalid) {
            Object.keys(this.compatibilityForm.controls).forEach(k =>
                this.compatibilityForm.get(k)?.markAsTouched());
            this.messageService.add({ severity: 'warn', summary: 'Validation', detail: 'Please select a vehicle' });
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
                    this.messageService.add({ severity: 'success', summary: 'Added', detail: `${vehicle.make} ${vehicle.model} added` });
                    this.resetCompatibilityForm();
                    this.loadCompatibleVehicles();
                    this.isCompatibilitySubmitting = false;
                },
                error: (error) => {
                    this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to add compatibility' });
                    this.isCompatibilitySubmitting = false;
                }
            });
            return;
        }

        if (this.pendingCompatibilities.some(p => p.vehicle.id === vehicle.id)) {
            this.messageService.add({ severity: 'warn', summary: 'Duplicate', detail: 'Vehicle already added' });
            return;
        }

        this.pendingCompatibilities = [...this.pendingCompatibilities, { vehicle, isCompatible: request.isCompatible, notes: request.notes || '' }];
        this.resetCompatibilityForm();
    }

    removeCompatibility(item: { id?: string; vehicleId: string; isPending?: boolean }): void {
        if (this.isViewMode) return;

        this.confirmationService.confirm({
            message: 'Remove this vehicle compatibility?',
            header: 'Confirm',
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
                        this.messageService.add({ severity: 'success', summary: 'Removed', detail: 'Compatibility removed' });
                        this.loadCompatibleVehicles();
                    },
                    error: (err) => this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to remove' })
                });
            }
        });
    }

    private resetCompatibilityForm(): void {
        this.compatibilityForm.reset({ vehicle: null, isCompatible: true, notes: '' });
        this.selectedVehicle = null;
    }

    getCompatibilityRows(): Array<{
        id?: string; vehicleId: string; vehicleInfo: string;
        isCompatible: boolean; notes: string; isPending?: boolean;
    }> {
        const apiRows = this.compatibleVehicles.map(v => ({
            id: v.id,
            vehicleId: v.vehicleId,
            vehicleInfo: `${v.vehicleMake} ${v.vehicleModel} ${v.vehicleYear} · ${v.vehicleEngineType}`,
            isCompatible: v.isCompatible,
            notes: v.notes || ''
        }));

        const pendingRows = this.pendingCompatibilities.map(p => ({
            vehicleId: p.vehicle.id,
            vehicleInfo: `${p.vehicle.make} ${p.vehicle.model} ${p.vehicle.year} · ${p.vehicle.engineType}`,
            isCompatible: p.isCompatible,
            notes: p.notes || '',
            isPending: true
        }));

        return [...pendingRows, ...apiRows];
    }

    // ── Submit ─────────────────────────────────────────────────────────────

    onSubmit(): void {
        if (this.partForm.invalid) {
            this.partForm.markAllAsTouched();
            this.messageService.add({ severity: 'warn', summary: 'Validation Error', detail: 'Please fill in all required fields' });
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
        const v = this.partForm.value;
        const request: CreatePartRequest = {
            name: v.name.trim(),
            description: v.description || '',
            richDescription: v.richDescription?.trim() || null,
            partNumber: v.partNumber.trim(),
            oemNumber: v.oemNumber?.trim() || null,
            localName: v.localName?.trim() || null,
            barcode: v.barcode?.trim() || null,
            categoryId: v.categoryId,
            brandId: v.brandId || null,
            baseUnitId: v.baseUnitId || null,
            unitId: v.unitId || v.baseUnitId || null,
            costPrice: 0,
            sellingPrice: v.sellingPrice || 0,
            minimumStock: v.minimumStock || 0,
            tags: v.tags?.trim() || null,
            productType: v.productType || 'PHYSICAL',
            isPerishable: v.isPerishable || false,
            weightKg: v.weightKg ?? null,
            taxCode: v.taxCode?.trim() || null,
            hasWarranty: v.hasWarranty || false,
            warrantyPeriodMonths: v.hasWarranty ? v.warrantyPeriodMonths : null,
            warrantyType: v.hasWarranty ? v.warrantyType : null,
            warrantyTerms: v.hasWarranty ? v.warrantyTerms : null,
            warrantyCertificateTemplate: v.hasWarranty ? v.warrantyCertificateTemplate : null
        };

        this.partService.createPart(request).subscribe({
            next: (response) => {
                const finalize = () => {
                    this.messageService.add({ severity: 'success', summary: 'Created', detail: `'${response.name}' created successfully` });
                    this.isSubmitting = false;
                    this.router.navigate(['/inventory/parts']);
                };

                const saveCatalogEntry$ = this.buildCatalogEntrySave(response.id);
                const saveCompatibilities$ = this.pendingCompatibilities.length > 0
                    ? this.savePendingCompatibilities(response.id)
                    : of(void 0);

                forkJoin([saveCatalogEntry$, saveCompatibilities$]).subscribe({
                    next: () => finalize(),
                    error: () => finalize()
                });
            },
            error: (error) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to create part' });
                this.isSubmitting = false;
            }
        });
    }

    private updatePart(): void {
        const v = this.partForm.value;
        const request: UpdatePartRequest = {
            id: this.partId!,
            name: v.name.trim(),
            description: v.description || '',
            richDescription: v.richDescription?.trim() || null,
            oemNumber: v.oemNumber?.trim() || null,
            localName: v.localName?.trim() || null,
            barcode: v.barcode?.trim() || null,
            categoryId: v.categoryId,
            brandId: v.brandId || null,
            baseUnitId: v.baseUnitId || null,
            unitId: v.unitId || v.baseUnitId || null,
            costPrice: this.existingCostPrice,
            sellingPrice: v.sellingPrice || 0,
            minimumStock: v.minimumStock || 0,
            isActive: v.isActive,
            tags: v.tags?.trim() || null,
            productType: v.productType || 'PHYSICAL',
            isPerishable: v.isPerishable || false,
            weightKg: v.weightKg ?? null,
            taxCode: v.taxCode?.trim() || null,
            hasWarranty: v.hasWarranty || false,
            warrantyPeriodMonths: v.hasWarranty ? v.warrantyPeriodMonths : null,
            warrantyType: v.hasWarranty ? v.warrantyType : null,
            warrantyTerms: v.hasWarranty ? v.warrantyTerms : null,
            warrantyCertificateTemplate: v.hasWarranty ? v.warrantyCertificateTemplate : null
        };

        this.partService.updatePart(this.partId!, request).subscribe({
            next: (response) => {
                this.buildCatalogEntrySave(response.id).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `'${response.name}' updated successfully` });
                        this.isSubmitting = false;
                        this.router.navigate(['/inventory/parts']);
                    },
                    error: () => {
                        this.messageService.add({ severity: 'success', summary: 'Updated', detail: `'${response.name}' updated (online listing save failed)` });
                        this.isSubmitting = false;
                        this.router.navigate(['/inventory/parts']);
                    }
                });
            },
            error: (error) => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: error?.error?.message || 'Failed to update part' });
                this.isSubmitting = false;
            }
        });
    }

    private buildCatalogEntrySave(partId: string) {
        const cv = this.catalogEntryForm.value;
        const slugValue = (cv.slug || '').trim();
        if (!slugValue) return of(null);

        const req: UpsertCatalogEntryRequest = {
            slug: slugValue,
            shortDescription: cv.shortDescription?.trim() || '',
            isPublished: cv.isPublished ?? true,
            isFeatured: cv.isFeatured ?? false,
            featuredRank: cv.featuredRank ?? 0,
            metaTitle: cv.metaTitle?.trim() || null,
            metaDescription: cv.metaDescription?.trim() || null
        };

        return this.catalogEntryService.upsert(partId, req).pipe(catchError(() => of(null)));
    }

    private savePendingCompatibilities(partId: string) {
        const requests = this.pendingCompatibilities.map(item =>
            this.vehicleService.addPartCompatibility(item.vehicle.id, partId, {
                isCompatible: item.isCompatible,
                notes: item.notes || ''
            }).pipe(catchError(() => of(null)))
        );
        return forkJoin(requests).pipe(map(() => void 0));
    }

    onCancel(): void { this.router.navigate(['/inventory/parts']); }

    // ── Helpers ────────────────────────────────────────────────────────────

    hasError(fieldName: string): boolean {
        const ctrl = this.partForm.get(fieldName);
        return ctrl ? ctrl.invalid && ctrl.touched : false;
    }

    hasCatalogError(fieldName: string): boolean {
        const ctrl = this.catalogEntryForm.get(fieldName);
        return ctrl ? ctrl.invalid && ctrl.touched : false;
    }

    getErrorMessage(fieldName: string): string {
        const ctrl = this.partForm.get(fieldName);
        if (ctrl?.hasError('required')) return `${this.getFieldLabel(fieldName)} is required`;
        if (ctrl?.hasError('minlength')) return `${this.getFieldLabel(fieldName)} must be at least ${ctrl.getError('minlength')?.requiredLength} chars`;
        if (ctrl?.hasError('maxlength')) return `${this.getFieldLabel(fieldName)} must not exceed ${ctrl.getError('maxlength')?.requiredLength} chars`;
        if (ctrl?.hasError('min')) return `${this.getFieldLabel(fieldName)} must be at least ${ctrl.getError('min')?.min}`;
        if (ctrl?.hasError('pattern') && fieldName === 'partNumber') return 'Part Number must start with a letter';
        return 'Invalid value';
    }

    private getFieldLabel(fieldName: string): string {
        const labels: Record<string, string> = {
            name: 'Part Name', partNumber: 'Part Number', oemNumber: 'OEM Number',
            categoryId: 'Category',
            minimumStock: 'Minimum Stock', warrantyPeriodMonths: 'Warranty Period',
            warrantyType: 'Warranty Type'
        };
        return labels[fieldName] || fieldName;
    }

    get pageTitle(): string {
        if (this.isViewMode) return 'View Part';
        if (this.isEditMode) return 'Edit Part';
        return 'Create New Part';
    }

    getCompatibilitySeverity(isCompatible: boolean): 'success' | 'warn' {
        return isCompatible ? 'success' : 'warn';
    }

    getCompatibilityLabel(isCompatible: boolean): string {
        return isCompatible ? 'Compatible' : 'Not Compatible';
    }

    get slugCharCount(): number {
        return (this.catalogEntryForm.get('slug')?.value || '').length;
    }

    get metaTitleCharCount(): number {
        return (this.catalogEntryForm.get('metaTitle')?.value || '').length;
    }

    get metaDescCharCount(): number {
        return (this.catalogEntryForm.get('metaDescription')?.value || '').length;
    }

    private syncSelectedLookups(): void {
        const { categoryId, unitId, brandId } = this.partForm?.value || {};
        if (categoryId && !this.selectedCategory)
            this.selectedCategory = this.categories.find(c => c.id === categoryId) || null;
        if (unitId && !this.selectedUnit)
            this.selectedUnit = this.units.find(u => u.id === unitId) || null;
        if (brandId && !this.selectedBrand)
            this.selectedBrand = this.brands.find(b => b.id === brandId) || null;
    }
}
