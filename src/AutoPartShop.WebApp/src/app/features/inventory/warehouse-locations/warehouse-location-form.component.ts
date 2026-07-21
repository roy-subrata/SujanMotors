import { Component, OnInit, inject } from '@angular/core';
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

/** Create/Edit form for a Warehouse Location (Zone-Aisle-Rack-Bin). Routed page, mirrors WarehouseFormComponent. */
@Component({
    selector: 'app-warehouse-location-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, ButtonModule, CardModule, InputTextModule, TextareaModule, Select, ToastModule],
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

    form: FormGroup;
    isEditMode = false;
    isSubmitting = false;
    locationId: string | null = null;
    pageTitle = 'Create Warehouse Location';

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
                this.pageTitle = 'Edit Warehouse Location';

                if (this.locationId) {
                    this.loadLocation(this.locationId);
                }
            }
        });
    }

    private loadPickerOptions(): void {
        this.warehouseService.getAllWarehouses().subscribe({
            next: (warehouses: WarehouseResponse[]) => {
                this.warehouseOptions = warehouses.map((w) => ({ label: `${w.name} (${w.code})`, value: w.id }));
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load warehouses' });
            }
        });

        this.categoryService.getAllCategories().subscribe({
            next: (categories: CategoryResponse[]) => {
                this.categoryOptions = categories.map((c) => ({ label: c.breadcrumbPath || c.name, value: c.id }));
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load categories' });
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
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load location details' });
            }
        });
    }

    onSubmit(): void {
        if (!this.form.valid) {
            this.form.markAllAsTouched();
            this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Please fill all required fields' });
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
                    summary: 'Success',
                    detail: `Location "${location.locationCode}" ${this.isEditMode ? 'updated' : 'created'} successfully`
                });
                this.router.navigate(['/inventory/warehouse-locations']);
            },
            error: (err) => {
                const isConflict = err?.status === 409;
                const detail = err?.error?.message
                    ?? (isConflict
                        ? 'A location with this Zone/Aisle/Rack/Bin already exists in this warehouse'
                        : `Failed to ${this.isEditMode ? 'update' : 'create'} location`);
                this.messageService.add({ severity: 'error', summary: isConflict ? 'Conflict' : 'Error', detail });
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
            return `${this.formatFieldName(fieldName)} is required`;
        }
        if (field?.hasError('maxlength')) {
            const maxLength = field.errors?.['maxlength'].requiredLength;
            return `${this.formatFieldName(fieldName)} cannot exceed ${maxLength} characters`;
        }
        return '';
    }

    private formatFieldName(fieldName: string): string {
        return fieldName.charAt(0).toUpperCase() + fieldName.slice(1).replace(/([A-Z])/g, ' $1');
    }
}
