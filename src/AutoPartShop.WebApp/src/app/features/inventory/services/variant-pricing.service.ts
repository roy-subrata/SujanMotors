import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface ActivePriceResponse {
  partId: string;
  productVariantId?: string | null;
  sellingPrice: number;
  currency: string;
  source: 'VARIANT_HISTORY' | 'PRODUCT_HISTORY';
  validFrom: string;
  validTo?: string | null;
}

export interface PriceHistoryRecord {
  id: string;
  partId: string;
  productVariantId?: string | null;
  sellingPrice: number;
  currency: string;
  startDate: string;
  endDate?: string | null;
  isActive: boolean;
  reason?: string | null;
  createdAt: string;
  createdBy?: string | null;
}

export interface SetPriceRequest {
  sellingPrice: number;
  startDate: string;
  currency: string;
  reason?: string;
}

@Injectable({ providedIn: 'root' })
export class VariantPricingService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/variant-pricing`;

  /** Get current active price — variant-specific first, then base product fallback */
  getActivePrice(partId: string, variantId?: string): Observable<ActivePriceResponse> {
    let params = new HttpParams();
    if (variantId) params = params.set('variantId', variantId);
    return this.http.get<ActivePriceResponse>(`${this.baseUrl}/${partId}/active`, { params });
  }

  /** Full price history for a part (all scopes, newest first) */
  getPriceHistory(partId: string): Observable<PriceHistoryRecord[]> {
    return this.http.get<PriceHistoryRecord[]>(`${this.baseUrl}/${partId}/history`);
  }

  /** Set a new price — closes previous active price automatically */
  setPrice(partId: string, request: SetPriceRequest, variantId?: string): Observable<PriceHistoryRecord> {
    let params = new HttpParams();
    if (variantId) params = params.set('variantId', variantId);
    return this.http.post<PriceHistoryRecord>(`${this.baseUrl}/${partId}/set-price`, request, { params });
  }
}
