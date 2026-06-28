import { Component, OnInit, OnDestroy, inject, signal, computed, ViewChild, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Subject, of } from 'rxjs';
import { map } from 'rxjs/operators';

// PrimeNG Imports
import { AutoCompleteModule } from 'primeng/autocomplete';
import { ButtonModule } from 'primeng/button';
import { MenuModule } from 'primeng/menu';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { TableModule } from 'primeng/table';
import { CardModule } from 'primeng/card';
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { TooltipModule } from 'primeng/tooltip';
import { DialogModule } from 'primeng/dialog';
import { SelectModule } from 'primeng/select';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TextareaModule } from 'primeng/textarea';
import { MessageService, ConfirmationService, MenuItem } from 'primeng/api';

// Services
import { QuickSaleService, QuickSaleLineItem, PaymentDetail, PaymentMethod, PaymentResponsibility } from '../services/quick-sale.service';
import { PaymentProviderService, PaymentProviderResponse } from '../../procurement/services/payment-provider.service';
import { PublicPartService, PublicPartResponse } from '../services/public-part.service';
import { UnitService, UnitResponse } from '../../inventory/services/unit.service';
import { UnitConversionService } from '../../inventory/services/unit-conversion.service';
import { CustomerService } from '../services/customer.service';
import { CustomerVehicleService, CustomerVehicleResponse } from '../services/customer-vehicle.service';
import { TechnicianService, TechnicianResponse } from '../services/technician.service';
import { InvoicePdfService, InvoicePdfData } from '../services/invoice-pdf.service';
import { ThermalReceiptService } from '../services/thermal-receipt.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PricingValidationService } from '../../../shared/services/pricing-validation.service';
import { extractApiError } from '../../../shared/utils/api-error.util';
import { composeVariantDisplayName } from '../../../shared/utils/variant-name.util';

// Components
import { QuickCustomerDialogComponent } from '../components/quick-customer-dialog.component';
import { InvoicePreviewComponent } from '../components/invoice-preview.component';
import { LazyAutocompleteComponent, LazyRequest, LazyResponse } from '../../../shared/components/lazy-autocomplete';

@Component({
  selector: 'app-quick-sale-shortcut',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    AutoCompleteModule,
    ButtonModule,
    MenuModule,
    InputTextModule,
    InputNumberModule,
    TableModule,
    CardModule,
    ToastModule,
    ConfirmDialogModule,
    DialogModule,
    TooltipModule,
    SelectModule,
    ToggleSwitchModule,
    TextareaModule,
    RouterLink,
    QuickCustomerDialogComponent,
    InvoicePreviewComponent,
    LazyAutocompleteComponent
  ],
  providers: [MessageService, ConfirmationService],
  templateUrl: './quick-sale-shortcut.component.html',
  styleUrl: './quick-sale-shortcut.component.css',
  encapsulation: ViewEncapsulation.None
})
export class QuickSaleShortcutComponent implements OnInit, OnDestroy {
  // ===== SERVICES =====
  private readonly fb = inject(FormBuilder);
  private readonly quickSaleService = inject(QuickSaleService);
  private readonly partService = inject(PublicPartService);
  private readonly unitService = inject(UnitService);
  private readonly unitConversionService = inject(UnitConversionService);
  private readonly customerService = inject(CustomerService);
  private readonly vehicleService = inject(CustomerVehicleService);
  private readonly technicianService = inject(TechnicianService);
  private readonly currencyService = inject(CurrencyService);
  private readonly messageService = inject(MessageService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly invoicePdfService = inject(InvoicePdfService);
  private readonly thermalReceipt = inject(ThermalReceiptService);
  private readonly pricingValidationService = inject(PricingValidationService);
  private readonly paymentProviderService = inject(PaymentProviderService);

  @ViewChild(QuickCustomerDialogComponent) quickCustomerDialog!: QuickCustomerDialogComponent;

  // ===== STATE =====
  quickSaleForm!: FormGroup;
  saving = signal(false);
  loading = signal(false);
  private destroy$ = new Subject<void>();

  // Invoice Preview
  showInvoicePreview = false;
  invoicePreviewData: InvoicePdfData | null = null;
  currentInvoiceId = signal<string | null>(null);

  // Parts
  selectedPartModel: PublicPartResponse | null = null;
  fetchPartsLazy = (req: LazyRequest) =>
    this.partService.getParts({
      search: req.search || '',
      pageNumber: req.pageNumber,
      pageSize: req.pageSize,
      isActive: true,
      flattenVariants: true
    }).pipe(
      map((res) => ({
        items: res.data ?? [],
        totalCount: res.pagination?.totalCount ?? 0
      }) as LazyResponse<PublicPartResponse>)
    );

  // Customers
  selectedCustomer = signal<any | null>(null);
  selectedCustomerModel: any | null = null;

  // Optional vehicle this sale is for (loaded once a customer is selected)
  customerVehicles = signal<CustomerVehicleResponse[]>([]);
  selectedVehicleId = signal<string | null>(null);
  loadingVehicles = signal(false);
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

  // Technicians
  selectedTechnician = signal<TechnicianResponse | null>(null);
  selectedTechnicianModel: TechnicianResponse | null = null;
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

  // Cart
  cartItems = signal<QuickSaleLineItem[]>([]);
  pricingErrors = new Map<number, string>();


  // Units
  units = signal<UnitResponse[]>([]);
  loadingUnits = signal(false);
  compatibleUnitsMap = new Map<string, UnitResponse[]>();
  private cartUnitSelection = new Map<number, string | null>();

  // Payments
  payments = signal<PaymentDetail[]>([]);
  paymentProviders: PaymentProviderResponse[] = [];
  private _paymentProvidersLoaded = signal(false);
  fetchPaymentProvidersLazy = (req: LazyRequest) => {
    if (!this._paymentProvidersLoaded()) {
      return this.paymentProviderService.getAllPaymentProviders().pipe(
        map(providers => {
          this.paymentProviders = Array.isArray(providers) ? providers : [];
          this._paymentProvidersLoaded.set(true);
          return {
            items: this.paymentProviders,
            totalCount: this.paymentProviders.length
          } as LazyResponse<PaymentProviderResponse>;
        })
      );
    }
    return of({
      items: this.paymentProviders,
      totalCount: this.paymentProviders.length
    } as LazyResponse<PaymentProviderResponse>);
  };

  // Manual Discount
  manualDiscountAmount = signal<number>(0);

  // Computed
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

  vatEnabled = signal(false);
  vatPercentage = signal(0);
  vatAmount = computed(() =>
    this.vatEnabled() ? Math.round((this.subtotal() - this.manualDiscountAmount()) * this.vatPercentage()) / 100 : 0
  );

  grandTotal = computed(() => {
    return this.subtotal() - this.manualDiscountAmount() + this.vatAmount();
  });

  availableAdvance = computed(() => {
    const customer = this.selectedCustomer();
    return customer?.advanceAmount || 0;
  });

  // Invoice & Info
  companyName = '';
  invoiceNumber = signal<string>('');
  currentDate = new Date().toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' });

  // Options
  autoCreatePO = false;
  /** Receipt format the sales manager prints on checkout. */
  printType: 'NONE' | 'THERMAL' | 'A4' = 'THERMAL';
  saleNotes = '';
  paymentResponsibility: PaymentResponsibility = 'CUSTOMER';

  // Customer Credit
  customerCreditInfo: { creditLimit: number; usedCredit: number; availableCredit: number; dueBalance: number } | null = null;
  loadingCustomerCredit = false;

  // Dialog States
  showCustomerHistoryDialog = false;
  showCustomerCreditDialog = false;
  showHeldSalesDialog = false;
  showLastSaleDialog = false;
  showReturnsDialog = false;
  showPriceCheckDialog = false;
  showStockSearchDialog = false;
  showBulkDiscountDialog = false;
  heldSales = signal<any[]>([]);
  customerPurchaseHistory = signal<any[]>([]);
  lastSale: any = null;
  returnInvoiceNumber = '';
  returnInvoice: any = null;
  returnRefundType: 'CASH_REFUND' | 'STORE_CREDIT' = 'CASH_REFUND';
  returnLines: { salesOrderLineId: string; partId: string; partName: string; soldQty: number; unitPrice: number; returnQty: number; selected: boolean }[] = [];
  priceCheckCode = '';
  priceCheckResult: any = null;
  priceCheckLoading = false;
  priceCheckNotFound = false;
  bulkDiscountPercent = 0;

  // Reprint Receipt dialog
  showReprintDialog = false;
  reprintInvoiceNumber = '';
  reprintLoading = signal(false);
  reprintError = '';

  // Resend notification (Last Sale dialog)
  resendingNotification = signal(false);

  // Stock Check / product search dialog
  stockSearchTerm = '';
  semanticMode = false;
  stockSearchResults: (PublicPartResponse & { similarityScore?: number })[] = [];
  stockSearchLoading = false;
  stockSearchPage = 1;
  stockSearchPageSize = 10;
  stockSearchTotal = 0;
  stockLevels = new Map<string, number>();

  // Barcode
  barcodeModeActive = false;
  barcodeValue = '';

  // Multi-payment (NEW)
  paymentInputAmount: number | null = null;
  selectedPaymentMethod: 'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE' = 'CASH';
  useCreditBalance = false;
  creditAmountToApply: number = 0;

  paymentMethodOptions = [
    { label: 'Cash', value: 'CASH' as const, icon: 'pi pi-money-bill' },
    { label: 'Card', value: 'CARD' as const, icon: 'pi pi-credit-card' },
    { label: 'Mobile', value: 'MOBILE_BANKING' as const, icon: 'pi pi-mobile' },
    { label: 'Due', value: 'DUE' as const, icon: 'pi pi-clock' }
  ];

  // Payment reference fields
  paymentReference: string = '';
  paymentNotes: string = '';

  totalPaid = computed(() => this.payments().reduce((sum, p) => sum + p.amount, 0));
  totalDueAmount = computed(() => this.payments().filter(p => p.method === 'DUE').reduce((sum, p) => sum + p.amount, 0));
  remainingBalance = computed(() => {
    const creditApplied = this.useCreditBalance ? (this.creditAmountToApply || 0) : 0;
    return Math.max(0, this.grandTotal() - this.totalPaid() - creditApplied);
  });

  readonly Math = Math;

  // ===== LIFECYCLE =====
  ngOnInit(): void {
    this.companyName = this.invoicePdfService.getCompanyConfig().companyName;
    this.initializeForm();
    this.generateInvoiceNumber();
    this.loadUnits();
    this.restoreDraft();
    this.quickSaleService.getVATConfig().subscribe(cfg => {
      this.vatPercentage.set(cfg.percentage);
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // ===== FORM =====
  initializeForm(): void {
    this.quickSaleForm = this.fb.group({
      paymentResponsibility: ['CUSTOMER', [Validators.required]],
      autoCreatePO: [false],
      notes: ['']
    });
  }

  // ===== GENERATE INVOICE =====
  generateInvoiceNumber(): void {
    this.quickSaleService.generateInvoiceNumber().subscribe({
      next: (result) => this.invoiceNumber.set(result.invoiceNumber),
      error: () => this.invoiceNumber.set(`INV-${Date.now()}`)
    });
  }

  // ===== LOAD UNITS =====
  loadUnits(): void {
    this.loadingUnits.set(true);
    this.unitService.getAllUnits().subscribe({
      next: (units: UnitResponse[]) => {
        this.units.set(units);
        this.loadingUnits.set(false);
      },
      error: () => this.loadingUnits.set(false)
    });
  }

  // ===== DRAFT =====
  restoreDraft(): void {
    const draft = this.quickSaleService.loadDraft();
    if (draft && draft.items?.length > 0) {
      this.confirmationService.confirm({
        message: 'Restore previous draft?',
        header: 'Draft Found',
        icon: 'pi pi-info-circle',
        accept: () => {
          this.cartItems.set(draft.items);
          this.payments.set(draft.payments || []);
          this.saleNotes = draft.notes || '';
          this.quickSaleService.clearDraft();
        }
      });
    }
  }

  // ===== PRODUCT SELECTION =====
  selectPart(event: any): void {
    const part = event as PublicPartResponse;
    const existing = this.cartItems().find(
      item => item.partId === part.id && (item.productVariantId ?? null) === (part.variantId ?? null)
    );
    if (existing) {
      this.messageService.add({ severity: 'info', summary: 'Already Added', detail: 'This item is already in the cart' });
      return;
    }

    if (part.unitId) {
      this.unitService.getCompatibleUnits(part.unitId).subscribe({
        next: (compatibleUnits) => this.compatibleUnitsMap.set(part.id, compatibleUnits),
        error: () => this.compatibleUnitsMap.set(part.id, this.units())
      });
    }

    const newItem: QuickSaleLineItem = {
      partId: part.id,
      productVariantId: part.variantId ?? undefined,
      partName: part.displayName || part.name,
      partNumber: part.partNumber,
      sku: part.variantSKU || part.sku,
      unitId: part.unitId || undefined,
      quantity: 1,
      unitPrice: part.effectiveSellingPrice ?? part.sellingPrice,
      discount: 0
    };

    this.cartItems.update(items => [...items, newItem]);
    if (part.unitId) {
      this.cartUnitSelection.set(this.cartItems().length - 1, part.unitId);
    }
    this.selectedPartModel = null;

    this.messageService.add({ severity: 'success', summary: 'Part Added', detail: `${part.displayName || part.name} added to cart` });
  }

  // ===== CART ACTIONS =====
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

  removeFromCart(index: number): void {
    this.cartItems.update(items => items.filter((_, i) => i !== index));
    this.pricingErrors.clear();
  }

  clearPricingError(index: number): void {
    this.pricingErrors.delete(index);
  }

  calculateLineTotal(item: QuickSaleLineItem): number {
    const lineTotal = item.quantity * item.unitPrice;
    const discountAmount = (lineTotal * item.discount) / 100;
    return lineTotal - discountAmount;
  }

  onCartUnitChanged(item: QuickSaleLineItem, index: number): void {
    const previousUnitId = this.cartUnitSelection.get(index);
    const nextUnitId = item.unitId;
    if (!previousUnitId || !nextUnitId || previousUnitId === nextUnitId) return;

    const currentPrice = Number(item.unitPrice || 0);
    this.unitConversionService.getConversion(nextUnitId, previousUnitId).subscribe({
      next: (res) => {
        const newPrice = currentPrice * res.conversionFactor;
        this.cartItems.update(items => {
          const newItems = [...items];
          newItems[index] = { ...newItems[index], unitPrice: Math.round(newPrice * 100) / 100 };
          return newItems;
        });
        this.cartUnitSelection.set(index, nextUnitId);
      }
    });
  }

  // ===== CUSTOMER & TECHNICIAN =====
  selectCustomer(event: any): void {
    if (event?.id) {
      this.customerService.getCustomerById(event.id).subscribe({
        next: (freshCustomer) => {
          this.selectedCustomer.set(freshCustomer);
          this.selectedCustomerModel = freshCustomer;
        },
        error: () => {
          this.selectedCustomer.set(event);
          this.selectedCustomerModel = event;
        }
      });
      this.loadCustomerVehicles(event.id);
    } else {
      this.selectedCustomer.set(event);
      this.selectedCustomerModel = event;
      this.clearVehicleSelection();
    }
  }

  private loadCustomerVehicles(customerId: string): void {
    this.clearVehicleSelection();
    this.loadingVehicles.set(true);
    this.vehicleService.getByCustomer(customerId, true).subscribe({
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

  private clearVehicleSelection(): void {
    this.customerVehicles.set([]);
    this.selectedVehicleId.set(null);
  }

  selectTechnician(event: any): void {
    this.selectedTechnician.set(event);
    this.selectedTechnicianModel = event;
  }

  openQuickCustomerDialog(): void {
    this.quickCustomerDialog.open();
  }

  onCustomerCreated(customer: any): void {
    this.selectedCustomer.set(customer);
    this.selectedCustomerModel = customer;
    this.clearVehicleSelection();
    this.messageService.add({ severity: 'success', summary: 'Customer Created', detail: `${customer.fullName} added` });
  }

  // ===== FORMAT CURRENCY =====
  formatCurrency(amount: number): string {
    return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
  }

  // ===== BARCODE =====
  toggleBarcodeMode(): void {
    this.barcodeModeActive = !this.barcodeModeActive;
  }

  setSearchMode(): void {
    this.barcodeModeActive = false;
  }

  processBarcodeInput(): void {
    if (!this.barcodeValue.trim()) return;
    const code = this.barcodeValue.trim();
    this.quickSaleService.getPriceByCode(code).subscribe({
      next: (result) => {
        if (result) {
          const variantId = result.variantId ?? undefined;
          const existing = this.cartItems().find(
            item => item.partId === result.partId && (item.productVariantId ?? null) === (result.variantId ?? null)
          );
          const displayName = composeVariantDisplayName(result.name, result.variantName);
          if (existing) {
            this.cartItems.update(items =>
              items.map(item =>
                item.partId === result.partId && (item.productVariantId ?? null) === (result.variantId ?? null)
                  ? { ...item, quantity: item.quantity + 1 }
                  : item)
            );
            this.messageService.add({ severity: 'info', summary: 'Qty Updated', detail: `${displayName} quantity increased` });
          } else {
            const newItem: QuickSaleLineItem = {
              partId: result.partId,
              productVariantId: variantId,
              partName: displayName,
              partNumber: result.partNumber,
              sku: result.variantCode ?? result.sku,
              unitId: result.unitId || undefined,
              quantity: 1,
              unitPrice: result.sellingPrice,
              discount: 0
            };
            this.cartItems.update(items => [...items, newItem]);
            if (result.unitId) {
              this.cartUnitSelection.set(this.cartItems().length - 1, result.unitId);
            }
            this.messageService.add({ severity: 'success', summary: 'Added', detail: `${displayName} added` });
          }
        }
        this.barcodeValue = '';
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Not Found', detail: `Code: ${code} not found` });
        this.barcodeValue = '';
      }
    });
  }

  // ===== PAYMENT METHODS =====
  selectPaymentMethod(method: 'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE'): void {
    this.selectedPaymentMethod = method;
  }

  addNewPayment(): void {
    const amount = this.paymentInputAmount || 0;
    if (amount <= 0) return;

    const payment: PaymentDetail = {
      method: this.selectedPaymentMethod,
      amount,
      reference: this.paymentReference.trim() || undefined,
      notes: this.paymentNotes.trim() || undefined
    };

    this.payments.update(payments => [...payments, payment]);
    this.paymentInputAmount = null;
    this.paymentReference = '';
    this.paymentNotes = '';
  }

  onPaymentMethodChange(): void {
    // Auto-fill remaining balance for DUE
    if (this.selectedPaymentMethod === 'DUE') {
      this.paymentInputAmount = this.remainingBalance();
    }
  }

  requiresReference(method: string): boolean {
    return method === 'CARD' || method === 'MOBILE_BANKING';
  }

  hasDuePayments(): boolean {
    return this.payments().some(p => p.method === 'DUE');
  }

  sumDuePayments = (sum: number, p: PaymentDetail): number => {
    return sum + p.amount;
  };

  removeNewPayment(index: number): void {
    this.payments.update(payments => payments.filter((_, i) => i !== index));
  }

  getPaymentIcon(method: string): string {
    const icons: Record<string, string> = {
      'CASH': 'pi pi-money-bill',
      'CARD': 'pi pi-credit-card',
      'MOBILE_BANKING': 'pi pi-mobile',
      'DUE': 'pi pi-clock'
    };
    return icons[method] || 'pi pi-wallet';
  }

  getPaymentLabel(method: string): string {
    const labels: Record<string, string> = {
      'CASH': 'Cash',
      'CARD': 'Card',
      'MOBILE_BANKING': 'Mobile',
      'DUE': 'Due'
    };
    return labels[method] || method;
  }

  // ===== SHORTCUT ACTIONS =====
  resetShortcut(): void {
    this.resetForm();
    this.generateInvoiceNumber();
  }

  resetForm(): void {
    this.cartItems.set([]);
    this.payments.set([]);
    this.selectedCustomer.set(null);
    this.selectedCustomerModel = null;
    this.clearVehicleSelection();
    this.selectedTechnician.set(null);
    this.selectedTechnicianModel = null;
    this.selectedPartModel = null;
    this.manualDiscountAmount.set(0);
    this.currentInvoiceId.set(null);
    this.saving.set(false);
    this.autoCreatePO = false;
    this.saleNotes = '';
    this.printType = 'THERMAL';
    this.useCreditBalance = false;
    this.creditAmountToApply = 0;
    this.paymentInputAmount = null;
    this.paymentReference = '';
    this.paymentNotes = '';
    this.pricingErrors.clear();
    this.cartUnitSelection.clear();
    this.quickSaleService.clearDraft();
  }

  saveDraft(): void {
    this.quickSaleService.saveDraft({
      customerId: this.selectedCustomer()?.id,
      customerName: this.selectedCustomer()?.fullName,
      items: this.cartItems(),
      payments: this.payments(),
      technicianId: this.selectedTechnician()?.id,
      notes: this.saleNotes
    });
    this.messageService.add({ severity: 'success', summary: 'Draft Saved' });
  }

  holdSale(): void {
    if (this.cartItems().length === 0) {
      this.messageService.add({ severity: 'warn', summary: 'No Items', detail: 'Add items before holding' });
      return;
    }
    const holdId = this.quickSaleService.holdSale({
      customerId: this.selectedCustomer()?.id,
      customerName: this.selectedCustomer()?.fullName,
      items: this.cartItems(),
      payments: this.payments(),
      technicianId: this.selectedTechnician()?.id,
      notes: this.saleNotes
    });
    this.messageService.add({ severity: 'success', summary: 'Sale Held', detail: `ID: ${holdId}` });
    this.resetForm();
  }

  recallHeldSales(): void {
    this.heldSales.set(this.quickSaleService.getHeldSales());
    this.showHeldSalesDialog = true;
  }

  recallHeldSale(holdId: string): void {
    const sale = this.quickSaleService.recallHeldSale(holdId);
    if (sale) {
      this.resetForm();
      this.cartItems.set(sale.items);
      this.payments.set(sale.payments);
      this.saleNotes = sale.notes || '';
      this.showHeldSalesDialog = false;
      this.messageService.add({ severity: 'success', summary: 'Sale Recalled' });
    }
  }

  deleteHeldSale(holdId: string): void {
    this.quickSaleService.removeHeldSale(holdId);
    this.heldSales.set(this.quickSaleService.getHeldSales());
  }

  viewLastSale(): void {
    const sale = this.quickSaleService.getLastSale();
    if (sale) {
      this.lastSale = sale;
      this.showLastSaleDialog = true;
    }
  }

  hasLastSale(): boolean {
    return !!this.quickSaleService.getLastSale();
  }

  /** Secondary checkout actions, shown in the "More" overflow to keep one clear primary action. */
  get moreActions(): MenuItem[] {
    return [
      { label: 'Save Draft', icon: 'pi pi-save', disabled: this.cartItems().length === 0, command: () => this.saveDraft() },
      { label: 'Hold Sale', icon: 'pi pi-pause', disabled: this.cartItems().length === 0, command: () => this.holdSale() },
      { label: 'Recall Held Sale', icon: 'pi pi-replay', command: () => this.recallHeldSales() },
      { separator: true },
      { label: 'Save as Quotation', icon: 'pi pi-file', disabled: this.cartItems().length === 0 || this.saving(), command: () => this.saveAsQuotation() },
      { label: 'Reprint Receipt', icon: 'pi pi-print', command: () => this.openReprintDialog() },
      { separator: true },
      { label: 'Returns', icon: 'pi pi-refresh', command: () => this.openReturns() },
      { label: 'Stock Check', icon: 'pi pi-box', command: () => this.openStockSearch() },
      { label: 'Price Check', icon: 'pi pi-tag', command: () => this.openPriceCheck() },
    ];
  }

  printLastSaleReceipt(): void {
    this.showLastSaleDialog = false;
    if (this.invoicePreviewData) {
      this.thermalReceipt.print(this.invoicePreviewData, (n) => this.formatCurrency(n));
    } else {
      this.messageService.add({ severity: 'warn', summary: 'No Receipt', detail: 'No recent sale available to print.' });
    }
  }

  resendNotification(): void {
    const salesOrderId = this.lastSale?.salesOrderId;
    if (!salesOrderId || this.resendingNotification()) return;
    this.resendingNotification.set(true);
    this.quickSaleService.resendInvoiceNotification(salesOrderId).subscribe({
      next: () => {
        this.resendingNotification.set(false);
        this.messageService.add({ severity: 'success', summary: 'Sent', detail: 'Notification resent to customer.' });
      },
      error: () => {
        this.resendingNotification.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed', detail: 'Could not resend notification.' });
      }
    });
  }

  saveAsQuotation(): void {
    if (this.cartItems().length === 0 || this.saving()) return;
    this.saving.set(true);
    const customer = this.selectedCustomer();
    const request = {
      customerId: customer?.id,
      customerName: customer?.fullName || 'Walk-in Customer',
      customerPhone: customer?.phone || '',
      technicianId: this.selectedTechnician()?.id,
      customerVehicleId: this.selectedVehicleId() || null,
      paymentResponsibility: this.paymentResponsibility,
      autoCreatePO: false,
      items: this.cartItems(),
      payments: [],
      subtotal: this.subtotal(),
      discountAmount: this.discountAmount(),
      vatAmount: this.vatAmount(),
      vatPercentage: this.vatPercentage(),
      grandTotal: this.grandTotal(),
      paidAmount: 0,
      dueAmount: 0,
      notes: this.saleNotes
    };
    this.quickSaleService.generateQuote(request).subscribe({
      next: (result) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'success', summary: 'Quotation Saved', detail: result.quoteNumber });
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Failed', detail: err.error?.message || 'Could not save quotation.' });
      }
    });
  }

  openReprintDialog(): void {
    this.reprintInvoiceNumber = '';
    this.reprintError = '';
    this.showReprintDialog = true;
  }

  reprintReceipt(): void {
    const num = this.reprintInvoiceNumber.trim();
    if (!num || this.reprintLoading()) return;
    this.reprintError = '';
    this.reprintLoading.set(true);
    this.invoicePdfService.getInvoiceByNumber(num).subscribe({
      next: (invoice) => {
        this.reprintLoading.set(false);
        this.showReprintDialog = false;
        this.invoicePdfService.downloadServerPdf(invoice.id, invoice.invoiceNumber).subscribe({
          error: () => this.messageService.add({ severity: 'error', summary: 'Download Failed', detail: 'Could not download PDF.' })
        });
      },
      error: () => {
        this.reprintLoading.set(false);
        this.reprintError = 'Invoice not found. Check the number and try again.';
      }
    });
  }

  openReturns(): void {
    this.showReturnsDialog = true;
    this.returnInvoiceNumber = '';
    this.returnInvoice = null;
    this.returnRefundType = 'CASH_REFUND';
    this.returnLines = [];
  }

  lookupReturnInvoice(): void {
    if (!this.returnInvoiceNumber.trim()) return;
    this.quickSaleService.lookupInvoice(this.returnInvoiceNumber.trim()).subscribe({
      next: (invoice) => {
        this.returnInvoice = invoice;
        // Default: every sold line selected, full quantity — cashier trims as needed.
        this.returnLines = (invoice?.lines ?? []).map(l => ({
          salesOrderLineId: l.salesOrderLineId,
          partId: l.partId,
          partName: l.variantName ? `${l.partName} - ${l.variantName}` : l.partName,
          soldQty: l.quantity,
          unitPrice: l.unitPrice,
          returnQty: l.quantity,
          selected: true
        }));
      },
      error: () => this.messageService.add({ severity: 'error', summary: 'Not Found' })
    });
  }

  /** Running total of the selected return lines — shown in the dialog footer. */
  get returnRefundTotal(): number {
    return this.returnLines
      .filter(l => l.selected)
      .reduce((sum, l) => sum + (l.unitPrice * (l.returnQty || 0)), 0);
  }

  processReturn(): void {
    if (!this.returnInvoice) return;

    const chosen = this.returnLines.filter(l => l.selected && l.returnQty > 0);

    if (chosen.length === 0) {
      this.messageService.add({ severity: 'warn', summary: 'Nothing to Return', detail: 'Select at least one item with a quantity greater than zero.' });
      return;
    }

    const invalid = chosen.find(l => l.returnQty > l.soldQty);
    if (invalid) {
      this.messageService.add({ severity: 'warn', summary: 'Invalid Quantity', detail: `Cannot return more than ${invalid.soldQty} of "${invalid.partName}".` });
      return;
    }

    const items = chosen.map(l => ({
      partId: l.partId,
      salesOrderLineId: l.salesOrderLineId,
      quantity: l.returnQty,
      reason: 'POS quick return'
    }));

    const refundLabel = this.returnRefundType === 'STORE_CREDIT' ? 'store credit' : 'cash refund';
    this.confirmationService.confirm({
      message: `Create a return for ${chosen.length} item(s) (${this.formatCurrency(this.returnRefundTotal)}) on invoice ${this.returnInvoice.invoiceNumber} as ${refundLabel}?`,
      header: 'Confirm Return',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.quickSaleService.processReturn({
          originalInvoiceNumber: this.returnInvoice.invoiceNumber,
          refundType: this.returnRefundType,
          items
        }).subscribe({
          next: (res: any) => {
            this.showReturnsDialog = false;
            this.messageService.add({
              severity: 'success',
              summary: 'Return Created',
              detail: `${res?.returnNumber ?? 'Return'} created (PENDING). Approve & receive it on the Sales Returns screen.`
            });
          },
          error: (err) => this.messageService.add({
            severity: 'error',
            summary: 'Return Failed',
            detail: extractApiError(err, 'Could not create the return')
          })
        });
      }
    });
  }

  openCustomerHistory(): void {
    if (!this.selectedCustomer()) {
      this.messageService.add({ severity: 'warn', summary: 'Select Customer First' });
      return;
    }
    this.showCustomerHistoryDialog = true;
    this.quickSaleService.getCustomerHistory(this.selectedCustomer()!.id, 10).subscribe({
      next: (history) => this.customerPurchaseHistory.set(history)
    });
  }

  viewCustomerCredit(): void {
    if (!this.selectedCustomer()) return;
    this.showCustomerCreditDialog = true;
    this.loadingCustomerCredit = true;
    this.quickSaleService.getCustomerCredit(this.selectedCustomer()!.id).subscribe({
      next: (credit) => { this.customerCreditInfo = credit; this.loadingCustomerCredit = false; },
      error: () => {
        const c = this.selectedCustomer()!;
        this.customerCreditInfo = {
          creditLimit: c.creditLimit || 0,
          usedCredit: c.dueBalance || 0,
          availableCredit: (c.creditLimit || 0) - (c.dueBalance || 0),
          dueBalance: c.dueBalance || 0
        };
        this.loadingCustomerCredit = false;
      }
    });
  }

  openPriceCheck(): void {
    this.showPriceCheckDialog = true;
    this.priceCheckCode = '';
    this.priceCheckResult = null;
    this.priceCheckNotFound = false;
    this.priceCheckLoading = false;
  }

  searchPrice(): void {
    const code = this.priceCheckCode.trim();
    if (!code) return;
    this.priceCheckLoading = true;
    this.priceCheckNotFound = false;
    this.quickSaleService.getPriceByCode(code).subscribe({
      next: (result) => {
        this.priceCheckLoading = false;
        this.priceCheckResult = result;
        if (!result) {
          this.priceCheckNotFound = true;
          this.messageService.add({ severity: 'warn', summary: 'Not Found', detail: `No product found for "${code}"` });
        }
      },
      error: () => {
        this.priceCheckLoading = false;
        this.priceCheckResult = null;
        this.priceCheckNotFound = true;
        this.messageService.add({ severity: 'error', summary: 'Not Found', detail: `No product found for "${code}"` });
      }
    });
  }

  addPriceCheckToCart(): void {
    const r = this.priceCheckResult;
    if (!r) return;
    // Reuse selectPart so variant id/name, dedupe and lot-price logic stay in one place.
    const variantSuffix = r.variantName ? ` - ${r.variantName}` : (r.variantCode ? ` - ${r.variantCode}` : '');
    this.selectPart({
      id: r.partId,
      variantId: r.variantId ?? null,
      name: r.name,
      displayName: r.name + variantSuffix,
      partNumber: r.partNumber,
      sku: r.sku,
      variantSKU: null,
      unitId: r.unitId,
      sellingPrice: r.sellingPrice,
      effectiveSellingPrice: r.sellingPrice
    });
    this.showPriceCheckDialog = false;
  }

  openStockSearch(): void {
    this.showStockSearchDialog = true;
    this.stockSearchTerm = '';
    this.semanticMode = false;
    this.stockSearchResults = [];
    this.stockLevels.clear();
    this.runStockSearch(1);
  }

  onSemanticToggle(): void {
    this.runStockSearch(1);
  }

  runStockSearch(page: number): void {
    this.stockSearchPage = page;
    this.stockSearchLoading = true;
    const term = this.stockSearchTerm.trim();

    const source = (this.semanticMode && term)
      ? this.partService.searchSemantic(term, page, this.stockSearchPageSize, true)
      : this.partService.getParts({ search: term, pageNumber: page, pageSize: this.stockSearchPageSize, isActive: true, flattenVariants: true });

    source.subscribe({
      next: (res) => {
        this.stockSearchResults = res.data;
        this.stockSearchTotal = res.pagination.totalCount;
        this.stockSearchLoading = false;
        this.loadStockLevels(res.data.map(r => r.id));
      },
      error: () => {
        this.stockSearchLoading = false;
        this.messageService.add({ severity: 'error', summary: 'Search failed' });
      }
    });
  }

  private loadStockLevels(partIds: string[]): void {
    const ids = Array.from(new Set(partIds));
    if (ids.length === 0) return;
    this.quickSaleService.checkMultipleStock(ids.map(partId => ({ partId, quantity: 1 }))).subscribe({
      next: (results) => results.forEach(r => this.stockLevels.set(r.partId, r.stockAvailable)),
      error: () => {}
    });
  }

  stockFor(partId: string): number | null {
    return this.stockLevels.has(partId) ? this.stockLevels.get(partId)! : null;
  }

  get stockSearchTotalPages(): number {
    return Math.max(1, Math.ceil(this.stockSearchTotal / this.stockSearchPageSize));
  }

  addStockRow(row: PublicPartResponse): void {
    this.selectPart(row);
  }

  searchStock(): void {
    this.runStockSearch(1);
  }

  openBulkDiscount(): void {
    this.showBulkDiscountDialog = true;
    this.bulkDiscountPercent = 0;
  }

  applyBulkDiscountConfirm(): void {
    const value = Math.max(0, Math.min(100, this.bulkDiscountPercent));
    this.cartItems.update(items => items.map(item => ({ ...item, discount: value })));
    this.showBulkDiscountDialog = false;
    this.messageService.add({ severity: 'success', summary: 'Discount Applied', detail: `${value}% applied` });
  }

  clearCart(): void {
    this.confirmationService.confirm({
      message: 'Clear all cart items?',
      header: 'Clear Cart',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.cartItems.set([]);
        this.payments.set([]);
        this.pricingErrors.clear();
      }
    });
  }

  quickCashPayment(): void {
    this.selectedPaymentMethod = 'CASH';
    this.paymentInputAmount = this.remainingBalance() > 0 ? this.remainingBalance() : this.grandTotal();
    this.paymentReference = '';
    this.paymentNotes = '';
    this.addNewPayment();
  }

  quickCardPayment(): void {
    this.selectedPaymentMethod = 'CARD';
    this.paymentInputAmount = this.remainingBalance() > 0 ? this.remainingBalance() : this.grandTotal();
    // Don't auto-add — user needs to enter transaction number
    this.paymentReference = '';
    this.paymentNotes = '';
  }

  quickMobilePayment(): void {
    this.selectedPaymentMethod = 'MOBILE_BANKING';
    this.paymentInputAmount = this.remainingBalance() > 0 ? this.remainingBalance() : this.grandTotal();
    // Don't auto-add — user needs to enter transaction number
    this.paymentReference = '';
    this.paymentNotes = '';
  }

  saveAndPrint(): void {
    this.printType = 'THERMAL';
    this.confirmCheckout();
  }

  // ===== CHECKOUT =====
  openCheckout(): void {
    if (this.cartItems().length === 0) {
      this.messageService.add({ severity: 'warn', summary: 'No Items', detail: 'Add items before checkout' });
      return;
    }
    this.paymentInputAmount = this.remainingBalance();
  }

  confirmCheckout(): void {
    if (!this.selectedCustomer()) {
      this.messageService.add({ severity: 'error', summary: 'Customer Required' });
      return;
    }
    if (this.cartItems().length === 0) {
      this.messageService.add({ severity: 'error', summary: 'No Items' });
      return;
    }

    const creditApplied = this.useCreditBalance ? (this.creditAmountToApply || 0) : 0;
    const totalPaid = this.payments().reduce((sum, p) => sum + p.amount, 0) + creditApplied;
    const remaining = this.grandTotal() - totalPaid;
    const hasDuePayment = this.payments().some(p => p.method === 'DUE');

    if (remaining > 0.01 && !hasDuePayment) {
      this.messageService.add({ severity: 'warn', summary: 'Incomplete Payment', detail: `Remaining: ${this.formatCurrency(remaining)}. Add a payment or select "Due" for credit sale.` });
      return;
    }

    // Require customer for due payments
    if (hasDuePayment && !this.selectedCustomer()?.id) {
      this.messageService.add({ severity: 'error', summary: 'Customer Required', detail: 'A registered customer is required for due/credit sales.' });
      return;
    }

    if (creditApplied > 0) {
      const existingNotes = this.saleNotes;
      this.saleNotes = existingNotes ? `${existingNotes} | Credit: ${this.formatCurrency(creditApplied)}` : `Credit: ${this.formatCurrency(creditApplied)}`;
    }

    this.onSubmit();
  }

  // ===== SUBMIT SALE =====
  onSubmit(): void {
    if (this.cartItems().length === 0 || !this.selectedCustomer()) return;

    this.saving.set(true);
    const customer = this.selectedCustomer()!;

    const request = {
      customerId: customer.id,
      customerName: customer.fullName,
      customerPhone: customer.phone || '',
      technicianId: this.selectedTechnician()?.id,
      customerVehicleId: this.selectedVehicleId() || null,
      paymentResponsibility: this.paymentResponsibility,
      autoCreatePO: this.autoCreatePO,
      items: this.cartItems(),
      payments: this.payments(),
      subtotal: this.subtotal(),
      discountAmount: this.discountAmount(),
      vatAmount: this.vatAmount(),
      vatPercentage: this.vatPercentage(),
      grandTotal: this.grandTotal(),
      paidAmount: this.payments().filter(p => p.method !== 'DUE').reduce((sum, p) => sum + p.amount, 0),
      dueAmount: this.totalDueAmount(),
      notes: this.saleNotes,
      useAdvanceBalance: false,
      advanceAmountToApply: 0,
      saveAsQuotation: false
    };

    this.quickSaleService.createQuickSale(request).subscribe({
      next: (result) => {
        this.quickSaleService.saveLastSale(result);
        this.messageService.add({ severity: 'success', summary: 'Sale Completed', detail: result.invoiceNumber });

        // Capture the receipt + chosen format BEFORE resetForm() (which restores printType to default).
        this.invoicePreviewData = this.buildReceiptData(result, request);
        this.currentInvoiceId.set(result.id);
        const receipt = this.invoicePreviewData;
        const printMode = this.printType;

        this.saving.set(false);
        this.resetForm();
        this.generateInvoiceNumber();

        // Print in the format the sales manager selected.
        if (receipt) {
          if (printMode === 'THERMAL') {
            // Auto-print the compact 80mm thermal receipt.
            this.thermalReceipt.print(receipt, (n) => this.formatCurrency(n));
          } else if (printMode === 'A4') {
            // Open the A4 invoice preview so the manager can review, then print or download.
            this.showInvoicePreview = true;
          }
        }
      },
      error: (err) => {
        this.saving.set(false);
        this.messageService.add({ severity: 'error', summary: 'Sale Failed', detail: err.error?.message || 'Failed' });
      }
    });
  }

  /** Assemble InvoicePdfData (used by the thermal receipt) from a completed sale. */
  private buildReceiptData(result: any, request: any): InvoicePdfData {
    const c = this.invoicePdfService.getCompanyConfig();
    return {
      companyName: c.companyName,
      companyAddress: c.companyAddress,
      companyPhone: c.companyPhone,
      companyEmail: c.companyEmail,
      companyTaxId: c.companyTaxId,
      invoiceNumber: result.invoiceNumber,
      invoiceDate: new Date(),
      salesOrderNumber: result.salesOrderNumber,
      customerName: request.customerName,
      customerPhone: request.customerPhone,
      technicianName: this.selectedTechnician()?.name,
      items: request.items.map((item: any, i: number) => ({
        slNo: i + 1,
        partNumber: item.partNumber || item.sku || '',
        description: item.partName || '',
        quantity: item.quantity,
        unitPrice: item.unitPrice,
        discount: item.discount || 0,
        total: (item.quantity * item.unitPrice) - (item.discount || 0)
      })),
      subtotal: request.subtotal,
      discountAmount: request.discountAmount,
      vatPercentage: request.vatPercentage,
      vatAmount: request.vatAmount,
      grandTotal: request.grandTotal,
      payments: (request.payments || []).map((p: any) => ({ method: p.method, amount: p.amount, reference: p.reference })),
      paidAmount: request.paidAmount,
      dueAmount: request.dueAmount,
      notes: request.notes,
      paymentTerms: 'Thank you for your business!'
    };
  }

}
