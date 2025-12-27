import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';

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
  paymentTerms: string;
  creditLimit: number;
  bankName: string;
  bankAccountNumber: string;
  bankIFSC: string;
  taxID: string;
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

@Injectable({
  providedIn: 'root'
})
export class SupplierService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/suppliers';

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
  getSuppliers(pageNumber: number, pageSize: number, searchTerm?: string): Observable<PaginatedSupplierResponse> {
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
