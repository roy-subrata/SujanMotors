import { Component, inject, OnInit, DestroyRef, ViewChild } from '@angular/core';
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
import { ProductVariantManagerComponent } from '../product-variant-manager/product-variant-manager.component';
import { ProductAttributeValuesManagerComponent } from '../product-attribute-values-manager/product-attribute-values-manager.component';

import { forkJoin, of, tap } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

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
        ProductAttributeValuesManagerComponent,
        RouterModule,
        CardModule,
        ToastModule,
        TableModule,
        TagModule,
        ConfirmDialogModule,
        TranslatePipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './part-form.component.html',
    styleUrls: ['./part-form.component.css']
})
export class PartFormComponent implements OnInit {
    /** Read right after createPart() succeeds, to save any attribute values entered before the
     *  product existed — mirrors pendingCompatibilities, but the values live in the child form. */
    @ViewChild(ProductAttributeValuesManagerComponent) attributeValuesManager?: ProductAttributeValuesManagerComponent;

    private readonly partService = inject(PartService);
    private readonly categoryService = inject(CategoryService);
    private readonly unitService = inject(UnitService);
    private readonly brandService = inject(BrandService);
    private readonly vehicleService = inject(VehicleService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly formBuilder = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    partForm!: FormGroup;
    compatibilityForm!: FormGroup;

    isEditMode = false;
    isViewMode = false;
    partId: string | null = null;
    /** Backend still has a CostPrice column (see Cost Model design), but this form has no UI for it — preserve the existing value on update instead of clobbering it. */
    private existingCostPrice = 0;
    /** Full product loaded for edit/view mode — passed down to child managers (e.g. attribute values) instead of re-fetching. */
    loadedPart: PartResponse | null = null;
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

    warrantyTypes: { label: string; value: string }[] = [];
    productTypes: { label: string; value: string }[] = [];
    taxCodes: { label: string; value: string }[] = [];

    private buildOptionLists(): void {
        this.warrantyTypes = [
            { label: this.i18n.t('parts.formDialog.warrantyManufacturer'), value: 'MANUFACTURER' },
            { label: this.i18n.t('parts.formDialog.warrantySeller'), value: 'SELLER' },
            { label: this.i18n.t('parts.formDialog.warrantyExtended'), value: 'EXTENDED' }
        ];

        this.productTypes = [
            { label: this.i18n.t('parts.partForm.productTypePhysical'), value: 'PHYSICAL' },
            { label: this.i18n.t('parts.partForm.productTypeDigital'), value: 'DIGITAL' },
            { label: this.i18n.t('parts.partForm.productTypeService'), value: 'SERVICE' }
        ];

        this.taxCodes = [
            { label: this.i18n.t('parts.partForm.taxCodeStandard'), value: 'STANDARD' },
            { label: this.i18n.t('parts.partForm.taxCodeFood'), value: 'FOOD' },
            { label: this.i18n.t('parts.partForm.taxCodeMedicine'), value: 'MEDICINE' },
            { label: this.i18n.t('parts.partForm.taxCodeExempt'), value: 'EXEMPT' }
        ];
    }

    constructor() {
        this.initializeForm();
        this.initializeCompatibilityForm();
    }

    ngOnInit(): void {
        this.buildOptionLists();
        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.buildOptionLists();
        });
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
            }

            if (this.isViewMode) {
                this.partForm.disable();
                this.compatibilityForm.disable();
            }
        });
    }

    private loadPart(id: string): void {
        this.isLoading = true;
        this.partService.getPartById(id).subscribe({
            next: (part) => { this.populateForm(part); this.isLoading = false; },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.partForm.messages.loadPartFailed') });
                this.isLoading = false;
            }
        });
    }

    private populateForm(part: PartResponse): void {
        this.loadedPart = part;
        this.existingCostPrice = part.costPrice ?? 0;
        this.selectedCategory = this.categories.find(c => c.id === part.categoryId) || null;
        this.selectedBaseUnit = this.units.find(u => u.id === part.baseUnitId) || null;
        this.selectedUnit = this.units.find(u => u.id === part.unitId) || null;
        this.selectedBrand = this.brands.find(b => b.id === part.brandId) || null;

        this.partForm.patchValue({
            name: part.name,
            description: part.description,
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
            // Optional — some brands don't publish a catalog part number; SKU identifies the part
            partNumber: ['', [Validators.maxLength(30)]],
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
            error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.formDialog.messages.loadCategoriesFailed') })
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
            error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.formDialog.messages.loadUnitsFailed') })
        });
    }

    private loadBrands(): void {
        this.brandService.getActiveBrands().subscribe({
            next: (response) => {
                this.brands = response;
                this.filteredBrands = response;
                this.syncSelectedLookups();
            },
            error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.formDialog.messages.loadBrandsFailed') })
        });
    }

    private loadVehicles(): void {
        this.vehicleService.getActiveVehicles().subscribe({
            next: (response) => { this.vehicles = response; this.filteredVehicles = response; },
            error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('parts.partForm.messages.loadVehiclesFailed') })
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('parts.partForm.validationSummary'), detail: this.i18n.t('parts.partForm.selectVehicleValidation') });
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
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partDetails.messages.addedSummary'), detail: this.i18n.t('parts.partForm.messages.vehicleAdded', { make: vehicle.make, model: vehicle.model }) });
                    this.resetCompatibilityForm();
                    this.loadCompatibleVehicles();
                    this.isCompatibilitySubmitting = false;
                },
                error: (error) => {
                    this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: error?.error?.message || this.i18n.t('parts.partDetails.messages.addCompatibilityFailed') });
                    this.isCompatibilitySubmitting = false;
                }
            });
            return;
        }

        if (this.pendingCompatibilities.some(p => p.vehicle.id === vehicle.id)) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('parts.partForm.duplicateSummary'), detail: this.i18n.t('parts.partForm.vehicleAlreadyAdded') });
            return;
        }

        this.pendingCompatibilities = [...this.pendingCompatibilities, { vehicle, isCompatible: request.isCompatible, notes: request.notes || '' }];
        this.resetCompatibilityForm();
    }

    removeCompatibility(item: { id?: string; vehicleId: string; isPending?: boolean }): void {
        if (this.isViewMode) return;

        this.confirmationService.confirm({
            message: this.i18n.t('parts.partForm.removeCompatibilityConfirm'),
            header: this.i18n.t('parts.partDetails.confirmHeader'),
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
                        this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partDetails.messages.removedSummary'), detail: this.i18n.t('parts.partDetails.messages.compatibilityRemovedDetail') });
                        this.loadCompatibleVehicles();
                    },
                    error: (err) => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.message || this.i18n.t('parts.partForm.messages.removeFailed') })
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
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('common.messages.validationError'), detail: this.i18n.t('parts.partForm.fillRequiredFields') });
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
            partNumber: v.partNumber?.trim() || null,
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
                const finalize = (attributeValuesFailed: boolean) => {
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partForm.createdSummary'), detail: this.i18n.t('parts.partForm.messages.createSuccess', { name: response.name }) });
                    if (attributeValuesFailed) {
                        this.messageService.add({
                            severity: 'warn',
                            summary: this.i18n.t('common.messages.warning'),
                            detail: this.i18n.t('parts.partForm.messages.attributeValuesSaveFailedAfterCreate'),
                            sticky: true
                        });
                    }
                    this.isSubmitting = false;
                    this.router.navigate(['/inventory/parts']);
                };

                const saveCompatibilities$ = this.pendingCompatibilities.length > 0
                    ? this.savePendingCompatibilities(response.id)
                    : of(void 0);

                const pendingAttributeValues = this.attributeValuesManager?.getPendingRequest() ?? [];
                let attributeValuesFailed = false;
                const saveAttributeValues$ = pendingAttributeValues.length > 0
                    ? this.partService.saveAttributeValues(response.id, pendingAttributeValues).pipe(
                        map(() => void 0),
                        catchError(() => { attributeValuesFailed = true; return of(void 0); }))
                    : of(void 0);

                forkJoin([saveCompatibilities$, saveAttributeValues$]).subscribe({
                    next: () => finalize(attributeValuesFailed),
                    error: () => finalize(attributeValuesFailed)
                });
            },
            error: (error) => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: error?.error?.message || this.i18n.t('parts.formDialog.messages.createFailed') });
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
            partNumber: v.partNumber?.trim() || null,
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('parts.partForm.updatedSummary'), detail: this.i18n.t('parts.partForm.messages.updateSuccess', { name: response.name }) });
                this.isSubmitting = false;
                this.router.navigate(['/inventory/parts']);
            },
            error: (error) => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: error?.error?.message || this.i18n.t('parts.formDialog.messages.updateFailed') });
                this.isSubmitting = false;
            }
        });
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

    getErrorMessage(fieldName: string): string {
        const ctrl = this.partForm.get(fieldName);
        const field = this.getFieldLabel(fieldName);
        if (ctrl?.hasError('required')) return this.i18n.t('common.messages.fieldRequired', { field });
        if (ctrl?.hasError('minlength')) return this.i18n.t('parts.partForm.fieldMinLengthChars', { field, min: String(ctrl.getError('minlength')?.requiredLength) });
        if (ctrl?.hasError('maxlength')) return this.i18n.t('parts.partForm.fieldMaxLengthChars', { field, max: String(ctrl.getError('maxlength')?.requiredLength) });
        if (ctrl?.hasError('min')) return this.i18n.t('common.messages.fieldMinValue', { field, min: String(ctrl.getError('min')?.min) });
        if (ctrl?.hasError('pattern') && fieldName === 'partNumber') return this.i18n.t('parts.partForm.partNumberMustStartWithLetter');
        return this.i18n.t('parts.formDialog.invalidValue');
    }

    private getFieldLabel(fieldName: string): string {
        const labels: Record<string, string> = {
            name: this.i18n.t('parts.formDialog.fieldLabelPartName'), partNumber: this.i18n.t('parts.partNumber'), oemNumber: this.i18n.t('parts.formDialog.fieldLabelOemNumber'),
            categoryId: this.i18n.t('common.labels.category'),
            minimumStock: this.i18n.t('parts.formDialog.fieldLabelMinimumStock'), warrantyPeriodMonths: this.i18n.t('parts.partForm.fieldLabelWarrantyPeriod'),
            warrantyType: this.i18n.t('parts.formDialog.fieldLabelWarrantyType')
        };
        return labels[fieldName] || fieldName;
    }

    get pageTitle(): string {
        if (this.isViewMode) return this.i18n.t('parts.partForm.viewPartTitle');
        if (this.isEditMode) return this.i18n.t('parts.editPart');
        return this.i18n.t('parts.partForm.createNewPartTitle');
    }

    getCompatibilitySeverity(isCompatible: boolean): 'success' | 'warn' {
        return isCompatible ? 'success' : 'warn';
    }

    getCompatibilityLabel(isCompatible: boolean): string {
        return isCompatible ? this.i18n.t('parts.partDetails.compatible') : this.i18n.t('parts.partDetails.notCompatible');
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
