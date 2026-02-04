import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PaginatedResponse } from '@/features/sales/services/customer.service';
import { environment } from 'src/environments/environment';

export interface SupplierResponse {
  id: string;
  name: string;
  code: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  country: string;
  postalCode: string;
  currentBalance: number;
  isActive: boolean;
  rating: number;
  createdBy: string;
  modifiedBy: string;
}

export interface CreateSupplierRequest {
  name: string;
  code: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  country: string;
  postalCode: string;
  paymentTerms: string;
  creditLimit: number;
}

export interface UpdateSupplierRequest {
  id: string;
  name: string;
  contactPerson: string;
  email: string;
  phone: string;
  address: string;
  city: string;
  state: string;
  country: string;
  postalCode: string;
  paymentTerms: string;
  creditLimit: number;
  isActive: boolean;
}

export interface PaginatedSupplierResponse {
  items: SupplierResponse[];
  pageNumber: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface SupplierQuery {
    search: string;
    pageSize: number;
    pageNumber: number;
    customerType?: string;
}
@Injectable({
  providedIn: 'root'
})
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiUrl}/suppliers`;

  /**
   * Get all suppliers
   */
  getAllSuppliers(): Observable<SupplierResponse[]> {
    return this.http.get<SupplierResponse[]>(this.apiUrl);
  }

  /**
   * Get all active suppliers
   */
  getActiveSuppliers(): Observable<SupplierResponse[]> {
    return this.http.get<SupplierResponse[]>(`${this.apiUrl}/active`);
  }

  /**
   * Get paginated suppliers with optional search
   */
  getSuppliers(rQuery:SupplierQuery): Observable<PaginatedResponse<SupplierResponse>> {
    return this.http.post<PaginatedResponse<SupplierResponse>>(`${this.apiUrl}/list`,rQuery);
  }

  /**
   * Get supplier by ID
   */
  getSupplierById(id: string): Observable<SupplierResponse> {
    return this.http.get<SupplierResponse>(`${this.apiUrl}/${id}`);
  }

  /**
   * Create new supplier
   */
  createSupplier(request: CreateSupplierRequest): Observable<SupplierResponse> {
    return this.http.post<SupplierResponse>(this.apiUrl, request);
  }

  /**
   * Update existing supplier
   */
  updateSupplier(id: string, request: UpdateSupplierRequest): Observable<SupplierResponse> {
    return this.http.put<SupplierResponse>(`${this.apiUrl}/${id}`, request);
  }

  /**
   * Activate supplier
   */
  activateSupplier(id: string): Observable<SupplierResponse> {
    return this.http.patch<SupplierResponse>(`${this.apiUrl}/${id}/activate`, {});
  }

  /**
   * Deactivate supplier
   */
  deactivateSupplier(id: string): Observable<SupplierResponse> {
    return this.http.patch<SupplierResponse>(`${this.apiUrl}/${id}/deactivate`, {});
  }

  /**
   * Delete supplier
   */
  deleteSupplier(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  /**
   * Set supplier rating
   */
  setSupplierRating(id: string, rating: number): Observable<SupplierResponse> {
    return this.http.patch<SupplierResponse>(`${this.apiUrl}/${id}/rating`, { rating });
  }
}
