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
import {
    QuotationService,
    CreateQuotationRequest,
    QuotationResponse
} from '../../services/quotation.service';
import { CustomerService, CustomerResponse } from '../../services/customer.service';
import { PublicPartService, PublicPartResponse } from '../../services/public-part.service';
import { UnitService, UnitResponse } from '../../../inventory/services/unit.service';
import { CurrencyService } from '@/shared/services/currency.service';
import { CurrencySelectorComponent } from '@/shared/components/currency-selector/currency-selector.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '@/shared/components/lazy-autocomplete';
import { Subject, takeUntil, map } from 'rxjs';

@Component({
    selector: 'app-quotation-form',
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
    templateUrl: './quotation-form.component.html',
    styleUrls: ['./quotation-form.component.css']
})
export class QuotationFormComponent implements OnInit, OnDestroy {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly quotationService = inject(QuotationService);
    private readonly customerService = inject(CustomerService);
    private readonly partService = inject(PublicPartService);
    private readonly unitService = inject(UnitService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);

    private readonly destroy$ = new Subject<void>();

    quotationForm!: FormGroup;
    loading = signal(false);
    saving = signal(false);
    error = signal<string | null>(null);
    mode = signal<'create' | 'view'>('create');
    quotationId = signal<string | null>(null);
    currentQuotation: QuotationResponse | null = null;
    processing = signal(false);

    // Customer selection
    selectedCustomer: CustomerResponse | null = null;
    selectedCustomerId = '';

    // Quick-add part search
    selectedPartQuickAdd: PublicPartResponse | null = null;

    // Units
    units = signal<UnitResponse[]>([]);
    loadingUnits = signal(false);
    compatibleUnitsMap = new Map<string, UnitResponse[]>();

    searchCustomers = (req: LazyRequest) => {
        return this.customerService
            .getCustomers({ search: req.search, pageNumber: req.pageNumber, pageSize: req.pageSize })
            .pipe(
                map((response) => ({ items: response.data, totalCount: response.pagination.totalCount }) as LazyResponse<CustomerResponse>)
            );
    };

    searchParts = (req: LazyRequest) => {
        return this.partService
            .getParts({ search: req.search, pageNumber: req.pageNumber, pageSize: req.pageSize, isActive: true, flattenVariants: true })
            .pipe(
                map((response) => ({ items: response.data, totalCount: response.pagination.totalCount }) as LazyResponse<PublicPartResponse>)
            );
    };

    ngOnInit(): void {
        this.initializeForm();
        this.loadUnits();

        this.route.queryParams.pipe(takeUntil(this.destroy$)).subscribe((params) => {
            const id = params['id'];
            const mode = params['mode'];

            if (id) {
                this.quotationId.set(id);
                this.mode.set('view');
                this.loadQuotation(id);
            } else {
                this.mode.set(mode === 'view' ? 'view' : 'create');
            }
        });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        this.compatibleUnitsMap.clear();
    }

    initializeForm(): void {
        const defaultCurrency = this.currencyService.selectedCurrency();

        this.quotationForm = this.fb.group({
            customerName: ['', [Validators.required, Validators.minLength(2)]],
            customerEmail: ['', [Validators.required, Validators.email]],
            customerPhone: ['', [Validators.required]],
            validUntil: [null],
            currency: [defaultCurrency, [Validators.required]],
            discountPercentage: [0, [Validators.min(0), Validators.max(100)]],
            taxAmount: [0, [Validators.min(0)]],
            notes: [''],
            lines: this.fb.array([])
        });
    }

    get lines(): FormArray {
        return this.quotationForm.get('lines') as FormArray;
    }

    createLine(data?: any): FormGroup {
        return this.fb.group({
            part: [data?.part || null, [Validators.required]],
            variantId: [data?.variantId || null],
            unitId: [data?.unitId || null],
            quantity: [data?.quantity || 1, [Validators.required, Validators.min(1)]],
            unitPrice: [data?.unitPrice || 0, [Validators.required, Validators.min(0)]],
            discount: [data?.discount || 0, [Validators.min(0), Validators.max(100)]]
        });
    }

    addLine(): void {
        this.lines.push(this.createLine());
    }

    removeLine(index: number): void {
        if (this.lines.length > 1) this.lines.removeAt(index);
    }

    loadUnits(): void {
        this.loadingUnits.set(true);
        this.unitService.getActiveUnits().pipe(takeUntil(this.destroy$)).subscribe({
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

    getCompatibleUnitsForPart(partId: string | null): UnitResponse[] {
        if (!partId) return this.units();
        return this.compatibleUnitsMap.get(partId) || this.units();
    }

    private parseNumber(value: unknown): number {
        if (value === null || value === undefined || value === '') return 0;
        const num = typeof value === 'number' ? value : parseFloat(String(value));
        return isNaN(num) ? 0 : num;
    }

    // ── Customer selection ────────────────────────────────────────────────
    onCustomerSelected(customer: CustomerResponse): void {
        this.selectedCustomer = customer;
        this.selectedCustomerId = customer.id;
        this.quotationForm.patchValue({
            customerName: `${customer.firstName} ${customer.lastName}`,
            customerEmail: customer.email,
            customerPhone: customer.phone
        });
    }

    onCustomerCleared(): void {
        this.selectedCustomer = null;
        this.selectedCustomerId = '';
        this.quotationForm.patchValue({ customerName: '', customerEmail: '', customerPhone: '' });
    }

    // ── Line item part selection ────────────────────────────────────────────
    onPartSelected(part: PublicPartResponse, lineIndex: number): void {
        if (!part?.id) return;

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
        line.patchValue({
            variantId: part.variantId ?? null,
            unitPrice: part.effectiveSellingPrice ?? part.sellingPrice ?? 0
        });

        this.ensureCompatibleUnitsForLine(part, line);
    }

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
        } else {
            this.lines.push(
                this.createLine({
                    part,
                    variantId: part.variantId ?? null,
                    unitId: part.unitId || null,
                    quantity: 1,
                    unitPrice: part.effectiveSellingPrice ?? part.sellingPrice ?? 0,
                    discount: 0
                })
            );
            const newLine = this.lines.at(this.lines.length - 1) as FormGroup;
            this.ensureCompatibleUnitsForLine(part, newLine);
        }

        this.selectedPartQuickAdd = null;
    }

    onQuickAddPartCleared(): void {
        this.selectedPartQuickAdd = null;
    }

    onPartCleared(lineIndex: number): void {
        const line = this.lines.at(lineIndex);
        line.patchValue({ unitPrice: 0, unitId: null, variantId: null });
    }

    private ensureCompatibleUnitsForLine(part: PublicPartResponse, line: FormGroup | null): void {
        if (!line) return;
        if (part.unitId) {
            this.unitService.getCompatibleUnits(part.unitId).pipe(takeUntil(this.destroy$)).subscribe({
                next: (compatibleUnits) => {
                    this.compatibleUnitsMap.set(part.id, compatibleUnits);
                    if (!line.get('unitId')?.value) line.patchValue({ unitId: part.unitId });
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

    // ── Totals ───────────────────────────────────────────────────────────
    getLineTotal(index: number): number {
        const line = this.lines.at(index);
        if (!line) return 0;
        const qty = this.parseNumber(line.get('quantity')?.value);
        const price = this.parseNumber(line.get('unitPrice')?.value);
        const discount = this.parseNumber(line.get('discount')?.value);
        const total = qty * price * (1 - discount / 100);
        return isNaN(total) ? 0 : total;
    }

    subTotal(): number {
        if (!this.quotationForm) return 0;
        return this.lines.controls.reduce((sum, _, i) => sum + this.getLineTotal(i), 0);
    }

    discountPercentage(): number {
        if (!this.quotationForm) return 0;
        return this.parseNumber(this.quotationForm.get('discountPercentage')?.value);
    }

    discountAmount(): number {
        return (this.subTotal() * this.discountPercentage()) / 100;
    }

    taxAmount(): number {
        if (!this.quotationForm) return 0;
        return this.parseNumber(this.quotationForm.get('taxAmount')?.value);
    }

    totalAfterDiscount(): number {
        const total = this.subTotal() - this.discountAmount();
        return total < 0 ? 0 : total;
    }

    grandTotal(): number {
        return this.totalAfterDiscount() + this.taxAmount();
    }

    // ── Load / Submit ────────────────────────────────────────────────────
    loadQuotation(id: string): void {
        this.loading.set(true);
        this.error.set(null);

        this.quotationService.getById(id).pipe(takeUntil(this.destroy$)).subscribe({
            next: (quotation) => {
                this.currentQuotation = quotation;
                this.selectedCustomerId = quotation.customerId;
                this.selectedCustomer = {
                    id: quotation.customerId,
                    firstName: quotation.customerName?.split(' ')[0] || '',
                    lastName: quotation.customerName?.split(' ').slice(1).join(' ') || '',
                    fullName: quotation.customerName || '',
                    email: quotation.customerEmail || '',
                    phone: quotation.customerPhone || ''
                } as CustomerResponse;

                let validUntil: Date | null = null;
                if (quotation.validUntil) {
                    const parts = quotation.validUntil.split('T')[0].split('-');
                    validUntil = new Date(+parts[0], +parts[1] - 1, +parts[2]);
                }

                this.quotationForm.patchValue({
                    customerName: quotation.customerName,
                    customerEmail: quotation.customerEmail,
                    customerPhone: quotation.customerPhone,
                    validUntil,
                    currency: quotation.currency || this.currencyService.selectedCurrency(),
                    discountPercentage: quotation.discountPercentage || 0,
                    taxAmount: quotation.taxAmount || 0,
                    notes: quotation.notes
                });

                this.lines.clear();
                quotation.lines.forEach((line) => {
                    const partObj = {
                        id: line.partId,
                        name: line.partName || '',
                        displayName: line.variantName ? `${line.partName} - ${line.variantName}` : line.partName,
                        partNumber: line.sku || '',
                        sku: line.sku || '',
                        unitName: line.unitSymbol || ''
                    } as unknown as PublicPartResponse;

                    this.lines.push(
                        this.createLine({
                            part: partObj,
                            quantity: line.quantity,
                            unitPrice: line.unitPrice,
                            discount: line.discount
                        })
                    );
                });

                this.quotationForm.disable();
                this.loading.set(false);
            },
            error: (err: Error) => {
                this.error.set('Failed to load quotation');
                this.loading.set(false);
                console.error('Error loading quotation:', err);
            }
        });
    }

    onSubmit(): void {
        if (!this.selectedCustomerId) {
            this.error.set('Please select a customer from the dropdown');
            return;
        }

        if (this.quotationForm.invalid) {
            Object.keys(this.quotationForm.controls).forEach((key) => {
                const control = this.quotationForm.get(key);
                if (control?.invalid) control.markAsTouched();
            });
            this.lines.controls.forEach((line) => {
                Object.keys((line as FormGroup).controls).forEach((key) => {
                    const control = line.get(key);
                    if (control?.invalid) control.markAsTouched();
                });
            });
            this.error.set('Please fill in all required fields');
            return;
        }

        if (this.lines.length === 0) {
            this.error.set('At least one line item is required.');
            return;
        }

        const invalidLines: number[] = [];
        this.lines.controls.forEach((line, index) => {
            if (!line.get('part')?.value) invalidLines.push(index + 1);
        });
        if (invalidLines.length > 0) {
            this.error.set(`Please select parts for line item(s): ${invalidLines.join(', ')}`);
            return;
        }

        this.saving.set(true);
        this.error.set(null);

        const formValue = this.quotationForm.value;
        let validUntil: string | undefined;
        if (formValue.validUntil instanceof Date) {
            const d = formValue.validUntil;
            validUntil = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
        } else if (formValue.validUntil) {
            validUntil = formValue.validUntil;
        }

        const request: CreateQuotationRequest = {
            customerId: this.selectedCustomerId,
            customerName: formValue.customerName,
            customerEmail: formValue.customerEmail,
            customerPhone: formValue.customerPhone,
            validUntil: validUntil ?? null,
            notes: formValue.notes,
            currency: formValue.currency,
            discount: this.discountPercentage(),
            taxAmount: this.taxAmount(),
            lines: formValue.lines.map((line: any) => ({
                partId: line.part?.id,
                productVariantId: line.variantId ?? line.part?.variantId ?? null,
                unitId: line.unitId,
                quantity: line.quantity,
                unitPrice: line.unitPrice,
                discount: line.discount || 0
            }))
        };

        this.quotationService.create(request).pipe(takeUntil(this.destroy$)).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Quotation created successfully!' });
                this.router.navigate(['/sales/quotations']);
            },
            error: (err) => {
                let errorMessage = 'Failed to create quotation';
                if (err.error?.message) errorMessage = err.error.message;
                else if (err.error?.errors) errorMessage = Object.values(err.error.errors).flat().join(', ');
                else if (err.message) errorMessage = err.message;

                this.error.set(errorMessage);
                this.saving.set(false);
                console.error('Error creating quotation:', err);
            }
        });
    }

    cancel(): void {
        this.router.navigate(['/sales/quotations']);
    }

    // ── Status transitions (view mode) ───────────────────────────────────
    sendQuotation(): void {
        if (!this.quotationId() || !this.currentQuotation) return;
        this.confirmationService.confirm({
            message: `Send quotation ${this.currentQuotation.quotationNumber} to the customer?`,
            header: 'Send Quotation',
            icon: 'pi pi-send',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.processing.set(true);
                this.quotationService.send(this.quotationId()!).pipe(takeUntil(this.destroy$)).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Quotation marked as Sent.' });
                        this.processing.set(false);
                        this.loadQuotation(this.quotationId()!);
                    },
                    error: (err) => {
                        this.processing.set(false);
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to send quotation.' });
                    }
                });
            }
        });
    }

    acceptQuotation(): void {
        if (!this.quotationId() || !this.currentQuotation) return;
        this.confirmationService.confirm({
            message: `Mark quotation ${this.currentQuotation.quotationNumber} as Accepted?`,
            header: 'Accept Quotation',
            icon: 'pi pi-check-circle',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.processing.set(true);
                this.quotationService.accept(this.quotationId()!).pipe(takeUntil(this.destroy$)).subscribe({
                    next: () => {
                        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Quotation accepted.' });
                        this.processing.set(false);
                        this.loadQuotation(this.quotationId()!);
                    },
                    error: (err) => {
                        this.processing.set(false);
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to accept quotation.' });
                    }
                });
            }
        });
    }

    rejectQuotation(): void {
        if (!this.quotationId() || !this.currentQuotation) return;
        const reason = prompt(`Reason for rejecting quotation ${this.currentQuotation.quotationNumber}:`);
        if (reason === null) return;

        this.processing.set(true);
        this.quotationService.reject(this.quotationId()!, reason).pipe(takeUntil(this.destroy$)).subscribe({
            next: () => {
                this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Quotation rejected.' });
                this.processing.set(false);
                this.loadQuotation(this.quotationId()!);
            },
            error: (err) => {
                this.processing.set(false);
                this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to reject quotation.' });
            }
        });
    }

    convertQuotation(): void {
        if (!this.quotationId() || !this.currentQuotation) return;
        this.confirmationService.confirm({
            message: `Convert quotation ${this.currentQuotation.quotationNumber} into a new Sales Order?`,
            header: 'Convert to Sales Order',
            icon: 'pi pi-arrow-right-arrow-left',
            acceptButtonStyleClass: 'p-button-success',
            accept: () => {
                this.processing.set(true);
                this.quotationService.convertToSalesOrder(this.quotationId()!).pipe(takeUntil(this.destroy$)).subscribe({
                    next: (result) => {
                        this.processing.set(false);
                        this.messageService.add({ severity: 'success', summary: 'Converted', detail: `Sales Order ${result.soNumber} created.` });
                        this.router.navigate(['/sales/sales-orders/view'], { queryParams: { id: result.salesOrderId } });
                    },
                    error: (err) => {
                        this.processing.set(false);
                        this.messageService.add({ severity: 'error', summary: 'Error', detail: err?.error?.message || 'Failed to convert quotation.' });
                    }
                });
            }
        });
    }

    downloadPdf(): void {
        if (!this.quotationId() || !this.currentQuotation) return;
        this.quotationService.downloadPdf(this.quotationId()!, this.currentQuotation.quotationNumber).subscribe({
            error: () => {
                this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to download the quotation PDF' });
            }
        });
    }

    formatCurrency(amount: number | null | undefined): string {
        if (amount == null || isNaN(amount)) return '—';
        const currencyCode = this.quotationForm?.get('currency')?.value || this.currencyService.selectedCurrency();
        return this.currencyService.formatCurrency(amount, currencyCode);
    }

    formatDate(date: string): string {
        if (!date) return '-';
        return new Date(date).toLocaleDateString('en-IN');
    }

    getStatusSeverity(status: string): string {
        const severityMap: Record<string, string> = {
            DRAFT: 'secondary',
            SENT: 'info',
            ACCEPTED: 'success',
            REJECTED: 'danger',
            CONVERTED: 'contrast',
            EXPIRED: 'warn'
        };
        return severityMap[status] || 'secondary';
    }
}
