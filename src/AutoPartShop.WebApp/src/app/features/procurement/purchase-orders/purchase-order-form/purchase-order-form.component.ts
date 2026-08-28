import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { TagModule } from 'primeng/tag';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { SupplierService, SupplierResponse, SupplierQuery } from '../../../inventory/services/supplier.service';
import { PartService, PartResponse, PartsQuery } from '../../../inventory/services/part.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { UnitConversionService } from '../../../inventory/services/unit-conversion.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CurrencySelectorComponent } from '../../../../shared/components/currency-selector/currency-selector.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../../shared/components/lazy-autocomplete';
import { DatePicker } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { tap, forkJoin, of } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApplyCreditNotesComponent } from '../../purchase-credits/apply-credit-notes.component';
import { CreditNoteService } from '../../services/credit-note.service';
import { StatusDisplayService } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-purchase-order-form',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        ButtonModule,
        InputTextModule,
        InputNumberModule,
        SelectModule,
        CardModule,
        DividerModule,
        ToastModule,
        CurrencySelectorComponent,
        DatePicker,
        ConfirmDialogModule,
        TooltipModule,
        TextareaModule,
        TagModule,
        LazyAutocompleteComponent,
        ApplyCreditNotesComponent,
        TranslatePipe
    ],
    templateUrl: './purchase-order-form.component.html',
    styleUrls: ['./purchase-order-form.component.css'],
    providers: [MessageService, ConfirmationService]
})
export class PurchaseOrderFormComponent implements OnInit {
    form: FormGroup;
    isEditing = false;
    isViewing = false;
    mode: 'create' | 'edit' | 'view' = 'create';
    isSubmitting = false;
    poId: string | null = null;
    currentPO: PurchaseOrderResponse | null = null;

    // Lazy load functions
    fetchSuppliersLazy = (req: LazyRequest) =>
      this.supplierService.getSuppliers({
        search: req.search,
        pageNumber: req.pageNumber,
        pageSize: req.pageSize
      } as SupplierQuery).pipe(
        map(res => ({
          items: res.data,
          totalCount: res.pagination.totalCount
        } as LazyResponse<SupplierResponse>))
      );

    fetchPartsLazy = (req: LazyRequest) =>
      this.partService.getParts({
        search: req.search,
        pageNumber: req.pageNumber,
        pageSize: req.pageSize,
        flattenVariants: true
      } as PartsQuery).pipe(
        map(res => ({
          items: res.data,
          totalCount: res.pagination.totalCount
        } as LazyResponse<PartResponse>))
      );

    // Autocomplete data
    units: UnitResponse[] = [];
    compatibleUnitsMap = new Map<string, UnitResponse[]>();
    lineUnitsMap = new Map<number, UnitResponse[]>();
    loadingUnitsForLine = new Set<number>();
    lineUnitSelection = new Map<number, string | null>();

    // Product search
    selectedPartToAdd: PartResponse | null = null;

    /** Getters, not fields: a field freezes the labels in the language active at construction. */
    get paymentTermsOptions() {
        return [
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.NET15'), value: 'NET15' },
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.NET30'), value: 'NET30' },
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.NET45'), value: 'NET45' },
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.NET60'), value: 'NET60' },
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.COD'), value: 'COD' },
            { label: this.i18n.t('purchaseOrders.form.paymentTerms.PREPAID'), value: 'PREPAID' }
        ];
    }

    get priorityOptions() {
        return [
            { label: this.i18n.t('purchaseOrders.form.priorities.LOW'), value: 'LOW', severity: 'info' },
            { label: this.i18n.t('purchaseOrders.form.priorities.MEDIUM'), value: 'MEDIUM', severity: 'warning' },
            { label: this.i18n.t('purchaseOrders.form.priorities.HIGH'), value: 'HIGH', severity: 'danger' }
        ];
    }

    private readonly poService = inject(PurchaseOrderService);
    private readonly supplierService = inject(SupplierService);
    private readonly partService = inject(PartService);
    private readonly unitService = inject(UnitService);
    private readonly unitConversionService = inject(UnitConversionService);
    private readonly currencyService = inject(CurrencyService);
    private readonly creditNoteService = inject(CreditNoteService);
    private readonly statusDisplay = inject(StatusDisplayService);
    private readonly i18n = inject(I18nService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    // Credit note state
    totalCreditApplied = 0;
    availableCreditForSupplier = 0;
    private readonly fb = inject(FormBuilder);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);

    constructor() {
        this.form = this.createForm();
    }

    ngOnInit(): void {
        this.route.queryParams.pipe(
            tap({
                next: (params) => {
                    const currentPath = this.route.snapshot.routeConfig?.path || '';
                    const poId = params["id"];
                    if (poId) {
                        this.poId = poId;
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
                        this.loadPurchaseOrder(poId);
                    } else {
                        this.mode = 'create';
                    }
                },
                error: (error) => {
                    console.error('Failed to load purchase order:', error);
                }
            }),
        ).subscribe();
    }


    private loadPurchaseOrder(id: string): void {
        this.poService.getPurchaseOrderById(id).subscribe({
            next: (po) => {
                this.currentPO = po;

                const paymentTermsValue = po.paymentTerms || 'NET30';
                const paymentTermsOption = this.paymentTermsOptions.find(pt => pt.value === paymentTermsValue);

                this.form.patchValue({
                    supplier: { id: po.supplierId, name: po.supplierName },
                    deliveryDate: po.deliveryDate ? new Date(po.deliveryDate) : null,
                    paymentTerms: paymentTermsOption?.value || 'NET30',
                    currency: this.currencyService.selectedCurrency(),
                    priority: 'MEDIUM',
                    notes: po.notes,
                    taxRate: po.taxPercentage || 0,
                    discountPercentage: po.discountPercentage || 0,
                    discountAmount: po.discountAmount || 0,
                    discountType: po.discountType || 'TOTAL'
                });

                const linesArray = this.linesArray;
                linesArray.clear();
                this.lineUnitsMap.clear();

                po.lines?.forEach((line, index) => {
                    const baseUnitId = line.partBaseUnitId || line.unitId || null;
                    const matchingPart = {
                        id: line.partId,
                        name: line.partName,
                        displayName: line.displayName || line.partName,
                        unitId: baseUnitId,
                        variantId: line.variantId || null,
                        variantName: line.variantName || null,
                        variantCode: line.variantCode || null
                    };
                    const matchingUnit = line.unitId
                        ? ({ id: line.unitId, name: line.unitName, symbol: line.unitSymbol } as UnitResponse)
                        : null;

                    linesArray.push(this.fb.group({
                        partId: [matchingPart || line.partId, Validators.required],
                        variantId: [line.variantId || null],
                        unitId: [matchingUnit],
                        quantity: [line.quantity, [Validators.required, Validators.min(1)]],
                        unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]]
                    }));

                    // Set current unit immediately so dropdown has options
                    if (matchingUnit) {
                        this.lineUnitsMap.set(index, [matchingUnit]);
                    }
                    this.lineUnitSelection.set(index, line.unitId || baseUnitId);

                    // Load compatible units for each line
                    if (baseUnitId) {
                        this.unitService.getCompatibleUnits(baseUnitId).subscribe({
                            next: (compatibleUnits) => {
                                this.compatibleUnitsMap.set(line.partId, compatibleUnits);
                                this.lineUnitsMap.set(index, compatibleUnits);
                            },
                            error: () => {
                                // Keep the current unit if API fails
                            }
                        });
                    }
                });

                if (this.isViewing) {
                    this.form.disable();
                }
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: this.i18n.t('purchaseOrders.form.formMessages.loadFailed')
                });
                console.error('Error loading purchase order:', error);
            }
        });
    }

    private createForm(): FormGroup {
        const defaultCurrency = this.currencyService.selectedCurrency();

        return this.fb.group({
            supplier: ['', Validators.required],
            deliveryDate: ['', Validators.required],
            paymentTerms: ['NET30', Validators.required],
            currency: [defaultCurrency, Validators.required],
            priority: ['MEDIUM', Validators.required],
            taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
            discountPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
            discountAmount: [0],
            discountType: ['TOTAL'], // 'BULK' or 'TOTAL'
            notes: [''],
            lines: this.fb.array([])
        });
    }

    onDiscountTypeChange(type: 'BULK' | 'TOTAL'): void {
        this.form.patchValue({ discountType: type });
    }

    get linesArray(): FormArray {
        return this.form.get('lines') as FormArray;
    }

    removeLineItem(index: number): void {
        this.linesArray.removeAt(index);
        // Re-index lineUnitsMap after removal
        const newMap = new Map<number, UnitResponse[]>();
        this.lineUnitsMap.forEach((units, i) => {
            if (i < index) {
                newMap.set(i, units);
            } else if (i > index) {
                newMap.set(i - 1, units);
            }
        });
        this.lineUnitsMap = newMap;
    }

    getLineTotal(index: number): number {
        const line = this.linesArray.at(index);
        const quantity = line?.get('quantity')?.value || 0;
        const unitPrice = line?.get('unitPrice')?.value || 0;
        return quantity * unitPrice;
    }

    getSubtotal(): number {
        return this.linesArray.controls.reduce((total, line) => {
            return total + this.getLineTotal(this.linesArray.controls.indexOf(line));
        }, 0);
    }

    getTaxAmount(): number {
        const subtotal = this.getSubtotal();
        const taxRate = this.form.get('taxRate')?.value || 0;
        return (subtotal * taxRate) / 100;
    }

    getDiscountAmount(): number {
        const subtotal = this.getSubtotal();
        const discountPercentage = this.form.get('discountPercentage')?.value || 0;
        const manualDiscountAmount = this.form.get('discountAmount')?.value || 0;
        const discountType = this.form.get('discountType')?.value || 'TOTAL';

        let percentageDiscount = 0;

        // For TOTAL discount, apply percentage to subtotal
        if (discountType === 'TOTAL') {
            percentageDiscount = (subtotal * discountPercentage) / 100;
        }
        // For BULK discount, sum up individual line discounts
        else {
            percentageDiscount = this.linesArray.controls.reduce((totalDiscount, line, index) => {
                const lineTotal = this.getLineTotal(index);
                const lineDiscount = (lineTotal * discountPercentage) / 100;
                return totalDiscount + lineDiscount;
            }, 0);
        }

        // Use the larger of percentage discount or manual amount
        return Math.max(percentageDiscount, manualDiscountAmount);
    }

    getGrandTotal(): number {
        const subtotal = this.getSubtotal();
        const tax = this.getTaxAmount();
        const discount = this.getDiscountAmount();
        return subtotal + tax - discount;
    }

    formatCurrency(value: number): string {
        const currencyCode = this.form.get('currency')?.value || this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(value, currencyCode);
    }

    onSubmit(): void {
        if (this.form.invalid) {
            this.markFormGroupTouched(this.form);
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('purchaseOrders.form.formMessages.validationError'),
                detail: this.i18n.t('purchaseOrders.form.formMessages.fillRequired')
            });
            return;
        }

        if (this.linesArray.length === 0) {
            this.messageService.add({
                severity: 'error',
                summary: this.i18n.t('purchaseOrders.form.formMessages.validationError'),
                detail: this.i18n.t('purchaseOrders.form.formMessages.addAtLeastOne')
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

    private createPurchaseOrder(): void {
        const lineItems = this.linesArray.value.map((line: any) => ({
            partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
            variantId: line.variantId ?? null,
            unitId: typeof line.unitId === 'string' ? line.unitId : line.unitId?.id,
            quantity: line.quantity,
            unitPrice: line.unitPrice
        }));

        const supplier = this.form.value.supplier;
        const request = {
            supplierId: typeof supplier === 'string' ? supplier : supplier?.id,
            deliveryDate: this.form.value.deliveryDate,
            taxPercentage: this.form.value.taxRate || 0,
            discountPercentage: this.form.value.discountPercentage || 0,
            discountAmount: this.form.value.discountAmount || 0,
            discountType: this.form.value.discountType || 'TOTAL',
            notes: this.form.value.notes,
            lineItems
        };

        this.poService.createPurchaseOrder(request).subscribe({
            next: (po) => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('purchaseOrders.form.formMessages.createSuccess', { number: po.poNumber })
                });
                setTimeout(() => {
                    this.router.navigate(['/procurement/purchase-orders']);
                }, 1500);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.createFailed')
                });
                console.error('Error:', error);
                this.isSubmitting = false;
            }
        });
    }

    private updatePurchaseOrder(): void {
        if (!this.poId) return;

        const lineItems = this.linesArray.value.map((line: any) => ({
            partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
            variantId: line.variantId ?? null,
            unitId: typeof line.unitId === 'string' ? line.unitId : line.unitId?.id,
            quantity: line.quantity,
            unitPrice: line.unitPrice
        }));

        const supplier = this.form.value.supplier;
        const request = {
            id: this.poId,
            supplierId: typeof supplier === 'string' ? supplier : supplier?.id,
            deliveryDate: this.form.value.deliveryDate,
            taxPercentage: this.form.value.taxRate || 0,
            discountPercentage: this.form.value.discountPercentage || 0,
            discountAmount: this.form.value.discountAmount || 0,
            discountType: this.form.value.discountType || 'TOTAL',
            notes: this.form.value.notes,
            lineItems
        };

        this.poService.updatePurchaseOrder(this.poId, request).subscribe({
            next: (po) => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t('purchaseOrders.form.formMessages.updateSuccess', { number: po.poNumber })
                });
                setTimeout(() => {
                    this.router.navigate(['/procurement/purchase-orders']);
                }, 1500);
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.updateFailed')
                });
                console.error('Error:', error);
                this.isSubmitting = false;
            }
        });
    }

    onCancel(): void {
        this.router.navigate(['/procurement/purchase-orders']);
    }

    /**
     * Handler for when credit is applied to this PO
     */
    onCreditApplied(amount: number): void {
        this.totalCreditApplied += amount;
        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('purchaseOrders.form.formMessages.creditAppliedSummary'),
            detail: this.i18n.t('purchaseOrders.form.formMessages.creditAppliedDetail', { amount: this.formatCurrency(amount) })
        });

        // Reload PO to get updated data
        if (this.poId) {
            this.loadPurchaseOrder(this.poId);
        }

        // Refresh available credit for supplier
        const supplierValue = this.form.get('supplier')?.value;
        const supplierId = typeof supplierValue === 'string' ? supplierValue : supplierValue?.id;
        if (supplierId) {
            this.loadAvailableCreditForSupplier(supplierId);
        }
    }

    /**
     * Load available credit for the selected supplier
     */
    loadAvailableCreditForSupplier(supplierId: string): void {
        this.creditNoteService.getTotalAvailableCredit(supplierId).subscribe({
            next: (response: { totalAvailableCredit: number }) => {
                this.availableCreditForSupplier = response.totalAvailableCredit;
            },
            error: () => {
                // Silently fail - credit info is optional
                this.availableCreditForSupplier = 0;
            }
        });
    }

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

    private formatFieldName(fieldName: string): string {
        return fieldName
            .replace(/([A-Z])/g, ' $1')
            .replace(/^./, str => str.toUpperCase())
            .trim();
    }

    hasError(fieldName: string): boolean {
        const control = this.form.get(fieldName);
        return !!(control && control.invalid && control.touched);
    }

    getPageTitle(): string {
        if (this.mode === 'view') return this.i18n.t('purchaseOrders.form.viewTitle');
        if (this.mode === 'edit') return this.i18n.t('purchaseOrders.form.editTitle');
        return this.i18n.t('purchaseOrders.form.createTitle');
    }

    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        return this.statusDisplay.getSeverity(status, 'purchase-order');
    }

    /** Localized status label; falls back to a humanized form when untranslated. */
    statusLabel(status: string): string {
        return this.statusDisplay.getLabel(status, 'purchaseOrders.statusOptions');
    }

    submitPurchaseOrder(): void {
        if (!this.poId || !this.currentPO) return;

        this.confirmationService.confirm({
            message: this.i18n.t('purchaseOrders.form.formMessages.submitConfirm', { number: this.currentPO.poNumber }),
            header: this.i18n.t('purchaseOrders.form.formMessages.submitHeader'),
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.poService.submitPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('purchaseOrders.form.formMessages.submitSuccess', { number: this.currentPO!.poNumber })
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.submitFailed')
                        });
                        console.error('Error submitting purchase order:', error);
                    }
                });
            }
        });
    }

    confirmPurchaseOrder(): void {

        if (!this.poId || !this.currentPO) return;

        this.confirmationService.confirm({
            message: this.i18n.t('purchaseOrders.form.formMessages.confirmMessage', { number: this.currentPO.poNumber }),
            header: this.i18n.t('purchaseOrders.form.formMessages.confirmHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.poService.confirmPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('purchaseOrders.form.formMessages.confirmSuccess', { number: this.currentPO!.poNumber })
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.confirmFailed')
                        });
                        console.error('Error confirming purchase order:', error);
                    }
                });
            }
        });
    }

    cancelPurchaseOrder(): void {
        if (!this.poId || !this.currentPO) return;

        this.confirmationService.confirm({
            message: this.i18n.t('purchaseOrders.form.formMessages.cancelConfirm', { number: this.currentPO.poNumber }),
            header: this.i18n.t('purchaseOrders.form.formMessages.cancelHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-warning',
            accept: () => {
                this.poService.cancelPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('common.messages.success'),
                            detail: this.i18n.t('purchaseOrders.form.formMessages.cancelSuccess', { number: this.currentPO!.poNumber })
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: this.i18n.t('common.messages.error'),
                            detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.cancelFailed')
                        });
                        console.error('Error cancelling purchase order:', error);
                    }
                });
            }
        });
    }

    onPartSelectAndAdd(event: any): void {
        const selectedPart = event;
        if (!selectedPart) return;

        // Match by partId + variantId so the same part in different variants is a separate line
        const existingIndex = this.linesArray.controls.findIndex(line => {
            const partVal = line.get('partId')?.value;
            const partId = typeof partVal === 'string' ? partVal : partVal?.id;
            const variantId = line.get('variantId')?.value;
            return partId === selectedPart.id && variantId === (selectedPart.variantId ?? null);
        });

        const selectedUnit = { id: selectedPart.unitId, name: selectedPart.unitName, symbol: selectedPart.unitSymbol || selectedPart.unitName } as UnitResponse;
        const displayLabel = selectedPart.displayName || selectedPart.name;

        if (existingIndex >= 0) {
            const existingLine = this.linesArray.at(existingIndex);
            const currentQty = existingLine.get('quantity')?.value || 0;
            existingLine.patchValue({ quantity: currentQty + 1 });
            this.messageService.add({
                severity: 'info',
                summary: this.i18n.t('purchaseOrders.form.formMessages.updated'),
                detail: this.i18n.t('purchaseOrders.form.formMessages.qtyIncreased', { name: displayLabel })
            });
        } else {
            const newLine = this.fb.group({
                partId: [selectedPart, Validators.required],
                variantId: [selectedPart.variantId ?? null],
                unitId: [selectedUnit],
                quantity: [1, [Validators.required, Validators.min(1)]],
                // Use effectiveSellingPrice (accounts for OVERRIDE vs ADDITIVE pricing)
                unitPrice: [selectedPart.effectiveSellingPrice ?? selectedPart.sellingPrice ?? 0, [Validators.required, Validators.min(0)]]
            });
            this.linesArray.push(newLine);
            const newLineIndex = this.linesArray.length - 1;

            this.lineUnitsMap.set(newLineIndex, [selectedUnit]);
            this.lineUnitSelection.set(newLineIndex, selectedUnit.id || null);

            if (selectedPart.unitId) {
                this.unitService.getCompatibleUnits(selectedPart.unitId).subscribe({
                    next: (compatibleUnits) => {
                        this.compatibleUnitsMap.set(selectedPart.id, compatibleUnits);
                        this.lineUnitsMap.set(newLineIndex, compatibleUnits);
                    },
                    error: () => {}
                });
            }

            this.messageService.add({
                severity: 'success',
                summary: this.i18n.t('purchaseOrders.form.formMessages.added'),
                detail: this.i18n.t('purchaseOrders.form.formMessages.addedDetail', { name: displayLabel })
            });
        }

        setTimeout(() => {
            this.selectedPartToAdd = null;
        }, 100);
    }

    navigateToStock(partId: any): void {
        const id = typeof partId === 'string' ? partId : partId?.id;
        if (!id) return;

        const url = `/inventory/stock?partId=${id}`;
        window.open(url, '_blank');
    }

    getCompatibleUnitsForLine(lineIndex: number): UnitResponse[] {
        return this.lineUnitsMap.get(lineIndex) || [];
    }

    onUnitChanged(lineIndex: number): void {
        const line = this.linesArray.at(lineIndex) as FormGroup | null;
        if (!line) return;

        const partValue = line.get('partId')?.value;
        const part = typeof partValue === 'string' ? null : partValue;
        if (!part?.unitId) {
            this.lineUnitSelection.set(lineIndex, null);
            return;
        }

        const currentUnitValue = line.get('unitId')?.value;
        const currentUnitId = typeof currentUnitValue === 'string' ? currentUnitValue : currentUnitValue?.id;
        const previousUnitId = this.lineUnitSelection.get(lineIndex) || part.unitId;
        const nextUnitId = currentUnitId || part.unitId;
        if (previousUnitId === nextUnitId) return;

        const currentPrice = Number(line.get('unitPrice')?.value || 0);
        const fromFactor$ = previousUnitId === part.unitId
            ? of(1)
            : this.unitConversionService.getConversion(previousUnitId, part.unitId).pipe(map(res => res.conversionFactor));
        const toFactor$ = nextUnitId === part.unitId
            ? of(1)
            : this.unitConversionService.getConversion(nextUnitId, part.unitId).pipe(map(res => res.conversionFactor));

        forkJoin({ fromFactor: fromFactor$, toFactor: toFactor$ }).subscribe({
            next: ({ fromFactor, toFactor }) => {
                const basePrice = fromFactor > 0 ? currentPrice / fromFactor : currentPrice;
                const newPrice = basePrice * toFactor;
                line.patchValue({ unitPrice: this.roundPrice(newPrice) }, { emitEvent: false });
                this.lineUnitSelection.set(lineIndex, nextUnitId);
            },
            error: (err) => {
                console.error('Error converting unit price:', err);
                this.messageService.add({
                    severity: 'warn',
                    summary: this.i18n.t('purchaseOrders.form.formMessages.unitConversionMissing'),
                    detail: this.i18n.t('purchaseOrders.form.formMessages.unitConversionMissingDetail')
                });
                this.lineUnitSelection.set(lineIndex, nextUnitId);
            }
        });
    }

    private roundPrice(value: number): number {
        return Math.round(value * 100) / 100;
    }

    loadCompatibleUnitsForLine(lineIndex: number): void {
        if (this.loadingUnitsForLine.has(lineIndex)) return;

        const line = this.linesArray.at(lineIndex);
        if (!line) return;

        const partId = line.get('partId')?.value;
        const part = typeof partId === 'string' ? null : partId;
        if (!part?.unitId) return;

        // Check if already loaded from compatibleUnitsMap
        if (this.compatibleUnitsMap.has(part.id)) {
            this.lineUnitsMap.set(lineIndex, this.compatibleUnitsMap.get(part.id) || []);
            return;
        }

        this.loadingUnitsForLine.add(lineIndex);

        this.unitService.getCompatibleUnits(part.unitId).subscribe({
            next: (compatibleUnits) => {
                this.compatibleUnitsMap.set(part.id, compatibleUnits);
                this.lineUnitsMap.set(lineIndex, compatibleUnits);
                this.loadingUnitsForLine.delete(lineIndex);
            },
            error: () => {
                this.lineUnitsMap.set(lineIndex, this.units);
                this.loadingUnitsForLine.delete(lineIndex);
            }
        });
    }

    onUnitFilter(event: any, lineIndex: number): void {
        const query = event.filter?.toLowerCase() || '';
        const allUnits = this.lineUnitsMap.get(lineIndex) || [];

        if (!query) {
            return;
        }

        const filtered = allUnits.filter(unit =>
            unit.name.toLowerCase().includes(query) ||
            unit.symbol?.toLowerCase().includes(query)
        );
        this.lineUnitsMap.set(lineIndex, filtered.length > 0 ? filtered : allUnits);
    }

    printPurchaseOrder(): void {
        if (!this.currentPO) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('common.messages.warning'),
                detail: this.i18n.t('purchaseOrders.form.formMessages.noPrintData')
            });
            return;
        }

        this.poService.downloadPdf(this.currentPO.id, this.currentPO.poNumber).subscribe({
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: this.i18n.t('common.messages.error'),
                    detail: error?.error?.message || this.i18n.t('purchaseOrders.form.formMessages.printFailed')
                });
                console.error('Error downloading purchase order PDF:', error);
            }
        });
    }
}
