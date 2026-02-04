import { Component, OnInit, OnDestroy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { TooltipModule } from 'primeng/tooltip';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService, ConfirmationService } from 'primeng/api';
import { SalesOrderService, CreateSalesOrderRequest, SalesOrderResponse } from '../../services/sales-order.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { PartService, PartResponse } from '../../../inventory/services/part.service';
import { TechnicianService, TechnicianResponse } from '../../services/technician.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { CurrencyService } from '../../../../shared/services/currency.service';
import { CurrencySelectorComponent } from '../../../../shared/components/currency-selector/currency-selector.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../../shared/components/lazy-autocomplete';
import { Subject, takeUntil, map } from 'rxjs';

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
        CardModule,
        ButtonModule,
        InputTextModule,
        TextareaModule,
        TooltipModule,
        DatePickerModule,
        LazyAutocompleteComponent
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
    private readonly customerService = inject(CustomerService);
    private readonly partService = inject(PartService);
    private readonly technicianService = inject(TechnicianService);
    private readonly unitService = inject(UnitService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    // Subscription management
    private readonly destroy$ = new Subject<void>();

    selectedCustomer: CustomerResponse | null = null;
    selectedTechnecian: TechnicianResponse | null = null;
    selectedPartQuickAdd: PartResponse | null = null;

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
                isActive: true
            })
            .pipe(
                map(
                    (response) =>
                        ({
                            items: response.data,
                            totalCount: response.pagination.totalCount
                        }) as LazyResponse<PartResponse>
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
    }

    // Handle customer clear from lazy autocomplete
    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.selectedCustomerId = '';
        this.clearCustomerSelection();
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

    // Units
    units = signal<UnitResponse[]>([]);
    loadingUnits = signal(false);
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
    onPartSelected(part: PartResponse, lineIndex: number): void {
        if (!part?.id) return;

        // Merge duplicates: if the same part exists in another line, increase its quantity and remove this line
        const existingIndex = this.lines.controls.findIndex(
            (line, idx) => idx !== lineIndex && line.get('part')?.value?.id === part.id
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

        // Auto-fill part details (part is already set by formControlName)
        const line = this.lines.at(lineIndex) as FormGroup;
        line.patchValue({
            unitPrice: part.sellingPrice
        });

        // Load compatible units for the selected part
        this.ensureCompatibleUnitsForLine(part, line, true);
    }

    // Quick-add part selection (single search above line items)
    onQuickAddPartSelected(part: PartResponse): void {
        if (!part?.id) return;

        const existingIndex = this.lines.controls.findIndex((line) => line.get('part')?.value?.id === part.id);
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
                    unitId: part.unitId || null,
                    quantity: 1,
                    unitPrice: part.sellingPrice || 0,
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
            technicianId: [null],
            technicianName: [null],
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
        return this.fb.group({
            part: [data?.part || null, [Validators.required]], // Full part object for lazy autocomplete
            unitId: [data?.unitId || null], // Optional unit selection
            quantity: [data?.quantity || 1, [Validators.required, Validators.min(1)]],
            unitPrice: [data?.unitPrice || 0, [Validators.required, Validators.min(0)]],
            discount: [data?.discount || 0, [Validators.min(0), Validators.max(100)]]
        });
    }

    addLine(): void {
        this.lines.push(this.createLine());
    }

    removeLine(index: number): void {
        if (this.lines.length > 1) {
            this.lines.removeAt(index);
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
                        deliveryDate: deliveryDate,
                        currency: order.currency || this.currencyService.selectedCurrency(),
                        orderDiscount: order.discount || 0,
                        notes: order.notes
                    });

                    // Clear and add lines
                    this.lines.clear();
                    order.lines.forEach((line) => {
                        // Create a minimal part object for the form control
                        const partObj = {
                            id: line.partId,
                            name: line.partName || '',
                            partNumber: line.partSku || '',
                            sku: line.partSku || '',
                            unitName: line.unitName || ''
                        } as PartResponse;

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
                    this.error.set('Failed to load sales order');
                    this.loading.set(false);
                    console.error('Error loading sales order:', err);
                }
            });
    }

    onSubmit(): void {
        // Validate customer selection
        if (!this.selectedCustomerId) {
            this.error.set('Please select a customer from the dropdown');
            return;
        }

        // Validate order-level discount
        if (this.orderDiscount() > 100) {
            this.error.set('Order discount cannot exceed 100%');
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
            this.error.set('Please fill in all required fields');
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
            customerName: formValue.customerName,
            customerEmail: formValue.customerEmail,
            customerPhone: formValue.customerPhone,
            customerCity: formValue.customerCity,
            technicianId: this.selectedTechnicianId || undefined,
            technicianName: formValue.technicianName || undefined,
            deliveryDate: deliveryDate,
            notes: formValue.notes,
            currency: formValue.currency,
            discount: this.orderDiscount(),
            lines: formValue.lines.map((line: any) => ({
                partId: line.part?.id,
                unitId: line.unitId,
                quantity: line.quantity,
                unitPrice: line.unitPrice,
                discount: line.discount || 0
            }))
        };

        const operation = this.mode() === 'edit' && this.salesOrderId() ? this.salesOrderService.updateSalesOrder(this.salesOrderId()!, request) : this.salesOrderService.createSalesOrder(request);

        operation.pipe(takeUntil(this.destroy$)).subscribe({
            next: () => {
                this.messageService.add({
                    severity: 'success',
                    summary: 'Success',
                    detail: `Sales order ${this.mode() === 'edit' ? 'updated' : 'created'} successfully!`
                });
                this.router.navigate(['/sales/sales-orders']);
            },
            error: (err) => {
                let errorMessage = `Failed to ${this.mode() === 'edit' ? 'update' : 'create'} sales order`;

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

    cancel(): void {
        this.router.navigate(['/sales/sales-orders']);
    }

    printProformaInvoice(): void {
        if (this.salesOrderForm.invalid || !this.selectedCustomerId) {
            this.messageService.add({
                severity: 'warn',
                summary: 'Incomplete Form',
                detail: 'Please complete all required fields before printing'
            });
            return;
        }

        const formValue = this.salesOrderForm.value;
        const customer = this.selectedCustomer;
        const technician = this.selectedTechnecian;

        const printWindow = window.open('', '_blank', 'width=800,height=600');
        if (!printWindow) {
            this.messageService.add({
                severity: 'error',
                summary: 'Print Failed',
                detail: 'Please allow pop-ups to print the proforma invoice'
            });
            return;
        }

        const today = new Date();
        const invoiceDate = today.toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' });
        const deliveryDate = formValue.deliveryDate ? new Date(formValue.deliveryDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : 'TBD';
        const invoiceNo = this.currentSO?.soNumber || `PI-${Date.now().toString().slice(-6)}`;
        const orderDiscount = this.orderDiscount();
        const orderDiscountAmount = this.orderDiscountAmount();

        let lineItemsHTML = '';
        this.lines.controls.forEach((line) => {
            const part = line.get('part')?.value as PartResponse | null;
            const quantity = line.get('quantity')?.value || 0;
            const unitPrice = line.get('unitPrice')?.value || 0;
            const discount = line.get('discount')?.value || 0;
            const lineTotal = quantity * unitPrice * (1 - discount / 100);
            const partMetaParts: string[] = [];
            if (part?.partNumber) partMetaParts.push(part.partNumber);
            if (part?.brandName) partMetaParts.push(part.brandName);
            if (part?.unitName) partMetaParts.push(part.unitName);
            const partMeta = partMetaParts.length ? partMetaParts.join(' | ') : '';

            lineItemsHTML += `
                <tr>
                    <td class="desc-cell">
                        <div class="item-name">${part?.name || 'N/A'}</div>
                        ${partMeta ? `<div class="item-desc">${partMeta}</div>` : `<div class="item-desc">-</div>`}
                    </td>
                    <td class="num-cell">${this.formatCurrency(unitPrice)}</td>
                    <td class="num-cell">${quantity}</td>
                    <td class="num-cell">${discount > 0 ? discount + '%' : '-'}</td>
                    <td class="num-cell">${this.formatCurrency(lineTotal)}</td>
                </tr>`;
        });

        const htmlContent = `<!DOCTYPE html>
<html>
<head>
    <title>Pro Forma Invoice - ${invoiceNo}</title>
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 12px; color: #333; padding: 20px; max-width: 800px; margin: 0 auto; }

        /* Header */
        .header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; }
        .logo-section { display: flex; align-items: center; gap: 10px; }
        .logo { width: 60px; height: 60px; background: linear-gradient(135deg, #1976d2 0%, #1565c0 100%); border-radius: 8px; display: flex; align-items: center; justify-content: center; color: white; font-size: 24px; font-weight: bold; }
        .company-name { font-size: 22px; font-weight: 700; color: #1976d2; }
        .title-section { text-align: right; }
        .title-section h1 { font-size: 28px; color: #1976d2; font-weight: 300; margin-bottom: 8px; }
        .invoice-meta { font-size: 11px; color: #666; }
        .invoice-meta span { display: inline-block; min-width: 80px; }
        .invoice-meta .value { color: #333; font-weight: 500; }

        /* Address Section */
        .address-section { display: flex; justify-content: space-between; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #e0e0e0; }
        .address-block { flex: 1; }
        .address-block.right { text-align: right; }
        .address-label { font-size: 10px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }
        .address-name { font-size: 14px; font-weight: 600; color: #333; margin-bottom: 4px; }
        .address-detail { font-size: 11px; color: #666; line-height: 1.5; }

        /* Table */
        .items-table { width: 100%; border-collapse: collapse; margin-bottom: 20px; }
        .items-table th { background: #1976d2; color: white; padding: 10px 8px; font-size: 10px; text-transform: uppercase; letter-spacing: 0.5px; font-weight: 500; }
        .items-table th:first-child { text-align: left; border-radius: 4px 0 0 0; }
        .items-table th:last-child { border-radius: 0 4px 0 0; }
        .items-table th.num-col { text-align: right; }
        .items-table td { padding: 10px 8px; border-bottom: 1px solid #eee; vertical-align: top; }
        .items-table tr:last-child td { border-bottom: none; }
        .desc-cell { width: 40%; }
        .num-cell { text-align: right; width: 15%; }
        .item-name { font-weight: 500; color: #333; }
        .item-desc { font-size: 10px; color: #999; margin-top: 2px; }

        /* Summary Section */
        .summary-section { display: flex; justify-content: space-between; margin-bottom: 20px; }
        .payment-info { flex: 1; padding-right: 40px; }
        .payment-info h4 { font-size: 11px; color: #999; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 8px; }
        .payment-info p { font-size: 11px; color: #666; line-height: 1.6; }
        .totals-box { width: 250px; }
        .totals-row { display: flex; justify-content: space-between; padding: 6px 0; font-size: 11px; }
        .totals-row.total { border-top: 2px solid #1976d2; margin-top: 8px; padding-top: 10px; font-size: 14px; font-weight: 600; color: #1976d2; }
        .totals-label { color: #666; }
        .totals-value { font-weight: 500; }

        /* Notes & Footer */
        .notes-section { background: #f9f9f9; padding: 12px; border-radius: 4px; margin-bottom: 20px; }
        .notes-section h4 { font-size: 11px; color: #1976d2; margin-bottom: 6px; }
        .notes-section p { font-size: 11px; color: #666; line-height: 1.5; }
        .disclaimer { text-align: center; padding: 15px; background: #fff3e0; border-radius: 4px; margin-bottom: 15px; }
        .disclaimer p { font-size: 10px; color: #e65100; }
        .disclaimer strong { display: block; font-size: 11px; margin-bottom: 4px; }
        .footer { text-align: center; color: #999; font-size: 10px; padding-top: 10px; border-top: 1px solid #eee; }

        /* Print */
        .no-print { margin-top: 20px; text-align: center; }
        .no-print button { padding: 10px 30px; border: none; border-radius: 4px; cursor: pointer; font-size: 14px; margin: 0 5px; }
        .btn-print { background: #1976d2; color: white; }
        .btn-close { background: #666; color: white; }
        @media print {
            body { padding: 10px; }
            .no-print { display: none; }
            .disclaimer { background: #fff; border: 1px solid #e65100; }
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="logo-section">
            <div class="logo">SM</div>
            <div class="company-name">Sujan Motors</div>
        </div>
        <div class="title-section">
            <h1>Pro Forma Invoice</h1>
            <div class="invoice-meta">
                <div><span>Invoice no.:</span> <span class="value">${invoiceNo}</span></div>
                <div><span>Invoice date:</span> <span class="value">${invoiceDate}</span></div>
                <div><span>Delivery:</span> <span class="value">${deliveryDate}</span></div>
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
            <div class="address-label">Bill to</div>
            <div class="address-name">${customer?.fullName || formValue.customerName}</div>
            <div class="address-detail">
                ${customer?.email || formValue.customerEmail}<br>
                ${customer?.phone || formValue.customerPhone}<br>
                ${customer?.city || formValue.customerCity}
            </div>
            ${
                technician
                    ? `
            <div style="margin-top: 10px;">
                <div class="address-label">Technician</div>
                <div class="address-detail">${technician.name} | ${technician.phone || 'N/A'}</div>
            </div>`
                    : ''
            }
        </div>
    </div>

    <table class="items-table">
        <thead>
            <tr>
                <th>Description</th>
                <th class="num-col">Unit Price</th>
                <th class="num-col">Qty</th>
                <th class="num-col">Disc</th>
                <th class="num-col">Amount</th>
            </tr>
        </thead>
        <tbody>
            ${lineItemsHTML}
        </tbody>
    </table>

    <div class="summary-section">
        <div class="payment-info">
            ${
                formValue.notes
                    ? `
            <h4>Notes</h4>
            <p>${formValue.notes}</p>`
                    : ''
            }
        </div>
        <div class="totals-box">
            <div class="totals-row">
                <span class="totals-label">Subtotal:</span>
                <span class="totals-value">${this.formatCurrency(this.subTotal())}</span>
            </div>
            ${
                orderDiscount > 0
                    ? `
            <div class="totals-row">
                <span class="totals-label">Discount (${orderDiscount}%):</span>
                <span class="totals-value">-${this.formatCurrency(orderDiscountAmount)}</span>
            </div>`
                    : ''
            }
            <div class="totals-row total">
                <span class="totals-label">Total:</span>
                <span class="totals-value">${this.formatCurrency(this.grandTotal())}</span>
            </div>
        </div>
    </div>

    <div class="disclaimer">
        <strong>THIS IS A PRO FORMA INVOICE</strong>
        <p>This is for estimation purposes only. An official invoice will be issued upon order confirmation.</p>
    </div>

    <div class="footer">
        <p>Thank you for choosing Sujan Motors | For inquiries, please contact us</p>
    </div>

    <div class="no-print">
        <button class="btn-print" onclick="window.print()">Print Invoice</button>
        <button class="btn-close" onclick="window.close()">Close</button>
    </div>
</body>
</html>`;

        printWindow.document.write(htmlContent);
        printWindow.document.close();

        this.messageService.add({
            severity: 'success',
            summary: 'Print Ready',
            detail: 'Proforma invoice opened in new window'
        });
    }

    formatCurrency(amount: number): string {
        const currencyCode = this.salesOrderForm?.get('currency')?.value || this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currencyCode);
    }

    /**
     * Confirm sales order
     */
    confirmSalesOrder(): void {
        if (!this.salesOrderId() || !this.currentSO) return;

        this.confirmationService.confirm({
            message: `Are you sure you want to confirm Sales Order ${this.currentSO.soNumber}? This action will make the order official and binding.`,
            header: 'Confirm Sales Order',
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
                                summary: 'Success',
                                detail: `Sales Order ${this.currentSO!.soNumber} confirmed successfully`
                            });
                            this.loadSalesOrder(this.salesOrderId()!);
                        },
                        error: (error) => {
                            this.messageService.add({
                                severity: 'error',
                                summary: 'Error',
                                detail: error?.error?.message || 'Failed to confirm sales order'
                            });
                            console.error('Error confirming sales order:', error);
                        }
                    });
            }
        });
    }

    /**
     * Format date
     */
    formatDate(date: string): string {
        return new Date(date).toLocaleDateString('en-IN');
    }

    /**
     * Get status badge severity
     */
    getStatusSeverity(status: string): string {
        const severityMap: Record<string, string> = {
            DRAFT: 'secondary',
            CONFIRMED: 'info',
            PARTIALLY_SHIPPED: 'warning',
            SHIPPED: 'primary',
            DELIVERED: 'success',
            CANCELLED: 'danger'
        };
        return severityMap[status] || 'secondary';
    }

    private ensureCompatibleUnitsForLine(part: PartResponse, line: FormGroup | null, preservePrice: boolean): void {
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
