import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { CartService } from '../services/cart.service';
import { CheckoutFormData, Order } from '../models/cart.model';
import { CUSTOMER_PAYMENT_METHODS, PaymentMethodOption } from '../../../shared/constants/payment-methods.constants';

@Component({
  selector: 'app-ecommerce-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './ecommerce-checkout.component.html',
  styleUrls: ['./ecommerce-checkout.component.css'],
})
export class EcommerceCheckoutComponent {
  readonly cartService = inject(CartService);
  private readonly router = inject(Router);

  paymentMethods: PaymentMethodOption[] = CUSTOMER_PAYMENT_METHODS;

  form: CheckoutFormData = {
    customer: { fullName: '', email: '', phone: '' },
    shipping: { address: '', city: '', postalCode: '', country: 'Bangladesh', notes: '' },
    paymentMethod: 'CASH',
  };

  errors: Record<string, string> = {};

  validate(): boolean {
    this.errors = {};
    if (!this.form.customer.fullName.trim()) this.errors['fullName'] = 'Full name is required';
    if (!this.form.customer.email.trim()) this.errors['email'] = 'Email is required';
    if (!this.form.customer.phone.trim()) this.errors['phone'] = 'Phone is required';
    if (!this.form.shipping.address.trim()) this.errors['address'] = 'Address is required';
    if (!this.form.shipping.city.trim()) this.errors['city'] = 'City is required';
    return Object.keys(this.errors).length === 0;
  }

  placeOrder(): void {
    if (!this.validate()) return;
    if (this.cartService.isEmpty()) return;

    const order: Order = {
      orderId: crypto.randomUUID(),
      items: [...this.cartService.items()],
      subtotal: this.cartService.subtotal(),
      currency: this.cartService.currency(),
      customer: { ...this.form.customer },
      shipping: { ...this.form.shipping },
      paymentMethod: this.form.paymentMethod,
      createdAt: new Date().toISOString(),
      status: 'confirmed',
    };

    this.cartService.placeOrder(order);
    this.router.navigate(['/shop/order-confirmation', order.orderId]);
  }
}
