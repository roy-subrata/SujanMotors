import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface UnitResponse {
  id: string;
  name: string;
  code: string;
  symbol: string;
  description: string;
  isActive: boolean;
  displayOrder: number;
  createdBy: string;
  modifiedBy: string;
}

export interface CreateUnitRequest {
  name: string;
  code: string;
  symbol: string;
  description: string;
}

export interface UpdateUnitRequest {
  name: string;
  code: string;
  symbol: string;
  description: string;
  isActive: boolean;
  displayOrder: number;
}

export interface UnitListResponse {
  data: UnitResponse[];
  pagination: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class UnitService {
  private readonly apiUrl = `${environment.apiUrl}/units`;

  constructor(private http: HttpClient) {}

  /**
   * Get all units
   */
  getAllUnits(): Observable<UnitResponse[]> {
    return this.http.get<UnitResponse[]>(this.apiUrl);
  }

  /**
   * Get active units only
   */
  getActiveUnits(): Observable<UnitResponse[]> {
    return this.http.get<UnitResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get unit by ID
   */
  getUnitById(id: string): Observable<UnitResponse> {
    return this.http.get<UnitResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Get units with pagination and optional search
   */
  getListUnits(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm: string = ''
  ): Observable<UnitListResponse> {
    let params = new HttpParams();
    params = params.set('pageNumber', pageNumber.toString());
    params = params.set('pageSize', pageSize.toString());
    if (searchTerm.trim()) {
      params = params.set('searchTerm', searchTerm);
    }
    return this.http.get<UnitListResponse>(`${this.apiUrl}/list`, { params });
  }

  /**
   * Create a new unit
   */
  createUnit(request: CreateUnitRequest): Observable<UnitResponse> {
    return this.http.post<UnitResponse>(this.apiUrl, request);
  }

  /**
   * Update an existing unit
   */
  updateUnit(id: string, request: UpdateUnitRequest): Observable<UnitResponse> {
    return this.http.put<UnitResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Delete a unit
   */
  deleteUnit(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Activate a unit
   */
  activateUnit(id: string): Observable<UnitResponse> {
    return this.http.patch<UnitResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate a unit
   */
  deactivateUnit(id: string): Observable<UnitResponse> {
    return this.http.patch<UnitResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Get compatible units for a given base unit
   * Returns the base unit itself plus all units that have conversions configured
   */
  getCompatibleUnits(baseUnitId: string): Observable<UnitResponse[]> {
    return this.http.get<UnitResponse[]>(`${this.apiUrl}/${baseUnitId}/compatible-units`);
  }
}
