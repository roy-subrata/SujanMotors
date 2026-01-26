import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { TableModule } from 'primeng/table';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { PurchaseOrderService, PurchaseOrderResponse } from '../services/purchase-order.service';
import { GoodsReceiptService } from '../services/goods-receipt.service';
import { WarehouseService, WarehouseResponse } from '../../inventory/services/warehouse.service';
import { PartService, PartResponse } from '../../inventory/services/part.service';
import { CurrencyService } from '../../../shared/services/currency.service';

@Component({
  selector: 'app-goods-receipt-form',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    InputTextModule,
    InputNumberModule,
    AutoCompleteModule,
    TableModule,
    ToastModule
  ],
  providers: [MessageService],
  templateUrl: './goods-receipt-form.component.html',
  styleUrls: ['./goods-receipt-form.component.css']
})
export class GoodsReceiptFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly poService = inject(PurchaseOrderService);
  private readonly grnService = inject(GoodsReceiptService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly partService = inject(PartService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  form: FormGroup;
  selectedPO: PurchaseOrderResponse | null = null;
  filteredPOs: PurchaseOrderResponse[] = [];
  purchaseOrders: PurchaseOrderResponse[] = [];
  selectedWarehouse: WarehouseResponse | null = null;
  filteredWarehouses: WarehouseResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  parts: PartResponse[] = [];
  conditions = [
    { label: 'Good', value: 'GOOD' },
    { label: 'Acceptable', value: 'ACCEPTABLE' },
    { label: 'Damaged', value: 'DAMAGED' },
    { label: 'Defective', value: 'DEFECTIVE' }
  ];
  isSubmitting = false;
  isEditing = false;
  grnId: string | null = null;

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.loadPurchaseOrders();
    this.loadWarehouses();
    this.loadParts();
    this.checkEditMode();
  }

  /**
   * Check if editing existing GRN
   */
  private checkEditMode(): void {
    this.grnId = this.route.snapshot.queryParamMap.get('id');
    if (this.grnId) {
      this.isEditing = true;
      this.loadGoodsReceipt(this.grnId);
    }
  }

  /**
   * Load existing goods receipt for editing
   */
  private loadGoodsReceipt(id: string): void {
    this.grnService.getGoodsReceiptById(id).subscribe({
      next: (grn) => {
        // Find matching PO and warehouse
        const matchingPO = this.purchaseOrders.find(po => po.id === grn.purchaseOrderId);
        const matchingWarehouse = this.warehouses.find(w => w.id === grn.warehouseId);

        this.selectedPO = matchingPO || null;
        this.selectedWarehouse = matchingWarehouse || null;

        this.form.patchValue({
          warehouseId: matchingWarehouse || grn.warehouseId,
          receivedDate: grn.receivedDate,
          // Delivery Information
          deliveryDate: grn.deliveryDate ? grn.deliveryDate.split('T')[0] : '',
          deliveryReference: grn.deliveryReference,
          carrierName: grn.carrierName,
          driverName: grn.driverName,
          deliveryNotes: grn.deliveryNotes
        });

        // Populate line items (get data from PO for ordered and remaining quantities)
        const linesArray = this.lineItemsArray;
        linesArray.clear();

        grn.lines?.forEach(line => {
          // Find matching PO line to get ordered and remaining quantities
          const poLine = matchingPO?.lines?.find(pl => pl.partId === line.partId);
          const orderedQty = poLine?.quantity || 0;
          const alreadyReceived = poLine?.receivedQuantity || 0;
          const remainingQty = poLine?.remainingQuantity || (orderedQty - alreadyReceived);

          linesArray.push(
            this.fb.group({
              partId: [line.partId, Validators.required],
              orderedQuantity: [orderedQty],
              receivedQuantity: [alreadyReceived],
              remainingQuantity: [remainingQty],
              receivingQuantity: [line.receivedQuantity, [Validators.required, Validators.min(1), Validators.max(remainingQty + line.receivedQuantity)]],
              condition: [line.condition, Validators.required],
              notes: [line.notes],
              hasDiscrepancy: [line.hasDiscrepancy],
              // Cost Information
              unitCost: [line.unitCost || 0, [Validators.required, Validators.min(0)]],
              currency: [line.currency || this.currencyService.selectedCurrency() || 'BDT', Validators.required],
              unitId: [line.unitId || '']
            })
          );
        });
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load goods receipt'
        });
        console.error('Error loading GRN:', error);
      }
    });
  }

  /**
   * Create form group
   */
  private createForm(): FormGroup {
    return this.fb.group({
      warehouseId: ['', Validators.required],
      receivedDate: [new Date().toISOString().split('T')[0], Validators.required],
      // Delivery Information
      deliveryDate: [''],
      deliveryReference: [''],
      carrierName: [''],
      driverName: [''],
      deliveryNotes: [''],
      lineItems: this.fb.array([])
    });
  }

  /**
   * Get line items array
   */
  get lineItemsArray(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  /**
   * Load purchase orders
   * Only show CONFIRMED and PARTIAL orders (ready to receive goods)
   */
  private loadPurchaseOrders(): void {
    this.poService.getAllPurchaseOrders().subscribe({
      next: (pos) => {
        // Only show purchase orders that are approved and ready to receive goods
        this.purchaseOrders = pos.filter(po =>
          po.status === 'CONFIRMED' || po.status === 'PARTIAL'
        );
        this.filteredPOs = this.purchaseOrders;
      },
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load purchase orders'
        });
        console.error('Error loading POs:', error);
      }
    });
  }

  /**
   * Load warehouses
   */
  private loadWarehouses(): void {
    this.warehouseService.getAllWarehouses().subscribe({
      next: (warehouses: WarehouseResponse[]) => {
        this.warehouses = warehouses;
        this.filteredWarehouses = warehouses;
      },
      error: (error: any) => {
        console.error('Error loading warehouses:', error);
      }
    });
  }

  /**
   * Load parts for unit information
   */
  private loadParts(): void {
    this.partService.getActiveParts().subscribe({
      next: (parts: PartResponse[]) => {
        this.parts = Array.isArray(parts) ? parts : [];
      },
      error: (error: any) => {
        console.error('Error loading parts:', error);
      }
    });
  }

  /**
   * Filter warehouses
   */
  filterWarehouses(event: { query: string }): void {
    const filtered = this.warehouses.filter(warehouse =>
      warehouse.name.toLowerCase().includes(event.query.toLowerCase())
    );
    this.filteredWarehouses = filtered;
  }

  /**
   * Handle warehouse selection
   */
  onWarehouseSelected(event: any): void {
    const warehouse = event.value as WarehouseResponse;
    this.selectedWarehouse = warehouse;
    this.form.patchValue({
      warehouseId: warehouse.id
    });
  }

  /**
   * Filter purchase orders
   */
  filterPOs(event: { query: string }): void {
    const filtered = this.purchaseOrders.filter(po =>
      po.poNumber.toLowerCase().includes(event.query.toLowerCase())
    );
    this.filteredPOs = filtered;
  }

  /**
   * Handle PO selection
   */
  onPOSelected(event: any): void {
    this.selectedPO = event.value as PurchaseOrderResponse;
    this.populateLineItems();
  }

  /**
   * Populate line items from selected PO
   */
  private populateLineItems(): void {
    if (!this.selectedPO) return;

    const linesArray = this.lineItemsArray;
    linesArray.clear();

    this.selectedPO.lines?.forEach(line => {
      const remainingQty = line.remainingQuantity || (line.quantity - line.receivedQuantity);

      linesArray.push(
        this.fb.group({
          partId: [line.partId, Validators.required],
          orderedQuantity: [line.quantity],
          receivedQuantity: [line.receivedQuantity || 0],
          remainingQuantity: [remainingQty],
          receivingQuantity: [0, [Validators.required, Validators.min(1), Validators.max(remainingQty)]],
          condition: ['GOOD', Validators.required],
          notes: [''],
          hasDiscrepancy: [false],
          // Cost Information
          unitCost: [line.unitPrice || 0, [Validators.required, Validators.min(0)]],
          currency: [this.currencyService.selectedCurrency() || 'BDT', Validators.required],
          unitId: ['']
        })
      );
    });
  }

  /**
   * Get part unit name for a given partId
   */
  getPartUnit(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part?.unitName || '-';
  }

  /**
   * Get part name for a given partId
   */
  getPartName(partId: string): string {
    const part = this.parts.find(p => p.id === partId);
    return part?.name || '-';
  }

  /**
   * Get total ordered quantity
   */
  getTotalOrderedQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + (line.orderedQuantity || 0), 0);
  }

  /**
   * Get total receiving quantity (for current GRN)
   */
  getTotalReceivingQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + (line.receivingQuantity || 0), 0);
  }

  /**
   * Get total received quantity (already received)
   */
  getTotalReceivedQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + (line.receivedQuantity || 0), 0);
  }

  /**
   * Get discrepancy count
   */
  getDiscrepancyCount(): number {
    return this.lineItemsArray.value.filter((line: any) => line.hasDiscrepancy).length;
  }

  /**
   * Get total cost for all items (receivingQuantity * unitCost)
   */
  getTotalCostAllItems(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => {
      const lineTotal = (line.receivingQuantity || 0) * (line.unitCost || 0);
      return sum + lineTotal;
    }, 0);
  }

  /**
   * Get average unit cost across all line items
   */
  getAverageUnitCost(): number {
    const lines = this.lineItemsArray.value;
    if (lines.length === 0) return 0;

    const totalCost = this.getTotalCostAllItems();
    const totalQuantity = lines.reduce((sum: number, line: any) => sum + (line.receivingQuantity || 0), 0);

    return totalQuantity > 0 ? totalCost / totalQuantity : 0;
  }

  /**
   * Submit form
   */
  onSubmit(): void {
    if (!this.form.valid || !this.selectedPO) {
      this.messageService.add({
        severity: 'error',
        summary: 'Error',
        detail: 'Please fill all required fields'
      });
      return;
    }

    this.isSubmitting = true;

    // Process line items to convert empty unitId strings to null
    // and map receivingQuantity to receivedQuantity for API
    const processedLines = this.lineItemsArray.value.map((line: any) => ({
      partId: line.partId,
      receivedQuantity: line.receivingQuantity || 0,
      condition: line.condition,
      notes: line.notes || '',
      hasDiscrepancy: line.hasDiscrepancy || false,
      unitCost: line.unitCost || 0,
      currency: line.currency || 'INR',
      unitId: line.unitId && line.unitId.trim() !== '' ? line.unitId : null
    }));

    const request = {
      purchaseOrderId: this.selectedPO.id,
      warehouseId: this.form.value.warehouseId,
      receivedDate: this.form.value.receivedDate,
      // Delivery Information
      deliveryDate: this.form.value.deliveryDate || null,
      deliveryReference: this.form.value.deliveryReference || '',
      carrierName: this.form.value.carrierName || '',
      driverName: this.form.value.driverName || '',
      deliveryNotes: this.form.value.deliveryNotes || '',
      lines: processedLines
    };

    if (this.isEditing && this.grnId) {
      // Update existing GRN
      this.grnService.updateGoodsReceipt(this.grnId, request).subscribe({
        next: (grn) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Goods Receipt '${grn.grnNumber}' updated successfully`
          });
          this.router.navigate(['/procurement/goods-receipts']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to update goods receipt'
          });
          console.error('Error updating GRN:', error);
          this.isSubmitting = false;
        }
      });
    } else {
      // Create new GRN
      this.grnService.createGoodsReceipt(request).subscribe({
        next: (grn) => {
          this.messageService.add({
            severity: 'success',
            summary: 'Success',
            detail: `Goods Receipt '${grn.grnNumber}' created successfully`
          });
          this.router.navigate(['/procurement/goods-receipts']);
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: 'Error',
            detail: error?.error?.message || 'Failed to create goods receipt'
          });
          console.error('Error creating GRN:', error);
          this.isSubmitting = false;
        }
      });
    }
  }

  /**
   * Go back
   */
  goBack(): void {
    this.router.navigate(['/procurement/goods-receipts']);
  }

  /**
   * Check if field has error
   */
  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!(field && field.invalid && field.touched);
  }

  /**
   * Get error message
   */
  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return `${fieldName} is required`;
    }
    return '';
  }

  /**
   * Format currency
   */
  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR'
    }).format(value);
  }
}
