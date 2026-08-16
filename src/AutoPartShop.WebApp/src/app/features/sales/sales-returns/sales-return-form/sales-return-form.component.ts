import { Component, OnInit, OnDestroy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesReturnService, CreateSalesReturnRequest, SalesReturnResponse } from '../../services/sales-return.service';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { WarehouseService, WarehouseResponse } from '../../../inventory/services/warehouse.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../../shared/components/lazy-autocomplete';
import { Subject } from 'rxjs';
import { map, of, switchMap, takeUntil } from 'rxjs';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
  selector: 'app-sales-return-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, CardModule, ToastModule, ConfirmDialogModule, ButtonModule, InputTextModule, LazyAutocompleteComponent, TranslatePipe],
  providers: [MessageService, ConfirmationService],
  templateUrl: './sales-return-form.component.html',
  styleUrls: ['./sales-return-form.component.css']
})
export class SalesReturnFormComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly salesReturnService = inject(SalesReturnService);
  private readonly salesOrderService = inject(SalesOrderService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly i18n = inject(I18nService);

  salesReturnForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'view'>('create');
  salesReturnId = signal<string | null>(null);
  currentSalesReturn = signal<SalesReturnResponse | null>(null);

  // Sales Order selection
  selectedSalesOrder = signal<SalesOrderResponse | null>(null);

  // Warehouse selection
  warehouses = signal<WarehouseResponse[]>([]);
  selectedWarehouse = signal<WarehouseResponse | null>(null);
  loadingWarehouses = signal(false);

  // Reject form state
  showRejectForm = false;
  rejectReasonInput = '';

  // Lazy fetch functions — status filter is server-side to avoid hiding paginated results
  fetchSalesOrdersLazy = (req: LazyRequest) =>
    this.salesOrderService.getSalesOrders({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data,
        totalCount: res.pagination.totalCount
      } as LazyResponse<SalesOrderResponse>))
    );

  fetchWarehousesLazy = (req: LazyRequest) => {
    const filtered = this.warehouses().filter(w =>
      !req.search ||
      w.name.toLowerCase().includes(req.search.toLowerCase()) ||
      w.code.toLowerCase().includes(req.search.toLowerCase()) ||
      w.location.toLowerCase().includes(req.search.toLowerCase())
    );

    return of({
      items: filtered,
      totalCount: filtered.length
    } as LazyResponse<WarehouseResponse>);
  };

  // Getter so it re-evaluates on every change detection cycle (reactive forms are not signals)
  get totalRefund(): number {
    if (!this.salesReturnForm) return 0;
    return this.lines.controls.reduce((sum, line) => {
      const qty = line.get('quantity')?.value || 0;
      const price = line.get('unitPrice')?.value || 0;
      return sum + (qty * price);
    }, 0);
  }

  ngOnInit(): void {
    this.initializeForm();
    this.loadWarehouses();

    this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params['id'];
      if (id) {
        this.salesReturnId.set(id);
        this.mode.set('view');
        this.loadSalesReturn(id);
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  initializeForm(): void {
    this.salesReturnForm = this.fb.group({
      salesOrderId: ['', [Validators.required]],
      warehouseId: ['', [Validators.required]],
      reason: ['', [Validators.required]],
      refundType: ['CASH_REFUND', [Validators.required]],
      notes: [''],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.salesReturnForm.get('lines') as FormArray;
  }

  loadWarehouses(): void {
    this.loadingWarehouses.set(true);
    this.warehouseService.getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (res) => {
          this.warehouses.set(res.data ?? []);
          this.loadingWarehouses.set(false);
        },
        error: (err) => {
          console.error('Error loading warehouses:', err);
          this.loadingWarehouses.set(false);
        }
      });
  }

  loadSalesReturn(id: string): void {
    this.loading.set(true);
    this.error.set(null);

    this.salesReturnService.getSalesReturnById(id).pipe(
      takeUntil(this.destroy$),
      switchMap(salesReturn => {
        this.currentSalesReturn.set(salesReturn);
        return this.salesOrderService.getSalesOrderById(salesReturn.salesOrderId).pipe(
          map(order => ({ salesReturn, order }))
        );
      })
    ).subscribe({
      next: ({ salesReturn, order }) => {
        this.selectedSalesOrder.set(order);
        this.salesReturnForm.patchValue({
          salesOrderId: salesReturn.salesOrderId,
          warehouseId: salesReturn.warehouseId,
          reason: salesReturn.reason,
          notes: salesReturn.notes
        });

        if (salesReturn.warehouseId) {
          const warehouse = this.warehouses().find(w => w.id === salesReturn.warehouseId);
          if (warehouse) this.selectedWarehouse.set(warehouse);
        }

        this.lines.clear();
        salesReturn.lines.forEach(line => {
          this.lines.push(this.fb.group({
            salesOrderLineId: [line.id],
            partId: [line.partId],
            quantity: [line.quantity],
            unitPrice: [line.unitPrice],
            unitId: [line.unitId || null],
            quantityInBaseUnit: [line.quantityInBaseUnit || line.quantity],
            unitPriceInBaseUnit: [line.unitPriceInBaseUnit || line.unitPrice],
            condition: [line.condition],
            notes: [line.notes],
            maxQuantity: [line.quantity],
            partName: [line.partName || ''],
            partLocalName: [line.partLocalName || null],
            variantName: [line.variantName || null],
            displayName: [line.displayName || line.partName || ''],
            partSku: [line.partSku || '']
          }));
        });

        if (this.mode() === 'view') this.salesReturnForm.disable();
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(this.i18n.t('salesReturnForm.messages.loadFailed'));
        this.loading.set(false);
        console.error('Error loading sales return:', err);
      }
    });
  }

  approve(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: this.i18n.t('salesReturnForm.messages.approveConfirm', { number: current.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.approveSalesReturn(current.id).pipe(takeUntil(this.destroy$)).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('salesReturnForm.messages.approveSuccess', { number: current.returnNumber }) });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturnForm.messages.approveFailed')) });
          }
        });
      }
    });
  }

  reject(): void {
    this.showRejectForm = true;
  }

  submitReject(): void {
    const current = this.currentSalesReturn();
    if (!current) return;
    if (!this.rejectReasonInput.trim()) {
      this.messageService.add({ severity: 'warn', summary: this.i18n.t('salesReturnForm.messages.required'), detail: this.i18n.t('salesReturnForm.messages.enterRejectReason') });
      return;
    }
    const reason = this.rejectReasonInput.trim();
    this.showRejectForm = false;
    this.rejectReasonInput = '';

    this.salesReturnService.rejectSalesReturn(current.id, reason).pipe(takeUntil(this.destroy$)).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('salesReturnForm.messages.rejectSuccess', { number: current.returnNumber }) });
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturnForm.messages.rejectFailed')) });
      }
    });
  }

  cancelReject(): void {
    this.showRejectForm = false;
    this.rejectReasonInput = '';
  }

  receive(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: this.i18n.t('salesReturnForm.messages.receiveConfirm', { number: current.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.receiveSalesReturn(current.id).pipe(takeUntil(this.destroy$)).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('salesReturnForm.messages.receiveSuccess', { number: current.returnNumber }) });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturnForm.messages.receiveFailed')) });
          }
        });
      }
    });
  }

  process(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: this.i18n.t('salesReturnForm.messages.processConfirm', { number: current.returnNumber }),
      header: this.i18n.t('common.actions.confirm'),
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.processSalesReturn(current.id).pipe(takeUntil(this.destroy$)).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('salesReturnForm.messages.processSuccess', { number: current.returnNumber }) });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: typeof err?.error === 'string' ? err.error : (err?.error?.message || this.i18n.t('salesReturnForm.messages.processFailed')) });
          }
        });
      }
    });
  }

  selectSalesOrder(event: SalesOrderResponse): void {
    const order = event as SalesOrderResponse;
    this.selectedSalesOrder.set(order);

    this.salesReturnForm.patchValue({
      salesOrderId: order.id,
      warehouseId: order.warehouseId || ''
    });

    // Auto-select warehouse if order has one
    if (order.warehouseId) {
      const warehouse = this.warehouses().find(w => w.id === order.warehouseId);
      if (warehouse) {
        this.selectedWarehouse.set(warehouse);
      }
    }

    // Clear existing lines and add order lines
    this.lines.clear();
    order.lines.forEach(line => {
      this.lines.push(this.createLineFromOrderLine(line));
    });
  }

  selectWarehouse(event: WarehouseResponse): void {
    const warehouse = event as WarehouseResponse;
    this.selectedWarehouse.set(warehouse);

    this.salesReturnForm.patchValue({
      warehouseId: warehouse.id
    });
  }

  createLineFromOrderLine(orderLine: any): FormGroup {
    // Calculate base unit ratio from order line (e.g., if ordered 2 boxes = 24 base units, ratio = 12)
    const baseUnitRatio = (orderLine.quantityInBaseUnit && orderLine.quantity)
      ? orderLine.quantityInBaseUnit / orderLine.quantity
      : 1;

    return this.fb.group({
      salesOrderLineId: [orderLine.id],
      partId: [orderLine.partId],
      quantity: [0, [Validators.required, Validators.min(0), Validators.max(orderLine.quantity)]],
      unitPrice: [orderLine.unitPrice],
      unitId: [orderLine.unitId || null],
      quantityInBaseUnit: [0],
      unitPriceInBaseUnit: [orderLine.unitPriceInBaseUnit || orderLine.unitPrice],
      condition: ['UNOPENED', [Validators.required]],
      notes: [''],
      // Read-only fields for display and calculation
      maxQuantity: [orderLine.quantity],
      baseUnitRatio: [baseUnitRatio],
      partName: [orderLine.partName || ''],
      partLocalName: [orderLine.partLocalName || null],
      variantName: [orderLine.variantName || null],
      displayName: [orderLine.displayName || orderLine.partName || ''],
      partSku: [orderLine.partSku || orderLine.sku || '']
    });
  }

  onQuantityChange(index: number): void {
    const line = this.lines.at(index);
    if (!line) return;

    const quantity = line.get('quantity')?.value || 0;
    const ratio = line.get('baseUnitRatio')?.value || 1;
    line.patchValue({ quantityInBaseUnit: Math.round(quantity * ratio) });
  }

  onSubmit(): void {
    if (this.salesReturnForm.invalid) {
      Object.keys(this.salesReturnForm.controls).forEach(key => {
        const control = this.salesReturnForm.get(key);
        if (control?.invalid) {
          control.markAsTouched();
        }
      });
      this.error.set(this.i18n.t('salesReturnForm.messages.fillRequired'));
      return;
    }

    // Validate that warehouseId is not empty
    const warehouseId = this.salesReturnForm.get('warehouseId')?.value;
    if (!warehouseId) {
      this.error.set(this.i18n.t('salesReturnForm.messages.noWarehouseOnOrder'));
      return;
    }

    // Validate that at least one line has quantity > 0
    const validLines = this.lines.controls.filter(line => (line.get('quantity')?.value || 0) > 0);
    if (validLines.length === 0) {
      this.error.set(this.i18n.t('salesReturnForm.messages.selectAtLeastOne'));
      return;
    }

    this.saving.set(true);
    this.error.set(null);

    const formValue = this.salesReturnForm.value;
    const request: CreateSalesReturnRequest = {
      salesOrderId: formValue.salesOrderId,
      warehouseId: formValue.warehouseId,
      reason: formValue.reason,
      refundType: formValue.refundType,
      notes: formValue.notes,
      lines: validLines.map((line: any) => ({
        salesOrderLineId: line.value.salesOrderLineId,
        partId: line.value.partId,
        quantity: line.value.quantity,
        unitId: line.value.unitId || null,
        quantityInBaseUnit: line.value.quantityInBaseUnit || line.value.quantity,
        unitPrice: line.value.unitPrice,
        unitPriceInBaseUnit: line.value.unitPriceInBaseUnit || line.value.unitPrice,
        condition: line.value.condition,
        notes: line.value.notes || ''
      }))
    };

    this.salesReturnService.createSalesReturn(request).pipe(takeUntil(this.destroy$)).subscribe({
      next: () => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: this.i18n.t('common.messages.success'), detail: this.i18n.t('salesReturnForm.messages.createSuccess') });
        this.router.navigate(['/sales/sales-returns']);
      },
      error: (err) => {
        const errorMessage = typeof err?.error === 'string' ? err.error : (err?.error?.message || 'Failed to create sales return');
        this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: errorMessage });
        this.error.set(errorMessage);
        this.saving.set(false);
        console.error('Error creating sales return:', err);
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/sales/sales-returns']);
  }

  formatCurrency(amount: number): string {
    return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
  }
}
