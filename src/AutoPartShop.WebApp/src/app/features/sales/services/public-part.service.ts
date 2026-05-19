import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface PublicPartResponse {
  id: string;
  name: string;
  displayName: string;
  description: string;
  partNumber: string;
  sku: string;
  categoryId: string;
  categoryName: string;
  brandId: string | null;
  brandName: string | null;
  brandCode: string | null;
  unitId: string | null;
  unitName: string | null;
  sellingPrice: number;
  effectiveSellingPrice: number;
  // Variant fields — populated when flattenVariants = true
  hasVariants: boolean;
  isVariant: boolean;
  variantId?: string | null;
  variantName?: string | null;
  variantCode?: string | null;
  variantSKU?: string | null;
  variantBarcode?: string | null;
  pricingMode?: string | null;
  minimumStock: number;
  isActive: boolean;
  hasWarranty: boolean;
  warrantyPeriodMonths: number | null;
  warrantyType: string | null;
  warrantyTerms: string | null;
  warrantyCertificateTemplate: string | null;
}

export interface PublicPartsQuery {
  search: string;
  pageSize: number;
  pageNumber: number;
  isActive?: boolean;
  flattenVariants?: boolean;
}

export interface PaginatedResponse<T> {
  data: T[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({ providedIn: 'root' })
export class PublicPartService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/products`;

  getActiveParts(): Observable<PublicPartResponse[]> {
    return this.http.get<{ data: PublicPartResponse[] }>(this.apiUrl, {
      params: new HttpParams().set('isActive', 'true').set('pageSize', '500')
    }).pipe(map(r => r.data));
  }

  getParts(query: PublicPartsQuery): Observable<PaginatedResponse<PublicPartResponse>> {
    let params = new HttpParams()
      .set('search', query.search ?? '')
      .set('page', query.pageNumber.toString())
      .set('pageSize', query.pageSize.toString())
      .set('flattenVariants', (query.flattenVariants ?? false).toString());
    if (query.isActive != null) params = params.set('isActive', query.isActive.toString());
    return this.http.get<{ data: PublicPartResponse[]; pagination: any }>(this.apiUrl, { params })
      .pipe(map(r => ({
        data: r.data,
        pagination: { ...r.pagination, pageNumber: r.pagination.page }
      })));
  }

  getPartById(id: string): Observable<PublicPartResponse> {
    return this.http.get<{ data: PublicPartResponse }>(`${this.apiUrl}/${id}`)
      .pipe(map(r => r.data));
  }

  getLotPrice(partId: string): Observable<{ productId: string; sellingPrice: number; lotSellingPrice: number | null; fallbackSellingPrice: number; hasLotPrice: boolean; stockAvailable: number }> {
    return this.http.get<{ data: any }>(`${this.apiUrl}/${partId}/lot-price`)
      .pipe(map(r => r.data));
  }
}
