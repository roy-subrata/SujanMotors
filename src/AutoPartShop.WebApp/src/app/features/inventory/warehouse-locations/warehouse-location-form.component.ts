import { Component, OnInit, inject, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

import {
    WarehouseLocationService,
    WarehouseLocationResponse,
    CreateWarehouseLocationRequest
} from '../services/warehouse-location.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { CategoryService, CategoryResponse } from '../services/category.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

/** Create/Edit form for a Warehouse Location (Zone-Aisle-Rack-Bin). Routed page, mirrors WarehouseFormComponent. */
@Component({
    selector: 'app-warehouse-location-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, CardModule, InputTextModule, TextareaModule, Select, ToastModule, TranslatePipe],
    providers: [MessageService],
    templateUrl: './warehouse-location-form.component.html',
    styleUrls: ['./warehouse-location-form.component.css']
})
export class WarehouseLocationFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly locationService = inject(WarehouseLocationService);
    private readonly warehouseService = inject(WarehouseService);
    private readonly categoryService = inject(CategoryService);
    private readonly messageService = inject(MessageService);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly i18n = inject(I18nService);
    private readonly destroyRef = inject(DestroyRef);

    form: FormGroup;
    isEditMode = false;
    isSubmitting = false;
    locationId: string | null = null;
    pageTitle = this.i18n.t('warehouseLocations.createTitle');

    warehouseOptions: { label: string; value: string }[] = [];
    categoryOptions: { label: string; value: string }[] = [];

    constructor() {
        this.form = this.createForm();
    }

    ngOnInit(): void {
        this.loadPickerOptions();

        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.locationId = params['id'];
                this.isEditMode = this.router.url.includes('/edit');
                this.pageTitle = this.i18n.t('warehouseLocations.editTitle');

                if (this.locationId) {
                    this.loadLocation(this.locationId);
                }
            }
        });

        this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
            this.pageTitle = this.i18n.t(this.isEditMode ? 'warehouseLocations.editTitle' : 'warehouseLocations.createTitle');
        });
    }

    private loadPickerOptions(): void {
        this.warehouseService.getAllWarehouses().subscribe({
            next: (warehouses: WarehouseResponse[]) => {
                this.warehouseOptions = warehouses.map((w) => ({ label: `${w.name} (${w.code})`, value: w.id }));
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('warehouseLocations.messages.loadWarehousesFailed') });
            }
        });

        this.categoryService.getAllCategories().subscribe({
            next: (categories: CategoryResponse[]) => {
                this.categoryOptions = categories.map((c) => ({ label: c.breadcrumbPath || c.name, value: c.id }));
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('warehouseLocations.messages.loadCategoriesFailed') });
            }
        });
    }

    private createForm(): FormGroup {
        return this.fb.group({
            warehouseId: [null, [Validators.required]],
            zone: ['', [Validators.required, Validators.maxLength(10)]],
            aisle: ['', [Validators.required, Validators.maxLength(10)]],
            rack: ['', [Validators.required, Validators.maxLength(10)]],
            bin: ['', [Validators.required, Validators.maxLength(10)]],
            categoryId: [null],
            notes: ['']
        });
    }

    private loadLocation(id: string): void {
        this.locationService.getById(id).subscribe({
            next: (location: WarehouseLocationResponse) => {
                this.form.patchValue({
                    warehouseId: location.warehouseId,
                    zone: location.zone,
                    aisle: location.aisle,
                    rack: location.rack,
                    bin: location.bin,
                    categoryId: location.categoryId,
                    notes: location.notes
                });
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('warehouseLocations.messages.loadLocationFailed') });
            }
        });
    }

    onSubmit(): void {
        if (!this.form.valid) {
            this.form.markAllAsTouched();
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('warehouseLocations.messages.requiredFieldsError') });
            return;
        }

        this.isSubmitting = true;
        const raw = this.form.getRawValue();
        const request: CreateWarehouseLocationRequest = {
            warehouseId: raw.warehouseId,
            zone: (raw.zone ?? '').trim(),
            aisle: (raw.aisle ?? '').trim(),
            rack: (raw.rack ?? '').trim(),
            bin: (raw.bin ?? '').trim(),
            categoryId: raw.categoryId || null,
            notes: raw.notes?.trim() || null
        };

        const action$ = this.isEditMode && this.locationId
            ? this.locationService.update(this.locationId, request)
            : this.locationService.create(request);

        action$.subscribe({
            next: (location) => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t(this.isEditMode ? 'warehouseLocations.messages.updateSuccess' : 'warehouseLocations.messages.createSuccess', { code: location.locationCode })
                });
                this.router.navigate(['/inventory/warehouse-locations']);
            },
            error: (err) => {
                const isConflict = err?.status === 409;
                const detail = err?.error?.message
                    ?? (isConflict
                        ? this.i18n.t('warehouseLocations.messages.conflictError')
                        : this.i18n.t(this.isEditMode ? 'warehouseLocations.messages.updateFailed' : 'warehouseLocations.messages.createFailed'));
                this.messageService.add({ severity: 'error', summary: isConflict ? this.i18n.t('common.messages.conflict') : this.i18n.t('common.messages.error'), detail });
                this.isSubmitting = false;
            }
        });
    }

    goBack(): void {
        this.router.navigate(['/inventory/warehouse-locations']);
    }

    hasError(fieldName: string): boolean {
        const field = this.form.get(fieldName);
        return !!(field && field.invalid && (field.touched || field.dirty));
    }

    getErrorMessage(fieldName: string): string {
        const field = this.form.get(fieldName);
        if (field?.hasError('required')) {
            return this.i18n.t('common.messages.fieldRequired', { field: this.formatFieldName(fieldName) });
        }
        if (field?.hasError('maxlength')) {
            const maxLength = field.errors?.['maxlength'].requiredLength;
            return this.i18n.t('common.messages.fieldMaxLength', { field: this.formatFieldName(fieldName), max: String(maxLength) });
        }
        return '';
    }

    private formatFieldName(fieldName: string): string {
        return fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
    }
}
