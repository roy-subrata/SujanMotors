import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface TechnicianResponse {
  id: string;
  technicianCode: string;
  name: string;
  phone: string;
  email: string;
  shopName: string;
  address: string;
  city: string;
  status: string;
  notes: string;
  createdAt: string;
}

export interface CreateTechnicianRequest {
  technicianCode: string;
  name: string;
  phone: string;
  email: string;
  shopName: string;
  address: string;
  city: string;
  notes: string;
}

export interface UpdateTechnicianRequest {
  name: string;
  phone: string;
  email: string;
  shopName: string;
  address: string;
  city: string;
  notes: string;
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
export class TechnicianService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/technician';

  getAllTechnicians(): Observable<TechnicianResponse[]> {
    return this.http.get<TechnicianResponse[]>(this.apiUrl);
  }

  getTechnicians(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PaginatedResponse<TechnicianResponse>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PaginatedResponse<TechnicianResponse>>(`${this.apiUrl}/list`, { params });
  }

  getTechnicianById(id: string): Observable<TechnicianResponse> {
    return this.http.get<TechnicianResponse>(`${this.apiUrl}/${id}`);
  }

  createTechnician(request: CreateTechnicianRequest): Observable<TechnicianResponse> {
    return this.http.post<TechnicianResponse>(this.apiUrl, request);
  }

  updateTechnician(id: string, request: UpdateTechnicianRequest): Observable<TechnicianResponse> {
    return this.http.put<TechnicianResponse>(`${this.apiUrl}/${id}`, request);
  }

  activateTechnician(id: string): Observable<TechnicianResponse> {
    return this.http.patch<TechnicianResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  deactivateTechnician(id: string): Observable<TechnicianResponse> {
    return this.http.patch<TechnicianResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  deleteTechnician(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
