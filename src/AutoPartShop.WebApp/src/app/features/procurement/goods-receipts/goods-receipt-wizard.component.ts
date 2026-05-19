        // Summary helpers
        get totalCostAllItems() {
          return this.lineItems.reduce((sum, item) => sum + (Number(item.receivingQuantity) * Number(item.unitPrice)), 0);
        }
        get totalOrdered() {
          return this.lineItems.reduce((sum, item) => sum + Number(item.quantity), 0);
        }
        get totalReceived() {
          return this.lineItems.reduce((sum, item) => sum + Number(item.receivedQuantity), 0);
        }
        get totalReceivingNow() {
          return this.lineItems.reduce((sum, item) => sum + Number(item.receivingQuantity), 0);
        }
        get discrepancyCount() {
          return this.lineItems.filter(item => item.hasDiscrepancy).length;
        }
      // Per-item Setup Dialog
      showItemSetupDialog = false;
      selectedItem: any = null;

      openItemSetupDialog(item: any) {
        // Clone the item to avoid mutating before save
        this.selectedItem = { ...item };
        this.showItemSetupDialog = true;
      }

      closeItemSetupDialog() {
        this.showItemSetupDialog = false;
        this.selectedItem = null;
      }

      saveItemSetupDialog() {
        // Find the item in lineItems and update it
        const idx = this.lineItems.findIndex(i => i.partId === this.selectedItem.partId);
        if (idx !== -1) {
          this.lineItems[idx] = { ...this.selectedItem };
        }
        this.closeItemSetupDialog();
      }
    // Step 3 state and submission
    isSubmitting = false;
    submitError: string | null = null;
    submitSuccess: string | null = null;
    grnService = inject(require('../services/goods-receipt.service')).GoodsReceiptService;

    get reviewSummary() {
      return {
        po: this.selectedPO,
        warehouse: this.selectedWarehouse,
        receivedDate: this.formStep1.value.receivedDate,
        delivery: this.formStep1.value.showDelivery ? {
          deliveryDate: this.formStep1.value.deliveryDate,
          deliveryReference: this.formStep1.value.deliveryReference,
          carrierName: this.formStep1.value.carrierName,
          driverName: this.formStep1.value.driverName,
          deliveryNotes: this.formStep1.value.deliveryNotes
        } : null,
        items: this.lineItems
      };
    }

    submitGRN() {
      this.isSubmitting = true;
      this.submitError = null;
      this.submitSuccess = null;
      // Validate edge cases
      if (!this.selectedPO || !this.selectedWarehouse || this.lineItems.length === 0) {
        this.submitError = 'Missing required data.';
        this.isSubmitting = false;
        return;
      }
      if (this.lineItems.some(item => item.receivingQuantity < 0)) {
        this.submitError = 'Receiving quantity cannot be negative.';
        this.isSubmitting = false;
        return;
      }
      // Prepare request
      const request = {
        purchaseOrderId: this.selectedPO.id,
        warehouseId: this.selectedWarehouse.id,
        receivedDate: this.formStep1.value.receivedDate,
        deliveryDate: this.formStep1.value.showDelivery ? this.formStep1.value.deliveryDate : null,
        deliveryReference: this.formStep1.value.showDelivery ? this.formStep1.value.deliveryReference : '',
        carrierName: this.formStep1.value.showDelivery ? this.formStep1.value.carrierName : '',
        driverName: this.formStep1.value.showDelivery ? this.formStep1.value.driverName : '',
        deliveryNotes: this.formStep1.value.showDelivery ? this.formStep1.value.deliveryNotes : '',
        lines: this.lineItems.map(item => ({
          partId: item.partId,
          receivedQuantity: item.receivingQuantity,
          condition: item.condition,
          notes: '',
          hasDiscrepancy: item.hasDiscrepancy,
          unitCost: item.unitPrice,
          currency: this.selectedPO.currency || 'NPR',
          unitId: item.unitId || '',
          sellingPrice: item.sellingPrice && item.sellingPrice > 0 ? item.sellingPrice : null,
          hasWarranty: !!item.hasWarranty,
          warrantyPeriodMonths: item.hasWarranty && item.warrantyPeriod ? item.warrantyPeriod : null,
          warrantyType: item.hasWarranty && item.warrantyType ? item.warrantyType : null,
          warrantyTerms: item.hasWarranty && item.warrantyTerms ? item.warrantyTerms : null
        }))
      };
      // Edge case: warn if any item with warranty is missing period
      if (this.lineItems.some(item => item.hasWarranty && !item.warrantyPeriod)) {
        this.submitError = 'Warranty period is required for items with warranty.';
        this.isSubmitting = false;
        return;
      }
      // Edge case: warn if any item has negative or missing selling price when required
      if (this.lineItems.some(item => item.sellingPrice !== undefined && item.sellingPrice < 0)) {
        this.submitError = 'Selling price cannot be negative.';
        this.isSubmitting = false;
        return;
      }
      this.grnService.createGoodsReceipt(request).subscribe({
        next: (res: any) => {
          this.isSubmitting = false;
          this.submitSuccess = `Goods Receipt '${res.grnNumber || ''}' created successfully.`;
          this.goToStep(1); // Optionally reset wizard
        },
        error: (err: any) => {
          this.isSubmitting = false;
          this.submitError = err?.error?.message || 'Failed to create goods receipt.';
        }
      });
    }
  // Step 2 state
  lineItems: any[] = [];
  conditions = [
    { label: 'Good', value: 'GOOD' },
    { label: 'Acceptable', value: 'ACCEPTABLE' },
    { label: 'Damaged', value: 'DAMAGED' },
    { label: 'Defective', value: 'DEFECTIVE' }
  ];

  initLineItemsFromPO() {
    if (!this.selectedPO) return;
    this.lineItems = this.selectedPO.lines.map(line => ({
      ...line,
      receivingQuantity: line.remainingQuantity,
      condition: 'GOOD',
      hasDiscrepancy: false
    }));
    this.updateDiscrepancies();
  }

  updateDiscrepancies() {
    this.lineItems.forEach(item => {
      item.hasDiscrepancy = item.receivingQuantity !== item.remainingQuantity;
    });
  }

  onReceivingQtyChange(item: any, value: string) {
    const qty = Number(value);
    item.receivingQuantity = qty;
    this.updateDiscrepancies();
  }

  onConditionChange(item: any, value: string) {
    item.condition = value;
  }

  goToStep(step: number) {
    if (this.step === 1 && step === 2) {
      this.initLineItemsFromPO();
    }
    this.step = step;
  }
import { Component, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PurchaseOrderService, PurchaseOrderResponse } from '../services/purchase-order.service';
import { WarehouseService, WarehouseResponse } from '../../inventory/services/warehouse.service';

@Component({
  selector: 'app-goods-receipt-wizard',
  templateUrl: './goods-receipt-wizard.component.html',
  styleUrls: ['./goods-receipt-wizard.component.css']
})
export class GoodsReceiptWizardComponent implements OnInit {
  step = 1;
  formStep1: FormGroup;
  purchaseOrders: PurchaseOrderResponse[] = [];
  warehouses: WarehouseResponse[] = [];
  isLoadingPO = false;
  isLoadingWarehouse = false;
  poService = inject(PurchaseOrderService);
  warehouseService = inject(WarehouseService);
  fb = inject(FormBuilder);

  selectedPO: PurchaseOrderResponse | null = null;
  selectedWarehouse: WarehouseResponse | null = null;

  constructor() {
    this.formStep1 = this.fb.group({
      purchaseOrderId: ['', Validators.required],
      warehouseId: ['', Validators.required],
      receivedDate: [new Date().toISOString().split('T')[0], Validators.required],
      showDelivery: [false],
      deliveryDate: [''],
      deliveryReference: [''],
      carrierName: [''],
      driverName: [''],
      deliveryNotes: ['']
    });
  }

  ngOnInit(): void {
    this.loadPurchaseOrders();
    this.loadWarehouses();
  }

  loadPurchaseOrders() {
    this.isLoadingPO = true;
    this.poService.getPurchaseOrders({
      search: '',
      pageNumber: 1,
      pageSize: 50,
      status: 'CONFIRMED,PARTIAL'
    }).subscribe({
      next: (res) => {
        this.purchaseOrders = res.data ?? [];
        this.isLoadingPO = false;
      },
      error: () => { this.isLoadingPO = false; }
    });
  }

  loadWarehouses() {
    this.isLoadingWarehouse = true;
    this.warehouseService.getWarehouses({
      search: '',
      pageNumber: 1,
      pageSize: 50
    }).subscribe({
      next: (res) => {
        this.warehouses = res.data ?? [];
        this.isLoadingWarehouse = false;
      },
      error: () => { this.isLoadingWarehouse = false; }
    });
  }

  onPOChange(poId: string) {
    this.selectedPO = this.purchaseOrders.find(po => po.id === poId) || null;
  }

  onWarehouseChange(warehouseId: string) {
    this.selectedWarehouse = this.warehouses.find(w => w.id === warehouseId) || null;
  }

  goToStep(step: number) {
    this.step = step;
  }
}
