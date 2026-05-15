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
        ApplyCreditNotesComponent
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
        pageSize: req.pageSize
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

    paymentTermsOptions = [
        { label: 'Net 15', value: 'NET15' },
        { label: 'Net 30', value: 'NET30' },
        { label: 'Net 45', value: 'NET45' },
        { label: 'Net 60', value: 'NET60' },
        { label: 'COD (Cash on Delivery)', value: 'COD' },
        { label: 'Prepaid', value: 'PREPAID' }
    ];

    priorityOptions = [
        { label: 'Low', value: 'LOW', severity: 'info' },
        { label: 'Medium', value: 'MEDIUM', severity: 'warning' },
        { label: 'High', value: 'HIGH', severity: 'danger' }
    ];

    private readonly poService = inject(PurchaseOrderService);
    private readonly supplierService = inject(SupplierService);
    private readonly partService = inject(PartService);
    private readonly unitService = inject(UnitService);
    private readonly unitConversionService = inject(UnitConversionService);
    private readonly currencyService = inject(CurrencyService);
    private readonly creditNoteService = inject(CreditNoteService);
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
                    console.log(error);
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
                    const matchingPart = { id: line.partId, name: line.partName, unitId: baseUnitId };
                    const matchingUnit = line.unitId
                        ? ({ id: line.unitId, name: line.unitName, symbol: line.unitSymbol } as UnitResponse)
                        : null;

                    linesArray.push(this.fb.group({
                        partId: [matchingPart || line.partId, Validators.required],
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
                    summary: 'Error',
                    detail: 'Failed to load purchase order'
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
                summary: 'Validation Error',
                detail: 'Please fill all required fields'
            });
            return;
        }

        if (this.linesArray.length === 0) {
            this.messageService.add({
                severity: 'error',
                summary: 'Validation Error',
                detail: 'Please add at least one product to the order'
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

    private updatePurchaseOrder(): void {
        if (!this.poId) return;

        const lineItems = this.linesArray.value.map((line: any) => ({
            partId: typeof line.partId === 'string' ? line.partId : line.partId.id,
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
            summary: 'Credit Applied',
            detail: `${this.formatCurrency(amount)} credit applied to this purchase order`
        });

        // Reload PO to get updated data
        if (this.poId) {
            this.loadPurchaseOrder(this.poId);
        }

        // Refresh available credit for supplier
        const supplierId = this.form.get('supplierId')?.value;
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
        if (this.mode === 'view') return 'View Purchase Order';
        if (this.mode === 'edit') return 'Edit Purchase Order';
        return 'Create New Purchase Order';
    }

    getStatusSeverity(status: string): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined {
        switch (status?.toUpperCase()) {
            case 'DRAFT':
                return 'warn';
            case 'SUBMITTED':
                return 'info';
            case 'CONFIRMED':
                return 'success';
            case 'PARTIAL':
                return 'warn';
            case 'DELIVERED':
                return 'success';
            case 'CANCELLED':
                return 'danger';
            default:
                return 'secondary';
        }
    }

    submitPurchaseOrder(): void {
        debugger;
        if (!this.poId || !this.currentPO) return;

        this.confirmationService.confirm({
            message: `Are you sure you want to submit Purchase Order ${this.currentPO.poNumber}?`,
            header: 'Confirm Submission',
            icon: 'pi pi-exclamation-triangle',
            accept: () => {
                this.poService.submitPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Purchase Order ${this.currentPO!.poNumber} submitted successfully`
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to submit purchase order'
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
            message: `Are you sure you want to confirm Purchase Order ${this.currentPO.poNumber}? This action will make the order official and binding.`,
            header: 'Confirm Purchase Order',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.poService.confirmPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Purchase Order ${this.currentPO!.poNumber} confirmed successfully`
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to confirm purchase order'
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
            message: `Are you sure you want to cancel Purchase Order ${this.currentPO.poNumber}?`,
            header: 'Confirm Cancellation',
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-warning',
            accept: () => {
                this.poService.cancelPurchaseOrder(this.poId!).subscribe({
                    next: () => {
                        this.messageService.add({
                            severity: 'success',
                            summary: 'Success',
                            detail: `Purchase Order ${this.currentPO!.poNumber} cancelled successfully`
                        });
                        this.loadPurchaseOrder(this.poId!);
                    },
                    error: (error) => {
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: error?.error?.message || 'Failed to cancel purchase order'
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

        const existingIndex = this.linesArray.controls.findIndex(line => {
            const partId = line.get('partId')?.value;
            const id = typeof partId === 'string' ? partId : partId?.id;
            return id === selectedPart.id;
        });

        const selectedUnit = { id: selectedPart.unitId, name: selectedPart.unitName, symbol: selectedPart.unitSymbol || selectedPart.unitName } as UnitResponse;

        if (existingIndex >= 0) {
            const existingLine = this.linesArray.at(existingIndex);
            const currentQty = existingLine.get('quantity')?.value || 0;
            existingLine.patchValue({
                quantity: currentQty + 1
            });
            this.messageService.add({
                severity: 'info',
                summary: 'Updated',
                detail: `Increased quantity for ${selectedPart.name}`
            });
        } else {
            const newLine = this.fb.group({
                partId: [selectedPart, Validators.required],
                unitId: [selectedUnit],
                quantity: [1, [Validators.required, Validators.min(1)]],
                unitPrice: [selectedPart.sellingPrice || 0, [Validators.required, Validators.min(0)]]
            });
            this.linesArray.push(newLine);
            const newLineIndex = this.linesArray.length - 1;

            // Set current unit immediately so dropdown has options
            this.lineUnitsMap.set(newLineIndex, [selectedUnit]);
            this.lineUnitSelection.set(newLineIndex, selectedUnit.id || null);

            if (selectedPart.unitId) {
                this.unitService.getCompatibleUnits(selectedPart.unitId).subscribe({
                    next: (compatibleUnits) => {
                        this.compatibleUnitsMap.set(selectedPart.id, compatibleUnits);
                        this.lineUnitsMap.set(newLineIndex, compatibleUnits);
                    },
                    error: () => {
                        // Keep the current unit if API fails
                    }
                });
            }

            this.messageService.add({
                severity: 'success',
                summary: 'Added',
                detail: `Added ${selectedPart.name} to order`
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
        debugger;
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
                    summary: 'Unit Conversion Missing',
                    detail: 'No conversion configured between the selected units.'
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
                summary: 'Warning',
                detail: 'No purchase order data available to print'
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

    private generatePrintContent(): string {
        const po = this.currentPO!;
        const orderDate = po.orderDate ? new Date(po.orderDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : '-';
        const deliveryDate = po.deliveryDate ? new Date(po.deliveryDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : '-';
        const currencyCode = this.form.get('currency')?.value || this.currencyService.selectedCurrency();

        const lineItemsHtml = (po.lines || []).map((line) => `
            <tr>
                <td class="desc-cell">
                    <div class="item-name">${line.partName || 'N/A'}</div>
                    <div class="item-desc">${line.unitName ? `Unit: ${line.unitName}` : '-'}${line.unitSymbol ? ` (${line.unitSymbol})` : ''}</div>
                </td>
                <td class="num-cell">${this.currencyService.formatCurrency(line.unitPrice, currencyCode)}</td>
                <td class="num-cell">${line.quantity}</td>
                <td class="num-cell">${this.currencyService.formatCurrency(line.lineTotal || (line.quantity * line.unitPrice), currencyCode)}</td>
            </tr>
        `).join('');

        return `<!DOCTYPE html>
<html>
<head>
    <title>Purchase Order - ${po.poNumber}</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; padding: 20px; max-width: 800px; margin: 0 auto; }

        .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
        .logo-section { display: flex; align-items: center; gap: 10px; }
        .logo { width: 60px; height: 60px; background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 24px; font-weight: bold; }
        .company-name { font-size: 22px; font-weight: 700; color: #1976d2; }
        .title-section { text-align: right; }
        .title-section h1 { font-size: 28px; color: #1976d2; font-weight: 300; margin-bottom: 8px; }
        .invoice-meta { font-size: 11px; color: #666; }
        .invoice-meta span { display: inline-block; min-width: 90px; }
        .invoice-meta .value { color: #333; font-weight: 500; }

        .address-section { display: flex; justify-content: space-between; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #e0e0e0; }
        .address-block { flex: 1; }
        .address-block.right { text-align: right; }
        .address-label { font-size: 10px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }
        .address-name { font-size: 14px; font-weight: 600; color: #333; margin-bottom: 4px; }
        .address-detail { font-size: 11px; color: #666; line-height: 1.5; }

        .items-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
        .items-table th { background: #1976d2; color: white; padding: 10px 8px; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 500; }
        .items-table th:first-child { text-align: left; border-radius: 4px 0 0 0; }
        .items-table th:last-child { border-radius: 0 4px 0 0; }
        .items-table th.num-col { text-align: right; }
        .items-table td { padding: 10px 8px; border-bottom: 1px solid #eee; vertical-align: top; }
        .desc-cell { width: 45%; }
        .num-cell { text-align: right; width: 18%; }
        .item-name { font-weight: 500; color: #333; }
        .item-desc { font-size: 10px; color: #999; margin-top: 2px; }

        .summary-section { display: flex; justify-content: space-between; margin-bottom: 20px; }
        .payment-info { flex: 1; padding-right: 40px; }
        .payment-info h4 { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
        .payment-info p { font-size: 11px; color: #666; line-height: 1.6; }
        .totals-box { width: 250px; }
        .totals-row { display: flex; justify-content: space-between; padding: 6px 0; font-size: 11px; }
        .totals-row.total { border-top: 2px solid #1976d2; margin-top: 8px; padding-top: 10px; font-size: 14px; font-weight: 600; color: #1976d2; }
        .totals-label { color: #666; }
        .totals-value { font-weight: 500; }

        .footer { text-align: center; color: #999; font-size: 10px; padding-top: 10px; border-top: 1px solid #eee; }

        .no-print { margin-top: 20px; text-align: center; }
        .no-print button { padding: 10px 30px; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; margin: 0 5px; }
        .btn-print { background: #1976d2; color: white; }
        .btn-close { background: #666; color: white; }
        @media print { body { padding: 10px; } .no-print { display: none; } }
    </style>
</head>
<body>
    <div class="header">
        <div class="logo-section">
            <div class="logo">SM</div>
            <div>
                <div class="company-name">Sujan Motors</div>
                <div class="address-detail">Auto Parts & Accessories</div>
                <div class="address-detail">Dhaka, Bangladesh</div>
            </div>
        </div>
        <div class="title-section">
            <h1>Purchase Order</h1>
            <div class="invoice-meta">
                <div><span>PO no.:</span> <span class="value">${po.poNumber}</span></div>
                <div><span>Order date:</span> <span class="value">${orderDate}</span></div>
                <div><span>Delivery:</span> <span class="value">${deliveryDate}</span></div>
                <div><span>Status:</span> <span class="value">${po.status}</span></div>
                <div><span>Terms:</span> <span class="value">${po.paymentTerms || '-'}</span></div>
            </div>
        </div>
    </div>

    <div class="address-section">
        <div class="address-block">
            <div class="address-label">From</div>
            <div class="address-name">Sujan Motors</div>
            <div class="address-detail">
                Auto Parts & Accessories<br>
                Dhaka, Bangladesh
            </div>
        </div>
        <div class="address-block right">
            <div class="address-label">Supplier</div>
            <div class="address-name">${po.supplierName || 'N/A'}</div>
            <div class="address-detail">
                ${po.supplierCode ? `Supplier Code: ${po.supplierCode}<br>` : ''}
                PO Ref: ${po.poNumber}
            </div>
        </div>
    </div>

    <table class="items-table">
        <thead>
            <tr>
                <th>Description</th>
                <th class="num-col">Unit Price</th>
                <th class="num-col">Qty</th>
                <th class="num-col">Amount</th>
            </tr>
        </thead>
        <tbody>
            ${lineItemsHtml || `<tr><td colspan="4" style="padding: 10px; text-align: center;">No items</td></tr>`}
        </tbody>
    </table>

    <div class="summary-section">
        <div class="payment-info">
            ${po.notes ? `<h4>Notes</h4><p>${po.notes}</p>` : ''}
        </div>
        <div class="totals-box">
            <div class="totals-row">
                <span class="totals-label">Subtotal:</span>
                <span class="totals-value">${this.currencyService.formatCurrency(po.subTotal || 0, currencyCode)}</span>
            </div>
            <div class="totals-row">
                <span class="totals-label">Tax (${po.taxPercentage || 0}%):</span>
                <span class="totals-value">${this.currencyService.formatCurrency(po.taxAmount || 0, currencyCode)}</span>
            </div>
            <div class="totals-row">
                <span class="totals-label">Discount (${po.discountPercentage || 0}%):</span>
                <span class="totals-value">-${this.currencyService.formatCurrency(po.discount || 0, currencyCode)}</span>
            </div>
            <div class="totals-row total">
                <span class="totals-label">Total:</span>
                <span class="totals-value">${this.currencyService.formatCurrency(po.grandTotal || 0, currencyCode)}</span>
            </div>
        </div>
    </div>

    <div class="footer">
        <p>Thank you for choosing Sujan Motors | For inquiries, please contact us</p>
    </div>

    <div class="no-print">
        <button class="btn-print" onclick="window.print()">Print</button>
        <button class="btn-close" onclick="window.close()">Close</button>
    </div>
</body>
</html>`;
    }
}
