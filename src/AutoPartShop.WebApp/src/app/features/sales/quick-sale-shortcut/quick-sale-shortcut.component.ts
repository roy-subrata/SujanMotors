import { Component, OnInit, OnDestroy, HostListener, inject, signal, computed, ViewChild, ViewEncapsulation } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { finalize } from 'rxjs/operators';

// PrimeNG Imports
import { ToastModule } from 'primeng/toast';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { MessageService, ConfirmationService } from 'primeng/api';

// Services
import { QuickSaleService, QuickSaleLineItem, QuickSaleDraft, PaymentDetail, PaymentMethod, PaymentResponsibility, CustomerOrderHistoryItem } from '../services/quick-sale.service';
import { PublicPartService, PublicPartResponse } from '../services/public-part.service';
import { DiscountService, ResolveDiscountResult } from '../../inventory/services/discount.service';
import { UnitService, UnitResponse } from '../../inventory/services/unit.service';
import { UnitConversionService } from '../../inventory/services/unit-conversion.service';
import { CustomerService } from '../services/customer.service';
import { CustomerVehicleService, CustomerVehicleResponse } from '../services/customer-vehicle.service';
import { TechnicianService, TechnicianResponse } from '../services/technician.service';
import { TillSessionService } from '../services/till-session.service';
import { InvoicePdfService, InvoicePdfData } from '../services/invoice-pdf.service';
import { ThermalReceiptService } from '../services/thermal-receipt.service';
import { CurrencyService } from '../../../shared/services/currency.service';
import { PricingValidationService } from '../../../shared/services/pricing-validation.service';
import { extractApiError } from '../../../shared/utils/api-error.util';
import { composeVariantDisplayName } from '../../../shared/utils/variant-name.util';
import { LayoutService } from '../../../layout/service/layout.service';
import { AuthService } from '../../../shared/services/auth.service';

// Components
import { QuickCustomerDialogComponent } from '../components/quick-customer-dialog.component';
import { InvoicePreviewComponent } from '../components/invoice-preview.component';
import { I18nService } from '@/shared/services/i18n.service';
import { TranslatePipe } from '@/shared/pipes/translate.pipe';

// POS presentational children (design_handoff_pos_sale)
import { PosHeaderComponent } from './pos/pos-header.component';
import { PosCatalogComponent, PosCatalogChip, PosCatalogTile } from './pos/pos-catalog.component';
import { PosCartPanelComponent, PosCartLine } from './pos/pos-cart-panel.component';
import { PosShortcutBarComponent, PosShortcutKey, PosExtraAction } from './pos/pos-shortcut-bar.component';
import { PosListDialogComponent } from './pos/pos-list-dialog.component';
import { PosTenderDialogComponent, PosTenderRow, PosPayType, PosQuickCash } from './pos/pos-tender-dialog.component';
import { PosReceiptDialogComponent, PosReceiptRow } from './pos/pos-receipt-dialog.component';
import { PosPrintPreviewComponent } from './pos/pos-print-preview.component';

/** Single active overlay — mirrors the design's own `overlay` state machine (README §"State Management"). */
export type PosOverlay =
    | 'search'
    | 'stock'
    | 'customer'
    | 'discount'
    | 'recall'
    | 'reprint'
    | 'returns'
    | 'priceCheck'
    | 'customerHistory'
    | 'customerCredit'
    | 'lastSale'
    | 'vehicle'
    | 'technician'
    | 'options'
    | 'tender'
    | 'receipt'
    | 'thermal';

@Component({
    selector: 'app-quick-sale-shortcut',
    standalone: true,
    imports: [
        CommonModule,
        ReactiveFormsModule,
        FormsModule,
        ToastModule,
        ConfirmDialogModule,
        RouterLink,
        QuickCustomerDialogComponent,
        InvoicePreviewComponent,
        TranslatePipe,
        PosHeaderComponent,
        PosCatalogComponent,
        PosCartPanelComponent,
        PosShortcutBarComponent,
        PosListDialogComponent,
        PosTenderDialogComponent,
        PosReceiptDialogComponent,
        PosPrintPreviewComponent
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
    private readonly discountService = inject(DiscountService);
    private readonly unitService = inject(UnitService);
    private readonly unitConversionService = inject(UnitConversionService);
    private readonly customerService = inject(CustomerService);
    private readonly vehicleService = inject(CustomerVehicleService);
    private readonly technicianService = inject(TechnicianService);
    private readonly tillSessionService = inject(TillSessionService);
    private readonly currencyService = inject(CurrencyService);
    private readonly messageService = inject(MessageService);
    private readonly confirmationService = inject(ConfirmationService);
    private readonly i18n = inject(I18nService);
    private readonly invoicePdfService = inject(InvoicePdfService);
    private readonly thermalReceipt = inject(ThermalReceiptService);
    private readonly pricingValidationService = inject(PricingValidationService);
    private readonly authService = inject(AuthService);
    private readonly sanitizer = inject(DomSanitizer);
    readonly layoutService = inject(LayoutService);
    readonly isDarkMode = computed(() => !!this.layoutService.isDarkTheme());

    toggleDarkMode(): void {
        this.layoutService.layoutConfig.update((state) => ({ ...state, darkTheme: !state.darkTheme }));
    }

    @ViewChild(QuickCustomerDialogComponent) quickCustomerDialog!: QuickCustomerDialogComponent;

    // ===== STATE =====
    quickSaleForm!: FormGroup;
    saving = signal(false);
    loading = signal(false);
    private destroy$ = new Subject<void>();

    // ===== POS SHELL STATE (design_handoff_pos_sale) =====
    /** Single active overlay — null means the sale screen itself. */
    activeOverlay = signal<PosOverlay | null>(null);
    isOverlayOpen = computed(() => this.activeOverlay() !== null);
    dialogQuery = signal('');
    /** Raw keypad digit string for the tender dialog, cents-first (README §3). */
    keypadDigits = signal('');
    /** Viewport-driven layout booleans (README "The layout is fluid" table). */
    narrow = signal(typeof window !== 'undefined' && window.innerWidth < 780);
    short = signal(typeof window !== 'undefined' && window.innerHeight < 660);

    // Catalog (paginated, lazy-loaded on scroll — chip filtering stays client-side over whatever
    // pages have loaded so far, since the parts API has no server-side category filter)
    private static readonly CATALOG_PAGE_SIZE = 60;
    catalogParts = signal<PublicPartResponse[]>([]);
    catalogLoading = signal(false);
    catalogLoadingMore = signal(false);
    catalogChip = signal<string | null>(null);
    private catalogPageNumber = 1;
    private catalogHasMore = true;
    private catalogStock = new Map<string, number>();

    // Product search list-dialog (F2 — backend search, debounced; independent of the lazily-paged
    // catalog grid cache so it can find any part regardless of scroll position)
    searchDialogResults = signal<PublicPartResponse[]>([]);
    searchDialogLoading = signal(false);
    private searchDialogDebounce: ReturnType<typeof setTimeout> | undefined;

    // Customer list-dialog (backend search, debounced — too many customers to bulk-load)
    customerDialogResults = signal<any[]>([]);
    customerDialogLoading = signal(false);
    private customerDialogDebounce: ReturnType<typeof setTimeout> | undefined;

    // Technician list-dialog (backend search, debounced)
    technicianDialogResults = signal<TechnicianResponse[]>([]);
    technicianDialogLoading = signal(false);
    private technicianDialogDebounce: ReturnType<typeof setTimeout> | undefined;

    // Discount dialog (F6) — manual preset % currently applied, so a second tap removes it
    // (mirrors the design's "selecting the active one removes it" rule for its DISCOUNTS rows).
    appliedManualDiscountPercent = signal(0);

    // Receipt overlay — populated from the last completed sale (onSubmit() success payload).
    lastReceiptData: InvoicePdfData | null = null;
    thermalPreviewHtml = signal<SafeHtml | null>(null);
    receiptReference = signal('');
    receiptRows = signal<PosReceiptRow[]>([]);

    // Till session gate (opt-in via Permissions.SalesRequireTillSession) — see
    // TillSessionController.RequiresOpenSession. Blocks the whole cart/checkout UI until the
    // cashier opens a till session, for roles the gate applies to; a no-op for everyone else.
    checkingTillSession = signal(true);
    tillSessionBlocked = signal(false);

    // Invoice Preview
    showInvoicePreview = false;
    invoicePreviewData: InvoicePdfData | null = null;
    currentInvoiceId = signal<string | null>(null);

    // Price-override approval — the industry-standard in-transaction manager approval, not a
    // role-based bypass. Opened when the server rejects a line for being below cost or above MRP
    // (error code PRICE_OVERRIDE_REQUIRED); on success the returned token is attached and the same
    // sale is resubmitted automatically.
    showPriceOverrideDialog = false;
    priceOverrideUsername = '';
    priceOverridePassword = '';
    priceOverrideError = '';
    priceOverrideSubmitting = false;
    private priceOverrideApprovalToken: string | null = null;

    // Customers
    selectedCustomer = signal<any | null>(null);
    selectedCustomerModel: any | null = null;

    // Optional vehicle this sale is for (loaded once a customer is selected)
    customerVehicles = signal<CustomerVehicleResponse[]>([]);
    selectedVehicleId = signal<string | null>(null);
    loadingVehicles = signal(false);

    // Technicians
    selectedTechnician = signal<TechnicianResponse | null>(null);
    selectedTechnicianModel: TechnicianResponse | null = null;

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

    // Manual Discount
    manualDiscountAmount = signal<number>(0);
    // Promo code entered by cashier — validated against the API before it can be applied
    promoCode = signal<string>('');
    // Result of the last promo-code validation (null until a valid code has been applied)
    promoResult = signal<ResolveDiscountResult | null>(null);
    promoApplying = signal(false);
    promoError = signal<string>('');

    /** Whether a validated cart-level promo is currently applied. */
    promoApplied = computed(() => {
        const r = this.promoResult();
        return !!r && r.appliedLevel === 'CART' && r.discountAmount > 0;
    });

    /**
     * Human-readable label for the applied promo (name + type/value), shown inline under the
     * promo input. Mirrors how the server reports the resolved rule.
     */
    promoDiscountLabel = computed(() => {
        const r = this.promoResult();
        if (!r || r.appliedLevel !== 'CART' || r.discountAmount <= 0) return '';
        const value = r.discountType === 'PERCENTAGE' ? `${r.discountValue}% OFF` : `${this.formatCurrency(r.discountValue)} OFF`;
        return r.discountName ? `${r.discountName} - ${value}` : value;
    });

    /**
     * Effective cart-level discount amount. A validated promo wins over the manual discount,
     * matching the server's priority (promo code > manual discount > threshold auto-apply).
     */
    cartDiscountAmount = computed(() => (this.promoApplied() ? this.promoResult()!.discountAmount : this.manualDiscountAmount()));

    // Computed
    subtotal = computed(() => {
        return this.cartItems().reduce((sum, item) => {
            const lineTotal = item.quantity * item.unitPrice;
            return sum + (lineTotal - this.lineDiscountAmount(item));
        }, 0);
    });

    discountAmount = computed(() => {
        return this.cartItems().reduce((sum, item) => {
            return sum + this.lineDiscountAmount(item);
        }, 0);
    });

    vatEnabled = signal(false);
    vatPercentage = signal(0);
    vatAmount = computed(() => (this.vatEnabled() ? Math.round((this.subtotal() - this.cartDiscountAmount()) * this.vatPercentage()) / 100 : 0));

    grandTotal = computed(() => {
        return this.subtotal() - this.cartDiscountAmount() + this.vatAmount();
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
    saleNotes = '';
    paymentResponsibility: PaymentResponsibility = 'CUSTOMER';

    // Customer Credit
    customerCreditInfo: { advanceAmount: number; dueBalance: number } | null = null;
    loadingCustomerCredit = false;

    // Dialog data (all now shown through the single `activeOverlay` state machine — see
    // openOverlay()/closeOverlay() below — instead of one boolean per dialog).
    heldSales = signal<any[]>([]);
    customerPurchaseHistory = signal<CustomerOrderHistoryItem[]>([]);
    lastSale: any = null;
    returnInvoiceNumber = '';
    returnInvoice: any = null;
    returnRefundType: 'CASH_REFUND' | 'STORE_CREDIT' = 'CASH_REFUND';
    returnLines: { salesOrderLineId: string; partId: string; partName: string; partLocalName?: string | null; soldQty: number; unitPrice: number; returnQty: number; selected: boolean }[] = [];
    priceCheckCode = '';
    priceCheckResult: any = null;
    priceCheckLoading = false;
    priceCheckNotFound = false;
    bulkDiscountPercent = 0;

    // Reprint Receipt dialog
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

    // Barcode (exact-code fallback used by the Search list-dialog — see onSearchDialogSubmit())
    barcodeValue = '';

    // Multi-payment (NEW)
    paymentInputAmount: number | null = null;
    selectedPaymentMethod: 'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE' = 'CASH';
    useCreditBalance = signal(false);
    creditAmountToApply = signal(0);

    /** Getter, not a field: a field would freeze the labels in the language active at
     *  construction. paymentMethodOptions() is a computed(), and i18n.t() reads the
     *  translations signal, so the computed re-evaluates when the language changes. */
    private get allPaymentMethodOptions() {
        return [
            { label: this.i18n.t('pos.methods.CASH'), value: 'CASH' as const, icon: 'pi pi-money-bill' },
            { label: this.i18n.t('pos.methods.CARD'), value: 'CARD' as const, icon: 'pi pi-credit-card' },
            { label: this.i18n.t('pos.methods.MOBILE_BANKING'), value: 'MOBILE_BANKING' as const, icon: 'pi pi-mobile' },
            { label: this.i18n.t('pos.methods.DUE'), value: 'DUE' as const, icon: 'pi pi-clock' }
        ];
    }

    /** Walk-in customers are a reserved account and must never carry a due/credit balance. */
    isWalkInCustomer(): boolean {
        return this.selectedCustomer()?.customerCode === 'WALKIN';
    }

    /** DUE is hidden from the picker entirely for the reserved Walk-in customer. */
    paymentMethodOptions = computed(() => (this.isWalkInCustomer() ? this.allPaymentMethodOptions.filter((o) => o.value !== 'DUE') : this.allPaymentMethodOptions));

    // Payment reference fields
    paymentReference: string = '';
    paymentNotes: string = '';

    totalPaid = computed(() => this.payments().reduce((sum, p) => sum + p.amount, 0));
    totalDueAmount = computed(() =>
        this.payments()
            .filter((p) => p.method === 'DUE')
            .reduce((sum, p) => sum + p.amount, 0)
    );
    /**
     * Sum of every payment that isn't CASH (CARD/MOBILE_BANKING/DUE). Change can only be handed
     * back in cash, so this must never exceed the grand total — otherwise we'd overcharge a
     * card/mobile-banking instrument (or record more DUE than owed) to produce "change" that was
     * never tendered in cash. Mirrors the backend guard in CreateQuickSale.
     */
    nonCashOrDueTotal = computed(() => this.payments().filter((p) => p.method !== 'CASH').reduce((sum, p) => sum + p.amount, 0));
    remainingBalance = computed(() => {
        const creditApplied = this.useCreditBalance() ? this.creditAmountToApply() || 0 : 0;
        return Math.max(0, this.grandTotal() - this.totalPaid() - creditApplied);
    });
    /**
     * Cash change to return to the customer when tendered exceeds the settled amount.
     * Only makes sense for a fully-settled (remainingBalance === 0) cash sale; the backend
     * returns the same excess as ChangeDue so the figure shown pre-submit matches the receipt.
     */
    changeDue = computed(() => {
        if (this.remainingBalance() > 0.01) return 0;
        const creditApplied = this.useCreditBalance() ? this.creditAmountToApply() || 0 : 0;
        return Math.max(0, this.totalPaid() + creditApplied - this.grandTotal());
    });

    /** Count of lines currently selling below cost. The backend rejects the whole sale over these,
     *  with no override for any role — confirmCheckout() blocks client-side to match. */
    belowCostLineCount = computed(() => this.cartItems().filter((i) => this.lineIsBelowCost(i)).length);

    /**
     * A cart-level discount (manual amount or promo) isn't tied to any one line, so a line whose OWN
     * price passed lineIsBelowCost() can still end up below cost once the cart discount is spread
     * across the order — mirrors the backend's EnforceCartDiscountAsync (proportional by each line's
     * share of subtotal) so confirmCheckout() can block before submit, not just after a server
     * rejection. Lines without a known cost are skipped, same as the backend.
     */
    cartDiscountBelowCostLineCount = computed(() => {
        const cartDiscount = this.cartDiscountAmount();
        const subTotal = this.subtotal();
        if (cartDiscount <= 0 || subTotal <= 0) return 0;

        return this.cartItems().filter((item) => {
            if (!item.costPrice || item.costPrice <= 0 || item.quantity <= 0) return false;
            const lineShare = this.calculateLineTotal(item) / subTotal;
            const cartDiscountForLine = cartDiscount * lineShare;
            const factor = item.baseUnitFactor || 1;
            const additionalDiscountPerBaseUnit = cartDiscountForLine / item.quantity / factor;
            const netUnitPriceInBaseUnit = this.lineNetUnitPrice(item) / factor;
            const finalNetUnitPriceInBaseUnit = Math.max(0, netUnitPriceInBaseUnit - additionalDiscountPerBaseUnit);
            return finalNetUnitPriceInBaseUnit < item.costPrice!;
        }).length;
    });
    // Balance before any credit deduction — used as the ceiling for the "Apply" input.
    // remainingBalance() already nets out creditAmountToApply(), so using it as the [max]
    // would clamp the value back to 0 the instant it fully covers the sale.
    maxCreditApplicable = computed(() => {
        return Math.min(this.availableAdvance(), Math.max(0, this.grandTotal() - this.totalPaid()));
    });

    /**
     * Why Complete Sale is currently disabled, shown as an inline hint under the button instead of
     * leaving it silently greyed out — a cashier shouldn't have to guess. null once nothing blocks
     * checkout. Deliberately silent while the cart is empty (the empty-cart placeholder already
     * explains that) and while a submit is in flight.
     */
    checkoutBlockedReason = computed<string | null>(() => {
        if (this.cartItems().length === 0 || this.saving()) return null;
        if (!this.selectedCustomer()) return this.i18n.t('pos.selectCustomerHint');
        if (this.remainingBalance() > 0.01 && !this.hasDuePayments()) return this.i18n.t('pos.paymentIncompleteHint');
        return null;
    });

    // ===== POS SHELL: HEADER (design_handoff_pos_sale §1a) =====
    operatorLabel = computed(() => this.i18n.t('pos.registerLabel', { name: this.authService.currentUser()?.fullName || this.i18n.t('pos.cashier') }));

    /** Single-vertical (auto parts) context line — the attached vehicle when there is one,
     *  otherwise a generic walk-in line (README §1a "Context line is per-configuration"). */
    contextLine = computed(() => {
        const vehicleId = this.selectedVehicleId();
        if (vehicleId) {
            const vehicle = this.customerVehicles().find((v) => v.id === vehicleId);
            if (vehicle) return this.i18n.t('pos.contextVehicle', { label: vehicle.registrationNo ? `${vehicle.registrationNo} · ${vehicle.make} ${vehicle.model}` : `${vehicle.make} ${vehicle.model}` });
        }
        return this.isWalkInCustomer() || !this.selectedCustomer() ? this.i18n.t('pos.contextWalkIn') : this.i18n.t('pos.contextCustomer', { name: this.selectedCustomer()?.fullName });
    });

    // ===== POS SHELL: CART PANEL (design_handoff_pos_sale §1c) =====
    customerBarAttached = computed(() => !!this.selectedCustomer() && !this.isWalkInCustomer());
    customerBarName = computed(() => this.selectedCustomer()?.fullName || this.i18n.t('pos.walkInCustomer'));
    customerBarInitials = computed(() => {
        const name = this.selectedCustomer()?.fullName;
        if (!name || this.isWalkInCustomer()) return '+';
        return name
            .split(' ')
            .filter(Boolean)
            .map((w: string) => w[0])
            .slice(0, 2)
            .join('')
            .toUpperCase();
    });
    customerBarMeta = computed(() => {
        const c = this.selectedCustomer();
        if (!c || this.isWalkInCustomer()) return this.i18n.t('pos.tapToAttach');
        const due = c.dueAmount || 0;
        const balance = due > 0.01 ? this.i18n.t('pos.owesAmount', { amount: this.formatCurrency(due) }) : this.i18n.t('pos.noBalance');
        return `${c.customerType || this.i18n.t('pos.customer')} · ${balance}`;
    });

    ticketLabel = computed(() => `${this.i18n.t('pos.ticketPrefix')} ${this.invoiceNumber()}`);
    /** Only shown once a technician is actually assigned — no placeholder when there isn't one. */
    technicianLabel = computed(() => {
        const tech = this.selectedTechnician();
        return tech ? `${this.i18n.t('pos.technician')}: ${tech.name}` : null;
    });

    cartLines = computed<PosCartLine[]>(() =>
        this.cartItems().map((item) => {
            const compatibleUnits = this.compatibleUnitsMap.get(item.partId);
            return {
                name: item.partName || '',
                localName: item.partLocalName,
                unitLabel: `${this.formatCurrency(item.unitPrice)} ${this.i18n.t('pos.each')}`,
                qty: item.quantity,
                totalLabel: this.formatCurrency(this.calculateLineTotal(item)),
                discountBadge: item.discount > 0 ? `${item.discount}% ${this.i18n.t('pos.off')}` : item.autoDiscountAmount ? `-${this.formatCurrency(this.lineDiscountAmount(item))}` : null,
                belowCostBadge: this.lineIsBelowCost(item) ? `${this.i18n.t('pos.belowCost')} ${this.formatCurrency(this.lineBelowCostLoss(item))}` : null,
                unitId: item.unitId ?? null,
                unitOptions: compatibleUnits && compatibleUnits.length > 1 ? compatibleUnits.map((u) => ({ id: u.id, label: u.symbol || u.name })) : null
            };
        })
    );

    /** Unit-of-sale selector on a cart line (e.g. switch a part from Piece to Box) — mutates the
     *  line's unitId then reuses the existing conversion/re-pricing logic in onCartUnitChanged(),
     *  same as the old inline p-select did. */
    onCartLineUnitChange(event: { index: number; unitId: string }): void {
        const { index, unitId } = event;
        const current = this.cartItems()[index];
        if (!current || current.unitId === unitId) return;
        this.cartItems.update((items) => {
            const next = [...items];
            next[index] = { ...next[index], unitId };
            return next;
        });
        this.onCartUnitChanged(this.cartItems()[index], index);
    }

    /** `compatibleUnitsMap` is a plain Map, not a signal, so writing into it doesn't by itself
     *  make the `cartLines` computed (which reads it) re-evaluate — call this right after any
     *  `compatibleUnitsMap.set(...)` so a just-arrived unit list actually appears in the UI. */
    private pokeCartLines(): void {
        this.cartItems.update((items) => [...items]);
    }

    /** Design rule: decrementing to zero removes the line — decrementQty() alone never goes
     *  below 1, so a tap at qty 1 falls through to the existing removeFromCart() instead. */
    onCartDecrement(index: number): void {
        const item = this.cartItems()[index];
        if (item && item.quantity <= 1) this.removeFromCart(index);
        else this.decrementQty(index);
    }

    itemCountLabel = computed(() => {
        const count = this.cartItems().reduce((sum, i) => sum + i.quantity, 0);
        return this.i18n.t(count === 1 ? 'pos.subtotalOneItem' : 'pos.subtotalItems', { count });
    });

    cartDiscountLabel = computed(() => (this.promoApplied() ? this.promoDiscountLabel() || this.i18n.t('pos.discount') : this.i18n.t('pos.manualDiscount')));

    // ===== POS SHELL: SHORTCUT BAR (design_handoff_pos_sale §1d) =====
    shortcutKeys = computed<PosShortcutKey[]>(() => [
        { key: 'F2', label: this.i18n.t('pos.toolbar.search'), action: 'search' },
        { key: 'F3', label: this.i18n.t('pos.toolbar.stock'), action: 'stock' },
        { key: 'F4', label: this.i18n.t('pos.toolbar.customer'), action: 'customer' },
        { key: 'F6', label: this.i18n.t('pos.toolbar.discount'), action: 'discount' },
        { key: 'F7', label: this.i18n.t('pos.toolbar.hold'), action: 'hold', disabled: this.cartItems().length === 0 },
        { key: 'F8', label: this.i18n.t('pos.toolbar.recall'), action: 'recall' },
        { key: 'F9', label: this.i18n.t('pos.toolbar.reprint'), action: 'reprint' },
        { key: 'F10', label: this.i18n.t('pos.toolbar.returns'), action: 'returns' }
    ]);

    shortcutExtraActions = computed<PosExtraAction[]>(() => [
        { id: 'vehicle', label: this.i18n.t('pos.toolbar.vehicle'), disabled: !this.selectedCustomer() },
        { id: 'technician', label: this.i18n.t('pos.toolbar.technician'), disabled: false },
        { id: 'options', label: this.i18n.t('pos.toolbar.options'), disabled: false },
        { id: 'lastSale', label: this.i18n.t('pos.toolbar.lastSale'), disabled: !this.hasLastSale() },
        { id: 'history', label: this.i18n.t('pos.toolbar.history'), disabled: !this.selectedCustomer() },
        { id: 'credit', label: this.i18n.t('pos.toolbar.credit'), disabled: !this.selectedCustomer() },
        { id: 'newSale', label: this.i18n.t('pos.toolbar.newSale'), disabled: false }
    ]);

    statusLine = computed(() => this.i18n.t('pos.statusLine', { held: this.quickSaleService.getHeldSales().length }));

    onShortcutClick(action: string): void {
        switch (action) {
            case 'search':
                this.openSearchDialog();
                break;
            case 'stock':
                this.openStockSearch();
                break;
            case 'customer':
                this.openCustomerDialog();
                break;
            case 'discount':
                this.openDiscountDialog();
                break;
            case 'hold':
                this.holdSale();
                break;
            case 'recall':
                this.recallHeldSales();
                break;
            case 'reprint':
                this.openReprintDialog();
                break;
            case 'returns':
                this.openReturns();
                break;
        }
    }

    onExtraActionClick(id: string): void {
        switch (id) {
            case 'vehicle':
                this.openVehicleDialog();
                break;
            case 'technician':
                this.openTechnicianDialog();
                break;
            case 'options':
                this.activeOverlay.set('options');
                break;
            case 'lastSale':
                this.viewLastSale();
                break;
            case 'history':
                this.openCustomerHistory();
                break;
            case 'credit':
                this.viewCustomerCredit();
                break;
            case 'newSale':
                this.resetShortcut();
                break;
        }
    }

    // Note: the old inline sidebar's "quick tender chip" one-tap suggestions are superseded by the
    // new Tender dialog's payment-type tiles (tenderFullBalance()) and Quick Cash grid
    // (quickCashOptions()/onQuickCashTap()) below, which cover the same ground per the design.

    readonly Math = Math;

    // ===== TENDER DIALOG (design_handoff_pos_sale §3) =====
    /** Charge → opens the Tender overlay pre-loaded with the full remaining balance, same guard
     *  (non-empty cart) the old inline Charge button used. */
    openTenderDialog(): void {
        if (this.cartItems().length === 0) return;
        this.keypadDigits.set('');
        this.activeOverlay.set('tender');
    }

    tenderPayTypeNotes: Record<'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE', string> = {
        CASH: 'drawerOpens',
        CARD: 'chipOrTap',
        MOBILE_BANKING: 'qrOrNfc',
        DUE: 'onAccount'
    };

    tenderPayTypes = computed<PosPayType[]>(() => {
        const settled = this.remainingBalance() <= 0.01;
        return this.paymentMethodOptions().map((o) => ({
            value: o.value,
            label: o.label,
            note: settled ? this.i18n.t('pos.balanceSettled') : this.i18n.t(`pos.payNote.${this.tenderPayTypeNotes[o.value]}`),
            disabled: settled
        }));
    });

    tenderRows = computed<PosTenderRow[]>(() => this.payments().map((p) => ({ label: this.getPaymentLabel(p.method), amountLabel: this.formatCurrency(p.amount) })));

    /** Tapping a payment-type tile tenders the *entire* remaining balance in that type (design
     *  §3) — additive to the existing manual-amount keypad flow, both funnel through addNewPayment().
     *  Whatever the cashier has already typed into the reference field (needed for CARD/MOBILE_BANKING
     *  reconciliation — see requiresReference()) is preserved, not cleared, since there's no separate
     *  "enter reference, then tender" step in this one-tap design. */
    tenderFullBalance(method: 'CASH' | 'CARD' | 'MOBILE_BANKING' | 'DUE'): void {
        const remaining = this.remainingBalance();
        if (remaining <= 0.01) return;
        this.selectedPaymentMethod = method;
        this.paymentInputAmount = remaining;
        this.addNewPayment();
    }

    /** Cents-first keypad entry (README §3 "Entry is cents-first"): digits append to a string,
     *  parsed as an integer and divided by 100. */
    onKeypadPress(label: string): void {
        this.keypadDigits.update((s) => (label === '⌫' ? s.slice(0, -1) : (s + label).replace(/^0+(?=\d)/, '').slice(0, 8)));
    }

    keypadAmount = computed(() => Math.round((parseInt(this.keypadDigits() || '0', 10) || 0)) / 100);
    keypadAmountEmpty = computed(() => this.keypadDigits() === '');
    keypadAmountDisplay = computed(() => this.formatCurrency(this.keypadAmountEmpty() ? 0 : this.keypadAmount()));

    amountHint = computed(() => {
        if (this.keypadAmountEmpty()) return this.i18n.t('pos.amountHintEmpty');
        const entered = this.keypadAmount();
        const due = this.remainingBalance();
        if (entered > due) return this.i18n.t('pos.amountHintChange', { amount: this.formatCurrency(entered), change: this.formatCurrency(entered - due) });
        if (Math.abs(entered - due) < 0.005) return this.i18n.t('pos.amountHintExact');
        return this.i18n.t('pos.amountHintRemaining', { amount: this.formatCurrency(due - entered) });
    });

    addKeypadCash(): void {
        if (this.keypadAmount() <= 0) return;
        this.selectedPaymentMethod = 'CASH';
        this.paymentInputAmount = this.keypadAmount();
        this.addNewPayment();
        this.keypadDigits.set('');
    }

    addKeypadCard(): void {
        if (this.keypadAmount() <= 0) return;
        this.selectedPaymentMethod = 'CARD';
        this.paymentInputAmount = this.keypadAmount();
        this.addNewPayment();
        this.keypadDigits.set('');
    }

    /** Quick-cash denominations (README §3): the exact balance, its ceiling, the balance rounded
     *  up to the next 5/10/20/50, and every standard note larger than the balance — deduplicated,
     *  ascending, first six. A small pure helper, not a new business rule. */
    static quickCashDenominations(due: number): number[] {
        if (due <= 0) return [];
        const notes = [5, 10, 20, 50, 100, 200, 500];
        const candidates = [due, Math.ceil(due)].concat([5, 10, 20, 50].map((s) => Math.ceil(due / s) * s)).concat(notes.filter((n) => n > due));
        const seen = new Set<number>();
        const unique: number[] = [];
        for (const v of candidates) {
            const rounded = Math.round(v * 100) / 100;
            if (rounded > 0 && !seen.has(rounded)) {
                seen.add(rounded);
                unique.push(rounded);
            }
        }
        return unique.sort((a, b) => a - b).slice(0, 6);
    }

    quickCashOptions = computed<PosQuickCash[]>(() => {
        const due = this.remainingBalance();
        return QuickSaleShortcutComponent.quickCashDenominations(due).map((amount) => {
            const change = Math.round((amount - due) * 100) / 100;
            return {
                amount,
                label: this.formatCurrency(amount),
                changeNote: change > 0.005 ? this.i18n.t('pos.changeNote', { amount: this.formatCurrency(change) }) : this.i18n.t('pos.exactNote'),
                exact: change <= 0.005
            };
        });
    });

    onQuickCashTap(amount: number): void {
        this.selectedPaymentMethod = 'CASH';
        this.paymentInputAmount = amount;
        this.addNewPayment();
    }

    /** Whether the tender dialog should show its optional reference field for the currently
     *  selected payment type — CARD/MOBILE_BANKING need a transaction reference for reconciliation
     *  (see requiresReference()); CASH/DUE never do. A plain method (not computed()) since
     *  `selectedPaymentMethod` is a plain field, not a signal — Angular's own change detection
     *  re-evaluates this on every check, same as any other template method call. */
    tenderReferenceVisible(): boolean {
        return this.requiresReference(this.selectedPaymentMethod);
    }

    tenderCompleteLabel = computed(() => {
        if (this.remainingBalance() > 0.01) return this.i18n.t('pos.balanceRemaining', { amount: this.formatCurrency(this.remainingBalance()) });
        const blocked = this.checkoutBlockedReason();
        return blocked ?? this.i18n.t('pos.completeSale');
    });

    // ===== LIFECYCLE =====
    ngOnInit(): void {
        // Company name/theme is cheap and needed even if the till-session gate ends up blocking
        // the screen (the header still renders in the blocked state).
        this.companyName = this.invoicePdfService.getCompanyConfig().companyName;

        // Till session gate — check this before loading anything else. If the current user's role
        // requires an open till session and they don't have one, there's no point pulling in
        // units/VAT config/drafts for a cart they won't be allowed to check out anyway.
        this.tillSessionService.checkRequiresOpenSession().subscribe({
            next: (result) => {
                this.checkingTillSession.set(false);
                if (result.required && !result.hasOpenSession) {
                    this.tillSessionBlocked.set(true);
                    return;
                }
                this.initQuickSaleData();
            },
            error: () => {
                // Fail open on the pre-check itself — this is only a UX nicety; the backend still
                // enforces the same gate at submit time (CreateQuickSale) as the real safety net.
                this.checkingTillSession.set(false);
                this.initQuickSaleData();
            }
        });
    }

    /** The screen's normal init work — skipped entirely while the till-session gate is blocking. */
    private initQuickSaleData(): void {
        this.initializeForm();
        this.generateInvoiceNumber();
        this.loadUnits();
        this.restoreDraft();
        this.selectWalkInCustomerIfNone();
        this.loadCatalog();
        this.quickSaleService.getVATConfig().subscribe((cfg) => {
            this.vatPercentage.set(cfg.percentage);
            this.vatEnabled.set(cfg.enabled);
        });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
        clearTimeout(this.customerDialogDebounce);
        clearTimeout(this.technicianDialogDebounce);
    }

    // ===== POS SHELL: RESPONSIVE LAYOUT (README "The layout is fluid") =====
    @HostListener('window:resize')
    onWindowResize(): void {
        const narrow = window.innerWidth < 780;
        const short = window.innerHeight < 660;
        if (narrow !== this.narrow()) this.narrow.set(narrow);
        if (short !== this.short()) this.short.set(short);
    }

    // ===== POS SHELL: KEYBOARD SHORTCUTS (README §1d "Keyboard") =====
    private readonly keyToOverlay: Record<string, PosOverlay> = {
        F2: 'search',
        F3: 'stock',
        F4: 'customer',
        F6: 'discount',
        F8: 'recall',
        F9: 'reprint',
        F10: 'returns'
    };

    @HostListener('window:keydown', ['$event'])
    onWindowKeydown(event: KeyboardEvent): void {
        // The price-override panel is a bespoke overlay kept outside the activeOverlay stack (it
        // layers on top of whatever triggered it) — it must own Escape/F-keys itself while open, or
        // Escape would silently close the overlay *behind* it and F-keys would silently change
        // activeOverlay while this panel stays visually on top.
        if (this.showPriceOverrideDialog) {
            if (event.key === 'Escape') {
                event.preventDefault();
                this.cancelPriceOverrideDialog();
            }
            return;
        }

        // The till-session gate hides the catalog/cart/shortcut-bar UI entirely, but this listener
        // is bound at the window level regardless — without this guard, a blocked cashier could
        // still drive the whole sale via F-keys (search/add a part, attach a customer, tender) only
        // to have it fail server-side at the final submit. Escape still closes whatever's open.
        if (this.tillSessionBlocked() && event.key !== 'Escape') return;

        if (event.key === 'Escape') {
            if (this.isOverlayOpen()) {
                event.preventDefault();
                this.closeOverlay();
            }
            return;
        }

        // F-keys must not fire while the cashier is typing into a filter/text field (matches the
        // plan's guard — Escape above is exempt on purpose).
        const target = event.target as HTMLElement | null;
        const typing = !!target && (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA' || target.isContentEditable);
        if (typing) return;

        if (event.key === 'F7') {
            event.preventDefault();
            this.holdSale();
            return;
        }

        const overlay = this.keyToOverlay[event.key];
        if (overlay) {
            event.preventDefault();
            this.openOverlayByKey(overlay);
        }
    }

    /** Routes a keyboard/F-key shortcut to the right opener so every overlay's own bespoke setup
     *  (search reset, stock search reset, etc.) still runs, instead of just poking activeOverlay. */
    private openOverlayByKey(overlay: PosOverlay): void {
        switch (overlay) {
            case 'search':
                this.openSearchDialog();
                break;
            case 'stock':
                this.openStockSearch();
                break;
            case 'customer':
                this.openCustomerDialog();
                break;
            case 'discount':
                this.openDiscountDialog();
                break;
            case 'recall':
                this.recallHeldSales();
                break;
            case 'reprint':
                this.openReprintDialog();
                break;
            case 'returns':
                this.openReturns();
                break;
            default:
                this.activeOverlay.set(overlay);
        }
    }

    // ===== POS SHELL: OVERLAY STATE MACHINE =====
    closeOverlay(): void {
        this.activeOverlay.set(null);
        this.dialogQuery.set('');
    }

    // ===== POS SHELL: CATALOG (design_handoff_pos_sale §1b) =====
    /** Loads the first page of the catalog. Further pages load on demand as the grid is scrolled
     *  near its bottom (see loadMoreCatalog()) rather than bulk-loading the whole catalog upfront —
     *  chip + free-text filtering still happen client-side, but only over pages loaded so far. */
    loadCatalog(): void {
        this.catalogLoading.set(true);
        this.catalogPageNumber = 1;
        this.catalogHasMore = true;
        this.partService.getParts({ search: '', pageNumber: 1, pageSize: QuickSaleShortcutComponent.CATALOG_PAGE_SIZE, isActive: true, flattenVariants: true }).subscribe({
            next: (res) => {
                this.catalogParts.set(res.data ?? []);
                this.catalogHasMore = this.catalogPageNumber < (res.pagination?.totalPages ?? 1);
                this.catalogLoading.set(false);
                this.resolveCatalogStock(res.data ?? []);
            },
            error: () => this.catalogLoading.set(false)
        });
    }

    /** Fetches the next catalog page and appends it — called when the grid scrolls near its
     *  bottom. No-ops while a fetch is already in flight or no further pages remain. */
    loadMoreCatalog(): void {
        if (this.catalogLoadingMore() || this.catalogLoading() || !this.catalogHasMore) return;
        this.catalogLoadingMore.set(true);
        const nextPage = this.catalogPageNumber + 1;
        this.partService.getParts({ search: '', pageNumber: nextPage, pageSize: QuickSaleShortcutComponent.CATALOG_PAGE_SIZE, isActive: true, flattenVariants: true }).subscribe({
            next: (res) => {
                this.catalogPageNumber = nextPage;
                this.catalogHasMore = this.catalogPageNumber < (res.pagination?.totalPages ?? 1);
                const newParts = res.data ?? [];
                this.catalogParts.set([...this.catalogParts(), ...newParts]);
                this.catalogLoadingMore.set(false);
                this.resolveCatalogStock(newParts);
            },
            error: () => this.catalogLoadingMore.set(false)
        });
    }

    /** Batch-resolves on-hand stock for a set of newly-loaded parts (advisory display only — the
     *  checkout itself is still authoritative server-side on a failure or omission here). */
    private resolveCatalogStock(parts: PublicPartResponse[]): void {
        const items = parts.map((p) => ({ partId: p.id, variantId: p.variantId ?? null, quantity: 1 }));
        if (items.length === 0) return;
        this.quickSaleService.checkMultipleStock(items).subscribe({
            next: (results) => {
                results.forEach((r) => this.catalogStock.set(this.catalogStockKey(r.partId, r.variantId ?? null), r.stockAvailable));
                // Force a re-render of the (memo-free) tile mapper now that stock is known.
                this.catalogParts.set([...this.catalogParts()]);
            },
            error: () => {
                // Stock badges are advisory only.
            }
        });
    }

    private catalogStockKey(partId: string, variantId?: string | null): string {
        return `${partId}|${variantId ?? ''}`;
    }

    /** Category chips derived from the loaded catalog, sorted, single-select (toggle off on repeat tap). */
    catalogChips = computed<PosCatalogChip[]>(() => {
        const active = this.catalogChip();
        const names = Array.from(new Set(this.catalogParts().map((p) => p.categoryName).filter((n): n is string => !!n))).sort((a, b) => a.localeCompare(b));
        return names.map((label) => ({ label, active: label === active }));
    });

    onCatalogChipToggle(label: string): void {
        this.catalogChip.set(this.catalogChip() === label ? null : label);
    }

    /** Grid tiles: filtered by the active category chip only — free-text lookup lives in the
     *  header's Search list-dialog (README §"Filtering"), not the grid itself. Formatting
     *  (currency, i18n stock labels) stays in the orchestrator so the presentational child never
     *  touches money/locale directly. */
    catalogTiles = computed<PosCatalogTile[]>(() => {
        const chip = this.catalogChip();
        return this.catalogParts()
            .filter((p) => !chip || p.categoryName === chip)
            .map((p) => this.toCatalogTile(p));
    });

    private toCatalogTile(p: PublicPartResponse): PosCatalogTile {
        const stock = this.catalogStock.get(this.catalogStockKey(p.id, p.variantId ?? null));
        const blocked = stock != null && stock <= 0;
        const tone: PosCatalogTile['stockTone'] = stock == null ? 'muted' : stock <= 0 ? 'red' : stock < 5 ? 'amber' : 'muted';
        const stockLabel = stock == null ? '' : stock <= 0 ? this.i18n.t('pos.outOfStock') : stock < 5 ? this.i18n.t('pos.leftCount', { count: stock }) : this.i18n.t('pos.inStock');
        return {
            partId: p.id,
            variantId: p.variantId ?? null,
            meta: (p.variantName || p.categoryName || '').toUpperCase(),
            name: p.displayName || p.name,
            priceLabel: this.formatCurrency(p.effectiveSellingPrice ?? p.sellingPrice),
            stockLabel,
            stockTone: tone,
            blocked
        };
    }

    /** "N on hand" sub-label reused by the Search list-dialog rows (README §2 "Search" row spec). */
    catalogOnHandLabel(part: PublicPartResponse): string {
        const stock = this.catalogStock.get(this.catalogStockKey(part.id, part.variantId ?? null));
        return stock == null ? '' : this.i18n.t('pos.onHand', { count: stock });
    }

    onCatalogTileAdd(tile: PosCatalogTile): void {
        const part = this.catalogParts().find((p) => p.id === tile.partId && (p.variantId ?? null) === tile.variantId);
        if (part) this.selectPart(part);
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
                message: this.i18n.t('pos.messages.restoreDraft'),
                header: this.i18n.t('pos.messages.draftFound'),
                icon: 'pi pi-info-circle',
                // Every confirm() call must set its own accept/reject labels explicitly — PrimeNG's
                // ConfirmationService carries over the LAST call's labels for any field a later call
                // omits (e.g. without this, this dialog would show confirmCheckout()'s "Complete Sale").
                acceptLabel: this.i18n.t('common.actions.restore'),
                rejectLabel: this.i18n.t('common.actions.cancel'),
                accept: () => {
                    this.restoreSaleState(draft);
                    this.quickSaleService.clearDraft();
                }
            });
        }
    }

    // ===== PRODUCT SELECTION =====
    selectPart(event: any): void {
        const part = event as PublicPartResponse;
        const existing = this.cartItems().find((item) => item.partId === part.id && (item.productVariantId ?? null) === (part.variantId ?? null));
        if (existing) {
            this.messageService.add({ severity: 'info', summary: this.i18n.t('pos.messages.alreadyAdded'), detail: this.i18n.t('pos.messages.alreadyAddedDetail') });
            return;
        }

        if (part.unitId) {
            this.unitService.getCompatibleUnits(part.unitId).subscribe({
                next: (compatibleUnits) => {
                    this.compatibleUnitsMap.set(part.id, compatibleUnits);
                    this.pokeCartLines();
                },
                error: () => {
                    this.compatibleUnitsMap.set(part.id, this.units());
                    this.pokeCartLines();
                }
            });
        }

        const newItem: QuickSaleLineItem = {
            partId: part.id,
            productVariantId: part.variantId ?? undefined,
            partName: part.displayName || part.name,
            partLocalName: part.localName ?? null,
            partNumber: part.partNumber,
            sku: part.variantSKU || part.sku,
            unitId: part.unitId || undefined,
            quantity: 1,
            unitPrice: part.effectiveSellingPrice ?? part.sellingPrice,
            discount: 0
        };

        this.cartItems.update((items) => [...items, newItem]);
        this.fetchLineInfo(newItem);
        if (part.unitId) {
            this.cartUnitSelection.set(this.cartItems().length - 1, part.unitId);
        }

        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.partAdded'), detail: this.i18n.t('pos.messages.partAddedDetail', { name: part.displayName || part.name }) });
    }

    // ===== CART ACTIONS =====
    incrementQty(index: number): void {
        this.cartItems.update((items) => {
            const newItems = [...items];
            newItems[index] = { ...newItems[index], quantity: newItems[index].quantity + 1 };
            return newItems;
        });
        this.clearPricingError(index);
    }

    decrementQty(index: number): void {
        this.cartItems.update((items) => {
            const newItems = [...items];
            if (newItems[index].quantity > 1) {
                newItems[index] = { ...newItems[index], quantity: newItems[index].quantity - 1 };
            }
            return newItems;
        });
        this.clearPricingError(index);
    }

    removeFromCart(index: number): void {
        this.cartItems.update((items) => items.filter((_, i) => i !== index));
        this.pricingErrors.clear();
    }

    clearPricingError(index: number): void {
        this.pricingErrors.delete(index);
    }

    calculateLineTotal(item: QuickSaleLineItem): number {
        const lineTotal = item.quantity * item.unitPrice;
        const discountAmount = this.lineDiscountAmount(item);
        return lineTotal - discountAmount;
    }

    /**
     * Effective discount AMOUNT for the whole line (currency). A manual percentage override
     * (item.discount > 0) wins — matching the backend, which applies it as a percentage before
     * considering any resolved rule. Otherwise the line carries the auto-resolved item-level
     * (VARIANT/PRODUCT) rule, whose unit amount is scaled by quantity to a line total.
     */
    lineDiscountAmount(item: QuickSaleLineItem): number {
        if (item.discount > 0) {
            return (item.quantity * item.unitPrice * item.discount) / 100;
        }
        return item.quantity * (item.autoDiscountAmount ?? 0);
    }

    /**
     * Net per-unit price after ALL discount sources (manual %, auto item rule, cart-level promo /
     * manual cart discount are folded into the unit price during line extract) — used to judge
     * below-cost margins. Cost availability is advisory; lines without cost are never flagged.
     */
    lineNetUnitPrice(item: QuickSaleLineItem): number {
        if (item.discount > 0) return Math.max(0, item.unitPrice - (item.unitPrice * item.discount) / 100);
        return Math.max(0, item.unitPrice - (item.autoDiscountAmount ?? 0));
    }

    /**
     * costPrice is always per BASE unit, but unitPrice (and so lineNetUnitPrice) is in the
     * currently-selected sale unit — divide by baseUnitFactor to compare like-for-like, matching
     * the backend's own base-unit normalization for the same check.
     */
    lineIsBelowCost(item: QuickSaleLineItem): boolean {
        const cost = item.costPrice;
        if (!cost || cost <= 0) return false;
        const factor = item.baseUnitFactor || 1;
        return this.lineNetUnitPrice(item) / factor < cost;
    }

    lineBelowCostLoss(item: QuickSaleLineItem): number {
        if (!this.lineIsBelowCost(item)) return 0;
        const factor = item.baseUnitFactor || 1;
        return Math.max(0, (item.costPrice ?? 0) * factor - this.lineNetUnitPrice(item));
    }

    /**
     * Populates both the authoritative per-unit COST (FIFO lot cost or catalogue fallback) and the
     * auto-resolved item discount on a freshly added cart line, in one round trip instead of two
     * separate calls (getUnitCost + resolveItemDiscount) — this fires on every "add to cart", so
     * halving it halves the request count for the POS's most frequent interaction. Best-effort: a
     * lookup failure simply leaves cost/discount unknown, same as before.
     */
    private fetchLineInfo(item: QuickSaleLineItem): void {
        const variantId = item.productVariantId ?? null;
        this.pricingValidationService.getLineInfo(item.partId, item.unitPrice, variantId).subscribe({
            next: (res) => {
                this.cartItems.update((items) =>
                    items.map((existing) => {
                        if (existing.partId !== item.partId || (existing.productVariantId ?? null) !== variantId) return existing;
                        const withCost = res.costPrice > 0 && existing.costPrice == null ? { ...existing, costPrice: Math.round(res.costPrice * 100) / 100 } : existing;
                        // Only apply the auto-discount while the line still has no manual override —
                        // a cashier may have set a manual % while this response was in flight, and
                        // manual always wins.
                        if (res.appliedLevel === 'NONE' || res.appliedLevel === 'CART' || res.discountAmount <= 0 || withCost.discount !== 0) return withCost;
                        return { ...withCost, autoDiscountAmount: Math.round(res.discountAmount * 100) / 100, autoDiscountName: res.discountName };
                    })
                );
            },
            error: () => {
                // Best-effort display only — never blocks. The backend re-resolves authoritatively at submit.
            }
        });
    }

    /**
     * Resolves the item-level (VARIANT/PRODUCT) discount rule for a cart line and records it on it,
     * so the payment section reflects the discount the backend will apply on submit. Only called
     * after a unit-of-sale switch (onCartUnitChanged) re-prices a line — freshly added lines instead
     * go through fetchLineInfo(), which resolves cost + discount together in one round trip.
     * result.discountAmount is a PER-UNIT amount (the backend uses it directly as salesOrderLine.Discount
     * and totals quantity * Discount), so it is stored per-unit too. A line that already carries a manual
     * percentage override is left untouched (manual wins).
     */
    private resolveItemDiscount(item: QuickSaleLineItem): void {
        if (item.discount > 0) return;

        const variantId = item.productVariantId ?? undefined;
        this.discountService.resolveItemDiscount(item.partId, item.unitPrice, variantId).subscribe({
            next: (result) => {
                if (result.appliedLevel === 'NONE' || result.appliedLevel === 'CART' || result.discountAmount <= 0) return;
                this.cartItems.update((items) =>
                    items.map((existing) =>
                        // Only apply while the line still has no manual override — a cashier may have
                        // set a manual % while this response was in flight, and manual always wins.
                        existing.partId === item.partId && (existing.productVariantId ?? null) === (variantId ?? null) && existing.discount === 0
                            ? {
                                  ...existing,
                                  autoDiscountAmount: Math.round(result.discountAmount * 100) / 100,
                                  autoDiscountName: result.discountName
                              }
                            : existing
                    )
                );
            },
            error: () => {
                // Resolution is best-effort display help — a resolution failure should never block
                // the sale. The backend re-resolves authoritatively at submit.
            }
        });
    }

    onCartUnitChanged(item: QuickSaleLineItem, index: number): void {
        const previousUnitId = this.cartUnitSelection.get(index);
        const nextUnitId = item.unitId;
        if (!previousUnitId || !nextUnitId || previousUnitId === nextUnitId) return;

        const currentPrice = Number(item.unitPrice || 0);
        const previousBaseUnitFactor = item.baseUnitFactor || 1;
        this.unitConversionService.getConversion(nextUnitId, previousUnitId).subscribe({
            next: (res) => {
                const newPrice = currentPrice * res.conversionFactor;
                // baseUnitFactor tracks the same rescaling as unitPrice so costPrice (always
                // per-base-unit) stays comparable to unitPrice after the switch.
                const newBaseUnitFactor = previousBaseUnitFactor * res.conversionFactor;
                this.cartItems.update((items) => {
                    const newItems = [...items];
                    newItems[index] = { ...newItems[index], unitPrice: Math.round(newPrice * 100) / 100, baseUnitFactor: newBaseUnitFactor };
                    return newItems;
                });
                this.cartUnitSelection.set(index, nextUnitId);
                // The per-unit auto discount depends on unit price, so re-resolve it against the new
                // price — otherwise the displayed discount drifts from what the backend applies on
                // submit. Manual % lines are untouched (manual wins).
                const converted = this.cartItems()[index];
                if (converted && converted.discount === 0) this.resolveItemDiscount(converted);
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
                    this.guardWalkInDuePaymentMethod();
                },
                error: () => {
                    this.selectedCustomer.set(event);
                    this.selectedCustomerModel = event;
                    this.guardWalkInDuePaymentMethod();
                }
            });
            this.loadCustomerVehicles(event.id);
        } else {
            this.selectedCustomer.set(event);
            this.selectedCustomerModel = event;
            this.clearVehicleSelection();
            this.guardWalkInDuePaymentMethod();
        }
    }

    /** Reserved Walk-in customer must never carry a DUE payment — bump back to Cash if it was pre-selected. */
    private guardWalkInDuePaymentMethod(): void {
        if (this.isWalkInCustomer() && this.selectedPaymentMethod === 'DUE') {
            this.selectedPaymentMethod = 'CASH';
            this.paymentInputAmount = this.remainingBalance();
        }
    }

    /**
     * Defaults the reserved Walk-in customer onto a fresh/empty screen so the cashier isn't
     * blocked on Complete Sale by an invisible "no customer selected" state — standard POS
     * behavior (Square, Shopify POS, Lightspeed all pre-select a walk-in/guest account). The
     * cashier can still swap it for a registered customer via the search at any time. A no-op
     * if a customer is already set (draft restore, held-sale recall) or no reserved Walk-in
     * account exists in this environment.
     */
    private selectWalkInCustomerIfNone(): void {
        if (this.selectedCustomer()) return;
        this.customerService.getCustomerByCode('WALKIN').subscribe({
            next: (walkIn) => {
                if (this.selectedCustomer()) return; // a draft/recall may have won the race meanwhile
                this.selectedCustomer.set(walkIn);
                this.selectedCustomerModel = walkIn;
            },
            error: () => {
                // No reserved Walk-in account here — cashier picks a customer manually, as before.
            }
        });
    }

    private loadCustomerVehicles(customerId: string, preselectVehicleId: string | null = null): void {
        this.clearVehicleSelection();
        this.loadingVehicles.set(true);
        this.vehicleService.getByCustomer(customerId, true).subscribe({
            next: (vehicles) => {
                this.customerVehicles.set(vehicles);
                if (preselectVehicleId && vehicles.some((v) => v.id === preselectVehicleId)) {
                    this.selectedVehicleId.set(preselectVehicleId);
                }
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

    // ===== VEHICLE PICKER (extra shortcut action — no F-key slot in the design) =====
    openVehicleDialog(): void {
        if (!this.selectedCustomer()) return;
        this.activeOverlay.set('vehicle');
    }

    selectVehicleFromDialog(vehicleId: string | null): void {
        this.selectedVehicleId.set(vehicleId);
        this.closeOverlay();
    }

    // ===== TECHNICIAN PICKER (extra shortcut action — no F-key slot in the design) =====
    openTechnicianDialog(): void {
        this.dialogQuery.set('');
        this.activeOverlay.set('technician');
        this.runTechnicianDialogSearch();
    }

    onTechnicianDialogQuery(value: string): void {
        this.dialogQuery.set(value);
        clearTimeout(this.technicianDialogDebounce);
        this.technicianDialogDebounce = setTimeout(() => this.runTechnicianDialogSearch(), 250);
    }

    private runTechnicianDialogSearch(): void {
        this.technicianDialogLoading.set(true);
        this.technicianService.getTechnicians({ search: this.dialogQuery(), pageNumber: 1, pageSize: 8 }).subscribe({
            next: (res) => {
                this.technicianDialogResults.set((res.data ?? []).filter((t) => t.status === 'ACTIVE'));
                this.technicianDialogLoading.set(false);
            },
            error: () => this.technicianDialogLoading.set(false)
        });
    }

    selectTechnicianFromDialog(tech: TechnicianResponse | null): void {
        this.selectTechnician(tech);
        this.closeOverlay();
    }

    setPaymentResponsibility(value: PaymentResponsibility): void {
        this.paymentResponsibility = value;
    }

    openQuickCustomerDialog(): void {
        this.quickCustomerDialog.open();
    }

    onCustomerCreated(customer: any): void {
        this.selectedCustomer.set(customer);
        this.selectedCustomerModel = customer;
        this.clearVehicleSelection();
        this.guardWalkInDuePaymentMethod();
        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.customerCreated'), detail: this.i18n.t('pos.messages.customerCreatedDetail', { name: customer.fullName }) });
    }

    // ===== SEARCH DIALOG (F2 / header search trigger) =====
    /** Backend-searched rows (max 8, debounced) — independent of the lazily-paged catalog grid
     *  cache so a search can find any active part regardless of how far the grid has scrolled. */
    private runSearchDialogSearch(fallbackToBarcodeIfEmpty = false): void {
        const q = this.dialogQuery().trim();
        if (!q) {
            this.searchDialogResults.set([]);
            return;
        }
        this.searchDialogLoading.set(true);
        this.partService.getParts({ search: q, pageNumber: 1, pageSize: 8, isActive: true, flattenVariants: true }).subscribe({
            next: (res) => {
                const rows = res.data ?? [];
                this.searchDialogResults.set(rows);
                this.searchDialogLoading.set(false);
                // A searched-up part may not be one of the catalog grid's currently-loaded pages,
                // so its on-hand stock (catalogOnHandLabel) may not be resolved yet — fetch it here
                // too (harmless no-op if already known; resolveCatalogStock merges into the same map).
                this.resolveCatalogStock(rows);
                if (fallbackToBarcodeIfEmpty && rows.length === 0) {
                    this.barcodeValue = q;
                    this.processBarcodeInput();
                    this.closeOverlay();
                }
            },
            error: () => this.searchDialogLoading.set(false)
        });
    }

    openSearchDialog(): void {
        this.dialogQuery.set('');
        this.searchDialogResults.set([]);
        this.activeOverlay.set('search');
    }

    onSearchDialogQuery(value: string): void {
        this.dialogQuery.set(value);
        clearTimeout(this.searchDialogDebounce);
        if (!value.trim()) {
            this.searchDialogResults.set([]);
            return;
        }
        this.searchDialogDebounce = setTimeout(() => this.runSearchDialogSearch(), 250);
    }

    selectSearchRow(part: PublicPartResponse): void {
        this.selectPart(part);
        this.closeOverlay();
    }

    /** Enter with no current matches falls back to an exact code lookup (barcode/SKU/part number)
     *  via the existing barcode pipeline, run immediately rather than waiting on the debounce. */
    onSearchDialogSubmit(): void {
        clearTimeout(this.searchDialogDebounce);
        if (this.searchDialogResults().length > 0) return;
        if (!this.dialogQuery().trim()) return;
        this.runSearchDialogSearch(true);
    }

    // ===== CUSTOMER LIST-DIALOG (F4 / customer bar) =====
    openCustomerDialog(): void {
        this.dialogQuery.set('');
        this.activeOverlay.set('customer');
        this.runCustomerDialogSearch();
    }

    onCustomerDialogQuery(value: string): void {
        this.dialogQuery.set(value);
        clearTimeout(this.customerDialogDebounce);
        this.customerDialogDebounce = setTimeout(() => this.runCustomerDialogSearch(), 250);
    }

    private runCustomerDialogSearch(): void {
        this.customerDialogLoading.set(true);
        this.customerService.getCustomers({ search: this.dialogQuery(), pageNumber: 1, pageSize: 8 }).subscribe({
            next: (res) => {
                this.customerDialogResults.set(res.data ?? []);
                this.customerDialogLoading.set(false);
            },
            error: () => this.customerDialogLoading.set(false)
        });
    }

    selectCustomerFromDialog(customer: any): void {
        this.selectCustomer(customer);
        this.closeOverlay();
    }

    openQuickCustomerFromDialog(): void {
        this.closeOverlay();
        this.openQuickCustomerDialog();
    }

    removeAttachedCustomer(): void {
        this.selectedCustomer.set(null);
        this.selectedCustomerModel = null;
        this.clearVehicleSelection();
        this.useCreditBalance.set(false);
        this.creditAmountToApply.set(0);
        this.closeOverlay();
        this.selectWalkInCustomerIfNone();
    }

    // ===== FORMAT CURRENCY =====
    formatCurrency(amount: number): string {
        return this.currencyService.formatCurrency(amount, this.currencyService.selectedCurrency());
    }

    // ===== BARCODE =====
    // The old dedicated "barcode mode" toggle is gone — the Search list-dialog (F2) now falls
    // back to this same exact-code lookup automatically when its filter query has no client-side
    // matches (see onSearchDialogSubmit()), so there's no separate UI mode to switch into.
    processBarcodeInput(): void {
        if (!this.barcodeValue.trim()) return;
        const code = this.barcodeValue.trim();
        this.quickSaleService.getPriceByCode(code).subscribe({
            next: (result) => {
                if (result) {
                    const variantId = result.variantId ?? undefined;
                    const existing = this.cartItems().find((item) => item.partId === result.partId && (item.productVariantId ?? null) === (result.variantId ?? null));
                    const displayName = composeVariantDisplayName(result.name, result.variantName);
                    if (existing) {
                        this.cartItems.update((items) => items.map((item) => (item.partId === result.partId && (item.productVariantId ?? null) === (result.variantId ?? null) ? { ...item, quantity: item.quantity + 1 } : item)));
                        this.messageService.add({ severity: 'info', summary: this.i18n.t('pos.messages.qtyUpdated'), detail: this.i18n.t('pos.messages.qtyUpdatedDetail', { name: displayName }) });
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
                        this.cartItems.update((items) => [...items, newItem]);
                        this.fetchLineInfo(newItem);
                        if (result.unitId) {
                            this.cartUnitSelection.set(this.cartItems().length - 1, result.unitId);
                        }
                        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.added'), detail: this.i18n.t('pos.messages.addedDetail', { name: displayName }) });
                    }
                }
                this.barcodeValue = '';
            },
            error: () => {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.notFound'), detail: this.i18n.t('pos.messages.codeNotFound', { code }) });
                this.barcodeValue = '';
            }
        });
    }

    // ===== PAYMENT METHODS =====
    // Note: the old sidebar's manual "pick a method, then type an amount" flow (selectPaymentMethod()
    // / onPaymentMethodChange()) is superseded by the Tender dialog's one-tap tiles
    // (tenderFullBalance()) and keypad (addKeypadCash()/addKeypadCard()), which set
    // selectedPaymentMethod + paymentInputAmount together and call addNewPayment() directly.
    addNewPayment(): void {
        const amount = this.paymentInputAmount || 0;
        if (amount <= 0) return;

        if (this.selectedPaymentMethod === 'DUE' && this.isWalkInCustomer()) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.dueNotAllowed'), detail: this.i18n.t('pos.messages.dueNotAllowedDetail') });
            return;
        }

        if (this.selectedPaymentMethod !== 'CASH') {
            const creditApplied = this.useCreditBalance() ? this.creditAmountToApply() || 0 : 0;
            if (this.nonCashOrDueTotal() + amount + creditApplied > this.grandTotal() + 0.01) {
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.nonCashOverpay'), detail: this.i18n.t('pos.messages.nonCashOverpayDetail') });
                return;
            }
        }

        const payment: PaymentDetail = {
            method: this.selectedPaymentMethod,
            amount,
            reference: this.paymentReference.trim() || undefined,
            notes: this.paymentNotes.trim() || undefined
        };

        this.payments.update((payments) => [...payments, payment]);
        this.paymentInputAmount = null;
        this.paymentReference = '';
        this.paymentNotes = '';
    }

    requiresReference(method: string): boolean {
        return method === 'CARD' || method === 'MOBILE_BANKING';
    }

    hasDuePayments(): boolean {
        return this.payments().some((p) => p.method === 'DUE');
    }

    sumDuePayments = (sum: number, p: PaymentDetail): number => {
        return sum + p.amount;
    };

    removeNewPayment(index: number): void {
        this.payments.update((payments) => payments.filter((_, i) => i !== index));
    }

    getPaymentIcon(method: string): string {
        const icons: Record<string, string> = {
            CASH: 'pi pi-money-bill',
            CARD: 'pi pi-credit-card',
            MOBILE_BANKING: 'pi pi-mobile',
            DUE: 'pi pi-clock'
        };
        return icons[method] || 'pi pi-wallet';
    }

    getPaymentLabel(method: string): string {
        if (!method) return method;
        const key = `pos.methods.${method}`;
        const label = this.i18n.t(key);
        return label === key ? method : label;
    }

    // ===== SHORTCUT ACTIONS =====
    resetShortcut(): void {
        this.resetForm();
        this.generateInvoiceNumber();
        this.selectWalkInCustomerIfNone();
    }

    resetForm(): void {
        this.cartItems.set([]);
        this.payments.set([]);
        this.selectedCustomer.set(null);
        this.selectedCustomerModel = null;
        this.clearVehicleSelection();
        this.selectedTechnician.set(null);
        this.selectedTechnicianModel = null;
        this.manualDiscountAmount.set(0);
        this.promoCode.set('');
        this.promoResult.set(null);
        this.promoError.set('');
        this.currentInvoiceId.set(null);
        this.saving.set(false);
        this.autoCreatePO = false;
        this.saleNotes = '';
        this.useCreditBalance.set(false);
        this.creditAmountToApply.set(0);
        this.paymentInputAmount = null;
        this.paymentReference = '';
        this.paymentNotes = '';
        this.pricingErrors.clear();
        this.cartUnitSelection.clear();
        this.quickSaleService.clearDraft();
        this.keypadDigits.set('');
        this.appliedManualDiscountPercent.set(0);
        this.bulkDiscountPercent = 0;
        this.showPriceOverrideDialog = false;
        this.priceOverrideUsername = '';
        this.priceOverridePassword = '';
        this.priceOverrideError = '';
        this.priceOverrideApprovalToken = null;
    }

    // ===== PROMO CODE VALIDATION =====
    /** Normalizes the entered code (uppercase) and clears any stale validation result. */
    onPromoCodeChange(value: string): void {
        const normalized = value.toUpperCase();
        this.promoCode.set(normalized);
        if (this.promoResult() || this.promoError()) {
            this.promoResult.set(null);
            this.promoError.set('');
        }
    }

    /** Validates the entered promo code against the live cart subtotal and applies it on success. */
    validatePromo(): void {
        const code = this.promoCode().trim();
        if (!code || this.promoApplying()) return;

        // A previously applied promo stays until the code changes or Apply re-validates.
        this.promoApplying.set(true);
        this.promoError.set('');
        this.discountService
            .resolveCartDiscount(this.subtotal(), code)
            .pipe(finalize(() => this.promoApplying.set(false)))
            .subscribe({
                next: (result) => {
                    if (result.appliedLevel === 'CART' && result.discountAmount > 0) {
                        this.promoResult.set(result);
                        this.messageService.add({
                            severity: 'success',
                            summary: this.i18n.t('pos.messages.promoApplied'),
                            detail: result.discountName ?? code
                        });
                    } else {
                        this.promoResult.set(null);
                        this.promoError.set(this.i18n.t('pos.promoInvalid'));
                    }
                },
                error: () => {
                    this.promoResult.set(null);
                    this.promoError.set(this.i18n.t('pos.promoInvalid'));
                }
            });
    }

    /** Snapshot of everything a parked/drafted sale needs to resume exactly where it left off. */
    private captureSaleState(): Partial<QuickSaleDraft> {
        return {
            customerId: this.selectedCustomer()?.id,
            customerName: this.selectedCustomer()?.fullName,
            customerPhone: this.selectedCustomer()?.phone,
            items: this.cartItems(),
            payments: this.payments(),
            technicianId: this.selectedTechnician()?.id,
            technicianName: this.selectedTechnician()?.name,
            customerVehicleId: this.selectedVehicleId(),
            manualDiscountAmount: this.manualDiscountAmount(),
            promoCode: this.promoCode(),
            total: this.grandTotal(),
            notes: this.saleNotes
        };
    }

    saveDraft(): void {
        this.quickSaleService.saveDraft(this.captureSaleState());
        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.draftSaved') });
    }

    holdSale(): void {
        if (this.cartItems().length === 0) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.noItems'), detail: this.i18n.t('pos.messages.addItemsBeforeHolding') });
            return;
        }
        const holdId = this.quickSaleService.holdSale(this.captureSaleState());
        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.saleHeld'), detail: this.i18n.t('pos.messages.saleHeldDetail', { id: holdId }) });
        this.resetForm();
        this.selectWalkInCustomerIfNone();
    }

    recallHeldSales(): void {
        this.heldSales.set(this.quickSaleService.getHeldSales());
        this.activeOverlay.set('recall');
    }

    recallHeldSale(holdId: string): void {
        const sale = this.quickSaleService.recallHeldSale(holdId);
        if (sale) {
            this.restoreSaleState(sale);
            this.closeOverlay();
            this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.saleRecalled') });
        }
    }

    /** Rebuild the full POS state from a held/drafted sale — cart, payments, customer, technician, vehicle, discount. */
    private restoreSaleState(sale: QuickSaleDraft): void {
        this.resetForm();
        this.cartItems.set(sale.items || []);
        this.payments.set(sale.payments || []);
        this.saleNotes = sale.notes || '';
        this.manualDiscountAmount.set(sale.manualDiscountAmount || 0);
        this.promoCode.set(sale.promoCode || '');

        // Rebuild per-line unit state so the unit dropdowns work after recall
        (sale.items || []).forEach((item, index) => {
            if (!item.unitId) return;
            this.cartUnitSelection.set(index, item.unitId);
            if (!this.compatibleUnitsMap.has(item.partId)) {
                this.unitService.getCompatibleUnits(item.unitId).subscribe({
                    next: (compatibleUnits) => {
                        this.compatibleUnitsMap.set(item.partId, compatibleUnits);
                        this.pokeCartLines();
                    },
                    error: () => {
                        this.compatibleUnitsMap.set(item.partId, this.units());
                        this.pokeCartLines();
                    }
                });
            }
        });

        if (sale.technicianId) {
            const tech = { id: sale.technicianId, name: sale.technicianName || 'Technician' } as TechnicianResponse;
            this.selectedTechnician.set(tech);
            this.selectedTechnicianModel = tech;
        }

        if (sale.customerId) {
            const customerId = sale.customerId;
            this.customerService.getCustomerById(customerId).subscribe({
                next: (freshCustomer) => {
                    this.selectedCustomer.set(freshCustomer);
                    this.selectedCustomerModel = freshCustomer;
                    this.guardWalkInDuePaymentMethod();
                    this.loadCustomerVehicles(customerId, sale.customerVehicleId ?? null);
                },
                error: () => {
                    // Customer fetch failed — fall back to the snapshot so the sale is still usable
                    const snapshot = { id: customerId, fullName: sale.customerName, phone: sale.customerPhone };
                    this.selectedCustomer.set(snapshot);
                    this.selectedCustomerModel = snapshot;
                    this.guardWalkInDuePaymentMethod();
                }
            });
        } else {
            // The draft/held sale never had a customer — default to Walk-in rather than leaving
            // Complete Sale invisibly blocked.
            this.selectWalkInCustomerIfNone();
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
            this.activeOverlay.set('lastSale');
        }
    }

    hasLastSale(): boolean {
        return !!this.quickSaleService.getLastSale();
    }

    printLastSaleReceipt(): void {
        this.closeOverlay();
        if (this.invoicePreviewData) {
            this.thermalReceipt.print(this.invoicePreviewData, (n) => this.formatCurrency(n));
        } else {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.noReceipt'), detail: this.i18n.t('pos.messages.noReceiptDetail') });
        }
    }

    resendNotification(): void {
        const salesOrderId = this.lastSale?.salesOrderId;
        if (!salesOrderId || this.resendingNotification()) return;
        this.resendingNotification.set(true);
        this.quickSaleService.resendInvoiceNotification(salesOrderId).subscribe({
            next: () => {
                this.resendingNotification.set(false);
                this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.sent'), detail: this.i18n.t('pos.messages.sentDetail') });
            },
            error: () => {
                this.resendingNotification.set(false);
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.failed'), detail: this.i18n.t('pos.messages.resendFailed') });
            }
        });
    }

    saveAsQuotation(): void {
        if (this.cartItems().length === 0 || this.saving()) return;
        const customer = this.selectedCustomer();
        if (!customer) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.customerRequired'), detail: this.i18n.t('pos.messages.customerRequiredQuotation') });
            return;
        }
        this.saving.set(true);
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
            // Cart-level discount only — line discounts are already netted into subtotal/items.
            // A validated promo overrides the manual discount (server re-resolves at submit).
            discountAmount: this.cartDiscountAmount(),
            promoCode: this.promoCode() || undefined,
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
                this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.quotationSaved'), detail: result.quoteNumber });
            },
            error: (err) => {
                this.saving.set(false);
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.failed'), detail: err.error?.message || this.i18n.t('pos.messages.quotationFailed') });
            }
        });
    }

    openReprintDialog(): void {
        this.reprintInvoiceNumber = '';
        this.reprintError = '';
        this.activeOverlay.set('reprint');
    }

    reprintReceipt(): void {
        const num = this.reprintInvoiceNumber.trim();
        if (!num || this.reprintLoading()) return;
        this.reprintError = '';
        this.reprintLoading.set(true);
        this.invoicePdfService.getInvoiceByNumber(num).subscribe({
            next: (invoice) => {
                this.reprintLoading.set(false);
                this.closeOverlay();
                this.invoicePdfService.downloadServerPdf(invoice.id, invoice.invoiceNumber).subscribe({
                    error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.downloadFailed'), detail: this.i18n.t('pos.messages.downloadFailedDetail') })
                });
            },
            error: () => {
                this.reprintLoading.set(false);
                this.reprintError = this.i18n.t('pos.reprintNotFound');
            }
        });
    }

    openReturns(): void {
        this.activeOverlay.set('returns');
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
                this.returnLines = (invoice?.lines ?? []).map((l) => ({
                    salesOrderLineId: l.salesOrderLineId,
                    partId: l.partId,
                    partName: l.variantName ? `${l.partName} - ${l.variantName}` : l.partName,
                    partLocalName: l.partLocalName ?? null,
                    soldQty: l.quantity,
                    unitPrice: l.unitPrice,
                    returnQty: l.quantity,
                    selected: true
                }));
            },
            error: () => this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.notFound') })
        });
    }

    /** Running total of the selected return lines — shown in the dialog footer. */
    get returnRefundTotal(): number {
        return this.returnLines.filter((l) => l.selected).reduce((sum, l) => sum + l.unitPrice * (l.returnQty || 0), 0);
    }

    processReturn(): void {
        if (!this.returnInvoice) return;

        const chosen = this.returnLines.filter((l) => l.selected && l.returnQty > 0);

        if (chosen.length === 0) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.nothingToReturn'), detail: this.i18n.t('pos.messages.nothingToReturnDetail') });
            return;
        }

        const invalid = chosen.find((l) => l.returnQty > l.soldQty);
        if (invalid) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.invalidQuantity'), detail: this.i18n.t('pos.messages.invalidQuantityDetail', { max: invalid.soldQty, name: invalid.partName }) });
            return;
        }

        const items = chosen.map((l) => ({
            partId: l.partId,
            salesOrderLineId: l.salesOrderLineId,
            quantity: l.returnQty,
            reason: 'POS quick return'
        }));

        const refundLabel = this.i18n.t(this.returnRefundType === 'STORE_CREDIT' ? 'pos.storeCredit' : 'pos.cashRefund');
        this.confirmationService.confirm({
            message: this.i18n.t('pos.returnConfirmMessage', {
                count: chosen.length,
                amount: this.formatCurrency(this.returnRefundTotal),
                invoice: this.returnInvoice.invoiceNumber,
                refundLabel
            }),
            header: this.i18n.t('pos.messages.confirmReturn'),
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: this.i18n.t('common.actions.confirm'),
            rejectLabel: this.i18n.t('common.actions.cancel'),
            accept: () => {
                this.quickSaleService
                    .processReturn({
                        originalInvoiceNumber: this.returnInvoice.invoiceNumber,
                        refundType: this.returnRefundType,
                        items
                    })
                    .subscribe({
                        next: (res: any) => {
                            this.closeOverlay();
                            this.messageService.add({
                                severity: 'success',
                                summary: this.i18n.t('pos.messages.returnCreated'),
                                detail: this.i18n.t('pos.messages.returnCreatedDetail', { number: res?.returnNumber ?? this.i18n.t('pos.messages.returnFallbackNumber') })
                            });
                        },
                        error: (err) =>
                            this.messageService.add({
                                severity: 'error',
                                summary: this.i18n.t('pos.messages.returnFailed'),
                                detail: extractApiError(err, 'Could not create the return')
                            })
                    });
            }
        });
    }

    openCustomerHistory(): void {
        if (!this.selectedCustomer()) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.selectCustomerFirst') });
            return;
        }
        this.activeOverlay.set('customerHistory');
        this.quickSaleService.getCustomerHistory(this.selectedCustomer()!.id, 10).subscribe({
            next: (history) => this.customerPurchaseHistory.set(history)
        });
    }

    viewCustomerCredit(): void {
        if (!this.selectedCustomer()) return;
        this.activeOverlay.set('customerCredit');
        this.loadingCustomerCredit = true;
        this.quickSaleService.getCustomerCredit(this.selectedCustomer()!.id).subscribe({
            next: (credit) => {
                this.customerCreditInfo = { advanceAmount: credit.advanceAmount || 0, dueBalance: credit.dueBalance || 0 };
                this.loadingCustomerCredit = false;
            },
            error: () => {
                const c = this.selectedCustomer()!;
                this.customerCreditInfo = {
                    advanceAmount: c.advanceAmount || 0,
                    dueBalance: c.dueAmount || 0
                };
                this.loadingCustomerCredit = false;
            }
        });
    }

    openPriceCheck(): void {
        this.activeOverlay.set('priceCheck');
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
                    this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.notFound'), detail: this.i18n.t('pos.messages.noProductForQuery', { code }) });
                }
            },
            error: () => {
                this.priceCheckLoading = false;
                this.priceCheckResult = null;
                this.priceCheckNotFound = true;
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.notFound'), detail: this.i18n.t('pos.messages.noProductForQuery', { code }) });
            }
        });
    }

    addPriceCheckToCart(): void {
        const r = this.priceCheckResult;
        if (!r) return;
        // Reuse selectPart so variant id/name, dedupe and lot-price logic stay in one place.
        const variantSuffix = r.variantName ? ` - ${r.variantName}` : r.variantCode ? ` - ${r.variantCode}` : '';
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
        this.closeOverlay();
    }

    openStockSearch(): void {
        this.activeOverlay.set('stock');
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

        const source =
            this.semanticMode && term
                ? this.partService.searchSemantic(term, page, this.stockSearchPageSize, true)
                : this.partService.getParts({ search: term, pageNumber: page, pageSize: this.stockSearchPageSize, isActive: true, flattenVariants: true });

        source.subscribe({
            next: (res) => {
                this.stockSearchResults = res.data;
                this.stockSearchTotal = res.pagination.totalCount;
                this.stockSearchLoading = false;
                this.loadStockLevels(res.data.map((r) => r.id));
            },
            error: () => {
                this.stockSearchLoading = false;
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.searchFailed') });
            }
        });
    }

    private loadStockLevels(partIds: string[]): void {
        const ids = Array.from(new Set(partIds));
        if (ids.length === 0) return;
        this.quickSaleService.checkMultipleStock(ids.map((partId) => ({ partId, quantity: 1 }))).subscribe({
            next: (results) => results.forEach((r) => this.stockLevels.set(r.partId, r.stockAvailable)),
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

    applyBulkDiscountConfirm(): void {
        const value = Math.max(0, Math.min(100, this.bulkDiscountPercent));
        // A manual % override clears any auto-resolved item discount so the manual value is the
        // single source of truth (backend uses item.discount > 0 as the manual override branch).
        this.cartItems.update((items) => items.map((item) => ({ ...item, discount: value, autoDiscountAmount: value > 0 ? undefined : item.autoDiscountAmount, autoDiscountName: value > 0 ? undefined : item.autoDiscountName })));
        this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.discountApplied'), detail: this.i18n.t('pos.messages.discountAppliedDetail', { value }) });
    }

    // ===== DISCOUNT DIALOG (F6) =====
    /** Preset manual discount rows (design_handoff_pos_sale §2 "Discount" row set) — a real analogue
     *  of the mock's named DISCOUNTS list, adapted to this app's actual promo/manual-% model
     *  (see plan §"Discount dialog (F6)"). */
    readonly discountPresets: number[] = [10, 15, 20, 25];

    openDiscountDialog(): void {
        this.activeOverlay.set('discount');
    }

    /** Applies (or, on a second tap of the already-active preset, removes) a manual % discount —
     *  mirrors the design's "selecting the active one removes it" rule. */
    applyPresetDiscount(percent: number): void {
        const next = this.appliedManualDiscountPercent() === percent ? 0 : percent;
        this.bulkDiscountPercent = next;
        this.applyBulkDiscountConfirm();
        this.appliedManualDiscountPercent.set(next);
        this.closeOverlay();
    }

    /** Toggle for the discount dialog's "use store credit / advance balance" row — the direct
     *  analogue of the mock's loyalty-points redemption row (see plan §"Discount dialog (F6)"). */
    toggleCreditRedeem(): void {
        const next = !this.useCreditBalance();
        this.useCreditBalance.set(next);
        this.creditAmountToApply.set(next ? this.maxCreditApplicable() : 0);
        this.closeOverlay();
    }

    clearCart(): void {
        this.confirmationService.confirm({
            message: this.i18n.t('pos.messages.clearCartMessage'),
            header: this.i18n.t('pos.messages.clearCartHeader'),
            icon: 'pi pi-exclamation-triangle',
            acceptLabel: this.i18n.t('common.actions.clear'),
            rejectLabel: this.i18n.t('common.actions.cancel'),
            accept: () => {
                this.cartItems.set([]);
                this.payments.set([]);
                this.pricingErrors.clear();
            }
        });
    }

    // ===== CHECKOUT =====
    confirmCheckout(): void {
        if (!this.selectedCustomer()) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.customerRequired') });
            return;
        }
        if (this.cartItems().length === 0) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.noItems') });
            return;
        }

        const creditApplied = this.useCreditBalance() ? this.creditAmountToApply() || 0 : 0;
        const totalPaid = this.payments().reduce((sum, p) => sum + p.amount, 0) + creditApplied;
        const remaining = this.grandTotal() - totalPaid;
        const hasDuePayment = this.payments().some((p) => p.method === 'DUE');

        // Reserved Walk-in customer must never carry a due/credit balance (backend enforces this too).
        if (hasDuePayment && this.isWalkInCustomer()) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.dueNotAllowed'), detail: this.i18n.t('pos.messages.dueNotAllowedDetail') });
            return;
        }

        if (remaining > 0.01 && !hasDuePayment) {
            this.messageService.add({ severity: 'warn', summary: this.i18n.t('pos.messages.incompletePayment'), detail: this.i18n.t('pos.messages.incompletePaymentDetail', { amount: this.formatCurrency(remaining) }) });
            return;
        }

        // Require customer for due payments
        if (hasDuePayment && !this.selectedCustomer()?.id) {
            this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.customerRequired'), detail: this.i18n.t('pos.messages.customerRequiredDue') });
            return;
        }

        if (creditApplied > 0) {
            const existingNotes = this.saleNotes;
            this.saleNotes = existingNotes ? `${existingNotes} | Credit: ${this.formatCurrency(creditApplied)}` : `Credit: ${this.formatCurrency(creditApplied)}`;
        }

        // Below-cost lines require manager approval server-side — surfaced here as a heads-up, not a
        // block, since the actual limits (cost floor, per-category margin, MRP ceiling) are only
        // authoritative server-side. Checked two ways, matching the backend: each line's own price,
        // and (separately) whether the cart-level discount alone pushes a line below cost once
        // distributed. If it does need approval, the server rejects with PRICE_OVERRIDE_REQUIRED and
        // onSubmit() opens the approval dialog — see there.
        const belowCostCount = this.belowCostLineCount();
        const cartDiscountBelowCostCount = this.cartDiscountBelowCostLineCount();
        const belowCostWarningCount = Math.max(belowCostCount, cartDiscountBelowCostCount);

        // Explicit pre-completion confirmation — makes change-due visible before the sale is
        // finalized. Print options are already selected in the sidebar.
        const change = this.changeDue();
        let message = this.i18n.t('pos.checkoutConfirmTotal', { amount: this.formatCurrency(this.grandTotal()) });
        if (change > 0) {
            message += `\n• ${this.i18n.t('pos.changeDue')}: ${this.formatCurrency(change)}`;
        }
        if (belowCostWarningCount > 0) {
            message += `\n• ${this.i18n.t('pos.checkoutConfirmBelowCost', { count: belowCostWarningCount })}`;
        }

        this.confirmationService.confirm({
            message,
            header: this.i18n.t('pos.checkoutConfirmTitle'),
            icon: change > 0 || belowCostWarningCount > 0 ? 'pi pi-exclamation-triangle' : 'pi pi-check-circle',
            acceptLabel: this.i18n.t('pos.checkoutConfirmAccept'),
            rejectLabel: this.i18n.t('common.actions.cancel'),
            accept: () => this.onSubmit()
        });
    }

    // ===== SUBMIT SALE =====
    onSubmit(): void {
        if (this.cartItems().length === 0 || !this.selectedCustomer()) return;

        this.saving.set(true);
        const customer = this.selectedCustomer()!;

        // Applied advance balance is sent as a real useAdvanceBalance/advanceAmountToApply request —
        // the API deducts it from the customer's actual advance payment record and marks the invoice
        // paid to that extent. It must NOT also appear as a DUE payment line (the API's
        // paymentsTendered + advancePaymentAmount === invoice.GrandTotal guard would double-count it).
        const creditApplied = this.useCreditBalance() ? this.creditAmountToApply() || 0 : 0;
        const paymentsForRequest = this.payments();

        const request = {
            customerId: customer.id,
            customerName: customer.fullName,
            customerPhone: customer.phone || '',
            technicianId: this.selectedTechnician()?.id,
            customerVehicleId: this.selectedVehicleId() || null,
            paymentResponsibility: this.paymentResponsibility,
            autoCreatePO: this.autoCreatePO,
            items: this.cartItems(),
            payments: paymentsForRequest,
            subtotal: this.subtotal(),
            // Cart-level discount only — line discounts are already netted into subtotal/items.
            // A validated promo overrides the manual discount (server re-resolves at submit).
            discountAmount: this.cartDiscountAmount(),
            discountType: this.promoCode() ? 'PROMO_CODE' : this.manualDiscountAmount() > 0 ? 'FIXED' : 'NONE',
            discountReason: this.promoCode() || (this.manualDiscountAmount() > 0 ? 'Manual discount' : undefined),
            promoCode: this.promoCode() || undefined,
            vatAmount: this.vatAmount(),
            vatPercentage: this.vatPercentage(),
            grandTotal: this.grandTotal(),
            paidAmount: paymentsForRequest.filter((p) => p.method !== 'DUE').reduce((sum, p) => sum + p.amount, 0) + creditApplied,
            dueAmount: paymentsForRequest.filter((p) => p.method === 'DUE').reduce((sum, p) => sum + p.amount, 0),
            notes: this.saleNotes,
            useAdvanceBalance: creditApplied > 0,
            advanceAmountToApply: creditApplied,
            saveAsQuotation: false,
            priceOverrideApprovalToken: this.priceOverrideApprovalToken || undefined
        };

        this.quickSaleService.createQuickSale(request).subscribe({
            next: (result) => {
                this.quickSaleService.saveLastSale(result);
                this.messageService.add({ severity: 'success', summary: this.i18n.t('pos.messages.saleCompleted'), detail: result.invoiceNumber });

                // Capture the receipt BEFORE resetForm() clears the cart/payments it's built from.
                const receipt = this.buildReceiptData(result, request);
                this.invoicePreviewData = receipt;
                this.lastReceiptData = receipt;
                this.currentInvoiceId.set(result.id);
                this.receiptReference.set(result.invoiceNumber);
                this.receiptRows.set(this.buildReceiptRows(result, paymentsForRequest, creditApplied));

                this.saving.set(false);
                this.priceOverrideApprovalToken = null; // single-use, already consumed server-side
                this.resetForm();
                // resetForm() also clears currentInvoiceId (it's part of the "current sale" state it
                // wipes) — restore it so the receipt's Tax-invoice preview can still download/print
                // the just-completed invoice by id (pre-existing gap: the old THERMAL/A4 auto-print
                // branch below used to run before this reset, so it never surfaced in practice).
                this.currentInvoiceId.set(result.id);
                this.generateInvoiceNumber();
                this.selectWalkInCustomerIfNone();

                // Sale-complete overlay (design_handoff_pos_sale §4) — the cashier picks which
                // document to print from there instead of it being auto-printed by a pre-selection.
                this.activeOverlay.set('receipt');
            },
            error: (err) => {
                this.saving.set(false);
                if (err.error?.code === 'PRICE_OVERRIDE_REQUIRED') {
                    // The floor/ceiling limits are only authoritative server-side, so this is the
                    // normal path for a below-cost/above-MRP line, not just an error case — open the
                    // approval dialog instead of a plain failure toast.
                    this.priceOverrideError = '';
                    this.showPriceOverrideDialog = true;
                    return;
                }
                this.messageService.add({ severity: 'error', summary: this.i18n.t('pos.messages.saleFailed'), detail: err.error?.message || this.i18n.t('pos.messages.genericFailed') });
            }
        });
    }

    // ===== PRICE-OVERRIDE APPROVAL =====
    /**
     * Verifies the entered manager credentials via PricingController's request-override endpoint;
     * on success stores the returned single-use token and resubmits the same sale, which now
     * carries it. On failure (wrong credentials, or valid credentials without the approval
     * permission) shows the error and leaves the dialog open to retry.
     */
    submitPriceOverrideApproval(): void {
        if (!this.priceOverrideUsername.trim() || !this.priceOverridePassword) {
            this.priceOverrideError = this.i18n.t('pos.priceOverrideDialogBody');
            return;
        }

        this.priceOverrideSubmitting = true;
        this.priceOverrideError = '';

        this.pricingValidationService.requestPriceOverride(this.priceOverrideUsername.trim(), this.priceOverridePassword).subscribe({
            next: (res) => {
                this.priceOverrideApprovalToken = res.token;
                this.priceOverrideSubmitting = false;
                this.showPriceOverrideDialog = false;
                this.priceOverridePassword = '';
                this.onSubmit();
            },
            error: (err) => {
                this.priceOverrideSubmitting = false;
                this.priceOverrideError = err.error?.message || this.i18n.t('pos.messages.genericFailed');
            }
        });
    }

    cancelPriceOverrideDialog(): void {
        this.showPriceOverrideDialog = false;
        this.priceOverridePassword = '';
        this.priceOverrideError = '';
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
                discount: this.lineDiscountAmount(item),
                total: this.calculateLineTotal(item)
            })),
            subtotal: request.subtotal,
            discountAmount: request.discountAmount,
            vatPercentage: request.vatPercentage,
            vatAmount: request.vatAmount,
            grandTotal: request.grandTotal,
            payments: (request.payments || []).map((p: any) => ({ method: p.method, amount: p.amount, reference: p.reference })),
            paidAmount: request.paidAmount,
            dueAmount: request.dueAmount,
            changeDue: result.changeDue ?? 0,
            notes: request.notes,
            paymentTerms: 'Thank you for your business!'
        };
    }

    /** Receipt-overlay summary rows (design_handoff_pos_sale §4) — built from the just-completed
     *  sale's own response/payment snapshot rather than the live cart signals, since resetForm()
     *  has already cleared those by the time the receipt overlay renders. */
    private buildReceiptRows(result: any, paymentsForRequest: PaymentDetail[], creditApplied: number): PosReceiptRow[] {
        const rows: PosReceiptRow[] = [{ label: this.i18n.t('pos.ticketTotal'), value: this.formatCurrency(result.grandTotal), tone: 'body', bold: false }];
        if (result.discountAmount > 0) {
            rows.push({ label: this.i18n.t('pos.discountApplied'), value: '−' + this.formatCurrency(result.discountAmount), tone: 'green', bold: true });
        }
        const tenderLabels = paymentsForRequest.map((p) => this.getPaymentLabel(p.method));
        if (creditApplied > 0) tenderLabels.push(this.i18n.t('pos.storeCreditLabel'));
        rows.push({ label: this.i18n.t('pos.paidWith'), value: tenderLabels.length ? tenderLabels.join(' + ') : this.i18n.t('pos.methods.CASH'), tone: 'body', bold: false });
        const change = result.changeDue ?? 0;
        if (change > 0.005) rows.push({ label: this.i18n.t('pos.changeGivenLabel'), value: this.formatCurrency(change), tone: 'ink', bold: true });
        return rows;
    }

    // ===== PRINT PREVIEWS (design_handoff_pos_sale §5) =====
    /** "Print receipt" from the receipt overlay — builds the on-screen 80mm preview from the exact
     *  same HTML the hidden-iframe printer uses (see ThermalReceiptService.buildReceiptHtml()), so
     *  the preview can never drift from the real print output. */
    openThermalPreview(): void {
        if (!this.lastReceiptData) return;
        const html = this.thermalReceipt.buildReceiptHtml(this.lastReceiptData, (n) => this.formatCurrency(n));
        this.thermalPreviewHtml.set(this.sanitizer.bypassSecurityTrustHtml(html));
        this.activeOverlay.set('thermal');
    }

    /** "Tax invoice" from the receipt overlay, or "Switch to A4" from the thermal preview — both
     *  hand off to the existing (restyled) InvoicePreviewComponent rather than a second on-screen
     *  A4 renderer (see plan §"Print previews (5)"). */
    openInvoicePreviewFromPos(): void {
        this.closeOverlay();
        this.showInvoicePreview = true;
    }

    /** Per design_handoff_pos_sale §5: "Send to printer" returns to the receipt screen (not a bare
     *  close) so the cashier can still print the other format or tap Next customer from there. */
    sendThermalToPrinter(): void {
        if (this.lastReceiptData) this.thermalReceipt.print(this.lastReceiptData, (n) => this.formatCurrency(n));
        this.activeOverlay.set('receipt');
    }
}
