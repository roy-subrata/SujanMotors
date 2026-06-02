import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { PaginatedResponse } from './stock-lot.service';
import { map } from 'rxjs/operators';

export interface WarehouseResponse {
  id: string;
  name: string;
  code: string;
  location: string;
  city?: string;
  state?: string;
  country?: string;
  postalCode?: string;
  manager?: string;
  managerEmail?: string;
  managerPhone?: string;
  storageCapacity?: number;
  capacityUnit?: string;
  description: string;
  isActive?: boolean;
  createdBy?: string;
  modifiedBy?: string;

  // Legacy UI fields (backward compatible)
  capacity: number;
  currentStock: number;
  createdAt: string;
}

export interface WarehouseQuery {
  search: string;
  pageSize: number;
  pageNumber: number;
  sorts?: Array<{ field: string; direction: 'asc' | 'desc' }>;
}

@Injectable({
  providedIn: 'root'
})
export class WarehouseService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/v1/warehouses`;


  /**
   * Get all warehouses (no dedicated backend GET endpoint; uses `/list`)
   */
  getAllWarehouses(): Observable<WarehouseResponse[]> {
    return this.getWarehouses({ search: '', pageNumber: 1, pageSize: 1000 }).pipe(
      map(res => res.data ?? [])
    );
  }

  getWarehouses(query: WarehouseQuery): Observable<PaginatedResponse<WarehouseResponse>> {
    return this.http.post<PaginatedResponse<WarehouseResponse>>(`${this.apiUrl}/list`, query);
  }


  /**
   * Get warehouse by ID
   */
  getWarehouseById(id: string): Observable<WarehouseResponse> {
    return this.http.get<WarehouseResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get warehouse by code
   */
  getWarehouseByCode(code: string): Observable<WarehouseResponse> {
    return this.http.get<WarehouseResponse>(`${this.apiUrl}/code/${encodeURIComponent(code)}`);
  }

  /**
   * Create warehouse
   */
  createWarehouse(warehouse: Partial<WarehouseResponse>): Observable<WarehouseResponse> {
    return this.http.post<WarehouseResponse>(this.apiUrl, warehouse);
  }

  /**
   * Update warehouse
   */
  updateWarehouse(id: string, warehouse: Partial<WarehouseResponse>): Observable<WarehouseResponse> {
    return this.http.put<WarehouseResponse>(`${this.apiUrl}/${id}`, warehouse);
  }

  /**
   * Activate warehouse
   */
  activateWarehouse(id: string): Observable<WarehouseResponse> {
    return this.http.patch<WarehouseResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate warehouse
   */
  deactivateWarehouse(id: string): Observable<WarehouseResponse> {
    return this.http.patch<WarehouseResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Delete warehouse
   */
  deleteWarehouse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
