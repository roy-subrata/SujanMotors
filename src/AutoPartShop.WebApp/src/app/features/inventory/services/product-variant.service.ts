import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { environment } from 'src/environments/environment';

export interface VariantAttributeValue {
  id: string;
  attributeId: string;
  attributeName: string;
  attributeCode: string;
  dataType: string;
  optionId?: string | null;
  optionValue?: string | null;
  valueText?: string | null;
  valueNumber?: number | null;
  valueBool?: boolean | null;
}

export interface ProductVariantResponse {
  id: string;
  partId: string;
  name: string;
  code: string;
  sku?: string | null;
  barcode?: string | null;
  costPrice?: number | null;
  sellingPrice?: number | null;
  currency: string;
  isActive: boolean;
  weightKg?: number | null;
  attributeValues: VariantAttributeValue[];
}

export interface VariantAttributeValueRequest {
  attributeId: string;
  optionId?: string | null;
  valueText?: string | null;
  valueNumber?: number | null;
  valueBool?: boolean | null;
}

export interface CreateVariantRequest {
  name: string;
  code: string;
  sku?: string | null;
  barcode?: string | null;
  costPrice?: number | null;
  sellingPrice?: number | null;
  currency?: string;
  isActive?: boolean;
  weightKg?: number | null;
  attributeValues: VariantAttributeValueRequest[];
}

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly http = inject(HttpClient);

  private url(partId: string): string {
    return `${environment.apiUrl}/v1/products/${partId}/variants`;
  }

  getVariants(partId: string): Observable<ProductVariantResponse[]> {
    return this.http.get<{ data: ProductVariantResponse[] }>(this.url(partId))
      .pipe(map(r => r.data));
  }

  getVariant(partId: string, id: string): Observable<ProductVariantResponse> {
    return this.http.get<{ data: ProductVariantResponse }>(`${this.url(partId)}/${id}`)
      .pipe(map(r => r.data));
  }

  createVariant(partId: string, req: CreateVariantRequest): Observable<ProductVariantResponse> {
    return this.http.post<{ data: ProductVariantResponse }>(this.url(partId), req)
      .pipe(map(r => r.data));
  }

  updateVariant(partId: string, id: string, req: CreateVariantRequest): Observable<ProductVariantResponse> {
    return this.http.put<{ data: ProductVariantResponse }>(`${this.url(partId)}/${id}`, req)
      .pipe(map(r => r.data));
  }

  deleteVariant(partId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.url(partId)}/${id}`);
  }
}
