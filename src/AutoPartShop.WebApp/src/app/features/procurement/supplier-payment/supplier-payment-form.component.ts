import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { InputNumberModule } from 'primeng/inputnumber';
import { AutoCompleteModule } from 'primeng/autocomplete';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { RadioButtonModule } from 'primeng/radiobutton';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { MessageService } from 'primeng/api';
import { SupplierPaymentService, CreateSupplierPaymentRequest } from '../services/supplier-payment.service';
import { SupplierService, SupplierResponse } from '../../inventory/services/supplier.service';
import { PaymentProviderService, PaymentProviderResponse } from '../services/payment-provider.service';
import { PurchaseOrderResponse, PurchaseOrderService } from '../services/purchase-order.service';
import { SupplierPaymentAccountService, SupplierPaymentAccountResponse } from '../../inventory/services/supplier-payment-account.service';
import { CurrencyService, Currency } from '../../../shared/services/currency.service';
import { SUPPLIER_PAYMENT_METHODS, PAYMENT_TYPES, PaymentMethodOption, PaymentTypeOption } from '../../../shared/constants/payment-methods.constants';

@Component({
    selector: 'app-supplier-payment-form',
    standalone: true,
    imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterLink, ButtonModule, InputTextModule, TextareaModule, InputNumberModule, AutoCompleteModule, CardModule, ToastModule, RadioButtonModule, DatePickerModule, SelectModule],
    providers: [MessageService],
    templateUrl: './supplier-payment-form.component.html',
    styleUrls: ['./supplier-payment-form.component.css']
})
export class SupplierPaymentFormComponent implements OnInit {
    private readonly fb = inject(FormBuilder);
    private readonly router = inject(Router);
    private readonly route = inject(ActivatedRoute);
    private readonly messageService = inject(MessageService);
    private readonly service = inject(SupplierPaymentService);
    private readonly supplierService = inject(SupplierService);
    private readonly paymentProviderService = inject(PaymentProviderService);
    private readonly poService = inject(PurchaseOrderService);
    private readonly supplierPaymentAccountService = inject(SupplierPaymentAccountService);
    private readonly currencyService = inject(CurrencyService);
    filteredPOs: PurchaseOrderResponse[] = [];
    purchaseOrders: PurchaseOrderResponse[] = [];
    form: FormGroup;
    loading = false;
    isEditing = false;
    paymentId: string | null = null;

    suppliers: SupplierResponse[] = [];
    filteredSuppliers: SupplierResponse[] = [];
    paymentProviders: PaymentProviderResponse[] = [];
    filteredPaymentProviders: PaymentProviderResponse[] = [];
    supplierPaymentAccounts: SupplierPaymentAccountResponse[] = [];
    filteredSupplierPaymentAccounts: SupplierPaymentAccountResponse[] = [];

    // Use shared payment methods from centralized constants
    paymentMethods: PaymentMethodOption[] = SUPPLIER_PAYMENT_METHODS;
    filteredPaymentMethods: PaymentMethodOption[] = [];

    // Use shared payment types from centralized constants
    paymentTypes: PaymentTypeOption[] = PAYMENT_TYPES;

    isAdvancePayment = false;

    // Currency settings
    defaultCurrency: Currency | null = null;
    currencies: Currency[] = [];
    selectedCurrency: Currency | null = null;
    currencyCode = this.currencyService.selectedCurrency();
    currencySymbol = '';

    constructor() {
        this.form = this.fb.group({
            supplierId: ['', Validators.required],
            paymentProviderId: ['', Validators.required],
            supplierPaymentAccountId: [''], // Supplier's payment account (destination)
            amount: [0, [Validators.required, Validators.min(1)]],
            paymentMethod: [''], // Auto-set from paymentProviderId's providerType
            paymentType: ['REGULAR', Validators.required],
            transactionNumber: [''],
            referenceNumber: [''],
            authorizationCode: [''],
            invoiceNumber: [''],
            purchaseOrderId: [''],
            paymentDate: [new Date()],
            notes: [''],
            description: [''] // For advance payments
        });
    }

    ngOnInit(): void {
        // Load default currency
        this.loadDefaultCurrency();

        // Load suppliers and payment providers
        this.loadSuppliers();
        this.loadPaymentProviders();
        this.loadPurchaseOrders();
        this.filteredPaymentMethods = this.paymentMethods;

        // Watch for payment type changes
        this.form.get('paymentType')?.valueChanges.subscribe((value) => {
            this.isAdvancePayment = value === 'ADVANCE';
            // Clear PO selection when switching to ADVANCE
            if (this.isAdvancePayment) {
                this.form.get('purchaseOrderId')?.setValue('');
            }
        });

        // Watch for supplier changes to load their payment accounts
        this.form.get('supplierId')?.valueChanges.subscribe((value) => {
            if (value) {
                const supplierId = typeof value === 'string' ? value : value?.id;
                if (supplierId) {
                    this.loadSupplierPaymentAccounts(supplierId);
                }
            } else {
                this.supplierPaymentAccounts = [];
                this.filteredSupplierPaymentAccounts = [];
                this.form.get('supplierPaymentAccountId')?.setValue('');
            }
        });

        // Watch for payment provider changes to auto-set payment method
        this.form.get('paymentProviderId')?.valueChanges.subscribe((value) => {
            if (value) {
                const provider = typeof value === 'object' ? value : this.paymentProviders.find((p) => p.id === value);
                if (provider?.providerType) {
                    // Auto-set payment method based on provider type
                    const matchingMethod = this.paymentMethods.find((m) => m.value === provider.providerType);
                    if (matchingMethod) {
                        this.form.patchValue({ paymentMethod: matchingMethod }, { emitEvent: false });
                    }
                }
            }
        });

        this.form.get('purchaseOrderId')?.valueChanges.subscribe((purchase) => {
            if (purchase && purchase.id) {
                // Pre-select the purchase order after POs are loaded
                this.poService.getPurchaseOrderById(purchase.id).subscribe({
                    next: (po) => {
                        if (po && po.outstandingAmount > 0) {
                            this.form.get('amount')?.addValidators([Validators.max(po.outstandingAmount)]);
                            this.form.patchValue({ amount: po.outstandingAmount });
                        }
                    },
                    error: (error) => {
                        console.error('Error loading purchase order:', error);
                        this.messageService.add({
                            severity: 'error',
                            summary: 'Error',
                            detail: 'Failed to load purchase order details'
                        });
                    }
                });
            }
        });

        this.route.queryParams.subscribe((params) => {
            if (params['id']) {
                this.paymentId = params['id'];
                this.isEditing = true;
                this.loadPayment();
                // Disable fields that shouldn't be edited after creation
                this.form.get('supplierId')?.disable();
                this.form.get('paymentProviderId')?.disable();
                this.form.get('amount')?.disable();
                this.form.get('transactionNumber')?.disable();
                this.form.get('referenceNumber')?.disable();
                this.form.get('invoiceNumber')?.disable();
                this.form.get('paymentDate')?.disable();
                this.form.get('purchaseOrderId')?.disable();
            } else if (params['purchaseOrderId']) {
                // Coming from Purchase Order - pre-select supplier and PO, disable supplier
                this.isFromPurchaseOrder = true;
                this.preSelectFromPurchaseOrder(params['purchaseOrderId'], params['supplierId']);
            } else if (params['supplierId']) {
                // Pre-select supplier when coming from supplier list "Record Payment" action
                this.preSelectSupplier(params['supplierId']);
            }
        });
    }

    // Flag to track if payment is being recorded from a purchase order
    isFromPurchaseOrder = false;

    private loadPurchaseOrders(): void {
        this.poService.getAllPurchaseOrders().subscribe({
            next: (pos) => {
                // Only show purchase orders that are approved and ready to receive goods
                this.purchaseOrders = pos.filter((po) => po.status === 'CONFIRMED' || po.status === 'PARTIAL');
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
     * Pre-select a supplier by ID (used when navigating from supplier list)
     */
    private preSelectSupplier(supplierId: string): void {
        this.supplierService.getSupplierById(supplierId).subscribe({
            next: (supplier) => {
                this.form.patchValue({ supplierId: supplier });
            },
            error: (error) => {
                console.error('Error loading supplier:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load supplier details'
                });
            }
        });
    }

    /**
     * Pre-select from Purchase Order context
     * Disables supplier field since it's determined by the PO
     */
    private preSelectFromPurchaseOrder(purchaseOrderId: string, supplierId?: string): void {
        // First load the supplier and disable the field
        if (supplierId) {
            this.supplierService.getSupplierById(supplierId).subscribe({
                next: (supplier) => {
                    this.form.patchValue({ supplierId: supplier });
                    // Disable supplier field - it's determined by PO
                    this.form.get('supplierId')?.disable();
                },
                error: (error) => {
                    console.error('Error loading supplier:', error);
                }
            });
        }

        // Pre-select the purchase order after POs are loaded
        this.poService.getPurchaseOrderById(purchaseOrderId).subscribe({
            next: (po) => {
                this.form.patchValue({ purchaseOrderId: po });
                // Disable PO field - it's passed via URL
                this.form.get('purchaseOrderId')?.disable();
                // Also set payment type to REGULAR since it's for a specific PO
                this.form.patchValue({ paymentType: 'REGULAR' });
                this.form.get('amount')?.addValidators([Validators.max(po.outstandingAmount)]);
                this.form.patchValue({ amount: po.outstandingAmount });
            },
            error: (error) => {
                console.error('Error loading purchase order:', error);
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load purchase order details'
                });
            }
        });
    }

    /**
     * Load suppliers for autocomplete
     */
    private loadSuppliers(): void {
        this.supplierService.getAllSuppliers().subscribe({
            next: (suppliers) => {
                this.suppliers = Array.isArray(suppliers) ? suppliers : [];
                this.filteredSuppliers = this.suppliers;
            },
            error: (error) => {
                console.error('Error loading suppliers:', error);
            }
        });
    }

    /**
     * Load payment providers for autocomplete
     */
    private loadPaymentProviders(): void {
        this.paymentProviderService.getAllPaymentProviders().subscribe({
            next: (providers) => {
                this.paymentProviders = Array.isArray(providers) ? providers : [];
                this.filteredPaymentProviders = this.paymentProviders;
            },
            error: (error) => {
                console.error('Error loading payment providers:', error);
            }
        });
    }

    /**
     * Load default currency and active currencies
     */
    private loadDefaultCurrency(): void {
        // Load active currencies for dropdown
        this.currencyService.getActiveCurrencies().subscribe({
            next: (currencies) => {
                this.currencies = currencies;
            },
            error: (error) => {
                console.error('Error loading currencies:', error);
            }
        });

        // Subscribe to default currency changes
        this.currencyService.defaultCurrency$.subscribe((currency) => {
            if (currency && currency.code) {
                this.defaultCurrency = currency;
                // Set as selected if no currency selected yet
                if (!this.selectedCurrency) {
                    this.selectedCurrency = currency;
                    this.currencyCode = currency.code;
                    this.currencySymbol = currency.symbol;
                }
            }
        });
    }

    /**
     * Handle currency change
     */
    onCurrencyChange(currency: Currency): void {
        this.selectedCurrency = currency;
        this.currencyCode = currency.code;
        this.currencySymbol = currency.symbol;
        this.currencyService.setSelectedCurrency(currency.code);
    }

    /**
     * Load supplier payment accounts for autocomplete
     */
    private loadSupplierPaymentAccounts(supplierId: string): void {
        this.supplierPaymentAccountService.getActiveBySupplier(supplierId).subscribe({
            next: (accounts) => {
                this.supplierPaymentAccounts = Array.isArray(accounts) ? accounts : [];
                this.filteredSupplierPaymentAccounts = this.supplierPaymentAccounts;
                // Auto-select default account if available
                const defaultAccount = this.supplierPaymentAccounts.find((a) => a.isDefault);
                if (defaultAccount && !this.form.get('supplierPaymentAccountId')?.value) {
                    this.form.patchValue({ supplierPaymentAccountId: defaultAccount });
                }
            },
            error: (error) => {
                console.error('Error loading supplier payment accounts:', error);
                this.supplierPaymentAccounts = [];
                this.filteredSupplierPaymentAccounts = [];
            }
        });
    }

    /**
     * Filter suppliers
     */
    filterSuppliers(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredSuppliers = this.suppliers.filter((supplier) => supplier.name.toLowerCase().includes(query) || supplier.code.toLowerCase().includes(query));
    }

    /**
     * Filter payment providers
     */
    filterPaymentProviders(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredPaymentProviders = this.paymentProviders.filter((provider) => provider.providerName.toLowerCase().includes(query));
    }

    /**
     * Filter payment methods (using shared constants)
     */
    filterPaymentMethods(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredPaymentMethods = this.paymentMethods.filter((method) => method.label.toLowerCase().includes(query));
    }

    /**
     * Filter supplier payment accounts
     */
    filterSupplierPaymentAccounts(event: any): void {
        const query = event.query.toLowerCase();
        this.filteredSupplierPaymentAccounts = this.supplierPaymentAccounts.filter((account) => account.accountName.toLowerCase().includes(query) || account.displayText.toLowerCase().includes(query));
    }

    filterPOs(event: { query: string }): void {
        const filtered = this.purchaseOrders.filter((po) => po.poNumber.toLowerCase().includes(event.query.toLowerCase()));
        this.filteredPOs = filtered;
    }

    loadPayment(): void {
        if (!this.paymentId) return;
        this.loading = true;
        this.service.getSupplierPaymentById(this.paymentId).subscribe({
            next: (payment) => {
                this.form.patchValue({
                    supplierId: payment.supplierId,
                    paymentProviderId: payment.paymentProviderId,
                    amount: payment.amount,
                    paymentMethod: payment.paymentMethod,
                    transactionNumber: payment.transactionNumber,
                    referenceNumber: payment.referenceNumber,
                    authorizationCode: payment.authorizationCode,
                    invoiceNumber: payment.invoiceNumber,
                    notes: payment.notes,
                    paymentDate: new Date(payment.paymentDate)
                });
                this.loading = false;
            },
            error: (error) => {
                this.messageService.add({
                    severity: 'error',
                    summary: 'Error',
                    detail: 'Failed to load supplier payment'
                });
                this.loading = false;
            }
        });
    }

    onSubmit(): void {
        if (!this.form.valid) {
            this.form.markAllAsTouched();
            this.messageService.add({
                severity: 'error',
                summary: 'Validation Error',
                detail: 'Please fill in all required fields'
            });
            return;
        }

        // Additional validation: REGULAR payments require a Purchase Order
        const paymentType = this.form.get('paymentType')?.value;
        const purchaseOrderId = this.form.get('purchaseOrderId')?.value;
        if (paymentType === 'REGULAR' && !purchaseOrderId) {
            this.messageService.add({
                severity: 'error',
                summary: 'Validation Error',
                detail: 'Regular payments must be linked to a purchase order. Select a PO or choose Advance Payment type.'
            });
            return;
        }

        this.loading = true;

        if (this.isEditing && this.paymentId) {
            // For update, only send mutable fields
            const updateRequest = {
                status: '',
                referenceNumber: this.form.get('referenceNumber')?.value || '',
                authorizationCode: this.form.get('authorizationCode')?.value || '',
                notes: this.form.get('notes')?.value || ''
            };
            this.service.updateSupplierPayment(this.paymentId, updateRequest).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'Supplier payment updated successfully'
                    });
                    this.router.navigate(['/procurement/supplier-payments']);
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.error?.message || 'Failed to update supplier payment'
                    });
                    this.loading = false;
                }
            });
        } else {
            // For create, send all required fields
            const supplierId = this.form.get('supplierId')?.value;
            const paymentProviderId = this.form.get('paymentProviderId')?.value;
            const supplierPaymentAccountId = this.form.get('supplierPaymentAccountId')?.value;
            const paymentMethod = this.form.get('paymentMethod')?.value;
            const purchaseOrderId = this.form.get('purchaseOrderId')?.value;
            const paymentType = this.form.get('paymentType')?.value || 'REGULAR';

            // Derive payment method from provider type if not set
            let paymentMethodValue = '';
            if (paymentMethod) {
                paymentMethodValue = typeof paymentMethod === 'string' ? paymentMethod : paymentMethod?.value || '';
            }
            // Fallback: use provider's type as payment method
            if (!paymentMethodValue && paymentProviderId) {
                const provider = typeof paymentProviderId === 'object' ? paymentProviderId : null;
                paymentMethodValue = provider?.providerType || '';
            }

            const createRequest: CreateSupplierPaymentRequest = {
                supplierId: typeof supplierId === 'string' ? supplierId : supplierId?.id || '',
                paymentProviderId: typeof paymentProviderId === 'string' ? paymentProviderId : paymentProviderId?.id || '',
                supplierPaymentAccountId: supplierPaymentAccountId ? (typeof supplierPaymentAccountId === 'string' ? supplierPaymentAccountId : supplierPaymentAccountId?.id) : undefined,
                purchaseOrderId: purchaseOrderId ? (typeof purchaseOrderId === 'string' ? purchaseOrderId : purchaseOrderId?.id) : undefined,
                amount: this.form.get('amount')?.value || 0,
                paymentMethod: paymentMethodValue,
                paymentType: paymentType,
                transactionNumber: this.form.get('transactionNumber')?.value || '',
                referenceNumber: this.form.get('referenceNumber')?.value || '',
                invoiceNumber: this.form.get('invoiceNumber')?.value || '',
                paymentDate: this.form.get('paymentDate')?.value,
                notes: this.form.get('notes')?.value || '',
                description: this.form.get('description')?.value || ''
            };
            this.service.createSupplierPayment(createRequest).subscribe({
                next: () => {
                    this.messageService.add({
                        severity: 'success',
                        summary: 'Success',
                        detail: 'Supplier payment created successfully'
                    });
                    this.router.navigate(['/procurement/supplier-payments']);
                },
                error: (error) => {
                    this.messageService.add({
                        severity: 'error',
                        summary: 'Error',
                        detail: error?.error?.message || 'Failed to create supplier payment'
                    });
                    this.loading = false;
                }
            });
        }
    }

    onCancel(): void {
        this.router.navigate(['/procurement/supplier-payments']);
    }
    formatCurrency(value: number): string {
        return this.currencyService.formatCurrency(value, this.currencyService.selectedCurrency());
    }

    /**
     * Get icon for payment provider type
     */
    getProviderTypeIcon(providerType: string): string {
        switch (providerType?.toUpperCase()) {
            case 'BANK_TRANSFER':
            case 'BANK':
                return 'pi pi-building';
            case 'MOBILE_BANKING':
            case 'MOBILE':
                return 'pi pi-mobile';
            case 'CASH':
                return 'pi pi-wallet';
            case 'ONLINE_GATEWAY':
                return 'pi pi-globe';
            default:
                return 'pi pi-credit-card';
        }
    }

    /**
     * Format payment provider display text with account details
     */
    formatProviderDisplay(provider: PaymentProviderResponse): string {
        if (!provider) return '';

        const type = provider.providerType?.toUpperCase();
        let details = '';

        if (type === 'BANK_TRANSFER' || type === 'BANK') {
            // Bank: Show Bank Name + Last 4 digits + Account Holder
            const lastFour = provider.bankAccountNumber ? '****' + provider.bankAccountNumber.slice(-4) : '';
            details = `${provider.bankName || 'Bank'} ${lastFour}`;
            if (provider.beneficiaryName) {
                details += ` (${provider.beneficiaryName})`;
            }
        } else if (type === 'MOBILE_BANKING' || type === 'MOBILE') {
            // Mobile: Show Provider Name + Mobile Number + Account Holder
            details = `${provider.providerName} - ${provider.mobileNumber || 'N/A'}`;
            if (provider.accountHolderName) {
                details += ` (${provider.accountHolderName})`;
            }
        } else if (type === 'CASH') {
            details = 'Cash Payment';
        } else if (type === 'ONLINE_GATEWAY') {
            details = `${provider.providerName} Gateway`;
        } else {
            details = provider.providerName;
        }

        return details;
    }

    /**
     * Get short label for provider type
     */
    getProviderTypeLabel(providerType: string): string {
        switch (providerType?.toUpperCase()) {
            case 'BANK_TRANSFER':
            case 'BANK':
                return 'Bank';
            case 'MOBILE_BANKING':
            case 'MOBILE':
                return 'Mobile';
            case 'CASH':
                return 'Cash';
            case 'ONLINE_GATEWAY':
                return 'Online';
            default:
                return providerType || 'Other';
        }
    }

    /**
     * Get icon for supplier account type
     */
    getAccountTypeIcon(accountType: string): string {
        switch (accountType?.toUpperCase()) {
            case 'BANK_TRANSFER':
            case 'BANK':
                return 'pi pi-building';
            case 'MOBILE_BANKING':
            case 'MOBILE':
                return 'pi pi-mobile';
            case 'CASH':
                return 'pi pi-wallet';
            default:
                return 'pi pi-credit-card';
        }
    }

    /**
     * Format supplier payment account display text
     */
    formatSupplierAccountDisplay(account: SupplierPaymentAccountResponse): string {
        if (!account) return '';

        const type = account.accountType?.toUpperCase();
        let details = '';

        if (type === 'BANK_TRANSFER' || type === 'BANK') {
            const lastFour = account.bankAccountNumber ? '****' + account.bankAccountNumber.slice(-4) : '';
            details = `${account.bankName || 'Bank'} ${lastFour}`;
            if (account.beneficiaryName) {
                details += ` - ${account.beneficiaryName}`;
            }
        } else if (type === 'MOBILE_BANKING' || type === 'MOBILE') {
            details = `${account.mobileProvider || 'Mobile'} - ${account.mobileNumber || 'N/A'}`;
            if (account.mobileAccountHolderName) {
                details += ` (${account.mobileAccountHolderName})`;
            }
        } else {
            details = account.displayText || account.accountName;
        }

        return details;
    }

    /**
     * Get selected payment provider display info
     */
    get selectedProviderInfo(): { icon: string; label: string; details: string } | null {
        const provider = this.form.get('paymentProviderId')?.value;
        if (!provider || typeof provider === 'string') return null;

        return {
            icon: this.getProviderTypeIcon(provider.providerType),
            label: this.getProviderTypeLabel(provider.providerType),
            details: this.formatProviderDisplay(provider)
        };
    }

    /**
     * Get selected supplier account display info
     */
    get selectedSupplierAccountInfo(): { icon: string; label: string; details: string } | null {
        const account = this.form.get('supplierPaymentAccountId')?.value;
        if (!account || typeof account === 'string') return null;

        return {
            icon: this.getAccountTypeIcon(account.accountType),
            label: this.getProviderTypeLabel(account.accountType),
            details: this.formatSupplierAccountDisplay(account)
        };
    }
}
