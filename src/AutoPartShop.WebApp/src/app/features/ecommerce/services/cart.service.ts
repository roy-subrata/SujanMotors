import { computed, effect, inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CartItem } from '../models/cart.model';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly CART_KEY = 'sm_ecommerce_cart';
  private readonly SESSION_KEY = 'sm_cart_session';
  private readonly baseUrl = `${environment.apiUrl}/ecommerce/stock`;

  readonly items = signal<CartItem[]>(this.loadCart());
  readonly itemCount = computed(() => this.items().reduce((sum, i) => sum + i.quantity, 0));
  readonly subtotal = computed(() => this.items().reduce((sum, i) => sum + i.price * i.quantity, 0));
  readonly currency = computed(() => this.items()[0]?.currency ?? 'BDT');
  readonly isEmpty = computed(() => this.items().length === 0);
  readonly sidebarOpen = signal(false);

  readonly sessionId: string = this.getOrCreateSessionId();

  constructor() {
    effect(() => {
      localStorage.setItem(this.CART_KEY, JSON.stringify(this.items()));
    });
  }

  // Returns observable so callers can react to out-of-stock errors
  addItem(item: CartItem): Observable<boolean> {
    const current = this.items();
    const existing = current.find(i => i.partId === item.partId && i.variantId === item.variantId);

    return this.http.post<{ reserved: boolean; available?: number }>(
      `${this.baseUrl}/reserve`,
      {
        sessionId: this.sessionId,
        partId: item.partId,
        variantId: item.variantId ?? null,
        quantity: item.quantity,
      }
    ).pipe(
      map(() => {
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
        return true;
      })
    );
  }

  removeItem(partId: string, variantId?: string | null): void {
    this.items.set(this.items().filter(i => !(i.partId === partId && i.variantId === variantId)));

    this.http.post(`${this.baseUrl}/release`, {
      sessionId: this.sessionId,
      partId,
      variantId: variantId ?? null,
    }).pipe(catchError(() => of(null))).subscribe();
  }

  // Sends the new TOTAL quantity to the reserve endpoint.
  // The backend handles both increases (reserve more) and decreases (release excess).
  updateQuantity(partId: string, variantId: string | null | undefined, newQuantity: number): void {
    if (newQuantity <= 0) {
      this.removeItem(partId, variantId);
      return;
    }

    const current = this.items();
    const existing = current.find(i => i.partId === partId && i.variantId === variantId);
    if (!existing) return;

    this.http.post(`${this.baseUrl}/reserve`, {
      sessionId: this.sessionId,
      partId,
      variantId: variantId ?? null,
      quantity: newQuantity,   // always send full desired total — backend computes delta
    }).pipe(
      map(() => {
        this.items.set(current.map(i =>
          i.partId === partId && i.variantId === variantId ? { ...i, quantity: newQuantity } : i
        ));
      }),
      catchError(() => of(null)) // out of stock — don't update
    ).subscribe();
  }

  clearCart(): void {
    if (this.items().length === 0) return;
    this.items.set([]);

    this.http.post(`${this.baseUrl}/release-session`, {
      sessionId: this.sessionId,
    }).pipe(catchError(() => of(null))).subscribe();
  }

  private getOrCreateSessionId(): string {
    let id = localStorage.getItem(this.SESSION_KEY);
    if (!id) {
      id = crypto.randomUUID();
      localStorage.setItem(this.SESSION_KEY, id);
    }
    return id;
  }

  private loadCart(): CartItem[] {
    try {
      const data = localStorage.getItem(this.CART_KEY);
      return data ? JSON.parse(data) : [];
    } catch {
      return [];
    }
  }
}
