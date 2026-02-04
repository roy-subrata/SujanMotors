import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { MessageService, ConfirmationService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { PurchaseOrderService, PurchaseOrderResponse } from '../services/purchase-order.service';
import { GoodsReceiptService, GoodsReceiptResponse } from '../services/goods-receipt.service';
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
    ToastModule,
    TagModule,
    TooltipModule,
    ConfirmDialogModule
  ],
  providers: [MessageService, ConfirmationService],
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
  private readonly confirmationService = inject(ConfirmationService);
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
  isViewing = false;
  mode: 'create' | 'edit' | 'view' = 'create';
  grnId: string | null = null;
  currentGRN: GoodsReceiptResponse | null = null;

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
   * Check mode (create/edit/view)
   */
  private checkEditMode(): void {
    const currentPath = this.route.snapshot.routeConfig?.path || '';
    this.grnId = this.route.snapshot.queryParamMap.get('id');

    if (this.grnId) {
      if (currentPath.endsWith('/view') || currentPath === 'view') {
        this.isViewing = true;
        this.mode = 'view';
      } else if (currentPath.endsWith('/edit') || currentPath === 'edit') {
        this.isEditing = true;
        this.mode = 'edit';
      } else {
        this.isEditing = true;
        this.mode = 'edit';
      }
      this.loadGoodsReceipt(this.grnId);
    } else {
      this.mode = 'create';
    }
  }

  /**
   * Load existing goods receipt for editing/viewing
   */
  private loadGoodsReceipt(id: string): void {
    this.grnService.getGoodsReceiptById(id).subscribe({
      next: (grn) => {
        this.currentGRN = grn;

        // Find matching PO and warehouse
        const matchingPO = this.purchaseOrders.find(po => po.id === grn.purchaseOrderId);
        const matchingWarehouse = this.warehouses.find(w => w.id === grn.warehouseId);

        // If PO not found in filtered list, create a mock object for display
        if (!matchingPO && grn.poNumber) {
          this.selectedPO = { id: grn.purchaseOrderId, poNumber: grn.poNumber } as PurchaseOrderResponse;
        } else {
          this.selectedPO = matchingPO || null;
        }

        // If warehouse not found, create a mock object for display
        if (!matchingWarehouse && grn.warehouseName) {
          this.selectedWarehouse = { id: grn.warehouseId, name: grn.warehouseName } as WarehouseResponse;
        } else {
          this.selectedWarehouse = matchingWarehouse || null;
        }

        this.form.patchValue({
          warehouseId: matchingWarehouse?.id || grn.warehouseId,
          receivedDate: grn.receivedDate ? grn.receivedDate.split('T')[0] : '',
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
              currency: [line.currency || this.currencyService.selectedCurrency(), Validators.required],
              unitId: [line.unitId || '']
            })
          );
        });

        // Disable form in view mode
        if (this.isViewing) {
          this.form.disable();
        }
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
          currency: [this.currencyService.selectedCurrency(), Validators.required],
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
      currency: line.currency || this.currencyService.selectedCurrency(),
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
   * Format currency - uses default currency from settings
   */
  formatCurrency(value: number): string {
    const currency = this.currencyService.selectedCurrency();
    return this.currencyService.formatCurrency(value, currency);
  }

  /**
   * Get page title based on mode
   */
  getPageTitle(): string {
    if (this.mode === 'view') return 'View Goods Receipt';
    if (this.mode === 'edit') return 'Edit Goods Receipt';
    return 'Create New Goods Receipt';
  }

  /**
   * Get status severity for p-tag
   */
  getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
    switch (status?.toUpperCase()) {
      case 'PENDING':
        return 'warn';
      case 'VERIFIED':
        return 'info';
      case 'ACCEPTED':
        return 'success';
      case 'REJECTED':
        return 'danger';
      default:
        return 'secondary';
    }
  }

  /**
   * Verify goods receipt
   */
  verifyGoodsReceipt(): void {
    if (!this.grnId || !this.currentGRN) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to verify Goods Receipt ${this.currentGRN.grnNumber}?`,
      header: 'Confirm Verification',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        // TODO: Get current user name from auth service
        const verifiedBy = 'System User';
        this.grnService.verifyGoodsReceipt(this.grnId!, verifiedBy).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt ${this.currentGRN!.grnNumber} verified successfully`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to verify goods receipt'
            });
            console.error('Error verifying GRN:', error);
          }
        });
      }
    });
  }

  /**
   * Accept goods receipt
   */
  acceptGoodsReceipt(): void {
    if (!this.grnId || !this.currentGRN) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to accept Goods Receipt ${this.currentGRN.grnNumber}? This will update the inventory.`,
      header: 'Confirm Acceptance',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-success',
      accept: () => {
        this.grnService.acceptGoodsReceipt(this.grnId!).subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt ${this.currentGRN!.grnNumber} accepted successfully`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to accept goods receipt'
            });
            console.error('Error accepting GRN:', error);
          }
        });
      }
    });
  }

  /**
   * Reject goods receipt
   */
  rejectGoodsReceipt(): void {
    if (!this.grnId || !this.currentGRN) return;

    this.confirmationService.confirm({
      message: `Are you sure you want to reject Goods Receipt ${this.currentGRN.grnNumber}?`,
      header: 'Confirm Rejection',
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.grnService.rejectGoodsReceipt(this.grnId!, 'Rejected by user').subscribe({
          next: () => {
            this.messageService.add({
              severity: 'success',
              summary: 'Success',
              detail: `Goods Receipt ${this.currentGRN!.grnNumber} rejected`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: 'Error',
              detail: error?.error?.message || 'Failed to reject goods receipt'
            });
            console.error('Error rejecting GRN:', error);
          }
        });
      }
    });
  }
}
