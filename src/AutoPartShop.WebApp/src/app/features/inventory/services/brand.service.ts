import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

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
}
