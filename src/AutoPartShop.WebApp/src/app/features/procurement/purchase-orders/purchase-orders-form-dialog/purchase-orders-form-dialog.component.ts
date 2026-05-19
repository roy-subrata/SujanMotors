import { Component, EventEmitter, Input, Output, ViewChild, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { ButtonModule } from 'primeng/button';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { PurchaseOrderService, PurchaseOrderResponse, CreatePurchaseOrderRequest, UpdatePurchaseOrderRequest } from '../../services/purchase-order.service';
import { SupplierService, SupplierResponse } from '../../../inventory/services/supplier.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

@Component({
  selector: 'app-purchase-orders-form-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    DialogModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    ButtonModule,
    TableModule
  ],
  templateUrl: './purchase-orders-form-dialog.component.html',
  styleUrls: ['./purchase-orders-form-dialog.component.css']
})
export class PurchaseOrdersFormDialogComponent implements OnInit {
  @Input() visible = false;
  @Input() purchaseOrder: PurchaseOrderResponse | null = null;

  @Output() visibleChange = new EventEmitter<boolean>();
  @Output() submitted = new EventEmitter<PurchaseOrderResponse>();

  @ViewChild('dialog') dialog: any;

  form: FormGroup;
  isEditing = false;
  isSubmitting = false;
  filteredSuppliers: SupplierResponse[] = [];
  suppliers: SupplierResponse[] = [];

  private readonly poService = inject(PurchaseOrderService);
  private readonly supplierService = inject(SupplierService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly currencyService = inject(CurrencyService);

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.loadSuppliers();
  }

  /**
   * Load suppliers for autocomplete
   */
  private loadSuppliers(): void {
    this.supplierService.getAllSuppliers().subscribe({
      next: (suppliers) => {
        this.suppliers = Array.isArray(suppliers) ? suppliers : [];
        this.filteredSuppliers = this.suppliers;
      },
      error: (error) => {
        console.error('Error loading suppliers:', error);
      }
    });
  }

  /**
   * Create form group with validators
   */
  private createForm(): FormGroup {
    return this.fb.group({
      supplierId: ['', Validators.required],
      deliveryDate: ['', Validators.required],
      taxPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      discountPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      notes: [''],
      lines: this.fb.array([])
    });
  }

  /**
   * Get lines form array
   */
  get linesArray(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  /**
   * Handle dialog show event
   */
  onDialogShow(): void {
    if (this.purchaseOrder) {
      this.isEditing = true;
      this.form.patchValue({
        supplierId: this.purchaseOrder.supplierId,
        deliveryDate: this.purchaseOrder.deliveryDate,
        taxPercentage: this.purchaseOrder.taxPercentage || 0,
        discountPercentage: this.purchaseOrder.discountPercentage || 0,
        notes: this.purchaseOrder.notes
      });

      // Populate line items
      const linesArray = this.linesArray;
      linesArray.clear();
      this.purchaseOrder.lines?.forEach(line => {
        linesArray.push(this.fb.group({
          partId: [line.partId, Validators.required],
          quantity: [line.quantity, [Validators.required, Validators.min(1)]],
          unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]]
        }));
      });
    } else {
      this.isEditing = false;
      this.form.reset();
      const linesArray = this.linesArray;
      linesArray.clear();
      // Add one empty line by default
      this.addLineItem();
    }
  }

  /**
   * Add a new line item
   */
  addLineItem(): void {
    const linesArray = this.linesArray;
    linesArray.push(this.fb.group({
      partId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    }));
  }

  /**
   * Remove a line item
   */
  removeLineItem(index: number): void {
    this.linesArray.removeAt(index);
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.markFormGroupTouched(this.form);
      return;
    }

    if (this.linesArray.length === 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Please add at least one line item'
      });
      return;
    }

    this.isSubmitting = true;

    if (this.isEditing && this.purchaseOrder) {
      this.updatePurchaseOrder();
    } else {
      this.createPurchaseOrder();
    }
  }

  /**
   * Create new purchase order
   */
  private createPurchaseOrder(): void {
    const lineItems = this.linesArray.value.map((line: any) => ({
      partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
      quantity: line.quantity,
      unitPrice: line.unitPrice
    }));

    const supplierId = this.form.value.supplierId;
    const request: CreatePurchaseOrderRequest = {
      supplierId: typeof supplierId === 'string' ? supplierId : supplierId.id,
      deliveryDate: this.form.value.deliveryDate,
      taxPercentage: this.form.value.taxPercentage || 0,
      discountPercentage: this.form.value.discountPercentage || 0,
      notes: this.form.value.notes,
      lineItems
    };

    this.poService.createPurchaseOrder(request).subscribe({
      next: (po) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Purchase Order '${po.poNumber}' created successfully`
        });
        this.submitted.emit(po);
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to create purchase order'
        });
        console.error('Error creating purchase order:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Update existing purchase order
   */
  private updatePurchaseOrder(): void {
    if (!this.purchaseOrder) return;

    const lineItems = this.linesArray.value.map((line: any) => ({
      partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
      quantity: line.quantity,
      unitPrice: line.unitPrice
    }));

    const supplierId = this.form.value.supplierId;
    const request: UpdatePurchaseOrderRequest = {
      id: this.purchaseOrder.id,
      supplierId: typeof supplierId === 'string' ? supplierId : supplierId.id,
      deliveryDate: this.form.value.deliveryDate,
      taxPercentage: this.form.value.taxPercentage || 0,
      discountPercentage: this.form.value.discountPercentage || 0,
      notes: this.form.value.notes,
      lineItems
    };

    this.poService.updatePurchaseOrder(this.purchaseOrder.id, request).subscribe({
      next: (po) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Purchase Order '${po.poNumber}' updated successfully`
        });
        this.submitted.emit(po);
        this.closeDialog();
        this.isSubmitting = false;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update purchase order'
        });
        console.error('Error updating purchase order:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Close dialog
   */
  closeDialog(): void {
    this.visibleChange.emit(false);
  }

  /**
   * Mark all form fields as touched to show validation errors
   */
  private markFormGroupTouched(formGroup: FormGroup): void {
    Object.keys(formGroup.controls).forEach(key => {
      const control = formGroup.get(key);
      control?.markAsTouched();

      if (control instanceof FormArray) {
        control.controls.forEach(item => {
          if (item instanceof FormGroup) {
            this.markFormGroupTouched(item);
          }
        });
      }
    });
  }

  /**
   * Get form field error message
   */
  getErrorMessage(fieldName: string): string {
    const control = this.form.get(fieldName);

    if (!control || !control.errors || !control.touched) {
      return '';
    }

    const errors = control.errors;

    if (errors['required']) {
      return `${this.formatFieldName(fieldName)} is required`;
    }

    return 'Invalid input';
  }

  /**
   * Format field name for display
   */
  private formatFieldName(fieldName: string): string {
    return fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, (str) => str.toUpperCase())
      .trim();
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string): boolean {
    const control = this.form.get(fieldName);
    return !!(control && control.invalid && control.touched);
  }

  /**
   * Get dialog title
   */
  getDialogTitle(): string {
    return this.isEditing ? 'Edit Purchase Order' : 'New Purchase Order';
  }

  /**
   * Get submit button label
   */
  getSubmitButtonLabel(): string {
    return this.isEditing ? 'Update' : 'Create';
  }

  /**
   * Filter suppliers based on search input
   */
  onSupplierFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredSuppliers = this.suppliers.filter(supplier =>
      supplier.name.toLowerCase().includes(query) ||
      supplier.code.toLowerCase().includes(query)
    );
  }

  /**
   * Calculate line total
   */
  getLineTotal(index: number): number {
    const line = this.linesArray.at(index);
    const quantity = line?.get('quantity')?.value || 0;
    const unitPrice = line?.get('unitPrice')?.value || 0;
    return quantity * unitPrice;
  }

  /**
   * Calculate grand total
   */
  getGrandTotal(): number {
    return this.linesArray.controls.reduce((total, line) => {
      const quantity = line.get('quantity')?.value || 0;
      const unitPrice = line.get('unitPrice')?.value || 0;
      return total + (quantity * unitPrice);
    }, 0);
  }

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
  }

  get currencyCode(): string {
    return this.currencyService.selectedCurrency();
  }
}
