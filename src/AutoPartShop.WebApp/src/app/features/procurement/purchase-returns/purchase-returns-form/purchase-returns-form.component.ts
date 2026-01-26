import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, Validators, FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TextareaModule } from 'primeng/textarea';
import { Select } from 'primeng/select';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { DatePickerModule } from 'primeng/datepicker';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { MessageService } from 'primeng/api';
import { PurchaseReturnService, PurchaseReturnResponse, AvailableLotForReturn } from '../../services/purchase-return.service';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';
import { CurrencyService } from '../../../../shared/services/currency.service';

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
    TextareaModule,
    Select,
    AutoCompleteModule,
    DatePickerModule,
    TableModule,
    CardModule,
    ToastModule,
    TooltipModule,
    DialogModule,
    CheckboxModule,
    TagModule
  ],
  templateUrl: './purchase-returns-form.component.html',
  styleUrls: ['./purchase-returns-form.component.css'],
  providers: [MessageService]
})
export class PurchaseReturnsFormComponent implements OnInit {
  form: FormGroup;
  isEditing = false;
  isViewMode = false;
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

  // View mode properties
  currentReturn: PurchaseReturnResponse | null = null;

  // Lot selection - map of partId to available lots
  availableLotsMap: Map<string, AvailableLotForReturn[]> = new Map();
  loadingLotsMap: Map<string, boolean> = new Map();

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
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  constructor() {
    this.form = this.createForm();
  }

  /**
   * Get current currency code
   */
  get currencyCode(): string {
    return this.currencyService.selectedCurrency() || 'BDT';
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
        // Only show CONFIRMED, PARTIAL, or DELIVERED orders (orders eligible for returns)
        const validOrders = (Array.isArray(pos) ? pos : []).filter(po =>
          po.status === 'CONFIRMED' || po.status === 'PARTIAL' || po.status === 'DELIVERED'
        );
        this.purchaseOrders = validOrders;
        this.filteredPurchaseOrders = validOrders;
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
   * Check if editing or viewing existing return
   */
  private checkEditMode(): void {
    this.returnId = this.route.snapshot.queryParamMap.get('id');
    const url = this.router.url;

    if (this.returnId) {
      // Check if it's view mode
      if (url.includes('/view')) {
        this.isViewMode = true;
        this.isEditing = false;
      } else {
        this.isEditing = true;
        this.isViewMode = false;
      }
      this.loadPurchaseReturn(this.returnId);
    }
  }

  /**
   * Load existing purchase return
   */
  private loadPurchaseReturn(id: string): void {
    this.prService.getPurchaseReturnById(id).subscribe({
      next: (pr) => {
        // Store current return for view mode
        this.currentReturn = pr;

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

        // Disable form in view mode
        if (this.isViewMode) {
          this.form.disable();
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
      // Load available lots for this part
      this.loadAvailableLotsForPart(lineData.partId);
    }

    return this.fb.group({
      id: [lineData?.id || ''],
      purchaseOrderLineId: [lineData?.purchaseOrderLineId || ''],
      part: [partObj, Validators.required],
      partId: [lineData?.partId || '', Validators.required],
      stockLotId: [lineData?.stockLotId || null],  // Optional: specific lot to return from
      quantity: [lineData?.quantity || 1, [Validators.required, Validators.min(1)]],
      rejectedQuantity: [lineData?.rejectedQuantity || 0, Validators.min(0)],
      unitPrice: [lineData?.unitPrice || 0, [Validators.required, Validators.min(0)]],
      condition: [lineData?.condition || 'UNOPENED', Validators.required],
      notes: [lineData?.notes || '']
    });
  }

  /**
   * Load available lots for a part
   */
  loadAvailableLotsForPart(partId: string): void {
    if (!partId || this.availableLotsMap.has(partId)) {
      return;  // Already loaded or no partId
    }

    // Get supplier ID from selected PO
    const supplierId = this.selectedPurchaseOrder?.supplierId;

    this.loadingLotsMap.set(partId, true);
    this.prService.getAvailableLotsForReturn(partId, supplierId).subscribe({
      next: (lots) => {
        this.availableLotsMap.set(partId, lots);
        this.loadingLotsMap.set(partId, false);
      },
      error: (error) => {
        console.error('Error loading available lots for part:', partId, error);
        this.availableLotsMap.set(partId, []);
        this.loadingLotsMap.set(partId, false);
      }
    });
  }

  /**
   * Get available lots for a part (for dropdown)
   */
  getAvailableLotsForPart(partId: string): AvailableLotForReturn[] {
    return this.availableLotsMap.get(partId) || [];
  }

  /**
   * Check if lots are loading for a part
   */
  isLoadingLots(partId: string): boolean {
    return this.loadingLotsMap.get(partId) || false;
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

    // Load available lots for this part
    if (poLine.partId) {
      this.loadAvailableLotsForPart(poLine.partId);
    }

    const lineItem = this.fb.group({
      id: [''],
      purchaseOrderLineId: [poLine.id, Validators.required],
      part: [partObj, Validators.required],
      partId: [poLine.partId, Validators.required],
      stockLotId: [null],  // Optional: specific lot to return from
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
          unitPrice: event.costPrice || 0,
          stockLotId: null  // Reset lot selection when part changes
        });

        // Load available lots for the selected part
        this.loadAvailableLotsForPart(event.id);
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
        stockLotId: line.stockLotId || null,  // Optional: specific lot to return from
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

  /**
   * Get status severity for display
   */
  getStatusSeverity(status: string): string {
    switch (status?.toUpperCase()) {
      case 'PENDING': return 'warning';
      case 'APPROVED': return 'info';
      case 'RETURNED': return 'primary';
      case 'RECEIVED': return 'success';
      case 'CREDITED': return 'success';
      case 'REJECTED': return 'danger';
      default: return 'secondary';
    }
  }

  /**
   * Get total quantity of all line items
   */
  getTotalQuantity(): number {
    return this.lineItemsArray.controls.reduce((sum, item) => {
      return sum + (item.get('quantity')?.value || 0);
    }, 0);
  }

  /**
   * Get total refund amount
   */
  getTotalRefundAmount(): number {
    return this.lineItemsArray.controls.reduce((sum, item) => {
      const qty = item.get('quantity')?.value || 0;
      const rejected = item.get('rejectedQuantity')?.value || 0;
      const price = item.get('unitPrice')?.value || 0;
      return sum + ((qty - rejected) * price);
    }, 0);
  }

  /**
   * Print the purchase return
   */
  printReturn(): void {
    if (!this.currentReturn) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Warning',
        detail: 'No purchase return data available to print'
      });
      return;
    }

    const printContent = this.generatePrintContent();
    const printWindow = window.open('', '_blank', 'width=800,height=600');
    if (printWindow) {
      printWindow.document.write(printContent);
      printWindow.document.close();
      printWindow.focus();
      setTimeout(() => {
        printWindow.print();
        printWindow.close();
      }, 250);
    }
  }

  /**
   * Generate print content HTML
   */
  private generatePrintContent(): string {
    const pr = this.currentReturn!;
    const lineItemsHtml = pr.lines.map(line => `
      <tr>
        <td style="padding: 8px; border: 1px solid #ddd;">${line.partName || line.partSku || 'N/A'}</td>
        <td style="padding: 8px; border: 1px solid #ddd; text-align: center;">${line.quantity}</td>
        <td style="padding: 8px; border: 1px solid #ddd; text-align: center;">${line.rejectedQuantity}</td>
        <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${this.currencyService.formatCurrency(line.unitPrice, this.currencyCode)}</td>
        <td style="padding: 8px; border: 1px solid #ddd; text-align: right;">${this.currencyService.formatCurrency(line.refundAmount, this.currencyCode)}</td>
        <td style="padding: 8px; border: 1px solid #ddd;">${line.condition}</td>
      </tr>
    `).join('');

    return `
      <!DOCTYPE html>
      <html>
      <head>
        <title>Purchase Return - ${pr.returnNumber}</title>
        <style>
          body { font-family: Arial, sans-serif; margin: 20px; color: #333; }
          .header { text-align: center; margin-bottom: 30px; border-bottom: 2px solid #333; padding-bottom: 20px; }
          .header h1 { margin: 0 0 10px 0; color: #1d4ed8; }
          .info-section { display: flex; justify-content: space-between; margin-bottom: 20px; }
          .info-block { width: 48%; }
          .info-block h3 { margin: 0 0 10px 0; color: #666; border-bottom: 1px solid #ddd; padding-bottom: 5px; }
          .info-row { display: flex; margin-bottom: 5px; }
          .info-label { font-weight: bold; width: 120px; }
          .info-value { flex: 1; }
          table { width: 100%; border-collapse: collapse; margin-top: 20px; }
          th { background: #f3f4f6; padding: 10px; border: 1px solid #ddd; text-align: left; }
          .totals { text-align: right; margin-top: 20px; }
          .total-row { display: flex; justify-content: flex-end; margin-bottom: 5px; }
          .total-label { font-weight: bold; margin-right: 20px; }
          .total-value { min-width: 120px; text-align: right; }
          .grand-total { font-size: 1.2em; border-top: 2px solid #333; padding-top: 10px; margin-top: 10px; }
          .footer { margin-top: 40px; border-top: 1px solid #ddd; padding-top: 20px; }
          .signature-section { display: flex; justify-content: space-between; margin-top: 60px; }
          .signature-box { width: 200px; text-align: center; }
          .signature-line { border-top: 1px solid #333; margin-top: 40px; padding-top: 5px; }
          .status-badge { display: inline-block; padding: 4px 12px; border-radius: 4px; font-weight: bold; }
          .status-pending { background: #fef3c7; color: #92400e; }
          .status-approved { background: #dbeafe; color: #1e40af; }
          .status-returned { background: #e0e7ff; color: #3730a3; }
          .status-received { background: #d1fae5; color: #065f46; }
          .status-credited { background: #d1fae5; color: #065f46; }
          .status-rejected { background: #fee2e2; color: #991b1b; }
          @media print { body { margin: 0; } }
        </style>
      </head>
      <body>
        <div class="header">
          <h1>Purchase Return</h1>
          <h2>${pr.returnNumber}</h2>
          <span class="status-badge status-${pr.status.toLowerCase()}">${pr.status}</span>
        </div>

        <div class="info-section">
          <div class="info-block">
            <h3>Return Details</h3>
            <div class="info-row"><span class="info-label">Return Number:</span><span class="info-value">${pr.returnNumber}</span></div>
            <div class="info-row"><span class="info-label">Return Date:</span><span class="info-value">${this.formatDate(pr.returnDate)}</span></div>
            <div class="info-row"><span class="info-label">Reason:</span><span class="info-value">${pr.reason}</span></div>
            <div class="info-row"><span class="info-label">Status:</span><span class="info-value">${pr.status}</span></div>
          </div>
          <div class="info-block">
            <h3>Supplier Details</h3>
            <div class="info-row"><span class="info-label">Supplier:</span><span class="info-value">${pr.supplierName || 'N/A'}</span></div>
            <div class="info-row"><span class="info-label">Supplier Code:</span><span class="info-value">${pr.supplierCode || 'N/A'}</span></div>
            <div class="info-row"><span class="info-label">PO Number:</span><span class="info-value">${pr.purchaseOrderNumber || 'N/A'}</span></div>
          </div>
        </div>

        <h3>Return Items</h3>
        <table>
          <thead>
            <tr>
              <th>Part</th>
              <th style="text-align: center;">Quantity</th>
              <th style="text-align: center;">Rejected</th>
              <th style="text-align: right;">Unit Price</th>
              <th style="text-align: right;">Refund Amount</th>
              <th>Condition</th>
            </tr>
          </thead>
          <tbody>
            ${lineItemsHtml}
          </tbody>
        </table>

        <div class="totals">
          <div class="total-row grand-total">
            <span class="total-label">Total Refund Amount:</span>
            <span class="total-value">${this.currencyService.formatCurrency(pr.refundAmount, this.currencyCode)}</span>
          </div>
          ${pr.creditNoteAmount > 0 ? `
          <div class="total-row">
            <span class="total-label">Credit Note Amount:</span>
            <span class="total-value">${this.currencyService.formatCurrency(pr.creditNoteAmount, this.currencyCode)}</span>
          </div>
          ` : ''}
        </div>

        ${pr.notes ? `
        <div class="footer">
          <h3>Notes</h3>
          <p>${pr.notes}</p>
        </div>
        ` : ''}

        <div class="signature-section">
          <div class="signature-box">
            <div class="signature-line">Prepared By</div>
          </div>
          <div class="signature-box">
            <div class="signature-line">Approved By</div>
          </div>
          <div class="signature-box">
            <div class="signature-line">Received By Supplier</div>
          </div>
        </div>

        <p style="text-align: center; margin-top: 30px; color: #666; font-size: 12px;">
          Printed on ${new Date().toLocaleString()}
        </p>
      </body>
      </html>
    `;
  }
}
