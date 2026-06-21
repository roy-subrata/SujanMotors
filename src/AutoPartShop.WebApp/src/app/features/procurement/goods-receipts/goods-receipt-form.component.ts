import { HttpErrorResponse } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ConfirmationService, MessageService } from 'primeng/api';
import { DialogService } from 'primeng/dynamicdialog';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { Select } from 'primeng/select';
import { TextareaModule } from 'primeng/textarea';
import { CheckboxModule } from 'primeng/checkbox';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { TooltipModule } from 'primeng/tooltip';
import { map } from 'rxjs/operators';

import { WarehouseResponse, WarehouseService } from '../../inventory/services/warehouse.service';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PriceCodeService } from '../../../shared/services/price-code.service';
import { AuthService } from '../../../shared/services/auth.service';
import {
  GoodsReceiptLineResponse,
  GoodsReceiptResponse,
  GoodsReceiptService
} from '../services/goods-receipt.service';
import { PurchaseOrderResponse, PurchaseOrderService } from '../services/purchase-order.service';
import { BarcodeDialogComponent } from '../../inventory/parts/barcode-dialog/barcode-dialog.component';
import { labelFromGrnLine } from '../../inventory/parts/barcode-dialog/label-data';

type WorkflowStatus = 'PENDING' | 'VERIFIED' | 'ACCEPTED';

interface SubmitValidationResult {
  errors: string[];
  warnings: string[];
}

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
    Select,
    ToastModule,
    TagModule,
    TooltipModule,
    ConfirmDialogModule,
    LazyAutocompleteComponent,
    TextareaModule,
    CheckboxModule
  ],
  providers: [MessageService, ConfirmationService, DialogService],
  templateUrl: './goods-receipt-form.component.html',
  styleUrls: ['./goods-receipt-form.component.css']
})
export class GoodsReceiptFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly poService = inject(PurchaseOrderService);
  private readonly grnService = inject(GoodsReceiptService);
  private readonly warehouseService = inject(WarehouseService);
  private readonly currencyService = inject(CurrencyService);
  readonly priceCodeService = inject(PriceCodeService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly dialogService = inject(DialogService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly auth = inject(AuthService);

  /** GRN create/verify/accept/reject are restricted to back-office roles; cashiers can only view. */
  get canManage(): boolean {
    return this.auth.hasAnyRole(['Admin', 'Manager']);
  }

  form: FormGroup;
  selectedPO: PurchaseOrderResponse | null = null;
  selectedWarehouse: WarehouseResponse | null = null;
  warehouses: WarehouseResponse[] = [];
  activeCurrencies$ = this.currencyService.activeCurrencies$;

  readonly createSteps = [
    { index: 1, label: 'PO & Warehouse' },
    { index: 2, label: 'Items' },
    { index: 3, label: 'Review' }
  ];

  readonly workflowSteps: WorkflowStatus[] = ['PENDING', 'VERIFIED', 'ACCEPTED'];

  readonly conditions = [
    { label: 'Good', value: 'GOOD' },
    { label: 'Acceptable', value: 'ACCEPTABLE' },
    { label: 'Damaged', value: 'DAMAGED' },
    { label: 'Defective', value: 'DEFECTIVE' }
  ];

  createStep = 1;
  showDeliverySection = false;
  lineItemWarnings: string[] = [];

  isSubmitting = false;
  isEditing = false;
  isViewing = false;
  mode: 'create' | 'edit' | 'view' = 'create';
  grnId: string | null = null;
  currentGRN: GoodsReceiptResponse | null = null;

  // Which line item has its details panel open (-1 = none)
  openDetailIndex = -1;

  readonly warrantyTypeOptions = [
    { label: 'Manufacturer', value: 'MANUFACTURER' },
    { label: 'Seller',       value: 'SELLER' },
    { label: 'Extended',     value: 'EXTENDED' }
  ];

  fetchPurchaseOrdersLazy = (req: LazyRequest) =>
    this.poService
      .getPurchaseOrders({
        pageNumber: req.pageNumber,
        pageSize: req.pageSize,
        search: req.search,
        status: 'CONFIRMED,PARTIAL',
        hasReceivableQuantity: true
      })
      .pipe(
        map(
          (res) =>
            ({
              items: res?.data ?? [],
              totalCount: res?.pagination?.totalCount ?? 0
            }) as LazyResponse<PurchaseOrderResponse>
        )
      );

  fetchWarehousesLazy = (req: LazyRequest) =>
    this.warehouseService
      .getWarehouses({
        pageNumber: req.pageNumber,
        pageSize: req.pageSize,
        search: req.search
      })
      .pipe(
        map(
          (res) =>
            ({
              items: res?.data ?? [],
              totalCount: res?.pagination?.totalCount ?? 0
            }) as LazyResponse<WarehouseResponse>
        )
      );

  constructor() {
    this.form = this.createForm();
  }

  ngOnInit(): void {
    this.loadWarehouses();
    this.currencyService.loadActiveCurrencies();
    this.checkEditMode();
  }

  get isCreateMode(): boolean {
    return this.mode === 'create';
  }

  get lineItemsArray(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  get totalLinesWithReceivingQty(): number {
    return this.lineItemsArray.controls.filter((control) => this.toNumber(control.get('receivingQuantity')?.value) > 0)
      .length;
  }

  getTotalOrderedQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + this.toNumber(line.orderedQuantity), 0);
  }

  getTotalReceivingQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + this.toNumber(line.receivingQuantity), 0);
  }

  getTotalReceivedQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + this.toNumber(line.receivedQuantity), 0);
  }

  getDiscrepancyCount(): number {
    return this.lineItemsArray.value.filter((line: any) => !!line.hasDiscrepancy).length;
  }

  getTotalCostAllItems(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => {
      return sum + this.toNumber(line.receivingQuantity) * this.toNumber(line.unitCost);
    }, 0);
  }

  getAverageUnitCost(): number {
    const totalQuantity = this.getTotalReceivingQuantity();
    if (totalQuantity <= 0) {
      return 0;
    }

    return this.getTotalCostAllItems() / totalQuantity;
  }

  /** Good (accepted) quantity for a line = Receiving - Damaged - Wrong, floored at 0. */
  getGoodQuantity(ctrl: any): number {
    const good =
      this.toNumber(ctrl.get('receivingQuantity')?.value) -
      this.toNumber(ctrl.get('damagedQuantity')?.value) -
      this.toNumber(ctrl.get('wrongQuantity')?.value);
    return Math.max(good, 0);
  }

  getTotalGoodQuantity(): number {
    return this.lineItemsArray.controls.reduce((sum, ctrl) => sum + this.getGoodQuantity(ctrl), 0);
  }

  getTotalDamagedQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + this.toNumber(line.damagedQuantity), 0);
  }

  getTotalWrongQuantity(): number {
    return this.lineItemsArray.value.reduce((sum: number, line: any) => sum + this.toNumber(line.wrongQuantity), 0);
  }

  /** Number of lines that would generate a return line (any damaged or wrong qty). */
  getPotentialReturns(): number {
    return this.lineItemsArray.value.filter(
      (line: any) => this.toNumber(line.damagedQuantity) > 0 || this.toNumber(line.wrongQuantity) > 0
    ).length;
  }

  /** Quick action: receive everything in perfect condition (Damaged = Wrong = 0). */
  acceptAll(): void {
    this.lineItemsArray.controls.forEach((ctrl, index) => {
      if (ctrl.get('receivingQuantity')?.disabled) {
        return;
      }
      const remaining = this.toNumber(ctrl.get('remainingQuantity')?.value);
      ctrl.patchValue(
        { receivingQuantity: remaining, damagedQuantity: 0, wrongQuantity: 0, rejectionReason: '', condition: 'GOOD' },
        { emitEvent: false }
      );
      this.updateLineDiscrepancy(index);
    });
    this.refreshLineWarnings();
  }

  getPageTitle(): string {
    if (this.mode === 'view') return 'View Goods Receipt';
    if (this.mode === 'edit') return 'Edit Goods Receipt';
    return 'Create Goods Receipt';
  }

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

  getWorkflowStatusIndex(status: string): number {
    const normalized = status?.toUpperCase();
    return this.workflowSteps.findIndex((step) => step === normalized);
  }

  formatWorkflowStatus(status: WorkflowStatus): string {
    if (status === 'PENDING') return 'Pending';
    if (status === 'VERIFIED') return 'Verified';
    return 'Accepted';
  }

  formatCurrency(value: number): string {
    return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
  }

  formatCostPrice(value: number): string {
    const coded = this.priceCodeService.getDisplayPrice(value);
    if (coded !== null) {
      return coded;
    }

    return this.formatCurrency(value);
  }

  hasError(fieldName: string): boolean {
    const field = this.form.get(fieldName);
    return !!field && field.invalid && field.touched;
  }

  getErrorMessage(fieldName: string): string {
    const field = this.form.get(fieldName);
    if (field?.hasError('required')) {
      return `${fieldName} is required`;
    }

    return '';
  }

  toggleDeliverySection(): void {
    this.showDeliverySection = !this.showDeliverySection;
  }

  goToCreateStep(stepIndex: number): void {
    if (!this.isCreateMode || stepIndex < 1 || stepIndex > 3) {
      return;
    }

    if (stepIndex > this.createStep) {
      if (this.createStep === 1 && !this.canProceedFromStepOne()) {
        this.showValidationError('Select a purchase order, warehouse, and received date to continue.');
        return;
      }

      if (this.createStep <= 2 && stepIndex === 3) {
        const validation = this.validateBeforeSubmit();
        this.lineItemWarnings = validation.warnings;
        if (validation.errors.length > 0) {
          this.showValidationError(validation.errors[0]);
          return;
        }
      }
    }

    this.createStep = stepIndex;
  }

  nextCreateStep(): void {
    this.goToCreateStep(this.createStep + 1);
  }

  previousCreateStep(): void {
    this.goToCreateStep(this.createStep - 1);
  }

  onPOSelected(po: PurchaseOrderResponse): void {
    this.selectedPO = po;
    this.populateLineItems();
  }

  onPOCleared(): void {
    this.selectedPO = null;
    this.lineItemsArray.clear();
    this.lineItemWarnings = [];
  }

  onWarehouseSelected(warehouse: WarehouseResponse): void {
    this.selectedWarehouse = warehouse;
    this.form.patchValue({ warehouseId: warehouse.id });
  }

  onWarehouseCleared(): void {
    this.selectedWarehouse = null;
    this.form.patchValue({ warehouseId: '' });
  }

  onReceivingQuantityChanged(index: number): void {
    this.updateLineDiscrepancy(index);
    this.refreshLineWarnings();
  }

  onConditionChanged(): void {
    this.refreshLineWarnings();
  }

  toggleDetails(index: number): void {
    this.openDetailIndex = this.openDetailIndex === index ? -1 : index;
  }

  isDetailOpen(index: number): boolean {
    return this.openDetailIndex === index;
  }

  /** True if any lot-level detail field has a value — drives the icon badge */
  hasDetails(ctrl: any): boolean {
    return !!(
      this.toNumber(ctrl.get('warrantyPeriodMonths')?.value) > 0 ||
      ctrl.get('expiryDate')?.value ||
      ctrl.get('notes')?.value?.trim()
    );
  }

  isFullyReceived(ctrl: any): boolean {
    return this.toNumber(ctrl.get('remainingQuantity')?.value) === 0;
  }

  /** Recompute discrepancy & warnings when unit cost changes. */
  onUnitCostChanged(index: number): void {
    this.updateLineDiscrepancy(index);
    this.refreshLineWarnings();
  }

  onSubmit(): void {
    if (this.mode === 'view') {
      return;
    }

    const validation = this.validateBeforeSubmit();
    this.lineItemWarnings = validation.warnings;

    if (validation.errors.length > 0) {
      this.showValidationError(validation.errors[0]);
      return;
    }

    const processedLines = this.buildSubmissionLines();

    if (processedLines.length === 0) {
      this.showValidationError('At least one line must have receiving quantity greater than 0.');
      return;
    }

    if (validation.warnings.length > 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Review Warning',
        detail: validation.warnings[0]
      });
    }

    const invoiceNotProvided = !!this.form.value.invoiceNotProvided;
    const request = {
      purchaseOrderId: this.selectedPO!.id,
      warehouseId: this.form.value.warehouseId,
      receivedDate: this.form.value.receivedDate,
      // Invoice
      supplierInvoiceNumber: invoiceNotProvided ? null : this.toTrimmedOrNull(this.form.value.supplierInvoiceNumber),
      supplierInvoiceDate: invoiceNotProvided ? null : this.toTrimmedOrNull(this.form.value.supplierInvoiceDate),
      invoiceNotProvided,
      // Delivery
      deliveryDate: this.toTrimmedOrNull(this.form.value.deliveryDate) ?? undefined,
      deliveryReference: this.toTrimmedOrEmpty(this.form.value.deliveryReference),
      carrierName: this.toTrimmedOrEmpty(this.form.value.carrierName),
      driverName: this.toTrimmedOrEmpty(this.form.value.driverName),
      deliveryNotes: this.toTrimmedOrEmpty(this.form.value.deliveryNotes),
      lines: processedLines
    };

    this.isSubmitting = true;

    if (this.isEditing && this.grnId) {
      this.grnService.updateGoodsReceipt(this.grnId, request).subscribe({
        next: (grn) => {
          this.isSubmitting = false;
          this.messageService.add({
            severity: 'success',
            summary: 'Updated',
            detail: `Goods Receipt '${grn.grnNumber}' updated successfully.`
          });
          this.router.navigate(['/procurement/goods-receipts']);
        },
        error: (error) => {
          this.isSubmitting = false;
          this.showApiError('Update Failed', error, 'Failed to update goods receipt.');
        }
      });

      return;
    }

    this.grnService.createGoodsReceipt(request).subscribe({
      next: (grn) => {
        this.isSubmitting = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Created',
          detail: `Goods Receipt '${grn.grnNumber}' created successfully.`
        });
        this.router.navigate(['/procurement/goods-receipts']);
      },
      error: (error) => {
        this.isSubmitting = false;
        this.showApiError('Creation Failed', error, 'Failed to create goods receipt.');
      }
    });
  }

  verifyGoodsReceipt(): void {
    if (!this.grnId || !this.currentGRN) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Verify Goods Receipt',
      message: `Verify receipt ${this.currentGRN.grnNumber} and move it to VERIFIED?`,
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        const verifiedBy = 'System User';
        this.grnService.verifyGoodsReceipt(this.grnId!, verifiedBy).subscribe({
          next: (grn) => {
            this.currentGRN = grn;
            this.messageService.add({
              severity: 'success',
              summary: 'Verified',
              detail: `Goods Receipt ${grn.grnNumber} verified successfully.`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => this.showApiError('Verification Failed', error, 'Failed to verify goods receipt.')
        });
      }
    });
  }

  /** True when the receipt has damaged/wrong items that could be returned to the supplier. */
  get hasReturnableItems(): boolean {
    return !!this.currentGRN && this.currentGRN.lines?.some((l) => (l.damagedQuantity ?? 0) > 0 || (l.wrongQuantity ?? 0) > 0);
  }

  /** Lines with at least one accepted ("good") unit — the ones worth labelling. */
  get printableLines(): GoodsReceiptLineResponse[] {
    return (this.currentGRN?.lines ?? []).filter(
      (l) => (l.acceptedQuantity ?? l.receivedQuantity ?? 0) > 0
    );
  }

  /**
   * Open the label dialog for one received line. Quantity defaults to the
   * accepted ("good") quantity (auto-qty) and batch/expiry flow into the label.
   */
  printLineLabel(line: GoodsReceiptLineResponse): void {
    this.dialogService.open(BarcodeDialogComponent, {
      data: { label: labelFromGrnLine(line, this.currentGRN?.receivedDate), layout: 'combo' },
      header: 'Print Labels',
      width: '100vw',
      height: '100vh',
      styleClass: 'fullscreen-dialog',
      modal: true,
      closable: true
    });
  }

  acceptGoodsReceipt(createReturn = false): void {
    if (!this.grnId || !this.currentGRN) {
      return;
    }

    const message = createReturn
      ? `Accept receipt ${this.currentGRN.grnNumber} and create a draft Purchase Return for the damaged/wrong items?`
      : `Accept receipt ${this.currentGRN.grnNumber}? This updates stock on hand.`;

    this.confirmationService.confirm({
      header: createReturn ? 'Accept & Create Return' : 'Accept Goods Receipt',
      message,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-success',
      accept: () => {
        this.grnService.acceptGoodsReceipt(this.grnId!, createReturn).subscribe({
          next: (grn) => {
            this.currentGRN = grn;
            this.messageService.add({
              severity: 'success',
              summary: 'Accepted',
              detail: createReturn
                ? `Goods Receipt ${grn.grnNumber} accepted and a draft Purchase Return was created.`
                : `Goods Receipt ${grn.grnNumber} accepted successfully.`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => this.showApiError('Acceptance Failed', error, 'Failed to accept goods receipt.')
        });
      }
    });
  }

  /**
   * Open the Purchase Return form pre-filled from this accepted receipt's remaining damaged/wrong units.
   * For when the receipt was accepted without creating a return, or damage is actioned later.
   */
  createReturnFromGrn(): void {
    if (!this.grnId) return;
    this.router.navigate(['/procurement/purchase-returns/create'], {
      queryParams: { goodsReceiptId: this.grnId }
    });
  }

  rejectGoodsReceipt(): void {
    if (!this.grnId || !this.currentGRN) {
      return;
    }

    this.confirmationService.confirm({
      header: 'Reject Goods Receipt',
      message: `Reject receipt ${this.currentGRN.grnNumber}?`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.grnService.rejectGoodsReceipt(this.grnId!, 'Rejected by user').subscribe({
          next: (grn) => {
            this.currentGRN = grn;
            this.messageService.add({
              severity: 'success',
              summary: 'Rejected',
              detail: `Goods Receipt ${grn.grnNumber} rejected.`
            });
            this.loadGoodsReceipt(this.grnId!);
          },
          error: (error) => this.showApiError('Rejection Failed', error, 'Failed to reject goods receipt.')
        });
      }
    });
  }

  goBack(): void {
    this.router.navigate(['/procurement/goods-receipts']);
  }

  private checkEditMode(): void {
    const currentPath = this.route.snapshot.routeConfig?.path || '';
    this.grnId = this.route.snapshot.queryParamMap.get('id');

    if (!this.grnId) {
      this.mode = 'create';
      return;
    }

    if (currentPath.endsWith('/view') || currentPath === 'view') {
      this.isViewing = true;
      this.mode = 'view';
    } else {
      this.isEditing = true;
      this.mode = 'edit';
    }

    this.loadGoodsReceipt(this.grnId);
  }

  private loadWarehouses(): void {
    this.warehouseService
      .getWarehouses({
        search: '',
        pageNumber: 1,
        pageSize: 1000,
        sorts: [{ field: 'name', direction: 'asc' }]
      })
      .subscribe({
        next: (res) => {
          const warehouseList = res.data ?? [];
          this.warehouses = Array.isArray(warehouseList) ? warehouseList : [];
        },
        error: (error) => {
          this.showApiError('Warehouse Load Failed', error, 'Could not load warehouses.');
        }
      });
  }

  private loadGoodsReceipt(id: string): void {
    this.grnService.getGoodsReceiptById(id).subscribe({
      next: (grn) => {
        this.currentGRN = grn;

        this.selectedWarehouse =
          this.warehouses.find((w) => w.id === grn.warehouseId) ||
          ({ id: grn.warehouseId, name: grn.warehouseName || 'Unknown Warehouse' } as WarehouseResponse);

        this.form.patchValue({
          warehouseId: this.selectedWarehouse?.id || grn.warehouseId,
          receivedDate: grn.receivedDate ? grn.receivedDate.split('T')[0] : '',
          // Invoice
          supplierInvoiceNumber: grn.supplierInvoiceNumber || '',
          supplierInvoiceDate: grn.supplierInvoiceDate ? grn.supplierInvoiceDate.split('T')[0] : '',
          invoiceNotProvided: grn.invoiceNotProvided || false,
          // Delivery
          deliveryDate: grn.deliveryDate ? grn.deliveryDate.split('T')[0] : '',
          deliveryReference: grn.deliveryReference,
          carrierName: grn.carrierName,
          driverName: grn.driverName,
          deliveryNotes: grn.deliveryNotes
        });

        this.showDeliverySection = !!(
          grn.deliveryDate ||
          this.toTrimmedOrNull(grn.deliveryReference) ||
          this.toTrimmedOrNull(grn.carrierName) ||
          this.toTrimmedOrNull(grn.driverName) ||
          this.toTrimmedOrNull(grn.deliveryNotes)
        );

        this.poService.getPurchaseOrderById(grn.purchaseOrderId).subscribe({
          next: (po) => {
            this.selectedPO = po;
            this.setLineItemsFromGoodsReceipt(grn, po);
          },
          error: () => {
            this.selectedPO = ({ id: grn.purchaseOrderId, poNumber: grn.poNumber || '' } as PurchaseOrderResponse);
            this.setLineItemsFromGoodsReceipt(grn, null);
          }
        });
      },
      error: (error) => {
        this.showApiError('Load Failed', error, 'Failed to load goods receipt.');
      }
    });
  }

  private createForm(): FormGroup {
    return this.fb.group({
      warehouseId: ['', Validators.required],
      receivedDate: [new Date().toISOString().split('T')[0], Validators.required],
      // Supplier Invoice
      supplierInvoiceNumber: [''],
      supplierInvoiceDate: [''],
      invoiceNotProvided: [false],
      // Delivery
      deliveryDate: [''],
      deliveryReference: [''],
      carrierName: [''],
      driverName: [''],
      deliveryNotes: [''],
      lineItems: this.fb.array([])
    });
  }

  private canProceedFromStepOne(): boolean {
    this.form.get('warehouseId')?.markAsTouched();
    this.form.get('receivedDate')?.markAsTouched();
    return !!this.selectedPO && !!this.selectedWarehouse && !!this.form.value.receivedDate;
  }

  private populateLineItems(): void {
    if (!this.selectedPO) {
      return;
    }

    const linesArray = this.lineItemsArray;
    linesArray.clear();

    this.selectedPO.lines?.forEach((line, index) => {
      // Trust the API-computed remaining (accepted net of rejections, minus in-flight GRNs).
      // Use ?? so a legitimate remaining of 0 isn't treated as falsy and recomputed locally.
      const remainingQty = Math.max(this.toNumber(line.remainingQuantity ?? 0), 0);
      const receivingNow = remainingQty;

      const cost = this.toNumber(line.unitPrice);

      const group = this.fb.group({
        partId:                  [line.partId, Validators.required],
        purchaseOrderLineId:     [line.id],
        partName:                [line.partName],
        variantName:             [line.variantName || null],
        displayName:             [line.displayName || line.partName],
        orderedQuantity:         [this.toNumber(line.quantity)],
        receivedQuantity:        [this.toNumber(line.receivedQuantity)],
        remainingQuantity:       [remainingQty],
        maxReceivableQuantity:   [remainingQty],
        receivingQuantity:       [receivingNow, [Validators.required, Validators.min(0), Validators.max(remainingQty)]],
        damagedQuantity:         [0, [Validators.min(0), Validators.max(remainingQty)]],
        wrongQuantity:           [0, [Validators.min(0), Validators.max(remainingQty)]],
        rejectionReason:         [''],
        condition:               ['GOOD', Validators.required],
        notes:                   [''],
        hasDiscrepancy:          [false],
        unitCost:                [cost, [Validators.required, Validators.min(0)]],
        currency:                [this.currencyService.selectedCurrency(), Validators.required],
        unitId:                  [line.unitId || ''],
        unitName:                [line.unitName || ''],
        unitSymbol:              [line.unitSymbol || ''],
        batchNumber:             [''],
        expiryDate:              [''],
        // Lot-level warranty (details panel)
        warrantyPeriodMonths:    [null],
        warrantyType:            [''],
        warrantyTerms:           ['']
      });

      if (remainingQty === 0) {
        group.get('receivingQuantity')?.setValue(0);
        group.get('receivingQuantity')?.disable();
        group.get('damagedQuantity')?.disable();
        group.get('wrongQuantity')?.disable();
        group.get('rejectionReason')?.disable();
        group.get('condition')?.disable();
        group.get('unitCost')?.disable();
      }

      linesArray.push(group);
      this.updateLineDiscrepancy(index);
    });

    this.refreshLineWarnings();
  }

  private setLineItemsFromGoodsReceipt(grn: GoodsReceiptResponse, po: PurchaseOrderResponse | null): void {
    const linesArray = this.lineItemsArray;
    linesArray.clear();

    grn.lines?.forEach((line, index) => {
      // Match the exact PO line (disambiguates same-part variant lines); fall back to partId for older data.
      const poLine =
        (line.purchaseOrderLineId ? po?.lines?.find((l) => l.id === line.purchaseOrderLineId) : undefined) ??
        po?.lines?.find((l) => l.partId === line.partId);
      const orderedQty = this.toNumber(poLine?.quantity);
      const alreadyReceived = this.toNumber(poLine?.receivedQuantity);
      const remainingQty = this.toNumber(poLine?.remainingQuantity ?? orderedQty - alreadyReceived);
      const maxReceivableQty = Math.max(remainingQty + this.toNumber(line.receivedQuantity), this.toNumber(line.receivedQuantity));

      linesArray.push(
        this.fb.group({
          partId: [line.partId, Validators.required],
          purchaseOrderLineId: [line.purchaseOrderLineId || poLine?.id || null],
          partName: [poLine?.partName || ''],
          variantName: [poLine?.variantName || null],
          displayName: [poLine?.displayName || poLine?.partName || ''],
          orderedQuantity: [orderedQty],
          receivedQuantity: [alreadyReceived],
          remainingQuantity: [remainingQty],
          maxReceivableQuantity: [maxReceivableQty],
          receivingQuantity: [
            this.toNumber(line.receivedQuantity),
            [Validators.required, Validators.min(0), Validators.max(maxReceivableQty)]
          ],
          damagedQuantity: [this.toNumber(line.damagedQuantity), [Validators.min(0), Validators.max(maxReceivableQty)]],
          wrongQuantity: [this.toNumber(line.wrongQuantity), [Validators.min(0), Validators.max(maxReceivableQty)]],
          rejectionReason: [''],
          condition: [line.condition, Validators.required],
          notes: [line.notes || ''],
          hasDiscrepancy: [!!line.hasDiscrepancy],
          unitCost: [this.toNumber(line.unitCost), [Validators.required, Validators.min(0)]],
          currency: [line.currency || this.currencyService.selectedCurrency(), Validators.required],
          unitId: [line.unitId || poLine?.unitId || ''],
          unitName: [poLine?.unitName || ''],
          unitSymbol: [poLine?.unitSymbol || ''],
          batchNumber:             [line.batchNumber || ''],
          expiryDate:              [line.expiryDate ? line.expiryDate.split('T')[0] : ''],
          // Lot-level warranty from saved GRN
          warrantyPeriodMonths:    [line.warrantyPeriodMonths ?? null],
          warrantyType:            [line.warrantyType ?? ''],
          warrantyTerms:           [line.warrantyTerms ?? '']
        })
      );

      this.updateLineDiscrepancy(index);
    });

    this.refreshLineWarnings();

    if (this.isViewing) {
      this.form.disable();
    }
  }

  private updateLineDiscrepancy(index: number): void {
    const line = this.lineItemsArray.at(index);
    if (!line) {
      return;
    }

    const receivingQty = this.toNumber(line.get('receivingQuantity')?.value);
    const remainingQty = this.toNumber(line.get('remainingQuantity')?.value);
    line.patchValue({ hasDiscrepancy: receivingQty !== remainingQty }, { emitEvent: false });
  }

  private refreshLineWarnings(): void {
    this.lineItemWarnings = this.validateBeforeSubmit().warnings;
  }

  private validateBeforeSubmit(): SubmitValidationResult {
    const errors: string[] = [];
    const warnings: string[] = [];

    if (!this.selectedPO) {
      errors.push('Purchase order is required.');
    }

    if (!this.selectedWarehouse || !this.form.value.warehouseId) {
      errors.push('Warehouse is required.');
    }

    if (!this.form.value.receivedDate) {
      errors.push('Received date is required.');
    }

    if (this.lineItemsArray.length === 0) {
      errors.push('No line items found for this goods receipt.');
      return { errors, warnings };
    }

    let linesWithQuantity = 0;

    this.lineItemsArray.controls.forEach((line, index) => {
      const partName = line.get('partName')?.value || `Item ${index + 1}`;
      const receivingQty = this.toNumber(line.get('receivingQuantity')?.value);
      const maxReceivableQty = this.toNumber(line.get('maxReceivableQuantity')?.value);
      const damagedQty = this.toNumber(line.get('damagedQuantity')?.value);
      const wrongQty = this.toNumber(line.get('wrongQuantity')?.value);
      const rejectionReason = this.toTrimmedOrNull(line.get('rejectionReason')?.value);
      const condition = this.toTrimmedOrNull(line.get('condition')?.value);

      if (receivingQty < 0) {
        errors.push(`${partName}: receiving quantity cannot be negative.`);
      }

      if (receivingQty > maxReceivableQty) {
        errors.push(`${partName}: receiving quantity exceeds allowed remaining quantity.`);
      }

      if (damagedQty < 0 || wrongQty < 0) {
        errors.push(`${partName}: damaged/wrong quantity cannot be negative.`);
      }

      if (damagedQty + wrongQty > receivingQty) {
        errors.push(`${partName}: damaged + wrong quantity cannot exceed received quantity.`);
      }

      if (damagedQty + wrongQty > 0 && !rejectionReason) {
        errors.push(`${partName}: a reason is required when reporting damaged or wrong items.`);
      }

      if (!condition) {
        errors.push(`${partName}: condition is required.`);
      }

      if (receivingQty === 0) {
        warnings.push(`${partName}: receiving quantity is 0 and this line will not be submitted.`);
      }

      if (receivingQty > 0) {
        linesWithQuantity += 1;
      }
    });

    if (linesWithQuantity === 0) {
      errors.push('At least one item must have receiving quantity greater than 0.');
    }

    return { errors, warnings };
  }

  private buildSubmissionLines(): any[] {
    return this.lineItemsArray.controls
      .map((line) => {
        const receivedQty = this.toNumber(line.get('receivingQuantity')?.value);
        if (receivedQty <= 0) return null;
        const warrantyMonths = line.get('warrantyPeriodMonths')?.value;
        return {
          partId:              line.get('partId')?.value,
          purchaseOrderLineId: line.get('purchaseOrderLineId')?.value ?? null,
          receivedQuantity:    receivedQty,
          damagedQuantity:     this.toNumber(line.get('damagedQuantity')?.value),
          wrongQuantity:       this.toNumber(line.get('wrongQuantity')?.value),
          rejectionReason:     this.toTrimmedOrEmpty(line.get('rejectionReason')?.value),
          condition:           line.get('condition')?.value,
          notes:               this.toTrimmedOrEmpty(line.get('notes')?.value),
          hasDiscrepancy:      !!line.get('hasDiscrepancy')?.value,
          unitCost:            this.toNumber(line.get('unitCost')?.value),
          currency:            this.toTrimmedOrEmpty(line.get('currency')?.value) || this.currencyService.selectedCurrency(),
          unitId:              this.toTrimmedOrNull(line.get('unitId')?.value),
          batchNumber:         this.toTrimmedOrNull(line.get('batchNumber')?.value),
          expiryDate:          this.toTrimmedOrNull(line.get('expiryDate')?.value),
          // Lot-level warranty
          hasWarranty:         this.toNumber(warrantyMonths) > 0 ? true : null,
          warrantyPeriodMonths: this.toNumber(warrantyMonths) > 0 ? this.toNumber(warrantyMonths) : null,
          warrantyType:        this.toTrimmedOrNull(line.get('warrantyType')?.value),
          warrantyTerms:       this.toTrimmedOrNull(line.get('warrantyTerms')?.value)
        };
      })
      .filter((line): line is NonNullable<typeof line> => line !== null);
  }

  private showValidationError(message: string): void {
    this.messageService.add({
      severity: 'error',
      summary: 'Validation Error',
      detail: message
    });
  }

  private showApiError(summary: string, error: unknown, fallbackMessage: string): void {
    this.messageService.add({
      severity: 'error',
      summary,
      detail: this.getApiErrorMessage(error, fallbackMessage)
    });
  }

  private getApiErrorMessage(error: unknown, fallbackMessage: string): string {
    const httpError = error as HttpErrorResponse;

    if (httpError?.status === 0) {
      return 'Network error. Please check your connection and try again.';
    }

    if (typeof httpError?.error === 'string' && httpError.error.trim().length > 0) {
      return httpError.error;
    }

    if (httpError?.error?.message) {
      return httpError.error.message;
    }

    if (httpError?.message) {
      return httpError.message;
    }

    return fallbackMessage;
  }

  private toNumber(value: unknown): number {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : 0;
  }

  private toTrimmedOrNull(value: unknown): string | null {
    if (typeof value !== 'string') {
      return null;
    }

    const trimmed = value.trim();
    return trimmed.length > 0 ? trimmed : null;
  }

  private toTrimmedOrEmpty(value: unknown): string {
    return this.toTrimmedOrNull(value) ?? '';
  }
}
