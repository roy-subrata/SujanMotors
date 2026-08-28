import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { DialogModule } from 'primeng/dialog';
import { DividerModule } from 'primeng/divider';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesOrderService, CreateSalesOrderRequest, SalesOrderResponse } from '../../services/sales-order.service';
import { ChallanService, GenerateChallanRequest } from '../../services/challan.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { CustomerVehicleService, CustomerVehicleResponse } from '../../services/customer-vehicle.service';
import { PublicPartService, PublicPartResponse } from '../../services/public-part.service';
import { TechnicianService, TechnicianResponse } from '../../services/technician.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CurrencySelectorComponent } from '../../../../shared/components/currency-selector/currency-selector.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../../shared/components/lazy-autocomplete';
import { PricingValidationService, PricingCalculationResponse } from '../../../../shared/services/pricing-validation.service';
import { Subject, takeUntil, map, forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { UnitConversionService } from '../../../inventory/services/unit-conversion.service';
import { WarehouseService, WarehouseResponse } from '../../../inventory/services/warehouse.service';
import { StockLotService } from '../../../inventory/services/stock-lot.service';
import { ApplyCustomerCreditNotesComponent } from '../../credits/apply-customer-credit-notes.component';
import { CustomerCreditNoteService } from '../../services/customer-credit-note.service';
import { ProformaInvoiceService } from '../../services/proforma-invoice.service';
import { StatusDisplayService } from '@/shared/services/status-display.service';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

@Component({
    selector: 'app-sales-order-form',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        AutoCompleteModule,
        SelectModule,
        CurrencySelectorComponent,
        TagModule,
        ToastModule,
        ConfirmDialogModule,
        DialogModule,
        DividerModule,
        CardModule,
        ButtonModule,
        InputTextModule,
        TextareaModule,
        TooltipModule,
        DatePickerModule,
        LazyAutocompleteComponent,
        ApplyCustomerCreditNotesComponent,
        TranslatePipe
    ],
    providers: [MessageService, ConfirmationService],
    templateUrl: './sales-order-form.component.html',
    styleUrls: ['./sales-order-form.component.css']
})
export class SalesOrderFormComponent implements OnInit, OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly salesOrderService = inject(SalesOrderService);
    private readonly challanService     = inject(ChallanService);
    private readonly customerService = inject(CustomerService);
    private readonly vehicleService = inject(CustomerVehicleService);
    private readonly partService = inject(PublicPartService);
    private readonly technicianService = inject(TechnicianService);
    private readonly unitService = inject(UnitService);
    private readonly currencyService = inject(CurrencyService);
    private readonly creditNoteService = inject(CustomerCreditNoteService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly pricingValidationService = inject(PricingValidationService);
    private readonly unitConversionService = inject(UnitConversionService);
    private readonly warehouseService = inject(WarehouseService);
    private readonly stockLotService = inject(StockLotService);
    private readonly proformaInvoiceService = inject(ProformaInvoiceService);
    private readonly statusDisplay = inject(StatusDisplayService);
    private readonly i18n = inject(I18nService);

    // Credit note state
    totalCreditApplied = 0;
    availableCreditForCustomer = 0;

    // Subscription management
    private readonly destroy$ = new Subject<void>();

    selectedCustomer: CustomerResponse | null = null;
    selectedTechnecian: TechnicianResponse | null = null;
    selectedPartQuickAdd: PublicPartResponse | null = null;

    linePricingErrors = new Map<number, string>();
    linePricingInfo = new Map<number, PricingCalculationResponse>();
    private readonly linePricingInfoTimers = new Map<number, ReturnType<typeof setTimeout>>();

    searchCustomers = (req: LazyRequest) => {
        return this.customerService
            .getCustomers({
                search: req.search,
                pageNumber: req.pageNumber,
                pageSize: req.pageSize
            })
            .pipe(
                map(
                    (response) =>
                        ({
                            items: response.data,
                            totalCount: response.pagination.totalCount
                        }) as LazyResponse<CustomerResponse>
                )
            );
    };

    searchTechnecian = (req: LazyRequest) => {
        return this.technicianService
            .getTechnicians({
                search: req.search,
                pageNumber: req.pageNumber,
                pageSize: req.pageSize
            })
            .pipe(
                map(
                    (response) =>
                        ({
                            items: response.data,
                            totalCount: response.pagination.totalCount
                        }) as LazyResponse<TechnicianResponse>
                )
            );
    };

    searchParts = (req: LazyRequest) => {
        return this.partService
            .getParts({
                search: req.search,
                pageNumber: req.pageNumber,
                pageSize: req.pageSize,
                isActive: true,
                flattenVariants: true
            })
            .pipe(
                map(
                    (response) =>
                        ({
                            items: response.data,
                            totalCount: response.pagination.totalCount
                        }) as LazyResponse<PublicPartResponse>
                )
            );
    };

    // Handle customer selection from lazy autocomplete
    onCustomerSelected(customer: CustomerResponse): void {
        this.selectedCustomer = customer;
        this.selectedCustomerId = customer.id;
        this.salesOrderForm.patchValue({
            customerName: `${customer.firstName} ${customer.lastName}`,
            customerEmail: customer.email,
            customerPhone: customer.phone,
            customerCity: customer.city
        });
        this.loadCustomerVehicles(customer.id);
    }

    // Handle customer clear from lazy autocomplete
    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.selectedCustomerId = '';
        this.clearCustomerSelection();
        this.customerVehicles.set([]);
        this.selectedVehicleId = null;
        this.salesOrderForm.patchValue({ customerVehicleId: null });
    }

    // Load the selected customer's vehicles for the optional vehicle picker
    loadCustomerVehicles(customerId: string): void {
        this.loadingVehicles.set(true);
        this.vehicleService.getByCustomer(customerId, true)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (vehicles) => {
                    this.customerVehicles.set(vehicles);
                    this.loadingVehicles.set(false);
                },
                error: () => {
                    this.customerVehicles.set([]);
                    this.loadingVehicles.set(false);
                }
            });
    }

    onVehicleChange(vehicleId: string | null): void {
        this.selectedVehicleId = vehicleId;
        this.salesOrderForm.patchValue({ customerVehicleId: vehicleId });
    }

    get selectedVehicleLabel(): string {
        if (!this.selectedVehicleId) return '';
        return this.customerVehicles().find(v => v.id === this.selectedVehicleId)?.label ?? '';
    }
    // Handle technician selection from lazy autocomplete
    onTechnicianSelected(technician: TechnicianResponse): void {
        this.selectedTechnecian = technician;
        this.selectedTechnicianId = technician.id;
        this.salesOrderForm.patchValue({
            technicianId: technician.id,
            technicianName: technician.name
        });
    }

    // Handle technician clear from lazy autocomplete
    onTechnicianCleared(): void {
        this.selectedTechnecian = null;
        this.selectedTechnicianId = '';
        this.salesOrderForm.patchValue({
            technicianId: null,
            technicianName: null
        });
    }

    salesOrderForm!: FormGroup;
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    mode = signal<'create' | 'edit' | 'view'>('create');
    salesOrderId = signal<string | null>(null);
    currentSO: SalesOrderResponse | null = null;

    // Customer selection
    selectedCustomerId = '';

    // Technician selection
    selectedTechnicianId = '';

    // Customer vehicle selection (optional — the car this purchase is for)
    customerVehicles = signal<CustomerVehicleResponse[]>([]);
    loadingVehicles = signal(false);
    selectedVehicleId: string | null = null;

    // Units
    units = signal<UnitResponse[]>([]);
    loadingUnits = signal(false);

    // Warehouses
    warehouses = signal<WarehouseResponse[]>([]);
    loadingWarehouses = signal(false);
    // Map to store compatible units for each part (keyed by part ID)
    compatibleUnitsMap = new Map<string, UnitResponse[]>();

    // Calculate subtotal from line items
    subTotal(): number {
        if (!this.salesOrderForm) return 0;
        const lines = this.lines.controls;
        const total = lines.reduce((sum, line) => {
            const qty = this.parseNumber(line.get('quantity')?.value);
            const price = this.parseNumber(line.get('unitPrice')?.value);
            const discount = this.parseNumber(line.get('discount')?.value);
            return sum + qty * price * (1 - discount / 100);
        }, 0);
        return isNaN(total) ? 0 : total;
    }

    // Order-level discount (percentage)
    orderDiscount(): number {
        if (!this.salesOrderForm) return 0;
        return this.parseNumber(this.salesOrderForm.get('orderDiscount')?.value);
    }

    orderDiscountAmount(): number {
        return (this.subTotal() * this.orderDiscount()) / 100;
    }

    // Calculate grand total
    grandTotal(): number {
        const total = this.subTotal() - this.orderDiscountAmount();
        return total < 0 ? 0 : total;
    }

    // Safely parse number values
    private parseNumber(value: unknown): number {
        if (value === null || value === undefined || value === '') return 0;
        const num = typeof value === 'number' ? value : parseFloat(String(value));
        return isNaN(num) ? 0 : num;
    }

    ngOnInit(): void {
        this.initializeForm();
        this.loadUnits();
        this.loadWarehouses();

        // Check route params
        this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((params) => {
            const id = params['id'];
            const mode = params['mode'];

            if (id) {
                this.salesOrderId.set(id);
                this.mode.set(mode === 'view' ? 'view' : 'edit');
                this.loadSalesOrder(id);
            }
        });

        if (this.mode() === 'view') {
            this.salesOrderForm.disable();
        }
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        // Clear state objects
        this.compatibleUnitsMap.clear();
        this.linePricingInfoTimers.forEach((timerId) => clearTimeout(timerId));
        this.linePricingInfoTimers.clear();
    }

    loadUnits(): void {
        this.loadingUnits.set(true);
        this.unitService
            .getActiveUnits()
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (units) => {
                    this.units.set(units);
                    this.loadingUnits.set(false);
                },
                error: (err: Error) => {
                    console.error('Error loading units:', err);
                    this.loadingUnits.set(false);
                }
            });
    }

    loadWarehouses(): void {
        this.loadingWarehouses.set(true);
        this.warehouseService
            .getWarehouses({ search: '', pageNumber: 1, pageSize: 1000, sorts: [{ field: 'name', direction: 'asc' }] })
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (res) => {
                    this.warehouses.set(res.data ?? []);
                    this.loadingWarehouses.set(false);
                },
                error: (err: Error) => {
                    console.error('Error loading warehouses:', err);
                    this.loadingWarehouses.set(false);
                }
            });
    }

    clearCustomerSelection(): void {
        this.selectedCustomerId = '';
        this.salesOrderForm.patchValue({
            customerName: '',
            customerEmail: '',
            customerPhone: '',
            customerCity: ''
        });
    }

    // Handle part selection from lazy autocomplete
    onPartSelected(part: PublicPartResponse, lineIndex: number): void {
        if (!part?.id) return;

        // Merge duplicates: same part + same variant → increase qty; different variant = separate line
        const existingIndex = this.lines.controls.findIndex(
            (line, idx) => idx !== lineIndex
                && line.get('part')?.value?.id === part.id
                && (line.get('variantId')?.value ?? null) === (part.variantId ?? null)
        );
        if (existingIndex >= 0) {
            const existingLine = this.lines.at(existingIndex);
            const currentLine = this.lines.at(lineIndex);
            const qtyToAdd = this.parseNumber(currentLine.get('quantity')?.value) || 1;
            const existingQty = this.parseNumber(existingLine.get('quantity')?.value) || 0;
            existingLine.patchValue({ quantity: existingQty + qtyToAdd });
            this.lines.removeAt(lineIndex);
            return;
        }

        const line = this.lines.at(lineIndex) as FormGroup;
        // Store variantId on the line
        line.patchValue({ variantId: part.variantId ?? null });

        const effectivePrice = part.effectiveSellingPrice ?? part.sellingPrice;
        const warehouseId = this.salesOrderForm?.get('warehouseId')?.value as string | null;
        if (warehouseId) {
            this.stockLotService.getFifoLotInfo(part.id, warehouseId)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: (lotInfo) => {
                        const price = lotInfo.hasAvailableLot && lotInfo.sellingPrice > 0
                            ? lotInfo.sellingPrice
                            : effectivePrice;
                        line.patchValue({ unitPrice: price });
                        this.clearLinePricingError(lineIndex);
                        this.scheduleLinePricingInfoRefresh(lineIndex);
                    },
                    error: () => {
                        line.patchValue({ unitPrice: effectivePrice });
                        this.clearLinePricingError(lineIndex);
                        this.scheduleLinePricingInfoRefresh(lineIndex);
                    }
                });
        } else {
            line.patchValue({ unitPrice: effectivePrice });
            this.clearLinePricingError(lineIndex);
            this.scheduleLinePricingInfoRefresh(lineIndex);
        }

        // Load compatible units for the selected part
        this.ensureCompatibleUnitsForLine(part, line, true);
    }

    // Quick-add part selection (single search above line items)
    onQuickAddPartSelected(part: PublicPartResponse): void {
        if (!part?.id) return;

        const existingIndex = this.lines.controls.findIndex(
            (line) => line.get('part')?.value?.id === part.id
                && (line.get('variantId')?.value ?? null) === (part.variantId ?? null)
        );
        if (existingIndex >= 0) {
            const line = this.lines.at(existingIndex) as FormGroup;
            const currentQty = this.parseNumber(line.get('quantity')?.value) || 0;
            line.patchValue({ quantity: currentQty + 1 });
            this.ensureCompatibleUnitsForLine(part, line, true);
        } else {
            const newIndex = this.lines.length;
            this.lines.push(
                this.createLine({
                    part: part,
                    variantId: part.variantId ?? null,
                    unitId: part.unitId || null,
                    quantity: 1,
                    unitPrice: part.effectiveSellingPrice ?? part.sellingPrice ?? 0,
                    discount: 0
                })
            );
            this.onPartSelected(part, newIndex);
        }

        // Clear quick add input for next search
        this.selectedPartQuickAdd = null;
    }

    onQuickAddPartCleared(): void {
        this.selectedPartQuickAdd = null;
    }

    // Handle part clear from lazy autocomplete
    onPartCleared(lineIndex: number): void {
        // Clear related fields (part is already cleared by formControlName)
        const line = this.lines.at(lineIndex);
        line.patchValue({
            unitPrice: 0,
            unitId: null
        });
        this.clearLinePricingError(lineIndex);
        this.linePricingInfo.delete(lineIndex);
    }

    /**
     * Get compatible units for a specific part
     */
    getCompatibleUnitsForPart(partId: string | null): UnitResponse[] {
        if (!partId) return this.units();
        return this.compatibleUnitsMap.get(partId) || this.units();
    }

    initializeForm(): void {
        // Get default currency from service
        const defaultCurrency = this.currencyService.selectedCurrency();

        this.salesOrderForm = this.fb.group({
            customerName: ['', [Validators.required, Validators.minLength(2)]],
            customerEmail: ['', [Validators.required, Validators.email]],
            customerPhone: ['', [Validators.required]],
            customerCity: ['', [Validators.required]],
            warehouseId: [null, [Validators.required]],
            technicianId: [null],
            technicianName: [null],
            customerVehicleId: [null],
            deliveryDate: [null, [Validators.required]],
            currency: [defaultCurrency, [Validators.required]],
            orderDiscount: [0, [Validators.min(0), Validators.max(100)]],
            notes: [''],
            lines: this.fb.array([])
        });

        // Start with no lines; user can add via Quick Add or Add Line button
    }

    get lines(): FormArray {
        return this.salesOrderForm.get('lines') as FormArray;
    }

    createLine(data?: any): FormGroup {
        const lineGroup = this.fb.group({
            part: [data?.part || null, [Validators.required]],
            variantId: [data?.variantId || null],
            unitId: [data?.unitId || null],
            quantity: [data?.quantity || 1, [Validators.required, Validators.min(1)]],
            unitPrice: [data?.unitPrice || 0, [Validators.required, Validators.min(0)]],
            discount: [data?.discount || 0, [Validators.min(0), Validators.max(100)]]
        });
        this.watchLineUnitChanges(lineGroup);
        return lineGroup;
    }

    addLine(): void {
        this.lines.push(this.createLine());
    }

    removeLine(index: number): void {
        if (this.lines.length > 1) {
            this.lines.removeAt(index);
            // Cancel the removed line's debounce timer
            const removedTimer = this.linePricingInfoTimers.get(index);
            if (removedTimer) clearTimeout(removedTimer);
            // Re-index all three maps so remaining indices stay accurate
            const reindex = <T>(src: Map<number, T>): Map<number, T> => {
                const out = new Map<number, T>();
                src.forEach((v, k) => { if (k < index) out.set(k, v); else if (k > index) out.set(k - 1, v); });
                return out;
            };
            this.linePricingErrors = reindex(this.linePricingErrors);
            this.linePricingInfo = reindex(this.linePricingInfo);
            const reindexedTimers = reindex(this.linePricingInfoTimers);
            this.linePricingInfoTimers.clear();
            reindexedTimers.forEach((v, k) => this.linePricingInfoTimers.set(k, v));
        }
    }

    getLineTotal(index: number): number {
        const line = this.lines.at(index);
        if (!line) return 0;
        const qty = this.parseNumber(line.get('quantity')?.value);
        const price = this.parseNumber(line.get('unitPrice')?.value);
        const discount = this.parseNumber(line.get('discount')?.value);
        const total = qty * price * (1 - discount / 100);
        return isNaN(total) ? 0 : total;
    }

    loadSalesOrder(id: string): void {
        this.loading.set(true);
        this.error.set(null);

        this.salesOrderService
            .getSalesOrderById(id)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (order) => {
                    this.currentSO = order;
                    this.selectedCustomerId = order.customerId;

                    // Load the customer's vehicles and preselect the linked one (if any)
                    if (order.customerId) {
                        this.loadCustomerVehicles(order.customerId);
                    }
                    this.selectedVehicleId = order.customerVehicleId ?? null;

                    // Set selectedCustomer object for lazy autocomplete display
                    this.selectedCustomer = {
                        id: order.customerId,
                        firstName: order.customerName?.split(' ')[0] || '',
                        lastName: order.customerName?.split(' ').slice(1).join(' ') || '',
                        fullName: order.customerName || '',
                        email: order.customerEmail || '',
                        phone: order.customerPhone || '',
                        city: order.customerCity || ''
                    } as CustomerResponse;

                    if (order.technicianId) {
                        this.selectedTechnicianId = order.technicianId;
                        // Set selectedTechnecian object for lazy autocomplete display
                        this.selectedTechnecian = {
                            id: order.technicianId,
                            name: order.technicianName || '',
                            technicianCode: '',
                            phone: '',
                            shopName: ''
                        } as TechnicianResponse;
                    }

                    // Parse delivery date avoiding timezone issues
                    let deliveryDate: Date | null = null;
                    if (order.deliveryDate) {
                        const parts = order.deliveryDate.split('T')[0].split('-');
                        deliveryDate = new Date(+parts[0], +parts[1] - 1, +parts[2]);
                    }

                    this.salesOrderForm.patchValue({
                        customerName: order.customerName,
                        customerEmail: order.customerEmail,
                        customerPhone: order.customerPhone,
                        customerCity: order.customerCity,
                        warehouseId: order.warehouseId || null,
                        customerVehicleId: order.customerVehicleId ?? null,
                        deliveryDate: deliveryDate,
                        currency: order.currency || this.currencyService.selectedCurrency(),
                        orderDiscount: order.discount || 0,
                        notes: order.notes
                    });

                    // Clear and add lines
                    this.lines.clear();
                    this.linePricingErrors.clear();
                    order.lines.forEach((line) => {
                        // Create a minimal part object for the form control.
                        // displayName is REQUIRED: the per-line lazy-autocomplete uses
                        // optionLabel="displayName" and PrimeNG renders [object Object]
                        // when that key is missing on the prefilled value.
                        const partObj = {
                            id: line.partId,
                            name: line.partName || '',
                            displayName: line.displayName || line.partName || '',
                            localName: line.partLocalName || null,
                            partNumber: line.partSku || '',
                            sku: line.partSku || '',
                            unitName: line.unitName || ''
                        } as PublicPartResponse;

                        this.lines.push(
                            this.createLine({
                                part: partObj,
                                unitId: line.unitId,
                                quantity: line.quantity,
                                unitPrice: line.unitPrice,
                                discount: line.discount
                            })
                        );
                    });

                    this.hydrateLinePartDetails(order.lines);
                    this.loading.set(false);
                },
                error: (err: Error) => {
                    this.error.set(this.i18n.t('salesOrders.form.messages.loadFailed'));
                    this.loading.set(false);
                    console.error('Error loading sales order:', err);
                }
            });
    }

    onSubmit(): void {
        // Validate customer selection
        if (!this.selectedCustomerId) {
            this.error.set(this.i18n.t('salesOrders.form.messages.selectCustomerFirst'));
            return;
        }

        // Validate order-level discount
        if (this.orderDiscount() > 100) {
            this.error.set(this.i18n.t('salesOrders.form.messages.discountTooHigh'));
            return;
        }

        // Validate warehouse selection
        const warehouseId = this.salesOrderForm.get('warehouseId')?.value;
        if (!warehouseId) {
            this.error.set(this.i18n.t('salesOrders.form.messages.selectWarehouse'));
            return;
        }

        // Validate form
        if (this.salesOrderForm.invalid) {
            Object.keys(this.salesOrderForm.controls).forEach((key) => {
                const control = this.salesOrderForm.get(key);
                if (control?.invalid) {
                    control.markAsTouched();
                }
            });
            this.lines.controls.forEach((line) => {
                Object.keys(line.value).forEach((key) => {
                    const control = line.get(key);
                    if (control?.invalid) {
                        control.markAsTouched();
                    }
                });
            });
            this.error.set(this.i18n.t('salesOrders.form.messages.fillRequired'));
            return;
        }

        // Validate all parts are selected (form validation handles required, but double-check)
        const invalidLines: number[] = [];
        this.lines.controls.forEach((line, index) => {
            const part = line.get('part')?.value;
            if (!part) {
                invalidLines.push(index + 1);
            }
        });

        if (invalidLines.length > 0) {
            this.error.set(`Please select parts for line item(s): ${invalidLines.join(', ')}`);
            return;
        }

        this.validatePricingBeforeSubmit().pipe(takeUntil(this.destroy$)).subscribe({
            next: (isValid) => {
                if (!isValid) {
                    this.error.set(this.i18n.t('salesOrders.form.messages.pricingViolation'));
                    return;
                }
                this.submitSalesOrder();
            },
            error: () => {
                this.error.set(this.i18n.t('salesOrders.form.messages.pricingValidationFailed'));
            }
        });
    }

    private submitSalesOrder(): void {
        this.saving.set(true);
        this.error.set(null);

        const formValue = this.salesOrderForm.value;
        // Convert Date object to YYYY-MM-DD string (avoiding timezone issues)
        let deliveryDate = '';
        if (formValue.deliveryDate instanceof Date) {
            const d = formValue.deliveryDate;
            deliveryDate = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        } else if (formValue.deliveryDate) {
            deliveryDate = formValue.deliveryDate;
        }

        const request: CreateSalesOrderRequest = {
            customerId: this.selectedCustomerId,
            warehouseId: formValue.warehouseId,
            customerName: formValue.customerName,
            customerEmail: formValue.customerEmail,
            customerPhone: formValue.customerPhone,
            customerCity: formValue.customerCity,
            technicianId: this.selectedTechnicianId || undefined,
            technicianName: formValue.technicianName || undefined,
            customerVehicleId: this.selectedVehicleId || null,
            deliveryDate: deliveryDate,
            notes: formValue.notes,
            currency: formValue.currency,
            discount: this.orderDiscount(),
            lines: formValue.lines.map((line: any) => ({
                partId: line.part?.id,
                productVariantId: line.variantId ?? line.part?.variantId ?? null,
                unitId: line.unitId,
                quantity: line.quantity,
                unitPrice: line.unitPrice,
                discount: line.discount || 0
            }))
        };

        const operation = this.mode() === 'edit' && this.salesOrderId()
            ? this.salesOrderService.updateSalesOrder(this.salesOrderId()!, request)
            : this.salesOrderService.createSalesOrder(request);

        operation.pipe(takeUntil(this.destroy$)).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: this.i18n.t('common.messages.success'),
                    detail: this.i18n.t(this.mode() === 'edit' ? 'salesOrders.form.messages.updateSuccess' : 'salesOrders.form.messages.createSuccess')
                });
                this.router.navigate(['/sales/sales-orders']);
            },
            error: (err) => {
                let errorMessage = this.i18n.t(this.mode() === 'edit' ? 'salesOrders.form.messages.updateFailed' : 'salesOrders.form.messages.createFailed');

                if (err.error?.message) {
                    errorMessage = err.error.message;
                } else if (err.error?.errors) {
                    const errors = Object.values(err.error.errors).flat();
                    errorMessage = errors.join(', ');
                } else if (err.message) {
                    errorMessage = err.message;
                }

                this.error.set(errorMessage);
                this.saving.set(false);
                console.error(`Error ${this.mode() === 'edit' ? 'updating' : 'creating'} sales order:`, err);
            }
        });
    }

    getLinePricingError(index: number): string | null {
        return this.linePricingErrors.get(index) || null;
    }

    clearLinePricingError(index: number): void {
        this.linePricingErrors.delete(index);
    }

    getLinePricingInfo(index: number): PricingCalculationResponse | null {
        return this.linePricingInfo.get(index) || null;
    }

    getMaxDiscountedPriceForPart(part: PublicPartResponse | null): number {
        if (!part) return 0;
        return this.parseNumber(part.sellingPrice);
    }

    getEffectivePrice(index: number): number {
        const line = this.lines.at(index);
        if (!line) return 0;
        const unitPrice = this.parseNumber(line.get('unitPrice')?.value);
        const discount = this.parseNumber(line.get('discount')?.value);
        const effective = unitPrice - (unitPrice * (discount / 100));
        return effective < 0 ? 0 : effective;
    }

    scheduleLinePricingInfoRefresh(index: number): void {
        const existing = this.linePricingInfoTimers.get(index);
        if (existing) clearTimeout(existing);
        const timerId = setTimeout(() => this.refreshLinePricingInfo(index), 250);
        this.linePricingInfoTimers.set(index, timerId);
    }

    private watchLineUnitChanges(line: FormGroup): void {
        let previousUnitId = line.get('unitId')?.value || null;
        line.get('unitId')?.valueChanges.pipe(takeUntil(this.destroy$)).subscribe((nextUnitId) => {
            const part = line.get('part')?.value as PublicPartResponse | null;
            if (!part?.id || !part.unitId) {
                previousUnitId = nextUnitId;
                return;
            }

            this.updateLineUnitPrice(line, part, previousUnitId, nextUnitId);
            previousUnitId = nextUnitId;
        });
    }

    private updateLineUnitPrice(line: FormGroup, part: PublicPartResponse, previousUnitId: string | null, nextUnitId: string | null): void {
        const baseUnitId = part.unitId;
        if (!baseUnitId) return;

        const fromUnitId = previousUnitId || baseUnitId;
        const toUnitId = nextUnitId || baseUnitId;
        if (fromUnitId === toUnitId) return;

        const currentPrice = this.parseNumber(line.get('unitPrice')?.value);
        const fromFactor$ = fromUnitId === baseUnitId
            ? of(1)
            : this.unitConversionService.getConversion(fromUnitId, baseUnitId).pipe(map((res) => res.conversionFactor));
        const toFactor$ = toUnitId === baseUnitId
            ? of(1)
            : this.unitConversionService.getConversion(toUnitId, baseUnitId).pipe(map((res) => res.conversionFactor));

            forkJoin({ fromFactor: fromFactor$, toFactor: toFactor$ })
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: ({ fromFactor, toFactor }) => {
                    const basePrice = fromFactor > 0 ? currentPrice / fromFactor : currentPrice;
                    const newPrice = basePrice * toFactor;
                    line.patchValue({ unitPrice: this.roundPrice(newPrice) }, { emitEvent: false });
                    const lineIndex = this.lines.controls.indexOf(line);
                    if (lineIndex >= 0) {
                        this.scheduleLinePricingInfoRefresh(lineIndex);
                    }
                },
                error: (err) => {
                    console.error('Error converting unit price:', err);
                    this.messageService.add({
                        severity: 'warn',
                        summary: this.i18n.t('salesOrders.form.messages.unitConversionMissing'),
                        detail: this.i18n.t('salesOrders.form.messages.unitConversionMissingDetail')
                    });
                }
            });
    }

    private roundPrice(value: number): number {
        return Math.round(value * 100) / 100;
    }

    private refreshLinePricingInfo(index: number): void {
        const line = this.lines.at(index);
        if (!line) return;
        const part = line.get('part')?.value as PublicPartResponse | null;
        if (!part?.id) {
            this.linePricingInfo.delete(index);
            return;
        }

        const unitPrice = this.parseNumber(line.get('unitPrice')?.value);
        const discount = this.parseNumber(line.get('discount')?.value);
        const unitId = line.get('unitId')?.value || null;

        this.pricingValidationService
            .calculateLine(part.id, unitPrice, discount, unitId)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: (result) => {
                    this.linePricingInfo.set(index, result);
                },
                error: () => {
                    this.linePricingInfo.delete(index);
                }
            });
    }

    private getLocalPricingError(part: PublicPartResponse, unitPrice: number, discount: number, unitId: string | null): string | null {
        if (unitPrice <= 0) return 'Selling price must be greater than 0.';
        if (discount < 0 || discount > 100) return 'Discount percentage must be between 0 and 100.';
        if (discount > 100) {
            return `Discount cannot exceed 100%.`;
        }
        return null;
    }

    private validatePricingBeforeSubmit() {
        if (this.lines.length === 0) return of(true);

        const validations = this.lines.controls.map((line, index) => {
            const part = line.get('part')?.value as PublicPartResponse | null;
            if (!part?.id) {
                this.linePricingErrors.set(index, 'Please select a part.');
                return of(false);
            }

            const unitPrice = this.parseNumber(line.get('unitPrice')?.value);
            const discount = this.parseNumber(line.get('discount')?.value);
            const unitId = line.get('unitId')?.value || null;
            const localError = this.getLocalPricingError(part, unitPrice, discount, unitId);
            if (localError) {
                this.linePricingErrors.set(index, localError);
                return of(false);
            }

            return this.pricingValidationService.validateLine(part.id, unitPrice, discount, unitId).pipe(
                map(() => {
                    this.clearLinePricingError(index);
                    return true;
                }),
                catchError((err) => {
                    const message = err?.error?.message || 'Invalid pricing for this item.';
                    this.linePricingErrors.set(index, message);
                    return of(false);
                })
            );
        });

        return forkJoin(validations).pipe(map((results) => results.every(Boolean)));
    }

    cancel(): void {
        this.router.navigate(['/sales/sales-orders']);
    }

    /**
     * Downloads the server-rendered Proforma Invoice PDF for the current sales order. Reuses an
     * existing proforma for this order if one was already generated, otherwise creates one first
     * (mirrors GenerateProformaDialogComponent.submit()).
     */
    printProformaInvoice(): void {
        if (this.salesOrderForm.invalid || !this.selectedCustomerId || !this.currentSO) {
            this.messageService.add({
                severity: 'warn',
                summary: this.i18n.t('salesOrders.form.messages.incompleteForm'),
                detail: this.i18n.t('salesOrders.form.messages.incompleteFormDetail')
            });
            return;
        }

        const salesOrderId = this.currentSO.id;
        this.proformaInvoiceService.getBySalesOrder(salesOrderId).subscribe({
            next: (proformas) => {
                const existing = proformas?.[0];
                if (existing) {
                    this.downloadProformaPdf(existing.id, existing.proformaNumber);
                } else {
                    this.proformaInvoiceService.create({ salesOrderId, validUntil: null, notes: '' }).subscribe({
                        next: (proforma) => this.downloadProformaPdf(proforma.id, proforma.proformaNumber),
                        error: (error) => this.handleProformaPdfError(error)
                    });
                }
            },
            error: (error) => this.handleProformaPdfError(error)
        });
    }

    private downloadProformaPdf(id: string, proformaNumber: string): void {
        this.proformaInvoiceService.downloadPdf(id, proformaNumber).subscribe({
            error: (error) => this.handleProformaPdfError(error)
        });
    }

    private handleProformaPdfError(error: any): void {
        this.messageService.add({
            severity: 'error',
            summary: this.i18n.t('salesOrders.form.messages.printFailed'),
            detail: error?.error?.message || this.i18n.t('salesOrders.form.messages.printFailedDetail')
        });
        console.error('Error downloading proforma invoice PDF:', error);
    }

    formatCurrency(amount: number | null | undefined): string {
        if (amount == null || isNaN(amount)) return '—';
        const currencyCode = this.salesOrderForm?.get('currency')?.value || this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currencyCode);
    }

    /**
     * Confirm sales order
     */
    confirmSalesOrder(): void {
        if (!this.salesOrderId() || !this.currentSO) return;

        this.confirmationService.confirm({
            message: this.i18n.t('salesOrders.form.messages.confirmMessage', { number: this.currentSO.soNumber }),
            header: this.i18n.t('salesOrders.form.messages.confirmHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.salesOrderService
                    .confirmSalesOrder(this.salesOrderId()!)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe({
                        next: () => {
                            this.messageService.add({
                                severity: 'success',
                                summary: this.i18n.t('common.messages.success'),
                                detail: this.i18n.t('salesOrders.form.messages.confirmSuccess', { number: this.currentSO!.soNumber })
                            });
                            this.loadSalesOrder(this.salesOrderId()!);
                        },
                        error: (error) => {
                            this.messageService.add({
                                severity: 'error',
                                summary: this.i18n.t('common.messages.error'),
                                detail: error?.error?.message || this.i18n.t('salesOrders.form.messages.confirmFailed')
                            });
                            console.error('Error confirming sales order:', error);
                        }
                    });
            }
        });
    }

    /** Later-delivery flow: pack & mark ready to dispatch. */
    markReadyForDelivery(): void {
        if (!this.salesOrderId() || !this.currentSO) return;
        this.confirmationService.confirm({
            message: this.i18n.t('salesOrders.form.messages.readyMessage', { number: this.currentSO.soNumber }),
            header: this.i18n.t('salesOrders.form.messages.readyHeader'),
            icon: 'pi pi-truck',
            acceptButtonStyleClass: 'p-button-warning',
            accept: () => {
                this.salesOrderService.markReadyForDelivery(this.salesOrderId()!)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe({
                        next: () => {
                            this.messageService.add({ severity: 'success', summary: this.i18n.t('salesOrders.form.messages.updated'), detail: this.i18n.t('salesOrders.form.messages.readySuccess', { number: this.currentSO!.soNumber }) });
                            this.loadSalesOrder(this.salesOrderId()!);
                        },
                        error: err => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.detail || this.i18n.t('salesOrders.form.messages.statusFailed') })
                    });
            }
        });
    }

    /** Direct-handover flow: deliver immediately without a challan. */
    deliverDirect(): void {
        if (!this.salesOrderId() || !this.currentSO) return;
        this.confirmationService.confirm({
            message: this.i18n.t('salesOrders.form.messages.deliverMessage', { number: this.currentSO.soNumber }),
            header: this.i18n.t('salesOrders.form.messages.deliverHeader'),
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.salesOrderService.deliverDirect(this.salesOrderId()!)
                    .pipe(takeUntil(this.destroy$))
                    .subscribe({
                        next: () => {
                            this.messageService.add({ severity: 'success', summary: this.i18n.t('salesOrders.form.messages.delivered'), detail: this.i18n.t('salesOrders.form.messages.deliverSuccess', { number: this.currentSO!.soNumber }) });
                            this.loadSalesOrder(this.salesOrderId()!);
                        },
                        error: err => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.detail || this.i18n.t('salesOrders.form.messages.deliverFailed') })
                    });
            }
        });
    }

    // ── Challan dialog state ─────────────────────────────────────────────────
    showChallanDialog = false;
    challanForm = {
        deliveryAddress:  '',
        receiverName:     '',
        receiverPhone:    '',
        transportCompany: '',
        vehicleNumber:    '',
        driverName:       '',
        driverPhone:      '',
        notes:            ''
    };

    openChallanDialog(): void {
        if (!this.currentSO) return;
        this.challanForm = {
            deliveryAddress:  this.currentSO.customerCity    || '',
            receiverName:     this.currentSO.customerName    || '',
            receiverPhone:    this.currentSO.customerPhone   || '',
            transportCompany: '',
            vehicleNumber:    '',
            driverName:       '',
            driverPhone:      '',
            notes:            ''
        };
        this.showChallanDialog = true;
    }

    /** Generate a challan for the later-delivery flow. */
    generateChallan(): void {
        if (!this.salesOrderId() || !this.currentSO) return;
        this.showChallanDialog = false;
        const req: GenerateChallanRequest = { ...this.challanForm };
        this.challanService.generate(this.salesOrderId()!, req)
            .pipe(takeUntil(this.destroy$))
            .subscribe({
                next: challan => {
                    this.messageService.add({ severity: 'success', summary: this.i18n.t('salesOrders.form.messages.challanGenerated'), detail: this.i18n.t('salesOrders.form.messages.challanGeneratedDetail', { number: challan.challanNumber }) });
                    this.loadSalesOrder(this.salesOrderId()!);
                    window.open(`/sales/challans/${challan.id}/print`, '_blank');
                },
                error: err => this.messageService.add({ severity: 'error', summary: this.i18n.t('common.messages.error'), detail: err?.error?.detail || this.i18n.t('salesOrders.form.messages.challanFailed') })
            });
    }

    /**
     * Format date
     */
    formatDate(date: string): string {
        return new Date(date).toLocaleDateString('en-IN');
    }

    /**
     * Handler for when credit is applied to this SO
     */
    onCreditApplied(amount: number): void {
        this.totalCreditApplied += amount;
        this.messageService.add({
            severity: 'success',
            summary: this.i18n.t('salesOrders.form.messages.creditAppliedSummary'),
            detail: this.i18n.t('salesOrders.form.messages.creditAppliedDetail', { amount: this.formatCurrency(amount) })
        });

        // Reload SO to get updated data
        if (this.salesOrderId()) {
            this.loadSalesOrder(this.salesOrderId()!);
        }

        // Refresh available credit for customer
        if (this.selectedCustomerId) {
            this.loadAvailableCreditForCustomer(this.selectedCustomerId);
        }
    }

    /**
     * Load available credit for the selected customer
     */
    loadAvailableCreditForCustomer(customerId: string): void {
        this.creditNoteService.getTotalAvailableCredit(customerId).subscribe({
            next: (response: { totalAvailableCredit: number }) => {
                this.availableCreditForCustomer = response.totalAvailableCredit;
            },
            error: () => {
                // Silently fail - credit info is optional
                this.availableCreditForCustomer = 0;
            }
        });
    }

    /**
     * Get status badge severity
     */
    getStatusSeverity(status: string): string {
        return this.statusDisplay.getSeverity(status, 'sales-order');
    }

    private ensureCompatibleUnitsForLine(part: PublicPartResponse, line: FormGroup | null, preservePrice: boolean): void {
        if (!line) return;
        if (part.unitId) {
            this.unitService
                .getCompatibleUnits(part.unitId)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: (compatibleUnits) => {
                        this.compatibleUnitsMap.set(part.id, compatibleUnits);
                        if (!line.get('unitId')?.value) {
                            line.patchValue({ unitId: part.unitId });
                        }
                        if (!preservePrice) {
                            line.patchValue({ unitPrice: part.sellingPrice });
                        }
                    },
                    error: (err: Error) => {
                        console.error('Error loading compatible units:', err);
                        this.compatibleUnitsMap.set(part.id, this.units());
                    }
                });
        } else {
            this.compatibleUnitsMap.set(part.id, this.units());
        }
    }

    private hydrateLinePartDetails(lines: { partId: string }[]): void {
        const uniquePartIds = Array.from(new Set(lines.map((line) => line.partId).filter(Boolean)));
        uniquePartIds.forEach((partId) => {
            this.partService
                .getPartById(partId)
                .pipe(takeUntil(this.destroy$))
                .subscribe({
                    next: (part) => {
                        this.lines.controls.forEach((line) => {
                            if (line.get('part')?.value?.id === partId) {
                                const current = line.get('part')?.value || {};
                                const formLine = line as FormGroup;
                                formLine.patchValue({ part: { ...current, ...part } }, { emitEvent: false });
                                this.ensureCompatibleUnitsForLine(part, formLine, true);
                                const index = this.lines.controls.indexOf(formLine);
                                if (index >= 0) {
                                    this.scheduleLinePricingInfoRefresh(index);
                                }
                            }
                        });
                    },
                    error: (err: Error) => {
                        console.error('Error loading part details:', err);
                    }
                });
        });
    }
}
