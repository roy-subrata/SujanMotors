import { Component, inject, model } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { DrawerModule } from 'primeng/drawer';
import { CartService } from '../../services/cart.service';
import { AuthService } from '../../../../shared/services/auth.service';

@Component({
  selector: 'app-cart-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule, DrawerModule],
  templateUrl: './cart-sidebar.component.html',
  styleUrls: ['./cart-sidebar.component.css'],
})
export class CartSidebarComponent {
  readonly cartService = inject(CartService);
  readonly authService = inject(AuthService);

  visible = model(false);

  get checkoutUrl(): string {
    return this.authService.isLoggedIn() ? '/shop/instore-checkout' : '/shop/checkout';
  }

  close(): void {
    this.visible.set(false);
  }

  increment(item: { partId: string; variantId?: string | null; quantity: number }): void {
    this.cartService.updateQuantity(item.partId, item.variantId, item.quantity + 1);
  }

  decrement(item: { partId: string; variantId?: string | null; quantity: number }): void {
    this.cartService.updateQuantity(item.partId, item.variantId, item.quantity - 1);
  }

  remove(item: { partId: string; variantId?: string | null }): void {
    this.cartService.removeItem(item.partId, item.variantId);
  }
}
