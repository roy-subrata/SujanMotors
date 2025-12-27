import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SalesReturnService, CreateSalesReturnRequest, SalesReturnResponse } from '../../services/sales-return.service';
import { SalesOrderService, SalesOrderResponse } from '../../services/sales-order.service';

@Component({
  selector: 'app-sales-return-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, AutoCompleteModule],
  templateUrl: './sales-return-form.component.html',
  styleUrls: ['./sales-return-form.component.css']
})
export class SalesReturnFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly salesReturnService = inject(SalesReturnService);
  private readonly salesOrderService = inject(SalesOrderService);

  salesReturnForm!: FormGroup;
  loading = signal(false);
  saving = signal(false);
  error = signal<string | null>(null);
  mode = signal<'create' | 'view'>('create');
  salesReturnId = signal<string | null>(null);
  currentSalesReturn = signal<SalesReturnResponse | null>(null);

  // Sales Order selection
  salesOrders = signal<SalesOrderResponse[]>([]);
  filteredSalesOrders = signal<SalesOrderResponse[]>([]);
  selectedSalesOrder = signal<SalesOrderResponse | null>(null);
  loadingSalesOrders = signal(false);

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
    this.loadSalesOrders();

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
      warehouseId: ['00000000-0000-0000-0000-000000000000'], // Default warehouse
      reason: ['', [Validators.required]],
      notes: [''],
      lines: this.fb.array([])
    });
  }

  get lines(): FormArray {
    return this.salesReturnForm.get('lines') as FormArray;
  }

  loadSalesOrders(): void {
    this.loadingSalesOrders.set(true);
    this.salesOrderService.getAllSalesOrders().subscribe({
      next: (orders) => {
        // Only show CONFIRMED or DELIVERED orders
        const validOrders = orders.filter(o =>
          o.status === 'CONFIRMED' || o.status === 'DELIVERED' || o.status === 'PARTIALLY_SHIPPED'
        );
        this.salesOrders.set(validOrders);
        this.filteredSalesOrders.set(validOrders);
        this.loadingSalesOrders.set(false);
      },
      error: (err) => {
        console.error('Error loading sales orders:', err);
        this.loadingSalesOrders.set(false);
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

            // Populate line items
            this.lines.clear();
            salesReturn.lines.forEach(line => {
              this.lines.push(this.fb.group({
                salesOrderLineId: [line.id],
                partId: [line.partId],
                quantity: [line.quantity],
                unitPrice: [line.unitPrice],
                condition: [line.condition],
                notes: [line.notes]
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
    if (!confirm(`Approve sales return ${current.returnNumber}?`)) return;
    this.salesReturnService.approveSalesReturn(current.id).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        console.error('Error approving sales return', err);
        alert('Failed to approve sales return');
      }
    });
  }

  reject(): void {
    const current = this.currentSalesReturn();
    if (!current) return;
    const reason = prompt(`Enter reason to reject ${current.returnNumber}:`);
    if (reason === null) return;
    this.salesReturnService.rejectSalesReturn(current.id, reason).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        console.error('Error rejecting sales return', err);
        alert('Failed to reject sales return');
      }
    });
  }

  receive(): void {
    const current = this.currentSalesReturn();
    if (!current) return;
    if (!confirm(`Mark ${current.returnNumber} as received?`)) return;
    this.salesReturnService.receiveSalesReturn(current.id).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        console.error('Error receiving sales return', err);
        alert('Failed to mark as received');
      }
    });
  }

  process(): void {
    const current = this.currentSalesReturn();
    if (!current) return;
    if (!confirm(`Process ${current.returnNumber}? This will finalize the return.`)) return;
    this.salesReturnService.processSalesReturn(current.id).subscribe({
      next: (updated) => {
        this.currentSalesReturn.set(updated);
        this.loadSalesReturn(updated.id);
      },
      error: (err) => {
        console.error('Error processing sales return', err);
        alert('Failed to process sales return');
      }
    });
  }

  onSalesOrderFilter(event: any): void {
    const query = event.query.toLowerCase();

    if (!query) {
      this.filteredSalesOrders.set(this.salesOrders());
      return;
    }

    const filtered = this.salesOrders().filter(o =>
      o.soNumber.toLowerCase().includes(query) ||
      o.customerName.toLowerCase().includes(query)
    );
    this.filteredSalesOrders.set(filtered);
  }

  selectSalesOrder(event: any): void {
    const order = event as SalesOrderResponse;
    this.selectedSalesOrder.set(order);

    this.salesReturnForm.patchValue({
      salesOrderId: order.id
    });

    // Clear existing lines and add order lines
    this.lines.clear();
    order.lines.forEach(line => {
      this.lines.push(this.createLineFromOrderLine(line));
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
      partName: [orderLine.partName || '']
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
        alert('Sales return created successfully!');
        this.router.navigate(['/sales/sales-returns']);
      },
      error: (err) => {
        let errorMessage = 'Failed to create sales return';
        if (err.error?.message) {
          errorMessage = err.error.message;
        }
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
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }
}
