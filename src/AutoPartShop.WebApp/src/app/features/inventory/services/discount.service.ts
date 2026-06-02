import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface DiscountResponse {
  id: string;
  name: string;
  description?: string;
  type: 'PERCENTAGE' | 'FIXED';
  value: number;
  // Computed by backend: VARIANT | PRODUCT | CART
  scope: 'VARIANT' | 'PRODUCT' | 'CART';
  // Scope is determined by these nullable FKs:
  partId?: string;         // null = CART level
  productVariantId?: string; // null + partId = PRODUCT level; both set = VARIANT level
  promoCode?: string;
  minimumCartAmount?: number;
  startDate: string;
  endDate?: string;
  isActive: boolean;
  createdAt: string;
  modifiedAt?: string;
}

export interface CreateDiscountRequest {
  name: string;
  description?: string;
  type: 'PERCENTAGE' | 'FIXED';
  value: number;
  // Scope determined automatically:
  //   partId = null, variantId = null  → CART
  //   partId = value, variantId = null → PRODUCT
  //   partId = value, variantId = value → VARIANT
  partId?: string;
  productVariantId?: string;
  promoCode?: string;
  minimumCartAmount?: number;
  startDate: string;
  endDate?: string;
}

export interface UpdateDiscountRequest {
  id: string;
  name: string;
  description?: string;
  type: 'PERCENTAGE' | 'FIXED';
  value: number;
  promoCode?: string;
  minimumCartAmount?: number;
  startDate: string;
  endDate?: string;
  isActive: boolean;
}

export interface ResolveDiscountResult {
  discountId?: string;
  discountName?: string;
  discountType?: string;
  discountValue: number;
  discountAmount: number;
  appliedLevel: 'VARIANT' | 'PRODUCT' | 'CART' | 'NONE';
  finalPrice: number;
}

@Injectable({ providedIn: 'root' })
export class DiscountService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/discounts`;

  getAllDiscounts(): Observable<DiscountResponse[]> {
    return this.http.get<DiscountResponse[]>(this.apiUrl);
  }

  getActiveDiscounts(): Observable<DiscountResponse[]> {
    return this.http.get<DiscountResponse[]>(`${this.apiUrl}/active`);
  }

  getDiscountById(id: string): Observable<DiscountResponse> {
    return this.http.get<DiscountResponse>(`${this.apiUrl}/${id}`);
  }

  getDiscountsByPart(partId: string): Observable<DiscountResponse[]> {
    return this.http.get<DiscountResponse[]>(`${this.apiUrl}/part/${partId}`);
  }

  createDiscount(request: CreateDiscountRequest): Observable<DiscountResponse> {
    return this.http.post<DiscountResponse>(this.apiUrl, request);
  }

  updateDiscount(id: string, request: UpdateDiscountRequest): Observable<DiscountResponse> {
    return this.http.put<DiscountResponse>(`${this.apiUrl}/${id}`, request);
  }

  deleteDiscount(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  resolveItemDiscount(partId: string, unitPrice: number, variantId?: string): Observable<ResolveDiscountResult> {
    let params = new HttpParams()
      .set('partId', partId)
      .set('unitPrice', unitPrice.toString());
    if (variantId) params = params.set('variantId', variantId);
    return this.http.get<ResolveDiscountResult>(`${this.apiUrl}/resolve/item`, { params });
  }

  resolveCartDiscount(cartSubtotal: number, promoCode?: string): Observable<ResolveDiscountResult> {
    let params = new HttpParams().set('cartSubtotal', cartSubtotal.toString());
    if (promoCode) params = params.set('promoCode', promoCode);
    return this.http.get<ResolveDiscountResult>(`${this.apiUrl}/resolve/cart`, { params });
  }
}
