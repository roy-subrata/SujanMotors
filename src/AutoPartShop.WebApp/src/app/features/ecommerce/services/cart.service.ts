import { computed, effect, Injectable, signal } from '@angular/core';
import { CartItem, Order } from '../models/cart.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly CART_KEY = 'sm_ecommerce_cart';
  private readonly ORDERS_KEY = 'sm_ecommerce_orders';

  readonly items = signal<CartItem[]>(this.loadCart());
  readonly itemCount = computed(() => this.items().reduce((sum, i) => sum + i.quantity, 0));
  readonly subtotal = computed(() => this.items().reduce((sum, i) => sum + i.price * i.quantity, 0));
  readonly currency = computed(() => this.items()[0]?.currency ?? 'BDT');
  readonly isEmpty = computed(() => this.items().length === 0);

  constructor() {
    effect(() => {
      localStorage.setItem(this.CART_KEY, JSON.stringify(this.items()));
    });
  }

  addItem(item: CartItem): void {
    const current = this.items();
    const existing = current.find(
      i => i.partId === item.partId && i.variantId === item.variantId
    );
    if (existing) {
      this.items.set(
        current.map(i =>
          i.partId === item.partId && i.variantId === item.variantId
            ? { ...i, quantity: i.quantity + item.quantity }
            : i
        )
      );
    } else {
      this.items.set([...current, item]);
    }
  }

  removeItem(partId: string, variantId?: string | null): void {
    this.items.set(
      this.items().filter(i => !(i.partId === partId && i.variantId === variantId))
    );
  }

  updateQuantity(partId: string, variantId: string | null | undefined, quantity: number): void {
    if (quantity <= 0) {
      this.removeItem(partId, variantId);
      return;
    }
    this.items.set(
      this.items().map(i =>
        i.partId === partId && i.variantId === variantId
          ? { ...i, quantity }
          : i
      )
    );
  }

  clearCart(): void {
    this.items.set([]);
  }

  placeOrder(order: Order): void {
    const orders = this.loadOrders();
    orders.push(order);
    localStorage.setItem(this.ORDERS_KEY, JSON.stringify(orders));
    this.clearCart();
  }

  getOrder(orderId: string): Order | undefined {
    return this.loadOrders().find(o => o.orderId === orderId);
  }

  private loadCart(): CartItem[] {
    try {
      const data = localStorage.getItem(this.CART_KEY);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  }

  private loadOrders(): Order[] {
    try {
      const data = localStorage.getItem(this.ORDERS_KEY);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  }
}
