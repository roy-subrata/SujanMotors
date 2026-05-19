import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface ActivePriceResponse {
  partId: string;
  productVariantId?: string | null;
  sellingPrice: number;
  currency: string;
  source: 'VARIANT_SCHEDULE' | 'PRODUCT_SCHEDULE';
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

  private baseUrl(productId: string): string {
    return `${environment.apiUrl}/v1/products/${productId}/price-schedule`;
  }

  getActivePrice(partId: string, variantId?: string): Observable<ActivePriceResponse | null> {
    let params = new HttpParams();
    if (variantId) params = params.set('variantId', variantId);
    return this.http.get<{ data: ActivePriceResponse | null }>(`${this.baseUrl(partId)}/active`, { params })
      .pipe(map(r => r.data));
  }

  getPriceHistory(partId: string): Observable<PriceHistoryRecord[]> {
    return this.http.get<{ data: PriceHistoryRecord[] }>(this.baseUrl(partId))
      .pipe(map(r => r.data));
  }

  setPrice(partId: string, request: SetPriceRequest, variantId?: string): Observable<PriceHistoryRecord> {
    let params = new HttpParams();
    if (variantId) params = params.set('variantId', variantId);
    return this.http.post<{ data: PriceHistoryRecord }>(this.baseUrl(partId), request, { params })
      .pipe(map(r => r.data));
  }
}
