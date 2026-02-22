import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { CartService } from '../services/cart.service';
import { CartItem } from '../models/cart.model';

@Component({
  selector: 'app-ecommerce-cart',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './ecommerce-cart.component.html',
  styleUrls: ['./ecommerce-cart.component.css'],
})
export class EcommerceCartComponent {
  readonly cartService = inject(CartService);

  increment(item: CartItem): void {
    this.cartService.updateQuantity(item.partId, item.variantId, item.quantity + 1);
  }

  decrement(item: CartItem): void {
    this.cartService.updateQuantity(item.partId, item.variantId, item.quantity - 1);
  }

  remove(item: CartItem): void {
    this.cartService.removeItem(item.partId, item.variantId);
  }

  clearCart(): void {
    this.cartService.clearCart();
  }
}
