import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface WarehouseResponse {
  id: string;
  name: string;
  code: string;
  location: string;
  capacity: number;
  description: number;
  currentStock: number;
  createdAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class WarehouseService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/warehouses';

  /**
   * Get all warehouses
   */
  getAllWarehouses(): Observable<WarehouseResponse[]> {
    return this.http.get<WarehouseResponse[]>(this.apiUrl);
  }

  /**
   * Get warehouse by ID
   */
  getWarehouseById(id: string): Observable<WarehouseResponse> {
    return this.http.get<WarehouseResponse>(`${this.apiUrl}/${id}`);
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
   * Delete warehouse
   */
  deleteWarehouse(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
