import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

export interface UnitConversionResponse {
  id: string;
  fromUnitId: string;
  toUnitId: string;
  fromUnitName: string;
  fromUnitCode: string;
  toUnitName: string;
  toUnitCode: string;
  conversionFactor: number;
  description: string;
  isActive: boolean;
  createdBy: string;
  modifiedBy: string;
}

export interface CreateUnitConversionRequest {
  fromUnitId: string;
  toUnitId: string;
  conversionFactor: number;
  description: string;
}

export interface UpdateUnitConversionRequest {
  conversionFactor: number;
  description: string;
  isActive: boolean;
}

export interface UnitConversionListResponse {
  data: UnitConversionResponse[];
  pagination?: {
    pageNumber: number;
    pageSize: number;
    totalCount: number;
    totalPages: number;
  };
}

@Injectable({
  providedIn: 'root'
})
export class UnitConversionService {
  private readonly apiUrl = `${environment.apiUrl}/v1/units`;

  constructor(private http: HttpClient) {}

  /**
   * Get conversions with pagination and search
   */
  getListConversions(pageNumber: number, pageSize: number, searchTerm?: string): Observable<UnitConversionListResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());
    
    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<UnitConversionListResponse>(`${this.apiUrl}/conversions/list`, { params });
  }

  /**
   * Get all unit conversions
   */
  getAllConversions(): Observable<UnitConversionResponse[]> {
    return this.http.get<UnitConversionResponse[]>(`${this.apiUrl}/conversions/all`);
  }

  /**
   * Get conversions for a specific unit
   */
  getConversionsForUnit(unitId: string): Observable<UnitConversionResponse[]> {
    return this.http.get<UnitConversionResponse[]>(`${this.apiUrl}/${unitId}/conversions`);
  }

  /**
   * Get specific conversion between two units
   */
  getConversion(fromUnitId: string, toUnitId: string): Observable<UnitConversionResponse> {
    return this.http.get<UnitConversionResponse>(
      `${this.apiUrl}/conversions/${fromUnitId}/to/${toUnitId}`
    );
  }

  /**
   * Create a new unit conversion
   */
  createConversion(request: CreateUnitConversionRequest): Observable<UnitConversionResponse> {
    return this.http.post<UnitConversionResponse>(`${this.apiUrl}/conversions`, request);
  }

  /**
   * Update a unit conversion
   */
  updateConversion(id: string, request: UpdateUnitConversionRequest): Observable<UnitConversionResponse> {
    return this.http.put<UnitConversionResponse>(`${this.apiUrl}/conversions/${id}`, request);
  }

  /**
   * Delete a unit conversion
   */
  deleteConversion(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/conversions/${id}`);
  }
}
