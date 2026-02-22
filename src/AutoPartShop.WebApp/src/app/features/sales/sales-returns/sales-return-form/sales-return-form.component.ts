import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ButtonModule } from 'primeng/button';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesReturnService, CreateSalesReturnRequest, SalesReturnResponse } from '../../services/sales-return.service';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';
import { WarehouseService, WarehouseResponse } from '../../../inventory/services/warehouse.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../../shared/components/lazy-autocomplete';
import { map } from 'rxjs/operators';

@Component({
  selector: 'app-sales-return-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, CardModule, ToastModule, ConfirmDialogModule, ButtonModule, LazyAutocompleteComponent],
  providers: [MessageService, ConfirmationService],
  templateUrl: './sales-return-form.component.html',
  styleUrls: ['./sales-return-form.component.css']
})
export class SalesReturnFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly salesReturnService = inject(SalesReturnService);
  private readonly salesOrderService = inject(SalesOrderService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);

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

  // Lazy fetch functions
  fetchSalesOrdersLazy = (req: LazyRequest) =>
    this.salesOrderService.getSalesOrders({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      status: 'CONFIRMED'
    }).pipe(
      map(res => ({
        items: res.data.filter(o =>
          o.status === 'CONFIRMED' || o.status === 'DELIVERED' || o.status === 'PARTIALLY_SHIPPED'
        ),
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
    
    return {
      subscribe: (fn: any) => {
        fn.next({ items: filtered, totalCount: filtered.length });
        return { unsubscribe: () => {} };
      }
    } as any;
  };

  // Computed total refund
  totalRefund = computed(() => {
    if (!this.salesReturnForm) return 0;
    const lines = this.lines.controls;
    return lines.reduce((sum, line) => {
      const qty = line.get('quantity')?.value || 0;
      const price = line.get('unitPrice')?.value || 0;
      return sum + (qty * price);
    }, 0);
  });

  ngOnInit(): void {
    this.initializeForm();
    this.loadWarehouses();

    // Check route params for view mode
    this.route.queryParams.subscribe(params => {
      const id = params['id'];
      if (id) {
        this.salesReturnId.set(id);
        this.mode.set('view');
        this.loadSalesReturn(id);
      }
    });

    // Form will be disabled after load when in view mode (handled in loadSalesReturn)
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
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses) => {
        this.warehouses.set(warehouses);
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

    this.salesReturnService.getSalesReturnById(id).subscribe({
      next: (salesReturn) => {
        this.currentSalesReturn.set(salesReturn);
        // Load the sales order first
        this.salesOrderService.getSalesOrderById(salesReturn.salesOrderId).subscribe({
          next: (order) => {
            this.selectedSalesOrder.set(order);

            // Populate form
            this.salesReturnForm.patchValue({
              salesOrderId: salesReturn.salesOrderId,
              warehouseId: salesReturn.warehouseId,
              reason: salesReturn.reason,
              notes: salesReturn.notes
            });

            // Load warehouse if available
            if (salesReturn.warehouseId) {
              const warehouse = this.warehouses().find(w => w.id === salesReturn.warehouseId);
              if (warehouse) {
                this.selectedWarehouse.set(warehouse);
              }
            }

            // Populate line items
            this.lines.clear();
            salesReturn.lines.forEach(line => {
              this.lines.push(this.fb.group({
                salesOrderLineId: [line.id],
                partId: [line.partId],
                quantity: [line.quantity],
                unitPrice: [line.unitPrice],
                condition: [line.condition],
                notes: [line.notes],
                // Read-only fields for display
                maxQuantity: [line.quantity],
                partName: [(line as any).partName || ''],
                partSku: [(line as any).partSku || (line as any).sku || '']
              }));
            });

            // If route indicated view mode, disable the form after population
            if (this.mode() === 'view') {
              this.salesReturnForm.disable();
            }

            this.loading.set(false);
          },
          error: (err) => {
            this.error.set('Failed to load sales order');
            this.loading.set(false);
          }
        });
      },
      error: (err) => {
        this.error.set('Failed to load sales return');
        this.loading.set(false);
        console.error('Error loading sales return:', err);
      }
    });
  }

  approve(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to approve return ${current.returnNumber}?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.approveSalesReturn(current.id).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Sales return ${current.returnNumber} approved successfully`
            });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to approve sales return'
            });
            console.error('Error approving sales return', err);
          }
        });
      }
    });
  }

  reject(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    // Using prompt for simplicity - could be replaced with a custom dialog
    const reason = prompt(`Enter reason to reject ${current.returnNumber}:`);
    if (reason === null || reason.trim() === '') return;

    this.salesReturnService.rejectSalesReturn(current.id, reason).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Sales return ${current.returnNumber} rejected`
        });
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: err?.error?.message || 'Failed to reject sales return'
        });
        console.error('Error rejecting sales return', err);
      }
    });
  }

  receive(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: `Mark return ${current.returnNumber} as received?`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.receiveSalesReturn(current.id).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Sales return ${current.returnNumber} marked as received`
            });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to mark as received'
            });
            console.error('Error receiving sales return', err);
          }
        });
      }
    });
  }

  process(): void {
    const current = this.currentSalesReturn();
    if (!current) return;

    this.confirmationService.confirm({
      message: `Process return ${current.returnNumber}? This will finalize the return.`,
      header: 'Confirm',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.salesReturnService.processSalesReturn(current.id).subscribe({
          next: (updated) => {
            this.currentSalesReturn.set(updated);
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Sales return ${current.returnNumber} processed successfully`
            });
            this.loadSalesReturn(updated.id);
          },
          error: (err) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: err?.error?.message || 'Failed to process sales return'
            });
            console.error('Error processing sales return', err);
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
    return this.fb.group({
      salesOrderLineId: [orderLine.id],
      partId: [orderLine.partId],
      quantity: [0, [Validators.required, Validators.min(0), Validators.max(orderLine.quantity)]],
      unitPrice: [orderLine.unitPrice],
      condition: ['UNOPENED', [Validators.required]],
      notes: [''],
      // Read-only fields for display
      maxQuantity: [orderLine.quantity],
      partName: [orderLine.partName || ''],
      partSku: [orderLine.partSku || orderLine.sku || '']
    });
  }

  onSubmit(): void {
    if (this.salesReturnForm.invalid) {
      Object.keys(this.salesReturnForm.controls).forEach(key => {
        const control = this.salesReturnForm.get(key);
        if (control?.invalid) {
          control.markAsTouched();
        }
      });
      this.error.set('Please fill in all required fields');
      return;
    }

    // Validate that warehouseId is not empty
    const warehouseId = this.salesReturnForm.get('warehouseId')?.value;
    if (!warehouseId) {
      this.error.set('The selected sales order does not have a warehouse assigned. Please select a different sales order.');
      return;
    }

    // Validate that at least one line has quantity > 0
    const validLines = this.lines.controls.filter(line => (line.get('quantity')?.value || 0) > 0);
    if (validLines.length === 0) {
      this.error.set('Please select at least one item to return with quantity > 0');
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
        unitPrice: line.value.unitPrice,
        condition: line.value.condition,
        notes: line.value.notes || ''
      }))
    };

    this.salesReturnService.createSalesReturn(request).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: 'Sales return created successfully'
        });
        this.router.navigate(['/sales/sales-returns']);
      },
      error: (err) => {
        const errorMessage = err?.error?.message || 'Failed to create sales return';
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: errorMessage
        });
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
