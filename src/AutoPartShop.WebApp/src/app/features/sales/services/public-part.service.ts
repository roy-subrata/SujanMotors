import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
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
  private readonly apiUrl = `${environment.apiUrl}/parts/public`;

  getActiveParts(): Observable<PublicPartResponse[]> {
    return this.http.get<PublicPartResponse[]>(`${this.apiUrl}/active`);
  }

  getParts(query: PublicPartsQuery): Observable<PaginatedResponse<PublicPartResponse>> {
    return this.http.post<PaginatedResponse<PublicPartResponse>>(`${this.apiUrl}/list`, query);
  }

  getPartById(id: string): Observable<PublicPartResponse> {
    return this.http.get<PublicPartResponse>(`${this.apiUrl}/${id}`);
  }

  /** Returns the FIFO lot selling price. Falls back to Part.SellingPrice when no active lot has a price. */
  getLotPrice(partId: string): Observable<{ partId: string; sellingPrice: number; lotSellingPrice: number | null; fallbackSellingPrice: number; hasLotPrice: boolean; stockAvailable: number }> {
    return this.http.get<any>(`${this.apiUrl}/${partId}/lot-price`);
  }
}
