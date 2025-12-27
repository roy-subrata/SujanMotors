import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { MessageService } from 'primeng/api';
import { PurchaseReturnService, PurchaseReturnResponse } from '../../services/purchase-return.service';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';

@Component({
  selector: 'app-purchase-returns-form',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    Select,
    AutoCompleteModule,
    TableModule,
    CardModule,
    ToastModule,
    TooltipModule,
    DialogModule,
    CheckboxModule
  ],
  templateUrl: './purchase-returns-form.component.html',
  styleUrls: ['./purchase-returns-form.component.css'],
  providers: [MessageService]
})
export class PurchaseReturnsFormComponent implements OnInit {
  form: FormGroup;
  isEditing = false;
  isSubmitting = false;
  returnId: string | null = null;
  filteredPurchaseOrders: PurchaseOrderResponse[] = [];
  purchaseOrders: PurchaseOrderResponse[] = [];
  parts: PartResponse[] = [];
  filteredParts: PartResponse[] = [];
  availablePOItems: any[] = [];
  showItemSelectionDialog = false;
  selectedPOItems: any[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;

  conditionOptions = [
    { label: 'Unopened', value: 'UNOPENED' },
    { label: 'Opened', value: 'OPENED' },
    { label: 'Damaged', value: 'DAMAGED' },
    { label: 'Defective', value: 'DEFECTIVE' }
  ];

  reasonOptions = [
    { label: 'Defective', value: 'Defective' },
    { label: 'Damaged', value: 'Damaged' },
    { label: 'Wrong Item', value: 'Wrong Item' },
    { label: 'Quality Issue', value: 'Quality Issue' },
    { label: 'Not Needed', value: 'Not Needed' },
    { label: 'Overstock', value: 'Overstock' },
    { label: 'Other', value: 'Other' }
  ];

  private readonly prService = inject(PurchaseReturnService);
  private readonly poService = inject(PurchaseOrderService);
  private readonly partService = inject(PartService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.loadPurchaseOrders();
    this.loadParts();
    this.checkEditMode();
  }

  /**
   * Create form group
   */
  private createForm(): FormGroup {
    return this.fb.group({
      purchaseOrderId: ['', Validators.required],
      returnDate: [new Date(), Validators.required],
      reason: ['', Validators.required],
      notes: [''],
      lineItems: this.fb.array([])
    });
  }

  /**
   * Get line items form array
   */
  get lineItemsArray(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  /**
   * Load purchase orders from API
   */
  private loadPurchaseOrders(): void {
    this.poService.getAllPurchaseOrders().subscribe({
      next: (pos) => {
        this.purchaseOrders = Array.isArray(pos) ? pos : [];
        this.filteredPurchaseOrders = this.purchaseOrders;
      },
      error: (error) => {
        console.error('Error loading purchase orders:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase orders'
        });
      }
    });
  }

  /**
   * Load parts from API
   */
  private loadParts(): void {
    this.partService.getActiveParts().subscribe({
      next: (parts) => {
        this.parts = Array.isArray(parts) ? parts : [];
        this.filteredParts = this.parts;
      },
      error: (error) => {
        console.error('Error loading parts:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load parts'
        });
      }
    });
  }

  /**
   * Check if editing existing return
   */
  private checkEditMode(): void {
    this.returnId = this.route.snapshot.queryParamMap.get('id');
    if (this.returnId) {
      this.isEditing = true;
      this.loadPurchaseReturn(this.returnId);
    }
  }

  /**
   * Load existing purchase return
   */
  private loadPurchaseReturn(id: string): void {
    this.prService.getPurchaseReturnById(id).subscribe({
      next: (pr) => {
        const matchingPO = this.purchaseOrders.find(po => po.id === pr.purchaseOrderId);

        this.form.patchValue({
          purchaseOrderId: matchingPO || pr.purchaseOrderId,
          returnDate: new Date(pr.returnDate),
          reason: pr.reason,
          notes: pr.notes
        });

        // Load line items
        this.lineItemsArray.clear();
        if (pr.lines && pr.lines.length > 0) {
          pr.lines.forEach(line => {
            this.lineItemsArray.push(this.createLineItem(line));
          });
        }
      },
      error: (error) => {
        console.error('Error loading purchase return:', error);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase return'
        });
      }
    });
  }

  /**
   * Create line item form group
   */
  private createLineItem(lineData?: any): FormGroup {
    // Find the part object if we have a partId
    let partObj = null;
    if (lineData?.partId) {
      partObj = this.parts.find(p => p.id === lineData.partId);
    }

    return this.fb.group({
      id: [lineData?.id || ''],
      purchaseOrderLineId: [lineData?.purchaseOrderLineId || ''],
      part: [partObj, Validators.required],
      partId: [lineData?.partId || '', Validators.required],
      quantity: [lineData?.quantity || 1, [Validators.required, Validators.min(1)]],
      rejectedQuantity: [lineData?.rejectedQuantity || 0, Validators.min(0)],
      unitPrice: [lineData?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      condition: [lineData?.condition || 'UNOPENED', Validators.required],
      notes: [lineData?.notes || '']
    });
  }

  /**
   * Add new line item
   */
  addLineItem(): void {
    this.lineItemsArray.push(this.createLineItem());
  }

  /**
   * Remove line item
   */
  removeLineItem(index: number): void {
    if (this.lineItemsArray.length > 1) {
      this.lineItemsArray.removeAt(index);
    } else {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'Purchase return must have at least one line item'
      });
    }
  }

  /**
   * Filter purchase orders
   */
  filterPurchaseOrders(event: any): void {
    const filtered: PurchaseOrderResponse[] = [];
    const query = event.query;

    if (query && query.trim() !== '') {
      const queryLower = query.toLowerCase();
      for (const po of this.purchaseOrders) {
        if (
          po.poNumber.toLowerCase().includes(queryLower) ||
          po.supplierName?.toLowerCase().includes(queryLower)
        ) {
          filtered.push(po);
        }
      }
    } else {
      for (const po of this.purchaseOrders) {
        filtered.push(po);
      }
    }

    this.filteredPurchaseOrders = filtered.slice(0, 10);
  }

  /**
   * Handle PO selection change
   */
  onPurchaseOrderSelected(event: any): void {
    console.log('PO Selected Event:', event);

    if (event && event.id) {
      console.log('Fetching PO details for ID:', event.id);

      // Load PO details and store available items
      this.poService.getPurchaseOrderById(event.id).subscribe({
        next: (po) => {
          console.log('PO Response:', po);
          console.log('PO Lines:', po.lines);

          // Store the complete PO object
          this.selectedPurchaseOrder = po;

          // Store available items for selection
          this.availablePOItems = po.lines || [];

          console.log('Available PO Items:', this.availablePOItems);
          console.log('Available PO Items Length:', this.availablePOItems.length);

          // Clear existing items
          this.lineItemsArray.clear();

          // Don't auto-populate - let user choose how to add items
          this.messageService.add({
            severity: 'info',
            summary: 'Purchase Order Loaded',
            detail: `${this.availablePOItems.length} items available from ${po.supplierName || 'supplier'}. Use "Add Items" or "Add All Items" to continue.`,
            life: 5000
          });
        },
        error: (error) => {
          console.error('Error loading PO details:', error);
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: 'Failed to load PO details'
          });
        }
      });
    } else {
      console.log('Invalid event or missing ID:', event);
    }
  }

  /**
   * Add all items from selected PO
   */
  addAllItemsFromPO(): void {
    const poId = this.form.get('purchaseOrderId')?.value;
    if (!poId || !this.availablePOItems.length) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Purchase Order',
        detail: 'Please select a purchase order first'
      });
      return;
    }

    this.lineItemsArray.clear();
    this.availablePOItems.forEach(poLine => {
      this.addPOLineItem(poLine);
    });

    this.messageService.add({
      severity: 'success',
      summary: 'Items Added',
      detail: `${this.availablePOItems.length} items added to return`
    });
  }

  /**
   * Show dialog to select specific items
   */
  showSelectItemsDialog(): void {
    const poId = this.form.get('purchaseOrderId')?.value;
    if (!poId || !this.availablePOItems.length) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Purchase Order',
        detail: 'Please select a purchase order first'
      });
      return;
    }

    this.selectedPOItems = [];
    this.showItemSelectionDialog = true;
  }

  /**
   * Add selected items from dialog
   */
  confirmItemSelection(): void {
    if (this.selectedPOItems.length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Items Selected',
        detail: 'Please select at least one item'
      });
      return;
    }

    this.lineItemsArray.clear();
    this.selectedPOItems.forEach(item => {
      this.addPOLineItem(item);
    });

    this.showItemSelectionDialog = false;
    this.messageService.add({
      severity: 'success',
      summary: 'Items Added',
      detail: `${this.selectedPOItems.length} items added to return`
    });
  }

  /**
   * Cancel item selection
   */
  cancelItemSelection(): void {
    this.showItemSelectionDialog = false;
    this.selectedPOItems = [];
  }

  /**
   * Helper method to add a PO line item to the form
   */
  private addPOLineItem(poLine: any): void {
    const partObj = this.parts.find(p => p.id === poLine.partId);

    const lineItem = this.fb.group({
      id: [''],
      purchaseOrderLineId: [poLine.id, Validators.required],
      part: [partObj, Validators.required],
      partId: [poLine.partId, Validators.required],
      quantity: [poLine.quantity, [Validators.required, Validators.min(1)]],
      rejectedQuantity: [0, Validators.min(0)],
      unitPrice: [poLine.unitPrice || 0, [Validators.required, Validators.min(0)]],
      condition: ['UNOPENED', Validators.required],
      notes: ['']
    });
    this.lineItemsArray.push(lineItem);
  }

  /**
   * Filter parts for autocomplete
   */
  filterParts(event: any, rowIndex: number): void {
    const filtered: PartResponse[] = [];
    const query = event.query;

    if (query && query.trim() !== '') {
      const queryLower = query.toLowerCase();
      for (const part of this.parts) {
        if (
          part.name.toLowerCase().includes(queryLower) ||
          part.partNumber.toLowerCase().includes(queryLower) ||
          part.sku.toLowerCase().includes(queryLower)
        ) {
          filtered.push(part);
        }
      }
    } else {
      for (const part of this.parts) {
        filtered.push(part);
      }
    }

    this.filteredParts = filtered.slice(0, 10);
  }

  /**
   * Handle part selection
   */
  onPartSelected(event: any, rowIndex: number): void {
    if (event && event.id) {
      const lineItemControl = this.lineItemsArray.at(rowIndex);
      if (lineItemControl) {
        lineItemControl.patchValue({
          partId: event.id,
          unitPrice: event.costPrice || 0
        });
      }
    }
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.messageService.add({
        severity: 'error',
        summary: 'Validation Error',
        detail: 'Please fill all required fields'
      });
      return;
    }

    this.isSubmitting = true;
    const formValue = this.form.value;
    const poId = typeof formValue.purchaseOrderId === 'string'
      ? formValue.purchaseOrderId
      : formValue.purchaseOrderId?.id;
    const supplierId = this.purchaseOrders.find(po => po.id === poId)?.supplierId;

    if (!poId || !supplierId) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Invalid purchase order selected'
      });
      this.isSubmitting = false;
      return;
    }

    const request = {
      purchaseOrderId: poId,
      supplierId,
      returnDate: new Date(formValue.returnDate).toISOString(),
      reason: formValue.reason,
      notes: formValue.notes,
      lines: formValue.lineItems.map((line: any) => ({
        purchaseOrderLineId: line.purchaseOrderLineId || '',
        partId: line.partId,
        quantity: line.quantity,
        rejectedQuantity: line.rejectedQuantity || 0,
        unitPrice: line.unitPrice || 0,
        condition: line.condition,
        notes: line.notes
      }))
    };

    if (this.isEditing && this.returnId) {
      this.prService.updatePurchaseReturn(this.returnId, { ...request, id: this.returnId }).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase return updated successfully'
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update purchase return'
          });
          console.error('Error updating purchase return:', error);
          this.isSubmitting = false;
        }
      });
    } else {
      this.prService.createPurchaseReturn(request).subscribe({
        next: () => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: 'Purchase return created successfully'
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create purchase return'
          });
          console.error('Error creating purchase return:', error);
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Cancel form
   */
  onCancel(): void {
    this.router.navigate(['/procurement/purchase-returns']);
  }

  /**
   * Get part name by part ID
   */
  getPartName(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part ? `${part.partNumber} - ${part.name}` : 'Unknown Part';
  }

  /**
   * Format date for display
   */
  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-IN');
  }
}
