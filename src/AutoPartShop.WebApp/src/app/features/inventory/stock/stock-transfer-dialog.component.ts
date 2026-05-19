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
  selector: 'app-stock-transfer-dialog',
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
    <div class="transfer-dialog">
      <h2 class="dialog-title">Stock Transfer</h2>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="transfer-form">
        <!-- Current Stock Info -->
        <p-card class="info-card">
          <div class="info-row">
            <div class="info-item">
              <label>Part</label>
              <span class="value">{{ part?.name }} ({{ part?.sku }})</span>
            </div>
            <div class="info-item">
              <label>From Warehouse</label>
              <span class="value">{{ fromWarehouse?.name }}</span>
            </div>
          </div>
          <div class="info-row">
            <div class="info-item">
              <label>Current Stock</label>
              <span class="value stock-value">{{ currentStock?.quantity }} units</span>
            </div>
            <div class="info-item">
              <label>Available</label>
              <span class="value">{{ currentStock?.availableQuantity }} units</span>
            </div>
          </div>
        </p-card>

        <!-- Transfer Form -->
        <div class="form-row">
          <div class="form-group">
            <label for="toWarehouseId">To Warehouse *</label>
            <p-select
              id="toWarehouseId"
              [options]="warehouses"
              optionLabel="name"
              optionValue="id"
              formControlName="toWarehouseId"
              placeholder="Select destination warehouse"
              [filter]="true"
              filterBy="name,code"
              [showClear]="true">
              <ng-template let-warehouse pTemplate="item">
                <div class="flex align-items-center gap-2">
                  <div>{{ warehouse.name }} ({{ warehouse.code }})</div>
                </div>
              </ng-template>
            </p-select>
            <small class="text-danger" *ngIf="form.get('toWarehouseId')?.invalid && form.get('toWarehouseId')?.touched">
              Destination warehouse is required
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
              [max]="currentStock?.availableQuantity || 999999"
              placeholder="Enter quantity to transfer"
              class="w-full">
            </p-inputNumber>
            <small class="text-muted">Max available: {{ currentStock?.availableQuantity }} units</small>
            <small class="text-danger" *ngIf="form.get('quantity')?.invalid && form.get('quantity')?.touched">
              Valid quantity is required (max: {{ currentStock?.availableQuantity }})
            </small>
          </div>
        </div>

        <div class="form-row">
          <div class="form-group full">
            <label for="notes">Notes</label>
            <textarea
              id="notes"
              formControlName="notes"
              placeholder="Enter reason for transfer (e.g., Replenishment, Rebalancing)"
              rows="3"
              class="w-full textarea-input">
            </textarea>
          </div>
        </div>

        <!-- Transfer Preview -->
        <p-card class="preview-card" *ngIf="form.get('toWarehouseId')?.value && form.get('quantity')?.value > 0">
          <div class="preview-row">
            <div class="preview-item">
              <label>From:</label>
              <span class="warehouse-name">{{ fromWarehouse?.name }}</span>
              <span class="stock-change decrease">-{{ form.get('quantity')?.value }} units</span>
            </div>
            <div class="arrow">
              <i class="pi pi-arrow-right"></i>
            </div>
            <div class="preview-item">
              <label>To:</label>
              <span class="warehouse-name">{{ getToWarehouseName() }}</span>
              <span class="stock-change increase">+{{ form.get('quantity')?.value }} units</span>
            </div>
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
            label="Transfer Stock"
            icon="pi pi-arrow-right"
            class="p-button-success"
            [loading]="isSubmitting"
            [disabled]="!form.valid || isSubmitting">
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .transfer-dialog {
      padding: 1.5rem;
      min-width: 550px;
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
      gap: 2rem;
      margin-bottom: 1rem;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .info-item label {
      font-size: 0.875rem;
      color: #6b7280;
      font-weight: 500;
    }

    .info-item .value {
      font-size: 1rem;
      font-weight: 600;
      color: #1f2937;
    }

    .stock-value {
      color: #059669;
      font-size: 1.1rem;
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

    .text-muted {
      font-size: 0.75rem;
      color: #6b7280;
    }

    .textarea-input {
      border: 1px solid #d1d5db;
      border-radius: 4px;
      padding: 0.5rem;
      font-family: inherit;
      resize: vertical;
    }

    .preview-row {
      display: flex;
      justify-content: space-between;
      align-items: center;
      gap: 1rem;
    }

    .preview-item {
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
      flex: 1;
    }

    .preview-item label {
      font-size: 0.75rem;
      color: #6b7280;
      font-weight: 500;
    }

    .warehouse-name {
      font-weight: 600;
      color: #1f2937;
    }

    .stock-change {
      font-size: 1.1rem;
      font-weight: 700;
    }

    .stock-change.increase {
      color: #059669;
    }

    .stock-change.decrease {
      color: #dc2626;
    }

    .arrow {
      font-size: 1.5rem;
      color: #3b82f6;
    }

    .text-danger {
      color: #dc2626;
      font-size: 0.75rem;
    }

    .button-group {
      display: flex;
      gap: 1rem;
      margin-top: 2rem;
      justify-content: flex-end;
    }

    .w-full {
      width: 100%;
    }
  `]
})
export class StockTransferDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);

  form: FormGroup;
  isSubmitting = false;
  part: PartResponse | null = null;
  fromWarehouse: WarehouseResponse | null = null;
  warehouses: WarehouseResponse[] = [];
  currentStock: StockLevelResponse | null = null;

  constructor() {
    this.form = this.fb.group({
      toWarehouseId: ['', Validators.required],
      quantity: [0, [Validators.required, Validators.min(1)]],
      notes: ['']
    });
  }

  ngOnInit(): void {
    this.currentStock = this.config.data.stock;
    this.loadData();
  }

  private loadData(): void {
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

    // Load source warehouse
    this.warehouseService.getWarehouseById(this.currentStock.warehouseId).subscribe({
      next: (warehouse) => {
        this.fromWarehouse = warehouse;
      },
      error: (_error) => {
        console.error('Error loading warehouse:', _error);
      }
    });

    // Load all warehouses for dropdown (excluding source warehouse)
    this.warehouseService.getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] }).subscribe({
      next: (res) => {
        const warehouses = res.data ?? [];
        this.warehouses = warehouses.filter(w => w.id !== this.currentStock?.warehouseId);
      },
      error: (_error) => {
        console.error('Error loading warehouses:', _error);
      }
    });
  }

  getToWarehouseName(): string {
    const toWarehouseId = this.form.get('toWarehouseId')?.value;
    const warehouse = this.warehouses.find(w => w.id === toWarehouseId);
    return warehouse?.name || '';
  }

  onSubmit(): void {
    if (!this.form.valid || !this.currentStock) {
      return;
    }

    this.isSubmitting = true;
    const request = {
      partId: this.currentStock.partId,
      fromWarehouseId: this.currentStock.warehouseId,
      toWarehouseId: this.form.get('toWarehouseId')?.value,
      quantity: this.form.get('quantity')?.value,
      quantityInBaseUnit: undefined,  // Backend will calculate if not provided
      unitId: this.currentStock.unitId || undefined,
      reference: '',
      notes: this.form.get('notes')?.value || ''
    };

    this.stockService.transferStock(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Stock transferred successfully'
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
          detail: error?.error?.message || 'Failed to transfer stock'
        });
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
