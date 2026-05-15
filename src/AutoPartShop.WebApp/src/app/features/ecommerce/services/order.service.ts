import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, of } from 'rxjs';
import { environment } from 'src/environments/environment';
import { CartItem } from '../models/cart.model';

export interface PromoValidationResult {
  valid: boolean;
  message?: string;
  code?: string;
  discountType?: string;
  discountValue?: number;
  discountAmount?: number;
  finalTotal?: number;
  description?: string;
  minimumCartAmount?: number | null;
}

export interface EcommerceCheckoutRequest {
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  shippingAddress: string;
  shippingCity: string;
  shippingPostalCode: string;
  shippingCountry: string;
  notes: string;
  currency: string;
  sessionId: string;
  paymentMode: string;
  amountPaid: number;
  paymentReference?: string;
  promoCode?: string;
  items: EcommerceOrderItem[];
}

export interface EcommerceOrderItem {
  partId: string;
  variantId?: string | null;
  quantity: number;
  unitPrice: number;
}

export interface EcommerceCheckoutResponse {
  salesOrderId: string;
  soNumber: string;
  customerName: string;
  grandTotal: number;
  amountPaid: number;
  dueBalance: number;
  currency: string;
  status: string;
  paymentStatus: string;
  invoiceNumber: string;
  channel: string;
  discountAmount?: number;
  discountType?: string;
  discountReason?: string;
  promoDiscountAmount?: number;
  appliedPromoCode?: string;
}

export interface InstoreCheckoutRequest extends EcommerceCheckoutRequest {
  discountType: 'NONE' | 'PERCENTAGE' | 'FIXED';
  discountValue: number;
  discountReason?: string;
  // promoCode inherited from EcommerceCheckoutRequest
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/ecommerce`;

  validatePromoCode(code: string, cartTotal: number): Observable<PromoValidationResult> {
    const params = `code=${encodeURIComponent(code)}&cartTotal=${cartTotal}`;
    return this.http
      .get<PromoValidationResult>(`${this.baseUrl}/promo/validate?${params}`)
      .pipe(catchError(() => of({ valid: false, message: 'Could not validate promo code' })));
  }

  getOrderBySoNumber(soNumber: string): Observable<EcommerceCheckoutResponse | null> {
    return this.http.get<EcommerceCheckoutResponse>(`${this.baseUrl}/orders/${soNumber}`).pipe(
      catchError(() => of(null))
    );
  }

  checkout(
    formData: {
      customerName: string; customerEmail: string; customerPhone: string;
      shippingAddress: string; shippingCity: string; shippingPostalCode: string; shippingCountry: string;
      notes: string; paymentMethod: string; paymentReference?: string;
      promoCode?: string; promoDiscountAmount?: number;
    },
    cartItems: CartItem[],
    currency: string,
    sessionId: string
  ): Observable<EcommerceCheckoutResponse> {
    const isCod = !formData.paymentMethod || formData.paymentMethod === 'CASH';
    const subtotal = cartItems.reduce((sum, i) => sum + i.price * i.quantity, 0);
    const promoDiscount = formData.promoDiscountAmount ?? 0;
    const grandTotal = Math.max(0, subtotal - promoDiscount);

    const request: EcommerceCheckoutRequest = {
      customerName: formData.customerName,
      customerEmail: formData.customerEmail,
      customerPhone: formData.customerPhone,
      shippingAddress: formData.shippingAddress,
      shippingCity: formData.shippingCity,
      shippingPostalCode: formData.shippingPostalCode,
      shippingCountry: formData.shippingCountry,
      notes: formData.notes,
      currency,
      sessionId,
      paymentMode: formData.paymentMethod || 'CASH',
      amountPaid: isCod ? 0 : grandTotal,
      paymentReference: formData.paymentReference,
      promoCode: formData.promoCode || undefined,
      items: cartItems.map(item => ({
        partId: item.partId,
        variantId: item.variantId ?? null,
        quantity: item.quantity,
        unitPrice: item.price,
      })),
    };

    return this.http.post<EcommerceCheckoutResponse>(`${this.baseUrl}/checkout`, request);
  }

  instoreCheckout(
    formData: {
      customerName: string; customerEmail: string; customerPhone: string;
      shippingAddress?: string; shippingCity?: string; notes?: string;
      paymentMode: string; amountPaid: number; paymentReference?: string;
      discountType: 'NONE' | 'PERCENTAGE' | 'FIXED'; discountValue: number; discountReason?: string;
      promoCode?: string;
    },
    cartItems: CartItem[],
    currency: string,
    sessionId: string
  ): Observable<EcommerceCheckoutResponse> {
    const request: InstoreCheckoutRequest = {
      customerName: formData.customerName,
      customerEmail: formData.customerEmail,
      customerPhone: formData.customerPhone,
      shippingAddress: formData.shippingAddress ?? '',
      shippingCity: formData.shippingCity ?? '',
      shippingPostalCode: '',
      shippingCountry: 'Bangladesh',
      notes: formData.notes ?? '',
      currency,
      sessionId,
      paymentMode: formData.paymentMode,
      amountPaid: formData.amountPaid,
      paymentReference: formData.paymentReference,
      discountType: formData.discountType,
      discountValue: formData.discountValue,
      discountReason: formData.discountReason,
      promoCode: formData.promoCode || undefined,
      items: cartItems.map(item => ({
        partId: item.partId,
        variantId: item.variantId ?? null,
        quantity: item.quantity,
        unitPrice: item.price,
      })),
    };
    return this.http.post<EcommerceCheckoutResponse>(`${this.baseUrl}/instore-checkout`, request);
  }
}
