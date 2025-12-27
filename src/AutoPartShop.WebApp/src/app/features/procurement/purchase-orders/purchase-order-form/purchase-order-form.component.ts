import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { MessageModule } from 'primeng/message';
import { ToastModule } from 'primeng/toast';
import { MessageService } from 'primeng/api';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { SupplierService, SupplierResponse } from '../../../inventory/services/supplier.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';

@Component({
  selector: 'app-purchase-order-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    TableModule,
    CardModule,
    DividerModule,
    MessageModule,
    ToastModule
  ],
  templateUrl: './purchase-order-form.component.html',
  styleUrls: ['./purchase-order-form.component.css'],
  providers: [MessageService]
})
export class PurchaseOrderFormComponent implements OnInit {
  form: FormGroup;
  isEditing = false;
  isSubmitting = false;
  poId: string | null = null;
  filteredSuppliers: SupplierResponse[] = [];
  suppliers: SupplierResponse[] = [];
  filteredPaymentTerms: any[] = [];
  filteredCurrencies: any[] = [];
  filteredPriorities: any[] = [];
  filteredParts: PartResponse[] = [];
  parts: PartResponse[] = [];

  paymentTermsOptions = [
    { label: 'Net 15', value: 'NET15' },
    { label: 'Net 30', value: 'NET30' },
    { label: 'Net 45', value: 'NET45' },
    { label: 'Net 60', value: 'NET60' },
    { label: 'COD (Cash on Delivery)', value: 'COD' },
    { label: 'Prepaid', value: 'PREPAID' }
  ];

  currencyOptions = [
    { label: 'Indian Rupee (INR)', value: 'INR' },
    { label: 'US Dollar (USD)', value: 'USD' },
    { label: 'Euro (EUR)', value: 'EUR' },
    { label: 'British Pound (GBP)', value: 'GBP' }
  ];

  priorityOptions = [
    { label: 'Low', value: 'LOW', severity: 'info' },
    { label: 'Medium', value: 'MEDIUM', severity: 'warning' },
    { label: 'High', value: 'HIGH', severity: 'danger' }
  ];

  private readonly poService = inject(PurchaseOrderService);
  private readonly supplierService = inject(SupplierService);
  private readonly partService = inject(PartService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.initializeFilteredArrays();

    // Load suppliers
    this.loadSuppliers();

    // Load parts first, then check edit mode (to ensure parts are available when loading PO)
    this.partService.getActiveParts().subscribe({
      next: (parts) => {
        this.parts = Array.isArray(parts) ? parts : [];
        this.filteredParts = this.parts;
        // Now that parts are loaded, we can safely load the purchase order if editing
        this.checkEditMode();
      },
      error: (error) => {
        console.error('Error loading parts:', error);
        // Still check edit mode even if parts failed to load
        this.checkEditMode();
      }
    });
  }

  /**
   * Initialize filtered arrays with default values
   */
  private initializeFilteredArrays(): void {
    this.filteredPaymentTerms = this.paymentTermsOptions;
    this.filteredCurrencies = this.currencyOptions;
    this.filteredPriorities = this.priorityOptions;
  }

  /**
   * Check if editing existing PO
   */
  private checkEditMode(): void {
    this.poId = this.route.snapshot.queryParamMap.get('id');
    if (this.poId) {
      this.isEditing = true;
      this.loadPurchaseOrder(this.poId);
    }
  }

  /**
   * Load existing purchase order
   */
  private loadPurchaseOrder(id: string): void {
    this.poService.getPurchaseOrderById(id).subscribe({
      next: (po) => {
        // Find matching supplier from loaded suppliers array
        const matchingSupplier = this.suppliers.find(s => s.id === po.supplierId);

        // Find matching option objects for autocomplete fields
        const paymentTermsValue = po.paymentTerms || 'NET30';
        const paymentTermsOption = this.paymentTermsOptions.find(pt => pt.value === paymentTermsValue);
        const currencyOption = this.currencyOptions.find(c => c.value === 'INR');
        const priorityOption = this.priorityOptions.find(p => p.value === 'MEDIUM');

        this.form.patchValue({
          supplierId: matchingSupplier || po.supplierId,
          deliveryDate: po.deliveryDate,
          paymentTerms: paymentTermsOption?.value || 'NET30',
          currency: currencyOption?.value || 'INR',
          priority: priorityOption?.value || 'MEDIUM',
          notes: po.notes,
          taxRate: po.taxPercentage || 0,
          discountPercentage: po.discountPercentage || 0
        });

        const linesArray = this.linesArray;
        linesArray.clear();
        po.lines?.forEach(line => {
          // Find the matching part from loaded parts array
          const matchingPart = this.parts.find(p => p.id === line.partId);
          linesArray.push(this.fb.group({
            partId: [matchingPart || line.partId, Validators.required],
            quantity: [line.quantity, [Validators.required, Validators.min(1)]],
            unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]]
          }));
        });
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase order'
        });
        console.error('Error loading purchase order:', error);
      }
    });
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
   * Load parts for autocomplete
   */
  private loadParts(): void {
    this.partService.getActiveParts().subscribe({
      next: (parts) => {
        this.parts = Array.isArray(parts) ? parts : [];
        this.filteredParts = this.parts;
      },
      error: (error) => {
        console.error('Error loading parts:', error);
      }
    });
  }

  /**
   * Create form group
   */
  private createForm(): FormGroup {
    return this.fb.group({
      supplierId: ['', Validators.required],
      deliveryDate: ['', Validators.required],
      paymentTerms: ['NET30', Validators.required],
      currency: ['INR', Validators.required],
      priority: ['MEDIUM', Validators.required],
      taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      discountPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
      notes: [''],
      lines: this.fb.array([this.createLineItem()])
    });
  }

  /**
   * Create a single line item
   */
  private createLineItem(): FormGroup {
    return this.fb.group({
      partId: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(1)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    });
  }

  /**
   * Get lines form array
   */
  get linesArray(): FormArray {
    return this.form.get('lines') as FormArray;
  }

  /**
   * Add line item
   */
  addLineItem(): void {
    this.linesArray.push(this.createLineItem());
  }

  /**
   * Remove line item
   */
  removeLineItem(index: number): void {
    if (this.linesArray.length > 1) {
      this.linesArray.removeAt(index);
    } else {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'At least one line item is required'
      });
    }
  }

  /**
   * Get part unit name for a given partId
   */
  getPartUnit(partId: any): string {
    const part = this.parts.find(p => p.id === (typeof partId === 'string' ? partId : partId?.id));
    return part?.unitName || '-';
  }

  /**
   * Get part name for a given partId
   */
  getPartName(partId: any): string {
    const part = this.parts.find(p => p.id === (typeof partId === 'string' ? partId : partId?.id));
    return part?.name || '-';
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
   * Calculate subtotal
   */
  getSubtotal(): number {
    return this.linesArray.controls.reduce((total, line) => {
      return total + this.getLineTotal(this.linesArray.controls.indexOf(line));
    }, 0);
  }

  /**
   * Calculate tax amount
   */
  getTaxAmount(): number {
    const subtotal = this.getSubtotal();
    const taxRate = this.form.get('taxRate')?.value || 0;
    return (subtotal * taxRate) / 100;
  }

  /**
   * Calculate discount amount
   */
  getDiscountAmount(): number {
    const subtotal = this.getSubtotal();
    const discountPercentage = this.form.get('discountPercentage')?.value || 0;
    return (subtotal * discountPercentage) / 100;
  }

  /**
   * Calculate grand total
   */
  getGrandTotal(): number {
    const subtotal = this.getSubtotal();
    const tax = this.getTaxAmount();
    const discount = this.getDiscountAmount();
    return subtotal + tax - discount;
  }

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    const currency = this.form.get('currency')?.value || 'INR';
    const currencyMap = { INR: 'en-IN', USD: 'en-US', EUR: 'de-DE', GBP: 'en-GB' };
    const currencyCode = currency;
    const locale = currencyMap[currency as keyof typeof currencyMap] || 'en-IN';

    return new Intl.NumberFormat(locale, {
      style: 'currency',
      currency: currencyCode
    }).format(value);
  }

  /**
   * Filter suppliers
   */
  onSupplierFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredSuppliers = this.suppliers.filter(supplier =>
      supplier.name.toLowerCase().includes(query) ||
      supplier.code.toLowerCase().includes(query)
    );
  }

  /**
   * Filter parts
   */
  onPartFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredParts = this.parts.filter(part =>
      part.name.toLowerCase().includes(query) ||
      part.sku.toLowerCase().includes(query) ||
      part.partNumber.toLowerCase().includes(query)
    );
  }

  /**
   * Filter payment terms
   */
  onPaymentTermsFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPaymentTerms = this.paymentTermsOptions.filter(term =>
      term.label.toLowerCase().includes(query) || term.value.toLowerCase().includes(query)
    );
  }

  /**
   * Filter currencies
   */
  onCurrencyFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredCurrencies = this.currencyOptions.filter(currency =>
      currency.label.toLowerCase().includes(query) || currency.value.toLowerCase().includes(query)
    );
  }

  /**
   * Filter priorities
   */
  onPriorityFilter(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPriorities = this.priorityOptions.filter(priority =>
      priority.label.toLowerCase().includes(query) || priority.value.toLowerCase().includes(query)
    );
  }

  /**
   * Handle form submission
   */
  onSubmit(): void {
    if (this.form.invalid || this.linesArray.length === 0) {
      this.markFormGroupTouched(this.form);
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill all required fields and add at least one line item'
      });
      return;
    }

    this.isSubmitting = true;

    if (this.isEditing && this.poId) {
      this.updatePurchaseOrder();
    } else {
      this.createPurchaseOrder();
    }
  }

  /**
   * Create purchase order
   */
  private createPurchaseOrder(): void {
    const lineItems = this.linesArray.value.map((line: any) => ({
      partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
      quantity: line.quantity,
      unitPrice: line.unitPrice
    }));

    const supplierId = this.form.value.supplierId;
    const request = {
      supplierId: typeof supplierId === 'string' ? supplierId : supplierId.id,
      deliveryDate: this.form.value.deliveryDate,
      taxPercentage: this.form.value.taxRate || 0,
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
        setTimeout(() => {
          this.router.navigate(['/procurement/purchase-orders']);
        }, 1500);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to create purchase order'
        });
        console.error('Error:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Update purchase order
   */
  private updatePurchaseOrder(): void {
    if (!this.poId) return;

    const lineItems = this.linesArray.value.map((line: any) => ({
      partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
      quantity: line.quantity,
      unitPrice: line.unitPrice
    }));

    const supplierId = this.form.value.supplierId;
    const request = {
      id: this.poId,
      supplierId: typeof supplierId === 'string' ? supplierId : supplierId.id,
      deliveryDate: this.form.value.deliveryDate,
      taxPercentage: this.form.value.taxRate || 0,
      discountPercentage: this.form.value.discountPercentage || 0,
      notes: this.form.value.notes,
      lineItems
    };

    this.poService.updatePurchaseOrder(this.poId, request).subscribe({
      next: (po) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Success',
          detail: `Purchase Order '${po.poNumber}' updated successfully`
        });
        setTimeout(() => {
          this.router.navigate(['/procurement/purchase-orders']);
        }, 1500);
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: error?.error?.message || 'Failed to update purchase order'
        });
        console.error('Error:', error);
        this.isSubmitting = false;
      }
    });
  }

  /**
   * Cancel and go back
   */
  onCancel(): void {
    this.router.navigate(['/procurement/purchase-orders']);
  }

  /**
   * Mark form as touched
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
   * Get error message for field
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
    if (errors['min']) {
      return `${this.formatFieldName(fieldName)} must be at least ${errors['min'].min}`;
    }
    if (errors['max']) {
      return `${this.formatFieldName(fieldName)} cannot exceed ${errors['max'].max}`;
    }

    return 'Invalid input';
  }

  /**
   * Format field name
   */
  private formatFieldName(fieldName: string): string {
    return fieldName
      .replace(/([A-Z])/g, ' $1')
      .replace(/^./, str => str.toUpperCase())
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
   * Get page title
   */
  getPageTitle(): string {
    return this.isEditing ? 'Edit Purchase Order' : 'Create New Purchase Order';
  }
}
