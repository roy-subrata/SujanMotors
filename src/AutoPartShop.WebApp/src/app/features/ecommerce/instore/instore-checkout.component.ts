import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { debounceTime, distinctUntilChanged, Subject, switchMap, of, catchError } from 'rxjs';
import { CartService } from '../services/cart.service';
import { OrderService, EcommerceCheckoutResponse, PromoValidationResult } from '../services/order.service';
import { environment } from 'src/environments/environment';

interface CustomerLookup {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  phone: string;
  currentBalance: number;
}

const POS_PAYMENT_METHODS = [
  { label: 'Cash', value: 'CASH', icon: 'pi-money-bill' },
  { label: 'Mobile Banking (bKash/Nagad)', value: 'MOBILE_BANKING', icon: 'pi-mobile' },
  { label: 'Card', value: 'CARD', icon: 'pi-credit-card' },
  { label: 'Bank Transfer', value: 'BANK_TRANSFER', icon: 'pi-building' },
];

@Component({
  selector: 'app-instore-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  styleUrls: ['./instore-checkout.component.css'],
  template: `
    <div class="breadcrumb">
      <a routerLink="/shop"><i class="pi pi-home"></i> Home</a>
      <i class="pi pi-angle-right"></i>
      <a routerLink="/shop/cart">Cart</a>
      <i class="pi pi-angle-right"></i>
      <span>In-Store Checkout</span>
    </div>

    <div class="checkout-container">
      <div class="pos-badge"><i class="pi pi-id-card"></i> In-Store / POS Mode</div>
      <h1>In-Store Checkout</h1>

      <div *ngIf="cartService.isEmpty()" class="empty-state">
        <i class="pi pi-shopping-cart"></i>
        <h3>Cart is empty</h3>
        <a class="btn-primary" routerLink="/shop">Go Shopping</a>
      </div>

      <div *ngIf="!cartService.isEmpty()" class="checkout-layout">
        <div class="checkout-forms">

          <div *ngIf="errorMessage()" class="error-banner">
            <i class="pi pi-exclamation-triangle"></i> {{ errorMessage() }}
          </div>

          <!-- ── Customer Search ── -->
          <div class="form-section">
            <h3><i class="pi pi-search"></i> Find Customer</h3>

            <div class="lookup-row">
              <input
                type="text"
                class="lookup-input"
                [(ngModel)]="searchQuery"
                (ngModelChange)="onSearch($event)"
                placeholder="Type phone number or customer name..."
                autocomplete="off"
              />
              <span class="lookup-spinner" *ngIf="searching()">
                <i class="pi pi-spin pi-spinner"></i>
              </span>
            </div>

            <!-- Results list -->
            <div class="search-results" *ngIf="searchResults().length > 0">
              <div
                class="search-result-item"
                *ngFor="let c of searchResults()"
                (click)="selectCustomer(c)"
              >
                <div class="result-info">
                  <span class="result-name">{{ c.fullName || (c.firstName + ' ' + c.lastName) }}</span>
                  <span class="result-meta">{{ c.phone }}<span *ngIf="c.email"> &nbsp;·&nbsp; {{ c.email }}</span></span>
                </div>
                <div class="result-right">
                  <span *ngIf="c.currentBalance > 0" class="due-badge">Due {{ c.currentBalance | number:'1.0-0' }}</span>
                  <button class="btn-use">Select</button>
                </div>
              </div>
            </div>

            <!-- Not found message -->
            <div class="not-found" *ngIf="notFound()">
              <i class="pi pi-info-circle"></i> No customer found — fill in the details below manually.
            </div>
          </div>

          <!-- ── Customer Details ── -->
          <div class="form-section">
            <h3><i class="pi pi-user"></i> Customer Details</h3>
            <div class="form-grid">
              <div class="form-field full">
                <label>Full Name *</label>
                <input type="text" [(ngModel)]="form.customerName" placeholder="Customer name" [class.error]="errors['customerName']" />
                <span class="field-error" *ngIf="errors['customerName']">{{ errors['customerName'] }}</span>
              </div>
              <div class="form-field">
                <label>Phone *</label>
                <input type="tel" [(ngModel)]="form.customerPhone" placeholder="01712345678" [class.error]="errors['customerPhone']" />
                <span class="field-error" *ngIf="errors['customerPhone']">{{ errors['customerPhone'] }}</span>
              </div>
              <div class="form-field">
                <label>Email</label>
                <input type="email" [(ngModel)]="form.customerEmail" placeholder="optional" />
              </div>
            </div>
          </div>

          <!-- ── Discount ── -->
          <div class="form-section">
            <h3><i class="pi pi-tag"></i> Salesperson Discount</h3>
            <div class="form-grid">
              <div class="form-field">
                <label>Discount Type</label>
                <select [(ngModel)]="form.discountType" (ngModelChange)="onDiscountTypeChange()">
                  <option value="NONE">No Discount</option>
                  <option value="PERCENTAGE">Percentage (%)</option>
                  <option value="FIXED">Fixed Amount</option>
                </select>
              </div>
              <div class="form-field" *ngIf="form.discountType !== 'NONE'">
                <label>{{ form.discountType === 'PERCENTAGE' ? 'Discount %' : 'Discount Amount' }} *</label>
                <input
                  type="number" [(ngModel)]="form.discountValue"
                  [placeholder]="form.discountType === 'PERCENTAGE' ? '0–100' : 'Amount'"
                  min="0" [max]="form.discountType === 'PERCENTAGE' ? 100 : cartService.subtotal()"
                  [class.error]="errors['discountValue']"
                />
                <span class="field-error" *ngIf="errors['discountValue']">{{ errors['discountValue'] }}</span>
              </div>
              <div class="form-field full" *ngIf="form.discountType !== 'NONE'">
                <label>Reason *</label>
                <input type="text" [(ngModel)]="form.discountReason" placeholder="Reason for discount" [class.error]="errors['discountReason']" />
                <span class="field-error" *ngIf="errors['discountReason']">{{ errors['discountReason'] }}</span>
              </div>
            </div>
            <div class="discount-preview" *ngIf="salespersonDiscountAmount() > 0">
              Discount: <strong>{{ cartService.currency() }} {{ salespersonDiscountAmount() | number:'1.0-0' }}</strong>
            </div>
          </div>

          <!-- ── Promo Code ── -->
          <div class="form-section">
            <h3><i class="pi pi-ticket"></i> Promo Code <span class="optional-badge">optional</span></h3>

            <div *ngIf="!promoApplied()" class="promo-row">
              <input
                type="text"
                class="promo-input"
                [(ngModel)]="promoInput"
                placeholder="Enter customer's promo code"
                (keyup.enter)="applyPromo()"
                [disabled]="promoValidating()"
              />
              <button
                class="btn-apply-promo"
                (click)="applyPromo()"
                [disabled]="promoValidating() || !promoInput.trim()"
              >
                <i class="pi" [ngClass]="promoValidating() ? 'pi-spin pi-spinner' : 'pi-check'"></i>
                {{ promoValidating() ? 'Checking...' : 'Apply' }}
              </button>
            </div>

            <div *ngIf="promoError()" class="promo-error">
              <i class="pi pi-times-circle"></i> {{ promoError() }}
            </div>

            <div *ngIf="promoApplied()" class="promo-applied">
              <div class="promo-applied-left">
                <i class="pi pi-check-circle"></i>
                <div>
                  <strong>{{ promoCode() }}</strong> applied
                  <span class="promo-applied-label">— {{ promoLabel() }}</span>
                </div>
              </div>
              <button class="promo-remove-btn" (click)="clearPromo()">
                <i class="pi pi-times"></i> Remove
              </button>
            </div>
          </div>

          <!-- ── Payment ── -->
          <div class="form-section">
            <h3><i class="pi pi-credit-card"></i> Payment</h3>
            <div class="payment-methods">
              <label class="payment-option" *ngFor="let m of paymentMethods" [class.selected]="form.paymentMode === m.value">
                <input type="radio" name="paymentMode" [value]="m.value" [(ngModel)]="form.paymentMode" />
                <i class="pi" [ngClass]="m.icon"></i>
                <span>{{ m.label }}</span>
              </label>
            </div>
            <div class="form-grid" style="margin-top:16px">
              <div class="form-field">
                <label>Amount Paid</label>
                <input type="number" [(ngModel)]="form.amountPaid" placeholder="0" min="0" [max]="grandTotal()" />
                <small class="hint">0 = fully on credit &nbsp;·&nbsp; Max: {{ cartService.currency() }} {{ grandTotal() | number:'1.0-0' }}</small>
                <span class="field-error" *ngIf="errors['amountPaid']">{{ errors['amountPaid'] }}</span>
              </div>
              <div class="form-field" *ngIf="form.paymentMode !== 'CASH'">
                <label>Transaction Reference</label>
                <input type="text" [(ngModel)]="form.paymentReference" placeholder="e.g. bKash TxID" />
              </div>
            </div>
            <div class="due-summary" *ngIf="dueBalance() > 0">
              <i class="pi pi-clock"></i>
              Due balance: <strong>{{ cartService.currency() }} {{ dueBalance() | number:'1.0-0' }}</strong>
              — recorded on customer account.
            </div>
          </div>

          <!-- Notes -->
          <div class="form-section">
            <h3><i class="pi pi-pencil"></i> Notes</h3>
            <textarea
              [(ngModel)]="form.notes" rows="2"
              placeholder="Optional order notes..."
              style="width:100%;padding:10px;border:1.5px solid var(--sf-border);border-radius:8px;resize:vertical;font-size:13px;box-sizing:border-box"
            ></textarea>
          </div>
        </div>

        <!-- ── Order Summary ── -->
        <div class="order-summary">
          <h3>Order Summary</h3>
          <div class="summary-items">
            <div class="summary-item" *ngFor="let item of cartService.items()">
              <div class="summary-item-info">
                <span class="summary-item-name">{{ item.name }}</span>
                <span class="summary-item-variant" *ngIf="item.variantName">{{ item.variantName }}</span>
                <span class="summary-item-qty">x{{ item.quantity }}</span>
              </div>
              <span class="summary-item-price">{{ item.currency }} {{ item.price * item.quantity | number:'1.0-0' }}</span>
            </div>
          </div>
          <div class="summary-divider"></div>
          <div class="summary-row">
            <span>Subtotal</span>
            <strong>{{ cartService.currency() }} {{ cartService.subtotal() | number:'1.0-0' }}</strong>
          </div>
          <div class="summary-row discount-row" *ngIf="salespersonDiscountAmount() > 0">
            <span>Salesperson Discount</span>
            <strong>− {{ cartService.currency() }} {{ salespersonDiscountAmount() | number:'1.0-0' }}</strong>
          </div>
          <div class="summary-row promo-row-summary" *ngIf="promoDiscount() > 0">
            <span style="color:#38a169"><i class="pi pi-ticket" style="font-size:11px"></i> Promo ({{ promoCode() }})</span>
            <strong style="color:#38a169">− {{ cartService.currency() }} {{ promoDiscount() | number:'1.0-0' }}</strong>
          </div>
          <div class="summary-divider"></div>
          <div class="summary-row total">
            <span>Total</span>
            <strong>{{ cartService.currency() }} {{ grandTotal() | number:'1.0-0' }}</strong>
          </div>
          <div class="summary-row" *ngIf="form.amountPaid > 0">
            <span>Paid</span>
            <strong class="paid-amount">{{ cartService.currency() }} {{ form.amountPaid | number:'1.0-0' }}</strong>
          </div>
          <div class="summary-row" *ngIf="dueBalance() > 0">
            <span>Due</span>
            <strong class="due-amount">{{ cartService.currency() }} {{ dueBalance() | number:'1.0-0' }}</strong>
          </div>

          <button class="btn-place-order" (click)="placeOrder()" [disabled]="submitting()">
            <i class="pi" [ngClass]="submitting() ? 'pi-spin pi-spinner' : 'pi-check'"></i>
            {{ submitting() ? 'Processing...' : 'Complete Sale' }}
          </button>
          <a routerLink="/shop/cart" class="back-to-cart">
            <i class="pi pi-arrow-left"></i> Back to Cart
          </a>
        </div>
      </div>
    </div>
  `
})
export class InstoreCheckoutComponent {
  readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);
  private readonly http = inject(HttpClient);

  paymentMethods = POS_PAYMENT_METHODS;
  submitting = signal(false);
  errorMessage = signal<string | null>(null);
  searching = signal(false);
  searchResults = signal<CustomerLookup[]>([]);
  notFound = signal(false);

  // Promo code state
  promoInput = '';
  promoValidating = signal(false);
  promoApplied = signal(false);
  promoDiscount = signal(0);
  promoCode = signal('');
  promoLabel = signal('');
  promoError = signal('');

  searchQuery = '';
  private search$ = new Subject<string>();

  form = {
    customerName: '',
    customerPhone: '',
    customerEmail: '',
    notes: '',
    discountType: 'NONE' as 'NONE' | 'PERCENTAGE' | 'FIXED',
    discountValue: 0,
    discountReason: '',
    paymentMode: 'CASH',
    amountPaid: 0,
    paymentReference: '',
  };

  errors: Record<string, string> = {};

  salespersonDiscountAmount = computed(() => {
    const sub = this.cartService.subtotal();
    if (this.form.discountType === 'PERCENTAGE' && this.form.discountValue > 0)
      return Math.round(sub * this.form.discountValue / 100 * 100) / 100;
    if (this.form.discountType === 'FIXED' && this.form.discountValue > 0)
      return Math.min(this.form.discountValue, sub);
    return 0;
  });

  // Total discount = salesperson discount + promo code discount
  discountAmount = computed(() =>
    Math.min(this.salespersonDiscountAmount() + this.promoDiscount(), this.cartService.subtotal())
  );

  grandTotal = computed(() =>
    Math.max(0, this.cartService.subtotal() - this.discountAmount())
  );

  dueBalance = computed(() =>
    Math.max(0, this.grandTotal() - (this.form.amountPaid || 0))
  );

  constructor() {
    // Fix: catchError INSIDE switchMap so the stream never terminates on 404
    this.search$.pipe(
      debounceTime(400),
      distinctUntilChanged(),
      switchMap(query => {
        const q = query.trim();
        if (q.length < 3) {
          this.searchResults.set([]);
          this.notFound.set(false);
          return of([] as CustomerLookup[]);
        }
        this.searching.set(true);
        this.notFound.set(false);

        const isPhone = /^[\d\+]/.test(q);

        if (isPhone) {
          // Phone search — returns single customer or 404
          return this.http
            .get<CustomerLookup>(`${environment.apiUrl}/v1/customers/search-by-phone?phone=${encodeURIComponent(q)}`)
            .pipe(
              catchError(err => {
                if (err.status === 404) return of(null);
                return of(null);
              })
            );
        } else {
          // Name search — POST /api/v1/customers/list with Search field
          return this.http
            .post<{ data: CustomerLookup[] }>(`${environment.apiUrl}/v1/customers/list`, {
              search: q, pageNumber: 1, pageSize: 8
            })
            .pipe(
              catchError(() => of({ data: [] as CustomerLookup[] }))
            );
        }
      })
    ).subscribe(result => {
      this.searching.set(false);
      if (!result) {
        // Phone search — null means 404
        this.searchResults.set([]);
        this.notFound.set(true);
      } else if (Array.isArray(result)) {
        // of([]) from short query
        this.searchResults.set(result);
        this.notFound.set(false);
      } else if ('data' in result) {
        // Name search — paged result
        const list = (result as any).data as CustomerLookup[];
        this.searchResults.set(list ?? []);
        this.notFound.set(list.length === 0);
      } else {
        // Phone search — single customer object
        this.searchResults.set([result as CustomerLookup]);
        this.notFound.set(false);
      }
    });
  }

  onSearch(value: string): void {
    this.search$.next(value);
  }

  selectCustomer(c: CustomerLookup): void {
    this.form.customerName = c.fullName || `${c.firstName} ${c.lastName}`.trim();
    this.form.customerPhone = c.phone;
    this.form.customerEmail = c.email ?? '';
    this.searchResults.set([]);
    this.notFound.set(false);
    this.searchQuery = '';
  }

  onDiscountTypeChange(): void {
    this.form.discountValue = 0;
    this.form.discountReason = '';
  }

  applyPromo(): void {
    const code = this.promoInput.trim().toUpperCase();
    if (!code) { this.promoError.set('Enter a promo code'); return; }
    this.promoError.set('');
    this.promoValidating.set(true);

    this.orderService.validatePromoCode(code, this.cartService.subtotal()).subscribe({
      next: (result: PromoValidationResult) => {
        this.promoValidating.set(false);
        if (result.valid) {
          this.promoDiscount.set(result.discountAmount ?? 0);
          this.promoCode.set(result.code ?? code);
          this.promoLabel.set(result.description ?? `${result.discountType === 'PERCENTAGE' ? result.discountValue + '%' : this.cartService.currency() + ' ' + result.discountAmount} off`);
          this.promoApplied.set(true);
          this.promoError.set('');
        } else {
          this.promoError.set(result.message ?? 'Invalid promo code');
          this.clearPromo();
        }
      },
      error: () => {
        this.promoValidating.set(false);
        this.promoError.set('Could not validate code. Please try again.');
      }
    });
  }

  clearPromo(): void {
    this.promoApplied.set(false);
    this.promoDiscount.set(0);
    this.promoCode.set('');
    this.promoLabel.set('');
    this.promoInput = '';
  }

  validate(): boolean {
    this.errors = {};
    if (!this.form.customerName.trim()) this.errors['customerName'] = 'Customer name is required';
    if (!this.form.customerPhone.trim()) this.errors['customerPhone'] = 'Phone is required';
    if (this.form.discountType !== 'NONE') {
      if (this.form.discountValue <= 0) this.errors['discountValue'] = 'Enter a value greater than 0';
      if (this.form.discountType === 'PERCENTAGE' && this.form.discountValue > 100)
        this.errors['discountValue'] = 'Cannot exceed 100%';
      if (!this.form.discountReason.trim()) this.errors['discountReason'] = 'Discount reason is required';
    }
    const paid = this.form.amountPaid ?? 0;
    if (paid < 0) this.errors['amountPaid'] = 'Amount paid cannot be negative';
    if (paid > this.grandTotal()) this.errors['amountPaid'] = 'Amount paid exceeds total';
    return Object.keys(this.errors).length === 0;
  }

  placeOrder(): void {
    if (this.cartService.isEmpty()) { this.errorMessage.set('Cart is empty.'); return; }
    if (!this.validate()) return;
    if (this.submitting()) return;

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.orderService.instoreCheckout(
      {
        customerName: this.form.customerName,
        customerPhone: this.form.customerPhone,
        customerEmail: this.form.customerEmail,
        notes: this.form.notes,
        paymentMode: this.form.paymentMode,
        amountPaid: this.form.amountPaid ?? 0,
        paymentReference: this.form.paymentReference || undefined,
        discountType: this.form.discountType,
        discountValue: this.form.discountValue,
        discountReason: this.form.discountReason || undefined,
        promoCode: this.promoCode() || undefined,
      },
      this.cartService.items(),
      this.cartService.currency(),
      this.cartService.sessionId
    ).subscribe({
      next: (response: EcommerceCheckoutResponse) => {
        this.cartService.clearCart();
        this.router.navigate(['/shop/order-confirmation', response.soNumber], {
          state: { order: response },
        });
      },
      error: (err) => {
        this.submitting.set(false);
        this.errorMessage.set(err?.error?.message ?? 'An error occurred. Please try again.');
      }
    });
  }
}
