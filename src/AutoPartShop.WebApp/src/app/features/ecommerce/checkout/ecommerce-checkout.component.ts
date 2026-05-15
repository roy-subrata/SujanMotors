import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../services/cart.service';
import { OrderService, EcommerceCheckoutResponse, PromoValidationResult } from '../services/order.service';
import { CustomerAuthService } from '../services/customer-auth.service';
import { CUSTOMER_PAYMENT_METHODS, PaymentMethodOption } from '../../../shared/constants/payment-methods.constants';

interface CheckoutForm {
  shippingAddress: string;
  shippingCity: string;
  shippingPostalCode: string;
  shippingCountry: string;
  notes: string;
  paymentMethod: string;
  paymentReference: string;
}

@Component({
  selector: 'app-ecommerce-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './ecommerce-checkout.component.html',
  styleUrls: ['./ecommerce-checkout.component.css'],
})
export class EcommerceCheckoutComponent {
  readonly cartService = inject(CartService);
  readonly customerAuthService = inject(CustomerAuthService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  paymentMethods: PaymentMethodOption[] = CUSTOMER_PAYMENT_METHODS;
  submitting = signal(false);
  errorMessage = signal<string | null>(null);

  form: CheckoutForm = {
    shippingAddress: '',
    shippingCity: '',
    shippingPostalCode: '',
    shippingCountry: 'Bangladesh',
    notes: '',
    paymentMethod: 'CASH',
    paymentReference: '',
  };

  // ── Promo code ──────────────────────────────────────────────────────────
  promoInput = '';
  promoValidating = signal(false);
  promoApplied = signal(false);
  promoDiscount = signal(0);
  promoCode = signal('');
  promoLabel = signal('');
  promoError = signal('');

  grandTotal = computed(() =>
    Math.max(0, this.cartService.subtotal() - this.promoDiscount())
  );

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

  // ── Form ─────────────────────────────────────────────────────────────────

  get requiresReference(): boolean {
    return !!this.form.paymentMethod && this.form.paymentMethod !== 'CASH';
  }

  errors: Record<string, string> = {};

  validate(): boolean {
    this.errors = {};
    if (!this.form.shippingAddress.trim()) this.errors['shippingAddress'] = 'Address is required';
    if (!this.form.shippingCity.trim()) this.errors['shippingCity'] = 'City is required';
    if (this.requiresReference && !this.form.paymentReference.trim())
      this.errors['paymentReference'] = 'Transaction reference is required for this payment method';
    return Object.keys(this.errors).length === 0;
  }

  placeOrder(): void {
    if (this.cartService.isEmpty()) {
      this.errorMessage.set('Your cart is empty. Add items before placing an order.');
      return;
    }
    if (!this.validate()) return;
    if (this.submitting()) return;

    const customer = this.customerAuthService.currentCustomer();
    if (!customer) {
      this.router.navigate(['/shop/login'], { queryParams: { returnUrl: '/shop/checkout' } });
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);

    this.orderService
      .checkout(
        {
          customerName: customer.fullName,
          customerEmail: customer.email,
          customerPhone: customer.phone,
          shippingAddress: this.form.shippingAddress,
          shippingCity: this.form.shippingCity,
          shippingPostalCode: this.form.shippingPostalCode,
          shippingCountry: this.form.shippingCountry,
          notes: this.form.notes,
          paymentMethod: this.form.paymentMethod,
          paymentReference: this.form.paymentReference || undefined,
          promoCode: this.promoCode() || undefined,
          promoDiscountAmount: this.promoDiscount(),
        },
        this.cartService.items(), this.cartService.currency(), this.cartService.sessionId)
      .subscribe({
        next: (response: EcommerceCheckoutResponse) => {
          this.cartService.clearCart();
          this.router.navigate(['/shop/order-confirmation', response.soNumber], {
            state: { order: response },
          });
        },
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(
            err?.error?.message ?? 'An error occurred while placing your order. Please try again.'
          );
        },
      });
  }
}
