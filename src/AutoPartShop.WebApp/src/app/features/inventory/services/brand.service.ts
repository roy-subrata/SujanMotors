import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from '@/features/sales/services/customer.service';

export interface BrandResponse {
  id: string;
  name: string;
  code: string;
  description: string;
  logoUrl: string;
  website: string;
  country: string;
  contactEmail: string;
  contactPhone: string;
  displayOrder: number;
  isActive: boolean;
  createdAt: string;
  modifiedAt: string | null;
}

export interface CreateBrandRequest {
  name: string;
  code: string;
  description: string;
  country: string;
}

export interface UpdateBrandRequest {
  id: string;
  name: string;
  code: string;
  description: string;
  logoUrl: string;
  website: string;
  country: string;
  contactEmail: string;
  contactPhone: string;
  displayOrder: number;
  isActive: boolean;
}

export interface SortOption {
  field: string;
  direction: 'asc' | 'desc';
}

export interface BrandQuery {
  pageSize: number;
  pageNumber: number;
  search?: string;
  isActive?: boolean | null;
  country?: string;
  sorts?: SortOption[];
}

@Injectable({
  providedIn: 'root'
})
export class BrandService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/brands`;

  /**
   * Get all brands
   */
  getAllBrands(): Observable<BrandResponse[]> {
    return this.http.get<BrandResponse[]>(this.apiUrl);
  }

  /**
   * Get paginated brands with optional search filter
   */
  getBrands(query: BrandQuery): Observable<PaginatedResponse<BrandResponse>> {
    return this.http.post<PaginatedResponse<BrandResponse>>(`${this.apiUrl}/list`, query);
  }

  /**
   * Get active brands only
   */
  getActiveBrands(): Observable<BrandResponse[]> {
    return this.http.get<BrandResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get brand by ID
   */
  getBrandById(id: string): Observable<BrandResponse> {
    return this.http.get<BrandResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get brand by code
   */
  getBrandByCode(code: string): Observable<BrandResponse> {
    return this.http.get<BrandResponse>(`${this.apiUrl}/code/${code}`);
  }

  /**
   * Create a new brand
   */
  createBrand(brand: CreateBrandRequest): Observable<BrandResponse> {
    return this.http.post<BrandResponse>(this.apiUrl, brand);
  }

  /**
   * Update an existing brand
   */
  updateBrand(id: string, brand: UpdateBrandRequest): Observable<BrandResponse> {
    return this.http.put<BrandResponse>(`${this.apiUrl}/${id}`, brand);
  }

  /**
   * Delete a brand (soft delete)
   */
  deleteBrand(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Activate a brand
   */
  activateBrand(id: string): Observable<BrandResponse> {
    return this.http.patch<BrandResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate a brand
   */
  deactivateBrand(id: string): Observable<BrandResponse> {
    return this.http.patch<BrandResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }
}
