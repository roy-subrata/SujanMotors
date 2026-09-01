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
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PurchaseReturnService, PurchaseReturnResponse, AvailableLotForReturn, ReturnPrefillFromGrn } from '../../services/purchase-return.service';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { PartService } from '../../../inventory/services/part.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { AppCurrencyPipe } from '@/shared/pipes/app-currency.pipe';
import { StatusDisplayService, StatusSeverity } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

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
    TagModule,
    ConfirmDialogModule,
    AppCurrencyPipe,
    TranslatePipe
  ],
  templateUrl: './purchase-returns-form.component.html',
  styleUrls: ['./purchase-returns-form.component.css'],
  providers: [MessageService, ConfirmationService]
})
export class PurchaseReturnsFormComponent implements OnInit {
  form: FormGroup;
  isEditing = false;
  isViewMode = false;
  isSubmitting = false;
  returnId: string | null = null;
  filteredPurchaseOrders: PurchaseOrderResponse[] = [];
  purchaseOrders: PurchaseOrderResponse[] = [];
  // Part names are resolved on demand (per partId) instead of preloading a capped catalog page —
  // every line here already originates from a known partId (PO line, saved return line, or GRN
  // prefill), so we only need name lookups, not a searchable part-picker. See resolvePartLabel().
  private partNameCache = new Map<string, string>();
  private partNameLoading = new Set<string>();
  availablePOItems: any[] = [];
  showItemSelectionDialog = false;
  selectedPOItems: any[] = [];
  selectedPurchaseOrder: PurchaseOrderResponse | null = null;

  // View mode properties
  currentReturn: PurchaseReturnResponse | null = null;

  // Lot selection - map of partId to available lots
  availableLotsMap: Map<string, AvailableLotForReturn[]> = new Map();
  loadingLotsMap: Map<string, boolean> = new Map();

  /** Getters, not fields: a field freezes the labels in the language active at construction. */
  get conditionOptions() {
    return [
      { label: this.i18n.t('purchaseReturns.form.conditions.unopened'), value: 'UNOPENED' },
      { label: this.i18n.t('purchaseReturns.form.conditions.opened'), value: 'OPENED' },
      { label: this.i18n.t('purchaseReturns.form.conditions.damaged'), value: 'DAMAGED' },
      { label: this.i18n.t('purchaseReturns.form.conditions.defective'), value: 'DEFECTIVE' }
    ];
  }

  // Which inventory bucket the returned units come from. Filters the lot picker; the actual bucket
  // is derived server-side from the chosen lot's status (Damaged/Quarantine require a specific lot).
  get bucketOptions() {
    return [
      { label: this.i18n.t('purchaseReturns.form.buckets.available'), value: 'AVAILABLE' },
      { label: this.i18n.t('purchaseReturns.form.buckets.damaged'), value: 'DAMAGED' },
      { label: this.i18n.t('purchaseReturns.form.buckets.quarantine'), value: 'QUARANTINE' }
    ];
  }

  get reasonOptions() {
    return [
      { label: this.i18n.t('purchaseReturns.form.reasons.defective'), value: 'Defective' },
      { label: this.i18n.t('purchaseReturns.form.reasons.damaged'), value: 'Damaged' },
      { label: this.i18n.t('purchaseReturns.form.reasons.wrongItem'), value: 'Wrong Item' },
      { label: this.i18n.t('purchaseReturns.form.reasons.qualityIssue'), value: 'Quality Issue' },
      { label: this.i18n.t('purchaseReturns.form.reasons.notNeeded'), value: 'Not Needed' },
      { label: this.i18n.t('purchaseReturns.form.reasons.overstock'), value: 'Overstock' },
      { label: this.i18n.t('purchaseReturns.form.reasons.other'), value: 'Other' }
    ];
  }

  private readonly prService = inject(PurchaseReturnService);
  private readonly poService = inject(PurchaseOrderService);
  private readonly partService = inject(PartService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly statusDisplay = inject(StatusDisplayService);
  private readonly i18n = inject(I18nService);

  constructor() {
    this.form = this.createForm();
  }

  /**
   * Get current currency code from settings
   */
  get currencyCode(): string {
    return this.currencyService.selectedCurrency();
  }

  ngOnInit(): void {
    this.loadPurchaseOrders();
    this.checkEditMode();
    if (!this.returnId) {
      this.checkPrefillFromGrn();
    }
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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('purchaseReturns.form.loadPOsFailed')
        });
      }
    });
  }

  /**
   * Resolve a display label for a partId without preloading the whole catalog.
   * If the caller already has the name (PO line / saved return line / GRN prefill all carry
   * `displayName`), use it directly and prime the cache. Otherwise fetch it once via
   * `getPartById` and patch any matching line(s) when it resolves.
   */
  private resolvePartLabel(partId: string, providedName?: string | null): string {
    if (providedName) {
      this.partNameCache.set(partId, providedName);
      return providedName;
    }
    const cached = this.partNameCache.get(partId);
    if (cached) return cached;

    this.fetchPartName(partId);
    return partId;
  }

  private fetchPartName(partId: string): void {
    if (!partId || this.partNameLoading.has(partId) || this.partNameCache.has(partId)) return;
    this.partNameLoading.add(partId);
    this.partService.getPartById(partId).subscribe({
      next: (part) => {
        const label = part.displayName || part.name || partId;
        this.partNameCache.set(partId, label);
        this.partNameLoading.delete(partId);
        // Patch any already-built line(s) that were waiting on this name.
        this.lineItemsArray.controls.forEach(ctrl => {
          if (ctrl.get('partId')?.value === partId && !ctrl.get('displayName')?.value) {
            ctrl.get('displayName')?.setValue(label);
            ctrl.get('part')?.setValue({ id: partId, name: label, displayName: label });
          }
        });
      },
      error: (_error) => {
        this.partNameLoading.delete(partId);
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
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('purchaseReturns.form.loadReturnFailed')
        });
      }
    });
  }

  /**
   * If opened with a ?goodsReceiptId= query param, pre-fill the return from that GRN's
   * remaining damaged/wrong units (each line pre-pointed at its damaged/quarantine lot).
   */
  private checkPrefillFromGrn(): void {
    const grnId = this.route.snapshot.queryParamMap.get('goodsReceiptId');
    if (!grnId) return;

    this.prService.getReturnPrefillFromGrn(grnId).subscribe({
      next: (prefill) => this.applyPrefill(prefill),
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('purchaseReturns.form.loadReturnDraftFailed')
        });
      }
    });
  }

  /**
   * Apply a GRN-based draft: load the PO, then build one line per remaining damaged/wrong unit.
   */
  private applyPrefill(prefill: ReturnPrefillFromGrn): void {
    if (!prefill.lines || prefill.lines.length === 0) {
      this.messageService.add({
        severity: 'info',
        summary: this.i18n.t('purchaseReturns.form.nothingToReturnSummary'),
        detail: this.i18n.t('purchaseReturns.form.nothingToReturnDetail', { grn: prefill.grnNumber })
      });
      return;
    }

    this.poService.getPurchaseOrderById(prefill.purchaseOrderId).subscribe({
      next: (po) => {
        this.selectedPurchaseOrder = po;
        this.availablePOItems = po.lines || [];
        if (!this.purchaseOrders.find(p => p.id === po.id)) {
          this.purchaseOrders = [po, ...this.purchaseOrders];
        }

        this.form.patchValue({
          purchaseOrderId: po,
          reason: prefill.reason === 'WRONG_ITEM' ? 'Wrong Item' : 'Damaged',
          notes: this.i18n.t('purchaseReturns.form.createdFromGrnNote', { grn: prefill.grnNumber })
        });

        this.lineItemsArray.clear();
        prefill.lines.forEach(line => {
          this.lineItemsArray.push(this.createLineItem({
            purchaseOrderLineId: line.purchaseOrderLineId,
            partId: line.partId,
            displayName: line.displayName,
            stockLotId: line.stockLotId,
            quantity: line.quantity,
            unitPrice: line.unitPrice,
            condition: line.condition,
            notes: line.notes,
            sourceBucket: line.bucket
          }));
        });

        this.messageService.add({
          severity: 'info',
          summary: this.i18n.t('purchaseReturns.form.draftFromGrnSummary'),
          detail: this.i18n.t('purchaseReturns.form.draftFromGrnDetail', { count: prefill.lines.length, grn: prefill.grnNumber }),
          life: 5000
        });
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: this.i18n.t('purchaseReturns.form.loadPOForDraftFailed')
        });
      }
    });
  }

  /**
   * Create line item form group
   */
  private createLineItem(lineData?: any): FormGroup {
    // Resolve a display object for the `part` control from the line's own data (no full-catalog
    // preload needed — see resolvePartLabel()).
    let partObj: any = null;
    if (lineData?.partId) {
      const label = this.resolvePartLabel(lineData.partId, lineData.displayName);
      partObj = { id: lineData.partId, name: label, displayName: label };
      // Load available lots for this part
      this.loadAvailableLotsForPart(lineData.partId);
    }

    return this.fb.group({
      id: [lineData?.id || ''],
      purchaseOrderLineId: [lineData?.purchaseOrderLineId || ''],
      part: [partObj, Validators.required],
      partId: [lineData?.partId || '', Validators.required],
      displayName: [lineData?.displayName || partObj?.name || ''],
      partLocalName: [lineData?.partLocalName || null],
      sourceBucket: [lineData?.sourceBucket || 'AVAILABLE'],  // Filters the lot picker; not persisted
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
  loadAvailableLotsForPart(partId: string, forceRefresh = false): void {
    if (!partId) return;
    if (!forceRefresh && this.availableLotsMap.has(partId)) return;

    const supplierId = this.selectedPurchaseOrder?.supplierId;
    const warehouseId = this.selectedPurchaseOrder?.warehouseId;

    this.loadingLotsMap.set(partId, true);
    // Load all buckets for the part; the per-line Source dropdown filters client-side.
    this.prService.getAvailableLotsForReturn(partId, supplierId, undefined, warehouseId).subscribe({
      next: (lots) => {
        this.availableLotsMap.set(partId, lots);
        this.loadingLotsMap.set(partId, false);
        // Reconcile each line's Source bucket with its selected lot's status (edit/view of existing returns)
        this.lineItemsArray.controls.forEach(ctrl => {
          if (ctrl.get('partId')?.value === partId) {
            const lotId = ctrl.get('stockLotId')?.value;
            if (lotId) {
              const lot = lots.find(l => l.lotId === lotId);
              if (lot?.status) ctrl.get('sourceBucket')?.setValue(lot.status, { emitEvent: false });
            }
          }
        });
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
   * Lots for a line, filtered to the chosen Source bucket (client-side).
   */
  getLotsForLine(partId: string, bucket: string): AvailableLotForReturn[] {
    const all = this.availableLotsMap.get(partId) || [];
    const b = bucket || 'AVAILABLE';
    return all.filter(l => (l.status || 'AVAILABLE') === b);
  }

  /**
   * Source bucket changed for a line: clear the previously selected lot (options have changed).
   */
  onSourceBucketChange(rowIndex: number): void {
    this.lineItemsArray.at(rowIndex)?.patchValue({ stockLotId: null });
  }

  /**
   * Check if lots are loading for a part
   */
  isLoadingLots(partId: string): boolean {
    return this.loadingLotsMap.get(partId) || false;
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
        summary: this.i18n.t('common.messages.warning'),
        detail: this.i18n.t('purchaseReturns.form.atLeastOneLineWarning')
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
    if (event && event.id) {
      this.poService.getPurchaseOrderById(event.id).subscribe({
        next: (po) => {
          this.selectedPurchaseOrder = po;
          this.availablePOItems = po.lines || [];

          // Clear lot cache when PO (and therefore supplier) changes
          this.availableLotsMap.clear();
          this.loadingLotsMap.clear();

          // Clear existing line items
          this.lineItemsArray.clear();

          this.messageService.add({
            severity: 'info',
            summary: this.i18n.t('purchaseReturns.form.poLoadedSummary'),
            detail: this.i18n.t('purchaseReturns.form.poLoadedDetail', { count: this.availablePOItems.length, supplier: po.supplierName || this.i18n.t('purchaseReturns.form.supplierLabel') }),
            life: 5000
          });
        },
        error: (_error) => {
          this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: this.i18n.t('purchaseReturns.form.loadPODetailsFailed') });
        }
      });
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
        summary: this.i18n.t('purchaseReturns.form.noPOSummary'),
        detail: this.i18n.t('purchaseReturns.form.noPODetail')
      });
      return;
    }

    this.lineItemsArray.clear();
    this.availablePOItems.forEach(poLine => {
      this.addPOLineItem(poLine);
    });

    this.messageService.add({
      severity: 'success',
      summary: this.i18n.t('purchaseReturns.form.itemsAddedSummary'),
      detail: this.i18n.t('purchaseReturns.form.itemsAddedDetail', { count: this.availablePOItems.length })
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
        summary: this.i18n.t('purchaseReturns.form.noPOSummary'),
        detail: this.i18n.t('purchaseReturns.form.noPODetail')
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
        summary: this.i18n.t('purchaseReturns.form.noItemsSelectedSummary'),
        detail: this.i18n.t('purchaseReturns.form.noItemsSelectedDetail')
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
      summary: this.i18n.t('purchaseReturns.form.itemsAddedSummary'),
      detail: this.i18n.t('purchaseReturns.form.itemsAddedDetail', { count: this.selectedPOItems.length })
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
    const label = this.resolvePartLabel(poLine.partId, poLine.displayName || poLine.partName);
    const partObj = { id: poLine.partId, name: label, displayName: label };

    // Load available lots for this part
    if (poLine.partId) {
      this.loadAvailableLotsForPart(poLine.partId);
    }

    const lineItem = this.fb.group({
      id: [''],
      purchaseOrderLineId: [poLine.id, Validators.required],
      part: [partObj, Validators.required],
      partId: [poLine.partId, Validators.required],
      displayName: [poLine.displayName || partObj?.name || ''],
      sourceBucket: ['AVAILABLE'],  // Filters the lot picker; not persisted
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
   * Submit form
   */
  onSubmit(): void {
    if (this.form.invalid) {
      this.messageService.add({
        severity: 'error',
        summary: this.i18n.t('common.messages.validationError'),
        detail: this.i18n.t('common.messages.fillRequiredFields')
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
        summary: this.i18n.t('common.messages.error'),
        detail: this.i18n.t('purchaseReturns.form.invalidPOSelected')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('purchaseReturns.form.updateSuccessDetail')
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('common.messages.error'),
            detail: error?.error?.message || this.i18n.t('purchaseReturns.form.updateFailedDetail')
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
            summary: this.i18n.t('common.messages.success'),
            detail: this.i18n.t('purchaseReturns.form.createSuccessDetail')
          });
          this.router.navigate(['/procurement/purchase-returns']);
          this.isSubmitting = false;
        },
        error: (error) => {
          this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('common.messages.error'),
            detail: error?.error?.message || this.i18n.t('purchaseReturns.form.createFailedDetail')
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
    return this.resolvePartLabel(partId);
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
  getStatusSeverity(status: string): StatusSeverity {
    return this.statusDisplay.getSeverity(status, 'purchase-return');
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
   * Approve purchase return
   */
  approveReturn(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.form.approveConfirmMessage', { number: this.currentReturn.returnNumber }),
      header: this.i18n.t('purchaseReturns.form.approveConfirmHeader'),
      icon: 'pi pi-check',
      accept: () => {
        this.prService.approvePurchaseReturn(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseReturns.form.approveSuccessDetail', { number: updated.returnNumber })
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseReturns.messages.approveFailed')
            });
          }
        });
      }
    });
  }

  /**
   * Mark purchase return as returned
   */
  markAsReturned(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.form.markReturnedConfirmMessage', { number: this.currentReturn.returnNumber }),
      header: this.i18n.t('purchaseReturns.form.confirmHeader'),
      icon: 'pi pi-send',
      accept: () => {
        this.prService.markAsReturned(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseReturns.form.markReturnedSuccessDetail', { number: updated.returnNumber })
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseReturns.form.markReturnedFailedDetail')
            });
          }
        });
      }
    });
  }

  /**
   * Mark purchase return as received by supplier
   */
  markAsReceived(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.form.markReceivedConfirmMessage', { number: this.currentReturn.returnNumber }),
      header: this.i18n.t('purchaseReturns.form.confirmHeader'),
      icon: 'pi pi-inbox',
      accept: () => {
        this.prService.markAsReceived(this.currentReturn!.id).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseReturns.form.markReceivedSuccessDetail', { number: updated.returnNumber })
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseReturns.form.markReceivedFailedDetail')
            });
          }
        });
      }
    });
  }

  /**
   * Issue credit note for purchase return
   */
  issueCreditNote(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.form.issueCreditNoteConfirmMessage', {
        amount: this.currencyService.formatCurrency(this.currentReturn.refundAmount, this.currencyCode),
        number: this.currentReturn.returnNumber
      }),
      header: this.i18n.t('purchaseReturns.form.issueCreditNote'),
      icon: 'pi pi-file',
      accept: () => {
        this.prService.issueCreditNote(this.currentReturn!.id, this.currentReturn!.refundAmount).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseReturns.form.issueCreditNoteSuccessDetail', { number: updated.returnNumber })
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseReturns.form.issueCreditNoteFailedDetail')
            });
          }
        });
      }
    });
  }

  /**
   * Reject purchase return
   */
  rejectReturn(): void {
    if (!this.currentReturn) return;

    this.confirmationService.confirm({
      message: this.i18n.t('purchaseReturns.form.rejectConfirmMessage', { number: this.currentReturn.returnNumber }),
      header: this.i18n.t('purchaseReturns.form.rejectConfirmHeader'),
      icon: 'pi pi-times',
      acceptButtonStyleClass: 'p-button-danger',
      accept: () => {
        this.prService.rejectPurchaseReturn(this.currentReturn!.id, this.i18n.t('purchaseReturns.form.rejectedByUserReason')).subscribe({
          next: (updated) => {
            this.currentReturn = updated;
            this.messageService.add({
              severity: 'success',
              summary: this.i18n.t('common.messages.success'),
              detail: this.i18n.t('purchaseReturns.form.rejectSuccessDetail', { number: updated.returnNumber })
            });
          },
          error: (error) => {
            this.messageService.add({
              severity: 'error',
              summary: this.i18n.t('common.messages.error'),
              detail: error?.error?.message || this.i18n.t('purchaseReturns.form.rejectFailedDetail')
            });
          }
        });
      }
    });
  }

  /**
   * Download the server-rendered Purchase Return PDF and show it in the preview dialog.
   */
  printReturn(): void {
    if (!this.currentReturn) {
      this.messageService.add({
        severity: 'warn',
        summary: this.i18n.t('common.messages.warning'),
        detail: this.i18n.t('purchaseReturns.form.noPrintData')
      });
      return;
    }

    this.prService.downloadPdf(this.currentReturn.id, this.currentReturn.returnNumber).subscribe({
      error: (error) => {
        this.messageService.add({
          severity: 'error',
          summary: this.i18n.t('common.messages.error'),
          detail: error?.error?.message || this.i18n.t('purchaseReturns.form.printFailed')
        });
        console.error('Error downloading purchase return PDF:', error);
      }
    });
  }
}
