import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ProductLocationResponse {
  id: string;
  partId: string;
  partName: string;
  partSKU: string;
  warehouseId: string;
  warehouseName: string;
  warehouseCode: string;
  section: string;
  shelf: string;
  fullLocation: string;
  notes: string;
  isPrimary: boolean;
  createdBy: string;
  createdAt: string;
}

export interface CreateProductLocationRequest {
  partId: string;
  warehouseId: string;
  section: string;
  shelf: string;
  isPrimary: boolean;
  notes?: string;
}

export interface UpdateProductLocationRequest {
  section: string;
  shelf: string;
  isPrimary: boolean;
  notes?: string;
}

export interface SetPrimaryLocationRequest {
  locationId: string;
}

@Injectable({
  providedIn: 'root'
})
export class ProductLocationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/productlocations';

  /**
   * Get all product locations
   */
  getAllLocations(): Observable<ProductLocationResponse[]> {
    return this.http.get<ProductLocationResponse[]>(this.apiUrl);
  }

  /**
   * Get product location by ID
   */
  getLocationById(id: string): Observable<ProductLocationResponse> {
    return this.http.get<ProductLocationResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get all locations for a specific part
   */
  getLocationsByPart(partId: string): Observable<ProductLocationResponse[]> {
    return this.http.get<ProductLocationResponse[]>(`${this.apiUrl}/part/${partId}`);
  }

  /**
   * Get all locations in a specific warehouse
   */
  getLocationsByWarehouse(warehouseId: string): Observable<ProductLocationResponse[]> {
    return this.http.get<ProductLocationResponse[]>(`${this.apiUrl}/warehouse/${warehouseId}`);
  }

  /**
   * Get primary location for a part
   */
  getPrimaryLocation(partId: string): Observable<ProductLocationResponse> {
    return this.http.get<ProductLocationResponse>(`${this.apiUrl}/part/${partId}/primary`);
  }

  /**
   * Create a new product location
   */
  createLocation(request: CreateProductLocationRequest): Observable<ProductLocationResponse> {
    return this.http.post<ProductLocationResponse>(this.apiUrl, request);
  }

  /**
   * Update a product location
   */
  updateLocation(id: string, request: UpdateProductLocationRequest): Observable<ProductLocationResponse> {
    return this.http.put<ProductLocationResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Set a location as the primary location for a part
   */
  setPrimaryLocation(partId: string, request: SetPrimaryLocationRequest): Observable<any> {
    return this.http.patch(`${this.apiUrl}/part/${partId}/set-primary`, request);
  }

  /**
   * Delete a product location
   */
  deleteLocation(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
