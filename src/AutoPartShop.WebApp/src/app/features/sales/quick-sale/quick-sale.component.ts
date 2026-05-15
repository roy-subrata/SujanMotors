import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, FormArray, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, switchMap, of, forkJoin } from 'rxjs';
import { map, catchError } from 'rxjs/operators';

// PrimeNG Imports
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { RadioButtonModule } from 'primeng/radiobutton';
import { CheckboxModule } from 'primeng/checkbox';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { MessageService, ConfirmationService } from 'primeng/api';

// Services
import { QuickSaleService, QuickSaleLineItem, PaymentDetail, PaymentMethod, PaymentResponsibility } from '../services/quick-sale.service';
import { DiscountService, ResolveDiscountResult } from '../../inventory/services/discount.service';
import { PaymentProviderService, PaymentProviderResponse } from '../../procurement/services/payment-provider.service';
import { PublicPartService, PublicPartResponse } from '../services/public-part.service';
import { UnitService, UnitResponse } from '../../inventory/services/unit.service';
import { UnitConversionService } from '../../inventory/services/unit-conversion.service';
import { CustomerService } from '../services/customer.service';
import { TechnicianService, TechnicianResponse } from '../services/technician.service';
import { InvoicePdfService, InvoicePdfData } from '../services/invoice-pdf.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PricingValidationService } from '../../../shared/services/pricing-validation.service';

// Components
import { QuickCustomerDialogComponent } from '../components/quick-customer-dialog.component';
import { InvoicePreviewComponent } from '../components/invoice-preview.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';

@Component({
  selector: 'app-quick-sale',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    ButtonModule,
    InputTextModule,
    InputNumberModule,
    TableModule,
    CardModule,
    ToastModule,
    ConfirmDialogModule,
    RadioButtonModule,
    CheckboxModule,
    TooltipModule,
    DialogModule,
    SelectModule,
    RouterLink,
    QuickCustomerDialogComponent,
    InvoicePreviewComponent,
    LazyAutocompleteComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './quick-sale.component.html',
  styleUrls: ['./quick-sale.component.css']
})
export class QuickSaleComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly quickSaleService = inject(QuickSaleService);
  private readonly partService = inject(PublicPartService);
  private readonly unitService = inject(UnitService);
  private readonly unitConversionService = inject(UnitConversionService);
  private readonly customerService = inject(CustomerService);
  private readonly technicianService = inject(TechnicianService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly invoicePdfService = inject(InvoicePdfService);
  private readonly pricingValidationService = inject(PricingValidationService);
  private readonly discountService = inject(DiscountService);

  @ViewChild(QuickCustomerDialogComponent) quickCustomerDialog!: QuickCustomerDialogComponent;
  @ViewChild('toolbarContainer') toolbarContainer!: ElementRef<HTMLDivElement>;

  // Form
  quickSaleForm!: FormGroup;
  saving = signal(false);
  loading = signal(false);

  // Invoice Preview
  showInvoicePreview = false;
  invoicePreviewData: InvoicePdfData | null = null;
  showSummaryDialog = false;

  // Parts search
  parts = signal<PublicPartResponse[]>([]);
  filteredParts = signal<PublicPartResponse[]>([]);
  selectedPart = signal<PublicPartResponse | null>(null);
  selectedPartModel: PublicPartResponse | null = null; // For ngModel binding
  searchingParts = signal(false);
  fetchPartsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search || '',
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true
    }).pipe(
      map((res) => ({
        items: res.data ?? [],
        totalCount: res.pagination?.totalCount ?? 0
      }) as LazyResponse<PublicPartResponse>)
    );

  // Customer management  
  selectedCustomer = signal<any | null>(null);
  selectedCustomerModel: any | null = null; // For ngModel binding
  
  fetchCustomersLazy = (req: LazyRequest) =>
    this.customerService.getCustomers({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data,
        totalCount: res.pagination.totalCount
      } as LazyResponse<any>))
    );

  // Technician management
  selectedTechnician = signal<TechnicianResponse | null>(null);
  selectedTechnicianModel: TechnicianResponse | null = null; // For ngModel binding
  
  fetchTechniciansLazy = (req: LazyRequest) =>
    this.technicianService.getTechnicians({
      search: req.search,
      pageNumber: req.pageNumber,
      pageSize: req.pageSize
    }).pipe(
      map(res => ({
        items: res.data.filter(t => t.status === 'ACTIVE'),
        totalCount: res.pagination.totalCount
      } as LazyResponse<TechnicianResponse>))
    );

  // Lazy fetch for payment providers (static list from API)
  private _paymentProvidersLoaded = signal(false);
  fetchPaymentProvidersLazy = (req: LazyRequest) => {
    // If not loaded yet, load from API
    if (!this._paymentProvidersLoaded()) {
      return this.paymentProviderService.getAllPaymentProviders().pipe(
        map(providers => {
          this.paymentProviders = Array.isArray(providers) ? providers : [];
          this._paymentProvidersLoaded.set(true);
          return {
            items: this.paymentProviders.filter(p =>
              !req.search || p.providerName.toLowerCase().includes(req.search.toLowerCase())
            ),
            totalCount: this.paymentProviders.length
          } as LazyResponse<PaymentProviderResponse>;
        })
      );
    } else {
      // Return cached list
      return of({
        items: this.paymentProviders.filter(p =>
          !req.search || p.providerName.toLowerCase().includes(req.search.toLowerCase())
        ),
        totalCount: this.paymentProviders.length
      } as LazyResponse<PaymentProviderResponse>);
    }
  };

  // Cart items
  cartItems = signal<QuickSaleLineItem[]>([]);
  globalDiscount = 0;
  discountReason = '';

  // Promo code
  promoCode = signal('');
  cartDiscountResult = signal<ResolveDiscountResult | null>(null);
  applyingPromo = signal(false);

  // Units
  units = signal<UnitResponse[]>([]);
  loadingUnits = signal(false);
  // Map to store compatible units for each part (keyed by part ID)
  compatibleUnitsMap = new Map<string, UnitResponse[]>();
  private cartUnitSelection = new Map<number, string | null>();

  paymentProviders: PaymentProviderResponse[] = [];
  filteredPaymentProviders: PaymentProviderResponse[] = [];
  private readonly paymentProviderService = inject(PaymentProviderService);

  // Advance Balance
  useAdvanceBalance = signal(false);

  // Computed for available advance balance
  availableAdvance = computed(() => {
    const customer = this.selectedCustomer();
    return customer?.advanceAmount || 0;
  });

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

  filterPaymentProviders(event: any): void {
    const query = event.query.toLowerCase();
    this.filteredPaymentProviders = this.paymentProviders.filter(provider =>
      provider.providerName.toLowerCase().includes(query)
    );
  }

  paymentResponsibilities = [
    { label: 'Customer', value: 'CUSTOMER' },
    { label: 'Technician (Temporary)', value: 'TECHNICIAN_TEMPORARY' }
  ];

  // Payments
  payments = signal<PaymentDetail[]>([]);

  // VAT Configuration
  vatEnabled = signal(false);
  vatPercentage = signal(0);

  // Calculations
  subtotal = computed(() => {
    return this.cartItems().reduce((sum, item) => {
      const lineTotal = item.quantity * item.unitPrice;
      const discountAmount = (lineTotal * item.discount) / 100;
      return sum + (lineTotal - discountAmount);
    }, 0);
  });

  discountAmount = computed(() => {
    return this.cartItems().reduce((sum, item) => {
      const lineTotal = item.quantity * item.unitPrice;
      return sum + ((lineTotal * item.discount) / 100);
    }, 0);
  });

  vatAmount = computed(() => {
    if (!this.vatEnabled()) return 0;
    return (this.subtotal() * this.vatPercentage()) / 100;
  });

  cartDiscountAmount = computed(() => {
    return this.cartDiscountResult()?.discountAmount ?? 0;
  });

  grandTotal = computed(() => {
    return Math.max(0, this.subtotal() - this.cartDiscountAmount());
  });

  paidAmount = computed(() => {
    return this.payments().reduce((sum, payment) => sum + payment.amount, 0);
  });

  // Amount paid from advance (if using advance balance)
  advancePaidAmount = computed(() => {
    if (!this.useAdvanceBalance()) return 0;
    const available = this.availableAdvance();
    const due = this.grandTotal() - this.paidAmount();
    return Math.min(available, Math.max(0, due));
  });

  // Total amount already covered (payments + advance used)
  totalPaidAmount = computed(() => {
    return this.paidAmount() + this.advancePaidAmount();
  });

  dueAmount = computed(() => {
    return Math.max(0, this.grandTotal() - this.totalPaidAmount());
  });

  // Invoice number
  invoiceNumber = signal<string>('');

  // Additional UI properties
  currentDate = new Date().toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });
  lowStockWarning = false;
  printReceipt = true;
  sendSmsNotification = false;
  autoCreatePO = false;
  saleNotes = '';
  favoriteParts: PublicPartResponse[] = [];

  pricingErrors = new Map<number, string>();

  // Dialog states
  showHeldSalesDialog = false;
  heldSalesList: any[] = [];
  showLastSaleDialog = false;
  lastSaleInfo: any = null;
  showReturnsDialog = false;
  returnInvoiceNumber = '';
  returnItems: any[] = [];
  returnInvoiceData: any = null;
  showPriceCheckDialog = false;
  priceCheckCode = '';
  priceCheckResult: { partId: string; name: string; partNumber: string; sku: string; sellingPrice: number; stockLevel: number } | null = null;
  barcodeModeActive = false;
  barcodeValue = '';
  showStockCheckDialog = false;
  stockCheckCode = '';
  stockCheckResult: any = null;

  // Customer History & Credit Dialog states
  showCustomerHistoryDialog = false;
  customerHistoryList: any[] = [];
  loadingCustomerHistory = false;
  showCustomerCreditDialog = false;
  customerCreditInfo: { creditLimit: number; usedCredit: number; availableCredit: number; dueBalance: number } | null = null;
  loadingCustomerCredit = false;

  // Auto-save timer
  private autoSaveTimer?: any;
  private destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.initializeForm();
    this.loadInitialData();
    this.loadDraft();
    this.setupAutoSave();
    this.generateInvoiceNumber();
     this.loadPaymentProviders();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.autoSaveTimer) {
      clearInterval(this.autoSaveTimer);
    }
  }

  initializeForm(): void {
    this.quickSaleForm = this.fb.group({
      customerPhone: [''],
      paymentResponsibility: ['CUSTOMER', [Validators.required]],
      autoCreatePO: [false],
      notes: [''],
      technicianNotes: ['']
    });

    // Watch customer phone for auto-fill
    this.quickSaleForm.get('customerPhone')?.valueChanges.pipe(
      debounceTime(500),
      distinctUntilChanged(),
      switchMap(phone => {
        if (phone && phone.length >= 10) {
          return this.quickSaleService.searchCustomerByPhone(phone);
        }
        return of(null);
      })
    ).subscribe(customer => {
      if (customer) {
        this.selectedCustomer.set(customer);
        this.selectedCustomerModel = customer;
        this.messageService.add({
          severity: 'success',
          summary: 'Customer Found',
          detail: `${customer.firstName} ${customer.lastName}`,
          life: 2000
        });
      }
    });
  }

  loadInitialData(): void {
    this.loading.set(true);
    let loadedCount = 0;
    const totalLoads = 3; // Parts, VAT Config, Units

    const checkComplete = () => {
      loadedCount++;
      if (loadedCount === totalLoads) {
        this.loading.set(false);
        this.messageService.add({
          severity: 'success',
          summary: 'Ready',
          detail: `Loaded ${this.parts().length} parts and ${this.units().length} units`,
          life: 3000
        });
      }
    };

    // Load active parts
    this.searchingParts.set(true);
    this.partService.getActiveParts().subscribe({
      next: (parts) => {
        this.parts.set(parts);
        this.filteredParts.set(parts);
        this.searchingParts.set(false);
        checkComplete();
      },
      error: (err) => {
        console.error('Error loading parts:', err);
        this.searchingParts.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load parts. Please refresh the page.',
          life: 5000
        });
        checkComplete();
      }
    });

    // Load recent customers for possible quick access patterns (optional)
    // Customers will be loaded lazily via autocomplete
    // Technicians will be loaded lazily via autocomplete

    // Load VAT configuration
    this.quickSaleService.getVATConfig().subscribe({
      next: (config) => {
        this.vatEnabled.set(config.enabled);
        this.vatPercentage.set(config.percentage);
        checkComplete();
      },
      error: (err) => {
        console.error('Error loading VAT config:', err);
        // Use defaults
        this.vatEnabled.set(false);
        this.vatPercentage.set(0);
        this.messageService.add({
          severity: 'info',
          summary: 'VAT Config',
          detail: 'VAT is disabled by default',
          life: 3000
        });
        checkComplete();
      }
    });

    // Load units
    this.loadingUnits.set(true);
    this.unitService.getActiveUnits().subscribe({
      next: (units) => {
        this.units.set(units);
        this.loadingUnits.set(false);
        checkComplete();
      },
      error: (err) => {
        console.error('Error loading units:', err);
        this.loadingUnits.set(false);
        this.messageService.add({
          severity: 'warn',
          summary: 'Warning',
          detail: 'Failed to load units. Unit selection may not work.',
          life: 5000
        });
        checkComplete();
      }
    });
  }

  loadDraft(): void {
    const draft = this.quickSaleService.loadDraft();
    if (draft) {
      this.confirmationService.confirm({
        message: 'A draft sale was found. Do you want to restore it?',
        header: 'Restore Draft',
        icon: 'pi pi-question-circle',
        accept: () => {
          this.cartItems.set(draft.items || []);
          this.syncCartUnitSelection();
          this.payments.set(draft.payments || []);
          if (draft.customerPhone) {
            this.quickSaleForm.patchValue({ customerPhone: draft.customerPhone });
          }
          if (draft.notes) {
            this.quickSaleForm.patchValue({ notes: draft.notes });
          }
          this.messageService.add({
            severity: 'info',
            summary: 'Draft Restored',
            detail: 'Previous draft has been restored'
          });
        },
        reject: () => {
          this.quickSaleService.clearDraft();
        }
      });
    }
  }

  setupAutoSave(): void {
    // Auto-save every 30 seconds
    this.autoSaveTimer = setInterval(() => {
      if (this.cartItems().length > 0) {
        this.saveDraft();
      }
    }, 30000);
  }

  saveDraft(): void {
    const formValue = this.quickSaleForm.value;
    this.quickSaleService.saveDraft({
      customerId: this.selectedCustomer()?.id,
      customerName: this.selectedCustomer() ? `${this.selectedCustomer()?.firstName} ${this.selectedCustomer()?.lastName}` : undefined,
      customerPhone: formValue.customerPhone,
      items: this.cartItems(),
      payments: this.payments(),
      technicianId: this.selectedTechnician()?.id,
      notes: formValue.notes
    });
  }

  generateInvoiceNumber(): void {
    this.quickSaleService.generateInvoiceNumber().subscribe({
      next: (result) => {
        this.invoiceNumber.set(result.invoiceNumber);
      },
      error: () => {
        // Fallback to timestamp-based number
        this.invoiceNumber.set(`INV-${Date.now()}`);
      }
    });
  }

  // Part search
  filterParts(event: any): void {
    const query = event.query.toLowerCase();
    const filtered = this.parts().filter(p =>
      p.name.toLowerCase().includes(query) ||
      p.partNumber.toLowerCase().includes(query) ||
      p.sku.toLowerCase().includes(query)
    );
    this.filteredParts.set(filtered);
  }

  onPartSearch(event: any): void {
    this.filterParts(event);
  }

  selectPart(event: any): void {
    const part = event as PublicPartResponse;
    this.selectedPart.set(part);
    this.selectedPartModel = part;
    this.addPartToCart();
  }

  addPartToCart(): void {
    const part = this.selectedPartModel || this.selectedPart();
    if (!part) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Part Selected',
        detail: 'Please select a part to add'
      });
      return;
    }

    // Check if already in cart
    const existing = this.cartItems().find(item => item.partId === part.id);
    if (existing) {
      this.messageService.add({
        severity: 'info',
        summary: 'Part Already Added',
        detail: 'This part is already in the cart. Update quantity there.'
      });
      return;
    }

    // Load compatible units for the part
    if (part.unitId) {
      this.unitService.getCompatibleUnits(part.unitId).subscribe({
        next: (compatibleUnits) => {
          this.compatibleUnitsMap.set(part.id, compatibleUnits);
        },
        error: (err) => {
          console.error('Error loading compatible units:', err);
          // Fallback to all units if error occurs
          this.compatibleUnitsMap.set(part.id, this.units());
        }
      });
    } else {
      // Part has no base unit, allow all units
      this.compatibleUnitsMap.set(part.id, this.units());
    }

    // Add to cart
    const newItem: QuickSaleLineItem = {
      partId: part.id,
      partName: part.name,
      partNumber: part.partNumber,
      sku: part.sku,
      unitId: part.unitId || undefined, // Set part's base unit as default
      quantity: 1,
      unitPrice: part.sellingPrice,
      discount: 0,
      stockAvailable: undefined, // Stock will be checked via API
      warehouseLocation: '', // Could be populated from warehouse data
      supplierName: '' // Could be populated from supplier data
    };

    this.cartItems.update(items => [...items, newItem]);
    this.syncCartUnitSelection();
    this.selectedPart.set(null);
    this.selectedPartModel = null; // Clear the model as well

    this.messageService.add({
      severity: 'success',
      summary: 'Part Added',
      detail: `${part.name} added to cart`
    });
  }

  removeFromCart(index: number): void {
    this.cartItems.update(items => items.filter((_, i) => i !== index));
    this.pricingErrors.clear();
    this.syncCartUnitSelection();
  }

  updateCartItem(index: number, field: keyof QuickSaleLineItem, value: any): void {
    this.cartItems.update(items => {
      const newItems = [...items];
      newItems[index] = { ...newItems[index], [field]: value };
      return newItems;
    });
    this.clearPricingError(index);
  }

  // Customer management
  filterCustomers(event: any): void {
    // No longer needed - filtering is handled by lazy autocomplete
  }

  onCustomerSearch(event: any): void {
    // No longer needed - searching is handled by lazy autocomplete
  }

  selectCustomer(event: any): void {
    // Fetch fresh customer data from API to get latest balance information
    if (event && event.id) {
      this.customerService.getCustomerById(event.id).subscribe({
        next: (freshCustomer) => {
          this.selectedCustomer.set(freshCustomer);
          this.selectedCustomerModel = freshCustomer;
        },
        error: (err) => {
          console.error('Error fetching customer details:', err);
          // Fallback to cached data if API fails
          this.selectedCustomer.set(event);
          this.selectedCustomerModel = event;
          this.messageService.add({
            severity: 'warn',
            summary: 'Warning',
            detail: 'Using cached customer data. Balance may not be current.',
            life: 3000
          });
        }
      });
    } else {
      this.selectedCustomer.set(event);
      this.selectedCustomerModel = event;
    }
  }

  openQuickCustomerDialog(): void {
    this.quickCustomerDialog.open();
  }

  onCustomerCreated(customer: any): void {
    this.selectedCustomer.set(customer);
    this.selectedCustomerModel = customer;
    this.quickSaleForm.patchValue({ customerPhone: customer.phone });

    this.messageService.add({
      severity: 'success',
      summary: 'Customer Created',
      detail: `${customer.firstName} ${customer.lastName} added successfully`
    });
  }

  // Technician management
  filterTechnicians(event: any): void {
    // No longer needed - filtering is handled by lazy autocomplete
  }

  onTechnicianDropdownClick(): void {
    // No longer needed - loading is handled by lazy autocomplete
  }

  onTechnicianSearch(event: any): void {
    // No longer needed - searching is handled by lazy autocomplete
  }

  selectTechnician(event: any): void {
    this.selectedTechnician.set(event);
    this.selectedTechnicianModel = event;
  }

  // Payment management
  addPayment(): void {
    const newPayment: PaymentDetail = {
      method: 'CASH',
      amount: this.dueAmount(),
      reference: '',
      notes: ''
    };
    this.payments.update(payments => [...payments, newPayment]);
  }

  removePayment(index: number): void {
    this.payments.update(payments => payments.filter((_, i) => i !== index));
  }

  updatePayment(index: number, field: keyof PaymentDetail, value: any): void {
    this.payments.update(payments => {
      const newPayments = [...payments];
      // If user selected a payment provider object from autocomplete, extract the providerType
      if (field === 'method' && value && typeof value === 'object') {
        // providerType should match the PaymentMethod string expected by the API
        const providerType = (value as any).providerType ?? (value as any).id ?? '';
        newPayments[index] = { ...newPayments[index], method: providerType } as PaymentDetail;
      } else {
        newPayments[index] = { ...newPayments[index], [field]: value };
      }
      return newPayments;
    });
  }

  getReferencePlaceholder(method: string): string {
    switch (method) {
      case 'MOBILE_BANKING':
        return 'Transaction ID / Reference No.';
      case 'CARD':
        return 'Card last 4 digits / Approval Code';
      case 'PART_PAY':
        return 'Reference / Receipt No.';
      default:
        return 'Reference Number';
    }
  }

  // Submit sale
  onSubmit(): void {
    // Validation
    if (this.cartItems().length === 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'No Items',
        detail: 'Please add at least one item to the sale'
      });
      return;
    }

    if (!this.selectedCustomer()) {
      this.messageService.add({
        severity: 'error',
        summary: 'Customer Required',
        detail: 'Please select a customer'
      });
      return;
    }

    this.validatePricingThen(() => {
      if (this.dueAmount() > 0 && this.payments().length === 0) {
        this.confirmationService.confirm({
          message: `There is an outstanding amount of ${this.formatCurrency(this.dueAmount())}. Do you want to proceed?`,
          header: 'Confirm Sale',
          icon: 'pi pi-exclamation-triangle',
          accept: () => {
            this.processSale();
          }
        });
      } else {
        this.processSale();
      }
    });
  }

  processSale(): void {
    this.saving.set(true);

    const formValue = this.quickSaleForm.value;
    const customer = this.selectedCustomer();

    const request = {
      customerId: customer?.id,
      customerName: customer ? `${customer.firstName} ${customer.lastName}` : 'Guest',
      customerPhone: formValue.customerPhone || customer?.phone || '',
      customerEmail: customer?.email,
      technicianId: this.selectedTechnician()?.id,
      technicianName: this.selectedTechnician()?.name,
      technicianNotes: formValue.technicianNotes,
      paymentResponsibility: formValue.paymentResponsibility as PaymentResponsibility,
      autoCreatePO: this.autoCreatePO,
      items: this.cartItems(),
      payments: this.payments(),
      subtotal: this.subtotal(),
      discountAmount: this.discountAmount(),
      discountType: this.globalDiscount > 0 ? 'PERCENTAGE' : (this.cartDiscountResult()?.discountAmount ?? 0) > 0 ? 'FIXED' : 'NONE',
      discountReason: this.discountReason || undefined,
      vatAmount: this.vatAmount(),
      vatPercentage: this.vatPercentage(),
      grandTotal: this.grandTotal(),
      paidAmount: this.paidAmount(),
      dueAmount: this.dueAmount(),
      notes: this.saleNotes,
      // Advance payment support
      useAdvanceBalance: this.useAdvanceBalance(),
      advanceAmountToApply: this.advancePaidAmount(),
      // Not a quotation
      saveAsQuotation: false
    };

    // Store for invoice preview
    const invoicePreviewRequest = { ...request };

    this.quickSaleService.createQuickSale(request).subscribe({
      next: (result) => {
        // Save as last sale for F3 recall
        this.quickSaleService.saveLastSale(result);

        // Refresh customer data if a customer was selected
        if (customer && customer.id) {
          this.customerService.getCustomerById(customer.id).subscribe({
            next: (freshCustomer) => {
              // Customer refreshed - update selected customer if it matches
              if (this.selectedCustomer()?.id === freshCustomer.id) {
                this.selectedCustomer.set(freshCustomer);
                this.selectedCustomerModel = freshCustomer;
              }
            },
            error: (err) => console.error('Error refreshing customer data:', err)
          });
        }

        this.messageService.add({
          severity: 'success',
          summary: 'Sale Completed',
          detail: `Invoice ${result.invoiceNumber} created successfully`,
          life: 5000
        });

        // Prepare invoice preview data
        this.prepareInvoicePreview(result, invoicePreviewRequest);

        // Reset form
        this.resetForm();

        // Ask to print
        this.confirmationService.confirm({
          message: 'Sale completed successfully. Do you want to print the invoice?',
          header: 'Print Invoice',
          icon: 'pi pi-print',
          accept: () => {
            this.showInvoicePreview = true;
          },
          reject: () => {
            // Generate new invoice number for next sale
            this.generateInvoiceNumber();
          }
        });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({
          severity: 'error',
          summary: 'Sale Failed',
          detail: err.error?.message || 'Failed to complete sale'
        });
        console.error('Error creating sale:', err);
      }
    });
  }

  processQuotation(): void {
    // Validation
    if (this.cartItems().length === 0) {
      this.messageService.add({
        severity: 'error',
        summary: 'No Items',
        detail: 'Please add at least one item to the quotation'
      });
      return;
    }

    if (!this.selectedCustomer()) {
      this.messageService.add({
        severity: 'error',
        summary: 'Customer Required',
        detail: 'Please select a customer for the quotation'
      });
      return;
    }

    this.validatePricingThen(() => {
      this.saving.set(true);

      const formValue = this.quickSaleForm.value;
      const customer = this.selectedCustomer();

      const request = {
        customerId: customer?.id,
        customerName: customer ? `${customer.firstName} ${customer.lastName}` : 'Guest',
        customerPhone: formValue.customerPhone || customer?.phone || '',
        customerEmail: customer?.email,
        technicianId: this.selectedTechnician()?.id,
        technicianName: this.selectedTechnician()?.name,
        technicianNotes: formValue.technicianNotes,
        paymentResponsibility: formValue.paymentResponsibility as PaymentResponsibility,
        autoCreatePO: false,
        items: this.cartItems(),
        payments: [], // No payments for quotations
        subtotal: this.subtotal(),
        discountAmount: this.discountAmount(),
        vatAmount: this.vatAmount(),
        vatPercentage: this.vatPercentage(),
        grandTotal: this.grandTotal(),
        paidAmount: 0,
        dueAmount: this.grandTotal(),
        notes: this.saleNotes,
        // Quotation flag
        saveAsQuotation: true
      };

      this.quickSaleService.createQuickSale(request).subscribe({
        next: (result) => {
          this.saving.set(false);

          this.messageService.add({
            severity: 'success',
            summary: 'Quotation Created',
            detail: `Quotation ${result.salesOrderNumber} created successfully`,
            life: 5000
          });

          // Reset form
          this.resetForm();
          this.generateInvoiceNumber();

          // Navigate to sales orders to view the quotation
          this.router.navigate(['/sales/sales-orders']);
        },
        error: (err) => {
          this.saving.set(false);
          this.messageService.add({
            severity: 'error',
            summary: 'Quotation Failed',
            detail: err.error?.message || 'Failed to create quotation'
          });
          console.error('Error creating quotation:', err);
        }
      });
    });
  }

  private validatePricingThen(action: () => void): void {
    this.validatePricingBeforeSubmit().subscribe({
      next: (isValid) => {
        if (!isValid) {
          this.messageService.add({
            severity: 'error',
            summary: 'Pricing Error',
            detail: 'One or more items violate pricing rules.'
          });
          return;
        }
        action();
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Pricing Error',
          detail: 'Failed to validate pricing rules.'
        });
      }
    });
  }

  private getLocalPricingError(part: PublicPartResponse, unitPrice: number, discount: number, unitId?: string): string | null {
    if (unitPrice <= 0) return 'Selling price must be greater than 0.';
    if (discount < 0 || discount > 100) return 'Discount percentage must be between 0 and 100.';
    if (part.sellingPrice <= 0) return 'MRP must be configured before sale.';
    const isBaseUnit = !unitId || !part.unitId || unitId === part.unitId;
    if (isBaseUnit && unitPrice > part.sellingPrice) return 'Selling price cannot exceed MRP.';
    if (discount > 100) {
      return `Discount cannot exceed 100%.`;
    }
    return null;
  }

  private validatePricingBeforeSubmit() {
    if (this.cartItems().length === 0) return of(true);

    const validations = this.cartItems().map((item, index) => {
      const part = this.parts().find(p => p.id === item.partId);
      if (!part) {
        this.pricingErrors.set(index, 'Part details not found.');
        return of(false);
      }

      const unitPrice = Number(item.unitPrice || 0);
      const discount = Number(item.discount || 0);
      const localError = this.getLocalPricingError(part, unitPrice, discount, item.unitId);
      if (localError) {
        this.pricingErrors.set(index, localError);
        return of(false);
      }

      return this.pricingValidationService.validateLine(item.partId, unitPrice, discount, item.unitId).pipe(
        map(() => {
          this.clearPricingError(index);
          return true;
        }),
        catchError((err: any) => {
          const message = err?.error?.message || 'Invalid pricing for this item.';
          this.pricingErrors.set(index, message);
          return of(false);
        })
      );
    });

    return forkJoin(validations).pipe(map((results) => results.every(Boolean)));
  }

  getPricingError(index: number): string | null {
    return this.pricingErrors.get(index) || null;
  }

  clearPricingError(index: number): void {
    this.pricingErrors.delete(index);
  }

  getMaxDiscountForItem(_item: QuickSaleLineItem): number {
    return 100;
  }

  resetForm(): void {
    this.quickSaleForm.reset({ paymentResponsibility: 'CUSTOMER' });
    this.cartItems.set([]);
    this.payments.set([]);
    this.selectedCustomer.set(null);
    this.selectedTechnician.set(null);
    this.selectedPart.set(null);
    this.saving.set(false);
    this.autoCreatePO = false;
    this.saleNotes = '';
    this.printReceipt = true;
    this.sendSmsNotification = false;
    this.quickSaleService.clearDraft();
    this.pricingErrors.clear();
    this.cartUnitSelection.clear();
    // Reset advance balance
    this.useAdvanceBalance.set(false);
    // Reset promo code
    this.promoCode.set('');
    this.cartDiscountResult.set(null);
    // Reset discount
    this.globalDiscount = 0;
    this.discountReason = '';
  }

  onCartUnitChanged(item: QuickSaleLineItem, index: number): void {
    const part = this.parts().find(p => p.id === item.partId);
    if (!part?.unitId) {
      this.cartUnitSelection.set(index, item.unitId || null);
      return;
    }

    const previousUnitId = this.cartUnitSelection.get(index) || part.unitId;
    const nextUnitId = item.unitId || part.unitId;
    if (previousUnitId === nextUnitId) return;

    const currentPrice = Number(item.unitPrice || 0);
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
        this.updateCartItem(index, 'unitPrice', this.roundPrice(newPrice));
        this.cartUnitSelection.set(index, nextUnitId);
      },
      error: (err) => {
        console.error('Error converting unit price:', err);
        this.messageService.add({
          severity: 'warn',
          summary: 'Unit Conversion Missing',
          detail: 'No conversion configured between the selected units.'
        });
        this.cartUnitSelection.set(index, nextUnitId);
      }
    });
  }

  private syncCartUnitSelection(): void {
    const items = this.cartItems();
    this.cartUnitSelection.clear();
    items.forEach((item, index) => {
      this.cartUnitSelection.set(index, item.unitId || null);
    });
  }

  private roundPrice(value: number): number {
    return Math.round(value * 100) / 100;
  }

  /**
   * Scroll toolbar left or right using arrow buttons
   */
  scrollToolbar(direction: 'left' | 'right'): void {
    if (this.toolbarContainer?.nativeElement) {
      const container = this.toolbarContainer.nativeElement;
      const scrollAmount = 200; // pixels to scroll
      const newScrollPosition = direction === 'left' 
        ? container.scrollLeft - scrollAmount 
        : container.scrollLeft + scrollAmount;
      container.scrollTo({ left: newScrollPosition, behavior: 'smooth' });
    }
  }

  printInvoice(saleId: string): void {
    // Show the invoice preview dialog
    this.showInvoicePreview = true;
  }

  /**
   * Prepare invoice data for preview/print
   */
  prepareInvoicePreview(result: any, request: any): void {
    const companyConfig = this.invoicePdfService.getCompanyConfig();
    
    this.invoicePreviewData = {
      // Company Info
      companyName: companyConfig.companyName,
      companyAddress: companyConfig.companyAddress,
      companyPhone: companyConfig.companyPhone,
      companyEmail: companyConfig.companyEmail,
      companyTaxId: companyConfig.companyTaxId,

      // Invoice Details
      invoiceNumber: result.invoiceNumber,
      invoiceDate: new Date(),
      salesOrderNumber: result.salesOrderNumber,

      // Customer Info
      customerName: request.customerName,
      customerPhone: request.customerPhone,
      customerEmail: request.customerEmail,

      // Technician Info
      technicianName: request.technicianName,

      // Line Items
      items: request.items.map((item: QuickSaleLineItem, index: number) => ({
        slNo: index + 1,
        partNumber: item.partNumber || item.sku || '',
        description: item.partName || '',
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discount: item.discount,
        total: (item.quantity * item.unitPrice) - item.discount
      })),

      // Totals
      subtotal: request.subtotal,
      discountAmount: request.discountAmount,
      vatPercentage: request.vatPercentage,
      vatAmount: request.vatAmount,
      grandTotal: request.grandTotal,

      // Payment Info
      payments: request.payments.map((p: PaymentDetail) => ({
        method: p.method,
        amount: p.amount,
        reference: p.reference
      })),
      paidAmount: request.paidAmount,
      dueAmount: request.dueAmount,

      // Additional
      notes: request.notes,
      paymentTerms: 'Payment due on receipt'
    };
  }

  /**
   * Handle invoice print event
   */
  onInvoicePrint(): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Print Initiated',
      detail: 'Invoice sent to printer',
      life: 3000
    });
  }

  /**
   * Handle invoice download event
   */
  onInvoiceDownload(): void {
    this.messageService.add({
      severity: 'success',
      summary: 'Download Complete',
      detail: 'Invoice PDF downloaded successfully',
      life: 3000
    });
  }

  // Keyboard shortcuts
  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent) {
    // Skip if user is typing in an input field
    const target = event.target as HTMLElement;
    const isInputField = target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable;
    
    // F2 - New Sale
    if (event.key === 'F2') {
      event.preventDefault();
      this.resetForm();
      this.generateInvoiceNumber();
      this.messageService.add({
        severity: 'info',
        summary: 'New Sale',
        detail: 'Started a new sale',
        life: 2000
      });
    }

    // F3 - View Last Sale
    if (event.key === 'F3') {
      event.preventDefault();
      this.viewLastSale();
    }

    // F4 - Returns
    if (event.key === 'F4') {
      event.preventDefault();
      this.openReturns();
    }

    // F5 - Hold Sale
    if (event.key === 'F5') {
      event.preventDefault();
      this.holdSale();
    }

    // F6 - Recall Held Sales
    if (event.key === 'F6') {
      event.preventDefault();
      this.recallHeldSales();
    }

    // F7 - Stock Check
    if (event.key === 'F7') {
      event.preventDefault();
      this.openStockSearch();
    }

    // F8 - Price Check
    if (event.key === 'F8') {
      event.preventDefault();
      this.openPriceCheck();
    }

    // F9 - Quick Cash Payment
    if (event.key === 'F9') {
      event.preventDefault();
      this.quickCashPayment();
    }

    // F10 - Quick Card Payment
    if (event.key === 'F10') {
      event.preventDefault();
      this.quickCardPayment();
    }

    // Escape - Clear/Reset
    if (event.key === 'Escape' && !isInputField) {
      event.preventDefault();
      this.confirmationService.confirm({
        header: 'Clear Sale',
        message: 'Are you sure you want to clear the current sale?',
        icon: 'pi pi-exclamation-triangle',
        accept: () => {
          this.resetForm();
          this.generateInvoiceNumber();
        }
      });
    }

    // Ctrl+S or Cmd+S - Save draft
    if ((event.ctrlKey || event.metaKey) && event.key === 's') {
      event.preventDefault();
      this.saveDraft();
      this.messageService.add({
        severity: 'info',
        summary: 'Draft Saved',
        detail: 'Your progress has been saved',
        life: 2000
      });
    }

    // Ctrl+Enter or Cmd+Enter - Submit sale
    if ((event.ctrlKey || event.metaKey) && event.key === 'Enter') {
      event.preventDefault();
      this.onSubmit();
    }

    // Ctrl+N or Cmd+N - New customer
    if ((event.ctrlKey || event.metaKey) && event.key === 'n') {
      event.preventDefault();
      this.openQuickCustomerDialog();
    }

    // Ctrl+P - Print
    if ((event.ctrlKey || event.metaKey) && event.key === 'p') {
      event.preventDefault();
      if (this.cartItems().length > 0) {
        this.saveAndPrint();
      } else {
        this.messageService.add({
          severity: 'warn',
          summary: 'No Items',
          detail: 'Add items to cart before printing',
          life: 2000
        });
      }
    }
  }

  // Utility methods
  formatCurrency(amount: number): string {
    return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
  }

  canAddToCart(): boolean {
    return !!this.selectedPart();
  }

  calculateLineTotal(item: QuickSaleLineItem): number {
    const lineTotal = item.quantity * item.unitPrice;
    const discountAmount = (lineTotal * item.discount) / 100;
    return lineTotal - discountAmount;
  }

  removeCartItem(index: number): void {
    this.removeFromCart(index);
  }

  /**
   * Get compatible units for a specific part
   */
  getCompatibleUnitsForPart(partId: string): UnitResponse[] {
    if (!partId) return this.units();
    return this.compatibleUnitsMap.get(partId) || this.units();
  }

  saveAndPrint(): void {
    // First save the sale, then print
    this.onSubmit();
    // Print will be triggered after successful save
  }

  // Quick action toolbar methods
  openStockSearch(): void {
    this.showStockCheckDialog = true;
    this.stockCheckCode = '';
    this.stockCheckResult = null;
  }

  searchStock(): void {
    if (!this.stockCheckCode) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Enter Code',
        detail: 'Please enter SKU or part number',
        life: 2000
      });
      return;
    }

    // Search in loaded parts first
    const found = this.parts().find(p => 
      p.sku.toLowerCase().includes(this.stockCheckCode.toLowerCase()) ||
      p.partNumber.toLowerCase().includes(this.stockCheckCode.toLowerCase()) ||
      p.name.toLowerCase().includes(this.stockCheckCode.toLowerCase())
    );

    if (found) {
      this.stockCheckResult = {
        partId: found.id,
        name: found.name,
        partNumber: found.partNumber,
        sku: found.sku,
        sellingPrice: found.sellingPrice,
        stockLevel: found.minimumStock || 0
      };
    } else {
      this.messageService.add({
        severity: 'warn',
        summary: 'Not Found',
        detail: 'No part found matching that code',
        life: 2000
      });
      this.stockCheckResult = null;
    }
  }

  addStockCheckToCart(): void {
    if (this.stockCheckResult) {
      const part = this.parts().find(p => p.id === this.stockCheckResult.partId);
      if (part) {
        this.selectedPartModel = part;
        this.addPartToCart();
        this.showStockCheckDialog = false;
      }
    }
  }

  openPaymentHistory(): void {
    if (!this.selectedCustomer()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Customer Selected',
        detail: 'Please select a customer to view payment history'
      });
      return;
    }

    // Navigate or show dialog with payment history
    this.messageService.add({
      severity: 'info',
      summary: 'Payment History',
      detail: `Viewing payment history for ${this.selectedCustomer()?.fullName}`
    });
  }

  openCustomerHistory(): void {
    const customer = this.selectedCustomer();
    if (!customer) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Customer Selected',
        detail: 'Please select a customer to view history'
      });
      return;
    }

    this.showCustomerHistoryDialog = true;
    this.loadingCustomerHistory = true;
    this.customerHistoryList = [];

    // Show customer purchase history
    this.quickSaleService.getCustomerHistory(customer.id, 20).subscribe({
      next: (history) => {
        this.customerHistoryList = history;
        this.loadingCustomerHistory = false;
      },
      error: () => {
        this.loadingCustomerHistory = false;
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: 'Failed to load customer history',
          life: 3000
        });
      }
    });
  }

  closeCustomerHistoryDialog(): void {
    this.showCustomerHistoryDialog = false;
    this.customerHistoryList = [];
  }

  // Quick Action Methods for Enterprise Toolbar
  holdSale(): void {
    if (this.cartItems().length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Cart Empty',
        detail: 'No items to hold'
      });
      return;
    }

    const customer = this.selectedCustomer();
    const holdId = this.quickSaleService.holdSale({
      customerId: customer?.id,
      customerName: customer ? `${customer.firstName} ${customer.lastName}` : undefined,
      customerPhone: this.quickSaleForm.value.customerPhone,
      items: this.cartItems(),
      payments: this.payments(),
      technicianId: this.selectedTechnician()?.id,
      notes: this.quickSaleForm.value.notes
    });

    this.messageService.add({
      severity: 'success',
      summary: 'Sale Held',
      detail: `Sale held with ID: ${holdId}. Press F6 to recall.`,
      life: 3000
    });

    // Reset for new sale
    this.resetForm();
    this.generateInvoiceNumber();
  }

  recallHeldSales(): void {
    const heldSales = this.quickSaleService.getHeldSales();
    
    if (heldSales.length === 0) {
      this.messageService.add({
        severity: 'info',
        summary: 'No Held Sales',
        detail: 'There are no held sales to recall',
        life: 2000
      });
      return;
    }

    // Show held sales dialog
    this.showHeldSalesDialog = true;
    this.heldSalesList = heldSales;
  }

  recallSale(sale: any): void {
    const recalled = this.quickSaleService.recallHeldSale(sale.draftId);
    if (recalled) {
      this.cartItems.set(recalled.items || []);
      this.syncCartUnitSelection();
      this.payments.set(recalled.payments || []);
      if (recalled.customerPhone) {
        this.quickSaleForm.patchValue({ customerPhone: recalled.customerPhone });
      }
      if (recalled.notes) {
        this.quickSaleForm.patchValue({ notes: recalled.notes });
      }
      this.showHeldSalesDialog = false;
      this.messageService.add({
        severity: 'success',
        summary: 'Sale Recalled',
        detail: `Recalled sale with ${recalled.items?.length || 0} items`,
        life: 2000
      });
    }
  }

  deleteHeldSale(sale: any): void {
    this.quickSaleService.removeHeldSale(sale.draftId);
    this.heldSalesList = this.quickSaleService.getHeldSales();
    this.messageService.add({
      severity: 'info',
      summary: 'Held Sale Removed',
      detail: 'The held sale has been deleted',
      life: 2000
    });
  }

  viewLastSale(): void {
    const lastSale = this.quickSaleService.getLastSale();
    if (lastSale) {
      this.showLastSaleDialog = true;
      this.lastSaleInfo = lastSale;
    } else {
      this.messageService.add({
        severity: 'info',
        summary: 'No Recent Sale',
        detail: 'No recent sale found in this session',
        life: 2000
      });
    }
  }

  openReturns(): void {
    this.showReturnsDialog = true;
    this.returnInvoiceNumber = '';
    this.returnItems = [];
  }

  searchReturnInvoice(): void {
    if (!this.returnInvoiceNumber) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Enter Invoice Number',
        detail: 'Please enter an invoice number to search',
        life: 2000
      });
      return;
    }

    this.quickSaleService.lookupInvoice(this.returnInvoiceNumber).subscribe({
      next: (invoice) => {
        if (invoice) {
          this.returnInvoiceData = invoice;
          this.messageService.add({
            severity: 'success',
            summary: 'Invoice Found',
            detail: `Found invoice ${invoice.invoiceNumber}`,
            life: 2000
          });
        } else {
          this.messageService.add({
            severity: 'warn',
            summary: 'Invoice Not Found',
            detail: 'No invoice found with that number',
            life: 2000
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Search Failed',
          detail: 'Failed to search for invoice',
          life: 2000
        });
      }
    });
  }

  viewCustomerCredit(): void {
    const customer = this.selectedCustomer();
    if (!customer) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Customer',
        detail: 'Please select a customer first',
        life: 2000
      });
      return;
    }

    this.showCustomerCreditDialog = true;
    this.loadingCustomerCredit = true;
    this.customerCreditInfo = null;

    this.quickSaleService.getCustomerCredit(customer.id).subscribe({
      next: (credit) => {
        this.customerCreditInfo = credit;
        this.loadingCustomerCredit = false;
      },
      error: () => {
        // Fallback to customer data
        this.customerCreditInfo = {
          creditLimit: customer.creditLimit || 0,
          usedCredit: customer.dueBalance || 0,
          availableCredit: (customer.creditLimit || 0) - (customer.dueBalance || 0),
          dueBalance: customer.dueBalance || 0
        };
        this.loadingCustomerCredit = false;
      }
    });
  }

  closeCustomerCreditDialog(): void {
    this.showCustomerCreditDialog = false;
    this.customerCreditInfo = null;
  }

  openPriceCheck(): void {
    this.showPriceCheckDialog = true;
    this.priceCheckCode = '';
    this.priceCheckResult = null;
  }

  searchPrice(): void {
    if (!this.priceCheckCode) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Enter Code',
        detail: 'Please enter SKU or part number',
        life: 2000
      });
      return;
    }

    this.quickSaleService.getPriceByCode(this.priceCheckCode).subscribe({
      next: (result) => {
        if (result) {
          this.priceCheckResult = result;
        } else {
          this.messageService.add({
            severity: 'warn',
            summary: 'Not Found',
            detail: 'No part found with that code',
            life: 2000
          });
        }
      },
      error: () => {
        // Fallback: search in loaded parts
        const found = this.parts().find(p => 
          p.sku.toLowerCase() === this.priceCheckCode.toLowerCase() ||
          p.partNumber.toLowerCase() === this.priceCheckCode.toLowerCase()
        );
        if (found) {
          this.priceCheckResult = {
            partId: found.id,
            name: found.name,
            partNumber: found.partNumber,
            sku: found.sku,
            sellingPrice: found.sellingPrice,
            stockLevel: found.minimumStock || 0
          };
        } else {
          this.messageService.add({
            severity: 'warn',
            summary: 'Not Found',
            detail: 'No part found with that code',
            life: 2000
          });
        }
      }
    });
  }

  addPriceCheckToCart(): void {
    if (this.priceCheckResult) {
      const part = this.parts().find(p => p.id === this.priceCheckResult!.partId);
      if (part) {
        this.selectedPartModel = part;
        this.addPartToCart();
        this.showPriceCheckDialog = false;
      }
    }
  }

  setSearchMode(): void {
    if (this.barcodeModeActive) {
      this.barcodeModeActive = false;
      this.barcodeValue = '';
      this.messageService.add({
        severity: 'info',
        summary: 'Search Mode',
        detail: 'Manual search mode active',
        life: 2000
      });
    }
  }

  toggleBarcodeMode(): void {
    this.barcodeModeActive = !this.barcodeModeActive;
    this.barcodeValue = '';
    this.messageService.add({
      severity: 'info',
      summary: this.barcodeModeActive ? 'Barcode Mode ON' : 'Barcode Mode OFF',
      detail: this.barcodeModeActive ? 'Scan barcode to add products' : 'Manual search mode active',
      life: 2000
    });
  }

  processBarcodeInput(): void {
    if (!this.barcodeValue?.trim()) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Input',
        detail: 'Please scan a barcode or enter SKU/part number',
        life: 2000
      });
      return;
    }

    const searchValue = this.barcodeValue.trim();
    
    // Search for part by SKU, part number, or barcode
    this.quickSaleService.getPriceByCode(searchValue).subscribe({
      next: (part) => {
        if (part) {
          // Find the full part from parts list
          const fullPart = this.parts().find(p => p.id === part.partId);
          if (fullPart) {
            this.selectedPartModel = fullPart;
            this.addPartToCart();
            this.barcodeValue = '';
            this.messageService.add({
              severity: 'success',
              summary: 'Part Added',
              detail: `${part.name} added to cart`,
              life: 2000
            });
          } else {
            // Add directly to cart without using selectedPartModel
            const cartItem: QuickSaleLineItem = {
              partId: part.partId,
              partName: part.name,
              partNumber: part.partNumber,
              sku: part.sku,
              quantity: 1,
              unitPrice: part.sellingPrice,
              discount: 0,
              stockAvailable: part.stockLevel
            };
            this.cartItems.update(items => [...items, cartItem]);
            this.barcodeValue = '';
            this.messageService.add({
              severity: 'success',
              summary: 'Part Added',
              detail: `${part.name} added to cart`,
              life: 2000
            });
          }
        } else {
          this.messageService.add({
            severity: 'error',
            summary: 'Not Found',
            detail: `No part found with code: ${searchValue}`,
            life: 3000
          });
        }
      },
      error: () => {
        this.messageService.add({
          severity: 'error',
          summary: 'Error',
          detail: `Failed to find part: ${searchValue}`,
          life: 3000
        });
      }
    });
  }

  quickCashPayment(): void {
    const dueAmount = this.dueAmount();
    if (dueAmount <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Amount Due',
        detail: 'Payment already complete'
      });
      return;
    }
    const newPayment: PaymentDetail = {
      method: 'CASH',
      amount: dueAmount,
      reference: '',
      notes: ''
    };
    this.payments.update(payments => [...payments, newPayment]);
    this.messageService.add({
      severity: 'success',
      summary: 'Cash Payment Added',
      detail: `${this.formatCurrency(dueAmount)} cash payment added`,
      life: 2000
    });
  }

  quickCardPayment(): void {
    const dueAmount = this.dueAmount();
    if (dueAmount <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Amount Due',
        detail: 'Payment already complete'
      });
      return;
    }
    const newPayment: PaymentDetail = {
      method: 'CARD',
      amount: dueAmount,
      reference: '',
      notes: ''
    };
    this.payments.update(payments => [...payments, newPayment]);
    this.messageService.add({
      severity: 'success',
      summary: 'Card Payment Added',
      detail: `${this.formatCurrency(dueAmount)} card payment added`,
      life: 2000
    });
  }

  quickMobilePayment(): void {
    const dueAmount = this.dueAmount();
    if (dueAmount <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Amount Due',
        detail: 'Payment already complete'
      });
      return;
    }
    const newPayment: PaymentDetail = {
      method: 'MOBILE_BANKING',
      amount: dueAmount,
      reference: '',
      notes: ''
    };
    this.payments.update(payments => [...payments, newPayment]);
    this.messageService.add({
      severity: 'success',
      summary: 'Mobile Payment Added',
      detail: `${this.formatCurrency(dueAmount)} mobile payment added`,
      life: 2000
    });
  }

  addToDue(): void {
    const dueAmount = this.dueAmount();
    if (dueAmount <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Amount Due',
        detail: 'Payment already complete'
      });
      return;
    }
    const newPayment: PaymentDetail = {
      method: 'DUE',
      amount: dueAmount,
      reference: '',
      notes: ''
    };
    this.payments.update(payments => [...payments, newPayment]);
    this.messageService.add({
      severity: 'info',
      summary: 'Added to Due',
      detail: `${this.formatCurrency(dueAmount)} added to customer due`,
      life: 2000
    });
  }

  splitPayment(): void {
    this.messageService.add({
      severity: 'info',
      summary: 'Split Payment',
      detail: 'Add multiple payment methods below',
      life: 2000
    });
    this.addPayment();
  }

  clearCustomer(): void {
    this.selectedCustomer.set(null);
    this.selectedCustomerModel = null;
    this.quickSaleForm.patchValue({ customerPhone: '' });
    // Clear advance balance
    this.useAdvanceBalance.set(false);
  }

  quickAddPart(part: PublicPartResponse): void {
    this.selectedPartModel = part;
    this.addPartToCart();
  }

  applyBulkDiscount(): void {
    this.confirmationService.confirm({
      header: 'Bulk Discount',
      message: 'Apply discount to all items?',
      accept: () => {
        this.cartItems.update(items => 
          items.map(item => ({ ...item, discount: 5 }))
        );
        this.pricingErrors.clear();
        this.messageService.add({
          severity: 'success',
          summary: 'Discount Applied',
          detail: '5% discount applied to all items'
        });
      }
    });
  }

  applyGlobalDiscount(): void {
    const value = Math.max(0, Math.min(100, Number(this.globalDiscount || 0)));
    this.globalDiscount = value;
    if (this.cartItems().length === 0) {
      return;
    }
    this.cartItems.update(items =>
      items.map(item => ({ ...item, discount: value }))
    );
    this.pricingErrors.clear();
    this.messageService.add({
      severity: 'success',
      summary: 'Discount Applied',
      detail: `${value}% discount applied to all items`
    });
  }

  clearCart(): void {
    this.confirmationService.confirm({
      header: 'Clear Cart',
      message: 'Are you sure you want to clear all items?',
      accept: () => {
        this.cartItems.set([]);
        this.pricingErrors.clear();
        this.messageService.add({
          severity: 'info',
          summary: 'Cart Cleared',
          detail: 'All items removed from cart'
        });
      }
    });
  }

  incrementQty(index: number): void {
    this.cartItems.update(items => {
      const newItems = [...items];
      newItems[index] = { ...newItems[index], quantity: newItems[index].quantity + 1 };
      return newItems;
    });
    this.clearPricingError(index);
  }

  decrementQty(index: number): void {
    this.cartItems.update(items => {
      const newItems = [...items];
      if (newItems[index].quantity > 1) {
        newItems[index] = { ...newItems[index], quantity: newItems[index].quantity - 1 };
      }
      return newItems;
    });
    this.clearPricingError(index);
  }

  /**
   * Apply the entered promo code by calling the discount resolve endpoint
   */
  applyPromoCode(): void {
    const code = this.promoCode().trim();
    if (!code) {
      this.messageService.add({
        severity: 'warn',
        summary: 'No Code',
        detail: 'Please enter a promo code',
        life: 2000
      });
      return;
    }

    const currentSubtotal = this.subtotal();
    if (currentSubtotal <= 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Empty Cart',
        detail: 'Add items to cart before applying a promo code',
        life: 2000
      });
      return;
    }

    this.applyingPromo.set(true);
    this.discountService.resolveCartDiscount(currentSubtotal, code).subscribe({
      next: (result) => {
        this.applyingPromo.set(false);
        if (result && result.discountAmount > 0) {
          this.cartDiscountResult.set(result);
          this.messageService.add({
            severity: 'success',
            summary: 'Promo Applied',
            detail: `"${result.discountName || code}" — saved ${this.formatCurrency(result.discountAmount)}`,
            life: 3000
          });
        } else {
          this.cartDiscountResult.set(null);
          this.messageService.add({
            severity: 'warn',
            summary: 'Code Not Applicable',
            detail: 'This promo code does not apply to your current cart',
            life: 3000
          });
        }
      },
      error: (err) => {
        this.applyingPromo.set(false);
        this.cartDiscountResult.set(null);
        this.messageService.add({
          severity: 'error',
          summary: 'Invalid Code',
          detail: err.error?.message || 'Promo code not found or expired',
          life: 3000
        });
      }
    });
  }

  /**
   * Remove the applied promo code discount
   */
  removePromoCode(): void {
    this.promoCode.set('');
    this.cartDiscountResult.set(null);
    this.messageService.add({
      severity: 'info',
      summary: 'Promo Removed',
      detail: 'Promo code discount has been removed',
      life: 2000
    });
  }

  toggleVat(): void {
    this.vatEnabled.update(v => !v);
    this.messageService.add({
      severity: 'info',
      summary: 'VAT ' + (this.vatEnabled() ? 'Enabled' : 'Disabled'),
      detail: `VAT is now ${this.vatEnabled() ? 'included' : 'excluded'}`,
      life: 2000
    });
  }

  generateQuote(): void {
    if (this.cartItems().length === 0) {
      this.messageService.add({
        severity: 'warn',
        summary: 'Cart Empty',
        detail: 'Add items to generate a quote',
        life: 2000
      });
      return;
    }

    const customer = this.selectedCustomer();
    const formValue = this.quickSaleForm.value;

    const quoteRequest = {
      customerId: customer?.id,
      customerName: customer ? `${customer.firstName} ${customer.lastName}` : 'Guest',
      customerPhone: formValue.customerPhone || customer?.phone || '',
      items: this.cartItems(),
      subtotal: this.subtotal(),
      discountAmount: this.discountAmount(),
      vatAmount: this.vatAmount(),
      vatPercentage: this.vatPercentage(),
      grandTotal: this.grandTotal(),
      notes: formValue.notes
    };

    this.quickSaleService.generateQuote(quoteRequest).subscribe({
      next: (result) => {
        this.messageService.add({
          severity: 'success',
          summary: 'Quote Generated',
          detail: `Quote ${result.quoteNumber} created successfully`,
          life: 4000
        });
      },
      error: () => {
        // Fallback - just show quote summary
        this.messageService.add({
          severity: 'success',
          summary: 'Quote Ready',
          detail: `Quote for ${this.formatCurrency(this.grandTotal())} ready to print`,
          life: 3000
        });
      }
    });
  }

  onRefresh(): void {
    this.loadInitialData();
  }

  // Close dialog methods
  closeHeldSalesDialog(): void {
    this.showHeldSalesDialog = false;
  }

  closeLastSaleDialog(): void {
    this.showLastSaleDialog = false;
  }

  closeReturnsDialog(): void {
    this.showReturnsDialog = false;
  }

  closePriceCheckDialog(): void {
    this.showPriceCheckDialog = false;
  }

  closeStockCheckDialog(): void {
    this.showStockCheckDialog = false;
  }
}
