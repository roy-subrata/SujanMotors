import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from 'src/environments/environment';

// ── Models ────────────────────────────────────────────────────────────────────

export interface BrandResponse {
  id: string;
  name: string;
  code: string;
  description: string | null;
  logoUrl: string | null;
  website: string | null;
  country: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  modifiedAt: string | null;
}

export interface CreateBrandRequest {
  name: string;
  code: string;
  description?: string | null;
  logoUrl?: string | null;
  website?: string | null;
  country?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  displayOrder?: number;
  isActive?: boolean;
}

export interface UpdateBrandRequest {
  name: string;
  code: string;
  description?: string | null;
  logoUrl?: string | null;
  website?: string | null;
  country?: string | null;
  contactEmail?: string | null;
  contactPhone?: string | null;
  displayOrder?: number;
  isActive?: boolean;
}

export interface BrandQuery {
  search?: string;
  isActive?: boolean | null;
  country?: string | null;
  page?: number;
  pageSize?: number;
}

export interface PaginationMeta {
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface PagedBrandResponse {
  data: BrandResponse[];
  pagination: PaginationMeta;
}

// ── Service ───────────────────────────────────────────────────────────────────

@Injectable({ providedIn: 'root' })
export class BrandService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/brands`;

  /** List brands — all filters via query params. */
  getBrands(query: BrandQuery): Observable<PagedBrandResponse> {
    let params = new HttpParams()
      .set('page', String(query.page ?? 1))
      .set('pageSize', String(query.pageSize ?? 10));

    if (query.search) params = params.set('search', query.search);
    if (query.isActive !== null && query.isActive !== undefined)
      params = params.set('isActive', String(query.isActive));
    if (query.country) params = params.set('country', query.country);

    return this.http.get<PagedBrandResponse>(this.apiUrl, { params });
  }

  /** Get a single brand by ID. */
  getBrandById(id: string): Observable<{ data: BrandResponse }> {
    return this.http.get<{ data: BrandResponse }>(`${this.apiUrl}/${id}`);
  }

  /** Look up a brand by its unique code (e.g. "NGK"). */
  getBrandByCode(code: string): Observable<{ data: BrandResponse }> {
    const params = new HttpParams().set('code', code);
    return this.http.get<{ data: BrandResponse }>(`${this.apiUrl}/by-code`, { params });
  }

  /** Create a new brand. Returns the created brand wrapped in { data }. */
  createBrand(brand: CreateBrandRequest): Observable<{ data: BrandResponse }> {
    return this.http.post<{ data: BrandResponse }>(this.apiUrl, brand);
  }

  /** Full replace of a brand. ID is in the URL only — not in the body. */
  updateBrand(id: string, brand: UpdateBrandRequest): Observable<{ data: BrandResponse }> {
    return this.http.put<{ data: BrandResponse }>(`${this.apiUrl}/${id}`, brand);
  }

  /** Soft delete a brand. */
  deleteBrand(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /** All active brands as a flat array — convenience for dropdowns. */
  getActiveBrands(): Observable<BrandResponse[]> {
    return this.getBrands({ isActive: true, pageSize: 500 }).pipe(map(r => r.data));
  }

  /** Unwrap single brand from the { data } envelope. */
  getBrandByIdValue(id: string): Observable<BrandResponse> {
    return this.getBrandById(id).pipe(map(r => r.data));
  }
}
