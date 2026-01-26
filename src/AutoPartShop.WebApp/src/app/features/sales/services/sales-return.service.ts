import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface SalesReturnLineRequest {
  salesOrderLineId: string;
  partId: string;
  quantity: number;
  unitPrice: number;
  condition: string; // UNOPENED, OPENED, DAMAGED
  notes: string;
}

export interface CreateSalesReturnRequest {
  salesOrderId: string;
  warehouseId: string;
  reason: string; // DAMAGED, DEFECTIVE, WRONG_ITEM, EXCESS_STOCK
  refundType: string; // CASH_REFUND, STORE_CREDIT
  notes: string;
  lines: SalesReturnLineRequest[];
}

export interface SalesReturnLineResponse {
  id: string;
  partId: string;
  quantity: number;
  unitPrice: number;
  refundAmount: number;
  condition: string;
  notes: string;
}

export interface SalesReturnResponse {
  id: string;
  returnNumber: string;
  salesOrderId: string;
  salesOrderNumber?: string; // Sales Order Number
  warehouseId: string;
  reason: string;
  status: string; // PENDING, APPROVED, RECEIVED, REJECTED, PROCESSED
  totalRefundAmount: number;
  refundType: string; // CASH_REFUND, STORE_CREDIT
  notes: string;
  lines: SalesReturnLineResponse[];
  createdAt: string;
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
export class SalesReturnService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5292/api/SalesReturn';

  getAllSalesReturns(): Observable<SalesReturnResponse[]> {
    return this.http.get<SalesReturnResponse[]>(this.apiUrl);
  }

  getSalesReturns(
    pageNumber: number = 1,
    pageSize: number = 10,
    searchTerm?: string
  ): Observable<PaginatedResponse<SalesReturnResponse>> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (searchTerm) {
      params = params.set('searchTerm', searchTerm);
    }

    return this.http.get<PaginatedResponse<SalesReturnResponse>>(`${this.apiUrl}/list`, { params });
  }

  getSalesReturnById(id: string): Observable<SalesReturnResponse> {
    return this.http.get<SalesReturnResponse>(`${this.apiUrl}/${id}`);
  }

  createSalesReturn(request: CreateSalesReturnRequest): Observable<SalesReturnResponse> {
    return this.http.post<SalesReturnResponse>(this.apiUrl, request);
  }

  approveSalesReturn(id: string): Observable<SalesReturnResponse> {
    return this.http.patch<SalesReturnResponse>(`${this.apiUrl}/${id}/approve`, {});
  }

  rejectSalesReturn(id: string, reason?: string): Observable<SalesReturnResponse> {
    return this.http.patch<SalesReturnResponse>(`${this.apiUrl}/${id}/reject`, { reason });
  }

  receiveSalesReturn(id: string): Observable<SalesReturnResponse> {
    return this.http.patch<SalesReturnResponse>(`${this.apiUrl}/${id}/receive`, {});
  }

  processSalesReturn(id: string): Observable<SalesReturnResponse> {
    return this.http.patch<SalesReturnResponse>(`${this.apiUrl}/${id}/process`, {});
  }
}
