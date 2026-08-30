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
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
    ToastModule,
    TranslatePipe
  ],
  providers: [MessageService],
  templateUrl: './stock-transfer-dialog.component.html',
  styleUrl: './stock-transfer-dialog.component.scss',
})
export class StockTransferDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);
  private readonly i18n = inject(I18nService);

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
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('stockTransfer.messages.success')
        });
        this.isSubmitting = false;
        setTimeout(() => {
          this.dialogRef.close({ success: true });
        }, 500);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('stockTransfer.messages.failed')
        });
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
