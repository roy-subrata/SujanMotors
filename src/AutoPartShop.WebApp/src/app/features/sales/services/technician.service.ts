import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';

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

export interface TechnicianQuery {
    search?: string;
    pageSize: number;
    pageNumber: number;
}

@Injectable({ providedIn: 'root' })
export class TechnicianService {
    private readonly http = inject(HttpClient);
    private readonly apiUrl = `${environment.apiUrl}/technician`;

    getAllTechnicians(): Observable<TechnicianResponse[]> {
        return this.http.get<TechnicianResponse[]>(this.apiUrl);
    }

    getTechnicians(query:TechnicianQuery): Observable<PaginatedResponse<TechnicianResponse>> {
        return this.http.post<PaginatedResponse<TechnicianResponse>>(`${this.apiUrl}/list`, query);
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

    searchTechnicians(query: string): Observable<TechnicianResponse[]> {
        return this.http.get<TechnicianResponse[]>(`${this.apiUrl}/search`, {
            params: new HttpParams().set('query', query)
        });
    }

    getActiveTechnicians(): Observable<TechnicianResponse[]> {
        return this.http.get<TechnicianResponse[]>(`${this.apiUrl}/active`);
    }
}
