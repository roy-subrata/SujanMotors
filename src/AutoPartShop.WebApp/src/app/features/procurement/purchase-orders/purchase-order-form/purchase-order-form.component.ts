import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, ReactiveFormsModule, FormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { CardModule } from 'primeng/card';
import { DividerModule } from 'primeng/divider';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { MessageService, ConfirmationService } from 'primeng/api';
import { PurchaseOrderService, PurchaseOrderResponse } from '../../services/purchase-order.service';
import { SupplierService, SupplierResponse, SupplierQuery } from '../../../inventory/services/supplier.service';
import { PartService, PartResponse, PartsQuery } from '../../../inventory/services/part.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CurrencySelectorComponent } from '../../../../shared/components/currency-selector/currency-selector.component';
import { DatePicker } from 'primeng/datepicker';
import { TextareaModule } from 'primeng/textarea';
import { tap } from 'rxjs';

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
        AutoCompleteModule,
        SelectModule,
        CardModule,
        DividerModule,
        ToastModule,
        CurrencySelectorComponent,
        DatePicker,
        ConfirmDialogModule,
        TooltipModule,
        TextareaModule
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

    // Autocomplete data
    filteredSuppliers: SupplierResponse[] = [];
    filteredPaymentTerms: any[] = [];
    filteredPriorities: any[] = [];
    filteredParts: PartResponse[] = [];
    units: UnitResponse[] = [];
    compatibleUnitsMap = new Map<string, UnitResponse[]>();
    lineUnitsMap = new Map<number, UnitResponse[]>();
    loadingUnitsForLine = new Set<number>();

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
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly fb = inject(FormBuilder);
    private readonly route = inject(ActivatedRoute);
    private readonly router = inject(Router);

    constructor() {
        this.form = this.createForm();
    }

    ngOnInit(): void {
        this.filteredPaymentTerms = this.paymentTermsOptions;
        this.filteredPriorities = this.priorityOptions;

        this.route.queryParams.pipe(
            tap({
                next: (params) => {
                    const currentPath = this.route.snapshot.routeConfig?.path;
                    const poId = params["id"];
                    if (poId) {
                        this.poId = poId;
                        if (currentPath === 'view') {
                            this.isViewing = true;
                            this.mode = 'view';
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

    private loadParts(query: PartsQuery): void {
        this.partService.getParts(query)
            .pipe(
                tap({
                    next: (response) => {
                        this.filteredParts = response.data;
                    },
                    error: (error) => {
                        console.error('Error loading parts:', error);
                    }
                })
            )
            .subscribe();
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
                    currency: 'BDT',
                    priority: 'MEDIUM',
                    notes: po.notes,
                    taxRate: po.taxPercentage || 0,
                    discountPercentage: po.discountPercentage || 0
                });

                const linesArray = this.linesArray;
                linesArray.clear();
                this.lineUnitsMap.clear();

                po.lines?.forEach((line, index) => {
                    const matchingPart = { id: line.partId, name: line.partName, unitId: line.unitId };
                    const matchingUnit = { id: line.unitId, name: line.unitName, symbol: line.unitSymbol } as UnitResponse;

                    linesArray.push(this.fb.group({
                        partId: [matchingPart || line.partId, Validators.required],
                        unitId: [matchingUnit],
                        quantity: [line.quantity, [Validators.required, Validators.min(1)]],
                        unitPrice: [line.unitPrice, [Validators.required, Validators.min(0)]]
                    }));

                    // Set current unit immediately so dropdown has options
                    this.lineUnitsMap.set(index, [matchingUnit]);

                    // Load compatible units for each line
                    if (line.unitId) {
                        this.unitService.getCompatibleUnits(line.unitId).subscribe({
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
        const defaultCurrency = this.currencyService.selectedCurrency() || 'BDT';

        return this.fb.group({
            supplier: ['', Validators.required],
            deliveryDate: ['', Validators.required],
            paymentTerms: ['NET30', Validators.required],
            currency: [defaultCurrency, Validators.required],
            priority: ['MEDIUM', Validators.required],
            taxRate: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
            discountPercentage: [0, [Validators.required, Validators.min(0), Validators.max(100)]],
            notes: [''],
            lines: this.fb.array([])
        });
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
        return (subtotal * discountPercentage) / 100;
    }

    getGrandTotal(): number {
        const subtotal = this.getSubtotal();
        const tax = this.getTaxAmount();
        const discount = this.getDiscountAmount();
        return subtotal + tax - discount;
    }

    formatCurrency(value: number): string {
        const currencyCode = this.form.get('currency')?.value || 'BDT';
        return this.currencyService.formatCurrency(value, currencyCode);
    }

    onSupplierFilter(event: any): void {
        const search = event.query.toLowerCase();
        this.loadSuppliers({ search, pageNumber: 1, pageSize: 100 });
    }

    private loadSuppliers(query: SupplierQuery) {
        this.supplierService.getSuppliers(query)
            .pipe(
                tap({
                    next: (response) => {
                        this.filteredSuppliers = response.data;
                    },
                    error: () => {
                        console.log("error loading suppliers!");
                    }
                })
            ).subscribe();
    }

    onPartFilter(event: any): void {
        const search = event.query.toLowerCase();
        this.loadParts({
            search,
            pageNumber: 1,
            pageSize: 100,
            isActive: true
        });
    }

    onPaymentTermsFilter(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredPaymentTerms = this.paymentTermsOptions.filter(term =>
            term.label.toLowerCase().includes(query) || term.value.toLowerCase().includes(query)
        );
    }

    onPriorityFilter(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredPriorities = this.priorityOptions.filter(priority =>
            priority.label.toLowerCase().includes(query) || priority.value.toLowerCase().includes(query)
        );
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

    submitPurchaseOrder(): void {
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
}
