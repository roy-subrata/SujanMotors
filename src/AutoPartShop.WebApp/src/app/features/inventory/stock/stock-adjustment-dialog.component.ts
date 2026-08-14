import { Component, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { DynamicDialogRef, DynamicDialogConfig } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { map } from 'rxjs/operators';
import { StockService, StockLevelResponse } from '../services/stock.service';
import { PartService, PartResponse } from '../services/part.service';
import { WarehouseService, WarehouseResponse } from '../services/warehouse.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-stock-adjustment-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    SelectModule,
    CardModule,
    ToastModule,
    LazyAutocompleteComponent,
    TranslatePipe
  ],
  providers: [MessageService],
  templateUrl: './stock-adjustment-dialog.component.html',
  styleUrl: './stock-adjustment-dialog.component.scss'
})
export class StockAdjustmentDialogComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly stockService = inject(StockService);
  private readonly partService = inject(PartService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly messageService = inject(MessageService);
  private readonly i18n = inject(I18nService);
  private readonly destroyRef = inject(DestroyRef);

  form: FormGroup;
  isSubmitting = false;
  mode: 'adjust' | 'create' = 'adjust';
  part: PartResponse | null = null;
  warehouse: WarehouseResponse | null = null;
  currentStock: StockLevelResponse | null = null;

  // Create-mode: product/variant picker (lazy server-side search — see LazyAutocompleteComponent)
  selectedProductModel: PartResponse | null = null;
  fetchProductsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search || '',
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true,
      flattenVariants: true
    }).pipe(
      map((res) => ({
        items: res.data ?? [],
        totalCount: res.pagination?.totalCount ?? 0
      }) as LazyResponse<PartResponse>)
    );

  warehouseOptions: { label: string; value: string }[] = [];

  adjustmentTypes: { label: string; value: string }[] = [];

  private buildAdjustmentTypes(): void {
    this.adjustmentTypes = [
      { label: this.i18n.t('stock.adjustmentDialog.types.increaseFound'), value: 'FOUND' },
      { label: this.i18n.t('stock.adjustmentDialog.types.decreaseDamaged'), value: 'DAMAGED' },
      { label: this.i18n.t('stock.adjustmentDialog.types.decreaseExpired'), value: 'EXPIRED' },
      { label: this.i18n.t('stock.adjustmentDialog.types.decreaseLost'), value: 'LOST' },
      { label: this.i18n.t('stock.adjustmentDialog.types.countUp'), value: 'COUNT_CORRECTION_UP' },
      { label: this.i18n.t('stock.adjustmentDialog.types.countDown'), value: 'COUNT_CORRECTION_DOWN' }
    ];
  }

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
    this.buildAdjustmentTypes();
    this.i18n.translationsLoaded$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.buildAdjustmentTypes();
    });

    this.mode = this.config.data?.mode === 'create' ? 'create' : 'adjust';

    if (this.mode === 'create') {
      this.form.get('productKey')?.addValidators(Validators.required);
      this.form.get('warehouseId')?.addValidators(Validators.required);
      this.loadWarehouseOptions();
    } else {
      this.currentStock = this.config.data.stock;
      this.loadPartAndWarehouse();
    }
  }

  /** Selection from the lazy product/variant search — keep the form control in sync for validation. */
  onProductSelected(product: PartResponse): void {
    this.selectedProductModel = product;
    this.form.get('productKey')?.setValue(product.id);
    this.form.get('productKey')?.markAsTouched();
  }

  onProductCleared(): void {
    this.selectedProductModel = null;
    this.form.get('productKey')?.setValue('');
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
      const selected = this.selectedProductModel;
      if (!selected) return;
      partId = selected.id;
      variantId = selected.isVariant ? (selected.variantId ?? null) : null;
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
          summary: this.i18n.t('common.messages.success'),
          detail: this.i18n.t('stock.adjustmentDialog.messages.success')
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
          detail: error?.error?.message || this.i18n.t('stock.adjustmentDialog.messages.failed')
        });
        this.isSubmitting = false;
      }
    });
  }

  onCancel(): void {
    this.dialogRef.close();
  }
}
