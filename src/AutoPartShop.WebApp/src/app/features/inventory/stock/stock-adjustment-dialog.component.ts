import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { StockService, StockLevelResponse } from '../services/stock.service';
import { PartService, PartResponse } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';

@Component({
  selector: 'app-stock-adjustment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    CardModule,
    ToastModule
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="adjustment-dialog">
      <h2 class="dialog-title">{{ mode === 'create' ? 'New Stock Entry' : 'Stock Adjustment' }}</h2>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="adjustment-form">
        <!-- Create mode: pick product (incl. variant) and warehouse -->
        <ng-container *ngIf="mode === 'create'">
          <div class="form-row">
            <div class="form-group">
              <label for="product">Product / Variant *</label>
              <p-select
                id="product"
                [options]="productOptions"
                optionLabel="label"
                optionValue="value"
                formControlName="productKey"
                [filter]="true"
                filterBy="label"
                placeholder="Select a product or variant"
                [showClear]="true">
              </p-select>
              <small class="text-danger" *ngIf="form.get('productKey')?.invalid && form.get('productKey')?.touched">
                Product is required
              </small>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label for="warehouse">Warehouse *</label>
              <p-select
                id="warehouse"
                [options]="warehouseOptions"
                optionLabel="label"
                optionValue="value"
                formControlName="warehouseId"
                placeholder="Select a warehouse"
                [showClear]="true">
              </p-select>
              <small class="text-danger" *ngIf="form.get('warehouseId')?.invalid && form.get('warehouseId')?.touched">
                Warehouse is required
              </small>
            </div>
          </div>
        </ng-container>

        <!-- Current Stock Info (adjust mode) -->
        <p-card class="info-card" *ngIf="mode === 'adjust'">
          <div class="info-row">
            <div class="info-item">
              <label>Part</label>
              <span class="value">
                {{ part?.name }} ({{ currentStock?.variantSku || part?.sku }})
                <span class="variant-tag" *ngIf="currentStock?.variantName">{{ currentStock?.variantName }}</span>
              </span>
            </div>
            <div class="info-item">
              <label>Warehouse</label>
              <span class="value">{{ warehouse?.name }}</span>
            </div>
          </div>
          <div class="info-row">
            <div class="info-item">
              <label>On Hand</label>
              <span class="value stock-value">{{ currentStock?.quantity }} {{ currentStock?.unitSymbol || 'units' }}</span>
            </div>
            <div class="info-item">
              <label>Available</label>
              <span class="value">{{ currentStock?.availableQuantity }} {{ currentStock?.unitSymbol || 'units' }}</span>
            </div>
          </div>
        </p-card>

        <!-- Adjustment Form -->
        <div class="form-row">
          <div class="form-group">
            <label for="type">Adjustment Type *</label>
            <p-select
              id="type"
              [options]="adjustmentTypes"
              optionLabel="label"
              optionValue="value"
              formControlName="type"
              placeholder="Select adjustment type"
              [showClear]="true">
            </p-select>
            <small class="text-danger" *ngIf="form.get('type')?.invalid && form.get('type')?.touched">
              Adjustment type is required
            </small>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="quantity">Quantity *</label>
            <p-inputNumber
              id="quantity"
              formControlName="quantity"
              [min]="1"
              [max]="getMaxQuantity()"
              placeholder="Enter quantity"
              class="w-full">
            </p-inputNumber>
            <small class="text-muted" *ngIf="isDecreaseType()">
              Max: {{ currentStock?.availableQuantity }} {{ currentStock?.unitSymbol || 'units' }} (available)
            </small>
            <small class="text-danger" *ngIf="form.get('quantity')?.invalid && form.get('quantity')?.touched">
              Valid quantity is required
            </small>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full">
            <label for="notes">Notes</label>
            <textarea
              id="notes"
              formControlName="notes"
              placeholder="Enter reason for adjustment (e.g., Damage, Shrinkage, Count Correction)"
              rows="3"
              class="w-full textarea-input">
            </textarea>
          </div>
        </div>

        <!-- New Stock Preview -->
        <p-card class="preview-card" *ngIf="mode === 'adjust' && form.get('quantity')?.value > 0 && form.get('type')?.value">
          <div class="preview-row">
            <label>New On-Hand After Adjustment:</label>
            <span class="preview-value" [ngClass]="getNewStock() < 0 ? 'text-danger' : 'text-success'">
              {{ getNewStock() }} {{ currentStock?.unitSymbol || 'units' }}
            </span>
          </div>
        </p-card>

        <!-- Action Buttons -->
        <div class="button-group">
          <button
            pButton
            type="button"
            label="Cancel"
            icon="pi pi-times"
            class="p-button-outlined"
            (click)="onCancel()"
            [disabled]="isSubmitting">
          </button>
          <button
            pButton
            type="submit"
            label="Apply Adjustment"
            icon="pi pi-check"
            class="p-button-success"
            [loading]="isSubmitting"
            [disabled]="!form.valid || isSubmitting">
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .adjustment-dialog {
      padding: 1.25rem;
      width: 100%;
      min-width: 0;
    }

    .dialog-title {
      margin-bottom: 1.5rem;
      font-size: 1.25rem;
      font-weight: 600;
    }

    .info-card, .preview-card {
      margin-bottom: 1.5rem;
    }

    .info-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1.5rem;
      margin-bottom: 1rem;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .info-item label {
      font-size: 0.875rem;
      color: var(--text-color-secondary);
      font-weight: 500;
    }

    .info-item .value {
      font-size: 1rem;
      font-weight: 600;
      color: var(--text-color);
    }

    .stock-value {
      color: #059669;
      font-size: 1.1rem;
    }

    .variant-tag {
      display: inline-block;
      margin-left: 0.4rem;
      padding: 0.05rem 0.45rem;
      font-size: 0.7rem;
      font-weight: 600;
      color: #2563eb;
      background: #eff6ff;
      border: 1px solid #bfdbfe;
      border-radius: 999px;
      vertical-align: middle;
      white-space: nowrap;
    }

    .form-row {
      margin-bottom: 1rem;
    }

    .form-group {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .form-group.full {
      grid-column: 1 / -1;
    }

    .form-group label {
      font-weight: 500;
      font-size: 0.875rem;
    }

    .textarea-input {
      border: 1px solid var(--surface-border);
      border-radius: 4px;
      padding: 0.5rem;
      font-family: inherit;
      resize: vertical;
      background: var(--surface-card);
      color: var(--text-color);
    }

    .preview-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      font-size: 1rem;
    }

    .preview-row label {
      font-weight: 600;
      color: var(--text-color);
    }

    .preview-value {
      font-size: 1.25rem;
      font-weight: 700;
    }

    .text-danger {
      color: #dc2626;
      font-size: 0.75rem;
    }

    .text-success {
      color: #059669;
    }

    .button-group {
      display: flex;
      gap: 1rem;
      margin-top: 1.5rem;
      justify-content: flex-end;
      flex-wrap: wrap;
    }

    .w-full {
      width: 100%;
    }

    @media (max-width: 640px) {
      .adjustment-dialog {
        padding: 1rem;
      }

      .dialog-title {
        font-size: 1.1rem;
      }

      .info-row {
        grid-template-columns: 1fr;
        gap: 0.75rem;
      }

      .button-group {
        flex-direction: column;
        align-items: stretch;
      }
    }
  `]
})
export class StockAdjustmentDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  isSubmitting = false;
  mode: 'adjust' | 'create' = 'adjust';
  part: PartResponse | null = null;
  warehouse: WarehouseResponse | null = null;
  currentStock: StockLevelResponse | null = null;

  // Create-mode option lists
  productOptions: { label: string; value: string; partId: string; variantId: string | null; unitId: string | null }[] = [];
  warehouseOptions: { label: string; value: string }[] = [];

  adjustmentTypes = [
    { label: 'Increase Stock (Found)', value: 'FOUND' },
    { label: 'Decrease Stock (Damaged)', value: 'DAMAGED' },
    { label: 'Decrease Stock (Expired)', value: 'EXPIRED' },
    { label: 'Decrease Stock (Lost)', value: 'LOST' },
    { label: 'Count Correction (Increase)', value: 'COUNT_CORRECTION_UP' },
    { label: 'Count Correction (Decrease)', value: 'COUNT_CORRECTION_DOWN' }
  ];

  constructor() {
    this.form = this.fb.group({
      productKey: [''],
      warehouseId: [''],
      type: ['', Validators.required],
      quantity: [0, [Validators.required, Validators.min(1)]],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.mode = this.config.data?.mode === 'create' ? 'create' : 'adjust';

    if (this.mode === 'create') {
      this.form.get('productKey')?.addValidators(Validators.required);
      this.form.get('warehouseId')?.addValidators(Validators.required);
      this.loadProducts();
      this.loadWarehouseOptions();
    } else {
      this.currentStock = this.config.data.stock;
      this.loadPartAndWarehouse();
    }
  }

  private loadProducts(): void {
    this.partService.getParts({ search: '', pageNumber: 1, pageSize: 500, isActive: true, flattenVariants: true }).subscribe({
      next: (res) => {
        const parts = res.data ?? [];
        this.productOptions = parts
          // Skip parent rows that have variants (the variant rows are listed separately)
          .filter(p => !(p.hasVariants && !p.isVariant))
          .map(p => {
            const variantId = p.isVariant ? (p.variantId ?? null) : null;
            const label = p.isVariant
              ? `${p.name} — ${p.variantName} (${p.variantSKU || p.sku})`
              : `${p.name} (${p.sku})`;
            return {
              label,
              value: variantId ? `${p.id}::${variantId}` : p.id,
              partId: p.id,
              variantId,
              unitId: p.unitId ?? null
            };
          });
      },
      error: (_error) => {
        console.error('Error loading products:', _error);
      }
    });
  }

  private loadWarehouseOptions(): void {
    this.warehouseService.getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] }).subscribe({
      next: (res) => {
        const warehouses = res.data ?? [];
        this.warehouseOptions = (Array.isArray(warehouses) ? warehouses : []).map(w => ({ label: w.name, value: w.id }));
      },
      error: (_error) => {
        console.error('Error loading warehouses:', _error);
      }
    });
  }

  private loadPartAndWarehouse(): void {
    if (!this.currentStock) return;

    // Load part
    this.partService.getPartById(this.currentStock.partId).subscribe({
      next: (part) => {
        this.part = part;
      },
      error: (_error) => {
        console.error('Error loading part:', _error);
      }
    });

    // Load warehouse
    this.warehouseService.getWarehouseById(this.currentStock.warehouseId).subscribe({
      next: (warehouse) => {
        this.warehouse = warehouse;
      },
      error: (_error) => {
        console.error('Error loading warehouse:', _error);
      }
    });
  }

  isDecreaseType(): boolean {
    const type = this.form.get('type')?.value;
    return type && type !== 'FOUND' && type !== 'COUNT_CORRECTION_UP';
  }

  getMaxQuantity(): number {
    if (!this.currentStock) return 999999;
    return this.isDecreaseType()
      ? (this.currentStock.availableQuantity ?? 999999)
      : 999999;
  }

  getNewStock(): number {
    if (!this.currentStock) return 0;
    const type = this.form.get('type')?.value;
    const quantity = this.form.get('quantity')?.value || 0;
    if (type === 'FOUND' || type === 'COUNT_CORRECTION_UP') {
      return this.currentStock.quantity + quantity;
    }
    return this.currentStock.quantity - quantity;
  }

  onSubmit(): void {
    if (!this.form.valid) {
      this.form.markAllAsTouched();
      return;
    }

    const type = this.form.get('type')?.value;
    const quantity = this.form.get('quantity')?.value;

    // Determine if this is a positive or negative adjustment
    const isIncrease = type === 'FOUND' || type === 'COUNT_CORRECTION_UP';
    const adjustmentQuantity = isIncrease ? quantity : -quantity;

    // Resolve target part/variant/warehouse/unit from the active mode
    let partId: string;
    let variantId: string | null;
    let warehouseId: string;
    let unitId: string | undefined;

    if (this.mode === 'create') {
      const selected = this.productOptions.find(p => p.value === this.form.get('productKey')?.value);
      if (!selected) return;
      partId = selected.partId;
      variantId = selected.variantId;
      warehouseId = this.form.get('warehouseId')?.value;
      unitId = selected.unitId || undefined;
    } else {
      if (!this.currentStock) return;
      partId = this.currentStock.partId;
      variantId = this.currentStock.variantId ?? null;
      warehouseId = this.currentStock.warehouseId;
      unitId = this.currentStock.unitId || undefined;
    }

    this.isSubmitting = true;
    const request = {
      partId,
      variantId: variantId ?? undefined,
      warehouseId,
      quantity: adjustmentQuantity,
      quantityInBaseUnit: undefined,  // Backend will calculate if not provided
      unitId,
      reason: type,
      reference: '',
      notes: this.form.get('notes')?.value || ''
    };

    this.stockService.adjustStock(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Stock adjustment recorded successfully'
        });
        this.isSubmitting = false;
        setTimeout(() => {
          this.dialogRef.close({ success: true });
        }, 500);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to record stock adjustment'
        });
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
