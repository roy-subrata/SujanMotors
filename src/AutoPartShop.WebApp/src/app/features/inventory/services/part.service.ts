import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface PartResponse {
  id: string;
  name: string;
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
  costPrice: number;
  sellingPrice: number;
  minimumStock: number;
  isActive: boolean;
  createdBy: string;
  modifiedBy: string;
}

export interface CreatePartRequest {
  name: string;
  description: string;
  partNumber: string;
  sku: string;
  categoryId: string;
  brandId: string | null;
  unitId: string | null;
  costPrice: number;
  sellingPrice: number;
  minimumStock: number;
}

export interface UpdatePartRequest {
  id: string;
  name: string;
  description: string;
  sku: string;
  categoryId: string;
  brandId: string | null;
  unitId: string | null;
  costPrice: number;
  sellingPrice: number;
  minimumStock: number;
  isActive: boolean;
}

export interface PaginatedPartResponse {
  items: PartResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface VehicleCompatibilityResponse {
  id: string;
  partId: string;
  vehicleId: string;
  vehicleMake: string;
  vehicleModel: string;
  vehicleYear: number;
  vehicleEngineType: string;
  isCompatible: boolean;
  notes: string;
}

@Injectable({
  providedIn: 'root'
})
export class PartService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/parts';

  /**
   * Get all parts
   */
  getAllParts(): Observable<PartResponse[]> {
    return this.http.get<PartResponse[]>(this.apiUrl);
  }

  /**
   * Get all active parts
   */
  getActiveParts(): Observable<PartResponse[]> {
    return this.http.get<PartResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get paginated parts with optional search
   */
  getParts(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedPartResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<any>(`${this.apiUrl}/list`, { params }).pipe(
      map(response => ({
        items: response.data,
        pageNumber: response.pagination.pageNumber,
        pageSize: response.pagination.pageSize,
        totalCount: response.pagination.totalCount,
        totalPages: response.pagination.totalPages,
        hasPreviousPage: response.pagination.pageNumber > 1,
        hasNextPage: response.pagination.pageNumber < response.pagination.totalPages
      }))
    );
  }

  /**
   * Get part by ID
   */
  getPartById(id: string): Observable<PartResponse> {
    return this.http.get<PartResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new part
   */
  createPart(request: CreatePartRequest): Observable<PartResponse> {
    return this.http.post<PartResponse>(this.apiUrl, request);
  }

  /**
   * Update existing part
   */
  updatePart(id: string, request: UpdatePartRequest): Observable<PartResponse> {
    return this.http.put<PartResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Activate part
   */
  activatePart(id: string): Observable<PartResponse> {
    return this.http.patch<PartResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate part
   */
  deactivatePart(id: string): Observable<PartResponse> {
    return this.http.patch<PartResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Delete part
   */
  deletePart(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get compatible vehicles for a part
   */
  getPartCompatibleVehicles(partId: string): Observable<VehicleCompatibilityResponse[]> {
    return this.http.get<VehicleCompatibilityResponse[]>(`${this.apiUrl}/${partId}/compatible-vehicles`);
  }
}
