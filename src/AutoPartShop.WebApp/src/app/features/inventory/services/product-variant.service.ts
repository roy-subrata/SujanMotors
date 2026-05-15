import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
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
  widthCm?: number | null;
  heightCm?: number | null;
  depthCm?: number | null;
  attributeValues: VariantAttributeValueRequest[];
}

@Injectable({ providedIn: 'root' })
export class ProductVariantService {
  private readonly http = inject(HttpClient);

  private url(partId: string): string {
    return `${environment.apiUrl}/parts/${partId}/variants`;
  }

  getVariants(partId: string): Observable<ProductVariantResponse[]> {
    return this.http.get<ProductVariantResponse[]>(this.url(partId));
  }

  getVariant(partId: string, id: string): Observable<ProductVariantResponse> {
    return this.http.get<ProductVariantResponse>(`${this.url(partId)}/${id}`);
  }

  createVariant(partId: string, req: CreateVariantRequest): Observable<ProductVariantResponse> {
    return this.http.post<ProductVariantResponse>(this.url(partId), req);
  }

  updateVariant(partId: string, id: string, req: CreateVariantRequest): Observable<ProductVariantResponse> {
    return this.http.put<ProductVariantResponse>(`${this.url(partId)}/${id}`, req);
  }

  deleteVariant(partId: string, id: string): Observable<void> {
    return this.http.delete<void>(`${this.url(partId)}/${id}`);
  }
}
